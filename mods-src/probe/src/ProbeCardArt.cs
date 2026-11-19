using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Saria;

/// <summary>
/// 卡图加载器。
///
/// mod 不能往 PCK 里塞原始 PNG（运行时读不了自己的 .ctex），所以卡图放在
/// <c>mods/saria/images/portraits/&lt;类名&gt;.png</c>，由这里按需读成 ImageTexture 并缓存。
/// 游戏侧通过给 <see cref="CardModel.Portrait"/> 的 getter 打补丁来取我们的贴图。
/// </summary>
internal static class ProbeCardArt
{
    private static readonly Dictionary<string, Texture2D?> Cache = new();

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

/// <summary>让塞雷娅的卡牌显示 mod 自带的卡图，其他角色不受影响。</summary>
[HarmonyPatch(typeof(CardModel), "get_Portrait")]
public static class ProbeCardPortraitPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, ref Texture2D __result)
    {
        try
        {
            string entry = __instance.Id.Entry;
            if (entry == null || !entry.StartsWith("SARIA-", StringComparison.Ordinal))
                return;

            var texture = ProbeCardArt.Load(__instance.GetType().Name);
            if (texture != null)
                __result = texture;
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][art] portrait patch failed: {ex.Message}");
        }
    }
}
