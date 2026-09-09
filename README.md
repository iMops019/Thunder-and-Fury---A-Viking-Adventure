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

Valheim 1.0 released 2026-09-09. Iron Gate gave no compatibility guarantee
for existing mods and published no public test branch ahead of launch.

Verified against the real local install (confirmed at
`C:\Program Files (x86)\Steam\steamapps\common\Valheim`, and against the
actual post-1.0 build specifically — see the decompile note below):
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
- **Full green build confirmed** (`dotnet build -m:1`, then plain
  `dotnet build`): all 5 projects compile with 0 errors. Only a harmless
  `UnityEngine.ProfilerModule` reference warning remains (an unused Unity
  module reference, not a real problem). See setup step 2 below for the
  one thing needed to get here if you installed BepInEx via a mod
  manager rather than a manual copy.
- **Decompiling the real game code:** `dotnet tool install -g ilspycmd`
  gives a throwaway global decompiler, no project reference needed. Run
  `ilspycmd -t <TypeName> <dll>` against
  `valheim_Data\Managed\publicized_assemblies\assembly_valheim_publicized.dll`
  to dump one class to readable C#. This is how every ValheimQoL patch was
  written — confirming actual 1.0 method/field names first rather than
  guessing against pre-release assumptions.

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
    RarityLoot/              Magic/Rare/Legendary item tiers. Depends on
                             Core. Generic rolled-affix framework
                             implemented; Stone Pickaxe and Voltun's Set
                             (Legendary) use it. Rolling ordinary vanilla
                             gear into Magic/Rare is still undesigned.
    Quests/                  Objective-based quests + quest giver NPC(s).
                             Depends on Core.
    ValheimQoL/              QoL pillar — every planned feature implemented
                             and compiles clean (weight/stack, stamina
                             drain+regen, building snap/placement,
                             auto-pickup+sort, quick slots, craft from
                             containers) but none runtime-tested yet — see
                             docs/PROGRESS.md for per-feature status.
                             Standalone: no Core or Jotunn dependency,
                             ships independently.
docs/
  valheim-mod-vision.md      Creative design: skills, combat, gear tiers,
                             quests, QoL feature list. Primary design doc.
  valheim-food-reference.md  Pre-1.0 food-item stat reference for Cooking
                             skill design (needs a pass once 1.0 lands).
  DESIGN.md                  Technical/repo-structure notes + modding-limits
                             research.
  PROGRESS.md                Living checklist of every system/mini-mod:
                             what it's for and whether it's designed,
                             started, half-baked, or fully wired. Check
                             items off here as things land.
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
   - **If you installed BepInEx via r2modman/Thunderstore Mod Manager**
     (the normal, recommended path), it lives in an isolated per-profile
     folder, not inside the Steam Valheim folder — Jotunn's props won't
     find it there automatically. Set a `BEPINEX_PATH` environment
     variable pointing at that profile's `BepInEx` folder instead, e.g.:
     `C:\Users\<you>\AppData\Roaming\Thunderstore Mod
     Manager\DataFolder\Valheim\profiles\<your profile name>\BepInEx`.
     Set it with `setx BEPINEX_PATH "<path>"` in PowerShell, then open a
     new terminal/IDE window for it to take effect.
3. First build: `dotnet build -m:1` at the repo root (see the race-condition
   note above). Regular builds after that can drop `-m:1`.
4. Optional but recommended: search Thunderstore for "BepInEx
   ConfigurationManager" and install it as a separate mod. Every
   mini-mod's tunables are plain BepInEx `Config.Bind` entries, so this
   gives an in-game F1 menu to change them live — material costs,
   multipliers, everything — without a recompile.

## Deploying a build for local testing

Build output isn't auto-copied into `BepInEx/plugins` yet — that's a
follow-up once we've confirmed the exact plugin folder layout each project
needs (a post-build copy step per project).
