using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Combat;

namespace Saria;

/// <summary>
/// 能量悬浮提示里的图标（原版 `HoverTipFactory.ForEnergyWithIconPath` 用 `PreloadManager` 按 res:// 路径取图，
/// mod 的 PCK 贴图加载不了，所以这里后置替换）。
///
/// 判定条件两条同时满足才替换：
///   1. 当前本地角色是塞雷娅（`LocalContext.GetMe(RunManager.Instance.State)`）；
///   2. 提示自带的图标是铁甲战士的能量图标（`energy_ironclad`）——即"这张牌的卡池就是我们的"，
///      避免把无色/其他卡池的提示也换掉。
/// </summary>
[HarmonyPatch(typeof(HoverTipFactory), "ForEnergyWithIconPath")]
public static class ProbeEnergyHoverTipPatch
{
    private static readonly System.Reflection.MethodInfo? IconSetter =
        AccessTools.PropertySetter(typeof(HoverTip), nameof(HoverTip.Icon))
        ?? AccessTools.Property(typeof(HoverTip), nameof(HoverTip.Icon))?.GetSetMethod(true);

    [HarmonyPostfix]
    private static void Postfix(IHoverTip __result)
    {
        try
        {
            if (__result is not HoverTip tip || tip.Icon == null)
                return;

            string? iconPath = tip.Icon.ResourcePath;
            if (iconPath == null || !iconPath.Contains("energy_ironclad"))
                return;

            var me = LocalContext.GetMe(CombatManager.Instance.DebugOnlyGetState())?.Character;
            if (me?.Id.Entry is not string entry || !entry.EndsWith("SARIA", StringComparison.Ordinal))
                return;

            var texture = ProbeOrbSkin.Load("energy_saria.png");
            if (texture == null || IconSetter == null)
                return;

            IconSetter.Invoke(tip, new object?[] { texture });
            Log.Info("[saria][orb] energy hover tip icon replaced");
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][orb] hover tip icon failed: {ex.Message}");
        }
    }
}
