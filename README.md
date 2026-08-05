# InscryptionWarning

A Monster Train 2 mod that gives you a hint on the map for the **Inscryption**
story event.

That event marks a creature with a secret 3-bit code (which path to take at
each of the next three branches). This mod highlights, on the map, which
branch (left or right) matches the required bit — so the "secret" code
actually helps you instead of being missable.

## Configuration

`BepInEx/config/InscryptionWarning.cfg`

| Key | Default | Meaning |
| --- | --- | --- |
| `Hint.CueStyle` | `Diegetic` | How obvious the hint is: `Off` (disabled), `Diegetic` (a barely-there tint on the correct node), `Icon` (a small visible badge), `Debug` (an unmissable marker). |

## Install

Use a mod manager (r2modman / Thunderstore Mod Manager) and install
`SpecialCircumstances-InscryptionWarning`. Dependencies are pulled in
automatically:

- BepInEx 5.4.2100
- Trainworks Reloaded 0.7.2
- Conductor 0.4.1

Manual install: drop `InscryptionWarning.dll` into `BepInEx/plugins/`.

## Build

```sh
dotnet build
```

The output DLL is copied to `../../mt2-plugins/InscryptionWarning` after every
build (`PluginDeployDir` in [InscryptionWarning.csproj](InscryptionWarning/InscryptionWarning.csproj)) —
point that at your `BepInEx/plugins` folder to test in-game directly.

Harmony patches live in [InscryptionWarning/patches/](InscryptionWarning/patches/);
the map hint logic is [MapHintPatch.cs](InscryptionWarning/patches/MapHintPatch.cs).

Publishing to Thunderstore is driven by [thunderstore.toml](thunderstore.toml)
via `tcli`; the GitHub workflows in [.github/workflows/](.github/workflows/) build and validate the package.

## License

See [LICENSE](LICENSE).
