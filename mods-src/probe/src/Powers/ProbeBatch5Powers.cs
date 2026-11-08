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
using MegaCrit.Sts2.Core.Models.Powers;

namespace Probe.Powers;

// 2026-11-08 batch 3.

/// <summary>再塑型 — Amount = 本回合被临时拿走的敏捷，回合结束时原样还回去。</summary>
public sealed class ProbeReshapePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        bool ours = false;
        foreach (var creature in participants)
        {
            if (creature == Owner)
            {
                ours = true;
                break;
            }
        }

        if (!ours || Amount <= 0)
            return;

        Flash();
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner, Amount, Owner, null);
        await PowerCmd.Remove(this);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "再塑型"),
        ("description", "在本回合结束前，失去[gold]敏捷[/gold]。"),
        ("smartDescription", "在本回合结束前，失去[blue]{Amount}[/blue]点[gold]敏捷[/gold]。"),
    };
}

/// <summary>洞察 — Amount = 每次获得钙铠时给予所有敌人的易伤/虚弱层数；本回合结束消失。</summary>
public sealed class ProbeInsightPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is not CalciumArmorPower || power.Owner != Owner || amount <= 0 || Amount <= 0)
            return;

        Flash();
        foreach (var enemy in CombatState.HittableEnemies)
        {
            await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, Amount, Owner, null);
            await PowerCmd.Apply<WeakPower>(choiceContext, enemy, Amount, Owner, null);
        }
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        foreach (var creature in participants)
        {
            if (creature == Owner)
            {
                await PowerCmd.Remove(this);
                return;
            }
        }
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "洞察"),
        ("description", "你在本回合每次获得[gold]钙铠[/gold]时，给予所有敌人[gold]易伤[/gold]与[gold]虚弱[/gold]。"),
        ("smartDescription", "你在本回合每次获得[gold]钙铠[/gold]时，给予所有敌人[blue]{Amount}[/blue]层[gold]易伤[/gold]与[blue]{Amount}[/blue]层[gold]虚弱[/gold]。"),
    };
}
