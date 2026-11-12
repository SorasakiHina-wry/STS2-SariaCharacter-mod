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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Probe.Powers;

// 2026-11-08 batch. All follow the vanilla pattern: one power per mechanic, Amount on the icon,
// extra numbers in named DynamicVars (docs 9.3 / 9.4).

/// <summary>
/// 煅烧 — Amount = damage each turn (icon), ArmorLose = 钙铠 consumed each turn (counter, +1 per play).
/// </summary>
public sealed class ProbeBurnPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new IntVar("ArmorLose", 0m),
    };

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
        await CreatureCmd.Damage(choiceContext, CombatState.HittableEnemies, Amount, ValueProp.Unpowered, Owner);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "煅烧"),
        ("description", "回合开始时，失去[gold]钙铠[/gold]，对所有敌人造成伤害。"),
        ("smartDescription", "回合开始时，失去[blue]{ArmorLose}[/blue]层[gold]钙铠[/gold]，对所有敌人造成[blue]{Amount}[/blue]点伤害。"),
    };
}

/// <summary>
/// 急救 — Amount = 剩余触发次数（图标显示）。回合结束且生命 ≤25 时，用至多 15 层钙铠换取等量治疗，然后次数 -1。
/// </summary>
public sealed class ProbeFirstAidPower : CustomPowerModel
{
    private const int ArmorCap = 15;
    private const int HpThreshold = 25;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0 || Owner.CurrentHp > HpThreshold)
            return;

        var armour = Owner.GetPower<CalciumArmorPower>();
        int available = armour?.Amount ?? 0;
        int loss = Math.Min(available, ArmorCap);
        if (loss > 0 && armour != null)
        {
            await PowerCmd.ModifyAmount(choiceContext, armour, -loss, Owner, null);
            await CreatureCmd.Heal(Owner, loss);
        }

        Flash();
        await PowerCmd.Decrement(this);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "急救"),
        ("description", "回合开始时，若生命垂危，则用[gold]钙铠[/gold]换取等量治疗。"),
        ("smartDescription", "回合开始时，若生命小于等于[blue]25[/blue]，则失去至多[blue]15[/blue]层[gold]钙铠[/gold]并回复等量生命，然后次数 -1。当前剩余[blue]{Amount}[/blue]次。"),
    };
}

/// <summary>钙质化 — Amount = 每层易伤带来的额外伤害。伤害结算由 <see cref="Probe.Patches.CalcifyDamagePatch"/> 加上。</summary>
public sealed class ProbeVulnerableBonusPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "钙质化"),
        ("description", "目标每有1层[gold]易伤[/gold]，你的攻击就额外造成伤害。"),
        ("smartDescription", "目标每有1层[gold]易伤[/gold]，你的攻击就额外造成[blue]{Amount}[/blue]点伤害。"),
    };
}

/// <summary>牵引 — 每当你失去钙铠，获得 Amount 点格挡。</summary>
public sealed class ProbeTractionPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is not CalciumArmorPower || power.Owner != Owner || amount >= 0 || Amount <= 0)
            return;

        Flash();
        await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Unpowered, null);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "牵引"),
        ("description", "每当你失去[gold]钙铠[/gold]时，获得[gold]格挡[/gold]。"),
        ("smartDescription", "每当你失去[gold]钙铠[/gold]时，获得[blue]{Amount}[/blue]点[gold]格挡[/gold]。"),
    };
}

/// <summary>充能护服 — Amount = 剩余回合数（图标）。每回合开始时获得 1 力量 1 敏捷，然后 -1。</summary>
public sealed class ProbeChargeSuitPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
            return;

        Flash();
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, 1m, Owner, null);
        await PowerCmd.Apply<DexterityPower>(choiceContext, Owner, 1m, Owner, null);
        await PowerCmd.Decrement(this);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "充能护服"),
        ("description", "在你的回合开始时，获得[gold]力量[/gold]与[gold]敏捷[/gold]。"),
        ("smartDescription", "在你的下[blue]{Amount}[/blue]个回合开始时，获得1点[gold]力量[/gold]与1点[gold]敏捷[/gold]。"),
    };
}
