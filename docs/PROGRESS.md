# Hearthdelve — Progress

_Last updated: 2026-10-03 (4c step 2)_

## Phase 4 — Vertical slice, rebuilt top-down

### The pivot (2026-10-02)

Before Phase 4 the game changed from a side-scroller to a **top-down action roguelite with tavern management** (closer to Cult of the Lamb and Hades than to Dead Cells), rebuilt on a fresh Unity 6.6 project with third-party assets:

- **TopDown Engine 5.0** for the character controller, combat, AI, camera and rooms, with its bundled **MMFeedbacks** for game feel.
- **Nice Vibrations** (from Feel) for haptics.
- **Super Text Mesh** with uGUI for all UI and text, replacing UI Toolkit.
- **Minifantasy** (8×8, top-down) for all art.

The core loop, Harvest system, Essence, ingredients, recipes, tavern minigames, economy and story are unchanged. Design details are in `docs/GDD.md` (v0.2); working rules are in `CLAUDE.md`.

- The side-scroller prototype (Phases 1–3) is preserved at the tag **`v0-sidescroller-prototype`** and checked out read-only at `C:\Dev\Hearthdelve-v0`.
- The pivot was built on the branch **`pivot/top-down`** and merged into `main` on 2026-10-02, after the 4a look test was approved. Work continues on `main`.
- Everything below the "Phase 1–3 history" heading describes the prototype at that tag, not the current project.

### Sub-milestones

Each is planned, approved, built and playtested separately. The web build must work at the end of each.

| | Sub-milestone | Contents |
|---|---|---|
| **4a** | Integration and look test (**complete**, 2026-10-02) | Project swap, port manifest, logic and tests ported, Nice Vibrations, Minifantasy import pipeline. One dungeon room and one tavern corner with real art at 320×180: TDE player with Essence, dodge and a melee combo; one enemy; one harvest drop; one combined hit feedback (visual + sound + haptic); one STM speech bubble; Y-sorting. `docs/ASSET_MAP.md`. |
| 4b | Dungeon migration (**complete**, 2026-10-03) | Phase 1 and 3 dungeon gameplay rebuilt on TDE (combat, harvest, Essence, death and Lockbox, extraction), with the dungeon haptics. Dungeon-only: ends on a result screen with restart. |
| 4c | Tavern and UI migration | Top-down tavern, customers pathing to tables, 2D serving, Grill/Tap/Serving with their haptics, all UI rebuilt in uGUI + STM. **Acceptance criterion: the complete top-down day loop is restored through `GameFlow` (Tavern → Dungeon → Tavern, Morning to Night, saves).** |
| 4d | Biome 1 runs | Room-by-room structure, room rewards, run power-ups, 3 floors plus a boss arena. |
| 4e | Combat depth and boss | Harvest Finisher, Kitchen Arts, 3–4 more weapons with rarity and affixes, Essence Tonics, field cooking, Delve Marks and the Delver's Board, one relic, the Biome 1 boss. |
| 4f | Tavern Stage 1 content | Butcher Block minigame, all Biome 1 recipes, customer requests, Pip and Gundra, furniture and decor placement. |
| 4g | Story, quests and character creation | Dialogue System for Unity and Quest Machine with a Super Text Mesh presenter, Minifantasy Portrait Generator portraits, architecture and hooks for Love/Hate later; the Act I opening, onboarding, character creation. (Locked 2026-10-03; replaces Yarn Spinner.) |
| 4h | Menus, options and polish | Settings (screen shake, flash and vibration intensity), accessibility per GDD 12, audio system, web build. |

### 4a status: complete

**Approved on 2026-10-02** and merged into `main`. Locked by the approval: **320×180 at 8 PPU**, and **URP 2D lit sprites** as the visual baseline (CLAUDE.md, GDD §8.1). Next: the 4b plan, awaiting approval.


**Done:**

- **Project swap.** The repo now holds the Unity 6000.6.4f1 project; history is preserved. Vendor demo and sample folders were removed before the first commit (Assets 488 MB → 92 MB, 69 MB in Git LFS). The list is in `docs/THIRD_PARTY.md`.
- **Vendors compile cleanly** in batch mode: TopDown Engine 5.0, Super Text Mesh (with two assembly definitions we added), Nice Vibrations 4.1.2. Only one copy of MMFeedbacks/MMTools exists, and TDE's haptic feedbacks compile against Nice Vibrations.
- **Port manifest** written: `docs/PORT_MANIFEST.md` (94 Keep, 72 Adapt, 105 Drop).
- **Pure logic and EditMode tests ported:** 209 ported EditMode tests pass in batch mode with 0 compiler errors and 0 warnings in our code. Retired with the code they covered: 24 platformer-motor tests and 4 hit-stop tests.
- **New pure logic with tests** (34 tests), all in `Hearthdelve.Core`:
  - `GridMap` / `GridPathfinder`: 8-direction A* that never cuts corners.
  - `FacingLogic`: 8-direction input → the four drawn facings.
  - `HapticMath` / `HapticRamp`: settings scaling, gameplay value → rumble strength, layering, low-Essence heartbeat timing.
  - `GameSettings` gained vibration on/off, intensity and the reduced-intensity option.
- Docs: `CLAUDE.md` updated, GDD rewritten as v0.2, `docs/THIRD_PARTY.md` and `docs/CREDITS.md` added.

- **Project setup** (*Hearthdelve → Setup → Configure Project Settings*): no gravity; custom transparency sort axis (0, 1, 0) on the 2D renderer and in Graphics settings; a `Pickup` layer beside TDE's layers (which keep TDE's indices); the Nice Vibrations define on Standalone and Web; stale demo scenes removed from the build list.
- **Input:** `HearthdelveInputManager`, a subclass of TDE's `InputSystemManager`, maps our Dungeon or Tavern action map onto TDE's movement and buttons. The Dungeon map was rebuilt for top-down (no jump; adds `AimPoint` and `Heavy`). Aim follows the mouse on keyboard and mouse and the movement direction on a gamepad (`AimControlSwitcher`).
- **Minifantasy import pipeline:** `MinifantasySheets` lists every imported image and how it is sliced; *Hearthdelve → Art → Import Minifantasy* copies them from `C:\Dev\Minifantasy` and a postprocessor applies 8 PPU, point filtering, no compression and the slicing. 32 sheets from six packs are in (about 0.9 MB). `docs/ASSET_MAP.md` records what each sheet contains.
- **Haptics:** `HapticPattern` assets for the whole starting vocabulary (15 patterns, `Data/Haptics`), a pure `HapticMixer` (one-shots, continuous channels, settings), and `HapticService`, which sends the result to the gamepad through Nice Vibrations. `MMF_HapticPattern` plays a pattern from an `MMF_Player`. With no controller, and always on the web, it does nothing.
- **TDE boundary:** `TdeEventBridge` republishes TDE damage, death, revive and level-start events on our `EventBus` (`CharacterDamaged`, `CharacterDied`, `CharacterRevived`, `LevelStarted`). Nothing else of ours listens to `MMEventManager`.
- **Essence:** `EssenceHealth`, a subclass of TDE's `Health` backed by `EssenceMeter`. It is the player's only health pool: it drains over time, drops when TDE applies damage, and at zero the character dies through TDE's death path and `PlayerDefeated` is published once.
- **Tuning stays in assets:** `PlayerTuning` applies `PlayerMoveConfig` (walk speed, dodge distance, duration, cooldown, i-frames) to the TDE abilities; `ComboWeaponTuning` applies the cleaver's `WeaponDefinition` to the TDE combo weapon; `EnemyIdentity` applies `EnemyDefinition`.
- **Sprite animation:** `SpriteAnimationSet` assets generated from the Minifantasy sheets and their frame-duration guides, shown by `CharacterSpriteAnimator` (four drawn facings, state taken from the TDE character). Approved as the animation path instead of Mecanim, presentation only (CLAUDE.md, GDD §10.1).
- **Harvest:** `HarvestSystem` turns an enemy's death into drops through the existing `HarvestRules`; `IngredientPickup` puts them in the satchel (`SatchelCarrier`).
- **Localization on the web:** `Loc.Preload` loads the string tables without blocking; `LocalizationBoot` runs it in every scene and `LocalizedSuperText` refreshes when the tables arrive. On the web a lookup never waits synchronously.
- **The look test** (*Hearthdelve → Generate → 4a Look Test (All)*): `LookTest_Dungeon` and `LookTest_Tavern`, with real Minifantasy art.
  - Dungeon room: a 22×11-tile room from the Dungeon tileset with props and torches; the player (Human Townsfolk) with Essence, a dodge roll with i-frames and the three-hit cleaver combo; two Green Slimes that chase and deal contact damage; a harvest drop to pick up; one combined hit feedback (flash, screen shake, placeholder sound and the `Tap.Firm` haptic in a single `MMF_Player`); props that sort by Y.
  - Tavern corner: the premade room from the Tavern Indoor add-on; the same TDE character without weapon or Essence; a cook behind the bar with a Super Text Mesh speech bubble that appears when the player is near.
  - **F2** cycles the reference resolution (320×180, 240×135, 160×90) so character scale can be compared live. **F3** switches between the two scenes.

Tests, all passing in batch mode with 0 compiler warnings in our code:

- **293 project EditMode tests** (the run reports 294; a package adds one stub test). 50 are new in 4a, including the pixel stepping rule and VSync: haptic envelope and mixer, the haptic library asset, sprite timing, Minifantasy import settings, `IngredientItem` identity, and the render pipeline setup.
- **21 PlayMode tests** against the two look-test scenes: the top-down setup, the camera follow keeping the player on one screen pixel and stepping diagonals cleanly while walking diagonally and dodging (both scenes), the speech bubble staying locked to its speaker, lit sprites and representative lighting in both scenes, Essence as the only health pool and its drain, keyboard movement, walls, the dodge roll and its i-frames, gameplay with every sprite animator switched off (the animator is presentation only), enemy contact damage with its feedback, the combo killing a slime that drops a harvest the player picks up, death at zero Essence (reported once), haptics respecting settings and doing nothing without a controller, the tavern character, the bar and both table sets blocking the player from below, furniture footprints ending at their sort point, the localized speech bubble, table preloading, and the resolution switch.

**Web smoke test (2026-10-02): passed.** A development web build (`Builds/Web`, 125 MB, 0 errors), served locally and played in Chrome:

