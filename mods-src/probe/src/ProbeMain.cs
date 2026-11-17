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
        }
        catch (Exception ex)
        {
            Log.Error($"[saria] patch setup failed: {ex}");
        }
    }
}
