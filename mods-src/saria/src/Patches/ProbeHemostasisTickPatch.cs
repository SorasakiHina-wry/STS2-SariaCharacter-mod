using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using Saria.Powers;

namespace Saria.Patches;

/// <summary>
/// 止血的「受到伤害次数」计数。
///
/// 用户口径：**任何伤害都算一次，哪怕被格挡完全挡住**；「失去生命」类（Unblockable）不计。
/// power 自身的 `BeforeDamageReceived` 在这条链路上不会被派发（实测），
/// 所以在静态入口 <see cref="Hook.BeforeDamageReceived"/> 挂 postfix：每段真实伤害一次、早于格挡。
/// 这里**只负责扣次数**；额度用尽后的「完全阻止」由 <see cref="ProbeHemostasisPower"/> 的
/// ModifyDamageCap 负责，避免预览伤害也来扣次数。
/// </summary>
[HarmonyPatch(typeof(Hook), nameof(Hook.BeforeDamageReceived))]
public static class ProbeHemostasisTickPatch
{
    [HarmonyPostfix]
    private static void Postfix(Creature target, decimal amount, ValueProp props)
    {
        if (amount <= 0)
            return;

        if (props.HasFlag(ValueProp.Unblockable))
            return;   // 「失去生命」类伤害不计

        foreach (var power in target.Powers)
        {
            if (power is ProbeHemostasisPower hemostasis)
            {
                hemostasis.ConsumeHit();
                break;
            }
        }
    }
}
