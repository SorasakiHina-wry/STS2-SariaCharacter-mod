using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using Probe.Cards;
using Probe.Pools;
using Probe.Relics;

namespace Probe.Characters;

/// <summary>
/// Validation character. Inherits BaseLib's PlaceholderCharacterModel, which borrows every visual
/// (combat visuals, icons, energy counter, rest-site / merchant animations, card trail, SFX) from
/// another character - Ironclad by default - so the whole character is playable before any art exists.
/// Override one member at a time as real assets land.
/// </summary>
public sealed class Probe : PlaceholderCharacterModel
{
    /// <summary>Which vanilla character to borrow placeholder visuals from.</summary>
    public override string PlaceholderID => "ironclad";

    /// <summary>能力牌动作改用骨架里的 powerup（由 Attack_2 复制并提速），其余沿用默认。</summary>
    protected override List<(AnimState, string)> AnimationStates => new()
    {
        (new AnimState("cast"), "Cast"),
        (new AnimState("attack"), "Attack"),
        (new AnimState("hurt"), "Hit"),
        (new AnimState("powerup"), "PowerUp"),
    };

    public override Color NameColor => new("7FD4FF");

    public override CharacterGender Gender => CharacterGender.Neutral;

    public override int StartingHp => 70;

    public override int StartingGold => 99;

    public override CardPoolModel CardPool => ModelDb.CardPool<ProbeCardPool>();

    public override RelicPoolModel RelicPool => ModelDb.RelicPool<ProbeRelicPool>();

    public override PotionPoolModel PotionPool => ModelDb.PotionPool<ProbePotionPool>();

    public override IEnumerable<CardModel> StartingDeck => new CardModel[]
    {
        ModelDb.Card<ProbeStrike>(),      // 打击 ×4
        ModelDb.Card<ProbeStrike>(),
        ModelDb.Card<ProbeStrike>(),
        ModelDb.Card<ProbeStrike>(),
        ModelDb.Card<ProbeDefend>(),      // 防御 ×4
        ModelDb.Card<ProbeDefend>(),
        ModelDb.Card<ProbeDefend>(),
        ModelDb.Card<ProbeDefend>(),
        ModelDb.Card<ProbeHardening>(),   // 硬化 ×1
        ModelDb.Card<ProbeIgnite>(),      // 激发 ×1
    };

    public override IReadOnlyList<RelicModel> StartingRelics => new RelicModel[]
    {
        ModelDb.Relic<BrittleCraft>(),
    };

    /// <summary>
    /// In-code localization. BaseLib writes these into the table that matches the model category
    /// ("characters" here), keyed as "PROBE.title", "PROBE.description", ...
    /// </summary>
    public override List<(string, string)>? Localization => new()
    {
        ("title", "塞雷娅"),
        ("titleObject", "塞雷娅"),
        ("pronounSubject", "it"),
        ("pronounObject", "it"),
        ("possessiveAdjective", "its"),
        ("pronounPossessive", "its"),
        ("description", "A quiet traveller whose bones remember every blow they have taken.\nShe lends that memory to others as armour."),
        ("bestiaryQuote", "She has been measured. She simply refuses the number."),
        ("eventDeathPrevention", "Not yet. I still owe someone a debt."),
        ("cardsModifierTitle", "Probe Cards"),
        ("cardsModifierDescription", "Probe cards will now appear in rewards and shops."),
    };
}
