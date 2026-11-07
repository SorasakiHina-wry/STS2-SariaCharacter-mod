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
using Probe.Pools;

namespace Probe.Relics;

// Relic icons reuse vanilla atlas entries via IconBaseName, so every relic is fully rendered
// before any custom art exists - swap IconBaseName for a custom atlas entry later.
// 脆化技艺 (the starter relic) lives in its own file because its effect is patched in.

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
        // 描述留空时回退到 effects.md 的「效果」原文，不拼接触发条件
        ("description", "获得1点力量。"),
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
        ("description", "获得1点敏捷。"),
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
        ("description", "获得2点力量，获得2点敏捷。"),
    };
}
