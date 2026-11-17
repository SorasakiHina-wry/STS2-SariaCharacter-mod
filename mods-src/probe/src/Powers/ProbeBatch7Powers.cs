using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Saria.Powers;

/// <summary>封装 — start of next turn: fetch a random card from the discard pile into hand.</summary>
public sealed class ProbeEncapsulatePower : CustomPowerModel
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
        var discard = PileType.Discard.GetPile(player);
        int want = Math.Min((int)Amount, discard.Cards.Count);
        if (want > 0)
        {
            // 自选（不是随机）：照原版「全息影像」的写法弹选牌界面
            var selected = await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, want),
                context: choiceContext,
                pile: discard,
                player: player);

            foreach (var card in selected)
            {
                if (card != null)
                    await CardPileCmd.Add(card, PileType.Hand);
            }
        }

        await PowerCmd.Decrement(this);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "封装"),
        ("selectionScreenPrompt", "选择{Amount}张牌放入你的手牌。"),
        ("description", "下回合，将你[gold]弃牌堆[/gold]中的一张牌放入你的[gold]手牌[/gold]。"),
        ("smartDescription", "下回合，将你[gold]弃牌堆[/gold]中的[blue]{Amount}[/blue]张牌放入你的[gold]手牌[/gold]。"),
    };
}

/// <summary>极限功率 — turn start (late pass): block is converted into armour after the normal payout.</summary>
public sealed class ProbeOverclockPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    /// <summary>
    /// Late pass: 钙铠 has already paid out its block (Early pass), so the block we take here is the
    /// full amount and the fresh armour will not be cashed in until next turn.
    /// </summary>
    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player || Amount <= 0)
            return;

        decimal block = Owner.Block;
        if (block <= 0)
            return;

        Flash();
        await CreatureCmd.LoseBlock(choiceContext, Owner, block, null);
        await PowerCmd.Apply<CalciumArmorPower>(choiceContext, Owner, block, Owner, null);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "极限功率"),
        ("description", "在你的回合开始时，失去所有[gold]格挡[/gold]，并获得相同数值的[gold]钙铠[/gold]。"),
    };
}

/// <summary>共振 — every armour stack gained this turn deals damage to the chosen target once.</summary>
public sealed class ProbeResonancePower : CustomPowerModel
{
    public const decimal DamagePerStack = 4m;

    /// <summary>The enemy the card was played on; armour gained during this turn keeps hitting it.</summary>
    public Creature? Target { get; set; }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    /// <summary>Only lasts for the current turn: removed at the start of your next turn.</summary>
    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return;

        await PowerCmd.Remove(this);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "共振"),
        ("description", "本回合你每获得1层[gold]钙铠[/gold]，就对目标造成4点伤害。"),
        ("smartDescription", "本回合你每获得1层[gold]钙铠[/gold]，就对目标造成[blue]{Amount}[/blue]×4点伤害。"),
    };
}

/// <summary>防卫经验 — tracks armour lost this turn; the first 8 stacks draw cards.</summary>
public sealed class ProbeDefenseExperiencePower : CustomPowerModel
{
    /// <summary>每回合累计失去这么多层钙铠就触发一次。</summary>
    public const int Threshold = 5;

    /// <summary>本回合到目前为止失去的层数。</summary>
    private int _accumulated;

    private bool _triggeredThisTurn;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override string? CustomPackedIconPath => "res://images/atlases/power_atlas.sprites/thorns_power.tres";
    public override string? CustomBigIconPath => "res://images/powers/thorns_power.png";

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player)
            return Task.CompletedTask;

        _triggeredThisTurn = false;
        _accumulated = 0;
        return Task.CompletedTask;
    }

    /// <summary>Called by <see cref="CalciumArmorPower"/> whenever stacks are lost.</summary>
    public async Task OnArmourLost(PlayerChoiceContext choiceContext, int lost)
    {
        if (_triggeredThisTurn || lost <= 0 || Amount <= 0)
            return;

        _accumulated += lost;
        if (_accumulated < Threshold)
            return;

        _triggeredThisTurn = true;
        Flash();
        await CardPileCmd.Draw(choiceContext, Amount, Owner.Player);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "防卫经验"),
        ("description", "本回合首次失去5层[gold]钙铠[/gold]后，抽{Amount}张牌。"),
    };
}
