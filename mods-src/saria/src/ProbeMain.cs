using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace Saria;

/// <summary>
/// Entry point. BaseLib discovers the custom models by scanning this assembly, so nothing has to be
/// registered manually here; this applies the mod's Harmony patches and logs a load marker.
/// </summary>
[ModInitializer("Init")]
public static class ProbeMain
{
    public const string ModId = "saria";

    private static Harmony? _harmony;

    public static void Init()
    {
        Log.Info("[saria] initializing character mod");

        try
        {
            _harmony = new Harmony("com.sts2modding.saria");
            // Patch class-by-class so one stale target can't take down the rest.
            int applied = 0, skipped = 0;
            foreach (var type in typeof(ProbeMain).Assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0)
                    continue;
                try
                {
                    _harmony.CreateClassProcessor(type).Patch();
                    applied++;
                }
                catch (Exception ex)
                {
                    skipped++;
                    Log.Warn($"[saria] patch {type.Name} skipped: {ex.GetBaseException().Message}");
                }
            }

            Log.Info($"[saria] harmony patches: {applied} applied, {skipped} skipped");

            // 能量图标：把运行时贴图占住 res:// 路径，正文 {Energy} 与卡面图标都会用它
            ProbeEnergyIcons.Register();

        // 诊断：确认 pck 里的卡图能被 ResourceLoader 看到、选中音效文件存在
        string probePath = "res://saria/images/portraits/ProbeStrike.png";
        bool probeExists = Godot.ResourceLoader.Exists(probePath);
        var probeTex = probeExists ? Godot.ResourceLoader.Load<Godot.Texture2D>(probePath) : null;
        Log.Info($"[saria][art] pck portrait: exists={probeExists} loaded={probeTex != null} size={(probeTex != null ? probeTex.GetSize().ToString() : "n/a")}");
        Log.Info($"[saria][art] select ogg exists = {Godot.FileAccess.FileExists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Godot.OS.GetExecutablePath()) ?? "", "mods", "saria", "audio", "selected.ogg"))}");
        }
        catch (Exception ex)
        {
            Log.Error($"[saria] patch setup failed: {ex}");
        }
    }
}
