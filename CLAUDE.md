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

Two things to know before changing `MapHintPatch`:

- `startDistance` is a single `static int?`, only ever written by the
  constructor patch, so the hint exists only for an event this process watched
  being created. A save loaded mid-event shows nothing — that's the known
  limitation in [README.md](README.md), not a bug to chase.
- The overlay is parented to the `MapNodeUI` itself, never to its icon child,
  which `MapNodeUI.Set()` destroys and recreates. `GetHintedBranch` is pure and
  guarded by `SelfCheck` at `Init`; extend the self-check when touching the bit
  math.

## Workspace layout

This repo sits alongside the other MT2 mod repos in the parent workspace
folder, which also holds three shared folders. Paths below are relative to
this repo's root; the `.csproj` sits one level deeper, so it spells the same
targets `../../mt2-plugins/...`.

- `../mt2-game` — symlink to the Steam install. The game's own assemblies are
  in `MonsterTrain2_Data/Managed/` (`Assembly-CSharp.dll` is the game code);
  the BepInEx loader, its config, and `LogOutput.log` are under `BepInEx/`.
- `../mt2-plugins` — symlink to `mt2-game/BepInEx/plugins`, the folder the game
  actually loads mods from. Each mod's `PluginDeployDir` targets it, so
  `dotnet build` deploys into the live install with no separate copy step.
- `../mt2-decompiled` — ILSpy output for the game's assemblies
  (`Assembly-CSharp/`, `Assembly-CSharp-firstpass/`, `CommandSystem/`).
  Read-only reference material; never built. See below.

## Build

```sh
set -a; . ./.env; set +a   # exports GITHUB_USER / GH_AUTH_TOKEN
dotnet build
```

`nuget.config` pulls `TrainworksReloaded.Base` and `Conductor` from a private
GitHub Packages feed, expanding `%GITHUB_USER%`/`%GH_AUTH_TOKEN%` from the
**process environment** — `dotnet` does not read `.env` itself, hence the
`set -a` line. Without those vars, restore fails with `401 Unauthorized` /
`NU1301`. There is no offline fallback: both packages exist only on that feed,
so `--source ~/.nuget/packages` helps only once an authenticated restore has
already cached them.

Build copies the DLL to `../../mt2-plugins/InscryptionWarning` (see
`PluginDeployDir` in
[InscryptionWarning.csproj](InscryptionWarning/InscryptionWarning.csproj)),
which symlinks into the game's `BepInEx/plugins` — build alone is enough to
deploy for in-game testing.

## Decompiled game code

Monster Train 2 decompiled source lives at `../mt2-decompiled`. Use it
to look up game types (e.g. `MapNodeUI`, `FollowupConditionInscryptionEvent`,
`SaveManager`) instead of guessing signatures — Harmony patches target these
classes directly and get silently skipped if a patched member doesn't exist.

Key data flow for the Inscryption event (see
`FollowupConditionInscryptionEvent.cs`, `MapSection.cs`, `MapNodeUI.cs`):
- `SaveManager.GetInscryptionEventPath()` — the secret 0-7 code, generated once per run.
- `FollowupConditionState`'s constructor receives `currentDistance` (the event's `startDistance`) when the Inscryption followup is created — not exposed publicly, so this mod caches it via a Harmony Postfix on that constructor.
- The 3 checked distances are `startDistance+1 .. startDistance+3`; the required bit at each is `(code >> (2 - i)) & 1` for `i = distance - (startDistance+1)`, where `0` = left branch, `1` = right branch.
- `MapNodeUI.GetLocation().Distance` / `.GetBranch()` identify which node is which distance/branch; `MapNodeUI.RefreshState` is the natural per-node Postfix point to add a UI overlay.
