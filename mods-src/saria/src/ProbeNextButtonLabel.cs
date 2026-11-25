using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Events;

namespace Saria;

/// <summary>
/// 先古对话右下角按钮上的字。
///
/// 原版 `UpdateFakeNextButton` 只看当前句的 `NextButtonText`，而 `AncientDialogueSet.PopulateLocKeys`
/// 只给「非最后一句」补这个字段（165 套原版对话里，最后一句一律没有 `.next`）。
/// 我们的作者会在最后一句之后也写 `[钮]`（例如涅奥的「接过祝福」），那段文字在原版逻辑里就永远不显示。
///
/// 所以这里补一条 postfix：当前句没有按钮文字、但有我们注入的文本时，手动显示它。
/// 非最后一句完全走原版逻辑，不改变行为。
/// </summary>
[HarmonyPatch(typeof(NAncientEventLayout), "UpdateFakeNextButton")]
public static class ProbeNextButtonLabelPatch
{
    private static readonly AccessTools.FieldRef<NAncientEventLayout, List<AncientDialogueLine>> DialogueRef =
        AccessTools.FieldRefAccess<NAncientEventLayout, List<AncientDialogueLine>>("_dialogue");

    private static readonly AccessTools.FieldRef<NAncientEventLayout, int> CurrentLineRef =
        AccessTools.FieldRefAccess<NAncientEventLayout, int>("_currentDialogueLine");

    private static readonly AccessTools.FieldRef<NAncientEventLayout, MegaLabel> LabelRef =
        AccessTools.FieldRefAccess<NAncientEventLayout, MegaLabel>("_fakeNextButtonLabel");

    [HarmonyPostfix]
    private static void Postfix(NAncientEventLayout __instance)
    {
        List<AncientDialogueLine>? dialogue = DialogueRef(__instance);
        if (dialogue == null || dialogue.Count == 0)
            return;

        int index = CurrentLineRef(__instance);
        if (index < 0 || index >= dialogue.Count - 1)
            return; // 非最后一句走原版逻辑即可

        LocString? text = dialogue[index].NextButtonText;
        if (text == null)
            return;

        MegaLabel label = LabelRef(__instance);
        if (label == null)
        {
            Log.Warn("[saria] next button label not found");
            return;
        }

        label.SetTextAutoSize(text.GetFormattedText() ?? "");
        label.Visible = true;
    }
}
