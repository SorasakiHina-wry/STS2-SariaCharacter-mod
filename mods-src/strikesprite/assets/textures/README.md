# Textures

Drop the effect image here.

| File | Required | Notes |
|---|---|---|
| `strike.png` | yes (placeholder) | RGBA PNG with alpha. The sprite is drawn at the creature's `(0, -100)` offset, `ZIndex = 10`, so it should be roughly 96–256 px wide and centred. |

The loader resolves it in this order:

1. `res://strikesprite/textures/strike.png` — only if the mod ships a PCK that contains it with a *valid* Godot import;
2. raw bytes at the same `res://` path, decoded at runtime (`Image.LoadPngFromBuffer`) — survives a broken `.ctex`;
3. loose file next to the DLL: `<mods>/strikesprite/assets/textures/strike.png` — this is the dev path and needs no PCK at all.

Because of step 3, you can iterate without building a PCK: rebuild, redeploy, restart, and the PNG is picked up from the folder.

> File name is configurable in `src/ModConfig.cs` (`TextureFileName`, `TextureResPath`).
