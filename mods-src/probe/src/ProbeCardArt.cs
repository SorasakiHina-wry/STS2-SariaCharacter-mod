using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
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
}

/// <summary>
/// 卡面刷新完之后，把塞雷娅卡牌的贴图换成散文件里的自制卡图。
/// 挂 <c>NCard.UpdatePortrait</c>（方法，不会被内联），原版的字号/模糊参数保持不动。
/// </summary>
[HarmonyPatch(typeof(NCard), "UpdatePortrait")]
public static class ProbeCardPortraitPatch
{
    private static readonly AccessTools.FieldRef<NCard, TextureRect> PortraitRef =
        AccessTools.FieldRefAccess<NCard, TextureRect>("_portrait");

    private static readonly AccessTools.FieldRef<NCard, TextureRect> AncientPortraitRef =
        AccessTools.FieldRefAccess<NCard, TextureRect>("_ancientPortrait");

    [HarmonyPostfix]
    private static void Postfix(NCard __instance)
    {
        try
        {
            var model = __instance.Model;
            if (model == null)
                return;

            string entry = model.Id.Entry;
            if (entry == null || !entry.StartsWith("SARIA-", StringComparison.Ordinal))
                return;

            var texture = ProbeCardArt.Load(model.GetType().Name);
            if (texture == null)
                return;

            var portrait = PortraitRef(__instance);
            if (portrait != null)
                portrait.Texture = texture;

            var ancient = AncientPortraitRef(__instance);
            if (ancient != null && ancient.Visible)
                ancient.Texture = texture;
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][art] NCard portrait hook failed: {ex.Message}");
        }
    }
}
