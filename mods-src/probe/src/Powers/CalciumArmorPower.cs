using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.ValueProps;

namespace Probe.Powers;

/// <summary>
/// 钙铠 — the character's signature defensive buff.
///
///   At the start of your turn: gain Block equal to the stack count.
///   Each time an attack hits you: lose 1 stack — one per hit, so a 3-hit attack strips 3.
///
/// Decay is counted from the attack's damage-result batches (AttackCommand.Results holds one list
/// per hit), not from BeforeDamageReceived: that damage hook does not fire once per hit, which made
/// a two-hit attack only strip a single layer. Statuses, curses and self-damage never decay it.
/// </summary>
public sealed class CalciumArmorPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    // Placeholder art: reuse a vanilla power icon until custom art exists.
    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    /// <summary>Start of the player's turn: block == stacks.</summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
            return;

        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }

    /// <summary>Every hit of an incoming attack strips one stack.</summary>
    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (Amount <= 0)
            return;

        if (command.Attacker == Owner)
            return; // being the attacker never decays your own armour

        if (!command.DamageProps.HasFlag(ValueProp.Move))
            return; // not an attack (statuses, curses, pure effects)

        int hits = 0;
        foreach (List<DamageResult> hit in command.Results)
        {
            foreach (DamageResult result in hit)
            {
                if (result.Receiver == Owner)
                {
                    hits++;
                    break;
                }
            }
        }

        if (hits <= 0)
            return; // the attack never touched us

        Log.Info($"[probe] 钙铠 takes {hits} hit(s), losing {Math.Min(hits, Amount)} stack(s)");
        Flash();
        for (int i = 0; i < hits && Amount > 0; i++)
            await PowerCmd.Decrement(this);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "钙铠"),
        ("description", "回合开始时，获得与层数相等的[gold]格挡[/gold]。每次受到攻击时失去 1 层。"),
    };
}
