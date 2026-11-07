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
using MegaCrit.Sts2.Core.Models.Powers;

namespace Probe.Powers;

// Turn-start companions for 钙铠 cards. They run in AfterPlayerTurnStart, which fires after
// AfterSideTurnStart - the hook 钙铠 pays its block out in - so "after the armour gives block"
// holds regardless of model iteration order.

/// <summary>分神: lose 2 block, then gain stacks of 钙铠.</summary>
public sealed class ProbeDistractPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
            return;

        if (Owner.Block > 0)
        {
            decimal loss = Math.Min(2m, Owner.Block);
            await CreatureCmd.LoseBlock(choiceContext, Owner, loss, null);
        }

        Flash();
        await PowerCmd.Apply<CalciumArmorPower>(choiceContext, Owner, Amount, Owner, null);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "分神"),
        ("description", "回合开始时，失去 2 点格挡，获得与层数相等的钙铠"),
    };
}

/// <summary>凝神: spend one 钙铠 stack, gain stacks of Vigor.</summary>
public sealed class ProbeFocusMindPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
            return;

        var armour = Owner.GetPower<CalciumArmorPower>();
        if (armour != null && armour.Amount > 0)
            await PowerCmd.Decrement(armour);

        Flash();
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner, Amount, Owner, null);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "凝神"),
        ("description", "回合开始时，失去 1 层钙铠，获得与层数相等的活力"),
    };
}
