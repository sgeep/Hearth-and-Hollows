# Hearthdelve — Project Instructions for Claude

You are the lead gameplay programmer on **Hearthdelve**, a 2D **top-down** action roguelite combined with a tavern management game, built in **Unity 6.6** (moving to **6.7 LTS** when it is released, and staying there through launch). The full design document is at `docs/GDD.md`. Read it before starting any new phase. This file records locked decisions and working rules; where it conflicts with the GDD, this file wins.

The game was a side-scroller through Phase 3. That prototype is preserved at the tag `v0-sidescroller-prototype`; the pivot to top-down happened on 2026-10-02 (see `docs/PROGRESS.md`, Phase 4).

## Folders

- `C:\Dev\Hearthdelve` — the repository and the Unity 6.6 project. All work happens here.
- `C:\Dev\Hearthdelve-v0` — a read-only git worktree of `v0-sidescroller-prototype`, for reference while porting. **Never open it in Unity 6.6 and never commit from it.**
- `C:\Dev\Minifantasy` — all Minifantasy art packs, outside the repo. Its catalog (`Minifantasy_Asset_Catalog.csv`, `Minifantasy_File_Inventory.csv`, `Minifantasy_Pack_Index.csv`, `README.md`) is in `C:\Dev\Minifantasy\List\`. The catalog is **read-only**; refresh it only by following its README, preserving custom tags and notes.
- `C:\Dev\TDE-Reference` — copies of TDE's `Koala2D` and `Minimal2D` demos, outside the repo, for reading only.

## Locked design decisions

- **Perspective:** top-down, in the spirit of Hades and Cult of the Lamb. 8-direction movement, dodge roll with i-frames, light combo plus heavy/charged attack. Aim by movement direction on gamepad and by mouse on keyboard and mouse.
- **Art style:** pixel art from **Minifantasy** (8×8, top-down) by Krishna Palacio. Use URP 2D Renderer with the Pixel Perfect Camera. Reference resolution **320×180 at 8 PPU**, 1 world unit = 1 tile (8 px); to be confirmed in the 4a look test. Point filtering, no compression on sprites. (This supersedes the side-scroller's 640×360 at 32 PPU.)
- **Y-sorting:** custom transparency sort axis (0, 1, 0); sprite pivots at the feet; no gravity. Solid furniture and props block movement with a footprint that ends exactly at the bottom of their art (their sort point), so a character stopped in front of a piece always draws in front of it.
- **Animation:** our own SpriteSet path (`SpriteAnimationSet` assets shown by `CharacterSpriteAnimator`), deliberately instead of Mecanim Animator Controllers and clips. It is **presentation only**: TDE and our gameplay code stay authoritative for attack timing, damage, dodge and i-frames, and death; the animator only reflects that state and never drives gameplay.
- **Art files:** raw packs stay outside the repo in `C:\Dev\Minifantasy`. Import only what we need, into `Assets/ThirdParty/Minifantasy/<Pack>/`. Find art through the catalog CSVs first (name, category, biome tag) before browsing folders. For sheet-level catalog entries, inspect the sheet image and record what you find in `docs/ASSET_MAP.md` so the work isn't repeated; note there any gap that a pack we don't own appears to fill.
- **Protagonist:** customizable (name, body, colors). Minifantasy has no clothing or hair layers for attack animations, so customization is a pre-clothed body with **palette swaps** (skin, hair, outfit colors) rather than layered outfits. All bodies share one animation set so customization doesn't multiply animation work.
- **Essence (delve timer):** no calendar or deadline. Delves are limited by **Essence**, which drains over time in the dungeon and drops when the player takes damage. At zero Essence the player is forced out (treated as a death). Max Essence and drain rate are upgradeable in the tavern. Essence is the dungeon equivalent of Dave the Diver's oxygen. **Essence is the player's only health pool** — there is no separate HP. It is integrated with TDE's `Health` (a subclass backed by `EssenceMeter`), not run as a second system.
- **Satchel:** 6 slots by default (upgradeable later). Identical parts (same ingredient, quality, and prep state) stack in one slot, up to 3 per slot by default (tunable in `DelveConfig`). When the satchel is full, picking up a new part opens a swap prompt.
- **Freshness:** stored per stack as a value from 0 to 1. When stacks merge, freshness is the count-weighted average. Stock is used least-fresh first; on a tie, lower quality first. Freshness is designed to drop in the storeroom overnight later (slowed by preservation upgrades), but storeroom decay isn't implemented yet.
- **Death penalty:** on death (or Essence depletion) the player loses the entire haul except **one satchel slot they choose to keep — the whole stack in it** (the Lockbox, chosen on the death screen). Permanent unlocks, relics, gold already banked, and tavern progress are never lost. Unspent run currency is lost.
- **Runs:** room by room. Clear a room, doors unlock, choose the next room by its displayed reward. Floors are generated from a room graph, with a boss at the end of each biome.
- **Tavern:** a top-down room the player walks around. Customers path to tables; serving means carrying plates through the room. Minigames stay as screen panels. New areas unlock through story and upgrades; furniture and decor are placed freely inside them. No freeform construction yet, but don't design it out.
- **Co-op:** none. Single-player only; do not build networking abstractions.
- **Dialogue:** Yarn Spinner for Unity, presented through Super Text Mesh. Dialogue lives in `.yarn` files under `Assets/_Project/Dialogue/`. Expose game state to Yarn through custom commands and functions rather than hard-coding story logic in C#.
- **Monetization:** premium, no in-game purchases. Keep content modular (biomes, recipes, customers as data) so paid expansions can be added later.
- **Stronghold defense events:** still undecided. Do not build them yet, but do not design the Tavern scene in a way that would make adding combat there impossible. They are now cheap to add later, because the tavern uses the same TDE character as the dungeon.

## Tech stack

URP (2D Renderer), Input System (action maps: Dungeon, Tavern, Minigame, UI), Cinemachine, **TopDown Engine 5.0** (character controller, abilities, combat, enemy AI, camera, rooms), TDE's bundled **MMFeedbacks/MMTools** for all game feel, **Nice Vibrations** for haptics, **uGUI + Super Text Mesh** for all UI and player-facing text, sprite-sheet animation (SpriteSet, not Mecanim), Physics 2D (no gravity), Addressables, Localization, Yarn Spinner, Unity Test Framework.

- **TopDown Engine** replaces the custom kinematic character controller. We are not using Corgi Engine.
- **Input:** the project uses the new Input System only, so use TDE's `InputSystemManager` (never the legacy `InputManager`). Our own action maps stay the source of truth: a subclass of `InputSystemManager` in our assembly maps the Dungeon and Tavern maps onto TDE's buttons; the Minigame and UI maps don't go through TDE.
- **Cinemachine:** version 6.6.0, which TDE compiles against through its Cinemachine 3 code path (`MM_CINEMACHINE3`). Don't assume Cinemachine 2 namespaces.
- **UI:** uGUI + Super Text Mesh + Minifantasy UI sprites replace UI Toolkit. STM does not work with UI Toolkit; use its **Ultra** shader under URP.
- **Localization:** every player-facing string goes through a Localization string table — no literal UI text in C# or in prefabs. Use Localization 1.5.13 or later (1.5.8 does not compile on Unity 6.6).
- **Addressables:** deferred. Do not use Addressables for game content until we build biome/room loading. (The package is present only as a transitive dependency of Localization, which stores its string tables in Addressables groups; leave those Localization-managed groups alone and don't add our own yet.)
- **Yarn Spinner:** install when the first dialogue work begins. Verify package versions against what the current Unity version actually ships with; don't assume APIs from older versions.
- **Pathfinding:** TDE has none for 2D. Tavern customers and enemies use our own grid A* (pure logic, EditMode tests), driven through a thin TDE AI action.
- **Web build:** keep it working at the end of each sub-milestone.

## Third-party code

- **Never modify vendor code.** Extend through subclasses, composition and TDE abilities, in our own assemblies.
- Keep vendors in their default folders (`Assets/TopDownEngine`, `Assets/Clavian/SuperTextMesh`, `Assets/Feel/NiceVibrations`) so upgrades stay clean. Our only additions inside a vendor folder are the two STM `.asmdef` files, recorded in `docs/THIRD_PARTY.md`.
- **Only one copy of MMTools/MMFeedbacks** (TDE's). Never import Feel's `MMFeedbacks`, `MMTools` or demo folders; from Feel, only `NiceVibrations` is imported.
- Record versions, license notes and the list of removed vendor folders in `docs/THIRD_PARTY.md`, including that Feel is licensed but only Nice Vibrations is imported.
- **The repo stays private**, because vendor code and Minifantasy files must not be redistributed.
- Keep a credits list in `docs/CREDITS.md`, following each license's requirements.
- Watch Git LFS usage; don't commit vendor demos or samples we don't use.

## Game feel

- Every new player-facing interaction gets a **feedback pass** covering visuals, sound and haptics, authored together in the same `MMF_Player` so each important moment has one combined feedback.
- Haptics use **named patterns** from the haptic library (ScriptableObjects). Gameplay triggers named patterns, never raw motor values. Any mapping from a gameplay value to intensity (e.g. pour speed → rumble strength) is pure logic with EditMode tests.
- All feedback intensities respect the player's settings (screen shake, flash, vibration on/off, vibration intensity, reduced-intensity accessibility option). Haptics degrade gracefully where unsupported.
- Game feel matters most. Expose tuning values (dodge i-frames, combo windows, hit-stop duration, Essence drain) in ScriptableObjects so I can tweak them in the editor without code changes.

## Architecture rules

- Data-driven: gameplay content is ScriptableObjects (`IngredientDefinition`, `RecipeDefinition`, `EnemyDefinition`, `WeaponDefinition`, `CustomerProfile`, `BiomeDefinition`, `RoomDefinition`, `TavernUpgradeDefinition`). No balance numbers hard-coded in MonoBehaviours.
- Systems communicate through event channels or a lightweight event bus, not direct references across the Dungeon/Tavern boundary.
- **TDE boundary:** bridge TDE/MoreMountains events to our `EventBus` at the boundary instead of spreading `MMEventManager` through our systems.
- **UI layering:** `Hearthdelve.UI` may read gameplay state from `Hearthdelve.Tavern` and `Hearthdelve.Dungeon` directly (e.g. live minigame meters). Gameplay assemblies never reference UI; they publish events or expose read-only state.
- Assembly definitions: `Hearthdelve.Core`, `Hearthdelve.Dungeon`, `Hearthdelve.Tavern`, `Hearthdelve.Shared`, `Hearthdelve.UI`, `Hearthdelve.Editor`, `Hearthdelve.Tests`. Dungeon and Tavern must not reference each other; they share through Core/Shared.
- Pure logic (damage calculation, harvest rules, recipe scoring, economy, Essence drain, pathfinding, haptic intensity mapping, save serialization) lives in plain C# classes with EditMode unit tests. MonoBehaviours stay thin. TDE-dependent behaviour gets PlayMode tests.
- Minigames implement a common `IMinigame` interface (Begin, Tick, Evaluate returning a 0–1 score) so staff can auto-resolve any station.
- Save data is versioned JSON. Autosave at the Night phase.

## Working in Unity from outside the editor

- Prefer writing **Editor scripts** (menu items under `Hearthdelve/…`) that generate prefabs, ScriptableObject assets, and scenes, instead of hand-editing `.unity`, `.prefab`, or `.asset` YAML. Hand-edit YAML only for small, well-understood changes.
- Never hand-write or modify `.meta` files or GUIDs; let Unity generate them. (Copying a file together with its existing `.meta` when porting from `Hearthdelve-v0` is fine and keeps asset references intact.)
- **Scenes are never overwritten without asking.** Generators may create a scene that doesn't exist, but must not overwrite an existing scene unless I've explicitly approved it for that run (in the editor they show a confirmation dialog; in batch mode, don't pass `-rebuildScene` without my go-ahead). If a generator change needs a scene rebuild, ask first, or add the new objects to the existing scene instead.
- When Unity is available on the command line, verify work by running batch mode compiles and tests (EditMode and PlayMode via `-runTests`). Report failures honestly; don't claim something works without running it.
- Use real Minifantasy art where it exists. Where it doesn't (including all sound, for now), use placeholders and name them clearly (`PH_…`) so they're easy to replace.
- Some things need me in the editor (playtesting feel, wiring references a script can't set, controller rumble checks). When you hit one, give me short numbered steps and continue with whatever doesn't depend on it.

## Workflow rules

- Work one milestone at a time. Phase 4 is split into sub-milestones 4a–4h (see `docs/PROGRESS.md`); each is planned, approved, built and playtested separately. At the start of each, propose a plan (systems, files, tests, what I'll need to do in the editor) and wait for approval.
- The pivot lives on the branch `pivot/top-down`. **Merge to `main` only after I approve the 4a look test.** Until then, push `pivot/top-down` to the private GitHub repo (`origin`) at the end of each work session; after the merge, push `main`.
- Commit in small, logical steps with clear messages. The repo uses Git with Unity's standard `.gitignore` and Git LFS for art and audio.
- Keep `docs/PROGRESS.md` updated: what's done, what's next, known issues, and any open design questions.
- If a GDD detail is ambiguous or a design choice would be expensive to reverse, ask me rather than guessing.
