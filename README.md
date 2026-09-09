# Viking Adventure Mod

A cozy-grinding Valheim RPG overhaul: OSRS-inspired skills, magic/rare/
legendary loot, real quests, and a QoL layer — split into a shared **Core**
framework plus independent mini-mods. Built on
[BepInEx](https://github.com/BepInEx/BepInEx) + [Jotunn](https://github.com/Valheim-Modding/Jotunn).
See [docs/valheim-mod-vision.md](docs/valheim-mod-vision.md) for the full
creative design (locked-in decisions on skills, combat, gear tiers, quests)
and [docs/DESIGN.md](docs/DESIGN.md) for the technical/repo-structure side
and modding-limits research.

## Status

Valheim 1.0 releases 2026-09-09; Iron Gate gives no compatibility guarantee
for existing mods and has published no public test branch.

Verified against the real local install (confirmed at
`C:\Program Files (x86)\Steam\steamapps\common\Valheim` — not confirmed
whether this is pre- or post-1.0 build, since Steam pre-loads ahead of
unlock; re-verify after the 9th if anything looks off):
- Every project references the official `JotunnLib` NuGet package rather
  than hand-wired local paths — it auto-detects the Valheim install (env
  var `VALHEIM_INSTALL`, or the Windows registry as a fallback) and wires
  up BepInEx/Harmony/game-assembly references itself. Confirmed by
  extracting the real package contents, not guessed.
- Jotunn's bundled prebuild task successfully generated the "publicized"
  game assemblies (public versions of otherwise-internal fields like
  `ItemDrop.ItemData.m_weight`) against this install — required for any
  code that touches game types directly, which every project here does.
- **Known quirk:** running `dotnet build` at the solution level races all
  projects' prebuild tasks against the same shared output folder the first
  time (file-lock errors). Build once with `dotnet build -m:1` (single
  node) to generate the publicized assemblies cleanly; normal parallel
  builds are fine afterward since the files then already exist.
- **Remaining blocker for a full green build:** BepInEx isn't installed
  into the Valheim folder yet — that's the only thing left failing
  (`BepInEx`/`0Harmony`/etc. reference errors). See setup step 1 below.

## Structure

```
src/
  Core/                     Shared framework: Skill system (Jotunn
                             SkillManager wrapper + our own XP curves),
                             shared UI theme, shared keybinds, shared
                             modifier registry. Everything else depends
                             on this.
  MiniMods/
    ExampleMiniMod/         Template — copy this folder to start a new
                             mini-mod that depends on Core.
    RarityLoot/              Magic/Rare/Legendary item tiers. Depends on Core.
    Quests/                  Objective-based quests + quest giver NPC(s).
                             Depends on Core.
    ValheimQoL/              QoL pillar — weight/stack tweaks (done), stamina
                             drain (done), stamina regen/building snap/quick
                             slots/auto-pickup/craft-from-containers (design
                             notes, not yet implemented). Standalone: no
                             Core or Jotunn dependency, ships independently.
docs/
  valheim-mod-vision.md      Creative design: skills, combat, gear tiers,
                             quests, QoL feature list. Primary design doc.
  valheim-food-reference.md  Pre-1.0 food-item stat reference for Cooking
                             skill design (needs a pass once 1.0 lands).
  DESIGN.md                  Technical/repo-structure notes + modding-limits
                             research.
```

Each mini-mod is its own BepInEx plugin (own GUID, own `.dll`), so a game
update that only breaks one mini-mod's hook doesn't require touching the
others.

## One-time setup (per machine)

1. Install Valheim (Steam), then [BepInEx 5](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
   into that Valheim folder — easiest via a mod manager
   ([r2modman](https://thunderstore.io/c/valheim/p/ebkr/r2modman/) or the
   Thunderstore app) rather than a manual copy, so future updates stay easy.
   (Jotunn itself doesn't need a separate manual install for *building* —
   the `JotunnLib` NuGet package handles that — but you still need the
   real Jotunn mod installed as a BepInEx plugin to actually *run* the
   Core/RarityLoot/Quests/ExampleMiniMod projects in-game, since they use
   Jotunn's runtime APIs. `ValheimQoL` doesn't need Jotunn installed at all.)
2. `VALHEIM_INSTALL` usually doesn't need to be set manually — Jotunn's
   build props auto-detect it via the Windows registry. Only set it as an
   environment variable if that detection fails for some reason.
3. First build: `dotnet build -m:1` at the repo root (see the race-condition
   note above). Regular builds after that can drop `-m:1`.

## Deploying a build for local testing

Build output isn't auto-copied into `BepInEx/plugins` yet — that's a
follow-up once we've confirmed the exact plugin folder layout each project
needs (a post-build copy step per project).
