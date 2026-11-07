using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using Godot;
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
        ("title", "The Probe"),
        ("titleObject", "the Probe"),
        ("pronounSubject", "it"),
        ("pronounObject", "it"),
        ("possessiveAdjective", "its"),
        ("pronounPossessive", "its"),
        ("description", "Afragmentofsomethingthatwasnevermeanttowake.\nCopiestheshapesofviolenceandhandsthemback."),
        ("bestiaryQuote", "This thing has not been measured yet."),
        ("eventDeathPrevention", "I have not finished observing."),
        ("cardsModifierTitle", "Probe Cards"),
        ("cardsModifierDescription", "Probe cards will now appear in rewards and shops."),
    };
}
