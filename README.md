# Viking Adventure Mod

A Valheim mod split into a shared **Core** framework plus a set of independent
**mini-mods** that each depend on Core. Built on [BepInEx](https://github.com/BepInEx/BepInEx)
+ [Jotunn](https://github.com/Valheim-Modding/Jotunn).

## Status

Scaffolded ahead of the Valheim 1.0 release (2026-09-09). Iron Gate has not
published a public test branch for 1.0 and gives no compatibility guarantee
for existing mods, so this project has **not been compiled or run yet** —
there's no local Valheim install to build against. Treat every filename/GUID
referenced in this scaffold marked `TODO(verify-once-installed)` as
unverified until checked against a real install.

## Structure

```
src/
  Core/                     Shared framework mod. Config, shared data/prefabs,
                             cross-cutting hooks. Everything else depends on it.
  MiniMods/
    ExampleMiniMod/         Template mini-mod — copy this folder to start a
                             new one. Depends on Core + Jotunn.
libs/                       Local-only, gitignored. Nothing lives here in git.
docs/DESIGN.md               Mod design notes.
```

Each mini-mod is its own BepInEx plugin (own GUID, own `.dll`), so a game
update that only breaks one mini-mod's hook doesn't require touching the
others.

## One-time setup (per machine)

1. Install Valheim (Steam).
2. Install [BepInEx 5](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/)
   into the Valheim folder (via Thunderstore/r2modman, or manually).
3. Install [Jotunn](https://valheim.thunderstore.io/package/ValheimModding/Jotunn/)
   the same way — it needs to land under `BepInEx/plugins/Jotunn/Jotunn.dll`.
4. Set an environment variable `VALHEIM_INSTALL` to the Valheim install folder,
   e.g. `C:\Program Files (x86)\Steam\steamapps\common\Valheim`. Restart your
   shell/IDE afterward.
5. `dotnet build` at the repo root.

## Deploying a build for local testing

Build output isn't auto-copied into `BepInEx/plugins` yet — that's a
follow-up once the toolchain is verified against a real install (a
post-build copy step, added per-project once we know the exact plugin
folder layout Jotunn expects).
