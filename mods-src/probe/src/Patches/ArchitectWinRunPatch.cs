using System;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Models.Events;

namespace Saria.Patches;

/// <summary>
/// A custom character can never win a run without this.
///
/// <c>TheArchitect.LoadDialogue</c> only knows the five vanilla characters, so for a modded one the
/// dialogue list is empty, <c>Rng.NextItem</c> returns null and <c>_dialogue</c> stays null - which
/// is harmless everywhere except the victory confirmation:
///
///     private async Task WinRun()
///     {
///         if (LocalContext.IsMe(Owner))
///         {
///             await AnimPlayerAttackIfNecessary(Dialogue.EndAttackers);   // Dialogue is null -> NRE
///             ...
///             await RunManager.Instance.WinRun();                          // never reached
///         }
///     }
///
/// The event still offers its single PROCEED option (GenerateInitialOptions has a null-dialogue
/// branch), so the player clicks it and gets a NullReferenceException instead of a win.
///
/// This prefix installs a silent placeholder dialogue *only when the real one is missing*, then lets
/// the original method run. Skipping the original instead would return a null Task to
/// EventOption.Chosen, which then throws on its own - the placeholder keeps the whole flow intact.
/// Vanilla characters are untouched: their dialogue is never null.
/// </summary>
[HarmonyPatch(typeof(TheArchitect), "WinRun")]
public static class ArchitectWinRunPatch
{
    private static AncientDialogue? _placeholder;

    private static AncientDialogue Placeholder()
    {
        if (_placeholder != null)
            return _placeholder;

        // One line is required by the constructor; a null LineText keeps it silent, and
        // EndAttackers stays None so WinRun skips the architect attack animation.
        var dialogue = new AncientDialogue("");
        dialogue.Lines[0].LineText = null;
        dialogue.Lines[0].Speaker = AncientDialogueSpeaker.Character;
        _placeholder = dialogue;
        return _placeholder;
    }

    private static bool Prefix(TheArchitect __instance)
    {
        var field = AccessTools.Field(typeof(TheArchitect), "_dialogue");
        if (field == null)
        {
            MegaCrit.Sts2.Core.Logging.Log.Warn("[saria] TheArchitect._dialogue not found; victory fix inactive");
            return true;
        }

        if (field.GetValue(__instance) == null)
        {
            MegaCrit.Sts2.Core.Logging.Log.Info("[saria] installing placeholder architect dialogue for custom character");
            field.SetValue(__instance, Placeholder());
        }

        return true;
    }
}
