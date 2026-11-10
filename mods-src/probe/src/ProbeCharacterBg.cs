using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace Probe;

/// <summary>
/// 角色选择界面的背景替换。
///
/// 原版是 `PreloadManager.Cache.GetScene(CharacterSelectBg)` 实例化一个场景；而 mod 的 PCK 里放不了
/// 可直接加载的 .tscn（需要 .godot/exported 的 .scn + .tscn.remap）。所以这里不去改路径，
/// 而是在 SelectCharacter 之后把背景容器（AnimatedBg）里的内容换成运行时用 PNG 建出来的 TextureRect。
///
/// PNG 用绝对路径读取（mods/probe/images/…），避免 res:// 的 .import 重定向。
/// </summary>
[HarmonyPatch(typeof(NCharacterSelectScreen), nameof(NCharacterSelectScreen.SelectCharacter))]
public static class ProbeCharacterSelectBgPatch
{
    private const string BgFileName = "char_select_bg_saria.png";

    private static Texture2D? _cached;

    [HarmonyPostfix]
    private static void Postfix(NCharacterSelectScreen __instance, CharacterModel characterModel)
    {
        if (!characterModel.Id.Entry.EndsWith("SARIA", StringComparison.Ordinal))
            return;

        var container = __instance.GetNodeOrNull<Control>("AnimatedBg");
        if (container == null)
        {
            Log.Warn("[probe] AnimatedBg node not found");
            return;
        }

        var texture = LoadTexture();
        if (texture == null)
            return;

        foreach (var child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }

        var rect = new TextureRect
        {
            Name = "SARIA_bg",
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        rect.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        container.AddChild(rect);
        Log.Info("[probe] character select background replaced");
    }

    private static Texture2D? LoadTexture()
    {
        if (_cached != null && GodotObject.IsInstanceValid(_cached))
            return _cached;

        try
        {
            string path = OS.GetExecutablePath().GetBaseDir()
                .PathJoin("mods").PathJoin("probe").PathJoin("images").PathJoin(BgFileName);

            if (!System.IO.File.Exists(path))
            {
                Log.Warn($"[probe] background image missing: {path}");
                return null;
            }

            var image = new Image();
            if (image.LoadPngFromBuffer(System.IO.File.ReadAllBytes(path)) != Error.Ok)
            {
                Log.Warn("[probe] background png decode failed");
                return null;
            }

            _cached = ImageTexture.CreateFromImage(image);
            return _cached;
        }
        catch (Exception ex)
        {
            Log.Error($"[probe] background load threw: {ex.Message}");
            return null;
        }
    }
}
