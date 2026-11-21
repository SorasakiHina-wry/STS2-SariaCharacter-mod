using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace Saria;

/// <summary>
/// 地图上的角色标记。
///
/// 原版 `CharacterModel.MapMarker` 返回 `CompressedTexture2D`（preload 的 .ctex），mod 造不出来；
/// 但它的唯一出口是 `TextureRect.Texture`（`NMapMarker.Initialize`），所以在这里换成运行时贴图。
/// 素材 `map_marker_saria.png`（98×128）由 head.png 居中裁剪 + 缩放生成。
/// </summary>
[HarmonyPatch(typeof(NMapMarker), nameof(NMapMarker.Initialize))]
public static class ProbeMapMarkerPatch
{
    [HarmonyPostfix]
    private static void Postfix(NMapMarker __instance, Player player)
    {
        try
        {
            if (player?.Character == null)
                return;
            if (!player.Character.Id.Entry.EndsWith("SARIA", StringComparison.Ordinal))
                return;

            var texture = ProbeCharacterIcons.MapMarker;
            if (texture == null)
                return;

            __instance.Texture = texture;
            Log.Info("[saria] map marker applied");
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria] map marker failed: {ex.Message}");
        }
    }
}