- Both scenes render; the camera follows the player; walls and the bar block movement.
- The Localization tables load from the build's Addressables bundles, and every Super Text Mesh text shows its English string: the HUD labels, the controls hint, the resolution label and the cook's speech bubble.
- F2 changes the reference resolution and F3 switches scenes.
- A slime's contact damage plays the combined hit feedback (flash, shake, sound, haptic) with no errors; rumble does nothing on the web, as intended.
- Essence reaching zero kills the player and the room restarts.
- The browser console shows no errors from the game.

**Fixed after the first look-test review (2026-10-02):**

- **The room shook whenever the player moved** (both scenes, controller and keyboard alike; fine when standing still). Measured in PlayMode, frame by frame at 60 fps:
  - No impulse or MMFeedbacks shake fired during movement.
  - With the camera follow disabled the world was stable, so the fault was in the follow / pixel-perfect pipeline.
  - The Pixel Perfect Camera is the only system that snaps or sizes the camera, and its snapped position never stepped backwards.
  - The cause was the follow's 0.4 damping. The camera trailed the player by a fractional amount that changed every frame, so the player and the camera rounded to different art pixels on alternate frames. The player's on-screen position flickered by 1 px, 31–87 times in a 1.2 s walk (up to 8 px of range in the dungeon), and with the eye on the player the whole room appeared to shake.
  - The Brain's SmartUpdate was a second risk: on FixedUpdate timing the camera stalled on frames without a physics step.

  After the first fix, the room still shook slightly, much more on diagonals, and the tavern text shook. Three more causes, each measured:
  - **Diagonal zig-zag:** the Pixel Perfect Camera rounds X and Y independently. Unless a diagonal walk happened to start on whole pixels, the axes crossed pixel boundaries on different frames, so the room stepped sideways one frame and up the next. In the tavern, 40 of 58 camera steps moved one axis only. Now the player is drawn at a pixel-snapped display position that steps like a Bresenham line along the direction of movement: the slower axis steps on the faster axis's frames, and the lead axis only changes when the other is clearly faster (otherwise float noise at exactly 45 degrees flips it). The camera follows that drawn position. Result: 0 single-axis steps during steady diagonal movement in both scenes. This is presentation only (`PixelSnappedPresentation`, rule in `Core/Movement/PixelStepping` with EditMode tests); gameplay positions are untouched, and the drawn position stays within 1.25 px of the real one.
  - **Speech bubble:** it was placed from the unsnapped camera, possibly before Cinemachine had moved it, so it slid smoothly while the room under it moved in whole-pixel steps (it took 12 different positions against the cook in one short walk). It now updates after Cinemachine and places itself from the snapped camera in whole art pixels: one constant offset.
  - **Frame pacing:** the quality level used for Windows and the web ("Fantastic") had VSync off, so frames were uncapped and the pixel steps landed unevenly against the display. VSync is now on for every quality level (the web uses requestAnimationFrame).

  Fix: the Brain updates in LateUpdate and the follow has no damping. The player now stays on one screen pixel while walking diagonally and dodging, in both scenes. The scenes and player prefabs were updated in place (*Hearthdelve → Generate → Update Look Test Camera*), and `CameraFollowTests` guards it: the player stays on one screen pixel, the camera never steps on one axis during steady diagonal movement, and the bubble stays locked to the cook. Pixel Perfect Camera, 320×180 / 8 PPU and the lit baseline are unchanged. The camera no longer eases after the player; if softer camera motion is wanted later, it has to keep the player on a fixed screen pixel.

- **Clipping into the bar from below.** Not movement through a collider: the bar's collider ended at the counter, 6 px above the bottom of its art (its stool row), so a player stopped below the counter was already behind the bar's sort point and drew behind it. The table sets had the same gap (their collider covered only the table, not the chairs). Each furniture piece now carries its own collision, and every footprint ends exactly at the bottom of its art. This is the rule for all solid furniture and props (CLAUDE.md, Y-sorting). The existing tavern scene was updated in place (*Hearthdelve → Generate → Update Tavern Furniture Collision*), not rebuilt.
- **The project was not running URP.** Graphics Settings pointed at a render pipeline asset that doesn't exist: the 2026-10-02 project swap kept the old repo's `UniversalRP.asset.meta`, which gave the asset a different GUID. Unity fell back to the Built-in pipeline without any error, in the editor, the tests and every build. On the web this showed up as missing text: build-time shader stripping removed every URP-tagged shader, Super Text Mesh's included. `ProjectConfigurator` now assigns `Assets/Settings/UniversalRP.asset`, and an EditMode test guards it. The look did not change at that point, because the generated sprites still used the unlit default sprite material; they are lit now (see "Decided at the end of 4a").

**4a sign-off:** the lit look test was approved on 2026-10-02 after the final visual check.

### Checks made for the pivot

1. **Import:** TDE 5.0 and STM compile on Unity 6000.6.4f1 with 0 errors. STM gives 22 obsolete-API warnings (worth re-checking on 6.7). Package changes are listed in `docs/THIRD_PARTY.md`.
2. **Nice Vibrations on its own:** works. Only `Feel/NiceVibrations` was imported; there is no duplicate MMTools/MMFeedbacks.
3. **Rumble platforms:** off mobile, Nice Vibrations rumbles through the Input System (`Gamepad.SetMotorSpeeds`). Expected, **not yet tested on hardware**: Xbox controllers work; DualShock 4 and DualSense work over USB; Switch Pro has no rumble on PC unless Steam Input presents it as an Xbox pad; web builds have no rumble. Unsupported cases must do nothing, silently.
4. **Art:**
   - Sheets are 32×32 frames with an ~8×8 body, four rows for four **diagonal** facings (front-right, front-left, back-right, back-left); death is a single row.
   - Creatures, True Heroes and many exclusives have full idle/walk/attack/damage/die sets.
   - A Myriad of NPCs has layered outfits and hair only for idle, walk, damage and die, so it can't be a layered combat character. The Weapons pack layers weapons over a bare body for all attacks.
   - There is no rat with an attack, no sound of any kind, and no pixel font found yet.
5. **Pathfinding:** TDE's is NavMesh-based and 3D-only. We write our own grid A* in pure C#.

### Decided for the pivot (2026-10-02)

1. **Player customization:** a pre-clothed body with palette swaps (skin, hair, outfit colours), not layered outfits.
2. **Biome 1 roster:** the Giant Rat is replaced by the Bat and Giant Spider; the boss is the Mother Slime, replacing the Cellar King. Final mapping is reviewed in `docs/ASSET_MAP.md`.
3. **Super Text Mesh** gets two assembly definitions inside its folder (no code changes).
4. **Sound:** generated placeholders for now. Real SFX and music need a source later.
5. **GDD** converted from an HTML export to Markdown.
6. **Technical:** 320×180 at 8 PPU (to confirm in the look test), Unity 6.6 now and 6.7 LTS on release.

### Needs you in the editor (4a)

Done on 2026-10-02: Web Build Support installed; the look scenes, the camera scales and controller rumble tested and reported good.

- **Tuning** (changes made in Play mode apply immediately): `Data/Config/PlayerMoveConfig` (walk speed, dodge), `Data/Weapons/Weapon_ButchersCleaver` (combo damage, timing, reach), `Data/Config/EssenceConfig` (drain, post-hit invulnerability), `Data/Enemies/Enemy_GreenSlime`, and the patterns in `Data/Haptics`.

### 4b plan (approved 2026-10-02)

Scope: the Phase 1 and 3 dungeon rebuilt on TDE in a new **`Dungeon_TestFloor`** scene (three hand-built rooms and an extraction rope). `LookTest_Dungeon` and `LookTest_Tavern` stay untouched as the known-good visual, camera and rendering baselines through 4b.

Order of work:

1. Test floor scene, grid pathfinding, and the pathfinding AI action.
2. Damage pipeline, heavy/charged attack, hit-stop (MMFeedbacks freeze-frame), simple knockback and stagger, and the three enemies with telegraphs.
3. Harvest, satchel, freshness and the swap prompt; rat ingredients re-themed.
4. Low Essence, death and Lockbox, extraction, and the result screen.
5. Dungeon HUD.
6. Feedback and haptics pass.
7. Web build smoke test; ASSET_MAP, PROGRESS and PORT_MANIFEST updated.

Decisions (2026-10-02):

1. **Day loop:** 4b is dungeon-only and ends on a result screen with restart. 4c reconnects the full loop through `GameFlow` (an explicit 4c acceptance criterion).
2. **Roster:** Green Slime, Bat and Giant Spider. Mushroom People are deferred until there is suitable attack art.
3. **Ingredients:** the rat ingredients are re-themed to bat and spider parts. The asset files are renamed in Unity (GUIDs and references kept), and their ids and data change; recipes are updated to match. Prototype saves need not load.
4. **Skills 1 and 2** (GDD §4.1, tools and throwables) are out of 4b. Raise them when planning 4e without widening 4e's approved scope; if the vertical slice doesn't need them, they can wait for Phase 5.
5. **Player body:** the Human Townsfolk stays as the stand-in until character creation (4g).

Adjustments: satchel quality is shown by an icon or mark as well as a tint (never colour alone); knockback and stagger stay a simple hit response, not a poise system; pathfinding tests check that the real character collider follows paths around corners and props without clipping.

### 4b status

**Step 1 done (2026-10-02): test floor, grid pathfinding, pathfinding AI action.**

- **`Dungeon_TestFloor`** (*Hearthdelve → Generate → 4b Test Floor*): three Cellar rooms joined by doorways, drawn from a text map in `TestFloorBuilder` and auto-tiled from the Dungeon tileset's wall sample. Room A (spawn, props), room B (pillars, a slime), room C (a U-shaped wall, a column of crates and barrels, a slime). Torches with the 4a lighting. It is first in the build list; F3 still opens the tavern look test. The 4a look scenes were not touched.
- **Enemy pathfinding:** `NavGrid` (in the scene) bakes the walkable grid from the obstacle colliders on first use; a tile is blocked when any collider overlaps it, even partly. `AIActionPathfindToTarget2D` (a TDE AI action in `Hearthdelve.Shared`) heads straight for the target when the enemy's whole collision box can slide there, and otherwise follows a grid A* path, re-planned four times a second. The follower aims at the furthest waypoint the box can reach in a straight line, so movement is smooth, not cell by cell.
- The slime prefab uses the new action. In the 4a look room (no `NavGrid`) it heads straight for the player as before, but no longer stalls when lined up with the player.
- **Tests:**
  - EditMode: the world-to-grid mapping, the box sweep (including a box that would clip a corner its centre line clears), the nearest open tile, the follower, and a simulated box following paths around a U-shaped wall, through a doorway and around a column of props at three sizes and speeds, never overlapping a blocked tile.
  - PlayMode: the baked grid matches the map; a real slime chases the player around the U-shaped wall, around the crate column and through a doorway into the next room. Its collider never touches a wall or prop on any physics step (0 touching, 0 overlapping in all three).
