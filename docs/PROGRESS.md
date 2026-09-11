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
      needed. (3) **Carcass visual: attempted, failed, simplified**
      (2026-09-10). First attempt pulled each animal's own
      `SkinnedMeshRenderer` off its living creature prefab and rendered
      it statically on the carcass via a plain `MeshFilter`/`MeshRenderer`
      — compiled clean, but confirmed live in-game to render as fully
      invisible (no error logged, so the mesh genuinely was found; the
      likely cause is a Valheim creature shader expecting per-instance
      skinning data a static mesh copy doesn't provide, but this
      couldn't be conclusively diagnosed further — this codebase has no
      way to inspect live Unity shader/material behavior, only C#
      decompilation). Rather than keep guessing at a problem outside
      what's actually verifiable here, simplified to clone
      `Pickable_MeatPile` directly instead of a mushroom — a real,
      already-working vanilla pickup visual, confirmed to exist via the
      same live ZNetScene dump used for the base-prefab-name fixes. All
      the mesh-grafting code (and its now-dead
      `SkinningCarcassVisualRotationX`/`Scale` config entries) were
      removed rather than left half-working. Trade-off accepted on
      purpose: every carcass now looks like a generic meat pile rather
      than the specific animal, in exchange for something that reliably
      renders. Compiles clean, deployed. **Not yet re-tested in-game.**
- [x] **Skinning channel (hold-to-harvest flavor mechanic)**
      ([Patches/SkinningChannelPatch.cs](../src/Core/Patches/SkinningChannelPatch.cs)) —
      *(implemented 2026-09-10, user's own ask: "a bit of a flavor
      mechanic," not yet re-tested in-game)*. Instant pickup on a carcass
      felt too abrupt, so harvesting one now takes a config-tunable hold
      (`SkinningChannelDuration`, default 2.5s) instead of an instant tap.
      Deliberately reuses vanilla's own action-progress bar
      (`Hud.m_actionBarRoot`/`m_actionProgress`/`m_actionName`, confirmed
      public via decompile — the same UI eating-with-a-delay already
      uses) via a Postfix on Hud's own `UpdateActionProgress`, rather than
      building a new custom bar — looks and feels like an existing
      vanilla timed action. Vanilla's own "minor action queue" that
      normally drives that bar is private with a closed
      Equip/Unequip/Reload-only `ActionType` enum (confirmed via
      decompile) — not something another mod can add a custom entry to,
      hence driving the bar directly instead. A Prefix on
      `Pickable.Interact` (scoped to `SkinningSystem.IsCarcass` only,
      every other Pickable in the game is untouched) accumulates hold
      time across repeated `Interact(hold: true)` calls and blocks the
      original method until the duration elapses; release is inferred
      (no explicit vanilla "let go" event exists) by checking whether a
      fresh Interact call happened within the last frame, with a
      one-frame tolerance since Hud.Update and Player.Update have no
      guaranteed relative execution order. Compiles clean, deployed.
      **Not yet tested in-game.**
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
      adding a second way to ask "is this item Legendary." **Resolved
      2026-09-10, see Pillar 3's ordinary-gear entry:** Magic/Rare tiers
      for ordinary vanilla gear are NOT level-gated at all, by design —
      only Legendary gets the hard craft-level gate this patch
      implements; a random Magic/Rare roll isn't something a player
      deliberately chooses to craft the way a named Legendary recipe is.
      **Boundary fix while wiring up Fletching:** the
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
      progression already paces ingredients, nothing to build). **Special/
      multi-ingredient recipe tier: resolved 2026-09-10, no new code
      needed.** This is what vanilla's own Cauldron already is — a
      normal multi-ingredient `Recipe` (same crafting system as
      Workbench/Forge), not `CookingStation`'s single-ingredient
      `ItemConversion` this file's own patch touches. Jotunn's
      `CraftingStations.Cauldron` was already a valid station value, and
      the Dev Tool's Recipe Creator (built later the same session)
      already supports authoring a Cauldron recipe with up to 4
      ingredients and an optional skill/level gate — so the "design
      space to flesh out" is now purely content (which specific dishes),
      buildable by the user directly through that tab, not an
      architecture gap. **One real, flagged limit:** a dish granting
      health + stamina simultaneously needs no new support (plain vanilla
      food fields), but one granting an EXTRA buff on top (vision.md's
      own "+ something extra like attack speed" example) would need a
      custom status effect on the food item, which the Item Creator
      doesn't expose yet (only stat multipliers) — not built, flagged for
      whenever that's actually wanted. Also still unaddressed:
      [valheim-food-reference.md](valheim-food-reference.md) still needs
      its pre-1.0-to-1.0 data refresh. Compiles clean. **Not yet tested
      in-game.**
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

- [x] **Scope decision (locked in 2026-09-10):** additive panels on top
      of vanilla's UI, not a full overhaul. Made fast, on purpose (user's
      own call, prioritizing finishing the mod over more design
      discussion) — the additive path reuses the exact `GUIManager`
      toolkit already proven extensively building the Dev Tool overlay,
      versus a full overhaul's much higher, less-proven risk.
- [x] **Character Sheet panel** ([Core/UI/CharacterSheetPanel.cs](../src/Core/UI/CharacterSheetPanel.cs)) —
      *(implemented, not yet runtime-tested)*. First real Pillar 2
      deliverable — a `C`-toggled (config-tunable) panel listing all 12
      skills with their current level, in one place. Deliberately reads
      skills by name via Core's own `SkillRegistry` (built earlier the
      same session for the Dev Tool's Recipe Creator gate picker) rather
      than iterating `Skills.GetSkillList()` and trying to resolve a
      display name from a `SkillType` — confirmed via decompile that
      `SkillDef` has no display-name field for custom skills at all
      (naming is a Jotunn-internal localization detail this doesn't need
      to reach into). Toggle is guarded against `Chat.HasFocus()` and
      `TextInput.IsVisible()` so pressing "C" while typing a chat message
      doesn't pop the panel open. Compiles clean. **Not yet tested
      in-game.**
- [ ] **Expanded inventory screen** — *(not built, flagged not dropped)*.
      Needs real surgery on `InventoryGui`'s own grid dimensions — a
      bigger, riskier undertaking than a read-only display panel, and
      deliberately not bundled into the same pass as the Character Sheet.
      ValheimQoL's bigger stack sizes (Pillar 4) already cover "carry
      more," just not "more grid slots."

## Pillar 3: Gear & Materials (`src/MiniMods/RarityLoot`)

What I want: new items/equipment built from Valheim's existing assets
(retextures, recombinations, stat variants) rather than modeled from
scratch, tied into the skill system for level-gated crafting.

