# InscryptionWarning

BepInEx/Harmony mod for Monster Train 2. Gives a visual cue on the map for
the **Inscryption** story event: the event marks a creature with a secret
3-bit code, and the mod highlights which branch (left/right) matches the
required bit at each of the next three map choices. See
[README.md](README.md) for the full feature description and config keys.

## Layout

- [InscryptionWarning/Plugin.cs](InscryptionWarning/Plugin.cs) — BepInEx entry point, calls `MapHintPatch.Init` then `harmony.PatchAll()`.
- [InscryptionWarning/patches/MapHintPatch.cs](InscryptionWarning/patches/MapHintPatch.cs) — all the mod's logic: tracks the active Inscryption followup's start distance via a Harmony patch on `FollowupConditionState`'s constructor, then Postfixes `MapNodeUI.RefreshState` to overlay a hint on the correct branch node.
- Harmony patches belong in `InscryptionWarning/patches/` (one file per concern) per [patches/README.md](InscryptionWarning/patches/README.md).

## Build

```sh
dotnet build
```

Copies the DLL to `../../mt2-plugins/InscryptionWarning` (see `PluginDeployDir`
in [InscryptionWarning.csproj](InscryptionWarning/InscryptionWarning.csproj)), which
symlinks to the game's `BepInEx/plugins` — build alone is enough to deploy for
in-game testing.

`nuget.config` at the repo root points at a GitHub Packages feed that needs
`GITHUB_USER`/`GH_AUTH_TOKEN` env vars; if those aren't set, restore straight
from the local NuGet cache instead: `dotnet build --source ~/.nuget/packages
--source https://api.nuget.org/v3/index.json --source
https://nuget.bepinex.dev/v3/index.json`.

## Decompiled game code

Monster Train 2 decompiled source lives at `~/MT2Mods/mt2-decompiled`. Use it
to look up game types (e.g. `MapNodeUI`, `FollowupConditionInscryptionEvent`,
`SaveManager`) instead of guessing signatures — Harmony patches target these
classes directly and get silently skipped if a patched member doesn't exist.

Key data flow for the Inscryption event (see
`FollowupConditionInscryptionEvent.cs`, `MapSection.cs`, `MapNodeUI.cs`):
- `SaveManager.GetInscryptionEventPath()` — the secret 0-7 code, generated once per run.
- `FollowupConditionState`'s constructor receives `currentDistance` (the event's `startDistance`) when the Inscryption followup is created — not exposed publicly, so this mod caches it via a Harmony Postfix on that constructor.
- The 3 checked distances are `startDistance+1 .. startDistance+3`; the required bit at each is `(code >> (2 - i)) & 1` for `i = distance - (startDistance+1)`, where `0` = left branch, `1` = right branch.
- `MapNodeUI.GetLocation().Distance` / `.GetBranch()` identify which node is which distance/branch; `MapNodeUI.RefreshState` is the natural per-node Postfix point to add a UI overlay.
