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
using Probe.Pools;

namespace Probe.Cards;

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

    internal static List<(string, string)> DamageLoc(string title) => new()
    {
        ("title", title),
        ("description", "造成 {Damage:diff()} 点伤害"),
    };

    internal static List<(string, string)> BlockLoc(string title) => new()
    {
        ("title", title),
        ("description", "获得 {Block:diff()} 点格挡"),
    };
}

[Pool(typeof(ProbeCardPool))]
public sealed class ProbeLance : CustomCardModel
{
    public ProbeLance() : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DamageVar(10m, ValueProp.Move) };
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => ProbeCardKit.Hit(c, p, this, "vfx/vfx_attack_slash");
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
    public override string? CustomPortraitPath => ProbeCardKit.StrikePortrait;
    public override List<(string, string)>? Localization => ProbeCardKit.DamageLoc("Probe Lance");
}

[Pool(typeof(ProbeCardPool))]
public sealed class ProbeCircuit : CustomCardModel
{
    public ProbeCircuit() : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self) { }
    public override bool GainsBlock => true;
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new BlockVar(12m, ValueProp.Move) };
    protected override Task OnPlay(PlayerChoiceContext c, CardPlay p) => ProbeCardKit.Guard(c, p, this);
    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(5m);
    public override string? CustomPortraitPath => ProbeCardKit.DefendPortrait;
    public override List<(string, string)>? Localization => ProbeCardKit.BlockLoc("Probe Circuit");
}

[Pool(typeof(ProbeCardPool))]
public sealed class ProbeNova : CustomCardModel
{
    public ProbeNova() : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(11m, ValueProp.Move),
        new PowerVar<VulnerablePower>(2m),
    };
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await ProbeCardKit.Hit(c, p, this, "vfx/vfx_attack_blunt");
        await PowerCmd.Apply<VulnerablePower>(c, p.Target, DynamicVars.Vulnerable.BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Vulnerable.UpgradeValueBy(1m);
    }
    public override string? CustomPortraitPath => ProbeCardKit.BashPortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "Probe Nova"),
        ("description", "造成 {Damage:diff()} 点伤害，施加 {VulnerablePower:diff()} 层易伤"),
    };
}

[Pool(typeof(ProbeCardPool))]
public sealed class ProbeResonance : CustomCardModel
{
    public ProbeResonance() : base(2, CardType.Power, CardRarity.Rare, TargetType.Self) { }
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new PowerVar<StrengthPower>(4m) };
    protected override async Task OnPlay(PlayerChoiceContext c, CardPlay p)
    {
        await PowerCmd.Apply<StrengthPower>(c, Owner.Creature, DynamicVars["StrengthPower"].BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars["StrengthPower"].UpgradeValueBy(2m);
    public override string? CustomPortraitPath => ProbeCardKit.InflamePortrait;
    public override List<(string, string)>? Localization => new()
    {
        ("title", "Probe Resonance"),
        ("description", "获得 {StrengthPower:diff()} 点力量"),
    };
}
