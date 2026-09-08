# Viking Adventure Mod

A Valheim mod split into a shared **Core** framework plus a set of independent
**mini-mods** that each depend on Core. Built on [BepInEx](https://github.com/BepInEx/BepInEx)
+ [Jotunn](https://github.com/Valheim-Modding/Jotunn).

## Status

Scaffolded ahead of the Valheim 1.0 release (2026-09-09). Iron Gate has not
published a public test branch for 1.0 and gives no compatibility guarantee
for existing mods.

Valheim is now installed locally and the game-assembly references
(`assembly_valheim.dll`, `assembly_utils.dll`, `UnityEngine.CoreModule.dll`)
have been confirmed against the real install — that part of the scaffold
builds clean. **BepInEx and Jotunn are not installed into the Valheim
folder yet**, so a full build still fails on those three references until
step 2 below is done. See [docs/DESIGN.md](docs/DESIGN.md) for the full
design plan and open questions.

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
    RarityLoot/              Magic/Rare/Legendary item tiers. Depends on Core.
    Quests/                  Objective-based quests + quest giver NPC(s).
                             Depends on Core.
    WeightTweaks/            QoL carry-weight slider. Standalone, no Core
                             dependency — ships/updates independently.
libs/                       Local-only, gitignored. Nothing lives here in git.
docs/DESIGN.md               Full design doc: vision, modding-limits
                             findings, per-system open questions.
```

Each mini-mod is its own BepInEx plugin (own GUID, own `.dll`), so a game
update that only breaks one mini-mod's hook doesn't require touching the
others.

## One-time setup (per machine)

1. ~~Install Valheim (Steam).~~ Done — confirmed at
   `C:\Program Files (x86)\Steam\steamapps\common\Valheim`.
2. Install [BepInEx 5](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
   and [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/)
   into that Valheim folder — easiest via a mod manager
   ([r2modman](https://thunderstore.io/c/valheim/p/ebkr/r2modman/) or the
   Thunderstore app) rather than a manual copy, so future updates stay easy.
   Jotunn needs to land at `BepInEx/plugins/Jotunn/Jotunn.dll`.
3. Set an environment variable `VALHEIM_INSTALL` to the Valheim install folder
   above, then restart your shell/IDE. (Or pass it per-build without
   touching machine-wide settings: `dotnet build -p:ValheimInstallDir="C:\Program Files (x86)\Steam\steamapps\common\Valheim"`.)
4. `dotnet build` at the repo root.

## Deploying a build for local testing

Build output isn't auto-copied into `BepInEx/plugins` yet — that's a
follow-up once the toolchain is verified against a real install (a
post-build copy step, added per-project once we know the exact plugin
folder layout Jotunn expects).
