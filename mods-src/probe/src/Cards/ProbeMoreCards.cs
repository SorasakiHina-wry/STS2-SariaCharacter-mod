using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using Probe.Pools;

namespace Probe.Cards;

// Reward generation rolls a rarity and then walks upward to the next rarity that actually has a
// card in this pool (CardFactory.GetNextAllowedRarity). With only Basic starter cards plus one
// Uncommon - and that Uncommon blacklisted once it leaves the reward pool - the roll lands on
// CardRarity.None and CardFactory throws, so the rewards screen never appears.
// These cards give the pool a full Common/Uncommon/Rare spread so rewards always resolve.

[Pool(typeof(ProbeCardPool))]
public sealed class ProbeJab : CustomCardModel
{
    public ProbeJab()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(7m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);

    public override string? CustomPortraitPath => "res://images/packed/card_portraits/ironclad/strike_ironclad.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "Probe Jab"),
        ("description", "Deal {Damage:diff()} damage."),
    };
}

[Pool(typeof(ProbeCardPool))]
public sealed class ProbeWard : CustomCardModel
{
    public ProbeWard()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(6m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);

    public override string? CustomPortraitPath => "res://images/packed/card_portraits/ironclad/defend_ironclad.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "Probe Ward"),
        ("description", "Gain {Block:diff()} [gold]Block[/gold]."),
    };
}

[Pool(typeof(ProbeCardPool))]
public sealed class ProbeSpark : CustomCardModel
{
    public ProbeSpark()
        : base(0, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(3m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);

    public override string? CustomPortraitPath => "res://images/packed/card_portraits/ironclad/strike_ironclad.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "Probe Spark"),
        ("description", "Deal {Damage:diff()} damage."),
    };
}

[Pool(typeof(ProbeCardPool))]
public sealed class ProbeSurge : CustomCardModel
{
    public ProbeSurge()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(9m, ValueProp.Move),
        new PowerVar<VulnerablePower>(2m),
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);
        await PowerCmd.Apply<VulnerablePower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars.Vulnerable.BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars.Vulnerable.UpgradeValueBy(1m);
    }

    public override string? CustomPortraitPath => "res://images/packed/card_portraits/ironclad/bash.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "Probe Surge"),
        ("description", "Deal {Damage:diff()} damage.\nApply {VulnerablePower:diff()} [gold]Vulnerable[/gold]."),
    };
}

[Pool(typeof(ProbeCardPool))]
public sealed class ProbeOverload : CustomCardModel
{
    public ProbeOverload()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(14m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_heavy_blunt")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);

    public override string? CustomPortraitPath => "res://images/packed/card_portraits/ironclad/bludgeon.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "Probe Overload"),
        ("description", "Deal {Damage:diff()} damage."),
    };
}

// The merchant always builds five character-card slots with the fixed types
// [Attack, Attack, Skill, Skill, Power] (MerchantInventory._coloredCardTypes) and dedupes within
// the shop, so the pool needs at least two non-Basic cards of each type or the shop throws.
// These fill out the Skill slots and the single Power slot.

[Pool(typeof(ProbeCardPool))]
public sealed class ProbeCalibrate : CustomCardModel
{
    public ProbeCalibrate()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(4m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(3m);

    public override string? CustomPortraitPath => "res://images/packed/card_portraits/ironclad/defend_ironclad.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "Probe Calibrate"),
        ("description", "Gain {Block:diff()} [gold]Block[/gold]."),
    };
}

[Pool(typeof(ProbeCardPool))]
public sealed class ProbeFocus : CustomCardModel
{
    public ProbeFocus()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(10m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(4m);

    public override string? CustomPortraitPath => "res://images/packed/card_portraits/ironclad/defend_ironclad.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "Probe Focus"),
        ("description", "Gain {Block:diff()} [gold]Block[/gold]."),
    };
}

[Pool(typeof(ProbeCardPool))]
public sealed class ProbeBastion : CustomCardModel
{
    public ProbeBastion()
        : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    public override bool GainsBlock => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(18m, ValueProp.Move),
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade() => DynamicVars.Block.UpgradeValueBy(6m);

    public override string? CustomPortraitPath => "res://images/packed/card_portraits/ironclad/defend_ironclad.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "Probe Bastion"),
        ("description", "Gain {Block:diff()} [gold]Block[/gold]."),
    };
}

[Pool(typeof(ProbeCardPool))]
public sealed class ProbeCharge : CustomCardModel
{
    public ProbeCharge()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<StrengthPower>(2m),
    };

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<StrengthPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["StrengthPower"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() => DynamicVars["StrengthPower"].UpgradeValueBy(1m);

    public override string? CustomPortraitPath => "res://images/packed/card_portraits/ironclad/inflame.png";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "Probe Charge"),
        ("description", "Gain {StrengthPower:diff()} [gold]Strength[/gold]."),
    };
}
