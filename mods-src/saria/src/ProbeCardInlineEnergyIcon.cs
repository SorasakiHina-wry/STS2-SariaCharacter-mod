using System;
using System.Text.RegularExpressions;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Saria;

/// <summary>
/// 卡牌描述里的内联能量图标。
///
/// 背景：`EnergyIconsFormatter` 生成的是
/// `[img]res://images/packed/sprite_fonts/&lt;前缀&gt;_energy_icon.png[/img]`。
/// 这条 `res://` 路径在导出包里**没有对应的资源加载器**（运行日志会报
/// `No loader found for resource: ... (expected type: Texture2D)`），
/// 所以无论用 `TakeOverPath` 还是真文件都读不出来，正文里只剩一个空白框。
///
/// 现在改为**不走加载器**：把描述按图标标签切开，用 RichTextLabel.AddImage(texture)
/// 直接把运行时解码的贴图插进正文。
/// </summary>
[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
public static class ProbeCardInlineEnergyIconPatch
{
    private static readonly Regex IconTag = new(
        @"\[img\]res://images/packed/sprite_fonts/(?:saria|ironclad)_energy_icon\.png\[/img\]",
        RegexOptions.Compiled);

    private static readonly System.Reflection.FieldInfo? DescriptionField =
        AccessTools.Field(typeof(NCard), "_descriptionLabel");

    private static int _logged;

    [HarmonyPostfix]
    private static void Postfix(NCard __instance)
    {
        try
        {
            if (!ProbeEnergyPrefix.IsOurs(__instance.Model))
                return;

            if (DescriptionField?.GetValue(__instance) is not RichTextLabel label)
                return;

            string text = label.Text;
            if (string.IsNullOrEmpty(text) || !IconTag.IsMatch(text))
                return;

            var texture = ProbeEnergyIcons.TextTexture;
            if (texture == null)
                return;

            // 先按「图标占两个字符宽」的占位文本量一次字号，再重建带图片的正文。
            // MegaRichTextLabel 的自适应是延迟执行的，所以这里只是把字号调到接近的值。
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
                Log.Info($"[saria][energy-icon] inline icon inserted on {__instance.Model.GetType().Name}");
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][energy-icon] inline rewrite failed: {ex.Message}");
        }
    }
}
