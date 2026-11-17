using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Saria;

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
                Log.Error("[saria][spine] SpineSprite instantiate failed");
                return null;
            }

            spine.Name = "Visuals";
            spine.Scale = new Vector2(SpriteScale, SpriteScale);
            // 角色保持原点；把 Bounds 矩形居中到原点，这样血条/状态栏/VFX 都以角色中心为准。
            // （VFX 用 %CenterPos，血条用 %Bounds，两者不一致时就会出现"特效在左边、角色在右边"）
            spine.Position = Vector2.Zero;
            root.AddChild(spine);
            spine.Owner = root;
            spine.UniqueNameInOwner = true;

            spine.Call("set_skeleton_data_res", data);

            var state = spine.Call("get_animation_state").AsGodotObject();
            state?.Call("set_animation", "idle_loop", true);

            // 原版视觉场景里有一个 %FormVfx 容器；缺了它，死亡时 RemoveFormVfx 会抛异常，
            // 导致死亡动画中断、后续事件不触发。
            var formVfx = new Control { Name = "FormVfx", Size = BoundsSize, MouseFilter = Control.MouseFilterEnum.Ignore };
            root.AddChild(formVfx);
            formVfx.Owner = root;
            formVfx.UniqueNameInOwner = true;
            // 原版约定（见 ironclad.tscn）：Bounds 的左上角在角色头顶，向右下覆盖整个身体，
// 即 Position = (-W/2, -H)、Size = (W, H)。引擎用它的「左上角 + 尺寸」生成鼠标判定框，
// 若从 (0,0) 开始就会变成判定框落在脚下。
            var bounds = new Control
            {
                Name = "Bounds",
                Size = BoundsSize,
                Position = new Vector2(-BoundsSize.X * 0.5f, -BoundsSize.Y),
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            root.AddChild(bounds);
            bounds.Owner = root;
            bounds.UniqueNameInOwner = true;

            var intent = new Marker2D { Name = "IntentPos", Position = new Vector2(0f, -BoundsSize.Y * 1.1f) };
            root.AddChild(intent);
            intent.Owner = root;
            intent.UniqueNameInOwner = true;

            var center = new Marker2D { Name = "CenterPos", Position = new Vector2(0f, -BoundsSize.Y * 0.6f) };
            root.AddChild(center);
            center.Owner = root;
            center.UniqueNameInOwner = true;

            Log.Info("[saria][spine] creature visuals built");
            return root;
        }
        catch (Exception ex)
        {
            Log.Error($"[saria][spine] visuals build threw: {ex}");
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
        Log.Info($"[saria][spine] CreateVisuals entry={__instance.Id.Entry}");

        if (!__instance.Id.Entry.EndsWith("SARIA", StringComparison.Ordinal))
            return true;

        var visuals = ProbeSpineVisuals.Create();
        if (visuals == null)
            return true; // 加载失败就退回占位视觉，至少还能玩

        __result = visuals;
        return false;
    }
}
