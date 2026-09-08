# Design Notes

## Vision

A cozy-grinding RPG overhaul, not a speedrun mod. OSRS-flavored skills with
our own XP curves, a magic/rare/legendary loot tier system built on the
game's existing assets, and quests that ask you to actually do something
("kill the boss in the 3rd area", "kill 20 trolls in the Black Forest") —
not "find 5 mushrooms."

## Modding limits — how open is Valheim compared to Project Zomboid?

Answered directly since this shapes every system below: **more constrained
than PZ.** PZ's Lua layer is a sandboxed scripting surface the game was
built to expose. Valheim has no official mod support at all — everything
here is Harmony patches + reflection into the real Mono/IL2CPP game code via
BepInEx, with Jotunn as a library that wraps the common patterns (adding
prefabs, items, skills, recipes, UI panels, key bindings) so we're not
hand-writing raw IL patches for every little thing.

Practically, that means:
- Adding data-shaped content (items, recipes, skills, status effects) is
  well-trodden and safe — Jotunn has first-class APIs for this.
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
  character type. "Adding a player" in the RPG sense here means adding
  **NPCs** (quest givers, vendors), not a new playable character — that's
  the realistic target and it's achievable.

## Core mod

Shared framework everything else depends on:
- **Skill framework** — wraps Jotunn's `SkillManager.AddSkill` (confirmed
  real API: [Jotunn custom skills tutorial](https://valheim-modding.github.io/Jotunn/tutorials/skills.html)).
  Jotunn registers the skill (name, icon, appears in the skill panel); *we*
  own the XP curve, XP-gain triggers, and level-based effects — Jotunn
  doesn't do any of that for us. This is where our own EXP values and
  modifiers live, exposed via BepInEx config so numbers are tunable without
  a recompile.
- **Shared UI theme** — Jotunn's `GUIManager` for building panels; central
  color palette / font / panel-style constants so every mini-mod's UI
  looks like one mod, not five.
- **Shared keybinds** — Jotunn's `InputManager` for custom key bindings
  (e.g. opening a quest log or skill panel).
- **Shared modifier registry** — a place other mini-mods read/write things
  like "does this creature drop rarity loot" without depending on each
  other directly.

## Skills (in Core)

TBD list — starter idea from the brief: Woodcutting, Mining, and other
OSRS-inspired skills layered on top of/alongside vanilla's existing skills
(vanilla has no Woodcutting/Mining skill today — these would be new,
tracked via Jotunn `AddSkill`). Needs, per skill: what action grants XP,
our own XP curve (formula + per-level thresholds), and what leveling
actually unlocks or improves (yield, speed, rare-drop chance, etc.).
**Not decided yet — needs your numbers.**

## RarityLoot (mini-mod)

Magic / Rare / Legendary tiers on top of vanilla items, reusing existing
assets — recolor via material tint + a particle/glow effect, rarity-colored
icon border in the UI. Architecturally this is the same space as the
existing open-source mod **EpicLoot** (Magic/Rare/Epic/Legendary/Mythic,
weighted rarity rolls, effects-per-item scaling with rarity, config-driven
drop tables) — good prior art to study for the roll/weighting math, not to
copy code from directly.

Open questions: rarity tiers (Magic/Rare/Legendary, or add Epic/Mythic
too?), what a rarity roll actually grants (stat bonus? proc effect?
skill-XP-on-use?), and whether rarity should read from the Skills system
(e.g. higher Mining level -> better rarity odds on ore-adjacent loot).

## Quests (mini-mod)

Structured objectives, not fetch quests:
- Kill-boss-in-biome quests ("kill the boss in the 3rd area")
- Kill-N-of-creature-in-biome quests ("kill 20 trolls in the Black Forest"),
  stackable/runnable alongside other active quests
- A quest giver NPC (or several) — feasible per the precedent mods above;
  needs a custom prefab (cloned from an existing Humanoid) + Harmony hook
  for interaction/dialogue + our own quest-state tracking and UI.

Open questions: quest log UI (Core's shared UI theme), how many quest
givers and where, reward structure (does completing quests grant Skill XP,
rarity-loot chances, or just vanilla currency/items?).

## WeightTweaks (mini-mod)

QoL: every item's carry weight becomes config-adjustable — likely a global
multiplier slider plus an optional per-item override table, applied by
patching `ItemDrop.ItemData` weight lookups at load. Simplest of the four
systems and a good "prove the deploy pipeline works" first target.

## Mod split (as scaffolded)

```
Core             Skill framework, shared UI theme, shared keybinds, shared
                 modifier registry. Everything else depends on this.
RarityLoot       Magic/Rare/Legendary item tiers + visuals.
Quests           Objective-based quest system + quest giver NPC(s).
WeightTweaks     Standalone QoL — carry-weight slider. No dependency on
                 Core beyond Jotunn conventions; ships/updates independently.
```

Each is its own BepInEx plugin GUID/DLL, so a 1.0 (or later) update that
breaks one system's game hook doesn't take the others down with it.

## Status

- [x] Toolchain validated: game assembly references (`assembly_valheim`,
      `assembly_utils`, `UnityEngine.CoreModule`) build clean against the
      real 1.0-bound install.
- [ ] BepInEx + Jotunn installed into the Valheim folder (needed for a full
      green build) — install via r2modman or the Thunderstore app rather
      than a manual copy, so updates stay manageable.
- [ ] Skill list + XP curves + per-level effects (needs your numbers)
- [ ] Rarity tiers + what a rarity roll grants
- [ ] Quest list + quest giver placement + reward structure
- [ ] Weight slider range/defaults
