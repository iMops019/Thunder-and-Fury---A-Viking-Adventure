# Valheim RPG Overhaul Mod — Project Vision

## Core Concept
Turn Valheim's survival-crafting loop into a more RPG-driven experience, OSRS-inspired skilling at the center, built on top of Valheim's existing assets rather than replacing them.

## The Four Pillars

### 1. Skilling System (OSRS-style)
- Distinct skills, each with its own XP curve and level-gated unlocks
- **Architecture decision (locked in):** fully custom skill system. Vanilla
  skills (Swords, Pickaxes, Woodcutting, etc.) stay technically present
  under the hood — required for engine/item stability — but get neutralized
  (their damage/stamina bonuses patched out) and hidden from the UI
  entirely. The player never sees or interacts with them.
- Our skills are added via Jotunn's native custom-skill support (same
  XP/leveling/icon system vanilla skills use, just ours) and behave however
  we design them — level gates, speed bonuses, unlocks, all custom logic

**Skill list (first pass — expect to tweak through the build):**
- Gathering: Mining, Woodcutting, Fishing, Skinning
- Production: Smithing, Cooking, Fletching, Building, Crafting
- Combat: Attack, Strength, Defense — OSRS-style broad stats that apply
  across all weapon types, not per-weapon-category like vanilla's
  Swords/Axes/Spears split

