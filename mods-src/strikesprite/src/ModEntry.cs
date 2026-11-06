using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace StrikeSprite;

/// <summary>
/// Mod entry point. The game calls <see cref="Init"/> from ModManager while it is still
/// starting up — before combat code is JIT-compiled, which is what makes the Harmony
/// detours effective (patching later, e.g. via hot swap, does not intercept inlined
/// call sites).
/// </summary>
[ModInitializer(nameof(Init))]
public static class ModEntry
{
    private const string HarmonyId = "dev.strikesprite";

    public static Harmony? Harmony { get; private set; }

    public static void Init()
    {
        ModLog.Init();
        ModLog.Info($"v{typeof(ModEntry).Assembly.GetName().Version} initialising");

        try
        {
            Harmony = new Harmony(HarmonyId);
            ApplyPatches(Harmony);
        }
        catch (System.Exception ex)
        {
            ModLog.Error($"init failed: {ex}");
        }
    }

    /// <summary>
    /// Patch class-by-class instead of Harmony's PatchAll: one stale target (game update
    /// renamed a hook) then costs that single patch instead of the whole mod.
    /// </summary>
    private static void ApplyPatches(Harmony harmony)
    {
        int applied = 0;
        int skipped = 0;

        foreach (var type in typeof(ModEntry).Assembly.GetTypes())
        {
            if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0)
                continue;

            try
            {
                harmony.CreateClassProcessor(type).Patch();
                applied++;
            }
            catch (System.Exception ex)
            {
                skipped++;
                ModLog.Warn($"patch class {type.Name} skipped: {ex.GetBaseException().Message}");
            }
        }

        ModLog.Info($"harmony: {applied} class(es) applied, {skipped} skipped");
    }
}
