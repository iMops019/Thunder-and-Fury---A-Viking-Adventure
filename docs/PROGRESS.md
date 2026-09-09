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

- [x] **Custom skill system architecture** — *(core pattern proven on one
      skill, not yet generalized to the other 8 — not yet runtime-tested)*.
      `SkillSystem/SkillXpRedirect.cs`: a generic Prefix on
      `Player.RaiseSkill` (confirmed the real single choke point every XP
      gain funnels through — weapon attacks, running, swimming, sneaking,
      blocking, dodging, jumping, building) redirects any registered
      vanilla `SkillType` into our custom one and skips the original,
      which is what "neutralized" actually means in code. Custom skills
      register via Jotunn's `SkillManager.AddSkill` and reuse vanilla's
      own leveling curve (confirmed from the decompile:
      `Mathf.Pow(level+1, 1.5) * 0.5 + 0.5`) rather than needing a
      bespoke one — satisfies vision.md's "smooth, no artificial walls"
      for free. Vanilla skills are now also hidden from the in-game skill
      list: confirmed every consumer of "what skills does this player
      have" (the skill list UI included) goes through
      `Skills.GetSkillList()`, which builds a fresh list from internal
      storage every call — a Postfix there filters out anything
      registered as redirected, so registering a skill's XP redirect
      hides it too, one call does both halves of "neutralized and
      hidden."
- [x] **Woodcutting** ([SkillSystem/WoodcuttingSkill.cs](../src/Core/SkillSystem/WoodcuttingSkill.cs), [Patches/WoodcuttingPatches.cs](../src/Core/Patches/WoodcuttingPatches.cs)) —
      *(implemented end-to-end, not yet runtime-tested)*. First skill,
      proving the whole loop: registration, XP redirect (vanilla
      WoodCutting → ours, and now hidden from the skill list UI too), and
      per-level tree/log damage scaling (config: +1%/level default —
      confirmed `TreeBase`/`TreeLog.RPC_Damage` both process hits through
      the same `HitData.m_damage` struct, scaled in a Prefix). Chop SPEED
      researched and implemented: vanilla has no literal
      swing-animation-speed stat at all (melee timing is baked into each
      weapon's animation clip, not a scriptable number, and hacking
      `Animator.speed` directly risks desyncing the networked hit-trigger
      timing) — instead, confirmed `Attack.GetAttackStamina()` already
      reduces stamina cost per swing by
      `0.33 * GetSkillFactor(weapon's skill type)`, vanilla's own
      "skill = efficiency" mechanic, just dead now since our redirect
      freezes vanilla WoodCutting. A Postfix adds the same style of
      reduction back sourced from our skill instead (config weight,
      default 0.33 matching vanilla) — more chops per stamina bar, and
      the same "efficiency" framing vision.md itself already uses for the
      future Attack skill, so not a one-off reinterpretation. Level-15
      milestone (2x XP + 25% bonus log yield, config levels/amounts) —
      the yield half reuses the log-drop-table research from RarityLoot's
      LogYieldPatch, reimplemented here rather than shared since Core
      can't depend on RarityLoot. Compiles clean. **Not yet tested
      in-game.**
- [x] **Mining** ([SkillSystem/MiningSkill.cs](../src/Core/SkillSystem/MiningSkill.cs), [Patches/MiningPatches.cs](../src/Core/Patches/MiningPatches.cs)) —
      *(implemented, not yet runtime-tested)*. Second skill, same proven
      pattern as Woodcutting — registration, XP redirect (vanilla
      `Pickaxes` → ours, hidden from the skill list), per-level ore
      damage scaling, and the same stamina-efficiency "speed" stand-in.
      One wrinkle Woodcutting didn't have: ore/rock damage flows through
      two different vanilla components depending on rock type —
      `MineRock5.RPC_Damage` (newer, multi-hit-area rocks) and
      `MineRock.RPC_Hit` (older, single-area) — both patched to cover
      every ore node. **No milestone** — unlike Woodcutting's level-15
      bonus, vision.md never designed a concrete Mining milestone, so
      nothing was invented; just the shared "level → speed + damage to
      ore/trees" gathering mechanic vision.md explicitly names for both
      skills. Compiles clean. **Not yet tested in-game.**
- [ ] **Skill list** — *(designed, first pass; Woodcutting + Mining now
      built)*. Gathering: Mining (built), Woodcutting (built), Fishing,
      Skinning. Production: Smithing, Cooking, Fletching, Building,
      Crafting. Combat: Attack, Strength, Defense (broad OSRS-style stats, not
      per-weapon-type like vanilla).
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

- [ ] **RarityLoot tier list + roll mechanics for ORDINARY vanilla gear**
      — *(still not designed — blocks giving random vanilla items
      Magic/Rare rolls)*. What's now decided and built (see the generic
      affix framework below): the roll/affix *mechanism* itself, and that
      Magic/Rare/Legendary all draw from one shared affix pool. What's
      still undecided: whether/how a normal dropped or crafted vanilla
      item (a found Bronze Sword, say) can roll into Magic/Rare, and at
      what odds — that's a separate, bigger question (which items are
      eligible, hooking every acquisition path in the whole game) from
      what today's session scoped. Only Voltun's Set uses the framework
      so far, and it's always Legendary by definition, not randomly
      rolled.
