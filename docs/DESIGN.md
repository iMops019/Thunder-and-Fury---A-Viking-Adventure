# Design Notes (technical)

Creative/gameplay design (skills, combat, gear tiers, quests, QoL feature
list) lives in [valheim-mod-vision.md](valheim-mod-vision.md) — that's the
primary, most up-to-date design doc. This file covers the technical side:
repo structure, toolchain verification, and modding-limits research.

## Modding limits — how open is Valheim compared to Project Zomboid?

**More constrained than PZ.** PZ's Lua layer is a sandboxed scripting
surface the game was built to expose. Valheim has no official mod support
at all — everything here is Harmony patches + reflection into the real
Mono game code via BepInEx, with Jotunn as a library that wraps the common
patterns (adding prefabs, items, skills, recipes, UI panels, key bindings)
so we're not hand-writing raw IL patches for every little thing.

Practically, that means:
- Adding data-shaped content (items, recipes, skills, status effects) is
  well-trodden and safe — Jotunn has first-class APIs for this, confirmed
  real (e.g. `SkillManager.AddSkill` —
  [Jotunn custom skills tutorial](https://valheim-modding.github.io/Jotunn/tutorials/skills.html)).
- Adding new *behavior* (custom NPC AI, dialogue, quest logic) means
  cloning/patching existing game systems (`Humanoid`, `MonsterAI`,
  `Character`) with Harmony — more work than PZ, but proven possible.
  Confirmed via existing mods that already do exactly this: **VillageLife**
  (quest givers, merchants, guards via a craftable Village Hall),
  **Marketplace And Server NPCs Revamped** (custom quests with
  configurable targets/types/rewards + 8 NPC types), and **CreatureQuests**
  (a full quest framework). None of these are ours to depend on/copy code
  from, but they're proof the *approach* works and worth reading for
  architecture ideas.
- No brand-new playable "class"/character slot — Valheim has one player
  character type. "Adding a player" in the RPG sense means adding **NPCs**
  (quest givers, vendors), not a new playable character.
- The magic/rare/legendary loot idea sits in the same space as the
  existing open-source mod **EpicLoot** (Magic/Rare/Epic/Legendary/Mythic,
  weighted rolls, effects scaling with rarity) — good prior art for the
  roll/weighting math, not something to copy code from directly.

## Toolchain — verified, not guessed

Extracted the real `JotunnLib` NuGet package (v2.29.2) rather than
assuming how Jotunn wants to be referenced. Findings, confirmed against
the official [JotunnModStub](https://github.com/Valheim-Modding/JotunnModStub)
template and the package's own `build/*.props`:

- **Target framework: `net48`.** (Jotunn.dll itself ships as `net462`;
  `net48` is the template's own choice and is backward-compatible.)
- **Reference Jotunn via `<PackageReference Include="JotunnLib" />`, not
  manual HintPaths.** The package's bundled `Paths.props` auto-detects
  `VALHEIM_INSTALL` (env var, or the Windows registry for Steam app
  `892970` as a fallback — no env var needed on most Windows machines) and
  wires up BepInEx, Harmony, and every Unity module reference itself.
- **Game-touching code needs "publicized" assemblies.** Fields like
  `ItemDrop.ItemData.m_weight` are otherwise internal/private. Jotunn
  bundles a prebuild MSBuild task (`JotunnBuildTask`) that generates public
  copies under `valheim_Data/Managed/publicized_assemblies/` when
  `DoPrebuild.props` sets `ExecutePrebuild=true` (see that file at the repo
  root) — confirmed working by actually running it against the local
  install (all 10 expected `*_publicized.dll` files generated).
- **Known quirk, confirmed by hitting it:** building the whole solution in
  parallel races every project's prebuild task against the same shared
  output folder → intermittent file-lock errors on the first cold build.
  Fix: `dotnet build -m:1` once, then normal parallel builds are fine.
- Jotunn.dll itself is excluded from each mod's build output
  (`ExcludeAssets=runtime` on the `PackageReference`) since it's meant to
  be a single shared runtime install (`BepInEx/plugins/Jotunn/`), not
  bundled redundantly inside every mod's own output folder.
- **Decompiling the real game code, confirmed working (2026-09-09):**
  `dotnet tool install -g ilspycmd` (ICSharpCode decompiler) against
  `assembly_valheim_publicized.dll` in the same
  `publicized_assemblies` folder the prebuild task generates.
  `ilspycmd -t <TypeName> <dll>` dumps one class to readable (if
  Unity-boilerplate-heavy) C#. This is how every ValheimQoL patch got
  written — e.g. finding that `Player.m_maxPlaceDistance` and
  `Player.m_staminaRegen` are plain public fields set in the constructor
  (patchable with a Harmony constructor Postfix), and that
  `Player.FindClosestSnapPoints`'s `maxSnapDistance` is a normal method
  parameter (patchable with a Prefix using `ref float` — Harmony allows
  `ref` on a patch parameter to mutate a non-ref original parameter
  before the original body runs). None of the four originally-stubbed
  QoL patches (`BuildingSnap`, `AutoPickupSort`, `QuickSlots`,
  `CraftFromContainers`) turned out to need a Transpiler, contrary to
  their pre-decompile guesses — worth checking the real code before
  assuming something needs one.

## Mod split (as scaffolded)

```
Core             Skill framework, shared UI theme, shared keybinds, shared
                 modifier registry. Everything else depends on this.
RarityLoot       Magic/Rare/Legendary item tiers + visuals. Depends on Core.
Quests           Objective-based quest system + quest giver NPC(s). Depends on Core.
ValheimQoL       Standalone QoL layer (weight/stack/stamina/etc). No Core
                 or Jotunn runtime dependency — ships/updates independently.
ExampleMiniMod   Template — copy to start a new Core-dependent mini-mod.
```

Each is its own BepInEx plugin GUID/DLL, so a 1.0 (or later) update that
breaks one system's game hook doesn't take the others down with it.

## Status

- [x] Toolchain validated end-to-end against the real local install: game
      assemblies resolve, publicized assemblies generate successfully,
      only failure remaining is the not-yet-installed BepInEx (expected).
- [x] BepInEx installed via Thunderstore Mod Manager and a full green
      build confirmed (`dotnet build -m:1`, 0 errors, all 5 projects).
      Mod-manager installs live in a per-profile folder rather than the
      Steam Valheim folder, so Jotunn's auto-detection needs a
      `BEPINEX_PATH` env var pointed at that profile's `BepInEx` folder —
      see the README setup section.
- [x] Skill list, XP, and per-level effects for all 9 Gathering +
      Production skills (Mining, Woodcutting, Fishing, Skinning,
      Smithing, Cooking, Fletching, Building, Crafting) — implemented,
      not yet runtime-tested. Only the 3 Combat stats (Attack, Strength,
      Defense) remain from vision.md's original skill list. Cooking's
      special-recipe tier stays open (vision.md itself flags it as
      unfleshed design space). See docs/PROGRESS.md for exact per-skill
      scope and caveats — several skills deliberately ship with "nothing
      designed beyond the name" (Fletching, Building, Crafting) rather
      than inventing gameplay effects vision.md never specified.
- [ ] Rarity tiers + what a rarity roll grants for *ordinary* vanilla
      gear — see vision.md open questions. (The rolled-affix mechanism
      itself is built and already used by Voltun's Set; what's still
      undesigned is which vanilla items are eligible to roll Magic/Rare
      and at what odds.)
- [ ] Quest list + quest giver placement + reward structure — see vision.md
- [ ] Weight slider range/defaults — ValheimQoL has working defaults
      (0.5x material weight, 2x stack size) already, tune later