- **Camera test fix:** the 4a camera test failed about one run in four in the tavern. When a 45° walk starts with the axes at different sub-pixel phases, the first step can move one axis only to bring them into line; after that they step together. The stepping unit tests already allowed this, but the PlayMode test counted it as a zig-zag. It now allows one alignment step per diagonal run and still fails on any one-axis step after that.

**Diagonal judder, round 3 (2026-10-02).** After step 1 the room still looked jittery on diagonals (straight lines looked fine). Two separate things:

- **A real stepping bug.** On a frame where the lead axis stepped, the follow axis decided from its error on that frame, which depends on how far into the frame the lead axis crossed its half pixel. A steady diagonal could pass on one step and fail on the next, so the world occasionally stepped one axis only, even after lining up. A simulation of TDE's acceleration showed it in about 1 walk in 11. Fixed in `PixelStepping`: the follow axis is measured at the lead axis's crossing point, along the line of movement (as in Bresenham's line), and if it would be too far behind by the next lead step it takes its single alignment step first. At most one alignment step per walk, always at the start, then lockstep; the drawn player stays within 1.5 px of its real position. New EditMode test: 500 accelerating 45° walks from random phases. (The camera PlayMode test had been allowing one late alignment step; that was this bug.)
- **The cadence of whole-pixel scrolling** (not a bug). At 6 tiles/s and 60 Hz the world moves 0.8 art px per frame on a straight line (it steps on 4 frames out of 5) but 0.57 per axis on a diagonal: it steps on only about every other frame, irregularly, and each step is a 1.4 px diagonal jump. That is inherent to moving the camera in whole art pixels. To compare alternatives, **F4** in the look scenes and the test floor now cycles the scroll mode (debug only; the default stays the locked pixel-perfect setup):
  1. **Pixel-perfect** (default): whole art pixels; the world moves on about 55% of frames on a diagonal.
  2. **Half-pixel:** the same low-res pipeline at 640×360, so positions snap to half art pixels; the world moves on every frame. Sprites can sit half an art pixel apart, and lighting is rendered at 640×360.
  3. **Smooth:** rendered at screen resolution; the camera and characters move in screen pixels on every frame. Art stays crisp, but nothing is held on the art grid, and 2D lighting is rendered at full resolution (softer).
  `CameraFollowTests` checks that every mode keeps the player fixed on screen and measures how often the world moves.

  **Decision (2026-10-02): smooth scrolling.** After comparing the three modes, smooth fixed the diagonal judder completely and is now the locked default (CLAUDE.md, GDD §8.1). All three scenes were switched in place (*Hearthdelve → Generate → Update Look Test Camera*), and the generators build it. The player's pixel snapping is off by default; F4 keeps the pixel-perfect and half-pixel modes for comparison only, and the pixel-perfect mode keeps its stepping regression test.

**Step 1 approved (2026-10-02).** Pathing works; enemy-to-enemy avoidance stays a known issue unless it becomes a practical combat problem.

**Step 2 done (2026-10-02), awaiting review and playtest: damage pipeline, heavy attack, hit-stop, knockback and stagger, the three enemies with telegraphs.**

- **Damage pipeline.** Player attacks are `CombatMeleeWeapon`s (a TDE `MeleeWeapon` subclass in our assembly): TDE deals the damage; when an attack lands, it passes the rest of the hit (its `AttackData`, its `WeaponDefinition`, light or heavy) to the target's `HitReaction`. A kill's harvest now uses the weapon that actually landed the killing blow (element, clean-kill categories), not a scene setting.
- **Heavy attack** (hold right mouse / gamepad north). TDE's `ChargeWeapon` on the secondary weapon slot, with three steps in `WeaponDefinition.heavy`: a tap (14 damage), held 0.45 s (22), held 0.9 s (34). The release is the Minifantasy charged spin, which hits all around the player. Pure `HeavyCharge` turns our charge times into TDE's step durations (tested to agree at every hold time). `PlayerAttackGate` keeps the light combo and the heavy from overlapping and slows the player to 40% while charging. The player shows the wind-up, then a charged loop, then the spin.
- **Hit-stop** through MMFeedbacks: each attack's hit feedback has an `MMF_HitStop` (MMFeedbacks' freeze frame, skipped when the hit-stop setting is off), its length taken from `AttackData.hitStop` (0.06 s light, up to 0.12 s for a full heavy). Generated scenes and the test floor have an `MMTimeManager`. The unused `GamePause` and `HitStopRequested` leftovers are gone.
- **Knockback and stagger** stay simple (`StaggerRules`, `HitReaction`): a hit slides the enemy away (starting at `AttackData.knockbackForce` tiles/s, stopping after 0.15 s), interrupts its attack, and holds it still for the attack's stagger time, scaled by the enemy's multipliers. Super armour (the slime, while leaping) ignores all of it. TDE's own force knockback does nothing with our characters: its 2D controller moves by `MovePosition` every physics step, which cancels the force. Hence the slide, which goes through TDE's movement ability, so walls and props stop it.
- **Melee aim is now instant.** TDE's weapon aim eased round at one turn per second by default, so an attack right after moving the mouse could swing the old way (this affected 4a too).
- **Enemies.** One `EnemyAttack` per attack, timed by `AttackCycle` from `EnemyDefinition` (all timings, ranges and damage are data): telegraph (stop, face the target, red "!" over the head, flash, rising warning sound, the attack animation's wind-up frames stretched over the telegraph), then active (the only time it can hurt), recovery and cooldown. TDE `AIBrain`s: Idle or Sleep → Chase (step 1's pathfinding) → an attack state per attack when its range and line are clear.
  - **Green Slime:** leap (0.6 s telegraph), super armour while attacking (the prototype's decision).
  - **Bat:** hangs asleep until the player comes within 4 tiles or hits it, wakes, flies after the player with a fluttering wobble (kept clear of walls), and swoops along the telegraphed line.
  - **Giant Spider:** keeps 3–5.5 tiles away (backs off if the player closes in), spits a web (0.8 s telegraph) that walls and props stop, and bites when the player gets within 1.8 tiles.
  - Enemies no longer hurt on contact: only their telegraphed attacks do (GDD §4.1).
- **Data.** `Enemy_GiantRat` and `Enemy_CellarShroom` were renamed in Unity to `Enemy_Bat` and `Enemy_GiantSpider` (GUIDs and references kept) and filled with the new data once (an `EnemyDefinition.schema` version, so later tuning is kept). Their harvest lists still name the rat and shroom ingredients until step 3 re-themes those.
- **Test floor** updated in place (*Hearthdelve → Generate → Update Test Floor Enemies*): a time manager, a bat in room B and in room C, and the spider in room C. Not rebuilt.
- **Art:** Bat (Creatures), Giant Spider (exclusive) with its web, the Human Townsfolk's ChargedAttack, and the red "!" from the User Interface pack (see `docs/ASSET_MAP.md`). Placeholder sounds `PH_Telegraph` and `PH_Whoosh`.
- **Tests:**
  - EditMode: heavy-charge rules (and TDE agreement), telegraph frame timing, stagger and knockback rules, and the roster's data (every attack readable, wired to its prefab, its animation and release frame present, hitboxes off until active).
  - PlayMode on the test floor: a charged heavy hits harder than a tap and plays wind-up, charged loop and spin; a light hit freezes time briefly and not when hit-stop is off; a hit interrupts the bat's telegraphed swoop, staggers it and knocks it back; a hit doesn't interrupt the slime's leap; each of the four enemy attacks shows its "!" and can't hurt before its telegraph has fully played, then deals its data's damage; dodging sideways avoids the slime's leap, with i-frames during the roll; the bat sleeps until the player comes near, then gives chase; the spider backs off to keep its distance.

**Step 2 playtest (2026-10-03):** heavy attack, telegraphs, hit feel and the alert approved; tuning stays as it is. One fix asked for and done:

- **Bats hang from walls.** Their sleep pose is a bat hanging upside down, so a sleeping bat in the middle of a room looked like it was floating. Now sleeping bats perch: `EnemyPerch` hangs a bat from the wall directly above it (the pose is drawn on the brick face; no ground shadow while hanging). When it wakes (the player comes near, or it is hit) it lets go and plays its unfold animation in place (a new Wake state in its brain, as long as the animation), then flies with its shadow back and chases and swoops as before. A hit on a hanging bat makes it let go rather than slide along the wall.
- **Placement and fallback.** The test floor's bat markers moved to the floor tile right under a north wall (room B and room C); the generator warns about any bat marker without a wall above it, and the in-place update moved the existing bats onto their perches. A bat placed with no wall above it never shows the hanging pose: it hovers in its flying idle, with its shadow, and logs a warning.
- **In the editor** a sleeping bat prefab shows its hanging sprite, so layouts read as they will in play.
- **Tests:** PlayMode, every bat on the floor starts hanging under a wall (sleep pose, no shadow), and when the player comes near it lets go, unfolds in place, then flies after the player with its shadow; a bat placed in open floor warns and hovers instead. EditMode, the bat prefab has its perch wired to its shadow.

**Step 2 approved (2026-10-03).**

**Step 3 done (2026-10-03), awaiting review and playtest: harvest, satchel, freshness, the swap prompt, and the rat ingredients re-themed.**

- **Re-theme.** The prototype's rat parts are now the giant spider's, renamed in Unity so GUIDs and every reference (recipes, the game database, the tavern) are kept: Rat Haunch → **Spider Leg** (meat), Rat Liver → **Venom Sac** (offal; same categories, so recipes still read right). The bat has its own new part, **Bat Wing** (meat). Grilled Haunch → **Grilled Spider Leg**. The Content table's keys were renamed in place (`ingredient.spider_leg`, `ingredient.venom_sac`, `enemy.bat`, `enemy.giant_spider`, `recipe.grilled_spider_leg`), so no rat names are left in assets or text. Harvests: the bat drops 1–2 Bat Wings; the spider drops 1–2 Spider Legs and, 60% of the time, a Fine Venom Sac. Icons from the Loot Icons sheet (spider leg and venom; the bat wing borrows the vampire's cape, see `docs/ASSET_MAP.md`).
- **Freshness** falls while parts are carried (the delve's rate, `FreshnessConfig`: 0.08 per minute; Chilled parts slower) and while they lie on the floor.
- **Satchel full.** Touching a part puts in as much as fits. If it doesn't fit, a hint shows ("Satchel full — press E to swap", with the gamepad button when using one); pressing Interact opens the **swap prompt**: gameplay pauses (through the MMFeedbacks time manager, `MenuPause`), gameplay input is off and the UI map takes over. It shows the part found and the six slots (icon, count, quality as one to four pips, freshness as a bar's length; the selected slot gets a marker tab), and a detail line for the selected slot. Choosing a slot drops its whole stack at your feet for the new part; "Leave it" or Cancel leaves the new part on the floor. Keyboard (arrows/WASD, Enter, Escape), controller and mouse (hover, click) all work. A dropped stack isn't picked straight back up: step off it first.
- **Drops pop out (playtest fix).** Bats and spiders seemed to drop nothing: they die right next to the player, their parts landed within reach and went straight into the satchel (with no satchel HUD until step 5, invisibly). A harvest now hops out of the body to land 0.7–1.1 tiles away in front of it (towards the camera, so a corpse can't hide it; against a wall it tries the sides, then behind), and can't be picked up until it lands (0.35 s). Landed parts bob gently, and their icons are unlit (like the enemy alert) so a dark part still reads on the dim floor. A diagnostic first confirmed all three enemies did drop their parts with real weapon kills.
- **UI art:** the UI Overhaul's Classic panel, bar and slots (one with a marker tab for the selected slot), on the 320×180 canvas.
- **Test floor** updated in place: the swap prompt and hint on its canvas (rebuilt on each update, so layout changes arrive), and floor freshness on its harvest system.
- **Tests:**
  - EditMode: the re-theme (renamed assets kept their GUIDs, new ids and categories, harvests and icons, every harvestable part known to saves, Grilled Spider Leg still uses the leg, no rat keys or names left).
  - PlayMode on the test floor: a killed bat drops Bat Wings with their icon that go into the satchel and lose freshness at the delve's rate; parts on the floor spoil too; with a full satchel the hint shows and Interact opens the prompt (paused, gameplay input off, first slot selected, its text showing the part found and the selected slot), the keyboard moves and chooses, the old stack drops at the player's feet and isn't re-collected until they step off; Cancel and "Leave it" change nothing; the mouse hovers and clicks a slot.

**Step 3 approved (2026-10-03).** Kept as decided: quality dots, freshness decay in the satchel and on the floor, whole-stack swaps, dropped stacks needing a step away. The Bat Wing icon stays a documented art gap; the mushroom recipes wait for 4f.

**Step 4 done (2026-10-03), awaiting review and playtest: low-Essence warning, death and the Lockbox, extraction, and the result screen.**

- **Low Essence.** Below the low threshold (25%) a heartbeat plays, sound and the Heartbeat.Warning haptic in one feedback, from every 1.1 s down to every 0.45 s as Essence nears zero (`HapticMath.HeartbeatInterval`; intervals in `EssenceConfig`). It stops above the threshold, at death and in god mode. The placeholder Essence bar pulses red while low (the real HUD is step 5).
- **Death.** At zero Essence the death animation plays (1.2 s, real time), then gameplay pauses (`MenuPause`), input goes to the UI, and the **death screen** opens: why the delve ended, the six satchel slots, and the Lockbox choice. Choosing a slot outlines it (a shape as well as a colour) and moves to "Return to the surface"; "Keep nothing" gives everything up. `DeathPenalty` applies: that whole stack goes home, the rest of the haul and any run currency is lost. With an empty satchel the screen just explains and returns.
- **Extraction.** A rope hangs from a hole in the ceiling in room C (near the spider). Standing at it shows "Press E to climb back to the tavern" (the gamepad button when using one). Climbing stops Essence drain and damage, the player rises up the rope and fades, and the whole satchel goes home.
- **Result screen.** How the delve ended ("Back from the Cellars" or "Dragged Back to the Surface"), the parts brought home shown as slots, and how many were lost. "Delve again" restarts the floor; with the day loop (4c) the button reads "Back to the tavern" and hands the report to `GameFlow.CompleteDelve`.
- `DelveRunController` owns the delve's end; `DelveExit` is the rope. The look-test overlay no longer restarts on death where the death screen exists (the 4a look room still restarts).
- **Test floor** updated in place: the delve controller, the rope (`R` on the map), and the exit hint, death screen and result screen on its canvas (all generated UI, rebuilt on each update).
- **Tests (PlayMode, test floor):** the heartbeat starts below the threshold, repeats with its haptic, speeds up as Essence falls and stops once restored, with the bar's low state following; death opens the Lockbox screen after the death animation (paused, UI input only, text showing the choice), keyboard choice keeps only that whole stack and the result says what was lost; "Keep nothing" loses the whole haul; an empty satchel still gets the screen; climbing the rope shows the hint, can't be drained or hurt, and brings the whole satchel home; each ends with "Delve again" giving a fresh floor with input and time restored.

**Step 4 playtest fixes (2026-10-03):**

- **Freeze at the rope.** Being pushed in and out of the rope's trigger (by the slime and spider nearby) hid and showed the exit hint inside physics callbacks; Super Text Mesh rebuilt its text there, which Unity forbids, and logged an error every physics step until the editor bogged down. Hints are now raised only from `Update` (the rope and the satchel-full hint), and hint views fade a `CanvasGroup` instead of switching the text off.
- **Slime and bat pushing the player.** They chased right into the player and shoved them; the bat was then always inside its swoop's minimum range and only attacked if the player moved. They now keep a stand-off from their data like the spider (`keepDistance`: slime holds at 1.2 tiles, bat hovers 1.4–2.6 and backs off if crowded), applied at runtime from the definition. Attacks now need only line of sight: the old body-sized check refused leaps near walls and props, which is why the slime only attacked "after a while".
- Regression tests: a still player is attacked by the slime and the bat without being shoved; being pushed in and out of the rope's trigger logs nothing and the hint still follows.

**Step 4 approved (2026-10-03).** Kept as they are: the heartbeat threshold and pacing, the death-screen dimming, the Lockbox flow, and the rise-and-fade climb (until clothed climb art exists).

**Step 5 approved (2026-10-03): the dungeon HUD.** Kept as built.

- **Essence**, top left: the UI Overhaul's Classic bar (a dark trough with a blue fill). When low it switches to the red fill and pulses (two cues, not just a colour); a hit flashes it. "Essence" sits under it. Replaces the look test's placeholder bar on the test floor (the 4a look room keeps its placeholder).
- **Satchel**, bottom left: the six slots, the same view as the swap prompt (icon, count, quality dots, freshness bar), live: parts appear as they go in, and freshness bars shorten as they spoil.
- **Harvest feed**, top right: what each kill produced, newest on top, with the part's icon: "Clean kill! Fine Bat Wing ×2", "Overkill! …", "Finisher! …", or "… was destroyed". Lines hold for 2.4 s and fade over 0.5 s (tunable on the HarvestFeed).
- **Prompts** (satchel full, climb out) moved up to sit above the satchel row. The test floor's debug controls line fades after 6 s so it doesn't sit over the satchel.
- **Tests (PlayMode, test floor):** the bar follows Essence, flashes on a hit, turns red and pulses when low; the satchel HUD binds to the delve's satchel, shows a part as it goes in with three dots for Fine, and its freshness bar shortens; a kill puts "Clean kill! … Bat Wing ×N" with its icon on the feed, which then fades; the feed picks the right words for clean kills, overkills and destroyed parts; the bar, its label, the satchel, the feed, the prompts and the debug label never overlap and stay on screen (checked at the test window's narrower-than-16:9 width).

