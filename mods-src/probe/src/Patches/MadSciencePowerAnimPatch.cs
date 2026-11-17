using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Saria.Patches;

/// <summary>
/// 原版「疯狂科学」按类型走不同分支：攻击走 AttackCommand、技能只加格挡、能力调
/// <c>TriggerAnim(Cast)</c>。但能力牌用 `Cast` 会播到技能动画（我们的骨架里 cast = Skill_Begin），
/// 导致它是全场唯一"能力牌却做技能动作"的卡。
///
/// 这里只改这一处：把 <c>ExecutePower</c> 里的 "Cast" 字面量换成 "PowerUp"，
/// 让它和普通能力牌走同一条动画链路（PowerUp → 骨架的 powerup 剪辑）。
/// 影响面仅限于这张牌的能力分支；原版角色的 PowerUp 映射不受本 mod 影响。
/// </summary>
[HarmonyPatch(typeof(MadScience), "ExecutePower")]
public static class MadSciencePowerAnimPatch
{
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
            Log.Warn("[saria] MadScience.ExecutePower: \"Cast\" literal not found; power animation unchanged");
    }
}
