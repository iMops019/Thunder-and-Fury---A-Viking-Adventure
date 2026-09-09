# ValheimQoL

The QoL module of the RPG overhaul mod (see [docs/valheim-mod-vision.md](../../../docs/valheim-mod-vision.md),
Pillar 4). Standalone — doesn't depend on the skilling/gear/UI pillars, and
doesn't use any Jotunn API, so it can be built, shipped, and run on its own
(including on a server that doesn't have Jotunn installed).

## Status
Valheim 1.0 launched Sept 9, 2026. BepInEx/Jotunn need a 1.0-compatible
release before this can actually run — check
https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/
before installing.

## What's here
- `Plugin.cs` — the BepInEx entry point, loads all patches, sets up config
- `Patches/InventoryTweaks.cs` — stack sizes + material weight reduction — **implemented**
- `Patches/StaminaCombatPacing.cs` — stamina drain multiplier — **implemented**;
  stamina *regen* is a flagged TODO (no single clean patch point the way
  drain has one — needs the real 1.0 decompile to find the right hook)
- `Patches/BuildingSnap.cs`, `Patches/QuickSlots.cs`, `Patches/AutoPickupSort.cs`,
  `Patches/CraftFromContainers.cs` — **design notes only, no patch code yet**
  (each file explains why and what the real implementation needs)

`InventoryTweaks` and the stamina-drain half of `StaminaCombatPacing` are
real Harmony patches using standard, long-standing Valheim field/method
names (`ObjectDB.Awake`, `ItemDrop.ItemData`, `Character.UseStamina`). The
exact names may still shift in 1.0 — verify against the real 1.0
`assembly_valheim.dll` (or its publicized copy — see below) before trusting
them.

## Building

Same as every other project in this solution — see the repo root
[README.md](../../../README.md). In short:
1. Valheim install is auto-detected (env var `VALHEIM_INSTALL`, or the
   Windows registry as a fallback) via the `JotunnLib` NuGet package this
   project references — no manual path editing needed.
2. Install BepInEx into the Valheim folder (needed for `BepInEx.dll`/`0Harmony.dll`).
3. `dotnet build` at the repo root — this also auto-generates the
   "publicized" game assemblies these patches compile against (see
   `DoPrebuild.props`).
4. Drop the built `ValheimQoL.dll` into `BepInEx/plugins/ValheimQoL/`.

## Config
All numeric tweaks (weight multiplier, stack size multiplier, stamina
multipliers, snap tolerance) are exposed via BepInEx's config system —
adjustable in `BepInEx/config/com.thunderfury.qol.cfg` without recompiling.
