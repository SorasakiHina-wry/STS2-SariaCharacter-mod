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
using MegaCrit.Sts2.Core.ValueProps;

namespace Probe.Powers;

// 2026-11-08 batch 2. Vanilla pattern: one power per mechanic, Amount on the icon,
// extra numbers as named DynamicVars (docs 9.3 / 9.4).

/// <summary>持械 — Amount = 本回合通过持械获得的活力；回合结束时收回至多这么多。</summary>
public sealed class ProbeArmPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

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

        var vigor = Owner.GetPower<VigorPower>();
        if (vigor != null && vigor.Amount > 0)
        {
            int loss = Math.Min(Amount, vigor.Amount);   // 绝不扣到小于 0
            if (loss > 0)
                await PowerCmd.ModifyAmount(choiceContext, vigor, -loss, Owner, null);
        }

        await PowerCmd.Remove(this);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "持械"),
        ("description", "在本回合结束前，获得[gold]活力[/gold]。"),
        ("smartDescription", "在本回合结束前，获得[blue]{Amount}[/blue]点[gold]活力[/gold]。"),
    };
}

/// <summary>腿甲 — Amount = 剩余回合数；每回合开始抽 1 张。</summary>
public sealed class ProbeLegArmorPower : CustomPowerModel
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
        await CardPileCmd.Draw(choiceContext, 1m, player);
        await PowerCmd.Decrement(this);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "腿甲"),
        ("description", "在你的回合开始时，抽1张牌。"),
        ("smartDescription", "在你的下[blue]{Amount}[/blue]个回合开始时，抽1张牌。"),
    };
}

/// <summary>骨矿化 — Amount = 每次「从卡牌获得格挡」后补的钙铠层数。结算由 CalciumArmorPower 触发。</summary>
public sealed class ProbeBoneMineralizePower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "骨矿化"),
        ("description", "当你从卡牌中获得[gold]格挡[/gold]时，获得[gold]钙铠[/gold]。"),
        ("smartDescription", "当你从卡牌中获得[gold]格挡[/gold]时，获得[blue]{Amount}[/blue]层[gold]钙铠[/gold]。"),
    };
}

/// <summary>技艺超载 — Amount = 下个回合开始时要失去的钙铠层数（在钙铠给出格挡之后结算）。</summary>
public sealed class ProbeTechniqueOverloadPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
            return;

        var armour = Owner.GetPower<CalciumArmorPower>();
        if (armour != null && armour.Amount > 0)
        {
            int loss = Math.Min(Amount, armour.Amount);
            if (loss > 0)
                await PowerCmd.ModifyAmount(choiceContext, armour, -loss, Owner, null);
        }

        await PowerCmd.Remove(this);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "技艺超载"),
        ("description", "在你的下个回合开始时，失去[gold]钙铠[/gold]。"),
        ("smartDescription", "在你的下个回合开始时，失去[blue]{Amount}[/blue]层[gold]钙铠[/gold]。"),
    };
}

/// <summary>精神回复 — Amount = 还差多少层钙铠触发下一份能量（图标显示）。</summary>
public sealed class ProbeSpiritRecoveryPower : CustomPowerModel
{
    /// <summary>每 18 层钙铠换 1 点能量（与 design/powers.md 一致）。</summary>
    public const int Threshold = 18;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is not CalciumArmorPower || power.Owner != Owner || amount <= 0 || Owner.Player == null)
            return;

        int remaining = Amount - (int)amount;
        int gained = 0;
        while (remaining <= 0)
        {
            remaining += Threshold;
            gained++;
        }

        if (gained > 0)
        {
            Flash();
            for (int i = 0; i < gained; i++)
                await PlayerCmd.GainEnergy(1m, Owner.Player);
        }

        SetAmount(remaining);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "精神回复"),
        ("description", "你每获得18层钙铠时，获得{energyPrefix:energyIcons(1)}。"),
        ("smartDescription", "你每获得[blue]18[/blue]层[gold]钙铠[/gold]，获得{energyPrefix:energyIcons(1)}。还需[blue]{Amount}[/blue]层。"),
    };
}

/// <summary>“科技新星” — Amount = 额外获得的力量；排除所有同名 power 造成的获得，避免递归。</summary>
public sealed class ProbeTechNovaPower : CustomPowerModel
{
    /// <summary>大于 0 表示正在由「科技新星」本身发放力量，所有同名实例都不再触发。</summary>
    private static int _granting;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is not StrengthPower || power.Owner != Owner || amount <= 0 || Amount <= 0)
            return;

        if (_granting > 0)
            return; // 同名 power 发放的力量不再触发

        _granting++;
        try
        {
            Flash();
            await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, Amount, Owner, null);
        }
        finally
        {
            _granting--;
        }
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "“科技新星”"),
        ("description", "每当你以此卡以外的方式获得[gold]力量[/gold]时，额外获得[gold]力量[/gold]。"),
        ("smartDescription", "每当你以此卡以外的方式获得[gold]力量[/gold]时，额外获得[blue]{Amount}[/blue]点[gold]力量[/gold]。"),
    };
}

/// <summary>紧密防护 — Amount = 剩余回合数；存在期间钙铠不会因卡牌格挡而衰减。</summary>
public sealed class ProbeTightGuardPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
            return;

        await PowerCmd.Decrement(this);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "紧密防护"),
        ("description", "在你的下几个回合开始前，你的[gold]钙铠[/gold]不会因为从卡牌中获得[gold]格挡[/gold]而失去。"),
        ("smartDescription", "在你的下[blue]{Amount}[/blue]个回合开始前，你的[gold]钙铠[/gold]不会因为从卡牌中获得[gold]格挡[/gold]而失去。"),
    };
}
