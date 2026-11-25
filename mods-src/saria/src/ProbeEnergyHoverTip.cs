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
/// mod 的贴图不在导出包里，所以这里后置替换成运行时贴图）。
///
/// ⚠️ `HoverTip` 是 **record struct**，方法返回的是装箱后的 `IHoverTip`。
/// 之前把 `__result` 模式匹配成 `HoverTip tip` 再调用 setter，改的是**拆箱拷贝**，
/// 所以日志显示替换成功、界面却还是铁甲战士的图标。
/// 现在直接把反射 setter 作用在装箱对象上（`ref IHoverTip __result`）。
/// </summary>
[HarmonyPatch(typeof(HoverTipFactory), "ForEnergyWithIconPath")]
public static class ProbeEnergyHoverTipPatch
{
    private static readonly System.Reflection.MethodInfo? IconSetter =
        AccessTools.PropertySetter(typeof(HoverTip), nameof(HoverTip.Icon))
        ?? AccessTools.Property(typeof(HoverTip), nameof(HoverTip.Icon))?.GetSetMethod(true);

    private static int _logged;

    [HarmonyPostfix]
    private static void Postfix(ref IHoverTip __result)
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

            // 作用在装箱的 HoverTip 上（ref 参数就是那个箱子），改动才能带回返回值。
            IconSetter.Invoke(__result, new object?[] { texture });

            if (_logged < 3)
            {
                _logged++;
                Log.Info("[saria][orb] energy hover tip icon replaced");
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][orb] hover tip icon failed: {ex.Message}");
        }
    }
}
