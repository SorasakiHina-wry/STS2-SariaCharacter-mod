using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using Probe.Pools;

namespace Probe.Relics;

// Relic icons reuse vanilla atlas entries via IconBaseName, so every relic is fully rendered
// before any custom art exists - swap IconBaseName for a custom atlas entry later.

/// <summary>Starter relic: replaces the borrowed Burning Blood.</summary>
[Pool(typeof(ProbeRelicPool))]
public sealed class ProbeCore : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override string IconBaseName => "burning_blood";

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new HealVar(6m),
    };

    public override async Task AfterCombatVictory(CombatRoom _)
    {
        if (Owner.Creature.IsDead)
            return;
        Flash();
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "Probe Core"),
        ("description", "At the end of combat, heal {Heal:diff()} HP."),
        ("flavor", "Still warm. Still listening."),
    };
}

[Pool(typeof(ProbeRelicPool))]
public sealed class AnchorChip : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override string IconBaseName => "anchor";

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(8m, ValueProp.Unpowered),
    };

    public override async Task BeforeCombatStart()
    {
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "Anchor Chip"),
        ("description", "At the start of each combat, gain {Block:diff()} [gold]Block[/gold]."),
        ("flavor", "It remembers the shape of the floor."),
    };
}

[Pool(typeof(ProbeRelicPool))]
public sealed class SmoothMemory : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Common;

    protected override string IconBaseName => "oddly_smooth_stone";

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new HealVar(5m),
    };

    public override async Task AfterCombatVictory(CombatRoom _)
    {
        if (Owner.Creature.IsDead)
            return;
        Flash();
        await CreatureCmd.Heal(Owner.Creature, DynamicVars.Heal.BaseValue);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "Smooth Memory"),
        ("description", "At the end of combat, heal {Heal:diff()} HP."),
        ("flavor", "Worn down by something else's hands."),
    };
}

[Pool(typeof(ProbeRelicPool))]
public sealed class SignalAmplifier : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override string IconBaseName => "vajra";

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<StrengthPower>(1m),
    };

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
            return;
        Flash();
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["StrengthPower"].BaseValue,
            Owner.Creature,
            null);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "Signal Amplifier"),
        ("description", "At the start of each combat, gain {StrengthPower:diff()} [gold]Strength[/gold]."),
        ("flavor", "Everything it hears, it makes louder."),
    };
}

[Pool(typeof(ProbeRelicPool))]
public sealed class StaticLantern : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override string IconBaseName => "lantern";

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<DexterityPower>(1m),
    };

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
            return;
        Flash();
        await PowerCmd.Apply<DexterityPower>(
            new ThrowingPlayerChoiceContext(),
            Owner.Creature,
            DynamicVars["DexterityPower"].BaseValue,
            Owner.Creature,
            null);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "Static Lantern"),
        ("description", "At the start of each combat, gain {DexterityPower:diff()} [gold]Dexterity[/gold]."),
        ("flavor", "The light does not move. The shadows do."),
    };
}

[Pool(typeof(ProbeRelicPool))]
public sealed class RecursionEngine : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override string IconBaseName => "bag_of_marbles";

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new PowerVar<StrengthPower>(2m),
        new PowerVar<DexterityPower>(2m),
    };

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is not CombatRoom)
            return;
        Flash();
        var ctx = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<StrengthPower>(ctx, Owner.Creature, DynamicVars["StrengthPower"].BaseValue, Owner.Creature, null);
        await PowerCmd.Apply<DexterityPower>(ctx, Owner.Creature, DynamicVars["DexterityPower"].BaseValue, Owner.Creature, null);
    }

    public override List<(string, string)>? Localization => new()
    {
        ("title", "Recursion Engine"),
        ("description", "At the start of each combat, gain {StrengthPower:diff()} [gold]Strength[/gold] and {DexterityPower:diff()} [gold]Dexterity[/gold]."),
        ("flavor", "It runs the same moment again, slightly better each time."),
    };
}
