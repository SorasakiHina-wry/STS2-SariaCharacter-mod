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
/// 把塞雷娅的先古对话接进原版先古，并且**沿用原版的抽取优先级**。
///
/// 原版 <see cref="AncientDialogueSet.GetValidDialogues"/> 的规则（见其源码）：
///   1. totalVisits == 0 且存在 FirstVisitEverDialogue → 只返回它（全存档首次，所有角色共用）
///   2. 该角色在 charVisits 这一档的专属对话 → 有就直接返回（不与通用混合）
///   3. 否则若允许通用对话：ANY 同档位的对话 → 有就返回
///   4. 都不满足 → 取「重复池」：角色专属的重复对话 + ANY 的重复对话**混在一起**随机抽
///
/// 自定义角色不在原版 CharacterDialogues 字典里，所以原版只会拿到 ANY 那部分。
/// 这里把我们自己的对话当成"该角色的专属对话"塞进同一套流程，于是：
///   - 有专属的当次对话 → 用我们的（和原版角色一致）
///   - 没有 → 退回 ANY（和原版一致）
///   - 重复池 → 我们 + ANY 混合抽（和原版一致）
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

        // 全存档首次：原版 firstVisitEver 是所有角色共用的，不插手
        if (totalVisits == 0)
            return;

        var mine = TryBuildOurDialogues(__instance);
        var any = __instance.AgnosticDialogues ?? Array.Empty<AncientDialogue>();

        // 1) 角色专属（这一档访问次数）
        if (mine != null)
        {
            var visitSpecific = mine.Where(d => d.VisitIndex == charVisits).ToList();
            if (visitSpecific.Count > 0)
            {
                __result = visitSpecific;
                return;
            }
        }

        // 2) 通用对话（这一档访问次数）
        if (allowAnyCharacterDialogues)
        {
            var genericSpecific = any.Where(d => d.VisitIndex == charVisits).ToList();
            if (genericSpecific.Count > 0)
            {
                __result = genericSpecific;
                return;
            }
        }

        // 3) 重复池：角色专属 + 通用，混在一起由调用方随机抽
        var pool = new List<AncientDialogue>();
        if (mine != null)
            AddRepeating(mine, pool, charVisits);
        if (allowAnyCharacterDialogues)
            AddRepeating(any, pool, charVisits);

        if (pool.Count > 0)
            __result = pool;
        // 两者都空：保留原版结果
    }

    /// <summary>与原版 AncientDialogueSet.AddRepeatingDialogues 一致。</summary>
    private static void AddRepeating(IEnumerable<AncientDialogue> source, List<AncientDialogue> destination, int charVisits)
    {
        foreach (var dialogue in source)
        {
            if (dialogue.IsRepeating && (!dialogue.VisitIndex.HasValue || charVisits >= dialogue.VisitIndex.Value))
                destination.Add(dialogue);
        }
    }

    private static List<AncientDialogue>? TryBuildOurDialogues(AncientDialogueSet set)
    {
        string? ancientEntry = GuessAncientEntry(set);
        if (ancientEntry == null || !ProbeAncientDialogueData.Sets.TryGetValue(ancientEntry, out var sets))
            return null;

        var dialogues = Build(ancientEntry, sets);
        return dialogues.Count > 0 ? dialogues : null;
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

                // 建筑师结局的「对峙」动画由 AncientDialogue 的两个 init 字段驱动（原版每个角色都写死了）：
                //   StartAttackers = 对话开始时谁出手；EndAttackers = 结束时谁出手
                // 我们自建的对话若保持默认（None），结局就完全没有攻击动画。
                // 对齐原版：结尾一律双方互击；后续轮次开头由玩家先出手（同 Silent）。
                var dialogue = ancientEntry == "THE_ARCHITECT"
                    ? new AncientDialogue(sfx)
                    {
                        VisitIndex = index,
                        EndAttackers = ArchitectAttackers.Both,
                        StartAttackers = index > 0 ? ArchitectAttackers.Player : ArchitectAttackers.None,
                    }
                    : new AncientDialogue(sfx) { VisitIndex = index };

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

        Log.Info($"[saria] ancient dialogues ready: {ancientEntry} -> {list.Count} 组" +
                 $" (repeating {list.Count(d => d.IsRepeating)})");
        Cache[ancientEntry] = list;
        return list;
    }
}
