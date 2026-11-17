using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Saria.Patches;

/// <summary>
/// 原版「疯狂科学」按类型走不同分支：攻击走 AttackCommand、技能只加格挡、能力调
/// <c>TriggerAnim(Cast)</c>。但用 `Cast` 会播到技能动画（骨架里 cast = Skill_Begin），
/// 于是它是全场唯一"能力牌却做技能动作"的卡。
///
/// 这里只改这一处：把 <c>ExecutePower</c> 里的 "Cast" 字面量换成 "PowerUp"，
/// 让它和普通能力牌走同一条动画链路。ExecutePower 是 async，方法体在编译器生成的
/// 状态机 <c>&lt;ExecutePower&gt;d__N.MoveNext</c> 里，所以补丁打在嵌套类型的 MoveNext 上。
/// </summary>
[HarmonyPatch]
public static class MadSciencePowerAnimPatch
{
    private static MethodBase TargetMethod()
    {
        foreach (var nested in typeof(MadScience).GetNestedTypes(BindingFlags.NonPublic))
        {
            if (!nested.Name.Contains("ExecutePower"))
                continue;
            var moveNext = nested.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (moveNext != null)
                return moveNext;
        }

        throw new MissingMethodException("MadScience.ExecutePower state machine not found");
    }

    [HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        bool patched = false;
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldstr && instruction.operand is string text && text == "Cast")
            {
                instruction.operand = "PowerUp";
                patched = true;
            }
            yield return instruction;
        }

        if (!patched)
            Log.Warn("[saria] MadScience ExecutePower: \"Cast\" literal not found; power animation unchanged");
    }
}
