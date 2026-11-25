using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Saria.Pools;
using Saria.Powers;

namespace Saria.Relics;

// 遗物图标暂时借原版图集（IconBaseName），等自制图到位再逐个替换。

/// <summary>调节仪 — 每场战斗第一次钙铠超过 12 层时获得 2 点力量。</summary>
[Pool(typeof(ProbeRelicPool))]
public sealed class CalibrationDevice : CustomRelicModel
{
    private const int Threshold = 12;

    private bool _triggered;

    public override RelicRarity Rarity => RelicRarity.Common;

    protected override string IconBaseName => "vajra";

    public override Task BeforeCombatStart()
    {
        _triggered = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (_triggered || power is not CalciumArmorPower || power.Owner != Owner.Creature)
            return;
        if (power.Amount <= Threshold)
            return;

        _triggered = true;
        Flash();
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, 2m, Owner.Creature, null);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "调节仪"),
        ("description", "每场战斗中首次[gold]钙铠[/gold]大于[blue]12[/blue]层时，获得[blue]2[/blue]点[gold]力量[/gold]。"),
    };
}

/// <summary>富集仓 — 战斗前 3 个回合，每回合开始获得 2 层钙铠。</summary>
[Pool(typeof(ProbeRelicPool))]
public sealed class EnrichmentVat : CustomRelicModel
{
    private const int Turns = 3;

    private int _turn;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override string IconBaseName => "anchor";

    public override Task BeforeCombatStart()
    {
        _turn = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner)
            return;

        _turn++;
        if (_turn > Turns)
            return;

        Flash();
        await PowerCmd.Apply<CalciumArmorPower>(choiceContext, Owner.Creature, 2m, Owner.Creature, null);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "富集仓"),
        ("description", "在战斗开始时的前[blue]3[/blue]个回合，你获得[blue]2[/blue]层[gold]钙铠[/gold]。"),
    };
}

/// <summary>工牌 — 每场战斗第一次钙铠归零时获得 2 点能量。</summary>
[Pool(typeof(ProbeRelicPool))]
public sealed class WorkBadge : CustomRelicModel
{
    private bool _triggered;

    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override string IconBaseName => "lantern";

    public override Task BeforeCombatStart()
    {
        _triggered = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (_triggered || power is not CalciumArmorPower || power.Owner != Owner.Creature)
            return;
        if (amount >= 0 || power.Amount > 0)
            return;

        _triggered = true;
        Flash();
        await PlayerCmd.GainEnergy(2m, Owner);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "工牌"),
        ("description", "你在每场战斗中第一次失去所有[gold]钙铠[/gold]时，获得[blue]2[/blue]点[gold]能量[/gold]。"),
    };
}

/// <summary>共振回路 — 每次获得钙铠时对所有敌人造成 5 点伤害。</summary>
[Pool(typeof(ProbeRelicPool))]
public sealed class ResonanceCircuit : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override string IconBaseName => "bag_of_marbles";

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is not CalciumArmorPower || power.Owner != Owner.Creature || amount <= 0)
            return;

        var combatState = Owner.Creature.CombatState;
        if (combatState == null)
            return;

        Flash();
        foreach (var enemy in combatState.HittableEnemies.ToList())
            await CreatureCmd.Damage(choiceContext, enemy, 5m, ValueProp.Unpowered, Owner.Creature);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "共振回路"),
        ("description", "每当你获得[gold]钙铠[/gold]时，对所有敌人造成[blue]5[/blue]点伤害。"),
    };
}

/// <summary>绑手绷带 — 每场战斗的第一回合，钙铠不会因为获得格挡而衰减。</summary>
[Pool(typeof(ProbeRelicPool))]
public sealed class WristBandage : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override string IconBaseName => "burning_blood";

    public override async Task BeforeCombatStart()
    {
        // 紧密防护在回合开始时 -1，所以给 2 层正好覆盖第 1 回合
        await PowerCmd.Apply<ProbeTightGuardPower>(
            new ThrowingPlayerChoiceContext(), Owner.Creature, 2m, Owner.Creature, null);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "绑手绷带"),
        ("description", "在每场战斗的第一回合，不会因为从卡牌中获得[gold]格挡[/gold]而失去[gold]钙铠[/gold]。"),
    };
}

/// <summary>源石样本 — 格挡被击破时获得 3 层钙铠（触发条件同原版「手钻」）。</summary>
[Pool(typeof(ProbeRelicPool))]
public sealed class OriginiumSample : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override string IconBaseName => "vajra";

    public override async Task AfterBlockBroken(PlayerChoiceContext choiceContext, Creature target, Creature? breaker)
    {
        if (target != Owner.Creature)
            return;

        Flash();
        await PowerCmd.Apply<CalciumArmorPower>(choiceContext, Owner.Creature, 3m, Owner.Creature, null);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "源石样本"),
        ("description", "每当你的[gold]格挡[/gold]被突破时，获得[blue]3[/blue]层[gold]钙铠[/gold]。"),
    };
}

/// <summary>
/// 钙质操纵 — 先古遗物：战斗开始 8 层钙铠；每失去钙铠，对随机敌人造成等量伤害。
/// </summary>
[Pool(typeof(ProbeRelicPool))]
public sealed class CalciumManipulation : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    protected override string IconBaseName => "burning_blood";

    public override async Task BeforeCombatStart()
    {
        await PowerCmd.Apply<CalciumArmorPower>(
            new ThrowingPlayerChoiceContext(), Owner.Creature, 8m, Owner.Creature, null);
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power is not CalciumArmorPower || power.Owner != Owner.Creature || amount >= 0)
            return;

        var combatState = Owner.Creature.CombatState;
        if (combatState == null)
            return;

        var enemies = combatState.HittableEnemies.ToList();
        if (enemies.Count == 0)
            return;

        var target = Owner.RunState.Rng.CombatTargets.NextItem(enemies);
        if (target == null)
            return;

        Flash();
        await CreatureCmd.Damage(choiceContext, target, -amount, ValueProp.Unpowered, Owner.Creature);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "钙质操纵"),
        ("description", "战斗开始时，获得[blue]8[/blue]层[gold]钙铠[/gold]。失去[gold]钙铠[/gold]时对随机敌人造成等量于失去[gold]钙铠[/gold]层数的伤害。"),
    };
}
