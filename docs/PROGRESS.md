# Progress Tracker

Living checklist of every system and mini-mod, what it's *for*, and where it
actually stands. Check a box only when a thing is designed, implemented,
**and** confirmed working in-game — compiling clean is not the same as
working, so don't check something off just because the build is green.
Everything else stays unchecked with a status note explaining the gap.

Update this as work lands — it's meant to be edited, not just read.

## Toolchain & Build Infrastructure

- [x] Jotunn/BepInEx build pipeline — NuGet-based Jotunn reference,
      auto-detected Valheim install, publicized game assemblies,
      `BEPINEX_PATH` env var for mod-manager profile installs. Confirmed:
      `dotnet build -m:1` succeeds with 0 errors across all 5 projects
      against the real local (1.0-era) install.

## Pillar 1: Skilling System (`src/Core`)

What I want: an OSRS-style skill system that fully replaces how Valheim's
own skills feel — vanilla skills stay technically present (engine needs
them) but are neutralized and hidden; every skill the player actually sees
and levels is ours, with our own XP curves and level-gated effects.

- [ ] **Custom skill system architecture** — *(designed only, zero code)*.
      Vanilla skill damage/stamina bonuses patched out and hidden from UI
      entirely; our skills registered via Jotunn's native skill support.
      Nothing in `Core` beyond a stub plugin that logs on load.
- [ ] **Skill list** — *(designed, first pass)*. Gathering: Mining,
      Woodcutting, Fishing, Skinning. Production: Smithing, Cooking,
      Fletching, Building, Crafting. Combat: Attack, Strength, Defense
      (broad OSRS-style stats, not per-weapon-type like vanilla).
- [ ] **Combat stats** — *(designed)*. Attack = attack speed + stamina
      efficiency for weapon use (the "higher Attack level, less stamina
      drain in combat" mechanic). Strength = single shared damage-scaling
      stat across all weapons. Defense = damage reduction / stagger
      resistance. No hit/miss roll — every swing connects like vanilla.
      **Note:** this stacks with ValheimQoL's flat stamina-drain reduction
      below — two separate, intentional layers, not overlapping ones.
- [ ] **Gathering mechanics** — *(designed)*. Soft-gating principle:
      access to a resource is never locked, only the payoff is. Mining
      and Woodcutting share one mechanic (level → speed + damage to
      ore/trees). Fishing keeps vanilla's cast/bait/reel feel, level
      mainly scales catch chance; Fishing Rod craftable from level 1, no
      Haldor dependency. Skinning: animal deaths leave a carcass, needs a
      new Skinning Knife tool to harvest (flavor/immersion first, leveled
      bonuses later).
- [ ] **Smithing tier ladder** — *(designed, one open question)*. Normal
      gear stays as vanilla, no level gate. New Magic/Rare tiers sit
      between normal and the existing Legendary tier. Legendary crafting
      is hard-gated by a Smithing level threshold. **Open:** what level
      Magic/Rare require is not decided yet.
- [ ] **Cooking** — *(designed, one open question — priority skill)*.
      Level effect = reduced burn/fail chance, not speed or access; no
      recipe-level-gating (biome progression already paces ingredients).
      Base single-ingredient recipes always available. **Open:** the
      special/multi-ingredient recipe tier (buffs stacked together) is
      still unfleshed — the main remaining design space for this skill.
      Also: [valheim-food-reference.md](valheim-food-reference.md) was
      compiled pre-1.0 and is flagged as needing a refresh now that 1.0
      has actually landed.
- [ ] **Building** — *(designed, trivial)*. Not gated behind a
      level at all, stays open like vanilla. May exist as a nominal skill
      with no functional effect.
- [ ] **Progression principle** — *(designed)*. Smooth XP curve plus real
      milestone unlocks layered on top (new recipe/drop chance/tool
      tier/passive at specific levels) so leveling has concrete payoffs.
      First concrete example decided: Woodcutting 15 → double XP from
      normal trees + 25% bonus log yield.
- [ ] **Save-compatibility policy** — *(decided, untested)*. Working
      assumption: only ever add skills/items, never rename or remove
      anything already in a save. Treat as standard practice, not a
      guarantee, until actually tested against a real save through 1.0.

## Pillar 2: UI & Menus

What I want: a real skill panel, character sheet, and expanded inventory
screen to go with the custom skill system — not just data hooked in with
no way to see it.

- [ ] **Scope decision** — *(not designed — the open question)*. How deep
      does this go: a full UI overhaul, or additive panels on top of
      vanilla's UI? Nothing else in this pillar can really be scoped until
      this is answered. This is flagged as the most technically involved
      pillar in the whole mod, and currently has the least design behind
      it of any pillar.
- [ ] **Skill panel / character sheet / expanded inventory** — *(not
      designed)*. Only a one-line description exists so far.

## Pillar 3: Gear & Materials (`src/MiniMods/RarityLoot`)

What I want: new items/equipment built from Valheim's existing assets
(retextures, recombinations, stat variants) rather than modeled from
scratch, tied into the skill system for level-gated crafting.

- [ ] **RarityLoot tier list + roll mechanics** — *(not designed — blocks
      all RarityLoot code)*. What tiers exist beyond the already-decided
      Magic/Rare/Legendary names, and what a rarity roll actually grants,
      isn't decided. `RarityLootPlugin.cs` is currently a stub that only
      logs on load.
- [ ] **Stone Pickaxe** — *(designed, not built)*. First planned item —
      a tier below the Antler Pickaxe, craftable early, no boss-material
      requirement, can mine copper just slower/weaker.
- [ ] **Skinning Knife** — *(designed, not built)*. Second planned item —
      required to harvest carcasses under the new Skinning mechanic.
- [ ] **Named-hero gear pattern** — *(designed as a template)*. Hand-
      crafted unique items that stack multiple bonus effects at once
      (e.g. Voltun's Hatchet: +50% log yield, extra tree damage, faster
      chop, double XP simultaneously) rather than one flat stat bump.
- [ ] **Voltun's Set** (Hatchet + Pickaxe) — *(designed, one open
      question)*. Findable/completable in Black Forest. **Open:**
      acquisition method — crafted from collected materials vs. a straight
      drop chance — still undecided.
