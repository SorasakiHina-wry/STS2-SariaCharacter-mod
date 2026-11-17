using System;
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
    private static GodotObject? _skeletonData;

    public static string SpineDir =>
        OS.GetExecutablePath().GetBaseDir().PathJoin("mods").PathJoin("saria").PathJoin("spine");

    /// <summary>加载（并缓存）SpineSkeletonDataResource；失败返回 null。</summary>
    public static GodotObject? SkeletonData()
    {
        if (_skeletonData != null && GodotObject.IsInstanceValid(_skeletonData))
            return _skeletonData;

        try
        {
            string dir = SpineDir;
            string skelPath = dir.PathJoin("char_202_demkni.skel");
            string atlasPath = dir.PathJoin("char_202_demkni.atlas");

            var atlas = ClassDB.Instantiate("SpineAtlasResource").AsGodotObject();
            var skelFile = ClassDB.Instantiate("SpineSkeletonFileResource").AsGodotObject();
            var data = ClassDB.Instantiate("SpineSkeletonDataResource").AsGodotObject();
            if (atlas == null || skelFile == null || data == null)
            {
                Log.Error("[saria][spine] instantiate failed");
                return null;
            }

            atlas.Call("load_from_atlas_file", atlasPath);
            skelFile.Call("load_from_file", skelPath);

            data.Call("set_atlas_res", atlas);
            data.Call("set_skeleton_file_res", skelFile);
            data.Call("update_skeleton_data");

            if (!data.Call("is_skeleton_data_loaded").AsBool())
            {
                Log.Error("[saria][spine] skeleton data did not load");
                return null;
            }

            Log.Info($"[saria][spine] skeleton ready version={data.Call("get_version").AsString()}");
            _skeletonData = data;
            return data;
        }
        catch (Exception ex)
        {
            Log.Error($"[saria][spine] load threw: {ex}");
            return null;
        }
    }
}
