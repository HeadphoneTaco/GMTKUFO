# Naming Conventions — GMTK Jam 2026

Quick reference for keeping the project tidy across four people. When in doubt, copy the pattern of an existing asset.

## Case Cheat Sheet

| Case | Looks like | Rule |
|------|-----------|------|
| **PascalCase** | `PlayerController` | Every word capitalized, no separators |
| **camelCase** | `moveSpeed` | First word lowercase, every word after capitalized |
| **_camelCase** | `_mashedTaters` | camelCase with a leading underscore |
| **SCREAMING_SNAKE_CASE** | `MAX_HEALTH` | All caps, words separated by underscores |

## General Rules

- **PascalCase** for all asset names and folders. No spaces, no hyphens.
- Name assets descriptively — the folder says what it is, the name says which one: `Art/Sprites/PlayerRun`, `Audio/SFX/Jump`, `Prefabs/Items/HealthPickup`.
- Variants get a suffix: `Rock_Mossy`, `PlayerRun_Damaged`.
- Textures keep map-type suffixes so materials are easy to wire up: `Crate_Albedo`, `Crate_Normal`, `Crate_Emission`.
- Everything we make goes under `Assets/_Project/`. Third-party imports stay in their own folders outside `_Project`.
- No asset named "New", "Final", "Test2", or "ActualFinal", or your pillows will be cursed to be an uncomfortable temperature.

## Folders

The committed `_Project` tree is the source of truth — put things where they belong and the project stays searchable:

- `Animations/` — clips and animator controllers (Characters / Objects / UI)
- `Art/` — Materials, Models, Shaders, Sprites, Textures, VFX
- `Audio/` — Music, SFX, VoiceOver
- `Code/` — see Code section below
- `Fonts/` — Originals + generated TMPro assets
- `Prefabs/` — Characters, Environment, Items, Props, UI
- `Scenes/` — flow scenes at root, `Levels/`, `Test/`
- `ScriptableObjects/` — GameData, Settings
- `UI/` — Elements, Icons, Screens

## Scenes

- `Scenes/` root: `MainMenu`, `Game`, `EndScreen` (or whatever the final flow needs).
- `Scenes/Levels/`: `Level_01`, `Level_02` if we go multi-level.
- `Scenes/Test/`: `Test_YourName_Whatever` — sandbox scenes, never referenced in builds. Prefix with your name so nobody deletes someone's active work.

## Code (C#)

- **PascalCase**: classes, methods, properties, enums.
- **camelCase**: local variables, parameters.
- **_camelCase**: private fields. `[SerializeField] private float _moveSpeed;`
- **SCREAMING_SNAKE_CASE**: constants.
- Folder homes: `Code/Core` (game loop, managers, singletons), `Code/Gameplay` (player, enemies, mechanics), `Code/Utilities` (helpers, extensions), `Code/Editor` (editor-only tools), `Code/Shaders` (HLSL includes).
- Events: name them `OnThingHappened` (`OnPlayerDied`, `OnScoreChanged`).
- No `public` fields unless there's a reason; use `[SerializeField] private` for inspector exposure.

## Git

- **Branches:** work directly on `Master` is fine for a jam this size, BUT pull before you push, and never push a broken project. If you're doing something risky, branch it: `feature/short-name` (e.g. `feature/enemy-ai`), merge when it compiles.
- **Commits:** short imperative summary, mention the area: `Add player jump buffering`, `Fix UI scaling on 16:10`. No "stuff", "wip", "asdf".
- **Scene collisions are the #1 jam killer.** Only one person in a given scene at a time — call it out in the group chat before opening a shared scene. Prefab everything so most work happens in prefabs, not scenes.
- Large binaries (PSDs, raw recordings) stay out of the repo — share those via Drive/Discord, commit only the exported PNG/WAV.
