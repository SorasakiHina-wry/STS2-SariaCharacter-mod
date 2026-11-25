using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Saria;

/// <summary>
/// 卡牌描述里的内联能量图标。
///
/// `EnergyIconsFormatter` 生成的是
/// `[img]res://images/packed/sprite_fonts/ironclad_energy_icon.png[/img]`
/// （前缀来自 `EnergyIconHelper.GetPrefix`，而那个方法在游戏里会被内联，
/// 我们的 `GetPrefix` 补丁实测根本不会被调用）。
///
/// 所以在 `NCard.UpdateVisuals` 之后直接改描述文本：把铁甲战士的 sprite-font 路径
/// 换成我们注册过虚拟路径（`ProbeEnergyIcons` 用 `TakeOverPath` 占住的那条，24×24）。
/// </summary>
[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
public static class ProbeCardInlineEnergyIconPatch
{
    private const string FromPath = "res://images/packed/sprite_fonts/ironclad_energy_icon.png";

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
            if (string.IsNullOrEmpty(text) || !text.Contains(FromPath, StringComparison.Ordinal))
                return;

            string replaced = text.Replace(FromPath, ProbeEnergyIcons.TextIconPath, StringComparison.Ordinal);

            // MegaRichTextLabel 有自己的字号自适应，直接改 Text 会绕过它，所以优先走 SetTextAutoSize。
            var setAutoSize = AccessTools.Method(label.GetType(), "SetTextAutoSize", new[] { typeof(string) });
            if (setAutoSize != null)
                setAutoSize.Invoke(label, new object[] { replaced });
            else
                label.Text = replaced;

            if (_logged < 3)
            {
                _logged++;
                Log.Info($"[saria][energy-icon] inline icon rewritten on {__instance.Model.GetType().Name}");
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][energy-icon] inline rewrite failed: {ex.Message}");
        }
    }
}
