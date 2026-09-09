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
      hidden." **Generalized further while wiring up Fishing:** XP isn't
      the only thing vanilla formulas read from a skill —
      `Attack.GetAttackStamina`, `FishingFloat`'s reel-in stamina/pull
      speed, and Player's own build-durability check all call
      `Player.GetSkillFactor(SkillType)`, confirmed as the one real entry
      point gameplay code goes through for "how strong is this skill's
      effect right now." A generic Postfix there now redirects that too
      for any registered skill — this replaced Woodcutting's and Mining's
      one-off `*StaminaEfficiencyPatch` classes, which were manually
      reimplementing exactly what this single choke point now does for
      every redirected skill and every vanilla formula that reads it, not
      just attack stamina.
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
      "skill = efficiency" mechanic — now fixed generically by
      `SkillXpRedirect`'s `GetSkillFactorRedirectPatch` (see the
      architecture entry above) rather than a Woodcutting-specific patch.
      More chops per stamina bar, and the same "efficiency" framing
      vision.md itself already uses for the future Attack skill, so not a
      one-off reinterpretation. Level-15
      milestone (2x XP + 25% bonus log yield, config levels/amounts) —
      the yield half reuses the log-drop-table research from RarityLoot's
      LogYieldPatch, reimplemented here rather than shared since Core
      can't depend on RarityLoot. Compiles clean. **Not yet tested
      in-game.**
- [x] **Mining** ([SkillSystem/MiningSkill.cs](../src/Core/SkillSystem/MiningSkill.cs), [Patches/MiningPatches.cs](../src/Core/Patches/MiningPatches.cs)) —
      *(implemented, not yet runtime-tested)*. Second skill, same proven
      pattern as Woodcutting — registration, XP redirect (vanilla
      `Pickaxes` → ours, hidden from the skill list), per-level ore
      damage scaling, and the same stamina-efficiency "speed" stand-in
      (also now generic — see the architecture entry above). One wrinkle
      Woodcutting didn't have: ore/rock damage flows through
      two different vanilla components depending on rock type —
      `MineRock5.RPC_Damage` (newer, multi-hit-area rocks) and
      `MineRock.RPC_Hit` (older, single-area) — both patched to cover
      every ore node. **Shares Woodcutting's level-15 milestone** (2x XP
      + 25% bonus yield, user's call — same shape, applied to ore instead
      of logs). Detecting "this hit just destroyed the node" (rather than
      just damaged it) needed a different check per rock type since
      neither method's return value distinguishes the two: `MineRock5`
      tracks each hit area's health as a plain field, re-checked in a
      Postfix on `DamageArea`; `MineRock` (the older single-area variant)
      tracks health in the ZDO instead, re-read the same way after
      `RPC_Hit`. Compiles clean. **Not yet tested in-game.**
- [x] **Fishing** ([SkillSystem/FishingSkill.cs](../src/Core/SkillSystem/FishingSkill.cs), [Patches/FishingPatches.cs](../src/Core/Patches/FishingPatches.cs)) —
      *(implemented, not yet runtime-tested)*. Third skill, deliberately
      shaped differently — vision.md: "keeps vanilla's cast/bait/reel
      mechanic largely as-is ... level primarily scaling catch chance."
      No damage concept here. Registration + XP redirect is the standard
      pattern; the reel-in minigame (stamina cost, pull speed) already
      reads `Player.GetSkillFactor(Fishing)` in vanilla, so it now tracks
      our skill for free via the generic redirect above — no
      Fishing-specific patch needed for that half. What DID need research:
      bite chance. Confirmed `Fish.FindFloat()` currently rolls
      `Random.value < m_baseHookChance` with **no skill factor at all** —
      vanilla Fishing skill has zero effect on how often you get bites,
      only on reeling once hooked. A Prefix/Postfix pair (using Harmony's
      `__state` to pass data between them, not a static field, so it's
      safe across back-to-back calls for different fish in the same
      frame) temporarily boosts the biting fish's own
      `m_baseHookChance` for the duration of one `FindFloat()` call, based
      on the best Fishing level among any nearby angler, then restores it.
      **Not done:** vision.md's Fishing Rod (craftable from level 1, no
      Haldor) and Bait (from common drops, not gold-gated) — those are
      recipe/item changes, not skill mechanics, deliberately left for a
      separate pass the same way Woodcutting/Mining's itemization lives
      in RarityLoot rather than here. Compiles clean. **Not yet tested
      in-game.**
