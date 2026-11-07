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
    /// Early start of the player's turn: block == stacks, and 紧密防护's protection lapses.
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
        // 原版范式：description 不带数字，smartDescription 用 [blue]{Amount}[/blue]
        ("description", "在你的回合开始时，获得[gold]格挡[/gold]。当你从卡牌中获得格挡时，失去获得格挡数值一半的钙铠。"),
        ("smartDescription", "在你的回合开始时，获得[blue]{Amount}[/blue]点[gold]格挡[/gold]。当你从卡牌中获得格挡时，失去获得格挡数值一半的钙铠。"),
    };
}
