using System;
using System.Text.RegularExpressions;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.addons.mega_text;

namespace Saria;

/// <summary>
/// 把正文里的能量图标标签换成**运行时贴图**。
///
/// 为什么不能直接让 `[img]res://...png[/img]` 生效：
/// 导出包里没有 `.png` 的资源加载器，日志会报
/// `No loader found for resource: ... (expected type: Texture2D)`，
/// 正文里只剩空白框。所以统一改成「按标签切开 → AppendText + AddImage」。
/// </summary>
internal static class ProbeInlineEnergyText
{
    internal static readonly Regex IconTag = new(
        @"\[img\]res://images/packed/sprite_fonts/(?:saria|ironclad)_energy_icon\.png\[/img\]",
        RegexOptions.Compiled);

    private static int _logged;

    /// <summary>就地重写 label 里的能量图标；返回是否发生了替换。</summary>
    internal static bool Replace(RichTextLabel? label)
    {
        if (label == null)
            return false;

        string text = label.Text;
        if (string.IsNullOrEmpty(text) || !IconTag.IsMatch(text))
            return false;

        var texture = ProbeEnergyIcons.TextTexture;
        if (texture == null)
            return false;

        try
        {
            // 先用「图标占两个字符」的占位文本量一次字号，再重建真实内容。
            var setAutoSize = AccessTools.Method(label.GetType(), "SetTextAutoSize", new[] { typeof(string) });
            setAutoSize?.Invoke(label, new object[] { IconTag.Replace(text, "OO") });

            label.Clear();
            int last = 0;
            foreach (Match match in IconTag.Matches(text))
            {
                if (match.Index > last)
                    label.AppendText(text.Substring(last, match.Index - last));

                label.AddImage(texture, 0, 0, null, InlineAlignment.Center);
                last = match.Index + match.Length;
            }

            if (last < text.Length)
                label.AppendText(text.Substring(last));

            if (_logged < 5)
            {
                _logged++;
                Log.Info("[saria][energy-icon] inline icon inserted");
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][energy-icon] inline rewrite failed: {ex.Message}");
            return false;
        }
    }
}

/// <summary>卡牌描述（NCard.UpdateVisuals 之后）。</summary>
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.Cards.NCard), "UpdateVisuals")]
public static class ProbeCardInlineEnergyIconPatch
{
    private static readonly System.Reflection.FieldInfo? DescriptionField =
        AccessTools.Field(typeof(MegaCrit.Sts2.Core.Nodes.Cards.NCard), "_descriptionLabel");

    [HarmonyPostfix]
    private static void Postfix(MegaCrit.Sts2.Core.Nodes.Cards.NCard __instance)
    {
        if (DescriptionField?.GetValue(__instance) is RichTextLabel label)
            ProbeInlineEnergyText.Replace(label);
    }
}

/// <summary>
/// 通用悬浮提示（遗物 / 状态 / 事件等）：`NHoverTipSet.Init` 里给每个 tip 的 %Description 赋值之后，
/// 把能量图标换成运行时贴图。原版遗物（双截棍、孙子兵法…）的描述里就带 `[img]...ironclad_energy_icon.png`。
/// </summary>
[HarmonyPatch(typeof(MegaCrit.Sts2.Core.Nodes.HoverTips.NHoverTipSet), "Init")]
public static class ProbeTooltipEnergyIconPatch
{
    private static readonly System.Reflection.FieldInfo? ContainerField =
        AccessTools.Field(typeof(MegaCrit.Sts2.Core.Nodes.HoverTips.NHoverTipSet), "_textHoverTipContainer");

    private static string? _energyTitle;
    private static int _logged;

    /// <summary>「能量」提示的本地化标题（用原版工厂拿，避免硬编码语言）。</summary>
    private static string? TryGetEnergyTitle()
    {
        try
        {
            var tip = HoverTipFactory.Static(StaticHoverTip.Energy);
            return tip is HoverTip hoverTip ? hoverTip.Title : null;
        }
        catch
        {
            return null;
        }
    }

    [HarmonyPostfix]
    private static void Postfix(MegaCrit.Sts2.Core.Nodes.HoverTips.NHoverTipSet __instance)
    {
        if (!ProbeEnergyPrefix.IsLocalSaria())
            return;

        if (ContainerField?.GetValue(__instance) is not Control container)
            return;

        // 「能量」词义提示的图标：它的 HoverTip 由 ForEnergyWithIconPath 构建，
        // 而那个方法可能被内联导致补丁不触发；这里在节点层兜底按标题识别并换图。
        string? energyTitle = _energyTitle ??= TryGetEnergyTitle();

        foreach (Node child in container.GetChildren())
        {
            if (child is not Control control)
                continue;

            if (control.GetNodeOrNull<RichTextLabel>("%Description") is { } label)
                ProbeInlineEnergyText.Replace(label);

            if (energyTitle == null)
                continue;

            var titleLabel = control.GetNodeOrNull<MegaLabel>("%Title");
            if (titleLabel == null || !string.Equals(titleLabel.Text, energyTitle, StringComparison.Ordinal))
                continue;

            var icon = control.GetNodeOrNull<TextureRect>("%Icon");
            var texture = ProbeOrbSkin.Load("energy_saria.png");
            if (icon != null && texture != null && icon.Texture != texture)
            {
                icon.Texture = texture;
                icon.Visible = true;
                if (_logged < 5)
                {
                    _logged++;
                    Log.Info("[saria][orb] energy keyword tip icon replaced");
                }
            }
        }
    }
}
