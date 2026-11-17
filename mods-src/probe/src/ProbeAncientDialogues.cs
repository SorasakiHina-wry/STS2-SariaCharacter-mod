using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace Saria;

/// <summary>
/// 把塞雷娅的先古对话接进原版先古。
///
/// 原版每个先古的 <see cref="AncientDialogueSet"/> 里写死了 CharacterDialogues 字典（只含 5 位原版角色），
/// GetValidDialogues 用 `characterId.Entry` 查表，自定义角色永远查不到，所以必须自己注入。
///
/// 做法：patch GetValidDialogues，先推断这个 set 属于哪位先古（从已有对话的本地化键前缀，
/// 形如 `NEOW.talk.IRONCLAD.0-0.ancient`），再返回我们自己的对话列表。
/// 全存档首次（totalVisits == 0）保持原版，因为 firstVisitEver 是所有角色共用的。
/// </summary>
[HarmonyPatch(typeof(AncientDialogueSet), nameof(AncientDialogueSet.GetValidDialogues))]
public static class ProbeAncientDialoguePatch
{
    private static readonly Dictionary<string, List<AncientDialogue>> Cache = new();

    [HarmonyPostfix]
    private static void Postfix(
        AncientDialogueSet __instance,
        ModelId characterId,
        int charVisits,
        int totalVisits,
        bool allowAnyCharacterDialogues,
        ref IEnumerable<AncientDialogue> __result)
    {
        if (characterId.Entry != ProbeAncientDialogueData.CharacterEntry)
            return;

        if (totalVisits == 0)
            return; // 全存档首次：原版 firstVisitEver（所有角色共用）

        string? ancientEntry = GuessAncientEntry(__instance);
        if (ancientEntry == null || !ProbeAncientDialogueData.Sets.TryGetValue(ancientEntry, out var sets))
            return;

        var dialogues = Build(ancientEntry, sets);
        if (dialogues.Count == 0)
            return;

        var visitSpecific = dialogues.Where(d => d.VisitIndex == charVisits).ToList();
        if (visitSpecific.Count > 0)
        {
            __result = visitSpecific;
            return;
        }

        var repeating = dialogues.Where(d => d.IsRepeating).ToList();
        if (repeating.Count > 0)
        {
            __result = repeating;
            return;
        }

        __result = dialogues;
    }

    private static string? GuessAncientEntry(AncientDialogueSet set)
    {
        foreach (var dialogue in set.GetAllDialogues())
        {
            if (dialogue.Lines.Count == 0)
                continue;
            string? key = dialogue.Lines[0].LineText?.LocEntryKey;
            if (!string.IsNullOrEmpty(key) && key.Contains(".talk."))
                return key.Split('.')[0];
        }
        return null;
    }

    private static List<AncientDialogue> Build(string ancientEntry, (int Index, int Lines, bool LastHasNext)[] sets)
    {
        if (Cache.TryGetValue(ancientEntry, out var cached))
            return cached;

        var list = new List<AncientDialogue>();
        foreach (var (index, lineCount, lastHasNext) in sets)
        {
            try
            {
                var sfx = new string[Math.Max(lineCount, 1)];
                for (int i = 0; i < sfx.Length; i++)
                    sfx[i] = string.Empty;

                var dialogue = new AncientDialogue(sfx) { VisitIndex = index };
                dialogue.PopulateLines(ancientEntry, ProbeAncientDialogueData.CharacterEntry, index);

                // PopulateLines 只填台词，按钮文字（`.next`）是 AncientDialogueSet.PopulateLocKeys 里补的；
                // 我们的对话是在那之后注入的，所以这里自己照同样的规则补一遍：
                // 第 k 句的按钮键 = 该句本地化键去掉最后一段 + ".next"。
                int textLines = lastHasNext ? dialogue.Lines.Count : dialogue.Lines.Count - 1;
                for (int k = 0; k < textLines; k++)
                {
                    var line = dialogue.Lines[k];
                    string? key = line.LineText?.LocEntryKey;
                    if (string.IsNullOrEmpty(key))
                        continue;
                    string next = key.Substring(0, key.LastIndexOf('.')) + ".next";
                    line.NextButtonText = new LocString("ancients", next);
                }

                list.Add(dialogue);
            }
            catch (Exception ex)
            {
                Log.Error($"[saria] dialogue build failed {ancientEntry}#{index}: {ex.Message}");
            }
        }

        Log.Info($"[saria] ancient dialogues ready: {ancientEntry} -> {list.Count} 组");
        Cache[ancientEntry] = list;
        return list;
    }
}
