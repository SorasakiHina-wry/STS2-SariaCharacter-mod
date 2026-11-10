using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace Probe;

/// <summary>
/// 角色图标接线。
///
/// 原版图标都是按 `Id.Entry` 拼路径、再经 preload 加载的导入贴图（CompressedTexture2D）。
/// mod 的 PCK 里塞不进可用的 .ctex，所以这里两手抓：
///   * 「路径」类属性保持指向原版（否则 AssetPathsCharacterSelect 预加载失败，角色整个不出现在选择栏）；
///   * 需要显示时用运行时 ImageTexture 覆盖（TextureRect.Texture / Texture2D 属性都能接）。
///
/// 尺寸对照（原版实测）：选择栏头像 132×195、方头像 85×85。
/// </summary>
public static class ProbeCharacterIcons
{
    public const string PortraitFile = "char_select_saria.png";     // 264×390，比例 0.677
    public const string SquareFile = "character_icon_saria.png";    // 220×220

    private static Texture2D? _portrait;
    private static Texture2D? _square;
    private static Texture2D? _blank;

    public static bool IsSaria(ModelId id) => id.Entry.EndsWith("SARIA", StringComparison.Ordinal);

    public static Texture2D? Portrait => _portrait ??= LoadPng(PortraitFile);
    public static Texture2D? Square => _square ??= LoadPng(SquareFile);
    public static Texture2D Blank
    {
        get
        {
            if (_blank != null && GodotObject.IsInstanceValid(_blank))
                return _blank;
            var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
            image.Fill(new Color(0f, 0f, 0f, 0f));
            _blank = ImageTexture.CreateFromImage(image);
            return _blank;
        }
    }

    public static Texture2D? LoadPng(string fileName)
    {
        try
        {
            string path = OS.GetExecutablePath().GetBaseDir()
                .PathJoin("mods").PathJoin("probe").PathJoin("images").PathJoin(fileName);

            if (!System.IO.File.Exists(path))
            {
                Log.Warn($"[probe] icon file missing: {path}");
                return null;
            }

            var image = new Image();
            if (image.LoadPngFromBuffer(System.IO.File.ReadAllBytes(path)) != Error.Ok)
            {
                Log.Warn($"[probe] icon decode failed: {fileName}");
                return null;
            }

            return ImageTexture.CreateFromImage(image);
        }
        catch (Exception ex)
        {
            Log.Error($"[probe] icon load threw ({fileName}): {ex.Message}");
            return null;
        }
    }
}

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconTexture), MethodType.Getter)]
public static class ProbeIconTexturePatch
{
    [HarmonyPrefix]
    private static bool Prefix(CharacterModel __instance, ref Texture2D __result)
    {
        if (!ProbeCharacterIcons.IsSaria(__instance.Id))
            return true;
        var texture = ProbeCharacterIcons.Square;
        if (texture == null)
            return true;
        __result = texture;
        return false;
    }
}

[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.IconOutlineTexture), MethodType.Getter)]
public static class ProbeIconOutlineTexturePatch
{
    [HarmonyPrefix]
    private static bool Prefix(CharacterModel __instance, ref Texture2D __result)
    {
        if (!ProbeCharacterIcons.IsSaria(__instance.Id))
            return true;
        __result = ProbeCharacterIcons.Blank;
        return false;
    }
}

/// <summary>选择栏按钮：Init 之后把图标换成我们的立绘。</summary>
[HarmonyPatch(typeof(NCharacterSelectButton), nameof(NCharacterSelectButton.Init))]
public static class ProbeCharacterSelectButtonPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCharacterSelectButton __instance, CharacterModel character)
    {
        if (!ProbeCharacterIcons.IsSaria(character.Id))
            return;

        var texture = ProbeCharacterIcons.Portrait;
        if (texture == null)
            return;

        var icon = __instance.GetNodeOrNull<TextureRect>("%Icon");
        if (icon == null)
        {
            Log.Warn("[probe] character select button icon not found");
            return;
        }

        icon.Texture = texture;
        Log.Info("[probe] character select portrait applied");
    }
}
