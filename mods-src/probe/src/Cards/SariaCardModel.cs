using System.Collections.Generic;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;

namespace Saria.Cards;

/// <summary>
/// 所有塞雷娅卡牌的基类：统一返回自制卡图的路径。
///
/// 注意 BaseLib 的 <c>CustomCardPortraitPath</c> 补丁会做
/// <c>ResourceLoader.Load&lt;Texture2D&gt;(CustomPortraitPath).ResourcePath</c>
/// —— 路径必须真的能被 ResourceLoader 加载，否则它会因空引用而让每张牌都显示成 bug 卡。
/// 所以这里先探测一次：能加载才返回我们自己的路径，否则退回原版卡图。
/// </summary>
public abstract class SariaCardModel : CustomCardModel
{
    protected SariaCardModel(int baseCost, CardType type, CardRarity rarity, TargetType target,
                             bool showInCardLibrary = true)
        : base(baseCost, type, rarity, target, showInCardLibrary)
    {
    }

    public override string? CustomPortraitPath => SariaPortraits.Resolve(GetType().Name);
}

internal static class SariaPortraits
{
    private static readonly Dictionary<string, string?> Cache = new();
    private static bool _reported;

    internal static string Resolve(string className)
    {
        if (Cache.TryGetValue(className, out var cached))
            return cached!;

        string pckPath = $"res://saria/images/portraits/{className}.png";
        string? result = null;

        try
        {
            bool exists = ResourceLoader.Exists(pckPath);
            Texture2D? tex = exists ? ResourceLoader.Load<Texture2D>(pckPath) : null;

            if (tex != null)
            {
                result = pckPath;
            }
            else if (!_reported)
            {
                _reported = true;
                Log.Warn($"[saria][art] pck portrait NOT loadable (exists={exists}, texture={tex != null}) — 回退原版卡图: {pckPath}");
            }
        }
        catch (System.Exception ex)
        {
            if (!_reported)
            {
                _reported = true;
                Log.Warn($"[saria][art] portrait probe threw: {ex.Message}");
            }
        }

        // 退回原版铁甲战士卡图：它一定能加载，这样 BaseLib 不会拿到空路径
        result ??= ProbeCardKit.StrikePortrait;
        Cache[className] = result;
        return result;
    }
}