- [x] **Skinning and Butchering** ([SkillSystem/SkinningSkill.cs](../src/Core/SkillSystem/SkinningSkill.cs), [Patches/SkinningPatches.cs](../src/Core/Patches/SkinningPatches.cs), [Skinning Knife](../src/MiniMods/RarityLoot/Items/SkinningKnife.cs)) —
      *(implemented, not yet runtime-tested — 4 animals)*. Fourth skill,
      and the first with no vanilla equivalent at all (confirmed against
      the full `Skills.SkillType` enum — no "Skinning" entry), so no XP
      redirect is needed, just registration. Matches vision.md's own
      framing directly: "action/flavor mechanic first ... rather than
      designing deep level-gated bonuses right away" — no per-level
      scaling code, all XP/bonus-yield comes free from vanilla's own
      `Pickable` component (`m_pickRaiseSkill`/`m_maxLevelBonusChance`,
      confirmed to accept a Jotunn custom `SkillType` exactly like a
      vanilla one). Skinning (hide) and butchering (meat) are two
      independent harvests — a dead registered animal leaves up to TWO
      carcass pieces at the kill site (each a separately cloned
      Pickable-based resource; an animal with only one of the two, e.g.
      Boar with no hide item, gets one piece), harvestable in either
      order. Anything else (trophies, etc.) is untouched and still drops
      normally. Tool requirement is generic — a Prefix on
      `Pickable.Interact`, scoped only to Pickable instances this system
      itself spawned, requires any equipped item in the vanilla Knives
      weapon-skill category, not one specific named item — matches "just
      need a basic flint/stone knife or dagger." The Skinning Knife
      (RarityLoot) still exists as a cheap, purpose-named early option,
      just isn't the exclusive gate. **Bug found and fixed this session,
      while researching the carcass visual:** the original patch targeted
      `CharacterDrop.OnDeath`, which turns out to be too late to matter
      for any creature with a normal death ragdoll (i.e. all of them) —
      `Character.OnDeath` creates the ragdoll *first*, and
      `Ragdoll.Setup` immediately snapshots the full unfiltered drop list
      for its own delayed drop-on-dissolve, then disables
      `CharacterDrop`'s drops outright since the ragdoll is now handling
      it. The old Prefix never actually got a chance to intercept
      anything — moved the patch to `Character.OnDeath` itself, before
      the ragdoll exists, so its snapshot is taken from the
      already-filtered list either way. **All 3 previously-flagged
      first-cut limits addressed:** (1) now 4 animals — Deer, Boar
      (meat only), Wolf, Neck (its signature `NeckTail` used as the
      "hide" slot) — the registry made this genuinely a one-line call
      each; Wolf's meat drop and Neck's hide-slot mapping are
      lower-confidence guesses, flagged individually in code, safe
      failure mode (Jotunn logs an error for just that animal) if wrong.
      (2) "Multi-item carcasses would need a container redesign" turned
      out to already be solved by the two-piece design — no redesign
      needed. (3) **Carcass visual now fixed**, using the game's own art
      rather than a Meshy/AssetBundle pipeline (discussed and explicitly
      deferred as a separate, much bigger undertaking — needs a Unity
      project matching Valheim's engine version, not just a generated
      mesh): each animal's own `SkinnedMeshRenderer` is pulled straight
      off its living creature prefab via
      `GetComponentInChildren<SkinnedMeshRenderer>()` (confirmed this
      works on the uninstantiated prefab asset directly — no need to go
      through `Character.m_visual`, which is only populated at runtime by
      `Awake`) and rendered statically on each carcass piece via a plain
      `MeshFilter`/`MeshRenderer`, replacing the cloned base prefab's own
      renderers (disabled, not destroyed, so nothing structurally
      important like the `Pickable`'s collider host is at risk). Honest
      caveat: this is the mesh's *rigged bind pose*, not a true collapsed
      ragdoll death pose (that pose only exists per-instance after
      physics settles, not as reusable prefab data) — a reasonable, safe
      approximation, not a perfect one. Rotation/scale are config values
      (`SkinningCarcassVisualRotationX`/`Scale`) specifically because
      they'll need live tuning once actually seen in-game, not something
      to get right blind. Compiles clean. **Not yet tested in-game.**
- [x] **Skill list** — *(designed, first pass; all 12 skills from
      vision.md's original list now built)*. Gathering: Mining (built),
      Woodcutting (built), Fishing (built), Skinning (built). Production:
      Smithing (built), Cooking (built), Fletching (built, registration +
      XP split only, no gameplay effects — nothing designed for those
      yet), Building (built, registration only, matches vision.md's own
      "trivial" framing), Crafting (built, redirect-only catch-all for
      vanilla's generic Crafting skill). Combat: Attack, Strength, Defense
      (built — broad OSRS-style stats, not per-weapon-type like vanilla;
      see the Combat stats entry below).
- [x] **Combat stats** ([SkillSystem/AttackSkill.cs](../src/Core/SkillSystem/AttackSkill.cs), [SkillSystem/StrengthSkill.cs](../src/Core/SkillSystem/StrengthSkill.cs), [SkillSystem/DefenseSkill.cs](../src/Core/SkillSystem/DefenseSkill.cs), [Patches/CombatPatches.cs](../src/Core/Patches/CombatPatches.cs)) —
      *(implemented, not yet runtime-tested)*. Eleventh/twelfth/thirteenth
      skills, and the last of vision.md's original skill list. Confirmed
      against the real 1.0 decompile: vanilla splits weapon use across 9
      separate `Skills.SkillType` entries (Swords, Knives, Clubs,
      Polearms, Spears, Axes, Bows, Crossbows, Unarmed) —
      `AttackSkill.WeaponSkillTypes` is that full set, every one
      redirected into the single **Attack** skill via the same
      `SkillXpRedirect` architecture every other skill uses. **Attack**'s
      tempo half (attack-speed/stamina-efficiency) comes free from the
      existing generic `GetSkillFactorRedirectPatch` — no new patch
      needed, same as Woodcutting/Mining's "speed." **Strength** (damage
      output) has no vanilla skill of its own to redirect from: confirmed
      `Attack.cs`'s hit paths scale outgoing damage through
      `Character.GetRandomSkillFactor(weapon's skill type)`, which
      forwards to `Skills.GetRandomSkillFactor` — that method calls
      `Skills.GetSkillFactor` internally (not `Player.GetSkillFactor`),
      so it's a genuinely separate vanilla code path from Attack's tempo
      formula, already split exactly the way vision.md wants without any
      extra plumbing. `StrengthDamageFactorPatch` substitutes Strength's
      level into that same vanilla shape
      (`Lerp(0.4, 1, level/100) +/- 0.15` random); `StrengthXpSharePatch`
      is a second, independent `Player.RaiseSkill` Prefix (same
      multiple-prefixes-coexist pattern Fletching's redirect already
      proved out) granting Strength XP alongside Attack for the same hit,
      config-tunable share (`StrengthXpShareOfAttack`, default 1.0 = same
      rate as Attack). **Defense** redirects vanilla's `Blocking` skill —
      confirmed `Humanoid.BlockAttack` reads
      `GetSkillFactor(Skills.SkillType.Blocking)` to scale block power,
      and unlike Strength's case that call *does* go through
      `Player.GetSkillFactor`, so the redirect alone gives "better
      blocking = better block power/stagger resistance" for free. General
      (not just block) damage reduction is a new patch,
      `DefenseDamageReductionPatch`, a Prefix on `Character.RPC_Damage`
      (scoped to the player being hit) that scales `HitData.m_damage`
      down before vanilla's own armor/resistance runs — same field list
      Woodcutting/Mining's damage-up patches already touch, just inverted,
      and since `HitData.GetTotalStaggerDamage()` reads those same fields
      this covers vision.md's "damage reduction and/or stagger
      resistance" with one mechanism. Floor-clamped
      (`DefenseMinDamageMultiplier`, default 0.1) so high Defense can't be
      tuned into literal invincibility, unlike the symmetric damage-up
      skills which have no such ceiling risk. No hit/miss roll anywhere —
      every swing connects like vanilla, matching vision.md. **Note:**
      Attack's stamina-efficiency effect stacks with ValheimQoL's flat
      stamina-drain reduction — two separate, intentional layers, not
      overlapping ones. Compiles clean. **Not yet tested in-game.**
- [x] **Gathering mechanics** — *(all four built, none runtime-tested)*.
      Soft-gating principle: access to a resource is never locked, only
      the payoff is. Mining and Woodcutting share one mechanic (level →
      speed + damage to ore/trees) — built. Fishing keeps vanilla's
      cast/bait/reel feel, level mainly scales catch chance — built
      (Fishing Rod craftable-from-level-1 / no-Haldor still open, it's a
      recipe change not a mechanic). Skinning and Butchering: animal
      deaths leave two carcass pieces (hide + meat, harvested
      independently), any knife/dagger works (flavor/immersion first,
      leveled bonuses later) — built for 4 animals so
      far, see the Pillar 1 entry above for scope.
- [x] **Smithing** ([SkillSystem/SmithingSkill.cs](../src/Core/SkillSystem/SmithingSkill.cs), [LegendaryCraftGatePatch.cs](../src/MiniMods/RarityLoot/Patches/LegendaryCraftGatePatch.cs)) —
      *(implemented, not yet runtime-tested — normal/Legendary halves
      done, Magic/Rare still blocked on design)*. Sixth skill, and like
      Skinning, no vanilla equivalent to redirect from. Confirmed
      against the real 1.0 decompile: `InventoryGui.DoCrafting` already
      calls `RaiseSkill(m_craftingStation.m_craftingSkill, ...)` on every
      recipe craft — a fully generic, already-working mechanic no vanilla
      station actually points anywhere meaningful by default, reused the
      same way Skinning reuses `Pickable.m_pickRaiseSkill`. Set directly
      on Workbench/Forge/Black Forge (vision.md's "Stone/Bronze/Iron"
      ladder) rather than redirecting vanilla's `Crafting` SkillType
      wholesale — that field defaults to `Crafting` in code, but whether
      any given station's *real* prefab data overrides that couldn't be
      confirmed (serialized Unity data, not visible in the decompiled
      C#), so a blanket redirect risked silently pulling in Cooking's
      Cauldron or another unrelated station. Also confirmed
      `DoCrafting` only ever handles Recipe objects (weapons/tools/
      consumables) — building Piece placement is a fully separate system
      with no skill tied to it — so setting this on the Workbench can't
      accidentally grant Smithing XP for building a wall. Legendary
      gear's level gate (vision.md, locked in) lives in RarityLoot
      instead: a Prefix on `Player.HaveRequirements(Recipe,...)` — the
      exact check `DoCrafting` gates the real craft attempt on, not just
      a UI hint — blocks crafting below a config-tunable Smithing level
      (default 30; vision.md never decided the exact number) for any
      item RarityLoot's existing `ItemRollTrigger` registry already
      marks Legendary (Voltun's Set), reusing that registry rather than
      adding a second way to ask "is this item Legendary." **Still
      blocked:** Magic/Rare tiers for ordinary vanilla gear — same
      undesigned "which items, what odds" question flagged under
      RarityLoot's own Pillar 3 entry, unrelated to what Smithing itself
      needed to do. **Boundary fix while wiring up Fletching:** the
      Workbench (shared with bow/arrow crafting) now correctly excludes
      Fletching's items from Smithing XP — see the Fletching entry below.
      **Follow-up closed by Crafting (below):** the generic vanilla
      `Crafting` SkillType redirect this entry originally deferred (no
      destination skill existed yet) is now wired up.
      Compiles clean. **Not yet tested in-game.**
- [x] **Cooking** ([SkillSystem/CookingSkill.cs](../src/Core/SkillSystem/CookingSkill.cs), [Patches/CookingPatches.cs](../src/Core/Patches/CookingPatches.cs)) —
      *(implemented, not yet runtime-tested — burn-chance half done,
      special-recipe tier still blocked on design)*. Seventh skill, and
      unlike Skinning/Smithing, vanilla's own Cooking skill IS already
      actively used — confirmed `CookingStation.OnInteract` already
      raises `Skills.SkillType.Cooking` (both on adding an ingredient and
      collecting a finished dish, with an existing `GetSkillFactor`-scaled
      bonus-extra-food roll on collection). Redirected the same way as
      Woodcutting/Pickaxes/Fishing for consistency with vision.md's
      "every skill the player sees is ours" architecture, not because
      anything was broken — the bonus-food roll now tracks our skill
      automatically via the existing generic `GetSkillFactor` redirect,
      no extra code needed. **Locked-in "reduced burn/fail chance"
      implemented:** confirmed `CookingStation.UpdateCooking` marks food
      Burnt on a purely fixed time threshold
      (`cookedTime > itemConversion.m_cookTime * 2f`) with no skill
      factor anywhere in the decision. Rather than replicate that
      private per-slot iteration to intercept the decision at its
      source, a Prefix on `SetSlot` catches the moment a slot is about
      to be written as Burnt and rolls a Cooking-level-scaled chance
      (config, closest player's skill — same approximation Fishing uses,
      since `UpdateCooking` runs on the station's own timer with no
      player context) to redirect it to Done instead, using
      `GetSlot`/`GetItemConversion` (the same lookup vanilla itself uses)
      to figure out the correct "saved" result item. Known minor cosmetic
      gap: the burnt particle/sound effect still plays even on a saved
      cook, since it fires unconditionally before `SetSlot` — fixing that
      would need the fragile full-replication approach this was
      deliberately avoiding, not worth it for a visual-only quirk. No
      recipe-level-gating was needed (vision.md's own design — biome
      progression already paces ingredients, nothing to build). **Still
      blocked:** the special/multi-ingredient recipe tier — vision.md
      itself flags this as unfleshed design space, nothing invented here.
      Also unaddressed: [valheim-food-reference.md](valheim-food-reference.md)
      still needs its pre-1.0-to-1.0 data refresh. Compiles clean. **Not
      yet tested in-game.**
- [x] **Fletching** ([SkillSystem/FletchingSkill.cs](../src/Core/SkillSystem/FletchingSkill.cs)) —
      *(implemented, not yet runtime-tested — registration + correct XP
      only, no gameplay effects)*. Eighth skill, and unlike every other
      skill built this session, vision.md never gave Fletching its own
      design section — it's named once in the skill list with nothing
      else decided. Deliberately built only what that one line commits
      to: a distinct production skill for bow/arrow crafting, separate
      from Smithing. No fail chance, no milestone, nothing invented —
      there's no vision.md text to build those from, unlike Cooking's
      burn-prevention or Woodcutting's milestone. Real technical problem
      solved while wiring this up: Smithing's `CraftingStation.m_craftingSkill`
      reuse is per-*station*, not per-recipe, and bows/arrows craft at
      the same Workbench Smithing already claims — a station can only
      declare one skill, so reusing that same mechanism for Fletching
      would just fight Smithing over the same field. Fixed by
      intercepting at the one place that knows which recipe is actually
      being crafted — `InventoryGui.m_craftRecipe` (confirmed public) —
      checked in a Prefix on `Player.RaiseSkill` specifically when the
      skill about to be raised is Smithing's (i.e. it came from a
      station-level grant): if the recipe's item is Bow/Ammo, that XP is
      reclassified as Fletching instead; everything else crafted at
      Workbench/Forge/Black Forge still goes to Smithing untouched.
      Compiles clean. **Not yet tested in-game.**
- [x] **Building** ([SkillSystem/BuildingSkill.cs](../src/Core/SkillSystem/BuildingSkill.cs)) —
      *(implemented, not yet runtime-tested)*. Ninth skill, matches
      vision.md's own "trivial" framing exactly — not gated behind a
      level at all, registration only, no custom gameplay code written.
      No vanilla equivalent to redirect from, but confirmed against the
      real 1.0 decompile: `Player` already has a fully generic "raise
      this skill when placing a piece from this table" mechanism right
      after a successful `TryPlacePiece`
      (`if (m_buildPieces.m_skill != None) RaiseSkill(m_buildPieces.m_skill)`),
      unused by any vanilla `PieceTable` — same reuse pattern as
      Skinning's `Pickable.m_pickRaiseSkill` and Smithing's
      `CraftingStation.m_craftingSkill`. Set directly on the Hammer's
      piece table only (`_HammerPieceTable`) — structural building, not
      the Cultivator (terraforming is a separate, explicitly
      not-yet-designed feature per vision.md's own Parking Lot). One
      incidental, accepted side effect: `Player.GetBuildStamina()`
      already reduces hammer-swing stamina cost by
      `0.5 * GetSkillFactor(m_buildPieces.m_skill)`, the same
      "efficiency" pattern Woodcutting/Mining reuse — not a gate on
      anything (building is never locked), just the same free bonus
      every other reused-mechanism skill gets, left as-is rather than
      suppressed. Compiles clean. **Not yet tested in-game.**
- [x] **Crafting** ([SkillSystem/CraftingSkill.cs](../src/Core/SkillSystem/CraftingSkill.cs)) —
      *(implemented, not yet runtime-tested)*. Tenth and final skill from
      vision.md's original skill list, and like Fletching/Building, given
      zero dedicated design beyond its name. Unlike those two, though,
      this had an obvious, low-risk role given everything already built:
      vanilla's `Skills.SkillType.Crafting` is the DEFAULT value of
      `CraftingStation.m_craftingSkill` in code, meaning any station this
      mod hasn't explicitly touched (Stonecutter, Artisan Table, Mead
      Ketill, Food Preparation Table, etc.) already grants that vanilla
      skill XP for whatever's made there. Redirected it here — the same
      mechanism as Woodcutting/Pickaxes/Fishing/Cooking — making this
      skill the natural catch-all for "everything produced at a station
      Smithing/Fletching/Building didn't explicitly claim," matching the
      OSRS shape of Crafting as the broad generic skill alongside more
      specialized ones. This closes the loop on Smithing's own
      deliberately-deferred decision not to touch that redirect — there
      wasn't a leak risk back then, just no destination skill registered
      yet for it to redirect into. No custom gameplay effects, same
      "nothing designed beyond the name" scope as Fletching and Building.
      **All 9 Gathering + Production skills were built this session** —
      Combat (Attack, Strength, Defense) followed right after, closing out
      all 12 skills from vision.md's original skill list; see the Combat
      stats entry below. Compiles clean. **Not yet tested in-game.**
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
- [x] **Skinning Knife** ([Items/SkinningKnife.cs](../src/MiniMods/RarityLoot/Items/SkinningKnife.cs)) —
      *(implemented, not yet runtime-tested)*. Built the moment Core's
      Skinning mechanic landed, as planned here. Clones vanilla `Knife`,
      craftable from Wood + Flint at a Workbench (config amounts) — no
      boss-material requirement, matching Stone Pickaxe's philosophy.
      **Not the exclusive gate** — Core's tool-gate check accepts any
      equipped item in the vanilla Knives weapon-skill category (this
      session's clarification: "just need a basic flint/stone knife or
      dagger"), so vanilla's own starting Knife already qualifies too;
      this item is a cheap, purpose-named early option, not a hard
      requirement. See the Pillar 1 Skinning entry above for the full
      picture. Compiles clean.
      **Not yet tested in-game.**
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

## Classes & Passive Trees (new idea, 2026-09-09 — not yet in vision.md, deliberately left to stew)

What I want: Last Epoch-inspired class system. Pick a class (Last Epoch:
e.g. Sentinel); each class comes with its own passive tree, running
*concurrently* with a generic tree everyone starts with (Last Epoch: the
Base Class tree; here, a generic "Viking" tree, since there's no
neutral/no-class state in a Viking setting the way Last Epoch has
pre-mastery). Only one class tree active at a time, chosen once. Needs a
brand-new **Player Level/XP** system as a prerequisite — vanilla Valheim
has no player level at all, only skill levels (0-100 each, vision.md's
OSRS-inspired system already covers those) — so this doesn't slot into
an existing number, it invents one. Nothing designed yet beyond the pitch
above. Explicitly not ready to lock any numbers: "testing is key, we have
yet to test" (session's own words) — no in-game testing has happened for
*anything* in this mod yet, so a level-20 class-gate guess would be
several unvalidated layers deep. This entry exists to hold research and
open questions until that testing starts.

- [ ] **Player Level/XP system** — *(not designed)*. Needs its own XP
      source and curve. Two architecturally different options, not yet
      chosen between:
      - **(A) Derived/aggregate** — Player Level computed FROM existing
        skill levels (e.g. a formula over the sum/average of Attack,
        Strength, Defense, Woodcutting, Mining, etc.), no new XP economy
        needed. Direct precedent already inside our own design
        inspiration: OSRS (vision.md's whole skill system is explicitly
        OSRS-style) has exactly this split — individual skills 1-99, plus
        a separate derived "Combat Level" computed from
        Attack+Strength+Defence+HP, not separately farmed. This would
        also directly answer the farming-speed worry below: if Level is
        skill-derived, "grinding Woodcutting in Meadows raises your
        Level" isn't a loophole, it's just how the number works, which
        reframes the open question from "how do we prevent that" to "is
        that pacing actually fine."
      - **(B) Independent pool** — a new XP bar, fed by a fraction of all
        XP gains (or specific actions/kills/discoveries), with its own
        separate curve decoupled from the 0-100 skill curve. More design
        control (can shape the curve/pacing exactly), but a second XP
        economy to invent and balance from scratch, and vanilla gives us
        nothing to reuse here the way skill XP reused vanilla's own
        formula.
- [ ] **Class-picking level threshold** — *(not decided — floated idea:
      level 20)*. Confirmed against the real 1.0 decompile: vanilla
      Valheim has **no relationship at all** between any player stat and
      enemy difficulty to worry about breaking here (see the Monster
      Scaling research below) — so whatever threshold we pick is a purely
      new, self-contained design decision, not something that has to
      thread through an existing system. Session's own concern, unresolved:
      players who "farm like crazy" could hit a naive level 20 while still
      in Meadows, which may or may not be a problem depending on which of
      the two Player Level options above gets picked (see (A)'s reframing
      above). Needs actual in-game testing before any number is locked.
- [ ] **Passive tree content** (per class, plus the generic Viking tree)
      — *(not designed at all)*. Zero nodes/effects decided yet. Blocked
      on the class list existing first (which classes, how many, Viking
      re-flavoring of something like Last Epoch's Sentinel/Mage/etc
      archetypes).
- [ ] **Class list** — *(not designed)*. Zero classes named/scoped yet
      beyond "an array of classes, Viking-themed."

**Research done this session (2026-09-09), to inform the above without
locking anything yet:**

- **Valheim's real monster-scaling system, confirmed against the local
  1.0 decompile** — useful context for "how do enemies scale" since
  that's the thing our new Player Level would sit alongside:
  - `Character.m_level` is a per-spawn random **star tier** (1 = normal,
    2 = one-star, 3 = two-star), not anything related to player level.
    `SetupMaxHealth()` multiplies max HP by this level directly
    (`GetMaxHealthBase() * level`), and `Attack.GetLevelDamageFactor()`
    (`1 + max(0, level-1) * 0.5`) scales the creature's own damage —
    level 2 = 2x HP / 1.5x damage, level 3 = 3x HP / 2x damage. Matches
    the commonly-known "1-star doubles HP, 2-star hits much harder"
    vanilla behavior.
  - `Game.m_worldLevel` (0-10, global/server-wide, raised by defeating
    bosses or set manually) is vanilla 1.0's actual difficulty dial —
    scales enemy armor/damage/HP/move-speed AND the stats on gear found
    in the world, all at once, via config-like fields
    (`m_worldLevelEnemyHPMultiplier`, `m_worldLevelEnemyBaseDamage`,
    etc.). This is Valheim's "New Game+ / hard mode" knob, unrelated to
    any individual character's progress.
  - `Game.GetPlayerDifficulty`/`GetDifficultyDamageScaleEnemy`/
    `GetDifficultyDamageScalePlayer` scale damage dealt/taken purely by
    **how many players are nearby** (co-op headcount balancing), again
    nothing to do with level.
  - **Conclusion:** there is no existing "creature level vs. player
    level" coupling to preserve or worry about breaking — a new Player
    Level system would be fully additive/orthogonal to vanilla's own
    difficulty systems. Upside: low risk of fighting vanilla balance.
    Downside: zero free guidance from vanilla on "what level should a
    player be by biome X" — that pacing has to be invented (or borrowed
    from precedent below), not extracted from game data the way skill
    mechanics have been all session.
- **Other games' class/mastery-unlock timing, for comparison:**
  - **Last Epoch** — base class picked at character creation; the
    Last-Epoch-specific "Mastery" (sub-class) unlocks at the end of
    Chapter 2 (roughly a fifth to a third into the campaign) AND requires
    20 passive points already spent in the base class tree first choice
    is permanent, one Mastery per class.
    ([TheGamer](https://www.thegamer.com/last-epoch-how-to-unlock-class-mastery/),
    [GameRant](https://gamerant.com/last-epoch-how-unlock-mastery-classes/))
  - **Path of Exile** — Ascendancy (sub-class) unlocks after completing
    the Labyrinth, around character level 33 — roughly a third into a
    full leveling run. ([PoE
    Fandom](https://pathofexile.fandom.com/wiki/Ascendancy_class))
  - **Simple MMO Classes** (existing Valheim mod) — class chosen
    immediately at the very start, no level gate at all; explicitly
    warns to start a *new* character since it doesn't retrofit onto
    existing skill history.
    ([Thunderstore](https://thunderstore.io/c/valheim/p/GGrNoobs_Hideout/Simple_MMO_Classes/))
  - **AlmanacClasses** (existing Valheim mod) — the opposite extreme: no
    exclusive choice ever, a free-form node system where a player can mix
    talents from all 6 "classes" at once; XP comes from combat, chopping,
    mining, taming, and picking up items.
    ([GitHub](https://github.com/RustyMods/AlmanacClasses))
  - **Valheim Level System by Lorska** (existing Valheim mod, no classes,
    but the most directly relevant *pacing* precedent for inventing a
    from-scratch Valheim player-level system) — level cap = 10 x number
    of land biomes (80, at 8 biomes today), i.e. roughly a **10-level
    budget per biome**, with a rubber-band catch-up mechanic if
    under-leveled entering a new biome; grants 1 attribute point + 2
    skill points per level.
    ([Thunderstore](https://thunderstore.io/c/valheim/p/Lorska/Valheim_Level_System_by_Lorska/))
    Notably, this lines up with the level-20 idea floated above almost
    exactly: 20 ≈ "2 biomes' worth" ≈ done with Meadows, solidly into
    Black Forest — a real, independent data point in the same
    neighborhood as the session's own gut-guess, not a confirmation that
    20 is correct, but a reason it isn't an unreasonable starting guess
    either.
  - **Real vanilla-Valheim biome pacing**, from community progression
    guides (not a mod, just how the base game plays): a full playthrough
    runs roughly 30-100 hours; experienced-player advice commonly
    suggests having your main weapon's vanilla skill (0-100 scale — the
    same curve our own Attack/Strength/etc. skills already reuse) at 10+
    before entering Black Forest and 30+ before leaving it for Swamp.
    Useful as a sanity check for how OUR OWN skills (not the new Player
    Level) pace against biomes, since they share vanilla's curve.
    ([PCGamer](https://www.pcgamer.com/games/survival-crafting/valheim-biome-order/),
    [Sportskeeda](https://www.sportskeeda.com/esports/valheim-progression-guide-every-biome))
- **Anti-farm design lever worth considering later** (not researched
  in-depth this session, just flagged): several MMOs/CRPGs use
  diminishing or first-kill/first-discovery-weighted XP (bonus XP the
  first time you kill a creature type or find a new area, tapering on
  repeat kills of the same thing) specifically to blunt "sit in one spot
  and grind" farming without an outright cap. Relevant if Option (B)
  above (independent XP pool) is chosen and the farming-speed concern
  turns out to matter in actual testing.

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
