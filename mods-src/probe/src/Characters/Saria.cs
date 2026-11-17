using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using Saria.Cards;
using Saria.Pools;
using Saria.Relics;

namespace Saria.Characters;

/// <summary>
/// Validation character. Inherits BaseLib's PlaceholderCharacterModel, which borrows every visual
/// (combat visuals, icons, energy counter, rest-site / merchant animations, card trail, SFX) from
/// another character - Ironclad by default - so the whole character is playable before any art exists.
/// Override one member at a time as real assets land.
/// </summary>
public sealed class Saria : PlaceholderCharacterModel
{
    /// <summary>Which vanilla character to borrow placeholder visuals from.</summary>
    public override string PlaceholderID => "ironclad";

    /// <summary>
    /// 动画触发名 → 骨架剪辑名。**保持引擎默认的能力牌映射**：PowerUp 用 cast 剪辑
    /// （原版 CharacterModel 的默认表就是 (new AnimState("cast"), "PowerUp")）。
    /// 早期版本把它覆写成 Attack_2（powerup 剪辑），结果打能力牌时看起来像"又打了一下"，
    /// 容易误认为播了旧素材动画。另外把原版卡可能触发的其它名字也接上，避免静默无动作。
    /// </summary>
    protected override List<(AnimState, string)> AnimationStates => new()
    {
        (new AnimState("cast"), "Cast"),
        (new AnimState("powerup"), "PowerUp"),   // 能力牌用专属的 powerup 剪辑（骨架里由 Attack_2 复制）
        (new AnimState("attack"), "Attack"),
        (new AnimState("hurt"), "Hit"),
        (new AnimState("cast"), "Buff"),
        (new AnimState("cast"), "Heal"),
        (new AnimState("cast"), "Summon"),
        (new AnimState("stun"), "Stun"),
        (new AnimState("stun"), "StunTrigger"),
        (new AnimState("idle_loop"), "WakeUp"),
        (new AnimState("idle_loop"), "WakeUpTrigger"),
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
        ModelDb.Relic<CalciumCraft>(),
    };

    /// <summary>
    /// In-code localization. BaseLib writes these into the table that matches the model category
    /// ("characters" here), keyed as "SARIA.title", "SARIA.description", ...
    /// </summary>
    public override List<(string, string)>? Localization => new()
    {
        ("title", "塞雷娅"),
        ("titleObject", "塞雷娅"),
        ("pronounSubject", "她"),
        ("pronounObject", "她"),
        ("possessiveAdjective", "她的"),
        ("pronounPossessive", "她的"),
        ("description", "前莱茵生命防卫科主任塞雷娅。\n通过源石技艺操作钙元素，武装，然后保卫。"),
        ("bestiaryQuote", "她被丈量过。她只是不承认那个数字。"),
        ("eventDeathPrevention", "举起盾牌的塞雷娅绝不会倒下。"),
        ("cardsModifierTitle", "塞雷娅卡牌"),
        ("cardsModifierDescription", "塞雷娅的卡牌现在会出现在奖励和商店中。"),
    };
}
