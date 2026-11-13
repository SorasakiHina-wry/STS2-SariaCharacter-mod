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
/// 游戏 2026-11 更新后，`SelectCharacter` 的签名从
/// `(NCharacterSelectButton, CharacterModel)` 变成了 `(NCharacterSelectButton)`，
/// 角色改为从按钮上取，所以这里跟着改；同时保留按钮节点名作为兜底判定。
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
    private static void Postfix(NCharacterSelectScreen __instance, NCharacterSelectButton charSelectButton)
    {
        if (!IsSaria(charSelectButton))
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

    /// <summary>新版游戏把角色挂在按钮上；老版没有这个属性时退回到节点名（`PROBE-SARIA_button`）。</summary>
    private static bool IsSaria(NCharacterSelectButton? button)
    {
        if (button == null)
            return false;

        string? entry = button.Character?.Id.Entry;
        if (!string.IsNullOrEmpty(entry))
            return entry.EndsWith("SARIA", StringComparison.Ordinal);

        string nodeName = button.Name?.ToString() ?? string.Empty;
        return nodeName.Contains("SARIA", StringComparison.Ordinal);
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
