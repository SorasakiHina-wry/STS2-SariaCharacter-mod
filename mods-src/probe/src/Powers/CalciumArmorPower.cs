using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Probe.Powers;

/// <summary>
/// 钙铠 — the character's signature defensive buff.
///
///   At the start of your turn: gain Block equal to the stack count.
///   Whenever you gain Block FROM A CARD: lose half that Block's value in stacks (rounded down).
///
/// "From a card" mirrors how Dexterity scopes its bonus: block granted by powers, relics or the
/// power's own turn-start payout (cardSource == null) never decays the armour.
/// 紧密防护 suppresses the decay until the next turn starts (see SuppressDecay).
/// </summary>
public sealed class CalciumArmorPower : CustomPowerModel
{
    /// <summary>Creatures whose armour ignores card-block decay until their next turn begins.</summary>
    private static readonly HashSet<Creature> Suppressed = new();

    /// <summary>Called by 紧密防护: stop card-block decay until the next player turn starts.</summary>
    public static void SuppressDecay(Creature creature) => Suppressed.Add(creature);

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // Placeholder art: reuse a vanilla power icon until custom art exists.
    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    /// <summary>
    /// Start of the player's side turn: block == stacks, and 紧密防护's protection lapses.
    /// This lives in AfterSideTurnStart (the hook vanilla turn-start powers use) so card-side
    /// turn-start effects like 分神 / 凝神, which hook AfterPlayerTurnStart, always resolve after it.
    /// </summary>
    public override async Task AfterSideTurnStart(
        CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        bool ours = false;
        for (int i = 0; i < participants.Count; i++)
        {
            if (participants[i] == Owner)
            {
                ours = true;
                break;
            }
        }

        if (!ours)
            return;

        Suppressed.Remove(Owner);

        if (Amount <= 0)
            return;

        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }

    /// <summary>Card-gained block costs half its value in stacks.</summary>
    public override async Task AfterBlockGained(Creature creature, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (creature != Owner || cardSource == null || Amount <= 0)
            return; // not us, or not block from a card

        if (Suppressed.Contains(Owner))
            return; // 紧密防护 is active

        int loss = (int)(amount / 2m); // 向下取整
        if (loss <= 0)
            return;

        if (loss > Amount)
            loss = Amount;

        Flash();
        await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -loss, Owner, cardSource);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "钙铠"),
        // 描述原样取自 design/powers.md
        ("description", "回合开始时，获得与层数相等的格挡。当你从卡牌中获得格挡时，失去获得格挡数值一半的钙铠。"),
    };
}
