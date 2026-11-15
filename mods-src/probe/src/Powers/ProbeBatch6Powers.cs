using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace Probe.Powers;

// 2026-11-09 batch.

/// <summary>下回合钙铠 — Amount = 下个回合开始时补的层数。用正常阶段，避免与钙铠的 Early 结算叠加。</summary>
public sealed class ProbeNextTurnArmorPower : CustomPowerModel
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
        await PowerCmd.Apply<CalciumArmorPower>(choiceContext, Owner, Amount, Owner, null);
        await PowerCmd.Remove(this);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "下回合钙铠"),
        ("description", "在你的下个回合开始时，获得[gold]钙铠[/gold]。"),
        ("smartDescription", "在你的下个回合开始时，获得[blue]{Amount}[/blue]层[gold]钙铠[/gold]。"),
    };
}

/// <summary>拓印 — Amount = 复制张数；本回合打出的下一张攻击牌复制到手牌。</summary>
public sealed class ProbeImprintPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    public override async Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner.Player || cardPlay.Card.Type != CardType.Attack)
            return;

        Flash();
        for (int i = 0; i < Amount; i++)
        {
            var copy = cardPlay.Card.CreateClone();
            await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner.Player);
        }

        await PowerCmd.Remove(this);
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
        ("title", "拓印"),
        ("description", "将你在本回合打出的下一张攻击牌的复制品加入到你的手牌。"),
        ("smartDescription", "将你在本回合打出的下一张攻击牌的[blue]{Amount}[/blue]张复制品加入到你的手牌。"),
    };
}

/// <summary>紊乱 — 挂在敌人身上；生命降到最大生命 30% 及以下或死亡时，给施加者钙铠。</summary>
public sealed class ProbeDisorderPower : CustomPowerModel
{
    private sealed class Data
    {
        public bool done;
    }

    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>每个施加者一个独立实例：多人联机时敌方 buff 栏分开显示。</summary>
    public override PowerInstanceType InstanceType => PowerInstanceType.InstancedPerApplier;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    protected override object? InitInternalData() => new Data();

    /// <summary>被挂上时就先检测一次：若目标此刻已在 30% 以下，立即触发。</summary>
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power != this || amount <= 0)
            return;

        if (Owner.CurrentHp > Owner.MaxHp * 0.3m)
            return;

        await Trigger();
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner || delta >= 0)
            return;

        if (Owner.CurrentHp > Owner.MaxHp * 0.3m)
            return;

        await Trigger();
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext, Creature creature, CardModel? source, bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (creature != Owner)
            return;

        await Trigger();
    }

    private async Task Trigger()
    {
        var data = GetInternalData<Data>();
        if (data.done)
            return;
        data.done = true;

        if (Applier != null)
        {
            Flash();
            await PowerCmd.Apply<CalciumArmorPower>(
                new ThrowingPlayerChoiceContext(), Applier, Amount, Applier, null);
        }

        await PowerCmd.Remove(this);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "紊乱"),
        ("description", "若生命值小于等于最大生命值的30%，使给予者获得[gold]钙铠[/gold]。"),
        ("smartDescription", "若生命值小于等于最大生命值的30%，使施加者获得[blue]{Amount}[/blue]层[gold]钙铠[/gold]。"),
    };
}

/// <summary>精密操作 — 本回合下一次获得钙铠时给能量。</summary>
public sealed class ProbePrecisionPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is not CalciumArmorPower || power.Owner != Owner || amount <= 0 || Owner.Player == null)
            return;

        Flash();
        await PlayerCmd.GainEnergy(Amount, Owner.Player);
        await PowerCmd.Remove(this);
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
        ("title", "精密操作"),
        ("description", "在本回合，下一次获得[gold]钙铠[/gold]时获得[gold]能量[/gold]。"),
        ("smartDescription", "在本回合，下一次获得[gold]钙铠[/gold]时获得{Amount:energyIcons()}。"),
    };
}

/// <summary>创始人 — 每次造成 ≥22 点伤害获得钙铠（按最终伤害计算，无视格挡）。</summary>
public sealed class ProbeFounderPower : CustomPowerModel
{
    private const int THRESHOLD = 22;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props,
        Creature target, CardModel? cardSource)
    {
        if (dealer != Owner || result.TotalDamage < THRESHOLD)
            return;

        Flash();
        await PowerCmd.Apply<CalciumArmorPower>(choiceContext, Owner, Amount, Owner, cardSource);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "创始人"),
        ("description", "每当你造成大于等于22点的伤害时，获得[gold]钙铠[/gold]。"),
        ("smartDescription", "每当你造成大于等于22点的伤害时，获得[blue]{Amount}[/blue]层[gold]钙铠[/gold]。"),
    };
}

/// <summary>
/// 止血 — Amount = 剩余回合；每回合前 2 次「受到伤害」照常结算，之后完全阻止。
///
/// 计数口径（用户确认）：**任何伤害都算一次，哪怕被格挡完全挡住**；只排除无法被格挡的伤害
/// （ValueProp.Unblockable，例如中毒那种「失去生命」）。
/// 计数 = ProbeHemostasisTickPatch（Hook.BeforeDamageReceived，每段真实伤害一次，早于格挡）；
/// 阻止 = ModifyDamageCap 返回 0。
/// </summary>
public sealed class ProbeHemostasisPower : CustomPowerModel
{
    private const int ALLOWED_HITS = 2;

    private sealed class Data
    {
        public int remaining = ALLOWED_HITS;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    protected override object? InitInternalData() => new Data();

    /// <summary>
    /// 额度用尽后把整段伤害压成 0（连格挡都不用消耗）。只在真正的伤害结算阶段读这个值，
    /// 所以不会影响预览。
    /// </summary>
    public override decimal ModifyDamageCap(
        Creature? target, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != Owner)
            return decimal.MaxValue;
        return GetInternalData<Data>().remaining <= 0 ? 0m : decimal.MaxValue;
    }

    /// <summary>每次受到伤害消耗一次额度（不论是否被格挡）。由 <c>ProbeHemostasisTickPatch</c> 调用。</summary>
    public bool ConsumeHit()
    {
        var data = GetInternalData<Data>();
        if (data.remaining > 0)
        {
            data.remaining--;
            Flash();
            return true;
        }
        return false;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        GetInternalData<Data>().remaining = ALLOWED_HITS;   // 每回合重置

        if (Amount <= 1)
        {
            await PowerCmd.Remove(this);
            return;
        }

        await PowerCmd.Decrement(this);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "止血"),
        ("description", "在你的下几个回合开始前，每回合至多受到2次伤害。"),
        ("smartDescription", "在你的下[blue]{Amount}[/blue]个回合开始前，每回合至多受到2次伤害。"),
    };
}
