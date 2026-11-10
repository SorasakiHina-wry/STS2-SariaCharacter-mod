using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Probe;

/// <summary>
/// 用程序化方式搭一个 NCreatureVisuals：SpineSprite 作为 %Visuals，外加 %Bounds / %IntentPos / %CenterPos。
/// 骨架缺 idle_loop/attack/cast/hurt/die 时，已通过复制动画补名（见 docs/spine-conversion.md）。
/// </summary>
public static class ProbeSpineVisuals
{
    private const float SpriteScale = 0.75f;
    private static readonly Vector2 BoundsSize = new(220f, 300f);

    public static NCreatureVisuals? Create()
    {
        var data = ProbeSpineAssets.SkeletonData();
        if (data == null)
            return null;

        try
        {
            var root = new NCreatureVisuals { Name = "SariaVisuals" };

            var spine = ClassDB.Instantiate("SpineSprite").AsGodotObject() as Node2D;
            if (spine == null)
            {
                Log.Error("[probe][spine] SpineSprite instantiate failed");
                return null;
            }

            spine.Name = "Visuals";
            spine.Scale = new Vector2(SpriteScale, SpriteScale);
            // Bounds 的矩形以 (0,0) 为左上角，血条按它的中心对齐；把角色右移半个宽度，让身体落在血条正上方
            spine.Position = new Vector2(BoundsSize.X * 0.5f + 20f, 0f);
            root.AddChild(spine);
            spine.Owner = root;
            spine.UniqueNameInOwner = true;

            spine.Call("set_skeleton_data_res", data);

            var state = spine.Call("get_animation_state").AsGodotObject();
            state?.Call("set_animation", "idle_loop", true);

            var bounds = new Control { Name = "Bounds", Size = BoundsSize };
            root.AddChild(bounds);
            bounds.Owner = root;
            bounds.UniqueNameInOwner = true;

            var intent = new Marker2D { Name = "IntentPos", Position = new Vector2(0f, -BoundsSize.Y * 0.6f) };
            root.AddChild(intent);
            intent.Owner = root;
            intent.UniqueNameInOwner = true;

            var center = new Marker2D { Name = "CenterPos", Position = new Vector2(0f, -BoundsSize.Y * 0.45f) };
            root.AddChild(center);
            center.Owner = root;
            center.UniqueNameInOwner = true;

            Log.Info("[probe][spine] creature visuals built");
            return root;
        }
        catch (Exception ex)
        {
            Log.Error($"[probe][spine] visuals build threw: {ex}");
            return null;
        }
    }
}

/// <summary>把 Saria 的战斗视觉换成上面的 Spine 节点；其它角色仍然走原逻辑。</summary>
[HarmonyPatch(typeof(CharacterModel), nameof(CharacterModel.CreateVisuals))]
public static class ProbeVisualsPatch
{
    [HarmonyPrefix]
    private static bool Prefix(CharacterModel __instance, ref NCreatureVisuals __result)
    {
        Log.Info($"[probe][spine] CreateVisuals entry={__instance.Id.Entry}");

        if (!__instance.Id.Entry.StartsWith("PROBE", StringComparison.Ordinal))
            return true;

        var visuals = ProbeSpineVisuals.Create();
        if (visuals == null)
            return true; // 加载失败就退回占位视觉，至少还能玩

        __result = visuals;
        return false;
    }
}
