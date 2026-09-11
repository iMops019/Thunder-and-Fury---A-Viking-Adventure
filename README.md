# Thunder & Fury - A Viking Adventure Mod

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
As of 2026-09-11, the mod has an in-game content editor (Dev Tool, F9), a
working skill/QoL/quest/rarity-loot layer, and 176 hand-placed Legendary
items across all 8 biomes — all confirmed running in-game. See
[docs/PROGRESS.md](docs/PROGRESS.md) for the authoritative, per-feature
status and [docs/valheim-mod-vision.md](docs/valheim-mod-vision.md)'s own
Status section for a higher-level summary.

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
  `dotnet build`): all 6 projects (Core, DevTool, ExampleMiniMod,
  RarityLoot, Quests, ValheimQoL) compile with 0 errors. Only a harmless
  `UnityEngine.ProfilerModule` reference warning remains (an unused Unity
  module reference, not a real problem). See setup step 2 below for the
  one thing needed to get here if you installed BepInEx via a mod
  manager rather than a manual copy.
- **A same-day post-1.0 hotfix silently broke several field accesses**:
  fields that were public when this project's code was first written
  (`Character.m_nview`, `InventoryGui.m_dragItem`/`m_craftRecipe`,
  `CharacterDrop.m_dropsEnabled`, `MineRock.m_nview`) became private on
  the real game assembly, while the locally-generated "publicized"
  reference assembly still showed them as public — meaning affected code
  compiled clean but threw `FieldAccessException` at runtime. If you hit
  one of these against a *future* game update, the fix pattern is:
  `GetComponent<T>()` for a public alternative where one exists, or
  Harmony's `Traverse.Create(obj).Field<T>("m_fieldName").Value`
  otherwise — never assume "compiles" means "the real field is still
  public."
- **BepInEx's actual install location may not be the Steam game
  folder.** If you manage mods through a mod manager (this project's
  active install is under Thunderstore Mod Manager's own profile
  folder), point `BEPINEX_PATH` at that profile's `BepInEx` folder, not
  `<Valheim install>\BepInEx` — the latter may not exist at all, or may
  be an empty/stale leftover.
- **Decompiling the real game code:** `dotnet tool install -g ilspycmd`
  gives a throwaway global decompiler, no project reference needed. Run
  `ilspycmd -t <TypeName> <dll>` against the REAL
  `valheim_Data\Managed\assembly_valheim.dll` (not the `_publicized`
  copy — see the hotfix note above for why that distinction now
  matters) to dump one class to readable C#. This is how every patch in
  this project was written — confirming actual 1.0 method/field names
  and accessibility first rather than guessing.

## Structure

```
src/
  Core/                     Shared framework: skill system (generic
                             vanilla-XP redirect + Jotunn registration,
                             reuses vanilla's own leveling curve), combat
                             extras (Chain Lightning proc, generic weapon
                             special-effect dispatch), shared quest/skill
                             "dumb registries" other mini-mods read from,
                             shared UI theme, shared keybinds. Everything
                             else depends on this. All 12 skills from
                             vision.md's skill list are implemented and
                             confirmed working in-game — Mining,
                             Woodcutting, Fishing, Skinning, Smithing,
                             Cooking, Fletching, Building, Crafting,
                             Attack, Strength, Defense.
  MiniMods/
    DevTool/                 In-game content editor overlay (press F9).
                             Depends on Core. 11 tabs: Item/Piece/
                             Creature/Recipe/Skill/Quest Creators, World/
                             Biome/Dungeon editors, a Spawn tab (instant-
                             give any item for testing), and a Values tab
                             that generically surfaces every loaded
                             plugin's Config.Bind entries for live tuning.
                             Confirmed working in-game.
    ExampleMiniMod/         Template — copy this folder to start a new
                             mini-mod that depends on Core.
    RarityLoot/              Magic/Rare/Legendary item tiers. Depends on
                             Core. Generic rolled-affix framework
                             (PoE-style, stored in vanilla's own per-item
                             save data); ordinary vanilla gear rolls
                             Magic/Rare at config-tunable odds; a
                             biome-aware ambient drop system gates
                             Magic/Rare/Legendary by the killed creature's
                             biome so higher tiers can't show up too
                             early. Plus 176 hand-placed Legendary
                             weapons/armor (11 each across all 8 biomes,
                             drop-only, themed by creature), Voltun's Set,
                             the Lightning Sword, and the Skinning Knife.
                             All confirmed working in-game.
    Quests/                  Objective-based quests via a buildable
                             Adventure Board (not an NPC). Depends on
                             Core and RarityLoot.
    ValheimQoL/              QoL pillar — weight/stack, stamina
                             drain+regen, building snap/placement,
                             auto-pickup+sort, quick slots, craft from
                             containers. Confirmed working in-game.
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
4. No separate config-editor mod needed — `ThunderFury.DevTool`'s own
   in-game panel (press **F9**) has a Values tab listing every mod's
   `Config.Bind` entries, live-editable, no recompile. (Superseded the
   earlier "install BepInEx ConfigurationManager" recommendation.)

## Deploying a build for local testing

Automated as of 2026-09-10 (`Directory.Build.targets`): every `dotnet build`
copies each project's own DLL+PDB straight into `BEPINEX_PATH\plugins\<AssemblyName>\`
(or `VALHEIM_INSTALL\BepInEx\plugins\...` if `BEPINEX_PATH` isn't set). Just
build, then launch the profile from Thunderstore Mod Manager/r2modman — no
manual copy step. Skipped silently if neither env var is set.
