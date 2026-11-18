using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Saria.Pools;
using Saria.Powers;

namespace Saria.Cards;

// Small shared helpers. Values are literals inside each card class rather than constructor
// parameters: CanonicalVars must never depend on subclass state that might not be assigned yet.
internal static class ProbeCardKit
{
    internal const string StrikePortrait = "res://images/packed/card_portraits/ironclad/strike_ironclad.png";
    internal const string DefendPortrait = "res://images/packed/card_portraits/ironclad/defend_ironclad.png";
    internal const string BashPortrait = "res://images/packed/card_portraits/ironclad/bash.png";
    internal const string BludgeonPortrait = "res://images/packed/card_portraits/ironclad/bludgeon.png";
    internal const string InflamePortrait = "res://images/packed/card_portraits/ironclad/inflame.png";

    internal static async Task Hit(PlayerChoiceContext ctx, CardPlay play, CardModel self, string fx)
    {
        ArgumentNullException.ThrowIfNull(play.Target, "cardPlay.Target");
        await DamageCmd.Attack(self.DynamicVars.Damage.BaseValue)
            .FromCard(self, play)
            .Targeting(play.Target)
            .WithHitFx(fx)
            .Execute(ctx);
    }

    internal static async Task Guard(PlayerChoiceContext ctx, CardPlay play, CardModel self)
    {
        await CreatureCmd.GainBlock(self.Owner.Creature, self.DynamicVars.Block, play);
    }

    /// <summary>失去最多 want 层钙铠（不足时按剩余层数），返回实际失去的层数。</summary>
    internal static async Task<int> SpendArmor(PlayerChoiceContext ctx, CardModel self, int want)
    {
        var armour = self.Owner.Creature.GetPower<CalciumArmorPower>();
        if (armour == null || armour.Amount <= 0 || want <= 0)
            return 0;

        int loss = Math.Min(want, armour.Amount);
        if (loss > 0)
            await PowerCmd.ModifyAmount(ctx, armour, -loss, self.Owner.Creature, self);

        return loss;
    }

    internal static List<(string, string)> DamageLoc(string title) => new()
    {
        ("title", title),
        ("description", "造成{Damage:diff()}点伤害。"),
    };

    internal static List<(string, string)> BlockLoc(string title) => new()
    {
        ("title", title),
        ("description", "获得{Block:diff()}点[gold]格挡[/gold]。"),
    };
}
