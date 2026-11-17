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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Probe.Powers;

// Turn-start companions for 钙铠 cards. They run in AfterPlayerTurnStart, which fires after
// AfterSideTurnStart - the hook 钙铠 pays its block out in - so "after the armour gives block"
// holds regardless of model iteration order.

/// <summary>
/// 分神 — vanilla-style single power with an internal counter.
///
///   Amount        = 回合开始时将失去的格挡总量（分神每次 +3，分神+ 每次 +2）
///   ArmorGain     = 打出次数（内部计数器，不显示在图标上；每次打出 +1）
///   Turn start    : 失去 min(Amount, 当前格挡) 点格挡，获得 ArmorGain 层钙铠
///
/// The card bumps ArmorGain through IncrementArmorGain(), exactly like vanilla InfernoPower.
/// </summary>
public sealed class ProbeDistractPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("ArmorGain", 0m),
    };

    /// <summary>Called by 分神 / 分神+ after applying: one more play means one more 钙铠 next turn.</summary>
    public void IncrementArmorGain()
    {
        AssertMutable();
        DynamicVars["ArmorGain"].BaseValue++;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
            return;

        if (Owner.Block > 0)
        {
            decimal loss = Math.Min(Amount, Owner.Block);
            if (loss > 0)
                await CreatureCmd.LoseBlock(choiceContext, Owner, loss, null);
        }

        int armour = DynamicVars["ArmorGain"].IntValue;
        if (armour <= 0)
            return;

        Flash();
        await PowerCmd.Apply<CalciumArmorPower>(choiceContext, Owner, armour, Owner, null);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "分神"),
        ("description", "回合开始时，失去{Amount}层[gold]格挡[/gold]，获得{ArmorGain}层[gold]钙铠[/gold]。"),
    };
}

/// <summary>
/// 凝神 — vanilla-style single power (see docs 9.3 / 9.4).
///
///   Amount     = 回合开始时获得的活力（图标显示；每次「凝神」+4，「凝神+」+6）
///   ArmorLose  = 打出次数（内部计数器，每次打出 +1，不显示在图标上）
///   Turn start : 失去 min(ArmorLose, 当前钙铠) 层钙铠，获得 Amount 点活力
/// </summary>
public sealed class ProbeFocusMindPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("ArmorLose", 0m),
    };

    /// <summary>Called by 凝神 / 凝神+ after applying: one more play means one more 钙铠 consumed.</summary>
    public void IncrementArmorLose()
    {
        AssertMutable();
        DynamicVars["ArmorLose"].BaseValue++;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
            return;

        var armour = Owner.GetPower<CalciumArmorPower>();
        if (armour != null && armour.Amount > 0)
        {
            int loss = Math.Min(DynamicVars["ArmorLose"].IntValue, armour.Amount);
            if (loss > 0)
                await PowerCmd.ModifyAmount(choiceContext, armour, -loss, Owner, null);
        }

        Flash();
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner, Amount, Owner, null);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "凝神"),
        ("description", "回合开始时，失去{ArmorLose}层[gold]钙铠[/gold]，获得{Amount}点[gold]活力[/gold]。"),
    };
}