**Step 6 approved (2026-10-03): the feedback and haptics pass.** Kept as built, including the cleaver's heavy at 14 / 22 / 34. Each moment has one combined `MMF_Player` (visuals, placeholder sound, named haptic pattern), all through the existing settings (screen shake scale, flash, hit-stop, vibration on/off, reduced vibration):

| Moment | Feedback |
|---|---|
| Light hit | hit-stop and shake sized from its AttackData, `PH_Hit`, `Tap.Firm` |
| Heavy hit | longer hit-stop, bigger shake, `PH_HitHeavy`, `Hit.Heavy` |
| Heavy charge reaching level 2 / 3 | `PH_ChargeTick`, `Cue.Threshold` (lighter at level 2, full at 3) |
| Clean kill | `PH_KillClean`, `Kill.Clean` |
| Overkill or a part destroyed | `PH_Thud`, `Bump.Soft` |
| Satchel full | `PH_SatchelFull`, `Buzz.Failure`, once when it first refuses a part |
| Enemy telegraph | flash and `PH_Telegraph` as before; `Cue.Threshold` at half strength only when the attack is aimed at the player and within 6 tiles |
| Dodge | `PH_Dodge` only, no haptic (frequent; it would numb the hits) |
| Taking damage, low Essence, pickup, climbing out | unchanged |

