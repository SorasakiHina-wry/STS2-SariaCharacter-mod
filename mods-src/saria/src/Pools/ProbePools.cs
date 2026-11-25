using BaseLib.Abstracts;
using Godot;

namespace Saria.Pools;

/// <summary>
/// The character's own card pool. BaseLib needs one per custom character; cards declare membership
/// with [Pool(typeof(ProbeCardPool))] and are concatenated into the pool automatically.
/// </summary>
public sealed class ProbeCardPool : CustomCardPoolModel
{
    public override string Title => "saria";

    public override Color DeckEntryCardColor => new("7FD4FF");

    public override bool IsColorless => false;

    /// <summary>
    /// Tints the card frame through BaseLib's HSV shader (CardFrameMaterialPath stays "card_frame_red").
    /// </summary>
    public override Color ShaderColor => new("7FD4FF");

    /// <summary>
    /// Reuse the Ironclad energy orb/icon art until custom energy icons exist.
    /// BaseLib's default would build a pool-specific icon key that has no texture yet.
    /// </summary>
    // 必须是游戏里真实存在的能量配色池：PreloadManager 会预加载 EnergyIconPath，
    // 指到只存在于资源缓存的虚拟路径会抛 AssetLoadException（曾导致卡在涅奥对话）。
    // 文本内联图标的前缀单独由 ProbeEnergyPrefixPatch 提供 "saria"。
    public override string EnergyColorName => "ironclad";
}

public sealed class ProbeRelicPool : CustomRelicPoolModel
{
    // 必须是游戏里真实存在的能量配色池：PreloadManager 会预加载 EnergyIconPath，
    // 指到只存在于资源缓存的虚拟路径会抛 AssetLoadException（曾导致卡在涅奥对话）。
    // 文本内联图标的前缀单独由 ProbeEnergyPrefixPatch 提供 "saria"。
    public override string EnergyColorName => "ironclad";
}

public sealed class ProbePotionPool : CustomPotionPoolModel
{
    // 必须是游戏里真实存在的能量配色池：PreloadManager 会预加载 EnergyIconPath，
    // 指到只存在于资源缓存的虚拟路径会抛 AssetLoadException（曾导致卡在涅奥对话）。
    // 文本内联图标的前缀单独由 ProbeEnergyPrefixPatch 提供 "saria"。
    public override string EnergyColorName => "ironclad";
}