**Combat stat mechanics (locked in):**
- Attack: attack speed + stamina efficiency for weapon use (tempo — how much you can fight)
- Strength: damage output, one shared scaling stat across all weapons (replaces vanilla's per-weapon ~1%/level damage bump with a single unified stat)
- Defense: damage reduction and/or stagger resistance (how much you take, how well you shrug off hits)
- No hit/miss chance — Valheim has no accuracy roll, and we're not introducing one; every connecting swing deals damage as vanilla does

**Gathering skill design philosophy (locked in):**
- Soft gating over hard locks — tool tier and skill level affect mining speed/damage, not whether you can attempt a resource at all
- First case: a new Stone Pickaxe tier below vanilla's Antler Pickaxe, craftable from early materials with no boss-drop requirement. Can mine copper, just slower/weaker than the Antler Pickaxe — not about avoiding Eikthyr (not a hard fight), it's about tool tier being an efficiency curve, not a binary gate
- XP curve should feel smooth/gradual — no artificial walls or steep cliffs between levels
- No caps on where XP comes from — a player who parks in Meadows/Black Forest and grinds one skill hard can hit very high levels there without biome/boss progression, and that's fine by design
- Mining and Woodcutting share the same core mechanic: each level increases both speed and damage dealt to their resource (ore for Mining, trees for Woodcutting) — same curve, just applied to different node types
- Fishing keeps vanilla's cast/bait/reel mechanic largely as-is (not reinventing it) — runs on our custom skill instead of vanilla's, with level primarily scaling catch chance
- Fishing Rod is craftable from level 1 with basic materials — no Haldor/merchant dependency. Bait comes from common early drops (e.g. Boar Guts) instead of being gold-gated
- Skinning (new): animal deaths no longer auto-drop hide/raw meat — instead they leave a carcass that requires a Skinning Knife (new tool, Pillar 3) to harvest. For now, treating this as an action/flavor mechanic first (skin + butcher instead of auto-loot, adds a bit of immersion) rather than designing deep level-gated bonuses right away — leveling effects can come later once the base interaction feels right

**Cooking mechanics (locked in, WIP — priority skill to get feeling right):**
- Level effect: reduced burn/fail chance while cooking (not speed, not access)
- Deliberate exception to "gate rewards" elsewhere: no level-gating on which recipes/items can be cooked. Reasoning: biome progression already naturally paces which ingredients you have (nobody's walking straight to Ashlands at spawn), so an extra Cooking-level gate on top would be redundant
- Two recipe tiers:
  1. **Base recipes** — always available at any level, single ingredient, heals a normal amount for that food's type (health or stamina), same spirit as vanilla
  2. **Special/multi-ingredient recipes** — combine multiple ingredients for a dish that gives several buffs at once (e.g. health + stamina + something extra like attack speed), going beyond vanilla's usual single-or-dual-stat food. This tier is the main design space still to flesh out

**Building (locked in):** not gated behind a skill/level at all — building stays open and unrestricted, same as vanilla. May still exist as a nominal skill, but no functional gating planned.

**Smithing (locked in, direction set):**
- Normal-tier crafting (Stone/Bronze/Iron/etc.) stays as-is — no level requirement, matches vanilla, same soft-gate philosophy as everything else
- New rarity ladder above normal gear: **Magic** and **Rare** tiers added in between normal tools and the existing **Legendary** (named/unique, e.g. Voltun's Set) tier
- Legendary weapons/armor specifically DO require a Smithing level threshold to craft — a deliberate hard gate on the top tier, same pattern as Mining/Woodcutting's reward-gating (access to normal gear stays open, the good stuff requires the level). Magic/Rare tier level requirements not yet decided

**Progression design principle (LOCKED IN):**
- Problem raised: pure smooth stat-scaling (level = slightly faster/more) doesn't feel rewarding over 100 levels — no clear payoff at any given level
- Fix, two parts working together:
  1. **Milestone unlocks layered on the smooth curve.** Every skill keeps continuous scaling, but also grants real unlocks at specific levels — new recipe, bonus/rare drop chance, tool tier, passive proc. Gives concrete answers to "level 40, now what?"
  2. **Gate rewards, not access.** Resource nodes (trees, ore, etc.) stay reachable at any level per the soft-gating principle already established — but the *payoff* from that resource (recipes that use it, bonus drop chance, better tool tiers) is what's level-gated. Example: Hardwood tree stays choppable at level 1 (so rushing Black Forest early still works), but high-value Hardwood recipes/bonus yields require Woodcutting level
- This keeps the "chop wherever, whenever" freedom while still giving levels real teeth
- First concrete milestone: **Woodcutting level 15** unlocks double XP from normal trees + 25% bonus log yield

**On save compatibility (adding content after 1.0 release / after a save exists):**
- The "mod update bricks my save" risk is mostly about mods using raw integer IDs (renumbering breaks references) or removing/renaming content already in the save
- Jotunn's skill/item systems use stable string identifiers specifically to avoid that. Working assumption: as long as we only ADD new skills/items/features and don't rename or remove anything already in a save, the mod should be safe to keep growing on the same character/world — not something that has to be fully finished before first playing
- Treat as the standard supported pattern, not a hard guarantee, until we've actually tested it against 1.0


### 2. UI & Menus
- New skill panel, character sheet, expanded inventory screen
- Built via Harmony UI patches + Jotunn's UI helpers on top of Unity's uGUI
- This is the most technically involved pillar — real UI/UX design work, not just data

### 3. Gear & Materials Expansion
- New items/equipment assembled from Valheim's *existing* assets (retextures, recombinations, stat variants) — keeps scope sane since we're not modeling new 3D art
- Ties into the skilling system for level-gated crafting/equipping
- First planned item: Stone Pickaxe (see Gathering design philosophy above) — lowest tier, no boss-material requirement, built from existing stone/wood assets
- Second planned item: Skinning Knife — required to harvest carcasses under the new Skinning mechanic
- **Unique/Named gear tier (locked in):** hand-crafted named items — e.g. "Voltun's Hatchet" — sitting above the normal Stone/Bronze/Iron tier progression, not just higher numbers on the same stats. These stack multiple bonus effects at once (e.g. Voltun's Hatchet: +50% log yield, extra tree damage, faster chop speed, double XP simultaneously) rather than one flat stat bump like a normal tool upgrade gives
- **Voltun's Set (WIP):** Hatchet + Pickaxe as a 2-piece set tied to a named hero ("Voltun"), findable/completable in the 2nd area (Black Forest). Pickaxe roughly mirrors the Hatchet's bonus profile for now — will be revisited and differentiated later. Acquisition method still undecided: recipe + collected materials, vs. a straight drop chance — TBD
- **Pattern to repeat:** named-hero gear sets are the template going forward — more made-up Viking hero names, each with their own multi-piece tool/gear set, to be designed later

### 4. QoL Layer — ACTIVE MODULE (building this first)
Independent of the other three pillars — good early, fast-win target.

Chosen features:
- Bigger inventory & stack sizes
- Weight reduction for materials (config-driven multiplier)
- Craft from nearby containers
- Auto-sort / auto-pickup
- Quick slots for gear/potions
- Better building snap/placement
- Stamina / combat pacing tweaks

Build order (simple → complex):
1. Stack sizes & material weight reduction — config-driven, low risk
2. Stamina/combat pacing — config-driven multipliers on existing values
3. Building snap/placement tolerance — single patch on placement logic
4. Quick slots for gear/potions — new keybinds + small UI element
5. Auto-sort / auto-pickup — inventory logic, moderate complexity
6. Craft from nearby containers — needs container-scanning logic, most involved of the six

## Technical Foundation
- BepInEx + Harmony + Jotunn (all pending 1.0 recompiles — see status)
- C#, single mod solution, modular so pillars can be built/tested independently
- Claude writes the code; docs and design come first, you direct and make the calls
- Skill system: Jotunn SkillManager for registering our custom skills;
  Harmony patches to neutralize vanilla skill effects and hide vanilla
  skill UI; our own patches to redirect XP from in-game actions (mining,
  chopping, etc.) into our custom skills instead

## Status
- Valheim 1.0 releases Sept 9, 2026
- Iron Gate: no official mod support, no guarantee mods work at launch — BepInEx/Jotunn need to catch up first (days–weeks)
- **This week:** design and planning groundwork
- **Once tooling catches up:** start implementation

## Open Questions (need answers before coding starts)
- How deep does "custom menus" go — full UI overhaul or additive panels?

## Parking Lot (ideas noted, not designed yet)
- Terraforming tools (clearing land, ground shaping) — wants to dig into this, but needs to be at the PC to actually test terrain changes in-game, so holding off for now
