using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace Probe;

/// <summary>
/// 临时探针：按 spine-godot 的真实 API 加载转换后的 4.2.43 骨骼与图集。
/// </summary>
public static class ProbeSpineProbe
{
    public static void Run()
    {
        try
        {
            string dir = OS.GetExecutablePath().GetBaseDir().PathJoin("mods").PathJoin("probe").PathJoin("spine");
            string skelPath = dir.PathJoin("build_char_202_demkni.skel");
            string atlasPath = dir.PathJoin("build_char_202_demkni.atlas");

            var atlas = ClassDB.Instantiate("SpineAtlasResource").AsGodotObject();
            var skelFile = ClassDB.Instantiate("SpineSkeletonFileResource").AsGodotObject();
            var data = ClassDB.Instantiate("SpineSkeletonDataResource").AsGodotObject();
            if (atlas == null || skelFile == null || data == null)
            {
                Log.Error("[probe][spine] instantiate failed");
                return;
            }

            atlas.Call("load_from_atlas_file", atlasPath);
            var textures = atlas.Call("get_textures").AsGodotArray();
            Log.Info($"[probe][spine] atlas textures = {textures.Count}");

            skelFile.Call("load_from_file", skelPath);

            data.Call("set_atlas_res", atlas);
            data.Call("set_skeleton_file_res", skelFile);
            data.Call("update_skeleton_data");

            bool loaded = data.Call("is_skeleton_data_loaded").AsBool();
            string version = data.Call("get_version").AsString();
            Log.Info($"[probe][spine] loaded={loaded} version={version}");

            var anims = data.Call("get_animations").AsGodotArray();
            var names = new List<string>();
            foreach (var item in anims)
            {
                var anim = item.AsGodotObject();
                if (anim != null)
                    names.Add(anim.Call("get_name").AsString());
            }
            Log.Info($"[probe][spine] animations({names.Count}): {string.Join(", ", names)}");
        }
        catch (Exception ex)
        {
            Log.Error($"[probe][spine] probe threw: {ex}");
        }
    }
}