- **The hit's feedback moved to the weapon.** The enemy's hit feedback is now only its flash; the weapon carries hit-stop, shake, sound and haptic, so light and heavy hits feel different and nothing plays twice.
- **Fixed: the heavy spin hit the same enemy twice.** Its hitbox is open for 12 frames, but a struck enemy was only invincible for 0.1 s, so a tapped heavy did 28 instead of 14 (and a full charge 68 instead of 34). A swing now hits each target once (`FrameData.OneHitInvincibility`: past the end of its active window by a frame). Heavy damage now matches its data; the heavy will feel weaker than in your earlier playtests.
- **Fixed: a stagger switched an enemy's AI back on when it ended,** even if something else had switched it off first. It now only restores what it paused.
- **Tests:** PlayMode `FeedbackTests`: one `Tap.Firm` per light hit, one `Hit.Heavy` per heavy hit (and the spin's damage lands once), a harder shake for the heavy; a tick per charge level past the first; `Kill.Clean` on a clean kill, `Bump.Soft` on an overkill; one buzz when the satchel first refuses a part; the telegraph cue only when aimed at the player; no haptic on a dodge. EditMode: the one-hit invincibility rule. `HapticService.PatternPlayed` reports each named pattern played (tests and debug overlays).
- **Needs you:** controller rumble checks (strength and feel of each pattern) and the placeholder sounds' volumes.

**Step 7 approved (2026-10-03): web smoke test and docs.**

**Web smoke test (2026-10-03): passed after one fix.** A development web build (`Builds/Web`, 125 MB, 0 errors) of `Dungeon_TestFloor`, served locally and played in Chrome:

- The floor renders with its lighting; the camera follows the player; walls block movement; F2 changes the resolution.
- The HUD: Essence bar and label, its red low-Essence fill and hit flash; the six satchel slots; localized text loads from the build's tables.
- Combat: a three-hit light combo kills the slime; its parts pop out, the harvest feed names them with their icons ("Fine Slime Core ×1", "Standard Slime Gel ×2"), and walking over them fills a satchel slot with its count, quality dots and freshness bar. The bat wakes from its perch and swoops; the spider keeps its distance, telegraphs (red flash and "!") and bites.
- Death: at zero Essence the death screen appears; with an empty satchel it says so, and with a haul it offers the Lockbox. Choosing slot 1 marks it and reads "Lockbox: Standard Slime Gel ×2"; the result screen then shows the saved stack and "Brought home: 2 parts. Lost: 0." Delve again restarts the floor.
- Extraction: pressing E at the rope climbs out to "Back from the Cellars".
- The browser console shows no errors from the game (only the web template's missing `productVersion` warning).
- **Fixed:** the em dash in the Lockbox and satchel-full strings showed as a gap. The web build has only Unity's built-in font, with no system fallback for characters past Latin-1. Those strings (and three tavern strings) now use plain punctuation, and a test keeps every UI string within Latin-1 until a real font exists. Rebuilt and rechecked: the Lockbox line now reads correctly in the browser.
- **Not exercised in the browser:** the satchel-full swap prompt (covered by PlayMode tests) and rumble (none on the web, by design).
- **Damage pace:** fighting all three enemies at once without dodging drains a full Essence bar in a few seconds (about 5 in the playthrough; two enemies took about 10; slime 10, bat 8, spider 14 per hit, 0.6 s of invulnerability after each). **Decided:** enemy damage stays as it is; judge the pace again in 4d with real room encounters.

**Docs:** `PORT_MANIFEST.md` is current for 4b (the rows 4b replaced now name their replacements; the dungeon debug panel is recorded as not rebuilt). Known issues are updated below.

**4b complete (approved 2026-10-03).** Decisions at sign-off:

1. Enemy damage values are unchanged; damage pace is judged again in 4d, once real room encounters exist.
2. The prototype's F1 debug panel is **not** 4c scope. It stays recorded as a developer-tooling gap; individual debug controls are rebuilt only when they're genuinely useful during 4c/4d.
3. The Latin-1 limit on UI text is temporary. When the real game font is chosen, replace it with a proper glyph-coverage check against that font.

**Next: 4c,** the tavern and UI migration.

### 4c plan (approved 2026-10-03)

Goal: the complete top-down day loop restored through `GameFlow` (Main Menu → Morning → Delve → Evening service → Results → Night → Sleep → Morning, with saves), working in the web build. Out of scope: room graphs and run rewards (4d), combat depth and the boss (4e), furniture placement, the Butcher Block, new recipes and Pip's and Gundra's characters (4f), story, quests and dialogue (4g).

Order of work, each step reviewed and playtested before the next:

1. The `Tavern` scene and layout, Minifantasy art import, the walkable grid, the player's movement and interactions.
2. Customers (paths, seats, queue, patience) and Pip as TDE characters.
3. Stations and their panels (Grill, Tap, Stew Pot with Chop), the pass, and 2D serving.
4. Prep, the Tavern HUD and Results: one complete evening.
5. Boot, Main Menu, Morning and Night, the delve integration, saves: the full day loop.
6. Feedback and haptics pass.
7. Web smoke test of the full loop; PROGRESS, ASSET_MAP and PORT_MANIFEST.

Decisions (2026-10-03):

1. **Camera:** fixed on the one-screen Stage 1 room. Follow comes back when later tavern stages are larger than one screen.
2. **Carrying a plate:** the dish icon above the player's head, with the spill meter. No waiting for a clothed carrying animation.
3. **Customers:** layered A Myriad of NPCs characters (body, outfit and hair). Drawn by a tavern NPC layered presentation component that reuses the existing facing and frame-selection logic and keeps every layer in step; the combat `CharacterSpriteAnimator` is not complicated for it. Presentation only. A customer's appearance is fixed for as long as that customer exists (deterministic, never re-rolled as their state changes).
4. **Debug controls:** F4 fill the storeroom, F5 end service now, F6 spawn a customer, F8 skip to the next phase, F9 +100 gold. The old F1 panel stays deferred.
5. **Delve scene:** `Dungeon_TestFloor` in the day loop until 4d.
6. **Main menu:** New Game and Continue; settings are 4h.

### 4c status

**Step 1 done (2026-10-03), awaiting review and playtest: the tavern room, art, walkable grid and interactions.**

- **`Tavern` scene** (*Hearthdelve → Generate → 4c Tavern*; *Update Tavern* applies later builder changes in place). The Stage 1 inn, 28×17 tiles: the Tavern Indoor premade room stretched cell by cell, with the front door in the middle of the south wall.
  - Back wall: the L-shaped bar with its taps and stools (the **Tap**), bottle shelves, a sign, a wall lamp, a low shelf of glasses.
  - Kitchen corner: the stone oven and range (the **Grill**), the cauldron over a floor fire (the **Stew Pot**), and the **pass** (a long table, usable from either side).
  - Dining: four round tables, each with a chair on either side facing it (8 seats, marked for customers in step 2). Barrels and a stool for decor.
  - Lighting: warm ambient light, with local light from the wall lamp, the kitchen fire, the stew fire and over the bar.
  - Every solid piece blocks movement with a footprint that ends at the bottom of its art (its sort point).
- **Camera:** fixed, centred on the room (decision 1). Centring keeps the whole room on screen from 16:9 down to 5:4; an off-centre view lost a tile at 16:10.
- **Walkable grid:** baked from the colliders at scene load. It can now be invalidated (`NavGrid.Invalidate`, or publishing `NavigationLayoutChanged`) and rebuilt (`Rebuild`); its `Version` goes up with each bake and path followers re-plan when it changes. Placement itself is 4f.
- **Interactions:** the tavern player picks the nearest usable station in reach (a pure rule with EditMode tests; the current target holds until another is clearly nearer, so the highlight doesn't flicker). The target gets gold corner brackets and a bobbing marker, and the hint reads "E | Space: Grill" (the tavern's Interact keys). Interact publishes `TavernInteracted`; what each station does comes in step 3. Nothing is targeted while the Tavern map is off (panels and menus).
- **Art imported and recorded in `ASSET_MAP.md`:** the stretched room's cells; the Tavern Indoor prop half (tables, chairs in four facings, stools, benches, long tables, shelves, taps, bottles and glasses); the Crafting And Professions II kitchen, idle and working; the Dwarven Kingdom floor and wall fires; the UI Overhaul selectors.
- **Tests:**
  - EditMode: the selection rule (nearest in reach, unavailable skipped, stickiness, dropping the target).
  - PlayMode (`TavernSceneTests`): the stations, seats and grid; lit sprites; the camera holding still and fitting the room; walking up into the bar, kitchen, cauldron, pass, tables and chairs stops the player in front and sorts them in front; every station is reachable from the door and the spawn; the grid rebuilds when a table moves; the highlight, hint and Interact; nothing targeted while the Tavern map is off.
- `LookTest_Tavern` is untouched. The shared tavern player prefab gains the interactor, which does nothing in the look scene (it has no stations).

**Step 1 playtest fixes (2026-10-03):**

- **The Grill and Stew Pot could be approached from behind,** where nothing reached them. Walled off in step 1; *superseded in the step 2 playtest*, which made both usable from behind instead (below).
- **The oven left of the range didn't highlight.** It's part of the same kitchen art; now the whole kitchen is the Grill, framed as one, usable from in front of either part. (The GDD's separate Oven station isn't in 4c.)
- **Highlights lost their top and right edges:** a 9-sliced SpriteRenderer dropped the frame's top row and right column of pixels. The highlight is now four separate corner sprites cut from the same selector.
- Applied to the existing scene by *Update Tavern* (no rebuild). New test: nothing is walkable behind the kitchen or cauldron, walking up to the oven targets the Grill, and all four highlight corners draw.

**Step 1 approved (2026-10-03).** Layout and camera kept as they are.

**Step 2 done (2026-10-03), awaiting review and playtest: customers and Pip.**

- **Customers** are TDE characters that walk the walkable grid through the same thin pathfinding AI action as the enemies. The ported, tested `ServiceSession` and `CustomerLogic` decide everything; the new `CustomerAgent` only moves the body and shows its state.
  - They come in at the door. With a seat free they walk to the spot below it, step onto the chair and sit facing their table; with none free they queue inside the door, along the front wall, and move up as seats free.
  - **Patience:** a small bar over the head while queueing or waiting for food, green to red, shortening pixel by pixel. A "…" bubble while they read the menu; their dish's icon while they wait (once dishes have icons, step 3).
  - **Walkouts:** when patience runs out they get up and leave by the door with an angry face (the ledger counts it).
  - Customers and Pip are on their own physics layer (`Npcs`): they collide with walls and furniture but not with the player or each other, so they never shove anyone.
- **Layered looks** (decision 3): A Myriad of NPCs layers (body, trousers, top, beard, hair or hat) drawn by a new presentation-only `LayeredSpriteAnimator`, which picks one facing and frame for all layers with the same rules as the combat animator. Curated for readability: few layers, tops only in colours that contrast with the floor. Each customer's look comes from the evening's seed and their id, so it's fixed for the whole visit.
- **Seats:** 6 open before upgrades; the fourth table is put away until the seat upgrade, and the walkable grid is rebuilt through the step 1 invalidation event.
- **Pip:** a TDE character (the premade Butcher as a stand-in) who walks to the post of their job: Serving, beside the pass, by default. Working the job comes with step 3.
- **The evening (until the prep screen in step 4):** played on its own, the scene opens straight into a debug evening (a debug-filled storeroom, the first dishes it can make). Customers order, wait and, with no serving yet, walk out.
- **Debug keys** (editor and development builds): **F5** ends the service now (everyone goes home), **F6** lets a customer in.
- **Tests:**
  - EditMode: the same seed always gives the same look; looks vary and every option gets used; empty lists and beard chance.
  - PlayMode (`TavernCustomerTests`): a customer walks from the door to a seat without ever clipping furniture, sits on it facing the table and stays put; with every seat taken they queue at the right spots, show patience, then take freed seats; patience running out makes them walk out upset and leave by the door; they walk through the player without moving them; the layered look stays in step (same animation and frame on every layer) and never changes; Pip reaches the serving post; the fourth table is put away and its floor becomes walkable; ending service sends everyone home.
- Applied to the existing scene by *Update Tavern* (no rebuild).

**Step 2 playtest fixes (2026-10-03):**

- **Pip "vibrated" at the pass.** Two causes. TDE eases characters to a stop, so Pip drifted past the post, turned back and overshot again each frame, flipping his facing. And the walk action and the staff agent were both steering. Now NPCs start and stop at once (no easing), and on arrival the agent switches the AI off and stands still, walking again only once the goal is clearly away (a little hysteresis). Queueing customers get the same, so they stand still in line instead of walking on the spot.
- **Walking behind the Grill and Stew Pot,** for immersion: this reverses the step 1 change that walled them off. There's a tile of floor behind the range again (reached round its east end), and the cauldron moved half a tile forward so a full tile is clear behind it. Stations can now be used from more than one spot: both highlight and work from the front or from behind, where the player is drawn behind them, like a cook at the stove. Staff still work from the front.
- Tests: Pip stands still, idle and without turning at the post; queueing customers stand still; the Grill and Stew Pot each have a walkable, reachable spot behind them that targets and highlights them.

**Next: step 3,** stations and serving. After your review and playtest of step 2.

Adjustments: the tavern's walkable grid can be explicitly invalidated and rebuilt when the furniture layout changes (in 4c it only builds at scene load; placement itself is 4f), so 4f doesn't have to replace an immutable-layout assumption. `LookTest_Tavern` stays untouched as the 4a baseline.

### Open design questions (4a)

1. **Protagonist body:** the Human Townsfolk is a stand-in. See `docs/ASSET_MAP.md`.

### Decided at the end of 4a (2026-10-02)

1. **Resolution locked:** 320×180 at 8 PPU (CLAUDE.md, GDD §8.1).
2. **URP 2D lit sprites are the visual baseline.** The look test uses URP's lit sprite material everywhere, with simple representative lighting: in the dungeon, a cool, dim ambient light and two warm torch lights; in the tavern, a warm ambient light and a warm glow over the bar. The existing scenes and prefabs were updated in place (*Hearthdelve → Generate → Update Look Test Lighting*). The values are in `LookTestBuilder` and on the scene lights, so they can be tweaked in the editor. This is not a lighting system yet.
3. **SpriteSet animation** instead of Mecanim, presentation only (CLAUDE.md, GDD §10.1).
4. **Frame pacing:** TDE's `GameManager` defaults to a 300 fps target, which makes a web build run its main loop on a timer instead of `requestAnimationFrame`. The look-test scenes use -1 (the platform default) instead.

### Known issues

- **Character size:** at 320×180 a character is about 4% of screen height. This is the main thing to judge in the look test.
- **Placeholder UI in the look scenes:** the 4a look scenes keep their plain Essence bar (the test floor has the real HUD). All text uses Unity's built-in font through Super Text Mesh (no pixel font exists).
- **Look-scene death:** at zero Essence the 4a look rooms still just restart after 2.5 s; the death screen, Lockbox and result screen are on the test floor.
- **Enemy hits don't push the player back** (enemy knockback on the player was left out of 4b's simple stagger; the player's own knockback would need care not to fight input).
- **Shroom Cap and Spore Sac drop from nothing** now (they were the Cellar Shroom's, which became the spider). They wait for the Mushroom People; until then Shroom Skewer, Cellar Kebab, Cellar Stew and Offal Pottage can't be cooked from a delve. The recipe rework is 4f.
- **Enemies only hurt with telegraphed attacks**, so standing in a slime is harmless. A deliberate fairness choice (GDD §4.1); say if you want light contact damage back.
- **Hit-stop needs an `MMTimeManager`:** the test floor and newly generated scenes have one; the 4a look scenes (kept as baselines) don't, so hits there don't freeze.
- **Pathfinding ignores other enemies:** enemies path around walls and props only, and can bunch up on the way to the player.
- **Vendor prefabs with missing references** after the demo trim are listed in `docs/THIRD_PARTY.md`; we don't use them.
- **Data:** the enemies are the slime, bat and spider (plus the look room's training dummy). The Bat Wing icon is a placeholder (a documented art gap in `ASSET_MAP.md`).
- **No dungeon debug panel (developer-tooling gap):** the prototype's F1 panel (god mode, refill or drain Essence, shake and hit-stop toggles, restart) wasn't rebuilt. Not planned as a whole; individual controls come back when they're genuinely useful. The look-test overlay's F2–F4 keys still work.
- **Customers walk through each other:** they don't collide with each other or the player (on purpose: no shoving), and their paths ignore other customers, so two can overlap briefly in an aisle.
- **No sitting pose:** seated customers use their idle pose on the chair.
- **UI strings stay within Latin-1** (no em dashes, curly quotes or ellipsis characters) until a real font replaces Unity's built-in one; the web build can't draw anything else. Temporary: once the game font is chosen, the test becomes a glyph-coverage check against that font.
- **All sound is placeholder** (`PH_…`, generated). Rumble on real controllers is checked by you; web builds have no rumble.

### Regenerating and verifying (current project)

- **Menu:** *Hearthdelve → Generate → 4a Look Test (All)*. It configures the project, imports the Minifantasy sheets, and builds the data assets, prefabs and the two scenes. *Hearthdelve → Generate → 4b Test Floor* does the same for the 4b test floor (and leaves the look scenes alone).
  - Scenes are created when missing and **never overwritten without asking** (a dialog in the editor; `-rebuildScene` in batch mode).
  - Data assets (configs, haptic patterns) are only created, so your tuning is kept. Animation sets, tiles and prefabs are rebuilt every run.
- **Command line** (close the editor first):

```
"C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -executeMethod Hearthdelve.Editor.LookTestBuilder.RunBatch -logFile BatchLogs/generate.log
"C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults BatchLogs/editmode.xml -logFile BatchLogs/editmode.log
"C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults BatchLogs/playmode.xml -logFile BatchLogs/playmode.log
"C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe" -batchmode -projectPath . -executeMethod Hearthdelve.Editor.BuildTools.CaptureLookTestBatch -logFile BatchLogs/capture.log
"C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe" -batchmode -projectPath . -executeMethod Hearthdelve.Editor.BuildTools.BuildWebBatch -logFile BatchLogs/webbuild.log
```

The capture command renders each look-test scene at 320×180 into `BatchLogs/looktest_*.png`. The web build needs the Web build module.

---

# Phase 1–3 history (side-scroller prototype, tag `v0-sidescroller-prototype`)

## Status

- **Phase 1 (Combat Prototype):** done and playtested by you in the editor. Feel tuning is ongoing.
- **Phase 2 (Tavern Prototype):** implemented and playtested once (2026-09-29). Feedback applied:
  - **Round 1:** hand delivery with a button, spare plates, final sold-out, and closing early when everything sells out.
  - **Round 2:** a 2.5-minute service, Pip on Serving by default, and a Stew Pot station with a chopping minigame (beyond the GDD's Phase 2 scope, approved).
  - Chopping and the stew pot were playtested by you and work.
- **Phase 3 (Loop Prototype):** implemented and passing automated verification. **Not yet playtested.** Balance numbers are first-pass.

Tests: **237 project EditMode tests + 21 PlayMode tests, all passing, 0 compiler warnings.** The batch run reports one more EditMode test because the Addressables package adds a stub test.

### Phase 3 criteria

| Criterion | Where | Verified by |
|---|---|---|
| Day cycle Morning → Delve → Evening → Night → next Morning, with a day counter | `DayCycle`, `DayRules`, `GameState` (Shared/Game); day shown on both HUDs and the Morning/Night screens | EditMode cycle tests; PlayMode full day |
| Main menu with New Game and Continue (one slot) | `MainMenu` scene, `MainMenuScreen`, `GameFlow.NewGame/Continue` | PlayMode full day (reload through Continue) |
| Transitions go through persistent Boot services; Dungeon and Tavern never reference each other | `Boot` scene with `GameFlow` (Shared), content scenes loaded additively | Assembly references unchanged; PlayMode full day |
| Exit at the end of the dungeon moves the whole satchel into the storeroom | `DelveExit`, `DelveRunController.Extract`, `DelveReport.Extraction` | EditMode extraction test; PlayMode full day (walk to exit, press E) |
| Death or Essence depletion: only the Lockbox stack goes home, and the day continues to Evening | `DelveRunController.FinishDelve`, `DelveReport.Death` | EditMode death tests; PlayMode death test |
| Dungeon freshness loss (tunable), overnight storeroom loss (tunable), Chilled parts keep better | `FreshnessSettings` (`Data/Config/FreshnessConfig`), `Satchel.Decay`, `Storeroom.Decay`, `DayRules.Sleep` | EditMode decay tests; PlayMode checks both |
| F4 debug fill available but off by default | `GameDatabase.allowDebugFill`, `TavernDirector.CanDebugFill` | PlayMode full day |
| Service gold persists and is spent at Night | `DayRules.CompleteService`, `TavernNightScreen` | EditMode service test; PlayMode full day |
| Three upgrades as `TavernUpgradeDefinition` assets with tunable costs/effects: satchel slot, max Essence, seat | `Data/Upgrades/*`, `Upgrades`, `DelveLoadout`, `TavernLayout.SetActiveSeats` | EditMode upgrade tests; PlayMode (bought slot gives 7 satchel slots next day) |
| A pre-delve breakfast from the storeroom, cooked with the existing minigames, buffs the next delve | Recipe `mealBuff`, `TavernMorningScreen`, `TavernPlayer.CookBreakfast`, `MealBuff`, `PlayerVitals` | EditMode buff tests; PlayMode Grill breakfast → Essence modifier in the dungeon |
| Versioned JSON save of all persistent state, autosave at Night, older version loads | `SaveSystem`, `SaveData` (v2), `SaveDataV1` migration, `SaveStore` | EditMode round-trip, v1 migration, unknown-content, bad-file tests; PlayMode reload |
| Debug end-of-day summary; debug keys to skip phases and add gold | `DaySummary`, Night screen panel (F10), `GameFlowDebugOverlay` (F8/F9) | EditMode summary checks |

### Phase 2 criteria

| Criterion | Where | Verified by |
|---|---|---|
| Debug action fills the storeroom with Phase 1 ingredients at mixed quality and freshness | `DebugStockFiller`, prep screen button, F4 | EditMode fill test; PlayMode prep test |
| Pick a menu of up to 3 dishes from 5 Biome 1 recipes before service (now 7, with two stews) | `TavernDirector.ToggleMenu`, `TavernPrepScreen`, `Data/Recipes` | EditMode menu-size test; PlayMode cap test |
| Grill, Tap and Serving work through `IMinigame` | `GrillMinigame`, `TapMinigame`, `ServingMinigame` (Core `IMinigame`) | 17 EditMode minigame tests, including "default tuning takes 5–10 s" |
| Villager, adventurer and dwarf enter, sit, order, wait (patience), eat, pay and leave, with flavor preferences affecting satisfaction | `CustomerLogic`, `Preferences`, `CustomerProfile` (`Data/Customers`), `CustomerAgent` | Customer lifecycle and preference tests; PlayMode service run |
| Dish score = recipe value × ingredient quality × freshness × minigame score, driving payment, tips and renown | `DishScoring`, `ServiceEconomy` (Shared), `ServiceSession.Settle` | 12 EditMode economy tests; full-loop ledger test |
| Tunable service length (default 2.5 min, shortened from 6 after the first playtest) and a results screen (dishes served, gold, tips, renown, walkouts) | `ServiceConfig`, `ServiceSession` clock, `TavernResultsScreen` | Clock and last-orders tests; PlayMode results test |
| One staff helper auto-resolves a station through `IMinigame` at reduced quality | `StaffDefinition` (Pip), `StaffCook`, `StaffAgent`, per-minigame auto-players | Auto-player tests (below expert, above zero); staff-cook test; PlayMode staff-only service |
| Tuning in ScriptableObjects, text localized, core logic tested | `Data/Tavern/*`, `Data/Config/EconomyConfig`, `TavernLocKeys` | Batch EditMode + PlayMode runs; file-name guard test |
| **Added after playtest:** Stew Pot station with a chopping minigame. Chop accuracy sets helpings (3–5); the pot simmers on its own; stew orders are ladled onto the pass automatically. Pip can run the pot. | `ChopMinigame` + `ChopAutoPlayer`, `StewPot`, `ServiceSession` (stew section), `StaffPotCook`, `StewPotView`, `Data/Tavern/StewConfig` | 19 EditMode chop/pot tests (including "a two-ingredient batch takes 5–10 s"); PlayMode mouse-chop and Pip-on-pot tests |
| **Also asked for:** sold-out dishes marked on the HUD; new customers order something else or leave with a smaller penalty than a walkout | `ServiceSession` (per-order ingredient reservation, sold-out tracking), `TavernHud` | 6 EditMode sold-out/cancellation tests; PlayMode HUD test |

### Phase 1 criteria (unchanged)

| Criterion | Where |
|---|---|
| Movement (coyote time, jump buffer, dodge i-frames, wall slide/jump, drop-through) | `PlatformerMotor`, `KinematicMover2D`, `PlayerController` |
| Butcher's Cleaver 3-hit combo with hit-stop and screen shake | `ComboLogic`, `WeaponDefinition`, `HitStopDriver`, `ScreenShaker` |
| Three enemies with telegraphed attacks | `RatBehaviour`, `SlimeBehaviour`, `ShroomBehaviour` |
| Harvest drops (clean kill / overkill / element → quality) | `HarvestRules`, `HarvestSystem`, `IngredientPickup` |
| Essence meter and forced exit at zero | `EssenceMeter`, `PlayerVitals`, `DelveRunController` |
| Death screen, keeping one slot (the whole stack) | `DeathScreen`, `DeathPenalty` (the kept stack now goes to the storeroom through `GameFlow`) |

## How a day works

1. **Start:** open `Assets/_Project/Scenes/Boot.unity` (or `MainMenu`, which opens Boot) and press Play.
   - **New Game** starts Day 1 with an empty storeroom and no gold.
   - **Continue** loads the one save slot and resumes where it was saved (Night, or Morning after you slept).
2. **Morning (tavern):**
   - The storeroom and today's delve bonuses are shown.
   - **Breakfast (optional, one dish):** pick a Grill or Tap dish the storeroom can make, then play its minigame. It uses one serving's ingredients.
     - Grill dishes add max Essence; Tap drinks slow Essence drain.
     - The dish's quality scales the buff. Stews aren't offered.
     - Esc puts the ingredients back.
   - **Descend into the dungeon.**
3. **Delve (dungeon):**
   - Satchel slots and max Essence include your upgrades and breakfast.
   - Carried parts lose freshness over time; the bar under each satchel slot shows it.
   - **The exit** is the door on the raised block at the far right end. Stand at it and press **E** to go home with the whole satchel.
   - If Essence runs out, pick one Lockbox slot on the death screen. Only that stack goes home, and the day still continues to Evening.
4. **Evening (tavern):**
   - The usual prep screen and service, cooking from the storeroom.
   - If nothing can be cooked (or you'd rather not open), **Close for the night** goes straight to Night.
   - On the results screen, **Close up for the night** banks the takings (payments + tips).
5. **Night (tavern):**
   - Gold and renown, and the three **upgrades**, each with a few levels at rising cost.
   - The **debug day summary** (F10): parts brought back and lost, dishes sold, gold and tips earned, and the cheapest next upgrade.
   - The game **autosaves** when Night starts and after each purchase.
   - **Sleep:** storeroom stock loses a little freshness overnight, then it's the next Morning (and another autosave).

When `TavernGreybox` or `CombatGreybox` is played on its own (no Boot), it behaves as before: a single evening with the debug fill, or a dungeon run that restarts.

## How a service works

1. **Prep screen:**
   - Fill the storeroom with the debug button or F4.
   - Pick up to 3 dishes. Each shows its station, gold value and how many servings your stock can make. For a stew it shows how many pots, and the helpings range per pot.
   - Pip starts on **Serving**. You can move them to the Grill, Tap or Stew Pot, or take them off duty.
   - **Open the doors.**
2. **Customers arrive.**
   - They sit if a seat is free (6 seats); otherwise they queue by the door.
   - After reading the menu they order a dish that's still available, choosing by their tastes.
   - Placing an order **reserves** the ingredients, so it can't later become impossible to make. Stews are the exception: ingredients are taken per batch when the pot goes on.
   - The order appears on the rail on the right.
3. **Cooking:** walk to the Grill or Tap and press **E / A** to cook the next order for that station. The Grill and Tap panels show the minigame.
   - **Stew Pot** (right-hand wall):
     - While the pot is empty and a stew on the menu can be made, press **E** to put a batch on. It takes one full set of the recipe's ingredients and makes the stew with the most orders waiting.
     - **Chop** each ingredient. Move the knife with the mouse (or the stick / A–D) and click (or Space / A) on each guide line. Each cut counts for the nearest uncut line and scores by how close it lands.
     - Each ingredient has a time limit (the bar under the board). Esc steps away and returns the ingredients.
     - Chop accuracy sets the helpings: 3 (sloppy) to 5 (clean). The quality of a stew comes from its ingredients, freshness and serving.
     - The pot then **simmers on its own** for 25 s, with a bar above it. Pips above the pot show the helpings: dim while simmering, bright when ready.
     - Stew orders show "Waiting on the pot" on the rail. Each is **ladled onto the pass automatically** when a helping is ready, for you or Pip to carry. The pot empties when the last helping goes.
     - A stew stays on the menu while the helpings in the pot, plus 3 per batch the storeroom can still make, cover the orders waiting.
4. **Serving:** at the pass, press **E** to pick up the next dish. Carrying it *is* the Serving minigame: customers walking across the floor bump you and spill the plate, and a full spill meter drops it.
   - Press **E** next to a seated customer who ordered that dish to serve it. It doesn't have to be the customer it was cooked for. The customer who would receive the plate gets a gold ring on the floor, like a station in reach.
   - If you give A's plate to B, B's order passes to A.
   - Next to someone who ordered something else, the hint shows what they ordered.
   - Press **E** at the pass to put the plate back.
   - The score compares your time with the straight-line trip from the pass, so wandering costs quality.
5. **Spare plates:** if a customer leaves after their dish started cooking, the dish isn't wasted. It becomes a *spare* (italic on the order rail, "Spare" once it's on the pass).
   - You can serve it to anyone waiting for that dish. Their own order is then cancelled and its ingredients go back to the storeroom.
   - A new customer who orders that dish gets the spare automatically, without using more stock.
   - Pip, when serving, only carries plates someone is waiting for. If that customer leaves or you serve them first, Pip takes the plate back to the pass.
6. **Eating and paying:** customers eat, pay the dish value, tip if they're happy, and change your renown.
   - **Walkout:** patience runs out. −3 renown, and uncooked reserved stock goes back to the storeroom.
   - **Sold out:** once the stock can't make a dish, it's marked "Sold out" and **stays off the menu for the rest of the night**, even if a walkout returns ingredients (they stay in the storeroom).
   - A customer who finds nothing left to order leaves with −1 renown.
7. **Closing:**
   - The service ends after 2.5 minutes (no new arrivals in the last 20 s).
   - **Everything sold out:** the door closes to new customers. Orders already placed can still be cooked and served. Service ends early once nobody is waiting for food and every diner has paid, and the results screen says you closed early.
   - Diners still eating at closing time pay for their meal.
   - Played on its own, "Prepare another evening" restarts the scene with an empty storeroom. In the day loop the button is "Close up for the night".
   - **Open the doors** needs at least one menu dish the storeroom can make.
   - **Seats:** 6 to start. The room has 8 stools, and the extra ones appear as seat upgrades are bought.

## Tavern controls

| | Keyboard/Mouse | Gamepad |
|---|---|---|
| Walk | A/D or arrows | Left stick / D-pad |
| Use station / pick up dish / serve / put plate back | E or Space | A / Cross |
| Grill: flip | Space or Left Mouse | A / Cross |
| Tap: pour (hold) / tilt glass | Space or Left Mouse / W–S | A / Cross / Left stick |
| Chop: move knife / cut | Mouse, or A–D / arrows / Left Mouse / Space | Left stick / A / Cross |
| Step away from a station | Esc | B / Circle |

**Tavern debug keys:**

| Key | Action |
|---|---|
| F1 | Show/hide the overlay |
| F4 | Fill the storeroom (in the day loop, only if `GameDatabase → allowDebugFill` is on) |
| F5 | End service now |
| F6 | Spawn a customer |
| F7 | Restart (standalone only) |

**Day-loop debug keys** (Boot scene, development builds):

| Key | Action |
|---|---|
| F8 | Skip to the next phase. The current phase finishes properly: a delve extracts with the satchel, a service ends and shows results, results go to Night, Night sleeps. |
| F9 | +100 gold |
| F10 | Show/hide the day summary at Night |

## Dungeon controls

| | Keyboard/Mouse | Gamepad |
|---|---|---|
| Move | A/D or arrows | Left stick / D-pad |
| Jump | Space | A / Cross |
| Attack (combo) | J or Left Mouse | X / Square |
| Dodge roll | Left Shift or L | B / Circle |
| Drop through platform | S + Space | Down + A |
| Swap when satchel full / leave at the exit | E | D-pad Up |

**Dungeon debug keys:**

| Key | Action |
|---|---|
| F1 | Show/hide the overlay |
| 1 / 2 / 3 / 4 | Weapon element: none / fire / ice / poison |
| F2 | Toggle screen shake |
| F3 | Toggle hit-stop |
| F4 | Refill Essence |
| F5 | Set Essence to 3 |
| F6 | God mode |
| F7 | Restart |

## Regenerating and verifying

- **Menus:** *Hearthdelve → Generate → Phase 3 Loop (All)*. This runs Phases 1 and 2 as well, and builds Boot and MainMenu. *Phase 1 (All)* and *Phase 2 Tavern (All)* still work on their own.
  - They create missing scenes but **never overwrite an existing scene without asking** (CLAUDE.md).
  - Data assets are only ever created, so your tuning is kept. Prefabs are rebuilt every run.
- **Command line** (close the editor first):

```
"C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -executeMethod Hearthdelve.Editor.Phase3Generator.RunBatch -logFile BatchLogs/generate3.log
"C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults BatchLogs/editmode.xml -logFile BatchLogs/editmode.log
"C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform PlayMode -testResults BatchLogs/playmode.xml -logFile BatchLogs/playmode.log
```

## Needs you in the editor

1. **Play the loop:** open `Assets/_Project/Scenes/Boot.unity` with a 16:9 Game view (1920×1080 works well) and press Play. Use the Night summary (F10) to judge whether a day funds about one upgrade.
   - Saves go to `Application.persistentDataPath` (`%USERPROFILE%\AppData\LocalLow\sagaphy\Hearthdelve\save_slot_1.json`). Delete that file for a clean start.
   - To test service alone, `TavernGreybox` still plays on its own.
2. **Tuning.** Changes made in Play mode are kept after you exit.

   | Asset | What's in it | When changes apply |
   |---|---|---|
   | `Data/Tavern/ServiceConfig` | Length, arrival gaps, walk speed | Next evening |
   | `Data/Tavern/GrillConfig`, `TapConfig`, `ServingConfig` | Timing windows, staff error ranges; each notes the 5–10 s target | Next minigame |
   | `Data/Tavern/StewConfig` | Chop: lines per ingredient, accuracy window, time limit, knife speed, board position on screen. Pot: simmer time, helpings range | Next batch |
   | `Data/Config/EconomyConfig` | Quality/freshness multipliers, tips, renown, flavor weights | Next dish |
   | `Data/Customers/*` | Patience, tastes, generosity | Next customer |
   | `Data/Staff/Staff_Pip` | Skill, quality cap | Next evening |
   | `Data/Recipes/*` | Ingredients, values, breakfast buff (kind + amount) | Next evening / next breakfast |
   | `Data/Upgrades/*` | Levels: cost and amount per level | Next purchase (effects apply next delve / evening) |
   | `Data/Config/FreshnessConfig` | Dungeon loss per minute, overnight loss, Chilled multiplier | Next delve / next night |
   | `Data/GameDatabase` | New-game gold, `allowDebugFill`, the ingredient and upgrade lists saves look up | Next new game / immediately |
   | `Data/Tavern/TavernContent` | `baseSeats` (before upgrades) | Next evening |

3. **Nothing needs manual wiring.**

## Known issues / limitations

**Day loop (Phase 3):**
- **Not playtested yet.**
  - **Upgrade costs:** 100/180/280 satchel, 80/150/240 Essence, 120/220 seats.
  - **Breakfast buffs:** +15–25 max Essence (Grill), 20–30% slower drain (Tap).
  - **Freshness:** 0.08 lost per dungeon minute and per night.
  - All of these are estimates; the Night summary is there to check them.
- **The same dungeon every day.** It's the one greybox level with fixed enemies, so the haul is similar each day. Procedural rooms are out of scope.
- **No spoilage.** Parts at 0 freshness are still usable, just worth less.
- **No staff wages or upkeep yet** (GDD §7.3).
- **Quitting mid-day** loses progress back to the last save (Night or Morning), by design (GDD §10.6).
- **Breakfast in `TavernGreybox` played on its own** isn't available; Morning only exists in the day loop.

**Tavern (Phase 2):**
- **Chopping and the stew pot:** playtested and working; simmer time, helpings and stew prices are still first-pass numbers.
- **Pip on the Stew Pot** keeps a batch going whenever the pot is empty, which can use up ingredients the Grill dishes need.
- **Stew orders aren't reserved.** A Grill dish can use up ingredients a waiting stew order was counting on. That order then waits until it walks out.
- **Leftover helpings** in the pot at closing are discarded.
- **One playtest so far.** Patience, arrival rate, prices and minigame windows are still first-pass numbers.
- **Delivery reach** reuses `ServiceConfig → player → interactRange` (0.9 tiles). The nearest matching customer in reach is served and gets the gold floor ring (a placeholder until real art brings a sprite outline).
- **Spare plates left at closing** are simply discarded.
- **One staff helper.** The PlayMode test adds a second, test-only cook to run an end-to-end service with no player input.
- **Flat floor.** Customers walk along the floor only and walk through each other; the only collision that matters is plate bumps.
- **Minigame hints:** the Tap prompt names the Aim binding generically (e.g. "W/S"). Proper button icons will come with real UI art.

**Dungeon (Phase 1):**
- Air attacks stop horizontal drift.
- Enemies have no contact damage.
- The Shroom aims where you stood when its telegraph began.
- **Harvest feed text** is built by joining two localized strings; some languages may need one formatted string later.
- **Deferred:** ledge grab, double jump, harvest finisher, secondary weapon/skills, damage numbers, audio. (One extraction point now exists, at the end of the level.)

**General:**
- **UI scale:** the UI uses a 1280×720 reference resolution so the default font stays readable. Pixel-art UI at 640×360 will come with a proper pixel font.
- **Localization lookups** are synchronous (`WaitForCompletion`). That's fine on PC and consoles but wouldn't work on WebGL.
- **Standalone builds:** build the Localization Addressables content first (*Window → Asset Management → Addressables → Groups → Build → New Build → Default Build Script*). The editor doesn't need this.

## Decided

**Phase 3 (2026-09-29):**
1. **Where the loop lives:** `GameFlow` and the day logic are in `Hearthdelve.Shared`. The Boot scene stays loaded, and content scenes load additively. Played on their own, the Tavern and Combat scenes work as before.
2. **Breakfast:**
   - Each recipe has its own buff: Grill → max Essence, Tap → slower drain.
   - Dish quality scales it, up to ×1.25.
   - Grill and Tap dishes only; no stews.
3. **Upgrades:** levels with rising cost, defined in the upgrade assets.
4. **Saves:** v2 JSON with content saved by id; v1 is migrated. Autosave at Night (and after purchases, and after sleeping). Continue resumes at the saved phase.
5. **Scenes:** CombatGreybox and TavernGreybox rebuilt (approved); Boot and MainMenu added; Boot is first in the build list.

**Phase 2 playtest feedback, round 2 (2026-09-29):**
1. **Service length:** 2.5 minutes, with last orders 20 s before the end.
2. **Pip defaults to Serving.**
3. **Chopping and the Stew Pot:**
   - Mouse or stick moves the knife.
   - Chop accuracy sets the helpings (not quality).
   - Stew orders are ladled onto the pass automatically.

**Phase 2 playtest feedback (2026-09-29):**
1. **Hand delivery:** a button serves a plate. Any waiting customer who ordered the same dish can take it; their order passes to the plate's original customer, or is cancelled with its stock returned if that customer has gone.
2. **Sold out is final** for the night.
3. **When everything sells out:** the door closes, open orders finish, and service ends early once the last diner has paid.

**Phase 2 (2026-09-29):**
1. **Freshness:**
   - Stored per stack, from 0 to 1; merged stacks take the count-weighted average.
   - Stock is used least-fresh first, then lower quality first.
   - Designed for later overnight storeroom decay with preservation upgrades (recorded in the GDD and CLAUDE.md).
2. **UI layering:** UI may read Tavern/Dungeon state; gameplay never references UI (CLAUDE.md).
3. **Sold out:**
   - Marked on the HUD.
   - New customers order something else, or leave with −1 renown (a walkout is −3).
   - Orders reserve their ingredients when placed.
4. **Minigame length:** 5–10 s each at default tuning, noted in each config and enforced by tests.
5. **Scenes:** never overwritten without approval. **Pushing:** at the end of each session.

**Phase 1:** whole-stack Lockbox; stack size 3; overkill tension with a clean-kill check mark; Inedible drops kept; uninterruptible Slime with its own tell.

## Open design questions

1. **Stronghold defense events:** still undecided. Not built; the Tavern floor is solid ground so combat could be added.
2. **Dropped plates:** a dropped plate currently re-queues the order if stock allows, costing the ingredients again; otherwise the customer leaves as sold out. (A dropped spare is just gone.) Is that the right cost?
3. **Staff scope:** one helper covers one station. Should later staff cover multiple stations, or share one?
