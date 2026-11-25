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
    /// 动画触发名 → 骨架剪辑名。
    ///
    /// 关键约定：**不给 <c>"Cast"</c> 绑任何动画**。
    /// 骨架里的 <c>cast</c> 只是 <c>Skill_Begin</c> 的复制，播出来很像「起手攻击」；
    /// 而原版无色技能牌（如「震荡波」）会主动 <c>TriggerAnim(Cast)</c>，
    /// 一旦绑上，所有这类牌都会播放错误的骨骼动画。
    /// 引擎本身不会自动播 Cast（只有攻击牌由 <c>AttackCommand</c> 播 <c>Attack</c>），
    /// 所以「没有 Cast 映射」正好等于「这些牌不播额外动画」。
    ///
    /// 能力牌用专用的 <c>powerup</c> 剪辑（骨架里由 <c>Attack_2</c> 复制），
    /// 不要退回默认的 <c>cast</c>，否则打能力牌看起来像又打了一下。
    /// </summary>
    protected override List<(AnimState, string)> AnimationStates => new()
    {
        (new AnimState("powerup"), "PowerUp"),
        (new AnimState("attack"), "Attack"),
        (new AnimState("hurt"), "Hit"),
        (new AnimState("stun"), "Stun"),
        (new AnimState("stun"), "StunTrigger"),
        (new AnimState("idle_loop"), "WakeUp"),
        (new AnimState("idle_loop"), "WakeUpTrigger"),
    };

    public override Color NameColor => new("7FD4FF");

    /// <summary>能量球上数字的描边颜色（3/3 那一圈）。</summary>
    public override Color EnergyLabelOutlineColor => new("0b3692");

    /// <summary>角色选择界面的选中音效：哨兵值，由 ProbeSelectSfx 拦截后播本地 ogg。</summary>
    public override string CharacterSelectSfx => ProbeSelectSfx.Key;

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
