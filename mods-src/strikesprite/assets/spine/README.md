# Spine

Reserved for the animated effect. Nothing here yet — the mod currently draws the
placeholder `textures/strike.png` instead.

Expected layout once the assets exist:

| File | Notes |
|---|---|
| `strike.atlas` | Spine atlas, paths inside must stay relative to this folder |
| `strike.skel` | binary skeleton (or `strike.json` if the export is JSON) |
| `strike.png` | atlas texture, keep the file name referenced in `strike.atlas` |

Wiring plan (`src/StrikeSpritePatch.cs`, `AddSpineAnimation`):

1. load the atlas + skeleton from `res://strikesprite/spine/strike.atlas` (or the loose
   folder next to the DLL, same fallback chain as textures);
2. build the Spine node via the game's spine runtime (`libspine_godot` ships with the game —
   see `MegaCrit.Sts2.Core.Nodes` for `SpineSprite` / `SpineAnimationAccess` usage);
3. add it as a child of the `Node2D` effect root, which already carries
   `Position = (0, -100)` and `ZIndex = 10`;
4. free the node when the animation finishes (`SpineSprite` animation-complete signal) or
   after `ModConfig.LifetimeSeconds` when `KeepSpriteAlive` is `false`.

Keep the effect root intact when adding the animation — the trigger patch does not need to
change, only `AddSpineAnimation`.
