using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Saria;

/// <summary>
/// 状态（power）小图加载器。
///
/// 和卡图同理：mod PCK 里的贴图加载不了，所以图标放散文件
/// <c>mods/saria/images/powers/&lt;类名&gt;.png</c>（64×64），运行时读成 ImageTexture 缓存。
/// 目前 64×64 一档同时用于状态栏小图标与悬停大图。
/// </summary>
internal static class ProbePowerArt
{
    private static readonly Dictionary<string, Texture2D?> Cache = new();

    internal static string IconDir =>
        OS.GetExecutablePath().GetBaseDir().PathJoin("mods").PathJoin("saria").PathJoin("images").PathJoin("powers");

    internal static Texture2D? Load(string className)
    {
        if (Cache.TryGetValue(className, out var cached))
            return cached;

        Texture2D? texture = null;
        try
        {
            string path = IconDir.PathJoin(className + ".png");
            if (FileAccess.FileExists(path))
            {
                byte[] bytes = FileAccess.GetFileAsBytes(path);
                var image = new Image();
                var err = image.LoadPngFromBuffer(bytes);
                if (err == Error.Ok && !image.IsEmpty())
                    texture = ImageTexture.CreateFromImage(image);
                else
                    Log.Warn($"[saria][power-art] decode failed ({err}): {path}");
            }
            else
            {
                Log.Warn($"[saria][power-art] missing icon: {path}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[saria][power-art] load threw for {className}: {ex.Message}");
        }

        Cache[className] = texture;
        return texture;
    }

    internal static Texture2D? ForModel(PowerModel? model)
    {
        if (model == null)
            return null;

        string entry = model.Id.Entry;
        if (entry == null || !entry.StartsWith("SARIA-", StringComparison.Ordinal))
            return null;

        return Load(model.GetType().Name);
    }
}

/// <summary>状态栏图标：NPower.Reload 里设完原版图标后换成我们的。</summary>
[HarmonyPatch(typeof(NPower), "Reload")]
public static class ProbePowerIconPatch
{
    private static readonly AccessTools.FieldRef<NPower, TextureRect> IconRef =
        AccessTools.FieldRefAccess<NPower, TextureRect>("_icon");

    private static readonly AccessTools.FieldRef<NPower, CpuParticles2D> FlashRef =
        AccessTools.FieldRefAccess<NPower, CpuParticles2D>("_powerFlash");

    [HarmonyPostfix]
    private static void Postfix(NPower __instance)
    {
        try
        {
            var texture = ProbePowerArt.ForModel(__instance.Model);
            if (texture == null)
                return;

            var icon = IconRef(__instance);
            if (icon != null)
                icon.Texture = texture;

            var flash = FlashRef(__instance);
            if (flash != null)
                flash.Texture = texture;
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][power-art] NPower hook failed: {ex.Message}");
        }
    }
}

/// <summary>悬停提示里的图标走 PowerModel.Icon，这里也补一刀（属性 getter 可能被内联，失败也不影响状态栏）。</summary>
[HarmonyPatch(typeof(PowerModel), "get_Icon")]
public static class ProbePowerHoverIconPatch
{
    [HarmonyPostfix]
    private static void Postfix(PowerModel __instance, ref Texture2D __result)
    {
        var texture = ProbePowerArt.ForModel(__instance);
        if (texture != null)
            __result = texture;
    }
}
