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
    public override string EnergyColorName => "ironclad";
}

public sealed class ProbeRelicPool : CustomRelicPoolModel
{
    public override string EnergyColorName => "ironclad";
}

public sealed class ProbePotionPool : CustomPotionPoolModel
{
    public override string EnergyColorName => "ironclad";
}
