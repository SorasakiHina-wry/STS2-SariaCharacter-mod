using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Saria;

/// <summary>
/// 塞雷娅的能量球：沿用原版场景骨架（NEnergyCounter 的旋转/压暗/数字都在脚本里），
/// 只在实例化之后把 5 层贴图换成自制图，并把前后粒子染成 #f5f98e。
/// </summary>
internal static class ProbeOrbSkin
{
    /// <summary>粒子染色（与球体配色一致的淡黄绿）。</summary>
    private static readonly Color ParticleTint = new("f5f98e");

    internal static string OrbDir =>
        OS.GetExecutablePath().GetBaseDir().PathJoin("mods").PathJoin("saria").PathJoin("images").PathJoin("orb");

    private static readonly Dictionary<string, Texture2D?> Cache = new();

    internal static Texture2D? Load(int index)
    {
        string key = index.ToString();
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        Texture2D? texture = null;
        try
        {
            string path = OrbDir.PathJoin($"Layer{index}.png");
            if (FileAccess.FileExists(path))
            {
                byte[] bytes = FileAccess.GetFileAsBytes(path);
                var image = new Image();
                var err = image.LoadPngFromBuffer(bytes);
                if (err == Error.Ok && !image.IsEmpty())
                    texture = ImageTexture.CreateFromImage(image);
                else
                    Log.Warn($"[saria][orb] decode failed ({err}): {path}");
            }
            else
            {
                Log.Warn($"[saria][orb] missing layer: {path}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[saria][orb] load threw for layer {index}: {ex.Message}");
        }

        Cache[key] = texture;
        return texture;
    }

    internal static void Apply(NEnergyCounter counter)
    {
        try
        {
            // 5 层贴图：Layer1 在 Layers 下，Layer2/3 在 Layers/RotationLayers 下，Layer4/5 在 Layers 下
            for (int i = 1; i <= 5; i++)
            {
                var node = counter.FindChild($"Layer{i}", recursive: true, owned: false) as TextureRect;
                if (node == null)
                {
                    Log.Warn($"[saria][orb] Layer{i} node not found");
                    continue;
                }

                var texture = Load(i);
                if (texture != null)
                    node.Texture = texture;
            }

            // 前后粒子统一染色
            foreach (var name in new[] { "EnergyVfxBack", "EnergyVfxFront" })
            {
                var container = counter.FindChild(name, recursive: true, owned: false) as CanvasItem;
                if (container != null)
                    Tint(container);
            }

            Log.Info("[saria][orb] custom energy orb applied");
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][orb] apply failed: {ex.Message}");
        }
    }

    private static void Tint(CanvasItem root)
    {
        root.Modulate = ParticleTint;
        foreach (var child in root.GetChildren())
        {
            if (child is CanvasItem item)
                Tint(item);
        }
    }
}

/// <summary>NEnergyCounter.Create 之后把铁甲战士的球换成我们的。</summary>
[HarmonyPatch(typeof(NEnergyCounter), "Create")]
public static class ProbeOrbPatch
{
    [HarmonyPostfix]
    private static void Postfix(Player player, NEnergyCounter __result)
    {
        if (__result == null || player?.Character == null)
            return;
        if (!player.Character.Id.Entry.EndsWith("SARIA", StringComparison.Ordinal))
            return;

        ProbeOrbSkin.Apply(__result);
    }
}
