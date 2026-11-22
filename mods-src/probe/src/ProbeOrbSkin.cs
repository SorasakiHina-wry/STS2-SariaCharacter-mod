using System;
using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Saria;

/// <summary>
/// 塞雷娅的能量球与能量图标。
///
/// 能量球：沿用原版 NEnergyCounter 场景骨架（旋转/压暗/数字都在脚本里），
/// 实例化之后把 5 层贴图换成自制图（`saria_orb_layer_1..5.png`），并把前后粒子染成 #f5f98e。
/// 能量图标：卡面费用位置的 `NCard._energyIcon` 换成 `energy_saria.png`。
/// </summary>
internal static class ProbeOrbSkin
{
    /// <summary>粒子染色（与球体配色一致的淡黄绿）。</summary>
    private static readonly Color ParticleTint = new("f5f98e");

    internal static string OrbDir =>
        OS.GetExecutablePath().GetBaseDir().PathJoin("mods").PathJoin("saria").PathJoin("images").PathJoin("orb");

    private static readonly Dictionary<string, Texture2D?> Cache = new();

    /// <summary>读取 images/orb 下的贴图（按文件名，缓存）。</summary>
    internal static Texture2D? Load(string fileName)
    {
        if (Cache.TryGetValue(fileName, out var cached))
            return cached;

        Texture2D? texture = null;
        try
        {
            string path = OrbDir.PathJoin(fileName);
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
                Log.Warn($"[saria][orb] missing file: {path}");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[saria][orb] load threw for {fileName}: {ex.Message}");
        }

        Cache[fileName] = texture;
        return texture;
    }

    internal static void ApplyOrb(NEnergyCounter counter)
    {
        try
        {
            for (int i = 1; i <= 5; i++)
            {
                var node = counter.FindChild($"Layer{i}", recursive: true, owned: false) as TextureRect;
                if (node == null)
                {
                    Log.Warn($"[saria][orb] Layer{i} node not found");
                    continue;
                }

                var texture = Load($"saria_orb_layer_{i}.png");
                if (texture != null)
                    node.Texture = texture;
            }

            foreach (var name in new[] { "EnergyVfxBack", "EnergyVfxFront" })
            {
                if (counter.FindChild(name, recursive: true, owned: false) is CanvasItem container)
                    Tint(container);
            }

            ApplyVfxTextures(counter);

            Log.Info("[saria][orb] custom energy orb applied");
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][orb] apply failed: {ex.Message}");
        }
    }

    /// <summary>把卡面费用图标换成自制能量图标。</summary>
    internal static void ApplyCardEnergyIcon(NCard card)
    {
        try
        {
            var model = card.Model;
            if (model?.Id.Entry is not string entry || !entry.StartsWith("SARIA-", StringComparison.Ordinal))
                return;

            var texture = Load("energy_saria.png");
            if (texture == null)
                return;

            foreach (var fieldName in new[] { "_energyIcon", "_unplayableEnergyIcon" })
            {
                var field = AccessTools.Field(typeof(NCard), fieldName);
                if (field?.GetValue(card) is TextureRect rect)
                    rect.Texture = texture;
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[saria][orb] card energy icon failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 「获得能量」特效用的 4 张粒子贴图（原版是火焰）。
    /// 文件名对应 `resources/vanilla/energy_counter_ironclad/images/vfx/fire_impact/` 里的原版贴图；
    /// 缺失时保留原版火焰，不影响运行。
    /// </summary>
    private static readonly (string Node, string File)[] VfxTextures =
    {
        ("vfx_fire_flipbook_1", "fire_burst_flipbook_1.png"),
        ("vfx_fire_flipbook_2", "fire_burst_flipbook_2.png"),
        ("vfx_fire_flipbook_3", "fire_burst_flipbook_3.png"),
        ("vfx_fire_embers_straight", "fire_burst_ember_straight.png"),
    };

    private static void ApplyVfxTextures(NEnergyCounter counter)
    {
        int replaced = 0;
        foreach (var (nodeName, fileName) in VfxTextures)
        {
            if (counter.FindChild(nodeName, recursive: true, owned: false) is not GpuParticles2D particles)
                continue;

            var texture = Load(fileName);
            if (texture == null)
                continue;

            particles.Texture = texture;
            replaced++;
        }

        if (replaced > 0)
            Log.Info($"[saria][orb] custom energy vfx textures: {replaced}");
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

        ProbeOrbSkin.ApplyOrb(__result);
    }
}

/// <summary>卡面刷新完之后换掉费用位置的图标。</summary>
[HarmonyPatch(typeof(NCard), "Reload")]
public static class ProbeCardEnergyIconPatch
{
    [HarmonyPostfix]
    private static void Postfix(NCard __instance) => ProbeOrbSkin.ApplyCardEnergyIcon(__instance);
}
