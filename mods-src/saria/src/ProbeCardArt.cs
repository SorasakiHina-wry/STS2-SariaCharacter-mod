using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace Saria;

/// <summary>
/// 卡图加载器。
///
/// mod PCK 里的贴图加载不了（`ResourceLoader.Exists` 为 true 但 `Load` 返回 null，
/// 因为 import remap 指向游戏自己的 `res://.godot/imported/`，模组无法覆盖），
/// 所以卡图仍然放在散文件里：<c>mods/saria/images/portraits/&lt;类名&gt;.png</c>，
/// 由这里读成 ImageTexture 缓存，再在 NCard 刷新卡面之后替换贴图。
/// </summary>
internal static class ProbeCardArt
{
    private static readonly System.Collections.Generic.Dictionary<string, Texture2D?> Cache = new();

    internal static string PortraitDir =>
        OS.GetExecutablePath().GetBaseDir().PathJoin("mods").PathJoin("saria").PathJoin("images").PathJoin("portraits");

    internal static Texture2D? Load(string className)
    {
        if (Cache.TryGetValue(className, out var cached))
            return cached;

        Texture2D? texture = null;
        try
        {
            string path = PortraitDir.PathJoin(className + ".png");
            if (FileAccess.FileExists(path))
            {
                byte[] bytes = FileAccess.GetFileAsBytes(path);
                var image = new Image();
                var err = image.LoadPngFromBuffer(bytes);
                if (err == Error.Ok && !image.IsEmpty())
                    texture = ImageTexture.CreateFromImage(image);
                else
                    Log.Warn($"[saria][art] decode failed ({err}): {path}");
            }
            else
            {
                Log.Warn($"[saria][art] missing portrait: {path}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[saria][art] load threw for {className}: {ex.Message}");
        }

        Cache[className] = texture;
        return texture;
    }

    /// <summary>把塞雷娅卡牌的贴图换掉；不是我们的卡或没素材就原样返回。</summary>
    internal static bool TryApply(NCard card)
    {
        try
        {
            var model = card.Model;
            if (model == null)
                return false;

            string entry = model.Id.Entry;
            if (entry == null || !entry.StartsWith("SARIA-", StringComparison.Ordinal))
                return false;

            var texture = Load(model.GetType().Name);
            return texture != null && CardPortraitFields.Set(card, texture, model.Rarity);
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][art] apply failed: {ex.Message}");
            return false;
        }
    }
}

internal static class CardPortraitFields
{
    private static readonly AccessTools.FieldRef<NCard, TextureRect> PortraitRef =
        AccessTools.FieldRefAccess<NCard, TextureRect>("_portrait");

    private static readonly AccessTools.FieldRef<NCard, TextureRect> AncientPortraitRef =
        AccessTools.FieldRefAccess<NCard, TextureRect>("_ancientPortrait");

    internal static bool Set(NCard card, Texture2D texture, CardRarity rarity)
    {
        if (rarity != CardRarity.Ancient)
        {
            var portrait = PortraitRef(card);
            if (portrait == null)
                return false;
            portrait.Texture = texture;
            return true;
        }

        var ancient = AncientPortraitRef(card);
        if (ancient == null)
            return false;
        ancient.Texture = texture;
        return true;
    }
}

/// <summary>卡面刷新完之后替换贴图（手牌、牌库、卡牌奖励）。</summary>
[HarmonyPatch(typeof(NCard), "UpdatePortrait")]
public static class ProbeCardPortraitPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCard __instance) => ProbeCardArt.TryApply(__instance);
}

/// <summary>
/// 检视大图：NInspectCardScreen 会在 UpdateVisuals 之后再调一次
/// <c>SetPortraitTexture(card.BigPortrait)</c>，把我们的贴图覆盖回去，所以这里再补一刀。
/// </summary>
[HarmonyPatch(typeof(NCard), nameof(NCard.SetPortraitTexture))]
public static class ProbeCardBigPortraitPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCard __instance) => ProbeCardArt.TryApply(__instance);
}
