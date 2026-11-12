using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Probe.Powers;

/// <summary>限位 — 下一次获得格挡时整段变 0，然后层数 -1。</summary>
public sealed class ProbeLimitPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Debuff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    /// <summary>
    /// 乘法阶段返回 0 就能把这一整段格挡清零。预览（cardSource 有、cardPlay 为空）不扣层，
    /// 否则悬停看牌就会把层数消耗掉。
    /// </summary>
    public override decimal ModifyBlockMultiplicative(
        Creature target, decimal block, ValueProp props, CardModel? cardSource, CardPlay? cardPlay)
    {
        if (target != Owner || block <= 0 || Amount <= 0)
            return 1m;

        if (cardSource != null && cardPlay == null)
            return 1m;   // 预览

        SetAmount(Amount - 1, silent: true);
        Flash();
        return 0m;
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "限位"),
        ("description", "下{Amount}次获得[gold]格挡[/gold]时将被阻止。"),
    };
}

/// <summary>迅捷 — 回合开始时失去{Amount}层钙铠，从抽牌堆随机 3×Amount 张里选 Amount 张加入手牌。</summary>
public sealed class ProbeSwiftPower : CustomPowerModel
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

        int take = (int)Amount;

        // 失去 take 层钙铠（不够就扣到 0；为 0 也照样抽牌）
        var armour = Owner.GetPower<CalciumArmorPower>();
        if (armour != null && armour.Amount > 0)
        {
            int loss = Math.Min(take, armour.Amount);
            if (loss > 0)
                await PowerCmd.ModifyAmount(choiceContext, armour, -loss, Owner, null);
        }

        var drawPile = PileType.Draw.GetPile(player);
        if (drawPile.Cards.Count == 0)
            return;

        var options = drawPile.Cards.ToList()
            .StableShuffle(player.RunState.Rng.CombatCardSelection)
            .Take(take * 3)
            .ToList();
        if (options.Count == 0)
            return;

        var picks = await CardSelectCmd.FromCombatPile(
            choiceContext,
            drawPile,
            player,
            new CardSelectorPrefs(SelectionScreenPrompt, Math.Min(take, options.Count)),
            (CardModel card) => options.Contains(card));

        foreach (var card in picks)
        {
            if (card != null)
                await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "迅捷"),
        ("selectionScreenPrompt", "从抽牌堆中选择一张牌加入你的手牌。"),
        ("description", "在你的回合开始时，失去1层[gold]钙铠[/gold]，从[gold]抽牌堆[/gold]的随机3张牌中选择1张加入你的[gold]手牌[/gold]。"),
        ("smartDescription", "在你的回合开始时，失去[blue]{Amount}[/blue]层[gold]钙铠[/gold]，从[gold]抽牌堆[/gold]的随机[blue]{Amount}[/blue]×3张牌中选择[blue]{Amount}[/blue]张加入你的[gold]手牌[/gold]。"),
    };
}

/// <summary>覆还 — 本回合每失去 1 层钙铠，就把等量钙铠记到下回合。</summary>
public sealed class ProbeReturnPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    /// <summary>只持续本回合：下一次自己的回合开始时移除（下回合钙铠在正常阶段结算，早于它即可）。</summary>
    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        await PowerCmd.Remove(this);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "覆还"),
        ("description", "本回合，你每失去1层[gold]钙铠[/gold]，就在下回合开始时获得1层[gold]钙铠[/gold]。"),
        ("smartDescription", "本回合，你每失去1层[gold]钙铠[/gold]，就在下回合开始时获得[blue]{Amount}[/blue]层[gold]钙铠[/gold]。"),
    };
}
