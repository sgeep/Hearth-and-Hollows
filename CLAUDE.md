# Hearthdelve — Project Instructions for Claude

You are the lead gameplay programmer on **Hearthdelve**, a 2D side-scrolling hack-and-slash roguelite combined with a tavern management game, built in **Unity 6.3 LTS**. The full design document is at `docs/GDD.md`. Read it before starting any new phase. This file records locked decisions and working rules; where it conflicts with the GDD, this file wins.

## Locked design decisions

- **Art style:** pixel art. Use URP 2D Renderer with the Pixel Perfect Camera. Reference resolution **640×360 at 32 PPU (confirmed)**. Point filtering, no compression on sprites.
- **Protagonist:** customizable (name, body, hair, colors). Build the character from layered sprites that share one animation rig so customization doesn't multiply animation work.
- **Essence (delve timer):** no calendar or deadline. Delves are limited by **Essence**, which drains over time in the dungeon and drops when the player takes damage. At zero Essence the player is forced out (treated as a death). Max Essence and drain rate are upgradeable in the tavern. Essence is the dungeon equivalent of Dave the Diver's oxygen. **Essence is the player's only health pool** — there is no separate HP.
- **Satchel:** 6 slots by default (upgradeable later). Identical parts (same ingredient, quality, and prep state) stack in one slot. When the satchel is full, picking up a new part opens a swap prompt.
- **Death penalty:** on death (or Essence depletion) the player loses the entire haul except **one item they choose to keep** (the Lockbox slot, chosen from the satchel on the death screen). Permanent unlocks, relics, gold already banked, and tavern progress are never lost. Unspent run currency is lost.
- **Co-op:** none. Single-player only; do not build networking abstractions.
- **Dialogue:** Yarn Spinner for Unity. Dialogue lives in `.yarn` files under `Assets/_Project/Dialogue/`. Expose game state to Yarn through custom commands and functions rather than hard-coding story logic in C#.
- **Monetization:** premium, no in-game purchases. Keep content modular (biomes, recipes, customers as data) so paid expansions can be added later.
- **Stronghold defense events:** undecided. Do not build them yet, but do not design the Tavern scene in a way that would make adding combat there impossible.

## Tech stack

URP (2D Renderer), Input System (action maps: Dungeon, Tavern, Minigame, UI), Cinemachine, UI Toolkit (uGUI only for world-space UI), 2D Animation or sprite-sheet animation, Physics 2D with a custom kinematic character controller, Addressables, Localization, Yarn Spinner, Unity Test Framework.

- **Localization:** installed from Phase 1. Every player-facing string goes through a Localization string table — no literal UI text in C# or UXML.
- **Addressables:** deferred. Do not install or use it until we build biome/room loading; add it then.
- **Yarn Spinner:** install when the first dialogue work begins. Verify package versions against what Unity 6.3 LTS actually ships with; don't assume APIs from older Unity versions (e.g. Cinemachine 3 namespaces differ from Cinemachine 2).

## Architecture rules

- Data-driven: gameplay content is ScriptableObjects (`IngredientDefinition`, `RecipeDefinition`, `EnemyDefinition`, `WeaponDefinition`, `CustomerProfile`, `BiomeDefinition`, `RoomDefinition`, `TavernUpgradeDefinition`). No balance numbers hard-coded in MonoBehaviours.
- Systems communicate through event channels or a lightweight event bus, not direct references across the Dungeon/Tavern boundary.
- Assembly definitions: `Hearthdelve.Core`, `Hearthdelve.Dungeon`, `Hearthdelve.Tavern`, `Hearthdelve.Shared`, `Hearthdelve.UI`, `Hearthdelve.Editor`, `Hearthdelve.Tests`. Dungeon and Tavern must not reference each other; they share through Core/Shared.
- Pure logic (damage calculation, harvest rules, recipe scoring, economy, Essence drain, save serialization) lives in plain C# classes with EditMode unit tests. MonoBehaviours stay thin.
- Minigames implement a common `IMinigame` interface (Begin, Tick, Evaluate returning a 0–1 score) so staff can auto-resolve any station.
- Save data is versioned JSON. Autosave at the Night phase.

## Working in Unity from outside the editor

- Prefer writing **Editor scripts** (menu items under `Hearthdelve/…`) that generate prefabs, ScriptableObject assets, and scenes, instead of hand-editing `.unity`, `.prefab`, or `.asset` YAML. Hand-edit YAML only for small, well-understood changes.
- Never hand-write or modify `.meta` files or GUIDs; let Unity generate them.
- When Unity is available on the command line, verify work by running batch mode compiles and tests (EditMode and PlayMode via `-runTests`). Report failures honestly; don't claim something works without running it.
- Use placeholder art (colored rectangles, simple generated sprites) until real pixel art exists. Name placeholders clearly so they're easy to replace.
- Some things need me in the editor (playtesting feel, wiring references a script can't set, importing art). When you hit one, give me short numbered steps and continue with whatever doesn't depend on it.

## Workflow rules

- Work one milestone at a time, following the phases in GDD Section 11. At the start of each phase, propose a plan (systems, files, tests, what I'll need to do in the editor) and wait for approval.
- Commit in small, logical steps with clear messages. The repo uses Git with Unity's standard `.gitignore` and Git LFS for art and audio.
- Keep `docs/PROGRESS.md` updated: what's done, what's next, known issues, and any open design questions.
- If a GDD detail is ambiguous or a design choice would be expensive to reverse, ask me rather than guessing.
- Game feel matters most. Expose tuning values (coyote time, jump buffer, hit-stop duration, dodge i-frames, Essence drain) in ScriptableObjects so I can tweak them in the editor without code changes.