- [x] **Generic affix/rarity framework** ([Affixes/](../src/MiniMods/RarityLoot/Affixes/), [Patches/ItemRollTrigger.cs](../src/MiniMods/RarityLoot/Patches/ItemRollTrigger.cs), [Patches/AffixApplication.cs](../src/MiniMods/RarityLoot/Patches/AffixApplication.cs)) —
      *(implemented, not yet runtime-tested)*. PoE-style rolled stats:
      confirmed against the real 1.0 decompile that
      `ItemDrop.ItemData.m_customData` (a per-item-instance
      `Dictionary<string,string>`) is genuinely written/read by vanilla's
      own save/load code and survives `ItemData.Clone()` — real
      foundation, not a hack. A 3-affix starter pool (Armor, Max Health,
      %Damage) rolls onto tracked items the moment their `ItemData` is
      cloned (`ItemRollTrigger`, patches `ItemData.Clone()` — one hook
      covers crafting, looting, and future boss drops alike, since they
      all clone through this same method). Bonuses apply via Postfix
      patches on `ItemData.GetArmor`/`GetDamage` and
      `Character.GetMaxHealth` (player-scoped, scans equipped items) —
      deliberately never touches `ItemData.m_shared`, which is the same
      object shared by every instance of that item type; mutating it
      would rewrite every copy in the world, not just one roll. Tooltip
      patch shows the rolled affixes + a color-coded rarity name.
      Compiles clean. **Not yet tested in-game.**
- [ ] **BepInEx.ConfigurationManager as a setup step** — *(recommended,
      not yet added to docs)*. Every material cost/multiplier below is a
      `Config.Bind` entry, so installing that companion mod (a separate
      Thunderstore download, not something this repo builds) gives an
      in-game F1 menu to tune Legendary recipes live, no recompile — this
      is the "dev tool" asked for this session. Just needs a line added
      to the README setup steps, not code.
- [ ] **Stone Pickaxe** ([StonePickaxe.cs](../src/MiniMods/RarityLoot/Items/StonePickaxe.cs)) —
      *(implemented, not yet runtime-tested)*. Jotunn `CustomItem` clones
      the real vanilla `PickaxeAntler` prefab wholesale (model,
      animations, tool tier — still able to mine copper — all inherited
      as-is), then only mining damage is scaled down, by a config
      multiplier (0.6x default) applied to whatever the real cloned
      Antler value turns out to be at runtime rather than a hardcoded
      guess. Recipe (5 Wood + 10 Stone at a Workbench, both config
      amounts) is new content we're choosing, not something extracted
      from vanilla, so no verification question there. **Two strings
      weren't independently verified against this install's binary asset
      data the way the C# hook points elsewhere were** — `"PickaxeAntler"`
      as the base prefab and `"Wood"`/`"Stone"` as requirement item ids
      are standard, well-established Jotunn/Valheim internal names, but
      item/prefab data lives in Unity asset bundles, not the decompiled
      C# assembly, so there was nothing local to grep them out of. Safe
      failure mode if wrong: Jotunn logs a clear "could not resolve
      reference" error on load rather than failing silently. Compiles
      clean. **Not yet tested in-game — first thing to check Friday.**
- [ ] **Skinning Knife** — *(designed, not built — intentionally
      deferred)*. Required to harvest carcasses under the new Skinning
      mechanic, but that mechanic is Core/Pillar 1 territory and Core is
      still just a stub — building the knife item alone right now would
      just be a prop with nothing to do. Pairs with Core's Skinning work
      when that starts, not before.
- [x] **Named-hero gear pattern** ([Patches/ItemRollTrigger.cs](../src/MiniMods/RarityLoot/Patches/ItemRollTrigger.cs)) —
      *(implemented generically)*. `ItemRollTrigger.Register` +
      `ItemRoller` work for any named set, not just Voltun's — a second
      hero set later is new data (an `Items/*.cs` file + config entries),
      no new plumbing.
- [ ] **Voltun's Set** (Hatchet + Pickaxe) ([Items/VoltunsSet.cs](../src/MiniMods/RarityLoot/Items/VoltunsSet.cs)) —
      *(implemented, not yet runtime-tested — one bonus still deliberately
      deferred)*. Acquisition question resolved for now: crafted from
      Wood + Copper + Bronze at the Forge (config amounts), spanning
      Meadows + Black Forest as discussed this session — swapping to a
      boss-drop model later only means moving where
      `ItemRollTrigger.Register` gets called, not touching the affix
      system. Both items clone a real vanilla base (`Hatchet`,
      `PickaxeAntler`) and get a damage + swing-speed multiplier (relative
      to the real cloned values, config-driven) plus 2 rolled Legendary
      affixes from the shared pool. Log yield researched and implemented
      ([Patches/LogYieldPatch.cs](../src/MiniMods/RarityLoot/Patches/LogYieldPatch.cs)):
      `TreeLog.Destroy(HitData)` is where a fully-chopped log spawns its
      "Wood" item drops, but the per-item drop count is an unreachable
      local variable — sidestepped rather than needing a Transpiler, by
      independently rolling and spawning bonus drops in a Prefix (reads
      the tree's own drop table before vanilla's `Destroy()` runs and
      destroys the GameObject those fields live on). **Still deferred, not
      faked:** vision.md's "double XP" needs Core's skill system, which
      doesn't exist yet — pairs with that work, not before. Same
      base-prefab-name verification caveat as Stone Pickaxe. Compiles
      clean. **Not yet tested in-game.**
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
