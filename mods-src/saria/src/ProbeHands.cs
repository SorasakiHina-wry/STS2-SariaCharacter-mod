using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Saria;

/// <summary>
/// 多人猜拳的手部贴图（石头 / 布 / 剪刀 / 指向）。
///
/// 原版 `CharacterModel.Arm*Texture` 返回 `PreloadManager` 加载的导入贴图（路径按角色 entry 拼），
/// mod 的 PCK 贴图加载不了，所以直接在这些 getter 上返回运行时解码的 `Texture2D`。
/// 素材：`mods/saria/images/hand/multiplayer_hand_saria_&lt;手部&gt;.png`（424×1200，与原版同规格）。
/// 消费方：`NHandImage`（猜拳界面、选人时的指向手部）。
/// </summary>
internal static class ProbeHands
{
    internal static string HandDir =>
        OS.GetExecutablePath().GetBaseDir().PathJoin("mods").PathJoin("saria").PathJoin("images").PathJoin("hand");

    private static readonly Dictionary<string, Texture2D?> Cache = new();

    internal static Texture2D? Load(string gesture)
    {
        if (Cache.TryGetValue(gesture, out var cached))
            return cached;

        Texture2D? texture = null;
        try
        {
            string path = HandDir.PathJoin($"multiplayer_hand_saria_{gesture}.png");
            if (FileAccess.FileExists(path))
            {
                var image = new Image();
                var err = image.LoadPngFromBuffer(FileAccess.GetFileAsBytes(path));
                if (err == Error.Ok && !image.IsEmpty())
                    texture = ImageTexture.CreateFromImage(image);
                else
                    Log.Warn($"[saria][hand] decode failed ({err}): {path}");
            }
            else
            {
                Log.Warn($"[saria][hand] missing file: {path}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[saria][hand] load threw for {gesture}: {ex.Message}");
        }

        Cache[gesture] = texture;
        return texture;
    }

    internal static bool IsOurs(CharacterModel? character) =>
        character?.Id.Entry is string entry && entry.EndsWith("SARIA", StringComparison.Ordinal);
}

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmPointingTexture), MethodType.Getter)]
public static class ProbeHandPointPatch
{
    [HarmonyPrefix]
    private static bool Prefix(CharacterModel __instance, ref Texture2D __result)
    {
        if (!ProbeHands.IsOurs(__instance))
            return true;
        var texture = ProbeHands.Load("point");
        if (texture == null)
            return true;
        __result = texture;
        return false;
    }
}

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmRockTexture), MethodType.Getter)]
public static class ProbeHandRockPatch
{
    [HarmonyPrefix]
    private static bool Prefix(CharacterModel __instance, ref Texture2D __result)
    {
        if (!ProbeHands.IsOurs(__instance))
            return true;
        var texture = ProbeHands.Load("rock");
        if (texture == null)
            return true;
        __result = texture;
        return false;
    }
}

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmPaperTexture), MethodType.Getter)]
public static class ProbeHandPaperPatch
{
    [HarmonyPrefix]
    private static bool Prefix(CharacterModel __instance, ref Texture2D __result)
    {
        if (!ProbeHands.IsOurs(__instance))
            return true;
        var texture = ProbeHands.Load("paper");
        if (texture == null)
            return true;
        __result = texture;
        return false;
    }
}

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.ArmScissorsTexture), MethodType.Getter)]
public static class ProbeHandScissorsPatch
{
    [HarmonyPrefix]
    private static bool Prefix(CharacterModel __instance, ref Texture2D __result)
    {
        if (!ProbeHands.IsOurs(__instance))
            return true;
        var texture = ProbeHands.Load("scissors");
        if (texture == null)
            return true;
        __result = texture;
        return false;
    }
}
