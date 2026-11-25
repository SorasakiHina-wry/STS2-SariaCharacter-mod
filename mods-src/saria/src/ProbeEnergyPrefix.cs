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

    /// <summary>
    /// 判断某个模型是不是我们 mod 的。
    ///
    /// ⚠️ 不能用 `Id.Entry.StartsWith("SARIA-")`：`ModelId` 是 `(Category, Entry)` 两段，
    /// 我们卡牌的 Entry 里并没有 "SARIA-" 前缀（v0.7.4 之前就是因此一直匹配失败，
    /// 内联能量图标退回铁甲战士图标）。用**类型所在命名空间**判断最稳：所有自制模型都在 `Saria` 命名空间下。
    /// </summary>
    /// <summary>当前本地角色是不是塞雷娅（没有对局 / 其他角色时为 false）。</summary>
    internal static bool IsLocalSaria()
    {
        try
        {
            // 没有对局时返回 null；本地角色是塞雷娅时 GetPrefix 已经被本补丁改成 "saria"。
            string? prefix = MegaCrit.Sts2.Core.Runs.RunManager.Instance.GetLocalCharacterEnergyIconPrefix();
            return string.Equals(prefix, Prefix, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    internal static bool IsOurs(AbstractModel? model)
    {
        if (model == null)
            return false;

        if (model is CardPoolModel pool && string.Equals(pool.Title, Prefix, StringComparison.OrdinalIgnoreCase))
            return true;

        var type = model.GetType();
        if (type.Namespace != null && type.Namespace.StartsWith("Saria", StringComparison.Ordinal))
            return true;

        // 兜底：某些包装类型（例如 BaseLib 的代理）命名空间可能不是 Saria，看 Id 两段里有没有前缀。
        string category = model.Id.Category ?? string.Empty;
        string entry = model.Id.Entry ?? string.Empty;
        return category.Contains("SARIA", StringComparison.OrdinalIgnoreCase)
               || entry.Contains("SARIA", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>文本里的 `{Energy}` 用我们的前缀。</summary>
[HarmonyPatch(typeof(EnergyIconHelper), nameof(EnergyIconHelper.GetPrefix), typeof(AbstractModel))]
public static class ProbeEnergyPrefixPatch
{
    private static int _logged;

    [HarmonyPostfix]
    private static void Postfix(AbstractModel model, ref string __result)
    {
        if (!ProbeEnergyPrefix.IsOurs(model))
            return;

        __result = ProbeEnergyPrefix.Prefix;
        if (_logged < 5)
        {
            _logged++;
            Log.Info($"[saria][energy-icon] prefix->saria for {model.GetType().Name} ({model.Id})");
        }
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