- [ ] **Future named-hero sets** beyond Voltun's — *(not designed)*. The
      pattern is set; no second or third hero/set has been thought
      through yet.

## Pillar 4: QoL Layer (`src/MiniMods/ValheimQoL`) — active module, building this first

What I want: this one's standalone on purpose — no Core or Jotunn runtime
dependency, so it ships and updates independently of everything else.

- [x] **Weight/stack size tweaks** ([InventoryTweaks.cs](../src/MiniMods/ValheimQoL/Patches/InventoryTweaks.cs)) —
      config-driven material weight multiplier (0.5x default) and stack
      size multiplier (2x default). Implemented and compiles clean
      against the real local 1.0-era assemblies. **Not yet
      runtime-tested in-game** — compiling clean confirms the patched
      fields/methods still exist, not that the mod behaves correctly
      once actually loaded.
- Stamina / combat pacing — what I want: overall stamina doesn't drain so
  much, less cost in combat, less cost running, than vanilla:
  - [x] **Stamina drain reduction** ([StaminaCombatPacing.cs](../src/MiniMods/ValheimQoL/Patches/StaminaCombatPacing.cs)) —
        flat multiplier (0.85x default) on `Character.UseStamina`, which
        covers attacks, blocking, sprinting, jumping, and dodging all at
        once. Implemented, compiles clean. **Not yet runtime-tested.**
  - [x] **Stamina regen boost** ([StaminaCombatPacing.cs](../src/MiniMods/ValheimQoL/Patches/StaminaCombatPacing.cs)) —
        *(implemented, not yet runtime-tested)*. The full regen formula is
        inline in `Player.UpdateStats` (not independently patchable), but
        confirmed against the real decompile that the base rate it reads,
        `Player.m_staminaRegen`, is a plain public field set once in the
        constructor (default 5f) — same constructor-Postfix trick as
        everything else patched today. `StaminaRegenMultiplier` (1.15x
        default) is now actually wired. Compiles clean. **Not yet tested
        in-game.**
  - *(Separate from both of these: the Attack skill's stamina-efficiency
    bonus for weapon use lives in Pillar 1 / Core, not here — the two are
    meant to stack once Core's skill system exists.)*
- [ ] **Building snap/placement tolerance** ([BuildingSnap.cs](../src/MiniMods/ValheimQoL/Patches/BuildingSnap.cs)) —
      *(implemented, not yet runtime-tested)*. Confirmed against the real
      1.0 decompile of `Player`: both hook points turned out to be simple
      Prefix/Postfix, no Transpiler needed (the old plan's guess was
      wrong). Two config-driven multipliers, 1.0 = vanilla: `SnapTolerance`
      (default 1.5x, Prefix on `Player.FindClosestSnapPoints` widens the
      vanilla 0.5m snap-point match distance) and `PlaceDistanceMultiplier`
      (default 1.5x, Postfix on `Player`'s constructor widens the vanilla
      5m placement range). Compiles clean against the real local 1.0-era
      assemblies. **Not yet tested in-game.**
- [ ] **Quick slots for gear/potions** ([QuickSlots.cs](../src/MiniMods/ValheimQoL/Patches/QuickSlots.cs)) —
      *(implemented, not yet runtime-tested — UX is a first cut)*. 4 slots
      (F1-F4 by default). Assign: pick up an item in the inventory screen
      (vanilla's own drag state) and press Ctrl+slot key. Use: press the
      slot key alone, anywhere — calls `Humanoid.UseItem`, the same
      dispatch vanilla itself uses on an inventory click, so it already
      routes to equip or consume correctly without us duplicating that
      logic. Assignments persist via `Player.m_customData`, which is
      already saved/loaded with the character — no new save data
      invented. No on-screen UI showing what's assigned (the plan's step
      4) — skipped for now since it's the most speculative part and
      hardest to get right without seeing it in-game; slot assignment
      confirms via a center-screen message instead. Compiles clean.
      **Not yet tested in-game — the assign workflow (drag + Ctrl+key) is
      a guess at good UX, most likely thing to need revisiting Friday.**
- [ ] **Auto-pickup / auto-sort** ([AutoPickupSort.cs](../src/MiniMods/ValheimQoL/Patches/AutoPickupSort.cs)) —
      *(implemented, not yet runtime-tested)*. Turns out vanilla 1.0
      already has auto-pickup built in (`Player.AutoPickup`, toggled by
      the existing "AutoPickup" keybind) — `AutoPickupRangeMultiplier`
      (default 1.5x) just widens its sweep radius via the same
      constructor-Postfix pattern as placement distance. Auto-sort has no
      vanilla equivalent, so it's a real implementation: `Alt+S` while the
      inventory screen is open merges partial stacks and re-lays the grid
      out sorted by item type → name → quality. Compiles clean against the
      real local 1.0-era assemblies. **Not yet tested in-game.**
- [ ] **Craft from nearby containers** ([CraftFromContainers.cs](../src/MiniMods/ValheimQoL/Patches/CraftFromContainers.cs)) —
      *(implemented, not yet runtime-tested)*. Temporarily borrows nearby
      chests' items into the player's own inventory list (by reference,
      respecting ward/privacy checks) around both the "can I build/craft"
      checks and the actual consumption call, then reconciles afterward —
      vanilla's own logic does the real work, nothing reimplemented.
      Config radius, default 10m. Covers piece placement and standard
      multi-ingredient recipe crafting. **Known gap:** recipes flagged
      "require only one ingredient" consume via a different code path
      (`Inventory.RemoveItem` called directly) that's too broadly used
      elsewhere to safely patch the same way — not covered, and uncommon
      enough to accept for now. Compiles clean. **Not yet tested
      in-game.**

## Quests (`src/MiniMods/Quests`)

What I want: real objective-based quests with quest-giver NPC(s), not
just fetch-flavor on top of vanilla.

- [ ] **Quest list, quest-giver placement, reward structure** — *(not
      designed — blocks everything)*. `QuestsPlugin.cs` is currently a
      stub that only logs on load. Prior-art mods (VillageLife,
      CreatureQuests, Marketplace And Server NPCs Revamped) confirm the
      Harmony-patch approach to custom NPCs/quests works — none of that
      is ours to copy, but it's worth reading for architecture ideas
      when this pillar's design actually starts.

## Parking Lot — not yet designed, deliberately deferred

- [ ] **Terraforming tools** (clearing land, ground shaping) — idea noted,
      not designed. Needs to be at the PC to actually test terrain
      changes in-game, so intentionally on hold.
