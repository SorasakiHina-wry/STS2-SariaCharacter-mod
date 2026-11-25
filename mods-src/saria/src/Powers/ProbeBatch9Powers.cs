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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Saria.Cards;

namespace Saria.Powers;

/// <summary>
/// 熟能生巧 — 每累计获得 7 层钙铠抽 Amount 张牌。
/// ArmorNeed 是「还差几层」，获得钙铠时扣减；扣到 ≤0 就补 7 再抽一次（可连续触发）。
/// </summary>
public sealed class ProbePracticePower : CustomPowerModel
{
    private const int THRESHOLD = 7;

    private int _armorNeed = THRESHOLD;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("ArmorNeed", THRESHOLD),
    };

    private void RefreshVars()
    {
        if (DynamicVars == null)
            return;
        if (DynamicVars["ArmorNeed"] is DynamicVar var)
        {
            var.BaseValue = _armorNeed;
            var.PreviewValue = _armorNeed;
        }
    }

    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power == this)
            RefreshVars();
        return Task.CompletedTask;
    }

    /// <summary>钙铠增加时推进进度；返回任务以便游戏按顺序等待抽牌。</summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;
        RefreshVars();
    }

    public async Task OnArmourGained(PlayerChoiceContext choiceContext, int gained)
    {
        if (Amount <= 0 || gained <= 0)
            return;

        _armorNeed -= gained;
        while (_armorNeed <= 0)
        {
            _armorNeed += THRESHOLD;
            Flash();
            await CardPileCmd.Draw(choiceContext, Amount, Owner.Player);
        }

        RefreshVars();
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "熟能生巧"),
        ("description", "你再获得7层[gold]钙铠[/gold]，就抽1张牌。"),
        ("smartDescription", "你再获得[blue]{ArmorNeed}[/blue]层[gold]钙铠[/gold]，就抽[blue]{Amount}[/blue]张牌。"),
    };
}

/// <summary>全身武装 — 每次抽牌前（即回合开始）往手里塞 Amount 张破阵。</summary>
public sealed class ProbeFullArmamentPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    public override async Task BeforeHandDraw(Player player, PlayerChoiceContext choiceContext, ICombatState combatState)
    {
        if (player != Owner.Player || Amount <= 0)
            return;

        Flash();

        var cards = new List<CardModel>();
        for (int i = 0; i < (int)Amount; i++)
            cards.Add(combatState.CreateCard<ProbeBreach>(player));

        await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand, player);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "全身武装"),
        ("description", "在你的回合开始时，在你的[gold]手牌[/gold]中加入1张[gold]破阵[/gold]。"),
        ("smartDescription", "在你的回合开始时，在你的[gold]手牌[/gold]中加入[blue]{Amount}[/blue]张[gold]破阵[/gold]。"),
    };
}

/// <summary>
/// 飞龙形态 — 回合开始时获得 Amount 层钙铠。
/// 走「正常」回合开始阶段，所以晚于钙铠自己的格挡结算：当回合只涨层数，格挡从下回合开始。
/// </summary>
public sealed class ProbeDragonFormPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    /// <summary>回合**结束**时结算（原版没有 AfterPlayerTurnEnd 钩子，玩家结束回合 = 玩家侧回合结束）。</summary>
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, System.Collections.Generic.IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player || Amount <= 0)
            return;

        Flash();
        await PowerCmd.Apply<CalciumArmorPower>(choiceContext, Owner, Amount, Owner, null);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "飞龙形态"),
        ("description", "在你的回合结束时，获得[gold]钙铠[/gold]。"),
        ("smartDescription", "在你的回合结束时，获得[blue]{Amount}[/blue]层[gold]钙铠[/gold]。"),
    };
}