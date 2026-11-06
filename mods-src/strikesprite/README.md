# Strike Sprite

Standalone visual-effect mod for Slay the Spire 2. When Ironclad plays **Strike**, it
attaches a `Sprite2D` (and later a Spine animation) above the struck creature.

This is a clean-room extraction of the debug patch that was prototyped inside `MCPTest`;
it carries no bridge, no TCP port and no test content, so it is safe to publish.

## Layout

```
strikesprite/
  strikesprite.csproj     net9.0, references the game's own sts2/GodotSharp/0Harmony
  mod_manifest.json       id/version/… read by ModManager
  src/
    ModEntry.cs           [ModInitializer] + per-class Harmony patching
    StrikeSpritePatch.cs  Postfix on StrikeIronclad.OnPlay
    TextureLoader.cs      res:// → raw-bytes → loose-file fallback chain
    ModConfig.cs          offset, ZIndex, lifetime, file names
    ModLog.cs             appends to user://strikesprite.log
  assets/
    textures/             drop strike.png here
    spine/                reserved for the Spine animation
  tools/
    build.ps1             dotnet build -c Release -o out
    deploy.ps1            copy out/* into the game's mods\strikesprite
```

## Build & run

```powershell
# game dir defaults to the path in the csproj; override if needed:
$env:STS2_GAME_DIR = 'D:\SteamLibrary\steamapps\common\Slay the Spire 2'

.\tools\build.ps1
.\tools\deploy.ps1          # copies into <game>\mods\strikesprite
# then fully restart the game (mods load only at startup)
```

No NuGet packages are required: the project compiles against the DLLs the game ships, so
the build works offline and cannot drift from the runtime Godot/Harmony versions.

## Why the DLL must load at startup

Harmony detours only take effect before the game JIT-compiles (and possibly inlines) the
patched call sites. Hot-patching a running session was measured to register the patch but
never fire. Always: build → deploy → restart.

## Verify

- log: `%APPDATA%\..\Godot\app_userdata\<game>\strikesprite.log` (also mirrored to the Godot console)
  - expect `harmony: 1 class(es) applied, 0 skipped`
  - on a hit: `target_node=… effect_global=… z=10`
- in-game: play Ironclad's Strike, look at (0, -100) relative to the enemy.

## Workshop checklist (later)

1. Set `author`, bump `version`, keep `id` stable forever.
2. Fill in `min_game_version` for the build you tested against.
3. Preview image: `image.jpg`, **< 1 MB**.
4. `Content/` gets the *folder* `strikesprite/` (manifest + `strikesprite.dll` + `assets/`),
   not a zip.
5. Upload with the official tool: <https://github.com/megacrit/sts2-mod-uploader>
   (`ModUploader.exe upload -w <workspace>`), tags are set once and cannot be changed later.
6. Keep `dll` file name equal to `id` (`strikesprite.dll`) — the loader looks for
   `<id>.dll` and only Windows' case-insensitive filesystem hides mistakes.
