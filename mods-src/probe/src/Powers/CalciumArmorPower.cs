using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Saria.Powers;

/// <summary>
/// 钙铠 — the character's signature defensive buff.
///
///   At the start of your turn: gain Block equal to the stack count (Early turn-start pass).
///   Whenever you gain Block FROM A CARD: lose half that Block's value in stacks (rounded down),
///   then 骨矿化 tops the armour back up.
///
/// "From a card" mirrors how Dexterity scopes its bonus: block granted by powers, relics or the
/// power's own turn-start payout (cardSource == null) never decays the armour.
/// 紧密防护 (ProbeTightGuardPower) suppresses the decay while it lasts.
/// </summary>
public sealed class CalciumArmorPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // Placeholder art: reuse a vanilla power icon until custom art exists.
    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    /// <summary>
    /// Early start of the player's turn: block == stacks.
    ///
    /// The engine runs every turn-start listener three times in separate passes - Early, normal,
    /// then Late (see Hook.AfterPlayerTurnStart) - so placing this power in the Early pass makes it
    /// resolve before any card-side turn-start power that uses the normal pass (分神 / 凝神),
    /// regardless of icon order.
    /// </summary>
    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        if (Amount <= 0)
            return;

        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }

    /// <summary>Card-gained block costs half its value in stacks, then 骨矿化 pays out.</summary>
    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature != Owner || cardSource == null)
            return; // not us, or not block from a card

        bool guarded = Owner.GetPower<ProbeTightGuardPower>() != null;

        if (Amount > 0 && !guarded)
        {
            int loss = (int)(amount / 2m); // 向下取整
            if (loss > Amount)
                loss = Amount;

            if (loss > 0)
            {
                Flash();
                await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -loss, Owner, cardSource);
            }
        }

        // 骨矿化：先掉层，再补层（用户确认的结算顺序）
        var bone = Owner.GetPower<ProbeBoneMineralizePower>();
        if (bone != null && bone.Amount > 0)
            await PowerCmd.Apply<CalciumArmorPower>(new ThrowingPlayerChoiceContext(), Owner, bone.Amount, Owner, cardSource);
    }

    /// <summary>How many armour stacks have been gained in this combat (used by 战略终结).</summary>
    public int TotalGainedThisCombat { get; private set; }

    /// <summary>
    /// Tracks armour gained/lost so other cards can react:
    ///   共振  — every gained stack deals damage once
    ///   防卫经验 — the first 8 lost stacks each turn draw cards
    /// </summary>
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power is not CalciumArmorPower armour || armour.Owner != Owner)
            return;

        if (amount > 0)
        {
            int gained = (int)amount;
            TotalGainedThisCombat += gained;

            var practice = Owner.GetPower<ProbePracticePower>();
            if (practice != null)
                await practice.OnArmourGained(choiceContext, gained);

            var resonance = Owner.GetPower<ProbeResonancePower>();
            if (resonance != null && resonance.Amount > 0 && resonance.Target != null && resonance.Target.IsAlive)
            {
                Flash();
                int hits = gained * (int)resonance.Amount;
                for (int i = 0; i < hits; i++)
                {
                    await CreatureCmd.Damage(choiceContext, resonance.Target, ProbeResonancePower.DamagePerStack,
                        ValueProp.Unpowered, Owner, null, null);
                }
            }
        }
        else if (amount < 0)
        {
            var experience = Owner.GetPower<ProbeDefenseExperiencePower>();
            if (experience != null)
                await experience.OnArmourLost(choiceContext, (int)(-amount));

            // 覆还：本回合每失去 1 层，就把等量钙铠记到下回合
            var giveBack = Owner.GetPower<ProbeReturnPower>();
            if (giveBack != null && giveBack.Amount > 0)
            {
                decimal carry = (int)(-amount) * giveBack.Amount;
                await PowerCmd.Apply<ProbeNextTurnArmorPower>(choiceContext, Owner, carry, Owner, null);
            }
        }
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "钙铠"),
        // 原版范式：description 不带数字，smartDescription 用 [blue]{Amount}[/blue]
        ("description", "在你的回合开始时，获得[gold]格挡[/gold]。当你从卡牌中获得格挡时，失去获得格挡数值一半的钙铠。"),
        ("smartDescription", "在你的回合开始时，获得[blue]{Amount}[/blue]点[gold]格挡[/gold]。当你从卡牌中获得格挡时，失去获得格挡数值一半的钙铠。"),
    };
}