- [x] **RarityLoot tier list + roll mechanics for ORDINARY vanilla gear**
      ([Patches/ItemRollTrigger.cs](../src/MiniMods/RarityLoot/Patches/ItemRollTrigger.cs)) —
      *(designed and implemented 2026-09-10, not yet runtime-tested)*.
      Closes the long-open question: **eligibility** is any weapon or
      armor-slot item, full stop — reuses `ItemRoller`'s own
      `item.IsWeapon()`/`IsArmorSlot()` type check (now `public`, shared
      rather than duplicated) instead of a hand-maintained item-name list,
      so every vanilla weapon/armor piece qualifies automatically, no
      research-per-item needed. **Acquisition paths:** already solved
      before this session even started — `ItemData.Clone()` was already
      confirmed to be the one hook every crafted/looted/dropped item
      instance passes through, so the existing fixed-tier Postfix just
      got a second branch rather than needing a new hook. **Odds**
      (config-tunable, vision.md never specified numbers so these are
      starting guesses, not locked-in balance): 1.5% Rare, 8% Magic,
      checked in that order so Rare's odds aren't shadowed by Magic's
      larger slice; 2 affixes on a Rare roll, 1 on Magic (Legendary stays
      at 2, `LegendaryAffixCount`). **Legendary is deliberately excluded**
      from this random pool — vision.md frames it as named/hand-crafted
      (Voltun's Set), never randomly rolled onto an ordinary item.
      **This also resolves Pillar 1's open Smithing question** ("Magic/Rare
      tier level requirements not yet decided"): they're NOT level-gated
      at all, by design — vision.md's own wording singles out Legendary
      specifically as the one deliberately hard-gated tier, and a random
      roll isn't something a player "chooses" to craft the way a named
      Legendary recipe is, so gating the roll itself wouldn't fit the
      same mechanic. **Deliberately not built this pass, flagged as a
      good follow-up:** scaling craft-time odds by the crafting player's
      Smithing level (fits vision.md's "gate rewards" progression
      principle nicely) — doing that correctly needs an earlier hook
      point than this generic `Clone()` Postfix (to know who's crafting
      without double-rolling), which is real additional work. Compiles
      clean. **Not yet tested in-game.** **Superseded for kill drops** by
      the biome-tiered ambient drop system below — this flat roll still
      governs crafted/looted items exactly as before (nothing here
      changed), it just no longer covers "a monster drops a rarity weapon
      or armor piece," which is now its own separate, biome-gated
      mechanism.
- [x] **Biome-tiered ambient rarity drops on kill**
      ([Loot/BiomeTier.cs](../src/MiniMods/RarityLoot/Loot/BiomeTier.cs),
      [Loot/MaterialTierMap.cs](../src/MiniMods/RarityLoot/Loot/MaterialTierMap.cs),
      [Loot/ItemTierClassifier.cs](../src/MiniMods/RarityLoot/Loot/ItemTierClassifier.cs),
      [Loot/BiomeGearCatalog.cs](../src/MiniMods/RarityLoot/Loot/BiomeGearCatalog.cs),
      [Patches/AmbientBiomeDropPatch.cs](../src/MiniMods/RarityLoot/Patches/AmbientBiomeDropPatch.cs)) —
      *(implemented 2026-09-10, not yet runtime-tested)*. The redesign the
      user asked for directly: "a Greybeard in Meadows can drop up to the
      highest tier able to be made in Meadows... you can't mine copper
      till Black Forest, so Rare Copper gear in low-level Meadows sounds
      super OP." Deliberately its own mechanism, layered on top of
      everything above rather than replacing it — every creature's normal
      loot table (trophies, meat, materials) is completely untouched and
      still uncapped, matching the user's explicit "regular loot tables
      are fine" carve-out.
      **Classifying items by tier without hand-typing a biome item list**
      (this session already got burned twice doing that — Hatchet/Knife/
      MushroomYellow): a weapon/armor item's tier is derived from the
      highest-tier raw material its own `Recipe.m_resources` actually
      requires (confirmed public via decompile), checked against a small,
      mostly-confirmed material→tier map (Wood/Stone/Flint = 0 through
      Flametal = 6). Any item whose recipe references a material not in
      that map is silently excluded from every biome's pool rather than
      guessed — the safe failure direction for exactly the exploit this
      feature exists to prevent.
      **The roll itself**, per the user's own two-stage description: a
      Harmony Postfix on `Character.OnDeath` (fires once per kill,
      regardless of how many vanilla drops it produces — no separate cap
      needed to satisfy "allow only 1 drop," one Postfix call is already
      exactly one attempt) rolls `AmbientDropChance` first (does anything
      drop at all this kill), then — if that hits — a second roll decides
      Magic vs. Rare vs. Legendary (`AmbientRareShare`/`AmbientLegendaryShare`,
      Legendary only reachable if the creature is on the new "Legendary
      Drop Sources" list below; for any other creature that share folds
      into Rare instead of being lost). Biome comes from
      `WorldGenerator.instance.GetBiome(deathPosition)` — the kill
      location, not the killer's, so a ranged kill across a biome border
      resolves correctly. A random slot (Weapon or one of the 4 armor
      slots) and a random tier-eligible base item for that biome get
      picked from `BiomeGearCatalog` (built lazily from
      `ObjectDB.m_recipes` the same way `VanillaItemCatalog` already
      works), spawned via the confirmed-public `ItemDrop.DropItem` static
      helper, then re-rolled with `ItemRoller.Roll` (a new
      `ItemRoller.ClearRoll` first discards whatever the generic
      Clone()-Postfix ordinary-gear roll already did to that same
      instance, so the two mechanisms never stack on one item).
      **"Add a field to what can drop the legendaries"** (the user's own
      first ask this round): a new creature-level eligibility list, not a
      per-item one — the ambient system picks a random base item at kill
      time, so eligibility has to live on the creature. Same "dumb
      registry in Core, mini-mod owns persistence/UI" split
      SkillRegistry/QuestRegistry already established:
      [Core/Loot/LegendaryDropSourceRegistry.cs](../src/Core/Loot/LegendaryDropSourceRegistry.cs)
      is the shared `HashSet<string>`, DevTool's Creature Creator tab owns
      the new "Legendary Drop Sources" section (creature picker + Add/
      Remove list, JSON-persisted) — see the Pillar 5 entry below.
      **Side discovery while decompile-verifying this**: found (and fixed)
      a live regression in two already-shipped patches — see the
      "Accessibility regression found" entry below. Compiles clean, 0
      errors, full solution build. **Not yet tested in-game.**
- [x] **Accessibility regression found + fixed: `Character.m_nview` /
      `ZNetView.GetPrefabName()`** ([Core/Utils/PrefabNameHelper.cs](../src/Core/Utils/PrefabNameHelper.cs)) —
      *(found and fixed 2026-09-10 while decompile-verifying the biome
      drop feature above)*. Both are now non-public on the real (non-
      publicized) game assembly — confirmed via ilspycmd against the
      actual `assembly_valheim.dll`, timestamped the same day as the
      in-session hotfix `DiscoverBiomePatch`'s own comment already
      documents breaking `Player.AddKnownBiome`. The local publicized
      reference assembly still shows both as public (regenerated slightly
      *after* that real assembly, so not stale in the usual sense —
      publicizing just makes everything compile-time accessible
      regardless of the real access modifier), which is exactly the
      "compiles fine, throws `FieldAccessException` at runtime" trap this
      project's own verify-before-build lesson exists to catch. This was
      silently broken in two already-shipped, already-"compiles clean"
      patches using the same `__instance.m_nview.GetPrefabName()` pattern:
      Core's Skinning carcass detection
      ([Patches/SkinningPatches.cs](../src/Core/Patches/SkinningPatches.cs))
      and Quests' KillCreature objective tracking
      ([Patches/QuestTrackingPatches.cs](../src/MiniMods/Quests/Patches/QuestTrackingPatches.cs))
      — neither had been re-tested in-game since the hotfix, so neither
      had actually been caught failing yet. Fixed all three call sites
      (the two above plus the new biome-drop patch) to use a new shared
      helper instead, built entirely from confirmed-public APIs:
      `Component.GetComponent<ZNetView>()` → `ZNetView.GetZDO()` →
      `ZDO.GetPrefab()` (a name hash) → `ZNetScene.GetPrefab(int)`
      (resolves the hash back to the real prefab GameObject, whose
      `.name` is never `"(Clone)"`-suffixed). Also found and fixed the
      same class of bug on `CharacterDrop.m_dropsEnabled` (also now
      private) in the same Skinning file — no clean public getter exists
      for that one, so it's read via Harmony's own `Traverse` helper
      instead, the sanctioned way to reach a private field from outside
      its declaring type. **Recommend a quick in-game re-test of Skinning
      and any active KillCreature quest** next playtest, since this
      confirms they were silently broken until just now.
- [x] **Four wrong base-prefab-name guesses found and fixed via live
      in-game testing** (2026-09-10) — the very first real relaunch after
      this session's BepInEx/build fixes surfaced exactly the risk this
      project's own comments had flagged but never actually confirmed:
      four `CustomItem`/`CustomPiece` clones failing outright because
      their guessed base prefab name doesn't exist post-1.0. Rather than
      guess a fifth time, added a temporary diagnostic (deleted once its
      job was done) that dumped the real candidate names straight from
      the running game's own `ObjectDB`/`ZNetScene` to the BepInEx log,
      plus one direct in-game check (the player's own inventory tooltip)
      to break a genuine tie between two similarly-named real items.
      Fixed:
      - [Items/VoltunsSet.cs](../src/MiniMods/RarityLoot/Items/VoltunsSet.cs):
        `"Hatchet"` → `"AxeStone"` — 1.0 replaced the old singular
        starting hatchet with a Stone→Flint progression; confirmed
        `AxeStone` is the real one via the player's own tooltip reading
        "Stone Axe."
      - [Items/SkinningKnife.cs](../src/MiniMods/RarityLoot/Items/SkinningKnife.cs):
        `"Knife"` → `"KnifeFlint"`.
      - [Quests/AdventureBoard.cs](../src/MiniMods/Quests/AdventureBoard.cs):
        `"piece_sign"` → `"sign"` (no `piece_` prefix; vanilla just calls
        it `sign`).
      - [Core/Patches/SkinningPatches.cs](../src/Core/Patches/SkinningPatches.cs):
        `"MushroomYellow"` → `"Pickable_Mushroom_yellow"` — vanilla
        Pickable-component world objects use a `Pickable_` prefix the
        original guess missed entirely.
      All four compile clean, deployed, and confirmed cleanly registering
      in-game (2026-09-10 relaunch) — no more "not valid, skipping
      registration" errors.
- [x] **Found and fixed a serious hidden bug via live testing: picking up
      ANYTHING that raises skill XP was silently failing game-wide**
      (2026-09-10). User report: killed Deer, saw the correct "Pickup
      [hide/meat]" hover text on the carcass pieces but pressing E did
      nothing and they never disappeared ("invincible"); separately,
      wild mushrooms/berries/veggies couldn't be picked up at all, while
      Flint (which doesn't raise any skill on pickup) worked fine. The
      BepInEx log's actual stack trace nailed the shared root cause
      immediately: `Pickable.Interact()` calls `Player.RaiseSkill()`
      internally for anything that grants skill XP on pickup, and that
      call was hitting the exact `InventoryGui.m_craftRecipe`
      `FieldAccessException` documented two entries up
      ([Core/SkillSystem/FletchingSkill.cs](../src/Core/SkillSystem/FletchingSkill.cs),
      fixed the same session) — the unhandled exception aborted
      `Interact()` partway through, before vanilla's own "grant the item,
      remove/decrement the pickable" step ever ran. One fix (the
      `Traverse`-based `m_craftRecipe` read) therefore resolved all three
      symptoms at once: mushroom/berry/veggie pickup, and the Skinning
      carcasses actually disappearing and granting their item. **Also
      found proactively while chasing this**: `MineRock.m_nview`
      (used by [Core/Patches/MiningPatches.cs](../src/Core/Patches/MiningPatches.cs)'s
      milestone-yield bonus) is the same "private on the real assembly,
      public on the stale-relative-to-behavior publicized one" issue,
      fixed to `GetComponent<ZNetView>()` before it could ever actually
      fire (mining hadn't reached the milestone level yet in testing).
      Deployed together with the four-name fix above; **pickup/skinning
      fix not yet re-confirmed in-game** — next relaunch should show
      mushrooms/berries/veggies pickable again and skinning carcasses
      actually granting their item and disappearing.
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
- [x] **~~BepInEx.ConfigurationManager as a setup step~~ — superseded.**
      This was the original, much smaller answer to "give me a dev tool":
      install a third-party companion mod for an F1 menu that tunes
      `Config.Bind` values live. Superseded 2026-09-10 by
      [Pillar 5: Dev Tool](#pillar-5-dev-tool-srcminimodsdevtool--new-2026-09-10)'s
      Values tab, which does the same thing (and more — structured content,
      not just flat values) without a separate download.
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
- [x] **Lightning Sword** ([Items/LightningSword.cs](../src/MiniMods/RarityLoot/Items/LightningSword.cs)) —
      *(implemented 2026-09-10, not yet runtime-tested)*. Second
      named-hero item, and the first thing built on a genuinely new
      mechanic: real elemental damage stacked on top of physical, and an
      on-hit proc, not just a stat multiplier. User's own design: "a
      simple, not OP but fun starting Legendary Sword found in the
      Meadows." Clones `SwordBronze`, adds flat lightning damage on top
      of its untouched physical damage (config-tunable, default +15 —
      "not OP" is the user's own framing, kept modest on purpose), and
      opts into a new **Chain Lightning** proc
      ([Core/Combat/ChainLightningEffect.cs](../src/Core/Combat/ChainLightningEffect.cs),
      [Core/Combat/WeaponSpecialEffectPatch.cs](../src/Core/Combat/WeaponSpecialEffectPatch.cs)) —
      a genuinely new, generic weapon-proc mechanism (any weapon can opt
      in via one `ItemData.m_customData` key, the exact same one the Dev
      Tool's Item Creator now exposes through its own "Special On-Hit
      Effect" dropdown — this isn't a one-off hack tied to a single item).
      On trigger (config chance, default 20%), jumps lightning damage
      between up to 3 nearby enemies (config range/damage) and spawns a
      self-contained flickering arc visual (a short-lived `LineRenderer`
      using Unity's always-available `Sprites/Default` shader) on each
      hit target for a couple seconds — deliberately never touches the
      target's own renderer/material/shader, so there was nothing to
      verify-or-guess-wrong about the target's own asset setup, unlike
      the Hatchet/Knife naming surprises earlier the same session.
      **Deliberately drop-only, not craftable** (`CustomItem.Recipe` set
      to `null` after construction — confirmed via decompile that
      `ItemManager.AddItem` only registers a recipe when that's non-null)
      — a rare (config, default 1%) drop from Boar or Neck, so finding
      one is a real "got lucky" moment, matching the user's own "found
      it" framing rather than a guaranteed unlock. Registered Legendary
      tier via the existing `ItemRollTrigger` (2 rolled affixes, same as
      Voltun's Set). Compiles clean.
      **Confirmed in-game (2026-09-10) — and confirmed way too strong:**
      one-shotting everything with zero gear. Root cause was really the
      same lesson as the day's whole biome-tier redesign, just baked
      into a hand-placed item instead of a random roll: `SwordBronze` is
      already a Black Forest-tier weapon base for a Meadows-only
      character, plus a **+15 flat lightning bonus** (roughly the same
      order of magnitude as the base weapon's own damage) plus up to
      **+20% total damage** from a rolled Legendary affix
      (`Affixes/AffixPool.cs`'s `DamageAffixId`, min/max 5-20, checked
      against the user's own "I think it's the ~7% damage stat" guess —
      right instinct, bigger actual range than they realized). Cut
      `LightningSwordBonusDamage`'s default 15 → 6 — the single biggest
      lever, since the affix roll and the Bronze base itself are shared
      systems used by other items too, not something to rebalance just
      for this one sword. Also updated the already-generated config file
      directly (a rebuild alone doesn't touch an existing saved value).
      **Confirmed fixed in-game (2026-09-11) — "works great."**
- [x] **10 Legendary weapons, one per elemental theme**
      ([Items/LegendaryWeaponsBatch.cs](../src/MiniMods/RarityLoot/Items/LegendaryWeaponsBatch.cs)) —
      *(implemented 2026-09-11, user's own concepts — Fire/Frost/Poison/
      Lightning across Sword/Axe/Knife/Spear/Sledge/Atgeir/Bow — not yet
      tested in-game)*. Same shape as Lightning Sword generalized into a
      data table (10 near-identical files would've been worse to
      maintain): clone a real vanilla base, add a flat elemental damage
      bonus, tag Legendary tier, no recipe (drop-only), register a
      Greydwarf drop source. Every tunable number (bonus damage, drop
      chance, one Config.Bind pair per weapon) is automatically visible
      and editable in the DevTool's Values tab — no new editor plumbing
      needed, since that tab already reads every loaded plugin's config
      generically. **Learned from the Lightning Sword one-shot incident
      above**: every base is Wood/Flint tier this time (verified real via
      a live ObjectDB dump — same session already got burned guessing
      "Hatchet"/"Knife"/"piece_sign"/"MushroomYellow"), not Bronze, and
      default bonus damage is a modest 4 rather than 15. Only one weapon
      (Skoll's Sting, the bow) has a signature proc — it reuses Chain
      Lightning verbatim; the other nine are elemental-bonus-only for
      this pass, deliberately not faking bespoke procs (ignite burst,
      frost nova, execute-on-poisoned, etc.) that would need their own
      verification pass. All drop from Greydwarf, 2% each (user's own
      call — "the only non-animal creature in Meadows," so a Wolf/Boar
      dropping Legendary gear "doesn't make sense"). Compiles clean,
      deployed.
- [x] **11 Legendary armor pieces**
      ([Items/LegendaryArmorBatch.cs](../src/MiniMods/RarityLoot/Items/LegendaryArmorBatch.cs)) —
      *(implemented 2026-09-11, not yet tested in-game)*. Same shape and
      same reasoning as the weapons batch above. Two loose four-piece
      sets (Brynhild: light/valkyrie, Helmet+Chest+Legs+Shoulder;
      Ulfhednar: heavier wolf-warrior, same four slots) plus three
      standalone pieces (a second Shoulder, two Shields) — 11 total,
      covering every armor slot at least once per the user's own ask.
      Flat `+armor` bonus (additive, not multiplicative — these bases
      have very low starting armor, 2-6, where a multiplier would either
      do nothing or swing wildly) plus Legendary tier, same Greydwarf 2%
      drop source. Two real vanilla items already fit the lore by name
      alone, found in the same live dump: `HelmetOdin` (Odin's Sight) and
      `CapeFeather` (Freyja's Feather Cloak, her actual mythological
      feathered cloak). Compiles clean, deployed.
- [x] **Black Forest: 11 Legendary weapons + 11 Legendary armor, split
      across three creature themes**
      ([Items/LegendaryWeaponsBlackForest.cs](../src/MiniMods/RarityLoot/Items/LegendaryWeaponsBlackForest.cs),
      [Items/LegendaryArmorBlackForest.cs](../src/MiniMods/RarityLoot/Items/LegendaryArmorBlackForest.cs)) —
      *(implemented 2026-09-11, not yet tested in-game)*. User's own
      call: rather than reuse-and-scale the Meadows set, a fresh 22 items
      per biome keeps the hunt from feeling repetitive, and the user
      wanted drop-source variety this round instead of one creature for
      everything. Split three ways by theme: **Troll** (primitive,
      crushing, no element — 2 weapons, 2 armor, "1-2" per the user's own
      framing), **Greydwarf Shaman** (elemental/poison, fits its own
      vanilla nature-magic identity — 5 weapons, 5 armor), **Greydwarf
      Brute** (pure melee/berserker, no element — 4 weapons, 4 armor).
      **Skeleton deliberately gets nothing**, per the user's explicit
      call. Same data-table shape as the Meadows batch (see that entry
      for the full "why," unchanged here) — Bronze/Copper/TrollLeather
      tier bases throughout (Black Forest's own real ceiling, same
      lesson as Lightning Sword), one Config.Bind pair per item for free
      Values-tab tuning. Physical-only items (Troll, Brute) add their
      bonus to `m_damage` (true damage) rather than an elemental field,
      the closest equivalent to "a flat damage bump with no element."
      **Creature name caveat**: `Troll` is near-certain; `Greydwarf_Elite`
      (Brute) and `Greydwarf_Shaman` are standard, well-established
      naming but not independently decompile-confirmed this session —
      safe failure mode if either is wrong (that one drop source just
      doesn't register, logged; the items themselves still exist and are
      spawnable via the Spawn tab). Compiles clean, deployed.
- [x] **Swamp: 11 Legendary weapons + 11 Legendary armor, same three-way
      split pattern**
      ([Items/LegendaryWeaponsSwamp.cs](../src/MiniMods/RarityLoot/Items/LegendaryWeaponsSwamp.cs),
      [Items/LegendaryArmorSwamp.cs](../src/MiniMods/RarityLoot/Items/LegendaryArmorSwamp.cs)) —
      *(implemented 2026-09-11, not yet tested in-game)*. Third biome,
      same generalized pattern as Black Forest: **Draugr Elite** (2+2,
      physical, "barrow-lord" lore-arc), **Blob** (5+5, all poison — this
      biome's elemental creature is a poison ooze, so unlike Meadows/
      Black Forest this set doesn't force fire/frost/lightning variety
      onto a creature that has none of those), **Draugr** (4+4, physical/
      melee, regular fodder). Skeleton still gets nothing. Iron/Elderbark
      tier bases (confirmed real via the same dump). **Creature name
      caveat**: `Draugr`/`Blob` near-certain, `Draugr_Elite` a reasonable
      guess matching the `Greydwarf_Elite` naming convention already used
      for Black Forest, not independently confirmed — same safe-failure
      fallback. Compiles clean, deployed.
      **Pre-emptive Meadows nerf (2026-09-11)**: user asked to cut the
      Meadows weapon/armor batch by 50% before first in-game test, same
      instinct as the Lightning Sword one-shot incident applied
      proactively this time — `LegendaryWeaponsBatch`'s default bonus
      damage 4 → 2, `LegendaryArmorBatch`'s default armor bonus 3 → 1.5.
      Landed cleanly since the config file hadn't been generated with the
      old values yet (game hadn't been relaunched since those batches
      were added), so no saved-value conflict to work around this time.
- [x] **Mountain: 11 Legendary weapons + 11 Legendary armor, same
      three-way split pattern**
      ([Items/LegendaryWeaponsMountain.cs](../src/MiniMods/RarityLoot/Items/LegendaryWeaponsMountain.cs),
      [Items/LegendaryArmorMountain.cs](../src/MiniMods/RarityLoot/Items/LegendaryArmorMountain.cs)) —
      *(implemented 2026-09-11, not yet tested in-game)*. Fourth biome:
      **Golem** (2+2, physical, ancient-stone-giant lore-arc), **Drake**
      (5+5, all frost — its own vanilla identity as a frost-breathing
      wyrm), **Wolf** (4+4, physical/melee, regular pack predator).
      Silver/Wolf-Fang tier bases. Real vanilla weapon variety at Silver
      tier is narrower than lower biomes (no Silver Atgeir/Axe/Sledge/Bow
      exist) — Iron-tier bases reused where Silver has no native option,
      same accepted pattern as earlier biomes' own reuse. One case
      needed no stand-in at all: Wolf's armor uses the real vanilla
      Wolf-pelt bases (`ArmorWolfChest`/`Legs`), a genuinely perfect
      thematic match rather than a compromise. Compiles clean, deployed.
- [x] **Plains: 11 Legendary weapons + 11 Legendary armor, same
      three-way split pattern**
      ([Items/LegendaryWeaponsPlains.cs](../src/MiniMods/RarityLoot/Items/LegendaryWeaponsPlains.cs),
      [Items/LegendaryArmorPlains.cs](../src/MiniMods/RarityLoot/Items/LegendaryArmorPlains.cs)) —
      *(implemented 2026-09-11, not yet tested in-game)*. Fifth biome:
      **Lox** (2+2, physical, giant-tank lore-arc), **Fuling Shaman**
      (5+5, all fire — its own vanilla identity as the fireball-throwing
      caster), **Fuling** (4+4, physical/melee, regular raider). Black
      Metal/Needle tier bases (Plains' own real ceiling) — the real
      vanilla Padded armor set (Linen Thread + Black Metal) used for
      every armor piece. No native Plains-tier Spear exists in vanilla,
      so that weapon type is simply skipped this biome rather than
      reusing a lower-tier one for no reason. Compiles clean, deployed.
- [x] **Mistlands: 11 Legendary weapons + 11 Legendary armor, same
      three-way split pattern**
      ([Items/LegendaryWeaponsMistlands.cs](../src/MiniMods/RarityLoot/Items/LegendaryWeaponsMistlands.cs),
      [Items/LegendaryArmorMistlands.cs](../src/MiniMods/RarityLoot/Items/LegendaryArmorMistlands.cs)) —
      *(implemented 2026-09-11, not yet tested in-game)*. Sixth biome:
      **Gjall** (2+2, physical, large-flying-creature lore-arc),
      **Dverger Mage** (5+5, mixing fire AND frost -- the first biome
      where the elemental creature actually casts both in vanilla,
      rather than one element being forced), **Seeker** (4+4, physical/
      melee, the ubiquitous Mistlands bug). Confirmed via the live dump
      that "Gold" is Mistlands' own real weapon-tier branding (not a
      Black-Metal-successor material name) — SwordGold/AxeGold/etc.,
      with elemental sub-variants like `Gold_FrostFire` already built
      into vanilla — Carapace used for the one weapon (spear) with a
      Mistlands-material-named option instead. Armor is Carapace-tier
      throughout; no Mistlands-specific cape exists, so Linen/TrollHide
      capes are reused again for the two Shoulder slots. **Creature name
      caveat**: `Seeker`/`Gjall` near-certain; `Dverger_Mage` a
      reasonable guess (the `Dverger` spelling itself confirmed real via
      the dump's own item names — `DvergerHairMale`, `DvergerSuitFire` —
      but the hostile mage creature's exact prefab name not independently
      confirmed) — same safe-failure fallback as every other biome.
      Compiles clean, deployed.
- [x] **Ashlands: 11 Legendary weapons + 11 Legendary armor, same
      three-way split pattern**
      ([Items/LegendaryWeaponsAshlands.cs](../src/MiniMods/RarityLoot/Items/LegendaryWeaponsAshlands.cs),
      [Items/LegendaryArmorAshlands.cs](../src/MiniMods/RarityLoot/Items/LegendaryArmorAshlands.cs)) —
      *(implemented 2026-09-11, not yet tested in-game)*. Seventh biome:
      **Morgen** (2+2, physical, large-rock-creature lore-arc),
      **Twitcher** (5+5, all fire, the small explosive fire bug),
      **Charred** (4+4, physical/melee, the biome's basic soldier).
      Weapon bases are vanilla's own already-named Ashlands set
      (`SwordDyrnwyn`, `AxeJotunBane`, `MaceEldner`, `SpearSplitner`,
      `AxeBerzerkr`, `BowAshlands`, confirmed real via the dump), cloned
      in their base un-suffixed form — the `_Blood`/`_Lightning`/
      `_Nature` variants also seen in the dump are vanilla's own separate
      pre-built magic items, untouched here. Armor is Flametal-tier
      throughout, plus `CapeAsh` — the first biome since Meadows/Black
      Forest where a genuinely biome-specific cape exists, no reuse
      needed for that slot. **Creature name caveat**: `Morgen` and
      `Twitcher` confirmed as real prefab-name fragments via the dump
      (`charred_twitcher_throw`); `Charred` itself may actually split
      into distinct melee/ranged variants in the real game — used here
      as the plain creature name, same safe-failure fallback as every
      other biome if wrong. Compiles clean, deployed.
- [x] **Deep North: 11 Legendary weapons + 11 Legendary armor, same
      three-way split pattern — closes out all 8 biomes**
      ([Items/LegendaryWeaponsDeepNorth.cs](../src/MiniMods/RarityLoot/Items/LegendaryWeaponsDeepNorth.cs),
      [Items/LegendaryArmorDeepNorth.cs](../src/MiniMods/RarityLoot/Items/LegendaryArmorDeepNorth.cs)) —
      *(implemented 2026-09-11, not yet tested in-game)*. Eighth and
      final biome: **Fenring** (2+2, physical, lore-arc — nicely closes
      out the Fenrir/Hati/Sköll wolf-god thread running through Meadows'
      "Fenrir's Howl" and Mountain's "Hati's Fang"/"Cloak of Hati"),
      **Volture** (5+5, all frost, this biome's own theme), **Urchin**
      (4+4, physical/melee, regular ground threat). Armor uses three
      genuinely distinct real Deep-North-specific tiers confirmed via the
      dump (`ArmorDeepNorthHeavy`/`Medium`/`Mage`, 44/34/22 armor) rather
      than one base reused three times, plus `ShieldSerpentscale` (300
      armor, the single highest-armor shield in the whole dump) for both
      Shield slots. No Deep-North-specific WEAPON tier was found the same
      way — "Gold" tier (already used for Mistlands) reused for weapons,
      consistent with Iron's reuse across several earlier biomes.
      **Confidence caveat, stronger than every earlier biome**: Deep
      North is the newest Valheim content this project has touched, and
      `Fenring`/`Volture`/`Urchin` as its own creature names are a
      reasonable best-effort rather than dump-confirmed the way most
      other biomes' creatures were — same safe-failure fallback if wrong
      (that drop source just doesn't register, logged; items still exist
      and are spawnable via the Spawn tab). Compiles clean, deployed.
      **Running total across all 8 biomes: 176 Legendary items** (11
      weapons + 11 armor × 8), every one drop-only, Legendary tier, with
      its own individually tunable bonus-damage/armor and drop-chance
      Config.Bind entries surfaced automatically in the DevTool's Values
      tab.
- [ ] **Future named-hero sets** beyond Voltun's and the Lightning Sword —
      *(not designed)*. The pattern (and now also the special-effect
      mechanism) is set; no further hero/item has been thought through
      yet.

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

## Pillar 5: Dev Tool (`src/MiniMods/DevTool`) — new, 2026-09-10

What I want: a single in-game overlay panel, toggled by a hotkey, for real
editing control over the whole mod without a recompile — create items
(armor/weapons/tools), create recipes (materials list, station, optional
skill+level gate) for anything that uses the recipe system, create new
skills from Core's already-proven generic mechanics, tune area/world
difficulty, and edit every mod's tunable Config value — all from one
panel, save, then test in-game. Prioritized ahead of Quests and
RarityLoot's ordinary-gear tier list (user's call), specifically so those
get wired into this overlay as they're built rather than bolted on after.
Not the same thing as the old "install BepInEx.ConfigurationManager"
recommendation (see the superseded note under Pillar 3) — that only ever
covered flat Config values; this covers structured content too.

Key technical findings that shape the whole pillar (confirmed by
decompiling the real `Jotunn.dll` from this project's own NuGet package,
not guessed):
- **`Jotunn.Managers.GUIManager`** provides the real Valheim-style UI
  toolkit every Jotunn mod's panel is built from — draggable wood panels
  (`CreateWoodpanel`), scroll views, buttons, input fields, toggles,
  dropdowns, even a keybind-capture field — confirmed these exist with
  real signatures, not assumed. This is the "industry standard" approach
  for in-game Valheim mod UI, not hand-rolled raw Unity UI.
- **`Jotunn.Configs.ItemConfig`/`RecipeConfig`/`RequirementConfig`**, plus
  `CustomItem(name, basePrefabName, itemConfig)`, mean new items and
  recipes can be built **entirely from data** — clone any vanilla item by
  name, define materials/station/min-station-level from a form, no
  AssetBundle, no per-item `.cs` file. This is the exact mechanism
  [StonePickaxe.cs](../src/MiniMods/RarityLoot/Items/StonePickaxe.cs) and
  [VoltunsSet.cs](../src/MiniMods/RarityLoot/Items/VoltunsSet.cs) already
  use by hand — the Item/Recipe Creator tabs generalize it into a form.
- Combat/tool stats (damage, armor, weight, swing speed) live on
  `ItemDrop.ItemData.m_shared` fields on the cloned prefab, not in
  `ItemConfig` — the Item Creator applies a small generic "stat override"
  step after creating the `CustomItem`, touching the same fields Stone
  Pickaxe/Voltun's Set already edit by hand.
- **Skill Creator ceiling (user's own call, locked in):** scoped to
  composing new skills from mechanics Core has already proven generic —
  XP source (redirect a vanilla `SkillType` via `SkillXpRedirect`, or
  none), station-based crafting XP (`CraftingStation.m_craftingSkill`,
  Smithing's pattern), tool-gated Pickable harvest (Skinning's pattern).
  A genuinely novel mechanic (Cooking's burn-save roll, Skinning's
  carcass-drop interception) still needs custom code — the editor doesn't
  invent new game logic, it composes existing generic pieces.
- Persistence: numeric tweaks (existing `Config.Bind` values) apply live,
  no restart. New items/recipes/skills register through Jotunn's managers,
  which only run their registration once at startup — those need a world
  reload to actually appear, not a true hot-reload. Being upfront about
  this now rather than overpromising a magic "no restart ever" workflow.

- [x] **Overlay shell** ([DevToolPlugin.cs](../src/MiniMods/DevTool/DevToolPlugin.cs), [UI/DevToolOverlay.cs](../src/MiniMods/DevTool/UI/DevToolOverlay.cs)) —
      *(implemented, not yet runtime-tested)*. New Core-dependent mini-mod.
      F9 (config-tunable) toggles a draggable `GUIManager.CreateWoodpanel`
      with a tab bar and a scrollable content area; `GUIManager.BlockInput`
      locks player input while open, the same mechanism vanilla's own
      menus use. Tabs register themselves in `DevToolOverlay`'s static
      constructor — adding Items/Recipes/Skills/World later is a one-line
      addition there, nothing else in the shell changes. Panel is rebuilt
      lazily the next time it's opened after a scene change, relying on
      Unity's overloaded null-check correctly detecting GUIManager's own
      teardown of `CustomGUIFront` rather than needing an explicit rebuild
      hook. Compiles clean (confirmed: full 6-project solution build,
      `dotnet build -m:1`, 0 errors). **Not yet tested in-game — first
      thing to open Friday, since exact panel/scroll-view positioning was
      written from the decompile, not eyeballed against the real font
      scale/resolution yet.**
- [x] **Values tab** ([UI/ValuesTab.cs](../src/MiniMods/DevTool/UI/ValuesTab.cs)) —
      *(implemented, not yet runtime-tested)*. Fully generic: reads
      `BepInEx.Bootstrap.Chainloader.PluginInfos` directly rather than
      each mod registering its config by hand, so every current and
      future mini-mod's `Config.Bind` entries show up automatically, no
      wiring needed on that mod's side — confirmed
      `BaseUnityPlugin.Config` is public and already a keyed collection of
      every bound entry. Bool/int/float/string entries are live-editable
      (edits go through `ConfigEntryBase.BoxedValue`, so patches reading
      `.Value` elsewhere pick the change up immediately). **Known first-cut
      gap:** enum/`KeyboardShortcut`/`Color` and other non-primitive
      setting types render read-only for now — a real remap/color-picker
      control per BepInEx setting type is real work, deliberately deferred
      rather than faked with a half-working text box. **Persistence
      confirmed, not assumed** (2026-09-10 follow-up): decompiled
      `BepInEx.Configuration.ConfigFile` directly to check —
      `SaveOnConfigSet` defaults to `true`, so every edit here writes
      straight through to that mod's own `.cfg` file on disk
      automatically, same as a hand-edit. No extra code needed for this
      tab. Compiles clean. **Not yet tested in-game.**
- [x] **Item Creator tab** ([UI/ItemCreatorTab.cs](../src/MiniMods/DevTool/UI/ItemCreatorTab.cs), [Data/DevItemDefinition.cs](../src/MiniMods/DevTool/Data/DevItemDefinition.cs), [Data/DevItemRegistry.cs](../src/MiniMods/DevTool/Data/DevItemRegistry.cs)) —
      *(implemented, not yet runtime-tested)*. Form fields for base
      vanilla item, display name/description, 5 stat multipliers
      (damage/armor/weight/durability/attack speed), crafting station
      (dropdown sourced live from Jotunn's own `CraftingStations.GetNames()`,
      not a hand-typed list), min station level, and up to 4 material
      slots. "Save & Register" persists to `DevToolData/items.json`
      (`UnityEngine.JsonUtility`, no new dependency) and immediately calls
      the same `CustomItem(name, basePrefabName, ItemConfig)` +
      `m_shared`-field-multiply pattern StonePickaxe/VoltunsSet already
      use by hand — so a new item shows up in this session's registry
      right away; whether it's fully live everywhere (recipe list caches,
      etc.) without a world reload is exactly the open question flagged
      under this pillar's persistence note above. **First-cut limit,
      flagged not hidden:** fixed 4 requirement slots, not a dynamic
      add/remove list — no real recipe in this codebase uses more than 3.
      **Lower-confidence field:** `m_maxDurability` (Durability
      Multiplier) wasn't independently confirmed against this project's
      own decompile the way `m_damages`/`m_armor`/`m_weight` were; safe
      failure mode if the name's wrong is simply no effect. Compiles
      clean.
- [x] **Drop Sources section** ([Data/CreatureDropPool.cs](../src/MiniMods/DevTool/Data/CreatureDropPool.cs), [Data/CreatureDropRegistry.cs](../src/MiniMods/DevTool/Data/CreatureDropRegistry.cs), [UI/CreaturePickerPopup.cs](../src/MiniMods/DevTool/UI/CreaturePickerPopup.cs)) —
      *(implemented 2026-09-10, not yet runtime-tested)*. Closes a real
      gap the user found: everything above only ever handled crafting —
      there was no way to make an item a creature/boss drop instead of
      (or alongside) a recipe. Uses `Character`'s own real loot mechanism
      (`CharacterDrop.m_drops`, confirmed public on the real assembly —
      the exact list every creature's meat/trophies/materials already
      live in) rather than inventing a parallel one, so a registered drop
      rolls through vanilla's own death-loot logic for free. Works on
      ANY item (vanilla or Item-Creator-made) and any creature (a new
      searchable `CreaturePickerPopup`, backed by a live
      `VanillaCreatureCatalog` read of `ZNetScene.instance.m_prefabs`
      filtered to anything with a `CharacterDrop` — bosses included,
      confirmed via decompile there's no separate "is boss" flag,
      they're just creatures). Guarded against duplicate entries
      stacking if `PrefabManager.OnVanillaPrefabsAvailable` fires more
      than once in a session (`m_drops` is a bare `List` with no built-in
      dedup, unlike Jotunn's own item/recipe manager dictionaries).
      **Known first-cut limit:** removing an entry from the list only
      stops it from being re-applied on the next load — it doesn't
      retroactively strip an already-injected `Drop` off a creature
      prefab that's already loaded this session, only a world
      reload/restart fully clears it. Compiles clean. **Not yet tested
      in-game.**
- [x] **Recipe Creator tab** ([UI/RecipeCreatorTab.cs](../src/MiniMods/DevTool/UI/RecipeCreatorTab.cs), [Data/DevRecipeDefinition.cs](../src/MiniMods/DevTool/Data/DevRecipeDefinition.cs), [Data/DevRecipeRegistry.cs](../src/MiniMods/DevTool/Data/DevRecipeRegistry.cs)) —
      *(implemented, not yet runtime-tested)*. Attaches a recipe to any
      existing item (vanilla or Item-Creator-made) via `RecipeConfig` +
      `CustomRecipe` — the same mechanism Jotunn's own
      `ItemManager.AddRecipesFromJson` uses internally, just routed
      through this mod's own persistence so the optional skill/level gate
      can be set in the same form. That gate is new, generic Core
      plumbing built for this
      ([SkillSystem/RecipeLevelGate.cs](../src/Core/SkillSystem/RecipeLevelGate.cs)) —
      a `Player.HaveRequirements` Prefix keyed by recipe item name (same
      real craft-attempt hook RarityLoot's `LegendaryCraftGatePatch`
      already proved out, generalized instead of hardcoded to "is this
      item Legendary"), plus
      [SkillSystem/SkillRegistry.cs](../src/Core/SkillSystem/SkillRegistry.cs)
      to resolve one of the 12 skills by name at gate-check time (needed
      since Jotunn assigns each custom skill's `SkillType` at runtime, so
      there's no compile-time constant to reference from data the way
      vanilla's own skills have). Same 4-material-slot limit as the Item
      Creator. Compiles clean. **Not yet tested in-game.**
- [x] **Skill Creator tab** ([UI/SkillCreatorTab.cs](../src/MiniMods/DevTool/UI/SkillCreatorTab.cs), [Data/DevSkillDefinition.cs](../src/MiniMods/DevTool/Data/DevSkillDefinition.cs), [Data/DevSkillRegistry.cs](../src/MiniMods/DevTool/Data/DevSkillRegistry.cs)) —
      *(implemented, deliberately narrower than first scoped, not yet
      runtime-tested)*. Registers a new skill via
      `SkillManager.Instance.AddSkill` and optionally redirects one
      vanilla skill's XP into it via `SkillXpRedirect` — the exact two
      calls every one of Core's 12 skills already makes by hand,
      generalized into a form. Guards against double-claiming a vanilla
      skill another Core skill already redirects (falls back to
      registering as an orphan skill with a logged warning, rather than
      silently stealing that skill's XP out from under it). **Scope cut
      from what this pillar's design note above originally described:**
      station-based crafting XP and Woodcutting/Mining-shaped per-level
      damage scaling are NOT offered here yet. Writing the code surfaced
      a real conflict risk that wasn't obvious from the design pass alone
      — pointing a new skill at an existing shared station (e.g.
      Workbench) the blunt way would hijack every OTHER recipe already
      crafted there too, the exact problem Fletching's recipe-name-based
      interception was built to work around for Smithing, and
      Woodcutting/Mining's damage patches are still hardcoded to one
      skill each rather than registry-driven. Both are real, scoped
      follow-up work, not silently dropped. Compiles clean. **Not yet
      tested in-game.**
- [x] **World/Areas tab** ([UI/WorldAreaTab.cs](../src/MiniMods/DevTool/UI/WorldAreaTab.cs)) —
      *(implemented, not yet runtime-tested)*. Confirmed against the real
      1.0 decompile (not just this session's earlier research notes):
      `Game.m_worldLevel` is a public **static** int (0-10), and
      `Game.instance` carries 10 public difficulty-multiplier fields
      (`m_worldLevelEnemyBaseAC/HPMultiplier/BaseDamage/MoveSpeedMultiplier/LevelUpExponent`,
      `m_worldLevelGearBaseAC/BaseDamage`,
      `m_worldLevelPieceBaseDamage/HPMultiplier`,
      `m_worldLevelMineHPMultiplier`) — all real vanilla fields, nothing
      new invented. Tab is inert with a message at the main menu
      (`Game.instance` is null there). **Persistence fixed 2026-09-10**
      ([Data/DevWorldSettings.cs](../src/MiniMods/DevTool/Data/DevWorldSettings.cs), [Data/DevWorldSettingsRegistry.cs](../src/MiniMods/DevTool/Data/DevWorldSettingsRegistry.cs), [Patches/WorldSettingsReapplyPatch.cs](../src/MiniMods/DevTool/Patches/WorldSettingsReapplyPatch.cs)) —
      digging into the original gap surfaced something worth flagging:
      `Game.m_worldLevel` turns out to be read ONCE at world start from a
      world-creation-time "modifiers" store (confirmed via decompile:
      `trySetIntKey(GlobalKeys.WorldLevel, ...)` inside `Game`'s own
      setup, sourced from `globalKeysValues` — the world's chosen
      difficulty preset), not a normal runtime-mutable save key the way
      boss-kill flags are. Writing an override back into that store
      directly would mean reaching into Valheim's own world-save format,
      real risk of corruption for a "let me tune this while testing"
      feature — judged not worth that risk. Fixed instead by having Dev
      Tool remember the values itself (own JSON, same pattern as
      items/recipes/skills) and silently reapply them via a `Game.Start`
      Postfix (same real hook Jotunn's own `GUIManager` already patches,
      confirmed via decompile, reused rather than guessed at) every time
      a world loads — same practical effect, zero save-file risk. A fresh
      install has no saved file, so this is a no-op until the user
      actually saves a change from this tab (an explicit "Save" button,
      not autosave-per-keystroke, so tweaking several fields doesn't
      write to disk on every character typed). Compiles clean. **Not yet
      tested in-game.**

### First in-game test (2026-09-10) — real bugs found and fixed

Booted the mod for the first time this session. Immediate finding: the
local publicized reference assembly was **2 days stale** relative to the
actual installed game (Valheim took a same-day hotfix) — several
decompile-based assumptions made earlier the same session, and some
pre-existing code from before it, were wrong as a result. Force-regenerated
the publicized assembly (`rm -rf .../publicized_assemblies` then rebuild)
and fixed everything it surfaced:

- **Character creation was completely broken** (couldn't change
  hair/appearance, "OK" did nothing) — root cause:
  `Humanoid.m_inventory` is `protected` on the real assembly (compiles
  fine against the local publicized reference, throws
  `FieldAccessException` at runtime). Hit during max-health calculation
  on every character load. Fixed everywhere it appeared: RarityLoot's
  `LifeAffixPatch`, Quests' `QuestBoardPanel`/`QuestPlayerState`,
  ValheimQoL's `AutoPickupSort`/`CraftFromContainers`/`QuickSlots` — all
  switched to the real public accessors (`GetInventory()`,
  `GetAllItems()`). `Inventory.m_inventory` (the raw list) and
  `Inventory.Changed()` are ALSO private on the real assembly (same
  compiles-fine-runs-broken shape) — fixed the same way, using
  `GetAllItems()` (confirmed via decompile to return the same live list,
  not a copy) and the real public `m_onChanged` event instead of the
  private `Changed()`.
- **Two Harmony patches were failing to apply at all**, each logging a
  HarmonyX error on every load: ValheimQoL's `StaminaDrainPatch` named
  its ref-parameter `v` instead of the real method's `stamina` (Harmony
  matches ref-parameter Prefixes by name); Quests' `DiscoverBiomePatch`
  targeted `Player.AddKnownBiome`, which the hotfix changed to `private`
  and switched its parameter from `Heightmap.Biome` to a new `BiomeSector`
  type, corrupting HarmonyX's IL patching. Fixed the stamina patch by
  renaming the parameter; rebuilt biome-discovery tracking on
  `Player.UpdateBiome` instead (also private now, but Harmony patches by
  reflection regardless of accessibility) reading the still-public,
  still-stable `GetCurrentBiome()`, with Quests now keeping its own
  simple "has this player stood in this biome" flag
  (`QuestPlayerState.HasVisitedBiome`) rather than depending on vanilla's
  actively-changing internal biome-discovery system.
- `CookingStation.GetSlot` gained a 5th `cheated` out-param;
  `Hoverable` gained a `GetHoverOffset()` method — both just needed
  updating to match, once the fresh assembly revealed them.
- **`Directory.Build.targets`'s auto-deploy target requires building via
  the solution file**, not a single project directly — discovered while
  debugging: `DoPrebuild.props`'s own import is keyed off
  `$(SolutionDir)`, which MSBuild only populates when a `.slnx`/`.sln` is
  in play. `dotnet build src/Core/Core.csproj` silently skips the whole
  Jotunn prebuild step. Always build from the repo root with no project
  path (`dotnet build -m:1`).
- **Still open, not blocking:** Skinning's 4 animals and RarityLoot's
  Voltun's Set/Skinning Knife still fail to register (`PrefabManager`
  couldn't resolve `"MushroomYellow"`/`"Hatchet"`/`"Knife"` as base
  prefabs) — plausibly more casualties of the same hotfix, not yet
  root-caused. Doesn't block anything else from working.

**Dev Tool UX overhaul, same session, driven by hands-on feedback:**
Once the panel was actually usable, direct feedback was "text doesn't fit
the screen" and "typing raw prefab names by hand is bad UX, especially
after watching that exact kind of guess break tonight." Both addressed:
- [DevToolUiHelpers.cs](../src/MiniMods/DevTool/UI/DevToolUiHelpers.cs)'s
  `TextRow` had a real bug: Unity's `Text` wraps horizontally by default
  but **silently truncates whatever doesn't fit vertically** — every
  multi-sentence note in every tab was fixed-height at one line's worth
  of space. Now takes an explicit height per call (~40 default, ~55-60
  for longer notes), and the whole overlay panel grew from 900x600 to
  1000x700 for headroom.
- [ItemPickerPopup.cs](../src/MiniMods/DevTool/UI/ItemPickerPopup.cs) +
  [VanillaItemCatalog.cs](../src/MiniMods/DevTool/Data/VanillaItemCatalog.cs) —
  a searchable, category-filterable (All/Weapons/Armor/Tools/
  Consumables/Materials) item picker, opened via a "Browse..." button
  next to every item-name field (Item Creator's base item, both
  creators' material slots, Recipe Creator's item-to-craft). Built from
  `ObjectDB.instance.m_items` **at the moment it's opened** — a live read
  of the actual running game, not a hand-typed list — specifically
  because tonight's own testing proved hardcoded prefab-name guesses go
  stale the instant the game updates (see the Hatchet/Knife/MushroomYellow
  failures above). Wired into
  [DevToolUiHelpers.FieldRowWithPicker](../src/MiniMods/DevTool/UI/DevToolUiHelpers.cs).
  Compiles clean.
- **Second in-game pass, same session:** panel resize alone didn't fix
  the text problem — a screenshot showed the World tab's header/note
  text with roughly the left half missing. Real root cause, found by
  comparing against the (working) field-label text right next to it in
  the same screenshot: `CreateText` anchors a left-edge point `(0, 0.5)`
  but leaves Unity's default `(0.5, 0.5)` CENTER pivot; a
  `HorizontalLayoutGroup`-wrapped row (used by every field label)
  recomputes its child's position directly and happens to paper over the
  mismatch, but a bare wrapper with no layout group (`TextRow`,
  `ValuesTab`'s section headers, the item picker's empty-state text) has
  nothing overriding it, so the text renders centered ON the left edge —
  half of it off-panel. Fixed by explicitly setting `pivot = (0, 0.5)`
  on the text's RectTransform right after creation in all three
  places — confirmed via a full sweep of every `CreateText` call site in
  DevTool that nothing else has the same unguarded pattern. Confirmed
  fixed in-game (2026-09-10).

### Biome Editor + Dungeon Editor tabs (2026-09-10, user's own ask after seeing the World tab)

User's own framing: the World tab (global World Level dial) wasn't what
they expected — they wanted per-biome control, and separately remembered
wanting dungeons gate-keeping items. Research before building (same
verify-first approach as everything else this session): dungeons turned
out to be structurally just Locations with a `DungeonGenerator`
component, not a separate system, and dungeon loot lives in hundreds of
individual hand-authored room prefabs, not a central table — closed off
"inject into vanilla loot" as an option, confirmed with the user before
building the alternative below.

- [x] **Biome Editor tab** ([UI/BiomeTab.cs](../src/MiniMods/DevTool/UI/BiomeTab.cs), [Data/ZoneLocationCatalog.cs](../src/MiniMods/DevTool/Data/ZoneLocationCatalog.cs)) —
      *(implemented, not yet runtime-tested)*. Pick a biome, see every
      non-dungeon `ZoneSystem.ZoneLocation` that can spawn there (a live
      read of `ZoneSystem.instance.m_locations`, same "read the game's
      own data" principle as `VanillaItemCatalog` — confirmed via
      decompile that `m_locations`/`m_biome`/`m_quantity` are genuinely
      public, and that `SoftReference<T>.Asset` lazily loads on access
      rather than needing a preload step), edit spawn quantity per
      location. **Honest limitation:** only affects zones generated/
      explored from here on — confirmed via decompile that locations are
      placed into a zone once, the first time it's generated, so this
      can't retroactively add more of something to already-explored
      terrain. Persisted the same way as the World tab
      ([Data/DevZoneSettingsRegistry.cs](../src/MiniMods/DevTool/Data/DevZoneSettingsRegistry.cs)) —
      reapplied on `Game.Start`. Compiles clean. **Not yet tested in-game.**
- [x] **Dungeon Editor tab** ([UI/DungeonTab.cs](../src/MiniMods/DevTool/UI/DungeonTab.cs)) —
      *(implemented, not yet runtime-tested)*. Same catalog, filtered to
      locations whose prefab has a `DungeonGenerator` anywhere in its
      hierarchy (confirmed via decompile this is the real distinction
      vanilla itself uses — no separate "is dungeon" flag exists) — same
      quantity editing and persistence as the Biome tab.
- [x] **Dungeon-exclusive bonus loot pool** ([Data/DungeonLootPool.cs](../src/MiniMods/DevTool/Data/DungeonLootPool.cs), [Patches/DungeonLootPatch.cs](../src/MiniMods/DevTool/Patches/DungeonLootPatch.cs)) —
      *(implemented, not yet runtime-tested)*. Answers "gate keep items
      behind dungeons" the way the user actually wanted once the vanilla-
      loot-table option was ruled out: a config-managed pool (item +
      chance% + min/max amount, added via the same searchable item
      picker) that rolls independently the first time a container is
      opened inside an active dungeon — a **chance, not a guarantee**
      (user's own words: "adds an incentive to do more of the content...
      run another dungeon to get that item to drop"), layered on top of
      whatever the container already holds rather than replacing
      anything. "Inside a dungeon" is a `Physics.OverlapSphere` proximity
      check for any `DungeonGenerator` within 50m — an approximation, not
      precise zone-membership, same style CraftFromContainers already
      uses for "nearby." The "already rolled" flag lives on the
      container's own ZDO (`GetZDO().GetBool`/`.Set`, the same real
      mechanism vanilla uses for its own "have I been discovered"
      tracking), so it persists correctly across save/reload and won't
      re-roll every time the same chest is reopened. Compiles clean.
      **Not yet tested in-game.**
- Tab bar sizing tightened (140px/150px step → 110px/125px) to fit 7 tabs
  in the panel without overflowing past the right edge, now that Biome
  and Dungeon pushed the count up from 5.

### Full editor audit: create/change/modify parity (2026-09-10, user's own ask)

User asked for a pass over the whole Dev Tool making sure every tab has
what it needs to create, change, and modify things, "just like Item
Generator" — auditing against that bar surfaced that Item Creator itself
was missing half of it: every list-backed tab (Items, Recipes, Skills,
Dungeon's loot pool) had Save + Remove, but no **Edit** — changing
anything meant deleting it and retyping the whole thing from scratch.
Fixed everywhere it applied:

- **Items, Recipes, Dungeon loot pool:** each list row now has an `Edit`
  button that loads that entry's saved values back into the form above
  (re-saving overwrites the same entry, matched by name — doesn't create
  a duplicate). Added a `Clear Form` button alongside each `Save` button
  so starting a new one after editing doesn't require leftover values
  from the last edit.
- **Skills tab was the furthest behind — had no Remove or Edit at all.**
  Added both, honestly scoped to what's actually true: Remove only stops
  a skill from re-registering on the *next* reload (`SkillXpRedirect` has
  no unregister, and Jotunn skills aren't designed to be removed once
  added), so it stays live for the rest of the current session either
  way — said directly in the tab's own UI text, not just a code comment.
- **Biome/Dungeon location quantity rows and the Values tab were already
  fine as-is** — direct field editing IS the whole interaction there,
  there's no separate "form vs. list" split to reconcile.

Compiles clean. **Not yet tested in-game.**

### Seven-item expansion pass (2026-09-10, Claude's own prioritized recommendations, user said "do all of them")

Asked directly what else the editor needed to actually help create content.
Answered with a ranked list; user said build all of it, in that order.
Panel now has 10 tabs (Items, Pieces, Creatures, Recipes, Skills, Quests,
World, Biomes, Dungeons, Values) — tab bar now **wraps into multiple rows**
instead of endlessly widening the panel, since that stopped scaling
around 9-10 tabs.

1. [x] **Quest Creator tab** ([UI/QuestCreatorTab.cs](../src/MiniMods/DevTool/UI/QuestCreatorTab.cs)) —
   *(implemented, not yet runtime-tested)*. The biggest real gap: the
   whole quest chain used to be hardcoded C# (`Quests/Data/QuestDatabase.cs`,
   **deleted this session**). `QuestDefinition`/`QuestObjectiveType` moved
   to [Core/Quests/QuestDefinition.cs](../src/Core/Quests/QuestDefinition.cs) +
   [Core/Quests/QuestRegistry.cs](../src/Core/Quests/QuestRegistry.cs) —
   a shared, dumb blackboard both Quests' own runtime (tracking patches,
   Adventure Board panel) and this tab read/write, without either
   mini-mod depending on the other (both already depend on Core). First-run
   seeding: DevTool's own [Data/DevQuestRegistry.cs](../src/MiniMods/DevTool/Data/DevQuestRegistry.cs)
   writes the original hand-authored 3-quest chain as the JSON's starting
   content on first load, so nothing was lost in the move, it just became
   editable. Full create/edit/duplicate/remove.
2. [x] **Piece Creator tab** ([UI/PieceCreatorTab.cs](../src/MiniMods/DevTool/UI/PieceCreatorTab.cs)) —
   *(implemented, not yet runtime-tested)*. Before this, a new buildable
   object meant hand-writing a `.cs` file the way `AdventureBoard.cs` was
   built for Quests -- no general tool existed. Same clone-a-vanilla-base
   shape as Item Creator, using `CustomPiece(name, baseName, PieceConfig)`
   (the exact mechanism AdventureBoard.cs already proved by hand). Category
   and Piece Table are both dropdowns sourced live from Jotunn's own
   `PieceCategories.GetNames()`/`PieceTables.GetNames()` (confirmed to
   exist, mirroring `CraftingStations.GetNames()`), not hand-typed lists.
3. [x] **Status effects on items** ([Data/VanillaStatusEffectCatalog.cs](../src/MiniMods/DevTool/Data/VanillaStatusEffectCatalog.cs)) —
   *(implemented, not yet runtime-tested)*. Closes the exact gap flagged
   under Cooking's entry above (a dish granting health + stamina +
   "something extra" needed this). Reuses one of the game's own existing
   status effects (confirmed via decompile: `ObjectDB.instance.m_StatusEffects`
   is a real public list of every registered one) rather than authoring a
   new one from scratch -- authoring genuinely new StatusEffect behavior
   is real, separate scope (subclasses carry actual behavior, not just
   data), deliberately not attempted here. New Item Creator field applies
   the resolved effect to both `m_consumeStatusEffect` (food/potions) and
   `m_equipStatusEffect` (gear) -- harmless to set the one a given item
   type doesn't read.
4. [x] **Live stat preview in Item Creator** — *(implemented, not yet
   runtime-tested)*. Reads the base item's real live template values
   (`PrefabManager.GetPrefab` -- the un-cloned prefab, never mutated) and
   applies the same multiplier math `DevItemRegistry.ApplyStatOverrides`
   uses, updating on every keystroke in the base-item or multiplier
   fields. Answers "what will Damage Multiplier 1.5 actually produce"
   without needing to save and check in-game first.
5. [x] **Duplicate button** — *(implemented)*. Added to Items, Recipes,
   Pieces, Creatures, and Quests: loads an entry into the form like Edit,
   then blanks just the unique-key field, so making 3 similar variants of
   something no longer means retyping the whole form each time.
6. [x] **Creature Creator tab** ([UI/CreatureCreatorTab.cs](../src/MiniMods/DevTool/UI/CreatureCreatorTab.cs)) —
   *(implemented, not yet runtime-tested)*. Deliberately narrower than
   Item/Piece Creator, scoped down rather than half-faked: clone + rename
   + HP multiplier only, via `CustomCreature(name, baseName, CreatureConfig)`
   (confirmed to exist, same clone-by-name shape Jotunn already provides
   for items/pieces/skills). Damage scaling was left out on purpose after
   checking how creature AI actually deals damage -- it varies per
   creature type with no one generic field the way items have
   `SharedData.m_damages`. A creature made here gets loot through the
   Item Creator's own Drop Sources section (reuses `VanillaCreatureCatalog`/
   `CreaturePickerPopup`, built for that feature, directly) -- not
   duplicated.
7. [x] **Backup** ([Data/DevToolBackup.cs](../src/MiniMods/DevTool/Data/DevToolBackup.cs)) —
   *(implemented, not yet runtime-tested)*. One button on the Values tab
   copies every JSON file in `DevToolData/` into a timestamped subfolder.
   Deliberately just a file copy, not a designed export/import bundle
   format -- restoring is copying a file back over its live counterpart,
   nothing opaque to reverse-engineer later.

Compiles clean across all 6 projects. **Not yet tested in-game.**

### Spawn tab (2026-09-10, user's own ask: "easier to spawn a knife or the Lightning Sword")

New 11th tab ([UI/SpawnTab.cs](../src/MiniMods/DevTool/UI/SpawnTab.cs)) — the same searchable
item picker every other tab uses, an amount field, and a Spawn button
that calls the confirmed-public `Inventory.AddItem(GameObject, int)`
(same overload `DungeonLootPatch.cs` already uses) straight on the local
player's own inventory. Works for vanilla items and anything this mod
registers via `ItemManager.AddItem` — Lightning Sword, Voltun's Set,
etc. — with zero special-casing, since Jotunn adds custom items into the
same `ObjectDB.m_items` list `VanillaItemCatalog` (and therefore the
picker) already reads. No crafting, no drop-chance roll to wait out —
purely a testing convenience. Compiles clean, deployed. **Not yet
tested in-game.**

### Legendary Drop Sources section, Creature Creator tab (2026-09-10, user's own ask)

Answers "add a field to what can drop the legendaries" from the same
message that drove the Pillar 3 biome-drop redesign above. Creature-level
rather than per-item — RarityLoot's new ambient biome drop system rolls a
random base item at kill time, so eligibility has to live on the
creature, not any one item. New section on
[UI/CreatureCreatorTab.cs](../src/MiniMods/DevTool/UI/CreatureCreatorTab.cs):
a creature picker + Add/Remove list, persisted to its own JSON
([Data/DevLegendaryDropSourceRegistry.cs](../src/MiniMods/DevTool/Data/DevLegendaryDropSourceRegistry.cs))
and pushed into a new shared Core registry
([Core/Loot/LegendaryDropSourceRegistry.cs](../src/Core/Loot/LegendaryDropSourceRegistry.cs))
on load — same "dumb registry in Core, mini-mod owns persistence/UI"
split as SkillRegistry/QuestRegistry, so RarityLoot (which DevTool
doesn't otherwise reference) can read eligibility without a new
cross-mod dependency. Works for any creature by prefab name, vanilla or
DevTool-made alike. Compiles clean, full solution build (0 errors).
**Not yet tested in-game.**

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

What I want: real objective-based quests, not just fetch-flavor on top of
vanilla. **Design pivoted this session (2026-09-10):** originally framed
around a quest-giver NPC (see the superseded note below), landed instead
on a buildable **Adventure Board** — avoids a real technical/visual risk
that surfaced while scoping the NPC idea (see below) without giving up
the "physical anchor point for the chain" feeling.

- [x] **~~Quest-giver NPC~~ — superseded.** Floated idea: clone The Elder
      (the only large "presence" creature readily available) and strip
      its boss AI to make it a friendly, dialogue-capable quest giver.
      Assessed rather than built: The Elder is a stationary tree-boss with
      boss-only animations/audio (roar, spawn VFX) baked into its AI, not
      built to idle the way a shopkeeper does — turning it passive would
      be a lot of animation-fighting for a visually uncertain payoff, and
      Valheim has essentially one other candidate humanoid to clone
      (Haldor) which would've meant a second, unrelated identity anyway.
      Replaced with the Adventure Board below, which sidesteps the whole
      problem — no AI to strip, no animation states to fight.
- [x] **Adventure Board** ([AdventureBoard.cs](../src/MiniMods/Quests/AdventureBoard.cs)) —
      *(implemented, not yet runtime-tested)*. A buildable piece (Hammer's
      own piece table, `_HammerPieceTable` — same hook Building's skill
      XP already uses) cloned from vanilla's standing sign
      (`"piece_sign"` — same unverified-but-standard-name caveat as
      StonePickaxe/VoltunsSet; safe failure mode if wrong is a clear
      Jotunn load error, nothing silent). A custom
      `AdventureBoardInteractable` component (implements vanilla's real
      `Interactable`/`Hoverable` interfaces directly, confirmed via
      decompile — same interfaces `Pickable` already uses) opens the
      quest panel on interact and starts the chain automatically the
      first time any player reads it — no separate "accept" step, per
      this session's own framing ("once built, players start getting
      quests"). Cost: 2 Wood (config-tunable), deliberately cheap.
      Compiles clean. **Not yet tested in-game.**
- [x] **Quest chain + tracking** ([Data/QuestDefinition.cs](../src/MiniMods/Quests/Data/QuestDefinition.cs), [Data/QuestDatabase.cs](../src/MiniMods/Quests/Data/QuestDatabase.cs), [Data/QuestPlayerState.cs](../src/MiniMods/Quests/Data/QuestPlayerState.cs), [Patches/QuestTrackingPatches.cs](../src/MiniMods/Quests/Patches/QuestTrackingPatches.cs)) —
      *(implemented, not yet runtime-tested)*. Three objective types, by
      design — no plain item-delivery/fetch quest, matching this
      session's explicit "not just fetch-flavor" direction:
      - **KillCreature** — tracked via a small addition alongside
        Defense's own `Character.RPC_Damage` Prefix (records the last
        `Player` attacker per `Character` instance; multiple patches on
        one method already coexist elsewhere in this codebase) plus a
        `Character.OnDeath` Postfix that reads it back and grants credit.
      - **DiscoverBiome** — a Postfix on `Player.AddKnownBiome`, the exact
        real vanilla method that fires the "Found new land" banner
        (confirmed via decompile) — not a guess at polling
        `GetCurrentBiome()` every frame.
      - **OwnLegendaryItem** — checked lazily (scans the player's
        inventory) whenever the board is opened, rather than hooking
        every possible acquisition path — there's no live "event" for
        "do you currently own one," just a standing question.
      Per-player status/progress stored in `Player.m_customData` — the
      same already-saved field QuickSlots and Voltun's Hatchet's fixed
      bonus both already use, no new save data invented. **First
      cross-mini-mod reference in this codebase:** Quests now has a real
      `ProjectReference` to RarityLoot (every other mini-mod only depends
      on Core) because the Legendary-item check needs RarityLoot's own
      `ItemRoller`/`RarityTier` concept — matched with a hard
      `[BepInDependency(RarityLootPlugin.PluginGUID)]` so load order is
      guaranteed; documented as a deliberate exception in
      `Quests.csproj`'s own comment, not an accidental coupling.
      Compiles clean. **Not yet tested in-game.**
- [x] **The chain itself (hand-authored content, exact narrative from this
      session's design conversation):**
      1. **"Find the Black Forest"** (Discover Black Forest) → 20 Coins.
      2. **"Find and Defeat The Elder"** (Kill `gd_king` — unverified
         creature-prefab-name caveat, same shape as other such strings in
         this codebase) → 100 Coins + 200 Attack XP. Gated on having
         already discovered the Black Forest — see the area-level-gate
         note below.
      3. **"Prove Your Legend"** (own any Legendary-tier item — crafting
         Voltun's Set satisfies this, or finding one once RarityLoot's
         random-roll system, built earlier the same session, produces
         one) → 300 Smithing XP.
      Not yet DevTool-editable — a natural future Dev Tool "Quest
      Creator" tab (same shape as the Item/Recipe/Skill Creator tabs)
      would generalize this into data, not designed this session.
- [ ] **Area-level gate: uses Biome, not Player Level** — *(deliberate
      substitution, flagged not hidden)*. The user's own ask was
      "Player Level / Biome / area level gate blocking," but a Player
      Level system doesn't exist yet — it's the still-undesigned
      prerequisite under Classes & Passive Trees below, and that
      section's own prior notes already said not to invent numbers for it
      before any in-game testing has happened. Biome (`RequiredBiome` on
      `QuestDefinition`, checked via `Player.IsBiomeKnown`) is the real,
      buildable version of "area level gate" available today, and
      vision.md already leans on biome progression as a pacing gate
      elsewhere (Cooking's design). Once a Player Level system exists,
      swapping/adding it as a second gate is a small addition, not a
      redesign — `QuestDefinition` doesn't need new shape for it, just a
      field and a check.

## Parking Lot — not yet designed, deliberately deferred

- [ ] **Terraforming tools** (clearing land, ground shaping) — idea noted,
      not designed. Needs to be at the PC to actually test terrain
      changes in-game, so intentionally on hold.
