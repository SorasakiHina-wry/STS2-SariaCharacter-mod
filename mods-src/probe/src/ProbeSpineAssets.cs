using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace Saria;

/// <summary>
/// 在运行时加载 mods/saria/spine 下的 Spine 资源（spine-godot 4.2.43）。
///
/// 注意：不能用 ResourceLoader.Load(".skel")——.skel/.atlas 的导入器只在 Godot 编辑器里存在，
/// 运行时必须走 SpineAtlasResource / SpineSkeletonFileResource / SpineSkeletonDataResource 这套 API。
/// </summary>
public static class ProbeSpineAssets
{
    /// <summary>战斗骨骼（含 idle_loop / attack / cast / powerup 等别名）。</summary>
    public const string FightSkeleton = "char_202_demkni";

    /// <summary>非战斗骨骼（Default / Interact / Move / Relax / Sit / Sleep），商店与火堆用。</summary>
    public const string BuildSkeleton = "build_char_202_demkni";

    private static readonly Dictionary<string, GodotObject?> Cache = new();

    public static string SpineDir =>
        OS.GetExecutablePath().GetBaseDir().PathJoin("mods").PathJoin("saria").PathJoin("spine");

    /// <summary>战斗立绘用的骨骼。</summary>
    public static GodotObject? SkeletonData() => Load(FightSkeleton);

    /// <summary>加载（并缓存）指定基名的骨骼；失败返回 null。</summary>
    public static GodotObject? Load(string baseName)
    {
        if (Cache.TryGetValue(baseName, out var cached) && cached != null && GodotObject.IsInstanceValid(cached))
            return cached;

        try
        {
            string dir = SpineDir;
            string skelPath = dir.PathJoin(baseName + ".skel");
            string atlasPath = dir.PathJoin(baseName + ".atlas");

            var atlas = ClassDB.Instantiate("SpineAtlasResource").AsGodotObject();
            var skelFile = ClassDB.Instantiate("SpineSkeletonFileResource").AsGodotObject();
            var data = ClassDB.Instantiate("SpineSkeletonDataResource").AsGodotObject();
            if (atlas == null || skelFile == null || data == null)
            {
                Log.Error($"[saria][spine] instantiate failed for {baseName}");
                return null;
            }

            atlas.Call("load_from_atlas_file", atlasPath);
            skelFile.Call("load_from_file", skelPath);

            data.Call("set_atlas_res", atlas);
            data.Call("set_skeleton_file_res", skelFile);
            data.Call("update_skeleton_data");

            if (!data.Call("is_skeleton_data_loaded").AsBool())
            {
                Log.Error($"[saria][spine] skeleton data did not load: {baseName}");
                return null;
            }

            Log.Info($"[saria][spine] skeleton ready {baseName} version={data.Call("get_version").AsString()}");
            Cache[baseName] = data;
            return data;
        }
        catch (Exception ex)
        {
            Log.Error($"[saria][spine] load threw for {baseName}: {ex}");
            return null;
        }
    }

    /// <summary>
    /// 把一个 SpineSprite 换成我们的骨骼并播放指定动画。
    /// 动画名不存在时退回 "Default"（占位），并记录日志。
    /// </summary>
    public static bool Apply(Node spineNode, string baseName, string animation, bool loop)
    {
        try
        {
            var data = Load(baseName);
            if (data == null)
                return false;

            spineNode.Call("set_skeleton_data_res", data);

            var state = spineNode.Call("get_animation_state").AsGodotObject();
            if (state == null)
                return false;

            string chosen = animation;
            if (!HasAnimation(spineNode, animation))
            {
                Log.Warn($"[saria][spine] animation '{animation}' missing in {baseName}, falling back to Default");
                chosen = "Default";
            }

            state.Call("set_animation", chosen, loop);
            Log.Info($"[saria][spine] {baseName}: playing {chosen} (loop={loop})");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error($"[saria][spine] Apply failed: {ex}");
            return false;
        }
    }

    private static bool HasAnimation(Node spineNode, string animation)
    {
        try
        {
            object? skeleton = spineNode.Call("get_skeleton").AsGodotObject();
            var data = skeleton?.GetType().GetProperty("Data")?.GetValue(skeleton);
            var animations = data?.GetType().GetProperty("Animations")?.GetValue(data) as System.Collections.IEnumerable;
            if (animations == null)
                return true;   // 查不到就不拦，照常播

            foreach (var item in animations)
            {
                var nameProp = item?.GetType().GetProperty("Name") ?? item?.GetType().GetProperty("name");
                if (nameProp?.GetValue(item)?.ToString() == animation)
                    return true;
            }

            return false;
        }
        catch
        {
            return true;
        }
    }
}
