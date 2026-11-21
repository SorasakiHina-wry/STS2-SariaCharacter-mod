using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Saria;

/// <summary>
/// 能量图标只在内联文本里用「自己的前缀」，其它路径一律保持原版可加载的。
///
/// 事故背景（v0.4.12–v0.5.3）：把 `CardPoolModel.EnergyColorName` 改成了 "saria"，
/// 于是 `PreloadManager` 预加载 `ModelDb.AllCardPools.Select(p =&gt; p.EnergyIconPath)` 时会去读
/// `res://images/packed/energy_icons/energy_saria.png` —— 这个路径只存在于资源缓存（TakeOverPath），
/// 磁盘/PCK 里没有文件，预加载直接抛 `AssetLoadException`，房间资源加载中断，
/// 表现为**开局卡在涅奥对话界面、点不动**。
///
/// 现在：
///   * `EnergyColorName` 保持 "ironclad"（所有真实路径都存在，预加载不会失败）；
///   * 只把**文本前缀**（`GetPrefix`）换 "saria"，让 BBCode 走
///     `sprite_fonts/saria_energy_icon.png`（这条路径不参与预加载，只走 TakeOverPath 缓存）；
///   * `GetPath` 对 "saria" 兜底返回铁甲战士的真实路径，避免任何同步加载失败。
/// </summary>
public static class ProbeEnergyPrefix
{
    internal const string Prefix = "saria";
    private const string FallbackPrefix = "ironclad";

    internal static bool IsOurs(AbstractModel? model)
    {
        if (model == null)
            return false;
        if (model is CardPoolModel pool)
            return string.Equals(pool.Title, Prefix, StringComparison.OrdinalIgnoreCase);
        string entry = model.Id.Entry;
        return entry != null && entry.StartsWith("SARIA-", StringComparison.Ordinal);
    }
}

/// <summary>文本里的 `{Energy}` 用我们的前缀。</summary>
[HarmonyPatch(typeof(EnergyIconHelper), nameof(EnergyIconHelper.GetPrefix), typeof(AbstractModel))]
public static class ProbeEnergyPrefixPatch
{
    [HarmonyPostfix]
    private static void Postfix(AbstractModel model, ref string __result)
    {
        if (ProbeEnergyPrefix.IsOurs(model))
            __result = ProbeEnergyPrefix.Prefix;
    }
}

/// <summary>任何按路径取图标的地方都回到原版可加载的路径（防止预加载/同步加载抛异常）。</summary>
[HarmonyPatch(typeof(EnergyIconHelper), nameof(EnergyIconHelper.GetPath), typeof(AbstractModel))]
public static class ProbeEnergyGetPathPatch
{
    [HarmonyPostfix]
    private static void Postfix(AbstractModel model, ref string __result)
    {
        if (!ProbeEnergyPrefix.IsOurs(model))
            return;
        __result = EnergyIconHelper.GetPath("ironclad");
    }
}

/// <summary>字符串重载兜底：有人直接拿 "saria" 前缀取路径时也给真实路径。</summary>
[HarmonyPatch(typeof(EnergyIconHelper), nameof(EnergyIconHelper.GetPath), typeof(string))]
public static class ProbeEnergyGetPathStringPatch
{
    [HarmonyPostfix]
    private static void Postfix(string prefix, ref string __result)
    {
        if (!string.Equals(prefix, ProbeEnergyPrefix.Prefix, StringComparison.OrdinalIgnoreCase))
            return;
        __result = EnergyIconHelper.GetPath("ironclad");
        Log.Warn($"[saria][energy-icon] GetPath(\"{prefix}\") fell back to ironclad (virtual paths are not preloadable)");
    }
}
