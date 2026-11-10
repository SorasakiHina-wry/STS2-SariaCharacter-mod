using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace Probe;

/// <summary>
/// Entry point. BaseLib discovers the custom models by scanning this assembly, so nothing has to be
/// registered manually here; this applies the mod's Harmony patches and logs a load marker.
/// </summary>
[ModInitializer("Init")]
public static class ProbeMain
{
    public const string ModId = "probe";

    private static Harmony? _harmony;

    public static void Init()
    {
        Log.Info("[probe] initializing validation character mod");

        try
        {
            _harmony = new Harmony("com.sts2modding.probe");
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
                    Log.Warn($"[probe] patch {type.Name} skipped: {ex.GetBaseException().Message}");
                }
            }

            Log.Info($"[probe] harmony patches: {applied} applied, {skipped} skipped");

            ProbeSpineProbe.Run();
        }
        catch (Exception ex)
        {
            Log.Error($"[probe] patch setup failed: {ex}");
        }
    }
}
