namespace StrikeSprite;

/// <summary>Tunables for the effect. Change here, rebuild, redeploy.</summary>
internal static class ModConfig
{
    public const string ModId = "strikesprite";
    public const string LogFileName = "strikesprite.log";

    /// <summary>Write per-hit debug lines to the log. Turn off for a release build.</summary>
    public const bool Verbose = true;

    // ── Texture ───────────────────────────────────────────────────────────────
    /// <summary>File name expected under assets/textures/ and inside the PCK.</summary>
    public const string TextureFileName = "strike.png";

    /// <summary>Resource path used when the mod ships a PCK with this texture.</summary>
    public const string TextureResPath = "res://strikesprite/textures/strike.png";

    // ── Sprite placement ──────────────────────────────────────────────────────
    public const float OffsetX = 0f;
    public const float OffsetY = -100f;
    public const int ZIndex = 10;

    // ── Lifetime ──────────────────────────────────────────────────────────────
    /// <summary>
    /// true  = never free the sprite (debugging; sprites accumulate per hit).
    /// false = free it automatically after <see cref="LifetimeSeconds"/>.
    /// </summary>
    public static readonly bool KeepSpriteAlive = true;

    public const float LifetimeSeconds = 2f;
}
