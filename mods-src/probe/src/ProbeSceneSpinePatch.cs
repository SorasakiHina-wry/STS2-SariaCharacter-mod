using System;
using System.Collections;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace Saria;

/// <summary>
/// 商店与火堆的角色骨骼：原版场景是铁甲战士的，这里在运行时把 SpineSprite 换成
/// mod 自带的非战斗骨骼（build_char_202_demkni），并播放我们选的动画：
///
///   商店 → Relax（原版商人循环叫 relaxed_loop，我们没有那个名字，直接播 Relax）
///   火堆 → Sit（原版按幕数要 overgrowth_loop / hive_loop / glory_loop，同样没有）
///
/// 动画名不存在时 ProbeSpineAssets.Apply 会退回 Default。
/// </summary>
internal static class SceneSpineHelper
{
    internal static bool IsSaria(Player? player) =>
        player?.Character != null &&
        player.Character.Id.Entry.EndsWith("SARIA", StringComparison.Ordinal);

    internal static void ApplyToChildren(
        Node root, string animation, bool loop, float scaleFactor = 1f, float? absoluteScale = null)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is not Node2D spine || child.GetClass() != "SpineSprite")
                continue;

            ProbeSpineAssets.Apply(spine, ProbeSpineAssets.BuildSkeleton, animation, loop);

            if (absoluteScale.HasValue)
            {
                spine.Scale = new Vector2(absoluteScale.Value, absoluteScale.Value);
                Log.Info($"[saria][spine] scale set to {spine.Scale}");
            }
            else if (scaleFactor != 1f)
            {
                spine.Scale *= scaleFactor;
                Log.Info($"[saria][spine] scale x{scaleFactor} -> {spine.Scale}");
            }
        }
    }
}

/// <summary>火堆：NRestSiteCharacter 自己带 Player，_Ready 里设完原版动画后我们覆盖一次。</summary>
[HarmonyPatch(typeof(NRestSiteCharacter), nameof(NRestSiteCharacter._Ready))]
public static class SariaRestSiteSpinePatch
{
    [HarmonyPostfix]
    private static void Postfix(NRestSiteCharacter __instance)
    {
        try
        {
            if (!SceneSpineHelper.IsSaria(__instance.Player))
                return;

            Log.Info("[saria][spine] rest site: swapping to build skeleton");
            SceneSpineHelper.ApplyToChildren(__instance, "Sit", loop: true, scaleFactor: 1.5f);
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][spine] rest site swap failed: {ex.Message}");
        }
    }
}

/// <summary>
/// 商店：NMerchantCharacter 不知道自己的主人，所以改在 NMerchantRoom 生成完角色之后，
/// 用 _players 与 _playerVisuals 的对应关系找出属于塞雷娅的那一个。
/// </summary>
[HarmonyPatch(typeof(NMerchantRoom), "AfterRoomIsLoaded")]
public static class SariaMerchantSpinePatch
{
    [HarmonyPostfix]
    private static void Postfix(NMerchantRoom __instance)
    {
        try
        {
            var playersField = AccessTools.Field(typeof(NMerchantRoom), "_players");
            var visualsField = AccessTools.Field(typeof(NMerchantRoom), "_playerVisuals");
            if (playersField == null || visualsField == null)
            {
                Log.Warn("[saria][spine] merchant room fields not found; shop skeleton unchanged");
                return;
            }

            if (playersField.GetValue(__instance) is not IList players ||
                visualsField.GetValue(__instance) is not IList visuals)
                return;

            for (int i = 0; i < Math.Min(players.Count, visuals.Count); i++)
            {
                if (players[i] is not Player player || !SceneSpineHelper.IsSaria(player))
                    continue;
                if (visuals[i] is not Node node)
                    continue;

                Log.Info("[saria][spine] merchant: swapping to build skeleton");
                SceneSpineHelper.ApplyToChildren(node, "Relax", loop: true, absoluteScale: 1.5f);
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][spine] merchant swap failed: {ex.Message}");
        }
    }
}
