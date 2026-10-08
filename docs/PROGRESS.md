# Hearth & Hollows — Progress

_Last updated: 2026-10-08 (**4f complete**, tag `milestone-4f`; **4g complete**, tag `milestone-4g`; **4h complete**, signed off 2026-10-08, tag `milestone-4h`). **4i approved** (2026-10-08, decisions D1–D10); **4i-A and 4i-B signed off** (2026-10-08); **4i-C approved** (2026-10-08, with D5), in progress: the non-audio work built; the sounds wait for the owner's listening (`C:\Dev\Music\SFX\_catalog\LISTENING_4iC.md`)._

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
| 4c | Tavern and UI migration (**complete**, 2026-10-04) | Top-down tavern, customers pathing to tables, 2D serving, Grill/Tap/Serving with their haptics, all UI rebuilt in uGUI + STM. **Acceptance criterion: the complete top-down day loop is restored through `GameFlow` (Tavern → Dungeon → Tavern, Morning to Night, saves).** |
| 4d | Biome 1 runs (**complete**, approved 2026-10-05) | Room-by-room structure, room rewards, run power-ups, 3 floors plus a boss arena. The reward architecture must leave room for future persistent reward kinds (customization discoveries, GDD §6.6; quest objects, GDD §4.8) without building any, and ingredient rewards must not assume monster parts are the only ingredients (GDD §5.5). Scope otherwise unchanged by v0.5; no farming, fishing or ranching. Step 5 adopted the new day order (daytime placeholder → evening → the night's delve → night → sleep). |
| 4e | Combat depth and boss (**complete**: signed off 2026-10-05) | The Larder Troll (the Cellars boss), the Harvest Finisher (small, plus an optional finishing moment on the defeated boss), the boss's rewards (Gold, a Premium larder cache, a persistent first-clear record, a hook for 4f's trophy), and tuning the cleaver and enemies for it. Deferred: more weapons (until 4g), Kitchen Arts, field cooking, Essence Tonics. Removed: weapon rarity and affixes, Delve Marks, relics. |
| 4f | Tavern Stage 1 content and customization foundation (**complete**: approved and signed off 2026-10-06; plan `docs/PLAN_4F.md`, record below) | *Added in v0.5 (approved 2026-10-05):* the customization foundation handles several areas from the start, proven with one small guest room as a second decoratable area (no guests yet); the recipe rework follows the surface and dungeon ingredient model with a few bought surface staples. Butcher Block minigame, all Biome 1 recipes, customer requests, Pip and Gunta, and **a real customization foundation** (revised 2026-10-04): Decorate Mode (move, add and remove furnishings, functional furniture where feasible), persistent layouts, Gold purchases, nav rebuild and service-layout validation, the furniture definition and data pipeline, a substantial curated Minifantasy catalog (visibly different taverns, not a token handful), one proven recolouring workflow, controller-first decorating UX. Not every possible furnishing: the pipeline and a substantial first collection, growing through Phase 5. Then **one small end-to-end reward loop in Biome 1** (added 2026-10-04): fight → a furnishing discovery drops → pick it up → extract → permanently owned → placed through Decorate Mode; a small real drop pool on suitable Biome 1 enemies, at least one rare or unique furnishing from the Biome 1 boss, persistent ownership and unlock state, visible pickup and reward feedback, and extraction and death behaviour under the rule approved when it is designed. An integration slice, not the production loot catalog. Customization may take more of 4f's budget than first planned (a learning priority). |
| 4g | Story, quests, character creation and relationship reactivity (**complete**: signed off 2026-10-06, tag `milestone-4g`; `docs/PLAN_4G.md`) | Dialogue System for Unity, Quest Machine and Love/Hate (relationships: Affinity and Respect) behind Hearth & Hollows adapters; uGUI + Super Text Mesh dialogue; Portrait Generator portraits; character creation; the Act I opening and onboarding; the representative quest **Boog's Bomb** (a quest object from the Hollows); relationship reactivity for Boog and Pip; save/load of dialogue, quest and relationship state in `SaveSystem`. Checkpoints: A (Steps 1–3, technology), B (Steps 4–6), C (Steps 7–8). (Dialogue System replaces Yarn Spinner entirely, locked 2026-10-03.) |
| 4h | Village and daytime slice (**complete**: signed off 2026-10-08, tag `milestone-4h`; `docs/PLAN_4H.md`) | A small part of Kariaston and the tavern grounds walkable in the daytime, replacing the Morning panel; a cheap prototype of the daytime time model (ticking clock or player-controlled phases); 3–4 named villagers with homes, simple presence or schedules, dialogue and relationship hooks; named villagers chosen into evening service alongside Visitors (today's customers become Visitors); one small farm plot feeding the storeroom; the full new day loop through `GameFlow`. Not: ranching, fishing, Inn guests, Visitor promotion, resident recruitment (Phase 5). |
| 4i | A playtest-ready vertical slice (*was "menus, options and polish"; **approved 2026-10-08** with D1–D10, `docs/PLAN_4I.md`; 4i-A built, waiting for the owner's playtest*) | The owner's objective: an unfamiliar player can launch the game, understand what to do, play several complete days and enjoy it without help. Four checkpoints: A first impressions (menu, pause, onboarding, trustworthy saves), B settings and accessibility (persistent options, audio mixer and volumes, feel, display, text speed, assists), C presentation and polish (real sound effects, audio balance, in-game credits with HeatleyBros' link, presentation bugs), D playtest-ready (release Web and Windows builds, performance, save compatibility, an external playtest). |

**Roadmap v0.5 (proposed 2026-10-04, approved 2026-10-05).** The v0.5 additions above (the guest room and the surface-staple recipe rework in 4f, the villager quest in 4g, the village and daytime slice as 4h, menus and polish as 4i), and Phase 5 opening with life-sim prototypes before content build-out (the Inn and Visitors; settling residents on the three plots; fishing with its minigame; ranching; farming depth), are in GDD §11.1. Until approved, the approved plan is 4d–4h as before.

**Title and terminology (2026-10-05, documentation and three strings; approved 2026-10-05).** The working title is now **Hearth & Hollows** (was Hearthdelve), and the underground world beneath Brackenford is **the Hollows** (was "the Dungeons"); a run is a **delve**, and the biomes are regions of the one Hollows, from the Cellars down to the Heart. Recorded in `CLAUDE.md` (Names) and the GDD (§1.1 the title, §2.1, §4.6, glossary). Player-facing English changed: the main menu title (`menu.title`, "Hearth & Hollows") and the two death subtitles and the power-up footer ("the Hollows cast you back to the surface", "it lasts until you leave the Hollows"). Deliberately unchanged: the repository, Unity project and code name Hearthdelve (namespaces, assemblies, menus, input actions asset), `Dungeon*` classes, scenes and the Dungeon input map, localization keys, save identifiers, and the Unity **Product Name**, which stays "Hearthdelve" because it sets the desktop save folder (`LocalLow\sagaphy\Hearthdelve`) and the build's file names; changing it is a separate migration. Verified in Unity on 2026-10-05: EditMode tests pass (`TextStyleTests` expects the new title) and the main menu title fits.

**Direction update: a village life sim (2026-10-04, documentation only, GDD v0.5; accepted 2026-10-04, when 4d step 3 resumed under it; its roadmap approved 2026-10-05).** Hearthdelve moves toward a Stardew-like fantasy life sim centred on owning and running a tavern and inn, with the dungeon as a major pillar inside a broader daily life. Recorded in the GDD and `CLAUDE.md`: the experience target (GDD §1.1); the new day, wake → free daytime → evening prep and service → nighttime delve → sleep (§3.1), with Essence still the dungeon's only time pressure; Brackenford as a small persistent village with named villagers, generated Visitors, promotion of relevant Visitors to persistent identities, and about three fixed plots for recruited residents (§2.8, §6A.5); the Inn on the same customization architecture (§6.8); farming, ranching and fishing as future daytime activities (§6A); the surface and dungeon ingredient model (§5.5) and the dungeon's role (§4.8); the mixed tavern crowd (§6.3); the progression philosophy (§7.4). The Stronghold, tower-defense and defense-event direction is dropped (§6.4; old text in GDD Appendix C); Morale is reinterpreted as the village community's state, with its separate purpose flagged. Story Acts II–IV are kept unchanged but conflict in places; the conflicts are listed for revision (§2.9). A revised roadmap is proposed (above). **Nothing was built:** no code, scene, prefab, asset, package or setting changed, the 4c customer system and the day loop as built are unchanged, and 4d step 3 waits for approval of this update.

**Design direction recorded (2026-10-04, documentation only).** The GDD (v0.4) and `CLAUDE.md` now record the project's **learning priorities** (GDD §1.5: dialogue writing, building and tavern customization, cooking minigames, memorable NPC interactions, relationships and reactivity, top-down combat; depth and variety are welcome there), **tavern customization as pillar 7** (§6.6: Decorate Mode, data-driven furniture and layouts, functional furniture, validation against service needs, Gold purchases, dungeon and boss discoveries, a curio channel to be designed, duplicates, palette recolouring, a large Minifantasy catalog, one architecture through Sanctuary and Stronghold), **worker customization** and canonical characters keeping their names (§6.7, §2.5), **contextual NPC reactivity** including reactions to the tavern (§2.7), and cooking as a depth priority (§5.4). Roadmap consequences: 4f gains a real customization foundation and (approved with one adjustment, 2026-10-04) proves one small dungeon-to-tavern reward loop in Biome 1; 4d and 4e keep their scope but their reward architecture leaves clean extension points for customization discoveries; Phase 5 owns the large catalog, many drop pools, more boss rewards, rarity tuning, Sanctuary and Stronghold customization, advanced customization content and worker customization. Nothing was built.

**Design direction recorded (2026-10-03, documentation only).** `CLAUDE.md` and the GDD (v0.3) now record the design philosophy (GDD §1.5), tavern immersion as a pillar (§6.5), preparation depth for rare dishes (§5.4), persistent quests (§2.6), relationships and recurring patrons (§2.7), the Pixel Crushers ownership boundary and save authority (§10.3, §10.6), and Portrait Generator portraits (§8.1). Roadmap consequences: 4g's scope is spelled out above. **4c's approved scope is unchanged:** the Grill, Tap, Stew Pot and serving work continues as planned, and nothing from Dialogue System, Quest Machine, Love/Hate or the Portrait Generator is imported or built before 4g. Multi-stage dishes are long-term direction, considered when 4f or a later milestone is planned.

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
3. The Latin-1 limit on UI text is temporary. When the real game font is chosen, replace it with a proper glyph-coverage check against that font. *(Done in 4c step 6: m5x7 and `TextStyleTests`.)*

**Next: 4c,** the tavern and UI migration.

### 4c plan (approved 2026-10-03)

Goal: the complete top-down day loop restored through `GameFlow` (Main Menu → Morning → Delve → Evening service → Results → Night → Sleep → Morning, with saves), working in the web build. Out of scope: room graphs and run rewards (4d), combat depth and the boss (4e), furniture placement, the Butcher Block, new recipes and Pip's and Gunta's characters (4f), story, quests and dialogue (4g).

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
- **The evening (until the prep screen in step 4):** played on its own, the scene opened straight into a debug evening (a debug-filled storeroom, the first dishes it can make). Step 4 replaced this with Prep.
- **Debug keys** (editor and development builds): **F5** ends the service now (everyone goes home), **F6** lets a customer in.
- **Tests:**
  - EditMode: the same seed always gives the same look; looks vary and every option gets used; empty lists and beard chance.
  - PlayMode (`TavernCustomerTests`): a customer walks from the door to a seat without ever clipping furniture, sits on it facing the table and stays put; with every seat taken they queue at the right spots, show patience, then take freed seats; patience running out makes them walk out upset and leave by the door; they walk through the player without moving them; the layered look stays in step (same animation and frame on every layer) and never changes; Pip reaches the serving post; the fourth table is put away and its floor becomes walkable; ending service sends everyone home.
- Applied to the existing scene by *Update Tavern* (no rebuild).

**Step 2 playtest fixes (2026-10-03):**

- **Pip "vibrated" at the pass.** Two causes. TDE eases characters to a stop, so Pip drifted past the post, turned back and overshot again each frame, flipping his facing. And the walk action and the staff agent were both steering. Now NPCs start and stop at once (no easing), and on arrival the agent switches the AI off and stands still, walking again only once the goal is clearly away (a little hysteresis). Queueing customers get the same, so they stand still in line instead of walking on the spot.
- **Walking behind the Grill and Stew Pot,** for immersion: this reverses the step 1 change that walled them off. There's a tile of floor behind the range again (reached round its east end), and the cauldron moved half a tile forward so a full tile is clear behind it. Stations can now be used from more than one spot: both highlight and work from the front or from behind, where the player is drawn behind them, like a cook at the stove. Staff still work from the front.
- Tests: Pip stands still, idle and without turning at the post; queueing customers stand still; the Grill and Stew Pot each have a walkable, reachable spot behind them that targets and highlights them.

**Step 2 approved (2026-10-03).** Layered looks and deterministic appearance kept as they are; aisle overlap, the idle pose when seated and Pip's stand-in accepted for now.

**Step 3 done (2026-10-03), awaiting review and playtest: stations, the pass, serving and Pip's jobs.**

- **Cooking** (the prototype's rules, top-down): using the Grill or Tap cooks the next order there in its **panel** over the room (uGUI and Super Text Mesh); using the empty **Stew Pot** puts a batch on and opens the **chop** board; the pot then simmers on its own and ladles helpings onto the pass. While a panel is open the Minigame input map is on and walking is off; Esc steps away and the order (or the stew's ingredients) goes back. The kitchen plays its working animation while anyone cooks at the Grill. The stew pot shows a bar while it simmers and a pip per helping.
- **Hints say what Interact will do:** "E: cook Kebab", "E: pick up Gelbrew", "E: serve Kebab", "They ordered Cellar Stew", "Cellar Stew is simmering", "Pip is working here".
- **The pass** shows up to four waiting plates. Using it picks up the next one (or puts the carried one back).
- **Serving in 2D** (decision 2): the dish's icon shows in a bubble over the keeper's head, with a spill meter above it once anything spills. The keeper walks at the carry speed (4 tiles/s; 6 without a plate). Customers walking across the floor bump the plate: the faster you close on each other the more it spills, and a full meter drops it (the order goes back to the kitchen if the stock allows). Seated customers become targets while you carry a plate: gold corners round them, "serve" if it's their dish, otherwise what they ordered. The score compares your time with par for the **shortest walkable path** from where you picked the plate up to where you served it, so wandering costs quality. `ServingMinigame` was rewritten for 2D (positions belong to the world now) with new EditMode tests; the tuning was redone for the top-down room (`ServingConfig`).
- **Pip** does their job: at the Grill, Tap or Stew Pot they stand at it and cook through the ported staff logic (lower quality, capped); the keeper can't use a station Pip is working. On Serving they take plates someone is waiting for from the pass and walk them over at a speed set by their skill (bumps count for them too), and take a plate back if its customer leaves or is served first.
- **Dish icons** for all seven recipes (`ASSET_MAP.md`); waiting customers now show their order in their bubble.
- **Readability with several people moving:** the carried plate sits in its bubble at the same height as customers' bubbles, clear of the head, with its spill meter above; patience bars sit lower. In captures with five customers and the keeper carrying, plates, bubbles and patience bars stay distinct. The open panel covers the lower part of the room (customers there are hidden while you cook); worth your eye in the playtest.
- **Tests:**
  - EditMode: 2D serving (the shortest way scores 1, wandering scores less, par comes from the shortest path to where it was served, bumps spill, harder bumps spill more, a full meter drops, the cooldown), staff serving slower at lower skill.
  - PlayMode (`TavernServiceTests`): the Grill cooks an order in its panel with the Minigame map on and walking off, the kitchen works, and the plate shows on the pass; stepping away puts the order back; carrying shows the plate, slows the keeper, and serving the right customer starts them eating; the pass takes a plate back; walking customers spill and finally drop a plate; the Stew Pot chops, simmers, shows five helpings and ladles one to the pass; Pip on Serving carries and serves; Pip at the Grill cooks while the keeper can't take over.
- One 4a test's lit-sprite exemption now covers markers on the Above layer (the tavern player prefab carries the plate overlay); the 4a scenes are untouched.

**Step 3 approved (2026-10-03).** Cooking, the pass, carrying, serving and Pip kept as they are, tuning included. The station panel covering the lower room stays unless full-service playtests show it hides information that causes unavoidable failures (reduced awareness while cooking is fine).

**Step 4 done (2026-10-03), awaiting review and playtest: Prep, the Tavern HUD and Results — one complete evening.**

- **The evening runs Prep → Service → Results** (`TavernDirector.Phase`). Played on its own the scene starts at Prep with a debug-filled storeroom; Results offers another evening (the scene reloads). The day loop (Morning, Night, the real storeroom from delves) is step 5. During Prep and Results only the UI map is on, so the keeper stands still while you plan.
- **Prep** (a panel over the room):
  - the **storeroom** as satchel-style slots (icon, count, quality dots, freshness bar);
  - a **card for each dish**: icon, name, value ("8 gold", "6 gold a bowl"), how many the storeroom makes ("13 to serve", "2 pots") and **how it's prepared** ("Grill", "Chop, then Simmer"). The step list comes from `PrepRules.Steps` and the card draws however many steps a dish has, so multi-stage dishes (GDD §5.4) won't need a new card. A dish the storeroom can't make is dimmed and can't be chosen;
  - choosing: tap a card to put it on tonight's menu, tap again to take it off; at most three ("Tonight: 2 of 3 dishes"); chosen cards turn gold with gold corners (by shape as well as colour);
  - **Pip's job** (a button cycling Serving, Grill, Tap, Stew Pot, off); Pip walks to that post at once, so you see the choice in the room;
  - **Open the doors** (only when a dish on the menu can be made) and **Close for the night**. When nothing in the storeroom makes any dish, a line says so in place of the menu count, leaving closing as the honest choice. F4 (debug builds) fills the storeroom again.
- **Tavern HUD** (only during service), in the side margins so the room stays the focus:
  - left: a **clock bar** running down, "Last orders!" near the end, gold, tips and Renown so far, and tonight's menu as icons (a sold-out dish dims and gets a red bar);
  - right: the **order rail**, one row per open order: dish icon, a one-word state (Waiting, Cooking, Ready, Serving, Stewing, Spare) and a thin bar for that customer's patience, green to red.
  - Interaction hints and the carried plate's spill meter stay as in step 3 (the hint line at the bottom, the plate and meter over the keeper's head).
- **Timing and sell-out:** service lasts its set length with last orders near the end (unchanged rules); when every dish on the menu sells out the door closes to new customers and service ends as soon as the last diners have paid ("Everything sold out, so we closed early.").
- **Results:** dishes served, gold earned, tips, Renown, then walkouts, sell-out leaves and dropped plates, each only if it happened. Lines appear one by one with the numbers counting up, then the evening's **takings** (payments plus tips). The button finishes the reveal if pressed early, then starts another evening. Closing without opening says "We kept the doors shut tonight." Banking the takings into the save is step 5.
- Pure logic with EditMode tests: `PrepRules` (steps, how many can be made, menu toggling up to three, whether the doors can open) and `EveningReport` (the lines, good news first, trouble only when it happened).
- PlayMode tests (`TavernEveningTests`): the scene opens at Prep with walking off and a card per dish showing its steps; choosing caps at three and un-chooses; Pip's job is set at Prep and kept; opening shows the HUD and turns walking on; an order appears on the rail with its icon, state and patience, the clock runs down and the takings update; selling out ends service early and Results tells it, then another evening's Prep opens; closing for the night says the doors stayed shut; the HUD keeps to the margins without overlaps. The earlier tavern tests now open an evening explicitly.
- `TavernEveningCaptures` (explicit, run by name) renders Prep, the HUD and Results to `BatchLogs/` for layout checks.
- Strings: shorter order-rail states ("Ready", "Stewing") so they fit the 40-px margin.

**Your playtest (Tavern scene, Play):**
1. Prep: check the storeroom, choose up to three dishes (try a fourth, and un-choosing), change Pip's job and watch them walk to it, then Open the doors.
2. During service: watch the rail and the clock; does the HUD tell you what you need without pulling your eyes from the room?
3. Sell out on purpose (choose one dish with little stock) and check service ends once the last diners pay.
4. Read Results, then press the button for another evening. Also try F4 at Prep, and Close for the night.

**Step 4 playtest fixes (2026-10-03):**

- **The keeper glided in the idle (glancing about) instead of walking.** TDE counts a character as grounded only over a Ground-layer collider, and none of our floors has one, so every TDE character sat in the Falling state and never reached Walking, which the sprite animator plays the walk from. This was true in the dungeon too (the player and the spider), hidden by attacks and dodges. New `FloorController2D` (a subclass of TDE's 2D controller, in our assembly) is grounded everywhere except over a hole; the generators add it, and *Hearthdelve → Setup → Upgrade Character Controllers* converted the seven existing prefabs in place (only the script reference changes). The idle still has the art's occasional glance while standing still; customers and Pip use the same idle.
- **The kitchen drew over the keeper in front of it.** The kitchen is one sprite sorted at the bottom of its frame; the range's footprint started 3 px up but the oven's 10 px up, so in front of the oven the keeper could stand inside the sort point. Both footprints now start 3 px up, on a tile edge (so pathfinding keeps the row in front open); every character's collider is at least 0.4 tiles tall, so anyone stopped in front draws in front. The furniture test now walks up to every footprint of a piece, not only the lowest.
- **A released stick stepped menus back.** The stick springs back slightly past centre, and the UI's Navigate action read that overshoot as a press the other way. The left stick's Navigate binding now has a 0.5 dead zone (menus only; walking and aiming are unchanged). Gamepad tests with a virtual pad cover the walk animation and the spring-back.
- Builder fix: the stew pot's progress bar was also named "Bar", and an update could find it instead of the bar; it's now "Progress", and the builder finds the bar by path.

**Step 4 approved (2026-10-03).** Kept as decided: the larger Prep panel (a dedicated planning phase; readable dish information matters more than seeing the room behind it), stew cards counting pots, the short order states, the HUD in the side margins, and the Results pacing. Prep's dish cards keep drawing a list of preparation steps, ready for multi-stage dishes later (not built in 4c). The missing Shroom Cap and Spore Sac icons stay a known content gap.

**Step 5 done (2026-10-03), awaiting your full-day playtest: Boot, Main Menu, Morning, Night, the delve, saves — the whole day.**

The day: **Boot → Main Menu → New Game / Continue → Morning → Delve → delve result → Evening Prep → Service → Results → Night (upgrades, saves) → Sleep → next Morning.** `GameFlow` (in Boot) owns every scene change; the tavern and the dungeon never refer to each other.

- **Boot** (new scene, first in the build list): `GameFlow` with the game database, localization, haptics, the F8/F9 debug keys, and the transition overlay. It stays loaded while the menu, tavern and dungeon load and unload beside it (GDD §10.2). A scene's own haptics service now steps aside for Boot's without taking its scene's other managers with it.
- **Transitions** tie the day together: each scene change fades to black, names where the day is going ("Morning · Day 2", "Into the dungeon", "Evening · Day 2") and fades in. Nightfall, which happens in the tavern without a scene change, cuts to "Night · Day 2" and fades into the room. The tavern's light follows the day too (`TavernMood`, tunable): cool daylight in the morning, the warm evening, dim blue at night.
- **Main Menu** (new scene): Continue only when there's a save that can be read, saying where it resumes ("Day 2, morning"); New Game. Starting over a save asks once (Back is the default). A new game is saved straight away, so Continue never brings back a game you started over from. Played on its own, the menu scene loads Boot.
- **Morning** (the tavern, a panel like Prep): the storeroom; breakfast cards for the Grill and Tap dishes with a buff (the established rules: one dish per morning, cooked at its station's panel with the real minigame, quality scales the buff, stews aren't breakfast; Esc puts the ingredients back); what you ate; **today's delve bonuses** from upgrades and breakfast ("Today's delve: satchel +1 · Essence +17"); and *Descend into the dungeon*.
- **The delve** is `Dungeon_TestFloor` (until 4d), started with the day's loadout (upgrades' satchel slots and max Essence, breakfast's Essence or slower drain). Extracting or dying (with the Lockbox) shows the delve result, whose button now reads *Back to the tavern*; the report goes to `GameFlow`, the haul goes into the storeroom, the breakfast is used up, and it's evening. F8 in the dungeon extracts. Played on its own the test floor still restarts itself.
- **Evening:** step 4's Prep → Service → Results with the real storeroom; seat upgrades bring out the fourth table. Results' button reads *Close up for the night*: the takings are banked, autosaved, and it's Night. *Close for the night* at Prep (nothing to cook, or by choice) goes straight to Night, with no empty Results to click through.
- **Night** (a panel): today's summary (how the delve went, parts brought home and lost, dishes served and walkouts or "The doors stayed shut tonight.", gold banked, Renown today), the purse, the existing three upgrades (each showing its level, what the next level adds, and its price; greyed when you can't afford it), "Game saved." after the autosave and each purchase, and *Sleep*. Sleep applies overnight freshness loss, saves, and the next morning loads.
- **Saves** (the versioned JSON `SaveSystem`, unchanged format): at New Game, as night falls, after each purchase, and on Sleep. Continue resumes at the saved phase.
- **Debug:** F8 finishes the current phase the way the player would (Morning sets off, the dungeon extracts, Prep closes, service ends, Results banks, Night sleeps); F9 adds 100 gold; F4 at Prep only on its own or if the game database allows it in the day loop (`allowDebugFill`, off).
- Tests:
  - PlayMode `DayLoopTests` (from the Boot scene, saves in a temp folder):
    - the whole day: New Game, breakfast cooked and eaten (raising max Essence on the delve), a haul extracted into the storeroom once, a dish served, Results, takings banked and autosaved, an upgrade bought and saved, Sleep (freshness lower, day 2 saved), then a fresh boot and Continue restoring gold, Renown, upgrades, the storeroom (no duplicates, same freshness), with the upgrade's extra satchel slot on the next delve;
    - death with the Lockbox (only the kept stack comes home);
    - closing with nothing to cook (straight to Night, saved, Sleep to day 2);
    - New Game over a save asks, Back keeps it, Start over replaces it.
  - EditMode: repeated save/load round trips neither duplicate nor lose a hauled day.
  - `DayLoopCaptures` (explicit) renders the menu, Morning, nightfall and Night.

**Your playtest (open the `Boot` scene and press Play):**
1. New Game. In the Morning, check the storeroom (empty on day 1), then *Descend*.
2. In the dungeon, harvest a few parts and extract. Check the result, then the Evening's storeroom.
3. Prep, serve, and close up after Results. Check the Night summary and the banked gold, buy an upgrade if you can (F9 for gold), and Sleep.
4. Day 2: if you brought home a Grill or Tap dish's ingredients, cook breakfast and watch "Today's delve" change; check the bonus in the dungeon.
5. Stop Play, press Play on `Boot` again, and Continue: you should be back where you saved.
6. Try the other ways a day goes: die in the dungeon (Lockbox), and close for the night with nothing to cook.
7. Overall: does it feel like one day, and is it always clear where you are and what to do next?

**Step 5 playtest fix (2026-10-03):** flicking the left stick (down-left especially) and letting go could leave the character facing the opposite way. A released stick springs back past centre for a frame or two; that overshoot read as a short push the other way, and the character turned to face it as it stopped (the same spring-back as the menu bounce in step 4, but walking can't simply take a wide dead zone without losing gentle movement). New pure `StickReleaseFilter` (EditMode tests), applied to movement in `HearthdelveInputManager`: a weak push pointing away from a strong push made in the last 0.15 s reads as rest. A real push the other way (over half) passes at once, a gentle one once the window is over, and keyboards are unaffected. Tunable on the input manager (`StickRelease`). A virtual-gamepad PlayMode test reproduces the flick, spring-back and release, and checks that a real turn still turns. The filter applies in the dungeon and the tavern alike.

**Step 5 playtest fix (2026-10-03):** a different screen flickered behind the transition caption ("Evening · Day 1", "Morning · Day 2"). GameFlow unloads the old scene before loading the new one, so for a moment (as long as the load takes) there was no camera at all; URP draws the screen-space cover only as part of a camera's render, so the screen showed whatever was there last (in the editor, the Game view's "No cameras rendering"). Boot now has a **Boot Camera** that is always there, behind every scene's own (depth −100), drawing only black. Added to the existing Boot scene in place (*Hearthdelve → Generate → 4c Boot and Main Menu* updates an existing Boot rather than rebuilding it). A PlayMode test walks a transition frame by frame and fails if any covered frame has no camera.

**Step 5 approved (2026-10-03).** The full loop works and is fun. Kept as decided: the transition captions, the morning, evening and night lighting, skipping an empty Results when the doors never opened, and the movement/use reminder only during service.

**Step 6 done (2026-10-03), awaiting your playtest: the font, text style, feedback, haptics and feel.**

*Font and text*
- **m5x7 is the game's font** (one copy, `Assets/_Project/Fonts/m5x7/`; the loose `Fonts/m5x7.ttf` was moved there with its `.meta`, nothing downloaded or replaced). Imported as hinted raster at 16, its own data only, no fallback fonts. Super Text Mesh draws it at size 16 (one font pixel per game pixel at 320×180, measured) or 32 for the game's name and the transition caption, rasterised at 16 and point filtered, on a 10-pixel line (`GameFonts`; any other size drops or doubles pixel rows, so none is used).
- **Whole-pixel UI scaling** (`PixelCanvasScaler`): canvases scale by the same whole number as the Pixel Perfect Camera's zoom (×6 at 1080p, ×4 at 720p and at 1366×768), never fractionally, so text and UI art stay crisp at any window size. The canvas is then never smaller than 320×180.
- **Layouts redone around the font's real widths** (each screen's strings measured against m5x7):
  - Prep: one storeroom row across the top, and two-line dish cards (name and price, then how many and the steps).
  - Morning: two-line breakfast cards (name and station, then the buff), and the bonus line can take two lines.
  - Night: a full-width summary with the purse on its first line, and an upgrade row each with its price on a button.
  - Results: wider.
  - HUD: a label over each number in the 48-pixel margins; "last orders!" moved to the top centre, over the room; rail rows put the state under the dish.
  - Station boxes, the interaction hint, the harvest feed, the swap prompt, the death screen, the delve result and the main menu: wider.
  - Buttons are now plain parchment with a dark edge: the Classic UI pill bar's inner border ran through 7-pixel text. Selected is gold, disabled dims.
  - All rebuilt in place by the updaters (*Update Tavern*, *4b Update Test Floor UI*, *4c Boot and Main Menu*). The 4a look-test scenes are untouched and keep the old font as baselines.
- **Lower-case English**, written into the source strings (no runtime lowercasing): every UI string, and the content names (ingredients, dishes, enemies, customers, upgrades) now authored in code (`LocalizationBuilder.ContentEnglish`). Proper nouns keep capitals: Hearthdelve, Pip, the Cellars. So do control labels: WASD, E, A, B, X, Space and the F keys. The rule is in `CLAUDE.md` and GDD §8.2.
- Wording tightened where the font needed room:
  - stew prices read "8 gold/bowl";
  - a dish's steps read "chop, simmer" (still drawn from the step list);
  - a dish not in stock shows "0 to serve";
  - the slower-drain buff reads "essence drain -30%";
  - station prompts are shorter ("Space / A: flip in the gold band", "Space / A: pour · A D / LS: tilt for foam", "A D / LS: move the knife · Space / A: chop");
  - the debug fill hint reads "F4: fill storeroom".
- **Glyph coverage replaces the Latin-1 rule:** m5x7 covers Basic Latin, Latin-1 (less the soft hyphen), Latin Extended-A and €. `TextStyleTests` checks every string in every locale's tables against the font itself.

*Feedback and haptics* (one combined feedback per moment: a placeholder sound, visuals and a named pattern in the same MMF player; all read from the minigames' and session's own state, never timed separately; tuning in `TavernFeedbackConfig`)

| Moment | Haptic | Also |
|---|---|---|
| Grill flip | `Tap.Light` | sizzle tick |
| Perfect flip (≥ 0.9) | `Pulse.Success` | bright ding, the box flashes gold |
| Burned side | `Buzz.Failure` | hiss and thump, a small shake, the box flashes red |
| Grill past the band | rising rumble (nothing before the band ends) | the sizzle loop grows, the needle reddens |
| Pouring | steady low rumble, the high motor rising toward the line and beyond | the pour loop grows |
| Fill reaches the line | `Cue.Threshold` | glass ting, the box flashes gold |
| Clean pour (≥ 0.85) / plain / overflow | `Pulse.Success` / `Tap.Light` / `Buzz.Failure` | clink / clink / splash and a red flash |
| Clean cut / ragged cut | `Tap.Firm` / `Cut.Ragged` | thock / dull double thud |
| Board finished | `Pulse.Success` (half strength for a poor board) | flourish |
| Stew pot ready | none | bubble and bell, once per pot, not per helping |
| Plate picked up / put back | `Tap.Light` | clinks |
| Bump: brush / collision | `Bump.Soft` / `Bump.Hard`, scaled by strength | thud (and a tiny shake for a collision) |
| Spill near falling | `Bump.Hard`, once per plate | wobble; the spill meter reddens |
| Dropped | `Buzz.Failure` | crash and a shake |
| Served | `Tap.Firm` | warm three notes |
| Payment / walkout | none | coin and the takings flash gold / a sour fall (and the angry emote) |
| Menus: a choice / a commitment (open the doors, descend, sleep, start, return to the surface, close up) / a purchase | none / very light `Tap.Light` / gentle `Pulse.Success` | click / click / chime and the row glows |
| Results lines / takings | none | soft ticks / a chime |
| Breakfast eaten | the cooking moments above | the delve's bonus line flashes |

- Only the keeper's own cooking and carrying are felt; Pip's work makes no vibration. Haptics follow the player's settings (on/off, intensity, reduced) through the haptic service and do nothing where unsupported (the web build); nothing depends on feeling them. Flashes follow the flash setting.
- Tests:
  - EditMode: the feedback rules (quiet before the band, rising to the burn; perfect, plain and burned flips; the pour's rise and the line crossing once; pour results; clean and ragged cuts; soft and hard bumps in proportion; the spill warning once); every tavern pattern is a named pattern in the library; m5x7's import and single copy; glyph coverage in every locale; lower-case source strings except listed proper nouns and controls; key strings' authored casing; no code changes text case; whole-pixel scale; every text in the day loop's scenes is m5x7 at a pixel size and every canvas scales by whole pixels.
  - PlayMode: a perfect flip pulses and flashes, the warning comes only past the band and grows, a burn buzzes and the warning stops; soft, hard, warning and dropped plates, and Pip's bumps ignored; vibration off (and a controller that can't rumble) silences the motors and breaks nothing; a dish card clicks, opening the doors is a light tap; a purchase plays its moment and the row glows.

**Your playtest:** play a full day from `Boot` and judge:
1. Is the text crisp and readable (HUD, order rail, Prep cards, Results, Morning, Night, hints, upgrades, captions, menu)? Try a windowed size as well as full screen.
2. Does m5x7 suit the art, and does the lower-case style feel cohesive?
3. Cooking: does the grill feel tactile (flip taps, the warning past the band, a perfect flip against a burn)? The tap (the pour, the line, a clean pour against an overflow)? Chopping (clean against ragged)?
4. Serving: are brushes, collisions, the near-fall warning and a drop proportionate? Is a successful serve satisfying?
5. Overall: is the vibration useful rather than constant, and does the game feel more alive without becoming noisy?

**Step 6 playtest fix (2026-10-03):** the haptics and feedback feel right; the text was readable but titles, labels and amounts were hard to tell apart (one font, one size, one ink), and some places felt cramped. A text hierarchy, by colour and layout rather than size (a pixel font has only 1× and 2×):
- **Titles** in a deep red, with a thin rule under each panel's title; section headings ("tonight: …", what you ate) in the same red.
- **Labels and amounts are separate text:** labels in a muted tone, amounts in full ink (on the dark HUD, a dim label over a gold number, with a gap between stats). Night's summary and Results are now two-column ledgers (label, then amount); the takings sit under a rule in gold. Prices on Prep's cards are gold.
- **Slot counts** are a plain dark number in the slot's corner (a white "×12" covered half the icon in m5x7).
- Strings split accordingly (the summary's and results' keys are now labels, with value strings beside them); tests follow.

**Step 6 second playtest change (2026-10-04):** the font is now **Silver** (Poppy Works), and resource names keep a capital in English (Essence, Gold, Renown; also reserved for Morale, Cheer, Marks); everything else keeps the lower-case style ("gold band" the colour stays lower case, as do tips, satchel and lockbox).
- **m5x7 is removed** from the project. Silver is pixel-exact at STM size 19 (1×) and 38 (2×): its pixels are 100 units on a 1900-unit em, capitals 9 px, descenders 2, on a **12-pixel line** (`GameFonts.LinePixels`; was 10). Imported as hinted raster at 19, no fallback names. It covers far more scripts than m5x7 (Latin, Greek, Cyrillic, Japanese, Chinese, Korean, Thai; not Arabic or Hebrew); the glyph test still checks against the font itself.
- **Every screen re-laid out for the taller line**, against measured Silver widths (about 6% wider than m5x7): buttons 16 px tall; Prep's cards 150×24 (two lines exactly) in a 316-px panel; Prep's menu count shortened to "menu: 3 of 3"; Night's summary is now a two-column ledger (the day on the left; banked tonight, purse and Renown with today's change on the right), with shorter labels ("served", "kept shut"); the HUD's stats and order rail repacked (still 8 rail rows, one per seat with every seating upgrade); station boxes 256 px wide with the chop board clear of its prompt; the dungeon's swap, death and result panels a little taller.
- Tests follow (font identity, resource casing, renamed strings). EditMode 369/369, PlayMode 108/108 (plus 2 explicit capture tests).

**Step 6 approved (2026-10-04).** Kept: Silver as the only font and its layouts, the lower-case style with capitalised resource names, the colour/layout text hierarchy, and the feedback and haptics tuning.

**Step 7: final QA, web build, docs (2026-10-04).** A verification step; no features added.
- **Tests, from a clean compile:** EditMode **369/369**, PlayMode **110/110** (the 108 baseline plus two added in this step), plus the 2 explicit capture tests (2/2). New: `EssenceAndSeatingUpgrades_ReachTheNextDaysScenes` (the Essence and seating upgrades bought at Night reach the next day's dungeon and tavern; all 8 seats open, a rail row each) and `TheDelve_InTheDayLoop_HasNoLookTestKeys`. The nothing-to-cook test also checks Prep's first selection.
- **The day loop is covered end to end by PlayMode tests from `Boot`:** New Game, Morning breakfast, delve, extraction, Evening, service, Results, Night with banking, a purchase and the autosave, Sleep with overnight freshness, Continue after a fresh boot (Gold, Renown, upgrades and the storeroom restored, nothing duplicated or lost), death with the Lockbox, closing with nothing to cook, selling out, and both upgrade kinds applying.
- **Web build** (`BuildTools.BuildWebBatch`, Boot first) **builds and runs.** Checked in Chrome at 1280×720 (a whole 4× scale): Silver is the font (its copyright string is in the build data; no m5x7, no system fallback), crisp; no errors or warnings in the console at startup or in play (haptics no-op on the web). Played: menu, New Game, Morning, the delve HUD, Essence running out, the death screen, the delve result, Prep with an empty storeroom, closing for the night, Night, Sleep, Day 2, and Continue after reloading the page. Keyboard navigation works on every screen visited.
- **Three defects found and fixed:**
  1. **Web saves were lost on reload.** The web's `persistentDataPath` is an in-memory file system that reaches IndexedDB only when synced, and nothing synced it (IndexedDB was empty). `SaveStore` now calls `WebStorage.Flush` (a ten-line `Plugins/WebGL/HearthdelveStorage.jslib`) after each write and delete; a no-op elsewhere. `SaveSystem` and the save format are unchanged. Verified: Continue appears after a reload.
  2. **The look-test overlay's debug keys were live in the day loop's delve** (the delve runs in `Dungeon_TestFloor`): F3 would load `LookTest_Tavern` outside GameFlow, and F2/F4 changed the game's resolution and scroll mode; its label showed, and its controls line ran off the screen. In the day loop the overlay now stands down (no keys, no label) and the controls line reads "move: WASD / stick   attack: click / X   dodge: Space / B" (315 px). Run on its own, the test floor keeps F2–F4.
  3. **Prep with nothing to cook opened with a disabled card selected,** so A / Enter did nothing until a direction was pressed. It now starts on "close for the night" (otherwise the first dish that can be cooked).
- **Not exercised in the browser** (scripted input can't steer through combat before Essence runs out): combat and harvesting, rope extraction, evening service and the stations, and a real gamepad. They share their code with the PlayMode tests above; a short manual web pass is on your list.
- **Architecture checks:** Dungeon and Tavern reference only Core and Shared; GameFlow owns every phase change; the LookTest scenes are unchanged since 4b; `Dungeon_TestFloor` and `Tavern` still run on their own; no Pixel Crushers, Yarn or dialogue/quest/relationship code; `SaveSystem` unchanged.
- **Silver's license verified** (CC BY 4.0, attribution to Poppy Works, with a budget condition): `docs/THIRD_PARTY.md`, `docs/CREDITS.md`.
- Docs brought up to date: `PORT_MANIFEST.md` (what 4c ported or replaced), `ASSET_MAP.md` (font, buttons, icon gaps), the known issues below, and one stale `CLAUDE.md` line (overnight storeroom freshness loss exists since step 5).

**Step 7 playtest changes (2026-10-04):**
- **Plain punctuation.** Silver draws its period, middle dot, comma, colon and semicolon as 3×3 plus signs. Our copy is adapted (CC BY 4.0 allows it; the credit says so): those five glyphs are single-pixel dots like Silver's own "!" and "?", with narrower advances, by `Tools/fonts/silver_plain_punctuation.py` (reproducible and idempotent). No strings changed. `TextStyleTests` guards it.
- **The Essence label is now an icon.** Silver has no smaller pixel-exact size, so the word under the bar became UI Overhaul's 8×8 magic-spark icon beside the bar (the pack's icons are meant for "next to ... HP bars"), centred on it; the bar moved right to make room. (A second, smaller font was considered and dropped: Silver stays the only font.)
- **The empty storeroom row read "storeroom the storeroom is empty."** It now reads as a label and its value, "storeroom  empty"; the line under it (breakfast at Morning, dishes at Prep) says what that means.
- **A Night resumed from a save showed a made-up day** ("delve: skipped", "served 0", "banked tonight 0 Gold"): the save keeps the purse and Renown but not the day's story (found on the web build). A resumed Night now shows only the purse and Renown. Tested by `ContinuingAtNight_ShowsOnlyWhatTheSaveKnows`.

**4c status: complete, approved 2026-10-04** after the final playtest of the web build. Deferred, as planned: biome runs, room graph and the boss (4d); the Harvest Finisher, Kitchen Arts, more weapons (4e); recipe rework, customer requests, the customization foundation, Pip's own look (4f); dialogue, quests, portraits, Love/Hate timing (4g); settings, the title screen and a decorative title font (4h).

**Next: 4d (Biome 1 runs).**

### 4d plan (approved 2026-10-04)

The experience: clear a room, see the rewards behind the exits, choose what you need, go deeper or take an extraction opportunity. Decisions: combat rooms of 1–1.5 screens with the camera confined to the room; dungeon Gold is unbanked run loot (kept on extraction, lost on death, banked Gold never lost); dedicated extraction opportunities in the floor graph, at least one reachable per floor, no extracting from any cleared room; the real boss arena with a temporary elite or mixed wave (the boss is 4e); door-lock art from the catalog or a `PH_` placeholder. A floor's graph has 5–7 nodes but a route traverses about 3–4 rooms per floor (9–12 rooms plus the arena per full run; tunable). One-way choices as in Hades, no backtracking. Doors show the reward category, not the exact roll. A small reward abstraction (kind plus payload) for ingredients, Gold and run power-ups, extendable later to Delve Marks, weapons and furnishing discoveries without rewriting the graph, room clearing, previews or the run report; no plugin framework. About 8 simple run power-ups on existing tuning. No mid-run saves. `Dungeon_TestFloor` stays a standalone test bed; the day loop switches over only in step 5. Steps: 1 rooms and sealed exits; 2 seeded floor graph, Biome 1 layouts, descent, extraction, arena placeholder; 3 rewards, previews, run report; 4 run power-ups; 5 day-loop integration, tuning, web, closeout. Each step stops for a playtest.

### 4d status

**Step 1: rooms, sealed exits, room camera (2026-10-04).** A new `Dungeon` scene (*Hearthdelve → Generate → 4d Dungeon (Rooms)*; *4d Update Dungeon UI* updates its UI in place) into which rooms are loaded one at a time.
- **Rooms are data.** A `RoomDefinition` asset holds the room as rows of characters (`RoomLayout`: walls, torches, pillars, props, enemies, the arrival `P`, the rope `R`, exits `x` through the north wall, the entrance `e` through the south wall) and the prefab built from it (tiles, props, torch lights, enemies, gates, the arrival point). `RoomLayout` checks every layout: rectangular, known characters, a closed border, 1–1.5 screens (40–60 × 23–34 tiles), 2×2 doorways in the right walls, one entrance, one arrival, an exit or the rope, bats under a wall, and everything important reachable from the arrival. Four test rooms in a line: a quiet start room, a slime hall with two exits, a bigger spider den, and the rope room.
- **One room at a time.** `RoomRunner` loads a room at the origin, puts the player on its `P`, points the navigation grid at it (rebaked on first use) and the camera's bounds at it. Leaving fades to black, unloads the room (parts left lying and webs in flight stay behind), loads the next and fades back; walking between rooms costs no Essence and takes no input. One way: the entrance stays shut behind the player.
- **Sealed until clear.** Entering a room with enemies drops its gates (`RoomEncounter`, pure): a collider blocks each doorway and the gate shows its bars. When the last enemy falls the gates rise and stepping into any doorway leads on. Feedback: an iron slam with a soft bump as they drop, a rattle with the success pulse as they rise (`PH_GateSlam`, `PH_GateRise`).
- **Gate art:** the Gladiator Arena animated gate's barred interior in a two-tile gap of the Cellars' own wall (`docs/ASSET_MAP.md`); no dungeon-pack gate exists.
- **The camera stays in the room.** `RoomCameraBounds`, a small Cinemachine extension, clamps the view to the room's rectangle after the pixel-perfect correction, and centres on the room on an axis where the view is larger. Cinemachine's 2D confiner was tried first: it bakes its shape over several frames and didn't confine reliably when a room loaded; rooms are rectangles, so the clamp is exact and immediate.
- **Also fixed:** the harvest feed's longer messages wrapped and overlapped in Silver; the feed is wider now (one line per message, on both dungeon scenes).
- `Dungeon_TestFloor` is unchanged as the combat test bed (its tiles now come from the same painter as the rooms, with identical output); the day loop still uses it until step 5. The LookTest scenes are untouched.
- Tests: `RoomTests` (EditMode: layout parsing and every validation rule, the encounter, the camera clamp, every room asset valid and matching its prefab) and `DungeonRoomTests` (PlayMode: arrival, sealing, a sealed doorway going nowhere, clearing, either exit leading on, leftovers staying behind, the navigation grid following the room, the camera inside the room, the route ending at the rope). `DungeonRoomCaptures` (explicit) renders the rooms.

**Step 1 approved (2026-10-04).** Kept: one-way rooms, the fade timing, the rectangular camera clamp, the lazy per-room grid, the Gladiator Arena bars as the Cellars gate, gates dropping after arrival, the plain south entrance gap, and `Dungeon_TestFloor` untouched.

**Step 2: the generated run (2026-10-04).** `Dungeon.unity` now plays a generated three-floor Cellars run (still standalone: the day loop keeps `Dungeon_TestFloor` until step 5). *Hearthdelve → Generate → 4d Dungeon (Rooms)* rebuilds the rooms and run settings and updates the scene in place (*4d Update Dungeon Run* does only the scene).
- **The graph** (`RunGenerator`, pure, seeded): a floor is a series of steps. Floor 1 opens with a quiet start room; floors 2 and 3 open with the fight the player drops into. Each step has one or two fights (every floor has at least one choice between fights), and every room leads to at least one room of the next step, sometimes crossing over. Every room of the last step leads to both of the floor's ends: the **rope** (extraction) and the **way on** (the hole, or on floor 3 the boss arena). So every route reaches a rope, and the end of each floor asks "leave with what I have, or go deeper?". Some floors (30%) also offer an early rope off a fight partway through. One way only: exits only lead to later steps, the entrance stays shut. Rooms are chosen by kind and exit count, preferring rooms not used on the floor, then the least used in the run, then the fewest spare exits (spare exits stay shut).
- **Size:** a floor has 6–9 rooms in its graph, of which a route goes through **3–4 fights** (tunable per floor, `minFights`/`maxFights`), plus the start room or the drop-in, and the rope or hole room. A full run is **9–12 fights plus the arena**, about 15–16 rooms with the quiet ones.
- **Rooms:** 16 authored layouts: 11 fights (3 with one exit, 6 with two, 2 with three: pillar hall, cistern, crossroads, long gallery, pillar grid, storeroom, ring, narrows, den, crypt, kennels), 2 start rooms (one and two exits), the rope room, the hole room and the boss arena. Variety comes from shape, obstacles, lanes, exits and spawn points; all pass `RoomLayout` validation (which now knows spawn points `m`, perches `v` and the hole `H`).
- **Encounters are separate from layouts:** a fight room marks spawn points (`m` on the ground, `v` bat perches under a wall); the generator chooses each fight's enemies from the floor's tuning (count range and weights for slime, bat, spider) and places them on shuffled points. Deeper floors have more and more mixed enemies (floor 1: 2–3, mostly slimes; floor 2: 3–4 with bats and spiders; floor 3: 4–5, more spiders). Only the existing enemies and behaviour.
- **Extraction** is a route decision: rope rooms are graph ends; the run's rope works as before (the delve result, then the standalone scene restarts).
- **Descent:** the Hole Entrances And Ropes pack's ladder-framed hole in a quiet hole room; step in and the player sinks and fades into it (with a whoosh), then the screen fades and the next floor begins. It reads well in the captures, so the stairs fallback wasn't needed.
- **The boss arena** (56×32, four pillars, statues by the north wall) is the real Biome 1 layout. **TEMPORARY:** its fight is a placeholder elite wave (2 spiders, 2 bats, 4 slimes; `RunTuning.arenaPlaceholder`); clearing it reveals the rope home. 4e replaces only the encounter.
- **TEMPORARY signs over exits** until step 3's reward previews: ↑ the rope out, ↓ deeper (the hole), crossed swords the arena; ordinary fights have none.
- **Seeds:** each run logs its seed and its whole graph; a small debug line (bottom right) shows seed, floor and room. Set the seed on the `RoomRunner` (scene `Rooms` object) to replay a run.
- **Tuning** lives in `Data/Dungeon/RunSettings.asset` (per floor: fights, width, branch and cross-over chances, early-rope chance, enemy counts and weights; the arena placeholder). Rebuilding the rooms keeps it.
- Tests: `RunGeneratorTests` (300 seeds: determinism, connected one-way floors, the floors leading down to the arena, a rope on every route, 3–4 fights per floor and 9–12 per run, a choice on every floor, rooms fitting their places, encounters fitting their rooms, no layout more than twice on a floor, the placeholder fight, deeper floors harder, the pool) and `DungeonRoomTests` rewritten for the generated run (seeded: the quiet opening, sealing and opening only the used exits, a branch left behind, a rope ending the run, a full run through three floors to the arena and out, the camera inside every room).
- **Step 2 playtest fixes (2026-10-04):** (1) a room with more exits than the run uses kept its spare gates shut, which read as gates failing to open (seed 51108329, floor 1 room 4; floor 2 room 8). Spare exits are now bricked up with the wall's own tiles (no gate, no sign), so they read as wall. (2) Bats sleeping on perches in a room's top-left corner were hidden under the Essence bar, so a room looked clear while it was still sealed (floor 3 rooms 12 and 14). Those perches moved, and `RoomLayout` now rejects a perch before column 9 (`FirstPerchColumn`). (3) *God Mode* shows on the player's `EssenceHealth` in the Inspector for playtesting.
- **Essence and run length (not tuned yet):** base Essence is 100 at 0.5 per second, about 200 seconds of delve without damage (transitions pause the drain). A full run of 9–12 fights plus the arena will take an estimated 6–9 minutes at 25–40 seconds per fight, so on base Essence a complete run is clearly **not possible**; breakfast and the max-Essence upgrade (+20 each) don't close the gap. Not tuned in step 2, as agreed; for step 5 (drain rate, fight length and how Essence is restored or extended).

**Paused before step 3 (2026-10-04)** for the v0.5 direction update. Steps 1 and 2 stand unchanged: the authored rooms, graph generation, extraction routes, floor transitions, the arena, room loading and encounters all fit the new direction, where the delve happens at night after service instead of between Morning and Evening. 4d keeps developing standalone until step 5's `GameFlow` integration.

**Step 3 under the v0.5 direction (when it resumes, after approval).** Rewards are designed knowing a larger ingredient ecosystem exists (GDD §5.5): ingredient routes don't assume monster parts will be the player's only cooking ingredients, so dungeon ingredient rewards lean toward the unusual and the rare. The reward abstraction (kind plus payload) stays open to dungeon ingredients, quest objects, customization discoveries and future weapons and currencies. Ingredient and Gold rewards remain the prototype's focus. No farming, fishing or ranching in 4d.

**Step 3: rewards, door previews, run Gold (2026-10-04).** Resumed after the v0.5 update; steps 1 and 2 unchanged.
- **Rewards are data on the graph:** each fight gets a `RoomReward` (kind plus payload: `RewardKind` Gold or Ingredient; an item id, a quality and an amount), rolled when the run is generated, so a seed replays it. Quiet rooms give nothing; the arena's reward waits for 4e's boss. The door to a fight shows its kind; the exact roll appears when the room is clear.
- **Door previews** (replacing step 2's temporary signs): a coin for Gold, a drumstick for a dungeon ingredient, ↑ the rope out, ↓ deeper, crossed swords the arena. Where a room offers a choice of fights, the generator makes it a choice of different rewards.
- **Ingredient rewards are dungeon opportunities, not pantry stock** (GDD §4.8, §5.5): a stack of 1–3 parts of one of the Cellars' monster parts (slime gel, bat wing, spider leg; slime core and venom sac from floor 2 down), standard or fine (fine more often deeper), dropped as an ordinary part through the harvest system, so it competes for satchel space like any part. It doesn't replace harvesting: kills still give the parts, and clean kills their quality.
- **Gold rewards and run Gold:** a coin on the floor once the room is clear; walking over it adds its Gold to the run (`DelveRunController.Loot`, a `RunLoot`), shown under the Essence bar. It's unbanked: extracting brings it home (`DelveReport.GoldSecured`, banked by `DayRules.CompleteDelve`), dying loses it (`GoldLost`); Gold already banked is never at risk. The delve result says which ("+N Gold to the purse" or "N Gold left in the dark"). Parts and Gold left lying stay behind with the room.
- **Depth:** per-floor tuning in `RunSettings` (Gold 10–18, 18–30, 30–45; parts 1–2, 1–2, 2–3; fine chance 20%, 45%, 70%; Gold and ingredient rooms equally likely). Existing settings got these once, when their ingredient list was empty; later edits are kept.
- **Extension points:** a new reward is a new `RewardKind` with its payload in the same fields, a door sign, and a case in `RoomRunner.GrantReward`; non-satchel finds (furnishing discoveries, quest objects) join `RunLoot` and travel home in the `DelveReport`. Run power-ups (step 4), Delve Marks and weapons (4e), furnishing discoveries (4f) and quest objects (4g) plug in there without touching the graph, room clearing, the previews or the report.
- **Art:** the Miscellany Icons pack's gold coin (door sign, HUD, the pickup) and UI Overhaul's food icon (`docs/ASSET_MAP.md`); `PH_Coin` for picking up Gold.
- **Not changed:** `GameFlow` still runs the old day order; the day loop still delves in `Dungeon_TestFloor` (step 5). Run Gold reaching the purse works in `DayRules` already, for when it does.
- Tests: generator rewards across 300 seeds (every fight has one in its floor's range, quiet rooms none, ingredients only from their floor down, a choice of fights is a choice of rewards, deeper floors pay more, both kinds common); `DayRules` banking run Gold on extraction and not on death; `RunLoot`; in PlayMode, every door's sign matching its room, a Gold room's coin adding to the run and the HUD, an ingredient room leaving its parts, and run Gold secured by extraction and lost by death.

**Step 3 approved (2026-10-04).**

**Step 4: run powers (2026-10-04).** The experience: a power room asks what this run needs right now (more time, more safety, more killing power, or cleaner parts), and the answer changes how the rest of the delve plays.
- **Power rooms** are a third reward kind (`RewardKind.Power`, a lightning sign over the door), about one fight in five (`powerWeight` 0.6 per floor against Gold 1 and ingredients 1; tunable). A choice between two power rooms becomes a power or a sure thing, as with the other kinds.
- **The spark:** a cleared power room leaves a small glowing spark. Stepping onto it pauses the run and offers three powers the run hasn't taken, as cards (icon, name, what it does). Choosing one keeps it until the delve ends; Cancel backs out and leaves the spark to come back to (it stays behind with the room if the player leaves). The offer is drawn from the run's seed and the room, so a seed and the same choices replay it. Feedback: a rising shimmer with the success pulse (`PH_PowerUp`, `Pulse.Success`).
- **Eight powers, each a nudge to existing tuning** (`Data/Dungeon/Powers/Power_*.asset`, amounts editable): deep reserves (+25 max Essence, filled at once), slow burn (Essence drains 30% slower), thick hide (hits cost 30% less Essence), keen edge (light attacks +25% damage), heavy hand (charged attacks +40%), light feet (the dodge recovers 40% sooner), second wind (each room cleared restores 10 Essence), butcher's eye (overkill needs 50% more spare damage, so fewer bruised and destroyed parts). Keen edge and heavy hand make overkill easier; butcher's eye pulls the other way, so damage and clean harvests compete.
- **Rules:** each power once a run; bonuses add, reductions multiply, and no multiplier falls below 20% (`RunPowers`, pure, EditMode tests). Powers end with the delve: they aren't saved and don't change the delve result.
- **Where they apply:** Essence through the meter (`RunDrainMultiplier`, `HitCostMultiplier`, `RaiseMax`), damage as each swing starts (`CombatMeleeWeapon`), the dodge cooldown through `PlayerTuning`, overkill thresholds in `HarvestSystem`, second wind on `RoomCleared`. All through `DelveRunController.Powers`; no vendor code changed.
- **HUD:** the powers taken, as their icons in a row under the Essence bar.
- **Art:** True Heroes I & II 16×16 skill icons for the eight powers, UI Overhaul's lightning icon for the door sign and the spark (`docs/ASSET_MAP.md`).
- **Not changed:** `GameFlow` and the day loop (step 5).
- Tests: `RunPowersTests` (each effect, once a run, stacking and the floor, offers of three distinct untaken powers that follow the seed and cover the pool, the pool running out, the shown amounts, the meter's raised max and multipliers); generator (power rooms about one fight in five, nearly every run has one, none without powers); in PlayMode, the lightning sign, the choice of three with icons, taking one (paused, then resumed, held, on the HUD, the spark gone, deep reserves raising max Essence), backing out and coming back to the same three, and light feet, thick hide and second wind changing their tuning in play. The run capture now photographs each spark and choice.

**Step 4 approved (2026-10-04).** The run powers, reward choices, room flow and three-floor run felt good; the step 3/4 systems and tuning structure are kept.

**Step 5: the generated run in the day loop, in the new day order (2026-10-04).** The experience: the tavern is closed, now I'm heading below; and in the dungeon, "I could push another room, but I'm risking what I've already found."
- **The day order (GDD §3.1, v0.5):** Boot → main menu → **daytime** → **evening** (Prep → service → Results → close up) → **the night's delve** (the generated Cellars run) → delve result → **night** (summary, upgrades) → **sleep** → the next daytime. `DayPhase` is now Daytime, Evening, Delve, Night; `DayCycle` enforces the order (no delve before the evening). Saves name phases; a pre-v0.5 save's "Morning" loads as Daytime (`DayCycle.Parse`).
- **The daytime placeholder:** the old morning panel, evolved (`MorningScreen`, "daytime · day N"): the storeroom, tonight's delve meal, what tonight's delve starts with (upgrades and the meal), and **open for the evening**. No village systems. The free-roaming day replaces it later by calling `GameFlow.StartEvening()` when its day ends; nothing else in the flow changes.
- **The delve meal** (was breakfast): the same mechanics (one Grill or Tap dish a day, cooked at its station, quality scales the buff), cooked in the daytime and kept until tonight's delve, used up when the delve ends. Player-facing text says "delve meal" and "tonight's delve"; code identifiers were renamed to match.
- **Tavern → dungeon:** Results' button reads **close up and head below** (Prep's reads **stay shut tonight**); either banks the evening, saves, and fades to "closing time · the Cellars" and the generated run. No sleep before the dungeon.
- **The generated run replaces the test floor in the loop** (`GameScenes.Dungeon` = `Dungeon`). `Dungeon_TestFloor` stays a standalone combat bed; the LookTest scenes are untouched; Boot stays the entry point. Coming home goes to the tavern at Night.
- **What the delve receives:** max Essence and satchel upgrades and the delve meal, through the existing `DelveLoadout` (checked in play and on the web: a bought +20 Essence starts the next delve at 120/120). **Run powers** live on the delve's `DelveRunController`: they start empty every delve, end with it (death, extraction, completion or quitting), and are never saved.
- **Rewards in the real loop:** extraction moves the satchel into the storeroom once (quality and freshness kept) and banks the run's Gold; death applies the Lockbox and loses the run's Gold; banked Gold is never touched. The delve result says "+N Gold to the purse" or "N Gold left in the dark"; the night's summary now reads evening, delve, parts home, **banked today** (takings plus the delve's Gold), purse, Renown.
- **Saves (versioned JSON, `SaveSystem` only, no mid-run saving):** new game; **after the evening** (phase Delve: the takings banked, before the run); **as soon as the delve ends** (phase Night: the haul and the run's Gold applied once, then saved, so a reload can neither apply them again nor lose them); after each upgrade; and on sleeping. Quitting mid-delve and continuing starts the night's delve from the top: the abandoned run's satchel, Gold and powers are gone, and nothing was banked from it. The day's story (what was served, what the delve did) isn't saved: a resumed night shows only what it knows.
- **Debug:** F8/F9 still work only in development builds; in the dungeon F8 extracts. The `Dungeon` scene has no look-test overlay, so no key can send a day into a look-test scene. The seed line shows only in development builds. A **run log** (development builds, in the console) prints the time, room and Essence at each room, each fight's length, and the run's end, for tuning from real play.

**Essence tuning (step 5).** The mismatch was drain alone lasting ~200 s against a full run estimated at 6–9 minutes. Settled values (`EssenceConfig`, `Power_*.asset`), with the reasoning:
- **Base max Essence 100 (unchanged), drain 0.5 → 0.15/s.** Drain alone now lasts about 11 minutes. A competent full run (about 6–7 minutes, below) costs about 60 Essence in drain, leaving about 40 for hits: a slime hit costs 10, a spider's bite 14. So a full run at base progression is possible with clean play, two hits a floor puts the third floor out of reach (the floor 2 rope becomes the sensible choice), and a bad first floor makes an early extraction rational. Hits and route choices, not the clock, spend most of the budget. `damageMultiplier` stays 1, so hit costs read as the enemies' damage.
- **Slow burn 30% → 50%:** at the lower drain, 30% saved only about 12 Essence over a run.
- **Second wind 10 → 4 per cleared room:** at 10 it more than repaid the drain of every room (about 5 a room), making drain irrelevant.
- **Unchanged:** deep reserves (+25), thick hide (30%), keen edge (+25%), heavy hand (+40%), light feet (40%), butcher's eye (50%); upgrades (+20 max Essence a level); meals (+15 to +25 max Essence, or 20–30% slower drain, by quality). The drain meals and slow burn are worth less now that drain is lower; a playtest question.
- `EssenceTuningTests` guards the budget: drain alone must outlast a full run by 1.3–2.2×, leave 20–60 Essence for hits, base Essence ≤ 120, hits at ≥ 75% of damage, and no single power may cover a run's whole drain.

**Expected run length (estimates from the combat data, not yet measured in play).** Effective player damage about 15/s once approach and dodging are counted (the light combo deals 36 in 0.83 s); slimes 34 health, spiders 36, bats 14; floors bring 2–3, 3–4 and 4–5 enemies. A fight takes about 10–20 s on floor 1 and 25–35 s on floor 3; crossing a room and collecting its reward about 8–12 s; quiet rooms about 5 s; a route crosses 3–4 rooms a floor. So: **floor 2 at about 2–2.5 minutes, floor 3 at about 4–4.5, a full run about 6–7 minutes**. Essence for a clean run (about one hit a floor): about 70 on reaching floor 2, 40 on reaching floor 3, 10 at the end; at two hits a floor, about 60, 20 and out. The run log measures all of this in play; the first full-loop playtest should replace these estimates.

**Rewards and powers (first balance pass):** no values changed beyond the two powers above. Gold 10–18, 18–30, 30–45 by floor; ingredient rewards 1–2, 1–2, 2–3 parts, fine 20%, 45%, 70%, slime core and venom sac from floor 2; reward kinds Gold 1 : ingredient 1 : power 0.6 (about one fight in five offers a power; nearly every run sees one). Gold and ingredients stay comparable (a reward of 1–3 parts is 1–2 dishes, worth about what a Gold room pays on the same floor, more when fine); deeper floors pay roughly twice to three times floor 1. Nothing obviously broken found from the numbers; the playtest decides.

**Found and fixed in step 5:**
- (Web smoke) **Every room load logged an error per wall-tile sprite** ("Sprite outline generation failed"): a tilemap collider instantiated at runtime asks each tile for a physics shape, and the tileset had none, so the player tried to trace it from an unreadable texture. The Dungeon tileset now bakes its physics shape at import (`MinifantasyImportPostprocessor`, version 2); no textures were made readable.
- (Web smoke) **The main menu showed "day 3, #phase.morning":** the phase word was looked up before the web's string tables had loaded. It now waits for them.
- (Web smoke) **A night resumed from a mid-delve save showed "served 0"** for an evening played before the reload: the evening (and today's takings) now show only when this session played it.
- The daytime button and the delve-result line were reworded to fit ("open for the evening"; "no parts brought home." when Gold came home without parts).

**Tests (step 5):** day order, the old "Morning" name, staying shut, today's banked total, the delve meal's lifetime, save round trips in the new order, and the Essence budget (EditMode); in PlayMode, from Boot: a whole day in the new order (the meal, service, closing to the generated run with its caption, a power and run Gold, extraction, the haul and Gold once, the night and an upgrade, sleep, reload, Continue, the upgrade in the next delve and no powers), quitting mid-delve, upgrades reaching the next day, dying (the Lockbox, the run's Gold lost, banked Gold safe), staying shut, a resumed night, and no look-test keys in the loop's dungeon. **EditMode 424/424, PlayMode 126/126**, explicit captures passing.

**Web (development build, 129 MB):** smoke-tested in Chrome: the menu, new game over a 4c save, daytime, evening prep, staying shut, the caption, the generated run, the run log, F8 extraction, the delve result, night, buying an upgrade, sleep, day 2, reload and Continue (daytime and mid-delve), the upgrade reaching the delve (120/120), keyboard and mouse. No console errors or warnings beyond the template's missing `productVersion`. Not driven in the browser (synthetic input can't fight): combat, power choices, the death screen, the satchel swap and a controller. They're covered by PlayMode tests and left for your web playtest.

### 4d status at the end of step 5 (complete, approved 2026-10-05; see below)

All of 4d's criteria are met in the editor and, where the browser could drive it, on the web: generated three-floor Cellars runs with branching, rewards, door previews and powers; extraction from each floor, death with the Lockbox, the arena placeholder at the end; the run in the real loop in the tavern-before-dungeon order; rewards and run Gold reaching the save once; powers reset every delve. What's left is your judgement of the Essence tension and the run length in play.

**Deferred:** the Biome 1 boss, Delve Marks, more weapons, the Harvest Finisher and Kitchen Arts (4e; the arena's elite wave is a placeholder until then); furnishing discoveries and quest objects as rewards (4f, 4g; the extension points exist); the village day that replaces the daytime placeholder, and where upgrades are bought in a life-sim day (village milestones); whether the delve can be skipped and a ticking clock (GDD §13 Open 10).

**4d playtest (2026-10-04): the loop works.** Two changes from it:
- **The dodge faced front-right whatever its direction.** The Townsfolk's jump (the dodge) has one row in the pack, front-right, so every facing reused it. A dodge to the left now shows it mirrored (`SpriteAnim.mirrorForLeft`, set only on that animation); up and down dodges keep the front-facing hop, as there's no back-facing jump art. Tested in EditMode and in play.
- **Essence never pressed:** the arena was easy to reach with plenty left, so the run is shorter in play than the 6–7 minute estimate. **Drain 0.15 → 0.2/s**: drain alone now lasts about 8 minutes; a full run of about 6 minutes spends about 72 of 100 on drain, leaving about 28, two or three hits. Expect to reach the arena low, with a messy floor 2 making the rope tempting; upgrades (+20 a level) and the delve meal (+15 to +25) still buy real margin, and the drain-based meal and slow burn are worth a little more again. `EssenceTuningTests` now assumes a 6-minute run. If the run log's end-of-run lines (development builds, in the console) are shared after the next playtest, the next adjustment can come from measured times.

**4d playtest, second round (2026-10-05):**
- **Dodging while facing away turned the head to the camera** for the length of the hop: the Townsfolk's jump has only a front-facing row. A dodge facing away now shows the walk's back-facing frames instead (`SpriteAnim.walkForBack`, set only on that animation), still mirrored left and right as before; front-facing dodges keep the hop. Tested in EditMode and in play.
- **A new game starts with a delve into the Hollows** (`GameFlow.NewGame`: day 1 begins at the Delve phase, saved at once), because an empty first daytime and a shut first evening were dead time. The first caption reads "night · into the Hollows" (also used for a delve resumed from a save; after a played evening it stays "closing time · the Cellars"). Then the first night, sleep, and day 2 begins in the daytime with the first haul in the storeroom. The first night's summary shows only the delve, and the night summary now packs its known lines without gaps.
- **Essence drain ×2.5: 0.2 → 0.5/s.** The goal changed: a full run shouldn't be made without some gear. At base, drain alone lasts about 3⅓ minutes: the first floor (about 2 minutes) with room for a few hits, and some of floor 2, so the floor 1 or floor 2 rope is the base delver's way home. Fully upgraded (+60) with a good delve meal (+25), drain alone lasts about 6 minutes, roughly a full run, so the deep runs also need good powers and few hits. More headroom is expected from 4e's gear (Essence Tonics). With this drain, **slow burn 50% → 30%** (halving a 0.5 drain made it the only pick) and **second wind 4 → 8** per room (4 was a fraction of a room's drain). `EssenceTuningTests` now guards this intent: base can't cover a full run, the first floor is comfortable, full gear about covers a full run, and no single power covers more than half a run's drain.
- **Title and terminology (your rename commit):** verified in Unity: `TextStyleTests` passes with "Hearth & Hollows", and the title fits the main menu (capture checked).
- Tests: EditMode 427/427, PlayMode 127/127, explicit captures passing; the web build is rebuilt.

**4d status: complete, approved 2026-10-05.** The generated Cellars runs, rewards, powers, the tavern-before-delve day order, the Essence tuning and the web integration are the stable baseline. (Suites at sign-off: EditMode 427/427, PlayMode 127/127.)

### 4e plan (approved 2026-10-05)

The experience: delve through the Cellars, make reward and build choices, reach the arena, fight and defeat a real boss, take a satisfying reward home. A few strong, readable patterns rather than many mediocre ones; the boss is the priority, and combat depth is added only where Hearth & Hollows benefits.

**Decisions:**
1. **The boss: the Larder Troll** (Minifantasy's Ancient Troll: walk, a two-handed ground slam, damage, a 21-frame death, and an Eat animation). A troll that raided the tavern's stores; its signature mechanic is **eating the drops** to recover, the first boss mechanic built on the game's food and harvest identity (to be prototyped carefully in step 2). This supersedes the old plan reserving the Ancient Troll for Frostvault, whose boss is now TBD (GDD §4.6).
2. **Essence during the boss:** passive drain stops while the boss encounter is active (from the moment it becomes active until victory, the player's death or the encounter ending); hits still cost Essence; no restore on entry. A boss-encounter rule (`BossDefinition`), not a special case.
3. **Persistent boss state:** a generic first-clear record by stable boss id in the versioned save (version bump and migration), for story reactions, first-clear rewards and 4f's trophy.
4. **Harvest Finisher:** a small version for ordinary enemies (staggered and low: a short prompt window; the Finisher action, F or LT; a moment of vulnerability; Premium parts). On the boss only after lethal damage: an optional finishing moment that improves the harvest and the finishing feedback, never skipping boss health; ignoring it lets the death proceed.
5. **Weapons:** deferred until the protagonist body decision (4g); the cleaver is tuned for the boss instead.
6. **Roadmap cleanup:** weapon rarity and affixes removed; Delve Marks removed; relics removed for now; Kitchen Arts and field cooking deferred. "Delver's Board" may return as a diegetic village quest and request board. Recorded in the GDD (§4.2, §4.5, §4.6, §7.1, §7.2, §11.1) with the history kept.
7. **Essence Tonics:** deferred; revisited only if the pre-boss delve still needs a recovery tool after the boss playtest.
8. **Boss reward (4e):** substantial Gold, a guaranteed Premium larder cache of existing valuable Cellars ingredients (the floor 2 and 3 pool), the first-clear record, and a clean hook for 4f's unique troll trophy. No unusable boss-only ingredient.

**Steps (each stops for a playtest):**
1. **Foundation and first patterns:** the troll's art and data, the boss encounter (activation, a short entrance, the drain rule, its health bar), the ground slam and the lumbering charge (a pillar or wall stuns it: the punish window), ground markers for both telegraphs, the arena hosting the troll instead of the mixed wave, and a development toggle to start a run in the arena.
2. **Eat-the-drops, escalation, defeat:** the troll eats parts on the floor to recover (the player can deny it), an enrage or phase change, the remaining patterns, the full entrance and defeat presentation.
3. **The Harvest Finisher:** ordinary enemies, and the boss's optional finishing moment.
4. **Rewards and feel:** the Gold and larder cache, the first-clear record (save version 3), the 4f hook, the boss feedback and haptics pass, cleaver and enemy tuning where full runs show a need.
5. **Closeout:** full-run balance, web, tests, captures, docs.

### 4e status

**Step 1: the Larder Troll's foundation and first patterns (2026-10-05).** The experience: walking into the arena and finding something big at its meal; then a lumbering fight with two patterns you learn to read, step out of and punish.
- **The troll** (Minifantasy's Ancient Troll, about 15×16 pixels): idle, walk, a two-handed ground slam, damage, a 21-frame death, and eat. Idle has only front rows; its back facings use the walk's first frame. Enemy data `Enemy_LarderTroll` (500 health; no knockback, no stagger from hits: its opening is earned), boss data `Boss_LarderTroll` (stable id `larder_troll`).
- **The encounter** (`BossEncounter`, any boss): the troll waits **eating** until the player is in the sealed arena, then a 1.6 s entrance (a roar, a shake, the `Boss.Telegraph` rumble) names it on a **boss bar** across the top, then it fights. From the entrance until victory, the player's death or the room going away, the boss's drain rule applies (`BossDefinition.drainMultiplierWhileActive`, 0 for the troll: **passive drain paused**, hits still cost). It's a separate multiplier on the meter, so the transitions' own pause can't undo it. No restore on entry.
- **Ground slam** (in range): arms up over 0.95 s while a red **mark on the floor** fills in where it will land (3 × 2.4 tiles in front), then both fists down (18 Essence in the mark, a thud and a shake), then a 1 s recovery to punish.
- **Lumbering charge** (at 4–16 tiles, with a clear line): 0.9 s wind-up with a red **line on the floor** to the first wall or pillar in its way, then a 13-tile run (16 Essence if it catches you). If a pillar or wall stops it short, it **stands dazed for 2.4 s** (its hurt pose; a heavier thud, a bigger shake, a hard bump): the window for a full charged heavy (`ChargeStun`, `ChargeRules.IsBlocked`).
- **The arena hosts the troll** instead of 4d's mixed wave (`RunTuning.boss`; settings without a boss keep the stand-in). It stands at the arena's far end (`RoomInstance.BossPoint`). The rope still appears when it falls.
- **Practising the boss:** tick **Start In Arena** on the `RoomRunner` in `Dungeon.unity` (development builds only; ignored in the day loop) and press Play.
- Placeholder sounds `PH_TrollRoar`, `PH_TrollSlam`, `PH_TrollThud`. Not yet: eating the drops, the enrage, the full entrance and defeat presentation (step 2); the finisher (step 3); the reward and the first-clear record (step 4). The troll drops nothing yet.
- Tests: EditMode (the charge's blocked rule, the encounter's drain multiplier, the troll's data and prefab, the arena's encounter across 50 seeds, the stand-in without a boss); PlayMode (found eating, then the entrance with the bar and no acting, then the fight; drain paused while it fights, hits still cost, drain back after; the slam's mark and its cost; the charge's line, the wall, the stun and the recovery; a full run ending at the troll). **EditMode 431/431, PlayMode 131/131**; the troll capture (`LarderTrollCaptures`) and the run capture pass.

**Step 1 playtest (2026-10-05): "plays great, telegraphs read well, but much too easy."** Retuned a lot harder (`Enemy_LarderTroll`):
- health 500 → **900**; walk speed 2.4 → **3.8** tiles/s (the player walks at 6, so outpacing it is no longer free);
- **slam:** reach 2.4 → 3, telegraph 0.95 → **0.7 s**, recovery 1 → **0.55 s**, cooldown 1.2 → **0.5 s**, damage 18 → **26**, area 3 × 2.4 → **3.6 × 3** tiles;
- **charge:** from 3.5 tiles (was 4) out to 18, telegraph 0.9 → **0.6 s**, run 13 → **16** tiles (about 10 tiles/s), recovery 0.7 → **0.4 s**, cooldown 2.2 → **1 s**, damage 16 → **24**, a wider body (2 × 1.6), and the daze after a wall 2.4 → **1.6 s**.
Two or three hits now cost most of an arriving delver's Essence, the openings are short enough to need timing, and the daze still fits one full charged heavy. All values stay in the asset for further tuning.

**Step 1 approved (2026-10-05)** with the harder tuning. Parts grabbed during the fight are ordinary loot (the usual death rule applies; decided 2026-10-05).

**Step 2: eating the drops, the frenzy, the entrance and the defeat (2026-10-05).** The experience: the troll's food is the fight's tug of war. Every part on the floor is both loot for you and a meal for it.
- **Its stolen larder:** a slam shakes **2 parts** loose (at most every 14 s, the first sooner; never while 4 lie on the floor): slime gel, spider leg or bat wing, standard quality, popping onto open floor (`LarderScraps`). They're ordinary parts: walk over one and it's in your satchel, to take home like any other.
- **Its appetite** (`ScrapEater`, through its TDE brain: Chase → **Feed** (walk to the nearest part) → **Eat** → Chase; the slam still answers a player in reach before it goes for food, the charge comes after): eating takes **1.5 s** (its Eat animation, a gulp and a flash) and heals **8% of its health** (72) per part, with 2.5 s before the next. Take the part first and the meal is denied; deal **40 damage** while it eats and the meal is **spoiled** (the part is ruined, nothing healed, a squelch and a firm tap). Pure rules: `FeedingRules`.
- **The frenzy** at half health (`BossFrenzy`): it roars for 1.2 s (it can't be hurt and doesn't act; a big shake, the `Boss.PhaseChange` rumble; any meal is dropped), turns a hot tint, the bar goes orange, and then it's **20% faster**, its **slams come in pairs** (the cooldown is skipped once; the lockout still gives a beat), and each **charge spills a part** where it started. A roar that begins during a daze keeps it still until the roar ends.
- **The entrance:** the camera cuts to the troll at its meal while it's named, and the player waits; both hand back when it fights.
- **The defeat:** slow motion (0.3× for 0.8 s), the fall (21 frames), a big shake, a heavy rumble, and the bar reads "the Larder Troll falls" for 3 s. The rope appears as before.
- All of it is tunable on `Boss_LarderTroll` (feeding and frenzy sections). Placeholder sounds `PH_TrollGulp`, `PH_TrollSpoil`, `PH_TrollFall`.
- **Fixed:** the boss bar's text never changed in step 1, because its field was named `m_Name` and collided with Unity's own serialized object name; it's `m_Label` now (the name only showed because it was the builder's default text).
- Tests: the feeding rules and cooldown skip (EditMode); in PlayMode, the entrance's camera and held input, a part eaten and healing, a part taken first (into the satchel, no meal), a meal spoiled by a hard hit, the frenzy's roar (invulnerable, still) and speed, and the defeat caption. **EditMode 434/434, PlayMode 136/136**; the troll capture covers the meal, the frenzy and the fall.

**Step 3: the Harvest Finisher (2026-10-05).** The experience: mid-fight, a moment to choose the cook's cut over one more swing. Combat → monsters → ingredients, without busywork.
- **When:** an ordinary enemy you've **just hit** (within 1.2 s) that's **low** (at most 35% of its health, or 20) shows a **drumstick** over its head (`FinisherTarget`, pure `FinisherRules`). Most enemies offer it once, partway through a combo; it's never required: ordinary kills still harvest as before.
- **How:** **Finisher** (F, left trigger) within 1.8 tiles (`PlayerFinisher`). You're committed for **0.5 s**: no moving, attacking or dodging, and no i-frames (the risk). The blow lands at 0.2 s with its own moment: a freeze, a shake, a heavy chop and ring (`PH_Finisher`), and the `Finisher.Harvest` rumble. The kill is a finisher for the harvest: **Premium parts**, never an overkill (the existing `HarvestRules` path).
- **The troll:** the hit that would kill it **brings it down** instead (at 1 health, `BossHealth`, a TDE `Health` subclass) for **3 s**: it can't be hurt, its drumstick shows, and the fight (and the paused drain) goes on. **Finish** it in reach for the finisher's harvest and its moment on top of the defeat; or leave it and it falls as it would have. It can't be finished before lethal damage, so no boss health is skipped. Its own harvest: a slime core and a venom sac, Fine (the cleaver's clean kill lifts the venom sac anyway), **Premium to the finisher**. The larder cache reward is step 4.
- Tunable in `HarvestRulesConfig` (finisher section). The prompt is unlit, like the "!" alert, so it reads in the dark.
- Tests: the rules (EditMode); finishing a low, freshly hit slime (committed, then free; Premium parts), the moment passing and a healthy enemy never eligible, the troll brought down then finished (no early execution, can't be hurt while down, Premium), and left down to fall with its ordinary harvest. **EditMode 435/435, PlayMode 140/140.**

**Step 4: the troll's reward, the first-clear record, feel and tuning (2026-10-05).**
- **The reward, where it falls** (`BossDefinition` reward section, granted by `RoomRunner` on its defeat): **120 run Gold** (unbanked until the delve ends well, like any other) and a **Premium larder cache**: 2 slime cores and 2 venom sacs (the deeper Cellars' pool), as ordinary parts for the satchel. Plus its own harvest (step 3) and the rope out. No boss-only ingredient (decision 8).
- **The first-clear record** (decision 3): `GameState.BossClears` (stable boss id → times defeated), recorded from the delve report (`DelveReport.BossesDefeated`, gathered in `RunLoot`) whatever the delve's end: a victory is a victory, even if the player dies on the way out (the haul still follows the death rule). Saved as **version 3** (`SaveData.bosses`); version 2 saves migrate with no bosses. On a boss's **first** defeat `GameFlow` publishes **`BossFirstCleared(id)`**: the hook for first-clear rewards, story reactions and **4f's trophy**, whose id goes in `BossDefinition.trophyId` (empty until 4f).
- **Feedback pass** (named patterns only; the web no-ops): entrance `Boss.Telegraph`; its telegraphs, when aimed at a close player, now the `Boss.Telegraph` rumble (scaled 0.6) instead of an ordinary enemy's cue; slam `Bump.Soft`; wall `Bump.Hard`; frenzy `Boss.PhaseChange`; spoiled meal `Tap.Firm`; finisher `Finisher.Harvest` with a 0.14 s freeze; defeat `Hit.Heavy` with slow motion; hits on the player `Hit.Taken` as ever.
- **Cleaver tuned for the boss:** the charged heavy's middle and full stages 22 → **26** and 34 → **40** damage, so a full charge in the troll's daze is a real payoff (it also makes overkill likelier on small enemies: the harvest tension stays). The Cellars' slime, bat and spider are unchanged: no playtest showed a need.
- Tests: boss clears recorded by id and count through death and extraction and the save; a version 2 save migrating; the troll's Gold and Premium cache where it falls and its defeat on the run's record; and in the real day loop, defeating it recorded once, announced once, kept across a reload. **EditMode 437/437**; PlayMode below.

**Step 5: closeout (2026-10-05).** Built without stopping, at your request: steps 1 (as approved) through 5, for one combined playtest.
- **HUD:** the harvest feed now starts below the boss bar (they overlapped at the top right); both dungeon scenes' HUDs rebuilt.
- **Tests fixed for step 4's changes:** the charged heavy now kills a small enemy outright (the test expects the smaller of the step's damage and its health); the troll's single harvested core is told apart from the cache's Premium pair; a meal's part is checked a frame after it's eaten (destroyed at the end of the frame).
- **Suites: EditMode 437/437, PlayMode 141/141** (plus the explicit captures: the troll's nine moments, the full run ending at the troll, the day screens, the evening).
- **Web (development build, 129 MB):** the title, Continue from a **version 2 save** (migrated: day 2 night, purse and upgrade kept), sleep, daytime, evening, staying shut, the delve (the upgrade's 120/120 Essence), extraction and Night, with **no console errors**. Not driven in the browser: the troll itself (combat can't be played with synthetic input, and Start In Arena is editor/development-scene only), the finisher and the meals; PlayMode covers them.
- **Expected boss fight (estimate, not measured):** 900 health at about 15 effective damage a second, plus charged heavies (40) in its dazes and finishes; each meal it gets heals 72. Roughly **2–3 minutes** for a player who denies most meals, more if it eats well. Passive drain is paused throughout, so only hits cost Essence: two or three of its 24–26 hits end most arriving delvers.

### 4e status: built, awaiting your playtest and sign-off

**What 4e delivers:** the Larder Troll at the end of the Cellars (two telegraphed patterns, a wall stun, eating the drops, a frenzy, an entrance, being brought down, a finishing moment and a defeat), the Harvest Finisher across the Cellars, the troll's Gold and Premium larder cache, the first-clear record in the save (version 3) with its `BossFirstCleared` hook for 4f's trophy, and the cleaver tuned for the fight.

**Known issues and notes:**
- No measured fight or run times yet (estimates above); the run log reports each room, and the troll's fight shows as one long room.
- The troll eats **any** part on the floor, including parts the player swapped out of a full satchel: intended (the floor is its larder), but say if it feels unfair.
- Its roar and its daze both use its damage pose (no roar art); the frenzy reads mainly through the tint, the orange bar and the shake.
- The Night summary and the delve result don't mention the boss; the bar's "the Larder Troll falls" is the only caption.
- The charged heavy's buff (40 at full) also makes overkill likelier on slimes, bats and spiders.
- The **Start In Arena** toggle only works in the editor/development scene; in the day loop the troll is reached by a full run.

**Deferred (as planned):** more weapons (4g's protagonist decision), Kitchen Arts, field cooking, Essence Tonics (to revisit after this playtest), the troll's trophy furnishing and any signature ingredient and recipe (4f).

**Your 4e playtest (2026-10-05): the balance "feels right: challenging, might fail if you're not careful, designed to help the player understand upgrades are needed".** Fixed and added:
- **The troll stuck at a pillar** (seed 459145174): a part lying where its big body can't reach (against a pillar) had it grinding and turning on the spot. It now gives up on a part it hasn't got closer to in 2 s (`FeedingSettings.giveUpSeconds`, `progressStep`) and never chases that part again.
- **The dodge rolls through enemies** (`DodgeThroughEnemies`): while rolling the player's body ignores enemy bodies, and stays ghosted until clear of them, so neither is shoved apart.
- **Dodge mashing skipped the cooldown** (web): TDE's cooldown lets a new dash cut its refill short by default; `PlayerTuning` turns that off.
- **A campfire before the boss** (`Campfire`, `RunTuning.campfire`): lit in the room that leads to the arena once it's clear; standing by it gives back **half your max Essence** over 2.5 s (only what's missing; the rest waits), then it burns low. Resting commits nothing: the rope is in the same choice. Art: Adventurer's Campsite big campfire, with a warm light; PH sounds.
- **The boss's fall fills your Essence** (`BossDefinition.essenceOnDefeat`, 1).
- **Aim and facing** (changes a locked decision, at your request; CLAUDE.md updated): the mouse aims on keyboard and mouse with the **system cursor visible** (TDE hid it for a reticle we don't draw, so it seemed to vanish), the **right stick** aims on a gamepad (new `Dungeon/AimStick` action; movement direction while centred), and the delver **faces where they aim**, except while rolling. Attacks go that way, as before.
- **The stray stool** right of the bar is gone from the tavern (builder and in-place updater).
- Tests: the give-up and campfire rules; the troll giving up on a part past the wall; the roll through a slime; mashing dodge; facing the mouse, walking or standing; the roll facing its way; the right stick aiming and turning, centred following the walk; the refill on victory; the campfire's room and its gift. **EditMode 439/439, PlayMode 149/149.**
- **Web:** rebuilt and smoke-tested (Continue, sleep, evening, staying shut, the delve; the cursor visible over the canvas, no pointer lock; no console errors). The first rebuild crashed at load with "Maximum call stack size exceeded" inside IL2CPP's type loading: a stale incremental WebGL cache after switching build targets. Clearing `Library/Bee/artifacts/WebGL`, `WebGL_CodeGen`, `UnityLinkerInputs` and `Library/PlayerDataCache` and rebuilding fixed it; no code change was needed.

**Second round (2026-10-05):**
- **The tap could never pour clean on a keyboard.** The glass tilted toward the stick's position, so W/S could only hold it upright, level or fully tilted, and the tilt that puts the head in the band (about a third of the way down) was out of reach: the best keyboard pour scored 0.8 against the 0.85 a clean pour needs. The tilt now moves while W/S or the stick is pushed and **stays where it's left** (`tiltSpeed` 1 per second); a short tap of S finds the band. The fill window is a little wider (`fillTolerance` 0.03 → 0.04, about ±0.25 s), and the prompt reads "pour · tilt, head in the band". An untouched level glass still pours a passable (not clean) drink.
- **A smaller campfire by each floor's hole down** (`RunTuning.floorCampfire`): a quarter of max Essence over 1.5 s, in the descent room, beside the arrival and clear of the hole. Only delvers going deeper pass it.
- Web rebuilt and loaded. A browser that played an earlier build can load stale string tables (CRC mismatch, `#menu.title` on screen), because Localization's bundle names don't change between builds; the local test server now sends no-cache headers. Elsewhere, a hard refresh (Ctrl+Shift+R) fixes it.
- Tests: EditMode 441/441, PlayMode 150/150.

- **The keeper looks around the tavern too** (your request, for immersion): `PlayerLook` turns them toward the mouse, or along the right stick (new `Tavern/LookStick` action), standing or walking; with the stick centred they face their walk. It stays out of the way at stations, in minigames and in menus (only while the tavern's gameplay map is on). Presentation only: which station you use doesn't depend on facing. Tests: the stick and the mouse, and the mouse ignored at a station. EditMode 441/441, PlayMode 152/152.

### 4e sign-off (2026-10-05): approved, with final adjustments

- **Campfires:** a quarter of max Essence both by each floor's hole down and before the arena (was a half there). Each has a **prompt** as you come near ("stand by the fire to recover Essence", then "the fire warms you: Essence returning" while it gives), gone once it burns low (`CampfireHint`, `CampfireHintView`).
- **The boss's fall** still refills Essence completely.
- **The troll's meals:** it may eat any ingredient lying in the arena, swapped-out satchel parts included; 8% healing (unchanged).
- **Harvest Finisher:** ordinary enemies at about **25% health or 15 or less** (was 35% or 20), still within 1.2 s of a hit and 1.8 tiles, same feedback. The troll's optional 3 s window is unchanged.
- **Entrance:** the full ~1.6 s reveal until the troll has first been beaten (the save's boss-clear record), then a **~0.85 s** intro (`BossDefinition.repeatEntranceSeconds`).
- **Frenzy cues besides colour** (checked, then strengthened): it already had the roar, a strong shake, the phase-change rumble and a held pose while it can't act; it now also **swells up** as it roars (+22%), takes a beat of **hit-stop**, and the bar reads "**the Larder Troll rages!**" for two seconds.
- **The delve result** says "you felled the Larder Troll" when it fell on that delve (whatever the delve's end, as in the save). The Night screen is unchanged (no duplicate).
- **Unchanged by decision:** the cleaver's heavy 26 / 40; independent movement and aim; the 0.5 s dodge cooldown.
- **Naming:** "the Larder Troll" is canonical (CLAUDE.md's proper nouns).
- **Renown:** gets its **first tangible gameplay use in 4f** (ideally a small tavern or customer-content unlock); 4g may also use it in story and dialogue conditions. Not built in 4e.
- Tests: the new finisher thresholds, the entrance's length by the clear record, the final campfire, refill and feeding values, the first meeting's full reveal, the frenzy's caption and swell, the campfire prompt (near, warming, gone when spent) and the result's boss line. **EditMode 443/443, PlayMode 153/153**, and all four captures pass (the troll, the full run, the day screens, the evening).

- **Web:** rebuilt (clean) and smoke-tested: the menu, Continue and a delve, with no console errors. A browser that cached an earlier build's string-table bundles before the local server sent no-cache headers can still show raw keys once; a hard refresh (Ctrl+Shift+R) clears it. (Unity's `Caching` API, which could have cleared bundles at startup, doesn't exist on the web in Unity 6.)

**4e is complete.**

### 4f plan (approved 2026-10-05)

The full plan, its catalogue survey and the 23 locked decisions are in **`docs/PLAN_4F.md`** (§22). In short: furniture and layouts as data across property areas, a cursor-based Decorate Mode, owned copies and property-wide storage, movable stations (the Grill wall-bound), validation that blocks only impossible service, explicit rotation modes (`None`, `AuthoredFacings`, `QuarterTurnSprite`), palette-channel recolouring baked into cached textures, a guest room on the same architecture, furnishing discoveries (kept on extraction, lost on death) and the Larder Troll's tusks (granted when it falls, never lost), Renown unlocking catalogue tiers, a temporary Brackenford market for surface staples, the Butcher Block as mise en place, in-evening customer requests, Pip and **Gunta Ashbelly** (renamed from Gundra, id `gunta`). The seat upgrade is retired and refunded.

**Checkpoints:** A (steps 1–2: furniture as data, Decorate Mode), B (steps 3–6: catalogue, buying and Renown tiers, recolouring, guest room), C (steps 7–9: discoveries and the trophy, market and recipes, Butcher Block, Gunta and Pip), D (step 10: requests, integration, sign-off). Within a checkpoint, work continues from step to step while tests pass and the work stays inside the plan; it stops early only for a change to an expensive decision, a major architectural problem, a material scope increase, a regression in saves or gameplay, or a blocking art or UX judgment.

### 4f step 1: furniture as data (2026-10-05, done; Checkpoint A continues with step 2)

The tavern's furniture is no longer baked into `Tavern.unity`: it's data, built as the scene loads.

- **Definitions** (`FurnitureDefinition`, `Hearthdelve.Shared.Customization`): each piece's facings with, in footprint space, the whole-tile **footprint** (D1), pixel-exact **bodies** (what the walkable grid bakes, as in 4e), art layers (with their own sorting, so the kitchen keeps its shadow on the ground), seats, use points, staff post, highlight, surface anchors, lights, display slots and status point. **Rotation modes (D2):** `None`, `AuthoredFacings` (the tavern chair uses Minifantasy's four drawn facings) and `QuarterTurnSprite` (real 0/90/180/270° rotation). One pure transform (`FurnitureGeometry`) turns and mirrors footprint, bodies, seats, use points, staff post, surfaces, lights, highlight and art together; a turned or mirrored piece sorts as one at the bottom of what it covers. Flipping is opt-in per piece (D3).
- **Placements** (`PlacedFurniture`): definition, footprint cell, quarter turns, mirror, and a **nudge** of up to half a tile in art pixels (the starting layout uses it to keep 4e's exact positions; whole-tile snapping applies when pieces are moved).
- **Ownership** (`FurnitureState` in `GameState`): owned copies per piece (D7), each area's layout, storage = owned − placed (D12).
- **The scene:** `Areas/Tavern` holds the area (`PropertyArea`: floor, wall band, the entrance kept clear) and its builder (`AreaFurniture`, runs before the director), which builds every piece, gives the service its seats (chairs facing a table, in placement order) and staff posts, gives the keeper the stations and seats, and invalidates the walkable grid. `FurnitureContent` generates the 13 definitions of the 4e room, the starting layout and the presentation asset (*Hearthdelve → Generate → Furniture (4f)*). `UpdateTavern` removed the old scene furniture, seat markers and the four furniture lights in place (D23) and adds the area once; run twice, it produces the same objects with the same counts (only Unity's file ids of the objects it has always recreated, the service and UI, change).
- **The seat upgrade is retired (D16).** Seating is the placed chairs that face a table. The starting layout is what a new 4e game showed: three tables, six seats (the fourth table, which only the upgrade brought out, waits for the catalogue). `Upgrade_TavernSeats` is gone from the Night screen and the project.
- **Save version 4:** furniture (owned counts, each area's pieces). Migrations now run one version at a time (1 → 2 → 3 → 4). Saves from before furniture get the starting furniture as they load, and any seat-upgrade levels are refunded (120 and 220 Gold).
- **The 4e room, checked:** a PlayMode snapshot of the running tavern (every sprite, body, use point, highlight, seat, post, queue spot, light, animation loop, station view and the walkable grid) was recorded from the 4e scene before the change (`Tests/PlayMode/Baselines/Tavern4e.txt`); the new tavern matches it line for line, apart from the fourth table's two seats that a new 4e game kept hidden (`Tavern4e_starting.txt`). A 320×180 render of the empty room is pixel-identical to 4e's.
- **Fixed on the way:** three PlayMode tests still expected "Gold" after gold went lower case (I'd only run EditMode for that change).
- Tests: EditMode 463/463 (new: the transform, rotation modes, a quarter-turned piece's whole geometry, flipping, nudges, seats facing tables, ownership and storage, the v4 save, v3 and v2 saves migrating with refunds, real old saves loading through the game database, the starting layout's validity); PlayMode 156/156 (new: the 4e room, each piece built once, a rebuilt layout replacing the pieces with the grid and seats following). The flaky Pip test (4d) passed in every run.
- **Not in step 1:** Decorate Mode (step 2). In the editor's Scene view the tavern now shows only its shell; the furniture appears in Play (the capture tools build it).

### 4f step 2: Decorate Mode (2026-10-05, done; Checkpoint A ready for your playtest)

**How to decorate:** the **decorate** button on the daytime panel or the Night screen (and F7 in the editor or a development build, which also works in the tavern played on its own). Never during service.

| | Keyboard and mouse | Gamepad |
|---|---|---|
| Move the cursor | WASD or arrows (held: repeats); or point with the mouse | Left stick or d-pad |
| Pick up / put down | E, Enter or Space; or click | A |
| Turn (D2: next drawn facing, or a real quarter turn) | R; or the mouse wheel while carrying | X |
| Flip (D3: only pieces that allow it) | F | Y |
| Put away in storage | Delete or Backspace | RT |
| Undo | Z | LT |
| The next piece under the cursor | Q | RB |
| Free placement, to the pixel (hold) | Shift | LB |
| The catalogue (storage, buying, walls and floors; Checkpoint B) | Tab | Start |
| Colours (the piece carried or under the cursor; Checkpoint B) | V | Right stick press |
| The other room (the tavern, the guest room; Checkpoint B) | G | Left stick press |
| The layout check (problems, put it all back, done) | C | Select |
| Put the carried piece back / done | Esc or right-click | B |

- **The room stays in view.** A strip at the top says what you're doing and whether the doors could open ("ready for service", "1 problem", "ready, with 2 to look at"); a line names the piece under the cursor ("round table · 1 more here") or the one carried, and why it won't go where it's held ("cellar barrel can't go there: something's already there"); the controls for the device in use sit at the bottom (from the actual bindings). The carried piece's ghost shows where it would stand, over green or red tiles; gold corners mark the hovered piece.
- **Placement rules** (`FurnitureLayout`, pure): standing pieces stay on the floor, off the entrance and its inside tile, and share neither footprint cells nor bodies; rugs (none yet) go under anything but not on each other; wall pieces hang on the back wall's band; the kitchen range stands against the back wall (D6); surface items need a surface (the rule exists; the first surface pieces come with the catalogue). Standing pieces move on whole tiles (D1); putting one back restores its exact place.
- **The layout check** (`LayoutCheck`, pure; D13): blocking, so the doors stay shut, when a Grill, Tap, Stew Pot or the pass is stored or can't be reached, or no seat can be: "there's no grill out", "the stew pot can't be reached", "no chair faces a table". Warnings, with the doors still open: seats nobody can reach (they go unused), staff unable to reach their work, furniture where customers queue. Its walkable grid is built from the bodies by the walkable grid's own rule and matches the 4e room's baked grid row for row, and the live grid after rearranging cell for cell (tests).
- **Prep:** when the layout keeps the doors shut, the banner says why ("the doors can't open: there's no grill out") and offers **decorate** right there, so a stored Grill never costs an evening.
- **Storage** is property-wide (D12): removing never destroys; the storage panel lists what's owned and not placed, and choosing one puts it on the cursor.
- **Leaving** keeps the layout and saves in the day loop; the room, the walkable grid, the seats and the staff posts are rebuilt. Continue brings it back (tested).
- **Feedback** (placeholder sounds, `PH_Furniture*`; named haptics): a light lift, a firm settle when a piece goes down, a click for a turn, a buzz for "it won't go there", a thunk to storage. Restrained: rearranging is many small actions.
- **A design detail settled within D1:** facing east or west, the tavern chair now sits a quarter tile back from its table and a quarter tile up (as 4e placed them). With the chair centred, whole-tile snapping couldn't put chair, round table and chair in neighbouring cells (their bodies overlapped by a pixel), so a table set could never be rebuilt after moving it. Now it rebuilds with 4e's spacing, and the starting layout needs no nudges except the third table's group, which 4e set half a tile off the grid. The room is unchanged (the 4e snapshot still matches).
- **Input:** a fifth action map, **Decorate** (`CLAUDE.md` updated); like Minigame and UI, it doesn't go through TDE.
- Tests: EditMode 475/475 (new: placement rules piece by piece, bodies overlapping across footprints, the wall and back-wall rules, rotation modes in placement, the check's grid against the 4e baked grid, a stored station, a walled-off station, no seats, warnings that don't shut the doors); PlayMode 164/164 (163 in the full run, and the day-loop save test, added after it, run with its class; new: entering and leaving with the controls handed over, the keyboard picking up, turning, carrying and putting down, a bad spot refused with its reason and put back exactly, the gamepad including undo, the mouse pointing and clicking, the doors shut without a Grill and Prep's decorate button fixing it, six customers seated in a rearranged tavern with the check's grid equal to the baked grid, decorating at night saved and back on Continue). Explicit captures: `DecorateCaptures` (`BatchLogs/decorate_*.png`).

**Deviations from the plan, and things to know:**
- **Queue spots** stay the authored lane inside the door rather than dynamic: furniture on them is a warning ("furniture stands where customers queue"). Simpler, and the check catches it.
- **Surface items and decor nudging** (D1's quarter-tile steps, D4's surface layer) are in the data and rules but have no pieces yet: they arrive with the catalogue (Checkpoint B), where they'll get their controls.
- **In the Scene view** (edit mode) the tavern shows its shell only; the furniture is built in Play.
- **At 1× in the captures**, two of the controls line's single-pixel "·" separators fall between pixels; in the browser at its normal size they all draw.

**Web (development build, 130 MB), smoke-tested in Chrome:** a 4e save already in the browser (version 3, day 5, mid-delve) continued into the delve and came home through the migration with the starting tavern (three tables, six seats; the seat upgrade gone from the Night screen); Decorate Mode from the Night screen, a barrel picked up and put down with the mouse, its ghost following the pointer, "game saved." on leaving; a page reload and Continue brought the barrel back where it was put. No console errors. Not driven on the web: the gamepad (covered by PlayMode tests).

**Your Checkpoint A playtest (2026-10-05): "decorating works awesome"; the rumble feels great.** Fixed and changed:
- **Grabbing felt off-centre, especially chairs.** The cursor picked by tile, and the highlight framed the tile, while a chair is drawn a quarter tile off its tile. Now the mouse picks by what's drawn under it (`FurnitureLayout.AtPoint`; a smaller piece wins over a larger one in the same layer, so a chair against a table's edge is picked before the table), the gold corners frame the drawing (`FurnitureGeometry.ArtBounds`), and a carried piece follows the mouse from the point it was grabbed. Keys and the d-pad still move a tile at a time.
- **The glasses and their shelf couldn't be put back together.** The row of glasses is now a **surface item** (D4) on the low shelf's surface anchor: it moves, turns and goes to storage with the shelf, and when carried on its own it snaps to the nearest free table or shelf surface within a tile ("it needs a table or a shelf" otherwise). Surface items are picked before what they stand on.
- **The prompts flickered between controller and keyboard.** Devices report idle updates every frame (a mouse's zero delta, a pad's stick noise); the prompts now switch only on a real press or movement.
- **A barrel couldn't go back beside the others.** The 4e barrels stood two pixels off their tiles, so a barrel snapped to a whole tile overlapped its neighbour. The barrel is now drawn a quarter tile west of its tile's middle (where 4e's stood), so barrels on neighbouring tiles touch, as they did, and the starting layout needs no nudges. For the same reason **the third table and its chairs, which 4e set half a tile off the grid, now stand on it (4 px east)**: the only change to the starting room (`Tests/PlayMode/Baselines/TavernStarting.txt`; `Tavern4e.txt` is kept as the 4e record).
- **Free placement (your request, amending D1):** snapping by default; **hold Shift (keyboard) or LB (gamepad)** to place to the pixel: the piece follows the mouse exactly, and the arrows or d-pad move it a pixel at a time. Stored as the cell plus a nudge of under half a tile, so the rules, the walkable grid and saves are unchanged. "Next piece under the cursor" is now RB (and Q) only.
- **Save version 5:** keeps your playtest saves exactly as they were: each saved barrel gains the 2 pixels back in its nudge, and a saved row of glasses goes onto the shelf it stood on (otherwise to storage).
- **The charged attack always faced one way** (not 4f, but the locked aim rule): Minifantasy's charged-attack sheet is drawn only facing front-right (its rows are the wind-up, the charged loop and the spin). Facing already followed the mouse or stick while charging; the drawing didn't. The three stages are now mirrored for left-facing aims, as the dodge is, so the charge turns with the aim while held (back facings keep the front pose). The heavy itself is a spin with a round hit area, so it hits all round whichever way you face.
- Tests: EditMode 479/479 (new: barrels side by side, the glasses riding on their shelf and needing a surface, picking by what's drawn, the version 4 save keeping barrels and glasses); PlayMode 167/167 (new: the mouse picking a chair by its drawing and free mode placing to the pixel, the glasses riding on their shelf, the charge turning with the mouse). The web build wasn't rebuilt for these fixes; it will be at the next checkpoint.

**Second look (2026-10-05):** the green tiles under a carried piece looked offset, most of all on chairs: they marked the floor tile, and a chair is drawn a quarter tile off its tile. A carried piece now shows green or red **corners round its drawing** while snapping; in **free mode there's no outline**: the piece itself turns red where it can't go (with the reason on the line at the top, and the buzz if you try). Also fixed: the previous ghost lingered for a frame when it was redrawn. Test: the ghost framed while snapping, unframed and tinted in free mode (Decorate tests 10/10).

**Checkpoint A approved (2026-10-05):** "Decorate Mode feels excellent now."

### 4f Checkpoint B: catalogue, buying, colours and the guest room (2026-10-05, done; waiting for your playtest)

Steps 3–6 as one package. The aim: from "I can move the existing tavern around" to "I can make the Sunken Flagon look like my own place". Commits `d2b9547`, `d3e7fae`, `b912161` and the web-test fixes after them.

**Step 3, the catalogue pipeline and first import.**
- The source table is `Assets/_Project/Data/Furniture/Catalog/catalog.json`: one entry per piece with its id, English name and description, category, theme, tier, price, sheet, a rect per drawn facing, layer, function, flip permission, uniqueness, sources, colourways (offsets or rects, or a shared variant set), palette channels, and per-facing overrides (footprint, body depth, seats and approaches, surfaces, light, animation frames). Adding a piece is an entry, not code.
- *Hearthdelve → Generate → Furniture Catalog* (`FurnitureCatalog`, also run by the in-place updater) copies each source sheet with only the named drawings kept (`ThirdParty/Minifantasy/Furniture/Furniture_<sheet>.png`), trims animation strips, writes the slicing the import pipeline reads (`catalog_sprites.json`, so a fresh clone slices the same way), builds the `FurnitureDefinition`s by rule (footprint from the drawing, art centred on whole pixels, body the drawing or its bottom rows, seats facing the way each facing is drawn), and reports problems (unknown sheets, empty or out-of-sheet rects, duplicate ids or turns, colourway overrides for unknown colourways, bodies bigger than footprints, palette channels that match nothing). It runs with no warnings.
- Review tools in `Tools/furniture/`: `islands.py` (numbered contact sheet of a raw sheet's drawings), `zoom.py` (a measured close-up), `contact.py` (every piece, facing and colourway by category, from the table and the raw sheets, with rect checks; `BatchLogs/catalog/contact_*.png`).
- **The first catalogue: 98 pieces (394 placeable entries counting facings and colourways) from 24 sheets, plus the 13 starting pieces.** By category: seating 11 (80 entries), tables 12 (54), bar and storage 13 (46), lighting 12 (24), on the walls 15 (49), rugs 6 (19), plants 9 (53), bedroom 10 (31), curios 10 (38). Collections: village 29, Dwarven 15, castle 14, haunted 12, tavern 10, Elven 9, curios 9.
- A developer sandbox (F10 in development builds): every piece open and free, for trying the whole catalogue in the room.

**Step 4, buying, storage and Renown tiers.**
- The catalogue opens from Decorate Mode (Tab / Start) on what's in storage, then a page per category and one for walls and floors (Q / RB next page, Shift / LB the previous one; the d-pad's left and right too). Each piece shows its drawing, name, description, price, Renown requirement when locked, placed and stored counts, where it comes from (starting furniture, a find, a trophy, a gift, one of a kind) and its colourways.
- A (E): place one from storage, or buy one straight onto the cursor. X (R): buy one into storage. Y (F): sell one from storage for half its price (D12; bought pieces only, never uniques, discoveries or trophies). Uniques cap at one (D7). Delivery is immediate. A purchase plays a coin sound and a pulse; the game saves after each purchase.
- **Renown opens catalogue tiers (D14), never spent:** 0 the village joiner (tavern basics, village pieces, rugs, plants, candles, basic bedroom), 25 the Dwarven masons (and the leather sofas, bathtub, washstand, globe, lucky cat, paper lanterns), 60 the Elven and castle workshops (and the knight statue, star chart, orrery), 100 the strange and the grand (the haunted collection, the great tun, the barrel pyramid, the stone fireplace). Tuning: `Data/Furniture/CatalogSettings.asset`. The Night screen announces a newly opened tier once ("word is spreading: the dwarven masons will take your orders."), with a chime and a pulse; the announcement is saved.

**Step 5, colours.**
- One colour panel (V / right stick press) for the piece carried or under the cursor: a "style" row for Minifantasy's own colourways (pots in six glazes, Dwarven pieces in six materials, Elven in five colours, castle gold or silver, chests in eight, rugs, beds…), a row per palette channel (the tavern's chairs, tables, benches and stools: wood and cushion), schemes setting several channels at once (dwarven hall, elven glade, castle feast, haunted den, harbour, hearth), "use last colours" (copies the last look given, by channel) and "match every copy" (this look for every copy in the room). Changes show at once and undo.
- Recolouring remaps a channel's drawn colours onto a ramp by brightness rank and bakes the result once per drawing and choice (D11), so pieces keep the lit sprite material and lighting. The ramps are colours Minifantasy drew: woods from the Elven tables and the Dwarven materials, cushions from the Elven rugs.
- **Walls and floors (D5):** area-wide finishes bought once for the whole property: floors green diamonds (the tavern's), teal tiles (the guest room's), plum tiles, pale chequer, parquet, blue and sage castle tiles; walls dark panelling (the tavern's), cream plaster (the guest room's), green plaster, and the panelling in walnut, birch and ash (the same ramps on the wall). No per-tile painting, and the structure stays fixed.

**Step 6, the guest room.**
- A second area beside the tavern in the Tavern scene (18×13 cells, 16×8 of floor), cut from the Shop Indoor add-on's cream-walled premade room. It starts with a double bed, a nightstand with a candle, a chest, a rug and a picture.
- Decorate Mode switches between the tavern and the guest room with G / left stick press, with one storage for the whole property; the camera holds on the room being decorated. Slim wooden stairs in the tavern's back-right corner (Medieval City's interior flight, against the right wall) lead up to it with a fade, and its door leads back down to their foot. (A wide stucco staircase on the back wall came first; it was replaced after your first look, see below.)
- Its check is only its doorway (no service checks). Bedroom pieces work in the tavern and tavern pieces upstairs.

**Also changed:**
- Stools seat whoever sits at whichever table is beside them; every seat now picks an approach that can be walked to (a chair facing down in a row is stepped onto from behind), the same way in the check and in the room.
- Surface items on a table sort just in front of it. Tables have surface anchors (candles, tankards, small plants, bottles, books, jack-o'-lanterns).
- A room's fixed parts (the stairs' flight) block the check's walkable grid like furniture; the stairs' cells and the tile in front of their foot are kept clear of furniture.
- Direction input keeps taps that start and end within one frame (the web build lost them in menus).
- **Save version 6:** each piece's colourway and palette, each area's floor and wall, finishes owned, the tier announced. A version 5 save (your Checkpoint A playtest) loads as it was, gains the guest room furnished, and hears about tiers it has already reached at its next Night.
- The starting tavern differs from Checkpoint A only by the corner stairs (their art, solid flight, foot, and two grid cells in column 26); `TavernStarting.txt` was re-recorded and the differences checked line by line.

**Tests:** EditMode 504/504 (new `CatalogueTests`: the generated catalogue and stable ids, every drawing per facing and colourway, the collections across four tiers, buying, uniques, selling, finishes, tier announcements, palette parsing and the remap, the baked recolour cache, finishes' patterns, property-wide storage, granting the guest room to an old save, the guest room's check, fixtures, stools, surface sorting, version 6 and the v5 migration). PlayMode 178/178 plus 7 explicit captures (new `CheckpointBTests`: buying onto the cursor, storing and selling; a tier opening with Renown; recolouring and undo; the colour panel and matching every copy; the panels and a one-frame tap from the keyboard; a floor finish and putting it back; decorating the guest room; the stairs and the door, also mid-service; a service in a tavern refurnished as a dwarven hall; and in `DayLoopTests` a tier announced once with both rooms' looks surviving Continue).

**Captures** (`CheckpointBCaptures`, explicit; `BatchLogs/checkpointB/`): the starting tavern, the tavern restyled through palettes alone, a dwarven hall, a castle banquet and a haunted den built from the catalogue, the guest room as it starts and furnished, every ramp on the tavern's chair and table, the catalogue and the colour panels.

**Web (2026-10-05):** built and served without caching. Checked with your version 5 save: Continue (migrated, the guest room arrived furnished), Decorate with the mouse and keys, the catalogue (pages, buying a table onto the cursor, placing it), the colour panel (a teal cushion), switching to the guest room, laying a plum floor there, leaving (saved), reloading and Continue (all kept), sleeping into the next day, opening for the evening, walking up the stairs mid-service and back down through the guest room's door. No console errors were seen (the console was watched from part-way through).

**Deviations from the plan:**
- The guest room's shell is Shop Indoor's cream-walled room, not the Towns II plank interior: the plank tileset is a dark modular exterior-style set; the shop room matches the tavern's cell geometry (so the same cutting works) and reads as a warm inn room. It's 18×13 cells rather than about 12×9, which looked lost on screen.
- The stairs work during service: until the village milestone the keeper walks only during service (the daytime, Prep and Night are panels), so blocking them then would make them unreachable. Customers keep waiting while you're upstairs, and the end of the evening brings you back down.
- The old storage panel became the catalogue's first page.
- The Animated UI Book wasn't used: the parchment panel keeps the list and details readable at 320×180.
- The catalogue's hand-coded pieces (the stations, the 4e tables, chairs, barrels and back-wall pieces) stay in `FurnitureContent` because of their use points, posts and overlays; their prices, themes, descriptions and palette channels are set there too.

**Known issues (Checkpoint B):**
- Not driven in the web check after the follow-up fixes (the stairs removed, the row width, the spellings): the build was rebuilt and the suites pass.
- The castle rugs' centring under 2×2 tables is half a tile off in the review capture (placement, not the art).
- The wooden lantern and paper-lantern stands are large; judge them in the room.
- The haunted den capture has one seat whose approach can't be reached (a warning, as designed).

**First look (2026-10-05):** three fixes.
- The staircase on the tavern's back wall is gone (its art, solid flight, foot, fixture and reserved cells, and the imported stucco sheet). The guest room is reached from Decorate Mode (G / left stick press).
- "not for sale" wrapped onto the next row in the catalogue's list: the price column is wider (72), and two starting descriptions that ran to five lines (the stool, the bar) are shorter. A new test (`EveryCatalogueText_FitsItsLines`) opens every page, selects every entry, and fails on any row or detail line that wraps past its room or a name that runs into its price.
- Player-facing "colours" is now "colors" (the controls line, the panel title, "use last colors", and two descriptions).
- Tests: EditMode 504/504, PlayMode 179 passed plus 7 explicit captures. Web build rebuilt.

**American spelling (2026-10-05):** all player-facing English now uses American spelling: catalog, gray, harbor, traveling trunk, checkered, pale checkerboard, paneling (and colors, above). Ids and saved values are unchanged (`grey`, `chequered`, `harbour`, `wall_panelling…`), so saves are unaffected. `TextStyleTests.EnglishStrings_UseAmericanSpelling` checks every English string; the localization builder now removes colorway name keys (`look.…`) that are no longer generated, so a renamed colorway leaves no stale entry. EditMode 505/505, PlayMode 179 passed plus 7 explicit captures.

**Corner stairs (2026-10-05):** your choice after the first look: a smaller staircase in a corner. Medieval City's slim wooden flight (9×17 px, about a third the size of the stucco one) stands in the back-right corner against the right wall, rising into the back wall, mirrored so its handrail faces the room. Walking onto its foot (column 26, row 11) fades up to the guest room; the guest room's door brings you down just below it. A Decorate test that carried a barrel to (26, 12) now carries it to (26, 10). EditMode 505/505, PlayMode 179 passed plus 7 explicit captures. Web build rebuilt.

**Color panel and the low shelf (2026-10-05):**
- The color panel's chosen-color name overlapped the swatches on longer names (a table's "slate black", "gray stone"): the panel is wider (288) and the name has its own column after the last swatch. A test (`EveryColorPanelText_FitsItsRow`) opens the panel on every piece with looks and every option of every row, and fails if a name wraps, touches a swatch or runs past the row.
- The chosen row was pale yellow text on parchment: it's now ink on a soft gold band, as in the catalog; a chosen button takes the same gold.
- The glasses stood on top of the low shelf: they now stand inside it (the shelf's surface anchor is its inner floor, half a tile lower, and the glasses are the sheet's brown-based row). Saves keep only which shelf an item is on, so old saves show them inside too.
- Tests: EditMode 505/505, PlayMode 180 passed plus 7 explicit captures (new capture: `colours_table.png`). Web build rebuilt.

**Checkpoint B: approved (2026-10-05).**

### 4f Checkpoint C: discoveries, the tusks, the market, the menu, the Butcher Block, Gunta and Pip (2026-10-05, done; waiting for your playtest)

Steps 7–9 as one package. The question: delve → bring strange things home → cook and use them → improve the tavern.

**Step 7, furnishing discoveries and the Larder Troll's tusks.**
- **Curios** are a channel of their own, never a satchel slot: a small chest with a twinkling glint on the floor, picked up by walking over it (the discovery chime, a violet light, the `Discovery.Found` haptic), a "found: …" line in the feed and a counter under the Essence bar. They are owned only when the delve ends in extraction (D9); dying loses them all (the Lockbox holds ingredients only), and the death screen and the result say so ("lost with you: …", "found, then lost: …"); an extraction's result says "found for the Sunken Flagon: …". Pieces brought home go to storage with a **new** badge until looked at in the catalog.
- **Two sources:** a fight room's reward can be a curio (`RewardKind.Curio`, its own door marker, the chest; weight 0.3 against gold 1, ingredient 1, power 0.6, so about one fight in ten), and enemies rarely drop one: 4% a kill, at most 2 a delve, only enemies the pool names.
- **The Cellars pool** (`Data/Dungeon/CurioPool_Cellars.asset`, all tunable): spiders drop the cobweb, the great cobweb and the spider brazier; slimes the slime jars; bats the tattered banner; room caches give any of those plus the skull candle, iron cage, cellar stores, delver's junk heap and the mimic chest. Duplicates (D8): useful pieces repeat; uniques already owned (or found earlier this delve) are never rolled; pieces not owned yet weigh ×3; when nothing is left, the reward is 25–40 run gold. Rolls are seeded by the run.
- **Discovery-only pieces:** the 10 above (7 new: skull candle, slime jars, tattered banner, iron cage, cellar stores, delver's junk heap, spider brazier; and the cobweb, great cobweb and mimic chest, moved out of the shop). The other curiosities (the globe, orrery, star chart, knight statue, lucky cat…) stay for sale.
- **The Larder Troll's tusks** ("Larder Troll tusks", a unique wall piece, no bonus; art in `ASSET_MAP.md`): granted when the troll's defeat is recorded, even if the delve then ends in death, and never lost (D10). The result screen says "a trophy: Larder Troll tusks". The next time Decorate Mode opens they're already on the cursor ("Larder Troll tusks: where should they hang?"); hanging them plays a homecoming moment (a warm chime and a long pulse) and publishes `TrophyDisplayed`. Seated patrons sometimes look up with a wordless "!" (half of them, once each).
- **Old saves:** a save that beat the troll before trophies existed gets the tusks once, on load, by the same rule as the normal path (`TrophyRules.GrantEarned`), so both end with one copy waiting for its homecoming.
- **Facts for 4g:** `CurioBroughtHome`, `FurniturePlaced`, `TrophyDisplayed`, `MarketPurchase`, `PartButchered` and `StaffWorkDone` are published on the `EventBus`; nothing listens yet.

**Step 8, the market, the staples and the menu.**
- **The Brackenford market** is a daytime errand (a button on the daytime panel): onions 2 gold, herbs 2, bread 3, eggs 3, malt 2, one each. Bought goods go straight to the storeroom, Standard and fully fresh, and age like anything else; the game saves after each purchase. It's a `SupplySource` asset (`Data/Tavern/Supply_BrackenfordMarket.asset`), and every ingredient now names its source (Hollows, Market; Farm, Ranch, Fishing and Villager are reserved for the village milestone). Bread and malt are a new **Grain** category.
- **The mushrooms** have icons at last and come from the Cellars: shroom caps are an ingredient room reward from the first floor, spore sacs from the second.
- **The menu (13 dishes; prices are first-pass):** everyday, from the market alone: Brackenford ale (malt; 6), onion broth (onions, herbs optional; 6), eggs on toast (eggs, bread; 7). Better, one thing from below: gelbrew (slime gel, malt; 10), grilled spider leg (13), crispy bat wings (12), shroom skewer (10), cellar stew (spider leg, onion, shroom cap optional; 10), offal pottage (venom sac, onion, bread optional; 9), cellar kebab (any meat or offal, shroom cap, bread; 16). Signature: core tonic (22), spider-leg steaks (two leg cuts, herbs; 26), bat-wing platter (three wing cuts, bread, spore sac; 28). Ale uses the tankard icon.
- **Delve meals,** retuned with the menu: ale, gelbrew and core tonic slow the drain (by 0.1, 0.2, 0.3); eggs on toast +10 Essence, wings and skewers +15, grilled leg +20, kebab +25, the cut dishes +30.
- **Prep** shows six dishes a page, the ones the storeroom can make first ("dishes 1/3" turns the page).

**Step 9, the Butcher Block, Gunta and Pip.**
- **The Butcher Block** is Crafting And Professions II's preparation table: a station, one of it, movable and storable like the others (it takes part in placement, storage, the walkable grid and the check, but never keeps the doors shut). It starts on the open floor right of the front table. It is used at **Prep** (mise en place), from the Prep screen's "butcher block" button: a list of the parts in the storeroom that break down (spider legs into leg cuts, bat wings into wing cuts), each with "cut it" or "Gunta cuts it".
- **The cut** (5–10 seconds): the part on a board with three slanted cut lines as dotted guides; hold E / Space / A (or the mouse button) and follow each line with the knife (mouse or left stick) as it draws down. Each line scores by how closely the knife followed it, and the score sets the yield (1 cut, 2 from 0.5, 3 from 0.85; per part, in its data). The cuts keep the part's quality and freshness. Feedback: a knife-in tick per stroke, a clean cleave (`Cut.Clean`) or a ragged one, a smooth or rough drag rumble while cutting, a closing thunk scaled by the yield, and a flash. Tuning: `Data/Tavern/ButcherConfig.asset`.
- **Gunta Ashbelly** (`gunta`) is in the tavern from the first evening: a stocky, bearded dwarf in a white cook's cap and a red apron. At Prep her job button cycles Grill, Stew Pot, Tap and off duty (she starts off duty; see deviations). At her station she cooks its orders with her own steady hand (skill 0.75, capped at 0.85 like all staff, so the keeper's best work still beats hers). Given a part, she walks to the Butcher Block and cuts it (auto-played at her skill). A station is never shared: giving Pip Gunta's station sends her off duty, and the reverse sends Pip back to the plates. No dialogue: a content face after a good batch, a happy one after a full cut, a frown after a poor chop.
- **Pip Marrowby** has their own look (a halfling in a green waistcoat and brown breeches, replacing the stand-in), still serves, tidies a seat after a patron pays, sweats when bumped and looks startled at a dropped plate. The evening's results end with "from Pip's ledger" beside a book icon.
- **Saves, version 7:** the new pieces still marked new, and a trophy waiting for its homecoming. A version 6 save loads as it was; it gains the Butcher Block in storage (marked new; its rooms unchanged) and, if it beat the troll, the tusks.
- **Wall decor and the stairs:** wall pieces can no longer be hung where the corner stairs would hide them.

**Tests:** EditMode 536/536 (new `CheckpointCTests`: seeded rolls, the first-copy weight, duplicates, uniques excluded, the gold fallback, enemy and room origins and the delve cap, extraction keeps and marks new, death loses, the trophy on a death, never lost or doubled, an old cleared save granted once and the same as the normal path, a save that never won, the market (gold, delivery, daytime only, aging), the yield thresholds, cuts inheriting quality and freshness and taking exactly that part, the minigame scoring a steady hand above a shaky one in 5–10 s, Gunta's capped butchering, the staples as market food, the 13-dish menu in three tiers with the everyday dishes from the market alone, the cut dishes needing the block, the mushroom forage, version 7 round trip and the v6 migration (the block once, sold starters stay sold); plus the stairs rule). PlayMode 189 passed, 0 failed, plus 14 explicit captures (new `CheckpointCTests`: a kill's and a room's curio picked up apart from the satchel and home new; dying loses them, with the death screen saying so; the tusks from the troll's fall, their homecoming and hanging (saved); an old cleared save gets them and the block once across two reloads; the market into the storeroom and an everyday dish through Prep and service into Pip's ledger; the Butcher Block by hand (yield from the cut's score, quality and freshness, the feedback) and by Gunta; Gunta taking a station and cooking its orders while Pip serves, stations never shared; both looks and emotes; patrons noticing the tusks. Existing tests updated for six cards a page, the block and Gunta). The shared `WaitUntil` test helper now also has a real-time limit, so a paused game fails a test instead of hanging the run.

**Captures** (`CheckpointCCaptures`, explicit; `BatchLogs/checkpointC/`): curios on the floor and in the HUD, the result kept and lost, the death screen, a new piece in storage, the daytime panel and the market, the trophy result, the homecoming and the tusks on the wall, Prep's pages and Gunta's job, the Butcher Block list, a cut and its result, Gunta at the block, Gunta and Pip in service, Pip's ledger.

**Web (2026-10-06):** built (development, 0 errors) and served without caching (a fresh query string on each load); the console was read from the first load to the end. Checked: a new game into the delve (the run's plan in the log shows curio rooms on floors 1 and 2); the browser's save rewritten as a **version 6 save that had beaten the troll twice and had no Butcher Block**, then **Continue**: the tusks arrived once and opened Decorate Mode on the cursor; hung on the back wall; the Butcher Block was in storage marked **new**, placed on the floor; leaving saved version 7 with one of each; **sleep**, the daytime panel with the **market** (bought eggs, bread, malt; "you have" counts, purse and the delve meals updated); **Prep**: the Butcher Block list's empty message, Gunta set to the Grill, ale and eggs on toast chosen; **service**: Gunta grilled the eggs, Pip carried them, the patron paid; the **results** with Pip's ledger; the **delve**, a **death** (the death screen and result); night; a reload and **Continue**: the tusks and the block still there once, no second homecoming. No errors or warnings in the console at any point.
- **Not driven in the browser:** picking up a curio and bringing it home or losing it. Curio rooms were generated (the run log listed them), but fighting through rooms with the browser tool's click latency wasn't practical; the pickup, extraction and death paths are covered by PlayMode tests and the captures.
- **Found and fixed during the web check:** the homecoming opened with the cursor over the middle of the floor, so it said "can't go there"; it now opens on the free spot of the back wall nearest its middle (verified in a rebuilt web build). In the starting tavern that's the one gap wide enough, above the kitchen range, whose chimney draws in front of the tusks' lower half (see known issues).

**Deviations from the plan:**
- **Gunta starts off duty,** not at the Grill: on the Grill by default she took every grill order on the first night, so the keeper never grilled. You choose her station at Prep. Easy to flip (`TavernDirector`) if the playtest says she should start working.
- The Butcher Block is used from the Prep screen, not by walking up to it (walking up to it during service shows no hint and does nothing): Prep isn't walkable (as you asked). The keeper appears at the block for the cut, and the block's knife moves while anyone works it.
- The trophy is named "Larder Troll tusks" (the catalog's one-line name has no room for "the Larder Troll's tusks").
- In the catalog, a unique piece shows "one of a kind" on its counts line instead of its source line (no room for both).
- The crystal lamp curio was dropped (its sheet is layered); the skull candle is the curio light.
- No warm screen flash at the homecoming: its moment is sound and haptic, with the result line and the cursor.

**Known issues (Checkpoint C):**
- Gunta and Pip have no sitting or eating poses, and Gunta's apron is a recolour of the dwarf's clothes, not a separate layer.
- Patrons' "!" at the tusks needs them seated with the tusks on the tavern's wall; it isn't aimed at the tusks.
- The market has no stock limits or day-to-day prices yet (no economy balance in 4f).
- The starting tavern's back wall is nearly full: the tusks fit in one gap, above the kitchen range, and the range's chimney draws over their lower half. Moving a shelf or the sign makes room; whether the starting wall should leave a trophy spot is a call for your playtest.
- Three files in `Assets/_Project/Fonts/silver/` (a field-guide HTML page, an image and a scanned PDF) aren't part of this work and are left uncommitted.

**Checkpoint C: approved (2026-10-06).** Final calls from its playtest: Gunta stays off duty by default; one clean trophy-sized patch of back wall in the starting layout; curio frequency and the Butcher Block's feel unchanged.

### 4f Checkpoint D: special requests, the balance pass, integration and the 4f sign-off package (2026-10-06, done; approved with 4f)

Step 10, the last checkpoint of 4f. Commits `688df57`, `4ffdc7f` and the fixes and docs after them.

**Special customer requests (D1).**
- Now and then a patron's order is one they particularly want tonight. It is the same order through the ordinary service loop (no second minigame, not a quest): when a patron orders, the service may mark the order a request (`CustomerRequestRules`, pure and seeded by the evening's random). Because it is decided at the moment of ordering, a request is only ever for a dish on today's menu that the stock can make right then (its ingredients are already set aside, or a stew helping promised); if the player later lets that dish sell out, the request can fail through their own choices.
- **Tuning** (`ServiceConfig.requests`): chance 0.2 per order, at most 2 an evening, never the evening's first order. In a normal service that is about one or two (0–2), with some nights none.
- **Met:** a thank-you of half the dish's value (at least 2 gold), added to the tips, and +1 Renown; a heart over the patron. **Missed** (walked out, sold out, closing time): a frown, nothing taken away.
- **Readable at 320×180:** a sparkle on the patron's bubble beside the dish and on the order rail; the results show "special requests 1 of 2" (met of made) when there were any.
- **Facts for 4g:** `CustomerRequestIssued`, `CustomerRequestCompleted` (quality, gold and Renown given), `CustomerRequestFailed` (reason `walked_out`, `sold_out` or `closing_time`), each with the patron's profile id and a visit id that tells tonight's patrons apart (not saved).
- Requests live only for the evening: a new service starts with none, and nothing about them is saved.

**The balance pass (D2).** Measured with a new tool, `BalanceReport` (*Hearthdelve → Balance → Evening Report*, or `-executeMethod Hearthdelve.Editor.BalanceReport.RunBatch`; writes `BatchLogs/balance.md`), which runs representative evenings through the game's own recipe matching and scoring (`EveningEstimate`: about 11 patrons a night, played weakly, competently or strongly; parts from below cost nothing, their price is the delve).

| Evening | Profit before | Profit after | Renown before | Renown after |
|---|---:|---:|---:|---:|
| market only, weak | −7 | +2 | 0 | −1 |
| market only, competent | (new) | +26 | | +7 |
| market only, strong | +28 | +43 | +12 | +13 |
| recovery after a death (a Lockbox stack of legs) | +25 | +41 | +1 | +7 |
| ordinary Cellars delve, competent | +63 | +63 | +1 | +6 |
| the same, Gunta on the grill | (+46, at a guessed score) | +50 | +1 | +5 |
| strong Cellars delve | +128 | +133 | +13 | +15 |
| signature night (butchered cuts, a core) | +261 | +208 | +11 | +13 |

- **What changed and why:**
  - **Renown adds up over the evening** and the total is rounded (it was rounded per patron, so anything short of strong play earned nothing); neutral satisfaction 0.5 → 0.45. Competent nights now earn about 6, strong ones about 15.
  - **Market:** bread 3 → 2 and eggs 3 → 2 gold; **eggs on toast** 7 → 9 (it cost more than it sold for). A weak market night now breaks even instead of losing money; at the same skill a delve night earns about 2.4× a market night (63 against 26), a strong one about 3× (133 against 43).
  - **Signature dishes:** spider-leg steaks 26 → 22 (and renamed "spider steaks" on screen: the full name ran into its price), bat-wing platter 28 → 26 and four wing cuts instead of three. A clean cut was turning a 6-gold bat wing into 28.
  - **Gunta's cap** 0.85 → 0.75. Measured, she reached her cap at every station, which beat a competent keeper (0.82): handing her the grill was simply better. At 0.75 she costs about a fifth of a competent night's profit (50 against 63) and never cooks badly. Pip is unchanged (0.85 cap, serving).
  - **The strange and the grand (tier 3, Renown 100)** priced as aspirational: from 15–200 (median 55) to 25–300 (median 85), e.g. the stone fireplace 200 → 300, the haunted sofa 85 → 130. Tiers 0–2 unchanged.
- **Kept:** the Renown tiers (0, 25, 60, 100: a strong player reaches the last in about a week of nights, a competent one in two or three), curio frequency (0.3, 4%, 2 a delve, ×3), the Butcher Block's feel and thresholds, Gunta off duty by default.
- **The Butcher Block's value per part (gold of dishes):** spider leg 13 whole; 11, 22, 33 at a weak, fair and clean cut. Bat wing 6 whole; 6.5, 13, 19.5.
- **Furniture against earnings:** a median tier-0 piece (20) is a third of a competent night; a median tier-2 piece (55) about one night; the dearest piece (300) two strong nights or five competent ones, before any upgrades.

**Integration (D3).** A new PlayMode test plays three days from a new game: the first delve brings a curio and a haul home; at night an upgrade is bought and a barrel moved; day 2 buys staples at the market, cuts a spider leg at the Butcher Block, puts Gunta on the grill and Pip on the plates, and a special request is met between them; the takings (the thanks among them) are banked; the second delve finds a curio and dies, losing it; day 3, then Continue twice, with every piece of furniture, the gold, Renown, storeroom and the rearranged room exactly as they were, and nothing granted again. Existing tests cover the rest: v6 and v5 migrations, a v7 save continued, extraction and death, the troll's first clear and the trophy, a service in a refurnished tavern, the guest room and its stairs.

**Polish (D4).**
- **A clean trophy spot:** the wall sign moves from (11,15) to (17,15), right of the fireplace, opening a clear stretch between the bottle shelves and the fireplace; the homecoming now prefers a spot whose drawing nothing covers, then the one nearest the middle (so older layouts avoid the range's chimney where they can). Old saves keep their own layouts.
- Prep: stew prices read "9 gold" (no "/bowl"), and the card test now wants three pixels between a name and its price.
- The results panel fits eight lines (special requests) on screen: 178 px tall, lines 11 px apart.

**Facts for 4g (D6).** Besides Checkpoint C's (`CurioBroughtHome`, `FurniturePlaced`, `TrophyDisplayed`, `MarketPurchase`, `PartButchered`, `StaffWorkDone`) and 4e's `BossFirstCleared`: `CustomerRequestIssued`, `CustomerRequestCompleted`, `CustomerRequestFailed`, `DishServed` (dish, patron, quality, gold, tip, whether it was a request), `ServiceCompleted` (day, dishes, gold, tips, Renown, walkouts, requests met and missed) and `BossDefeated` (every defeat, whether the delve came home). All in `Shared/Game/GameFacts.cs`, stable ids only; nothing listens yet.

**Saves (D5).** Still version 7: requests are evening-only and the tuning lives in assets, so nothing new is saved.

**Tests:** EditMode 556/556 (new: `CustomerRequestTests`, `RenownAndBalanceTests`, `ArrivalTests`, and the trophy-spot wall check). PlayMode 197 passed, 0 failed, plus 16 explicit captures (new: `CheckpointDTests`, including Gunta stepping to a job she can't walk to and Pip serving at three frames a second, and `CheckpointDDayLoopTests`).

**Captures** (`CheckpointDCaptures`, explicit; `BatchLogs/checkpointD/`): Prep's menu, the tusks' first spot, a request in service, met and missed, the results with requests, Night, the catalog, the guest room, the market. Checkpoint C's captures were rerun as a regression check.

**Web:** a fresh build (`bundleVersion` 0.4f, which also silences the loader's version warning), served without caching and every build file refetched. Checked: a v6 save with two troll clears migrated once to v7 (tusks and Butcher Block granted once, nothing doubled), Continue, the homecoming, the guest room, the catalog and a purchase, an upgrade, the market, Prep, the Butcher Block by hand and by Gunta, Gunta on the grill with Pip serving, a special request (the sparkle on the bubble and the rail, met for a 4-gold thank-you and +1 Renown, "special requests 1 of 1" on the results), Night, a delve into the second room and an extraction, sleep, and reloading the page to Continue on the next day, twice. No console errors; the only warning is URP's own stripped-upscaler notice at load.
- **Two bugs found and fixed.** The browser tab was in the background, which runs Unity at about three frames a second, and that exposed a frame-rate-dependent arrival: staff and patrons only arrived within 0.2 tiles of a goal, but a slow frame moves them about a tile, so they overshot for ever (Gunta never reached the Butcher Block; Pip circled the pass and never picked up a plate). Now a goal within one frame's travel counts as reached (`Core/Pathfinding/Arrival`, used by `StaffAgent` and `CustomerAgent`; at normal frame rates nothing changes), and a staff job outside service steps straight to its spot after 8 s of walking. Both have regression tests (`Time.captureDeltaTime`). Enemies use the same AI action but were not changed; the delve was only checked at normal speed.
- **Not checked in the browser:** a curio pickup and a death (clearing rooms by synthetic clicks at three frames a second wasn't practical); both are covered by PlayMode tests and were checked in the browser at Checkpoint C.

**Deviations from the request:**
- The tier-3 furniture price rise and Gunta's lower cap are the two tuning changes beyond the menu and Renown; both are explained above.
- "spider-leg steaks" became "spider steaks" on screen (the id is unchanged), so its name clears its price at 320×180.
- A request missed at closing time frowns behind the results panel (a walkout's frown during service is visible).
- The arrival fix (staff and patrons at a slow frame rate) and Gunta's step-to fallback were not in the request; they came out of the web check.

**Known issues (end of 4f):**
- `EveningEstimate` is a comparison tool, not a simulation: no walking, queues or walkouts, an average patron, and a fixed play style per evening.
- At a very low frame rate (a background browser tab) long staff walks still lean on the 8 s step-to, and enemies, which share the pathfinding action, were not reviewed for the same overshoot.
- The oversized lanterns (subjective), the patrons' "!" not aimed at the tusks, and no sitting or eating poses for Gunta and Pip remain, as agreed.
- The three unrelated files that had been in `Assets/_Project/Fonts/silver/` were no longer there when this checkpoint began (removed or moved outside this work); nothing of theirs was touched or committed.

**4f signed off 2026-10-06** (see "4f complete" below).

### UI foundation pass: a type scale for Silver (2026-10-06, done; approved with 4f)

Before 4g, at your request: a typography hierarchy with the one font, applied conservatively.

**What made the UI look flat.** All 349 texts in the game's scenes were the same size: Silver at 19 (1×), except the menu title and the transition caption at 2×. The builders still asked for m5x7-era sizes (7 for titles, 6 for body, 5 for counts), which `GameFonts` snapped to 1× when Silver replaced m5x7, so even the title/body distinction was lost.

**The finding that shaped it.** Silver is pixel-clean only at whole multiples of 19 on the 320×180 grid (its em is 19 font pixels). There is no clean size *smaller* than body text: half-size text would be unreadable at 320×180, uneven at odd window scales and a second pixel density on screen. You chose the art-grid-only scale.

**The scale** (`Data/UI/TypeScale.asset`, `TextStyle`, `StyledText`; baked by `GameFonts` from the builders through the updaters):

| Style | Size | Line | Used for |
|---|---:|---:|---|
| Display | 57 (3×) | 36 | the game's name |
| Heading | 38 (2×) | 24 | screen titles: Prep, Daytime, Night, Results, the death screen; the transition caption; the tavern HUD's numbers |
| Body | 19 (1×) | 12 | rows, names, values, descriptions (dialogue in 4g) |
| Secondary | 19 (1×) | 12 | labels, counts, sources, notes (muted colours) |
| Prompt | 19 (1×) | 12 | button labels and control lines |

Secondary and Prompt share Body's metrics; they're named roles so every text says what it is and each can be tuned later from one place. Sub-panel titles (market, Butcher Block, catalog, colors, layout check, swap prompt, run power, minigames) and the delve result's title stay 1×: "dragged back to the surface" is 314 px at 2×, wider than the screen allows.

**Layouts fixed along the way.**
- Results: the 11-pixel line squeeze is gone (a true 12-pixel line); a night's note sits in the row after its last line (the ledger's row only if all eight are used).
- Prep, Daytime and Night: panels 176 tall; content under the heading band; the storeroom row re-packed so its freshness bars clear the cards; Prep's count sits on the heading's baseline; Prep's kitchen buttons rebalanced so "butcher block" keeps its b (the frame was clipping it before this pass).
- Decorate: the controls take three lines (the last word, "done", was cut off before); both strips are even heights (two texts sat half a pixel off the grid).
- Prep's "nothing makes a dish" banner has two lines (its sentence overflowed one).
- Market buy buttons 74 wide and the death screen's confirm 128, so their labels clear their frames.
- The tavern HUD's gold, tips and Renown are 2× numbers under 1× labels; the menu icons move down.

**Words.** "spider-leg steaks" is back on screen (Checkpoint D's "spider steaks" shortening undone; the name starts a pixel nearer its icon). The debug "F4: fill storeroom" is "F4: fill" (it shares the heading's row).

**Tests:** EditMode 564/564 (new `TypographyTests`: the scale's styles at whole multiples with room for their glyphs, a broken scale reported, applying a style sets Super Text Mesh exactly, every text in the game's scenes styled from the asset, every fixed English string fitting its box at its style with headings keeping a fifth spare and button labels two pixels inside their frames, Prep's title row on one baseline, the restored name). PlayMode 200 passed, 0 failed, plus 18 explicit captures (new `TypographyLayoutTests`: the results at their fullest on whole lines with nothing overlapping, the HUD's 2× numbers to four digits, "spider-leg steaks" clearing its price; `TypographyCaptures` for before/after at 320×180 in `BatchLogs/typography/`).

**Web:** a fresh build (every build file refetched without the cache) at 1280×720 (canvas scale 4). Checked in the browser: the 3× title and the 1× menu text with even, crisp pixels; the delve result; Night's, Daytime's, Prep's and Results' 2× headings; Prep's count on the heading's baseline and its two-line banner; the market's wider buttons; the tavern HUD's 2× numbers through a short service; Results on whole lines with Pip's ledger; Decorate's three-line controls. No console errors or warnings. The browser held a save from someone playing the build (day 5, written while I was working on code); it was backed up first and written back byte for byte afterwards.

**Known issues:**
- Accented capitals and CJK glyphs are 11–12 pixels tall on a 12-pixel line, so they touch the line above; a locale that needs them will want a 14-pixel line (one value in the scale).
- Gold in the tavern HUD fits four digits at 2×; a five-figure night would need the column revisited.
- The market's "not enough gold." sits close to its panel's edge (unchanged).

**After your first look (2026-10-06):**
- The catalog's page name ran into the purse once gold reached three digits ("bar and storage", "walls and floors" beside "148 gold · Renown 4"). The title row now has reserved columns: the page's name left over the list (150 px), the purse right-aligned over the chosen piece (136 px; "9999 gold · Renown 100" is 120).
- Daytime's delve-meal cards show each meal's effect ("+20 max Essence") in the delve-effect purple of "tonight's delve", apart from the dish's name.
- A new check, `TextOverlapTests` (PlayMode): no two visible texts overlap and none leaves its panel, across Prep's pages, the Butcher Block, Decorate, every catalog page and piece at Renown 0 and 100 with 9999 gold (246 states, 5,842 texts), the layout check, the delve's result, Night, Daytime and the market. It proves itself by catching the old purse position when planted. It found nothing else; Decorate's status line was measured separately (12 px spare at its longest).
- Tests: EditMode 564/564, PlayMode 203 passed, 0 failed, plus 18 explicit captures. Not rebuilt for the web (builder-only layout and colour changes).

### Names, Boog and safe ground (2026-10-06, the owner's request)

- **Names:** the tavern and inn is **Tally Ho!** ("decorating Tally Ho!", "found for Tally Ho!: …", "dealers … will call at Tally Ho!"), the village **Kariaston** ("the Kariaston market", "Kariaston ale", "made in Kariaston"), the cook **Boog**. Only player-facing English and the docs changed (CLAUDE.md, GDD Decided 35, the story outline); ids and asset names keep the old words (`gunta`, `brackenford_ale`, `Supply_BrackenfordMarket`, `Gunta.prefab`), so saves are untouched. Earlier entries in this file keep the names they were written with.
- **Boog** is the Goblin Sapper from All Exclusives, used as drawn (idle 20 frames, its 10-frame run as his walk, 100 ms, with its own shadow); imported to `Assets/ThirdParty/Minifantasy/GoblinSapper/`. Gunta's derived sheets' imported copies and her animation set were removed (the sources stay in `Tools/characters/derived/`). ASSET_MAP updated.
- **Safe ground:** Essence doesn't drain in the rope room or the hole room (`FloorNode.PausesEssenceDrain`); damage still applies, and walking between rooms already cost nothing. GDD Decided 36.
- **Tests:** EditMode 565/565, PlayMode 204 passed, 0 failed, plus 18 explicit captures (new: every generated rope and hole room pauses the drain and no other kind does; Essence drains in an ordinary room and holds at the hole down; the cook drawn from the Goblin Sapper's sheets; the curio line says "found for Tally Ho!").

### 4f complete (approved and signed off 2026-10-06)

4f, Tavern Stage 1 content and the customization foundation, is complete: Checkpoints A–D, the UI foundation pass (the type scale), and the names change (Tally Ho!, Kariaston, Boog) with safe ground in the Hollows. No further 4f feature work; only fixes for regressions found later.

- **Final commit:** `64ddf4d` (Tally Ho!, Kariaston and Boog; no Essence drain at the rope and the hole), on `origin/main`.
- **Final tests:** EditMode 565/565; PlayMode 204 passed, 0 failed, plus 18 explicit capture fixtures (222 in all).
- **Web:** the final build (`64ddf4d`) compiles and builds; the last full browser check was the type pass (`4958c28`, 1280×720, no console errors or warnings), which followed the full Checkpoint D smoke test (v6 migration, Continue, market, Prep, Butcher Block by hand and by the cook, a met special request, service, Results, Night, a delve and extraction, decorating, the guest room, reloads).
- **Save version:** 7.
- **Known issues carried forward:**
  - `EveningEstimate` is a comparison tool, not a simulation.
  - At a very low frame rate (a background browser tab), long staff walks lean on the 8 s step-to; enemies share the pathfinding action and weren't reviewed for the same overshoot.
  - Accented capitals and CJK glyphs are 11–12 px tall on a 12-px line and touch the line above (a 14-px line in the type scale when such a locale arrives).
  - The tavern HUD's gold fits four digits at 2×.
  - The market's "not enough gold." sits close to its panel's edge.
  - The oversized lanterns (subjective), the patrons' "!" not aimed at the tusks, no sitting or eating poses for Boog and Pip.
  - Code identifiers keep the old names (`gunta`, `brackenford_ale`, `Supply_BrackenfordMarket`, `Gunta.prefab`); comments still say Gunta in places.
- **Milestone tag:** `milestone-4f` (annotated, on `f7812b3`, pushed 2026-10-06). Later milestones: `milestone-4g`, `milestone-4h`…

Adjustments: the tavern's walkable grid can be explicitly invalidated and rebuilt when the furniture layout changes (in 4c it only builds at scene load; placement itself is 4f), so 4f doesn't have to replace an immutable-layout assumption. `LookTest_Tavern` stays untouched as the 4a baseline.

### 4g plan (approved 2026-10-06)

`docs/PLAN_4G.md`, approved with the owner's decisions (recorded at its top): Love/Hate is the relationship backend (Affinity and Respect only); conversations are authored in the Dialogue System's editor (D1); the first quest is **Boog's Bomb**; quest objects are lost on death and offered again while the quest is active (D6); no keeper portrait (D4); old saves skip the opening through explicit story state (D7). Checkpoint A = Steps 1–3, B = 4–6, C = 7–8.

### 4g Checkpoint A: story technology, the dialogue box, characters and Love/Hate (2026-10-06; approved)

- **Packages (the owner's import):** Dialogue System 2.2.74, Quest Machine 1.2.74, Love/Hate 1.10.74.1, one Common, in `Assets/Plugins/Pixel Crushers`. Added only their official assembly-definition packages; the Quest Machine demo removed at the owner's request. No support packages installed (`docs/THIRD_PARTY.md`). Clean post-import baseline before any 4g code: EditMode 565/565; PlayMode 204 passed, 0 failed, 18 explicit.
- **Adapters (`Hearthdelve.Story`):** the `StoryHost` in Boot (with the Dialogue Manager, Quest Machine and the Love/Hate faction manager); `RelationshipAdapter` (social stand-ins keyed by character id; deeds committed to who learns of them; Respect through Love/Hate's `EvaluateRumor` hook; memories on the game-day clock); `QuestAdapter` (gives quests, sends facts as messages, records the journal); `DialogueAdapter` and the `HH_` Lua functions. Gameplay reaches them only through facts and `StoryServices`; `StoryTests` checks that no gameplay assembly references Pixel Crushers.
- **Save version 8:** the story block (the opening flag, the keeper's profile, relationships in our own shape, the Dialogue System's and Quest Machine's recorded state). A version 7 save continues with its opening marked done, as Bram, with nothing inferred.
- **The dialogue box (`Hearthdelve.Story.Presentation`):** uGUI + STM, Silver 1×, 312×74 at the bottom of 320×180: a 2× portrait (talking while the line is revealed, blinking), the name, four lines, ▼ when it waits; choices in a panel above with a ‣ beside the focused one. Enter, Space, E, A or a click finishes the reveal, then moves on; choices by stick, d-pad, arrows, W/S, hover and click. Opening it takes the UI map and pauses the world; closing gives back exactly the maps that were on. Text from the Dialogue string table.
- **Characters:** `CharacterDefinition` for the keeper, Boog and Pip (linked from the staff); portraits composed from the Portrait Generator's layers (`Tools/portraits`, `docs/ASSET_MAP.md`).
- **The proof:** the troll falls, the tusks come home and are hung in Decorate Mode; `TrophyDisplayed` → `displayed_trophy` → Boog: affinity +21.5, respect +13.5, remembered; Pip: affinity +29.4, respect +0, remembered. Boog's conversation takes the tusks branch; through sleep (a scene change), a save, quitting and Continue, the values, the memory and the branch are the same. The same fact completes the Quest Machine proof quest ("something with teeth", given in Boog's conversation), whose state survives Continue. A new game starts the story afresh.
- **Talking in play:** walk up to Boog or Pip during service (standing still, hands free) and press E. Debug builds: F1 talks to Boog in any tavern phase; Shift+F1 brings the tusks home again for a repeat.
- **Tests:** EditMode **585/585**. PlayMode **209 passed, 0 failed, 19 explicit** (228; the explicit 19 are the capture fixtures, now with `StoryCaptures`). An earlier full run had two room-snapshot tests seeing the staff's new talk interactables; they now ignore them.
- **Web (fresh development build, 2026-10-06):** continued the owner's 4f save (version 7, day 6) straight into the daytime, as Bram, with no opening; Boog's offer by F1; choices by hover, W/S and arrows (with the ‣ following the focus) and Enter; the quest given and its "still bare" branch (Quest Machine's real end-of-frame listener path); Shift+F1, the homecoming in Decorate, the tusks hung: Boog +21.5 affinity, +13.5 respect, Pip +29.4, +0, both remembered, as in PlayMode; Boog's tusks branch; the save written as version 8 with the story block; reload, Continue, the same branch. No console errors or warnings. The owner's save was backed up first and restored byte for byte. Not checked in the browser: a gamepad (covered by PlayMode tests), talking during service by walking up to Boog.
- **Known issues and risks before Checkpoint B:** Quest Machine's message conditions start listening at the end of a frame, which batch-mode tests never reach (the tests add the listener as Quest Machine would; the web check covers the real path). STM draws U+25B6 ▶ as an emoji, so the pointer is ‣ (a test now forbids emoji-range characters). The dialogue box covers the tavern HUD's lower values while open. A save made before the tusks went up can't remember them (Shift+F1 to replay). Boog's lines are temporary proof writing. In a hidden browser tab (about 3 fps) dialogue still works but feels slow.

### Orik replaces Pip (2026-10-06, the owner's request, after Checkpoint A)

The server and bookkeeper is now **Orik**, a dwarf with a ginger beard (he; GDD Decided 43). His look is the Creatures pack's yellow-bearded dwarf (ginger hair and beard as drawn, brown belt, green clothes; `Tools/characters/staff_looks.py`); his portrait a ginger-bearded dwarf (`Tools/portraits/pip.json`). His name in the Content table and "from Orik's ledger" in the UI; the Dialogue System actor and conversation are renamed (`Orik`, `Orik/Talk`) by a one-off migration in the story builder, lines and Guids unchanged; code constants `StaffIds.Orik`/`CharacterIds.Orik`. The stable id stays `pip` (saves, staff, Love/Hate faction, portrait recipe), and `Pip.prefab`/`Staff_Pip.asset` keep their names. Entries above written before the change still say Pip.

### 4g Checkpoint B: the keeper, the Act I opening and Boog's Bomb (2026-10-06; approved)

Checkpoint A was signed off on 2026-10-06. Details as built are in `docs/PLAN_4G.md` ("As built: Checkpoint B").

- **The builder boundary:** conversations are seeded once and logged (`Data/Story/DialogueSeeds.txt`); the node editor owns them from then on (an edit, rename or deletion stands). Checkpoint A's proof conversations were replaced only because they were unedited; the proof quest is retired (a save that has it drops it on load).
- **Step 4, the keeper:** New Game → the creator (MainMenu) → arrival day. A name (16 letters; typed, or the letters on a gamepad), four complete bodies (townsfolk, warrior, dwarf, orc), Minifantasy colourways for skin, hair and clothes with a live, turning preview; worn in the tavern and the Hollows; the name reaches dialogue (`HH_PlayerName`).
- **Step 5, the opening:** arrival (`Act1/Arrival`: Orik, Boog, Tamsin missing below, the empty storeroom) with the keeper on foot, then the cellar hatch; the first delve's one-time prompts (move and aim, attack and roll, Essence, the satchel, the finisher, the rope); the homecoming (`Act1/Homecoming`); the first evening's prep (`Act1/FirstEvening`) and takings (`Act1/FirstTakings`), which hand over to Boog's question. Stages are saved (`OpeningStage`).
- **Step 6, Boog's Bomb:** a real Quest Machine quest and a real conversation (`Boog/Bomb`): accept, ask, or not now (it stays open). The bomb lies in the second fight cleared on the Cellars' first floor; taken home by extraction, lost by a death and found again later (never in the satchel or the Lockbox); handed over for 60 gold and a deed done for Boog alone (`returned_boogs_bomb`: his Affinity and Respect rise; remembered). Persists through save, quit and Continue.
- **Typewriter:** one speed setting (`Data/Story/DialogueSettings.asset`, 45 characters a second); confirm while revealing finishes the line and stops; the next moves on; choices unlock only once the confirm that brought them up is let go.
- **Save version 9:** the opening's stage, creation done, prompts seen, the keeper's palette, quest objects. Version 8 saves (Checkpoint A's playtests) skip creation, the opening and the prompts; version 7 saves as before.
- **Found and fixed on the way:** the opening's first conversation could start under the scene's fade-in (the world pauses while talking, so the cover stayed up and took clicks): beats now wait for the reveal. In the creator, the Enter that opened the letters also closed them in the same frame, and Enter on the letters typed the focused letter as it closed them.
- **Tests (closeout, 2026-10-06):** EditMode **614/614**. PlayMode **214 passed, 0 failed, 22 explicit capture fixtures skipped (236)**.
- **Web:** a fresh development build (139 MB) loads; the owner's own version 8 save (day 9) continued straight into the daytime with no creator, arrival or prompts, dropping Checkpoint A's retired quest with one warning and no errors; New Game asked before replacing the save and opened the creator, crisp at the browser's scale; naming worked through the letters. The full playthrough (the opening, the bomb) wasn't done in the browser: the tab was hidden, so it ran no frames and synthetic keys were lost. The PlayMode tests cover that path. The owner's save was backed up first and restored byte for byte.
- **The intermittent finisher test (fixed in the closeout):** `HarvestFinisherTests.ALowFreshlyHitEnemy_CanBeFinished_ForPremiumParts` spawned the slime one tile beside the keeper, and the harvest's landing spot is random (time-seeded `System.Random`); on some rolls the part landed within the keeper's pickup reach and went into the satchel before the test looked at the floor. Reproduced deterministically with scripted rolls (one part, angle 255°, reach 0.66: floor 0, satchel 1). A pre-existing 4e test flaw, not a Checkpoint B regression, nor shared state or teardown leakage; the game's behaviour (a part that lands on you is picked up) is right. Fix: the slime is spawned a tile below the keeper (parts land below the body, so no spot reaches the keeper), and the test now runs through scripted extreme rolls and a fixed seed, also asserting none went into the satchel. With the old placement the strengthened test fails on the nearest roll; with the new one it passed 10/10, the fixture 5/5 (×5). The two troll tests that leave the keeper where it starts are 16 tiles from the troll.
- **Known issues:** the bomb is small on the floor (it glows); the dialogue box covers the night summary's lower half; in batch captures Silver's "l" can smear to look like "'". Carried from Checkpoint A: Quest Machine's message conditions start at the end of a frame (batch tests add the listener as Quest Machine would).
- **Waiting for the owner's acceptance checks (not done until the owner reports them):**
  1. In the Unity editor, open Boog/Bomb in the Dialogue System's node editor (*Tools → Pixel Crushers → Dialogue System → Dialogue Editor*, or *Hearthdelve → Story → Capture Boog's Bomb Graph*, which also saves a picture to `BatchLogs/story/DialogueEditor_BoogBomb.png`); edit a line, a condition or a link; run *Hearthdelve → Story → Update Story Content* and check the edit is still there; play it (F1 talks to Boog in a debug build); restore it.
  2. In the web build (`Builds/Web`, served with no-cache headers), with the tab visible: New Game → the creator (keyboard, mouse and gamepad) → arrival (Orik and Boog, the typewriter: confirm finishes a line, the next moves on) → the hatch → the first delve's prompts → home → the first evening → the takings → Boog's question → accept → find the bomb (the second fight on the Cellars' first floor) → die with it or extract → hand it over → save, quit, Continue.

### 4g closeout patch: Old Phi replaces Old Tamsin (2026-10-06)

The owner playtested Checkpoint C end to end in the browser ("it's fine") and asked for one narrative-canon patch before sign-off.

- **Canon:** the former proprietor of Tally Ho! is **Phi'rai, usually called Old Phi**, a drow, once of **the Fortunate Five**; she rebuilt the tavern with Orik, left for many years, came home decades later and sought him out to hire him again, then vanished into the Hollows (GDD Decided 52–53, CLAUDE.md). Orik's loyalty is now a lifelong friend's.
- **Dialogue:** nine lines changed in place (the arrival, the homecoming, the first takings, Boog's bomb, Orik's troll and wish callbacks, Orik's question); Orik calls her Phi, Boog calls her Old Phi, and the keeper, who knows her only from the letter, asks for Phi'rai. Orik's old "hired me for a week, eleven years ago" became three short lines, heard only if you ask: the rebuilding and the Fortunate Five, his years keeping it open alone, her coming home to find him. Applied once by a guarded batch edit through the Dialogue System's API (then deleted); the graphs stay yours to edit, and the seed code matches.
- **Nothing migrates:** no stable id, save field, quest or Dialogue System variable ever named her (no character asset existed), so v7, v8 and v9 saves load as before and no story or quest state resets.
- **Validation:** Tamsin is now an old name (string tables, actors and every dialogue entry); CLAUDE.md and the GDD may name her only where they record the change; Orik's history branch is checked; every line fits its box. Older plans and progress notes keep the name they were written with.
- **Tests:** EditMode **627/627**. PlayMode **219 passed, 0 failed**, 24 explicit capture fixtures skipped (243), including the arrival, troll and bomb conversations with the new lines, Continue, and the v7/v8/v9 round trips.
- **Web:** not rebuilt for this patch (dialogue text and docs only; the owner chose to stop here). The served build at `5dd8a4d` still has the Tamsin lines; rebuild before the next browser session.

### 4g Checkpoint C: relationship reactivity, the Act I slice and 4g closeout (2026-10-06, complete; playtested by the owner)

Checkpoint B was signed off on 2026-10-06: you playtested it and verified the node-editor workflow (your arrival line, "ahh, you must be …!", is kept). Details as built are in `docs/PLAN_4G.md` ("As built: Checkpoint C").

- **Step 7, deeds:** `displayed_trophy`, `returned_boogs_bomb`, and three new ones from existing facts, each only when remarkable: `felled_larder_troll` (the troll's first fall), `kept_a_wish` (a request met at quality 0.9+), `fine_butchery` (the keeper's own cut at 0.9+; Boog's business alone). Boog and Orik read each through their own values; repeats fade 1, ½, ¼, ⅒, then nothing.
- **Step 7, dialogue:** each character opens a hub (critical story and quest → one-time callbacks to what they remember → their everyday conversation). Boog: the troll (with his bomb, if he remembers it), his bomb, a wish kept, a clean cut. Orik: the troll and Tamsin, a wish kept. No numbers shown anywhere.
- **Fixed on the way:** the tusks remark repeated forever above Boog's everyday branch, hiding the bomb offer if the tusks went up first (and Orik's tusks and incident-book lines hid his greeting): all said once now. An unset Dialogue System variable isn't nil to Lua's `not`, so once-only conditions compare with `~= true`. Respect's repeat damping read one count too far. Orik no longer says "1 parts".
- **Step 8, presentation:** the tavern's day panels step aside while someone talks (the dialogue box covered the night's summary); Boog's bomb has the target arrow and a stronger glow. No quest journal (deferred to a later UI/daytime milestone).
- **Save:** still version 9; v7 → 9, v8 → 9 and v9 round trips covered by EditMode and PlayMode tests (no duplicate deeds, rewards paid once, callbacks not repeated, memories kept, Quest Machine not replayed).
- **Tests (2026-10-06):** EditMode **625/625**. PlayMode **219 passed, 0 failed**, 24 explicit capture fixtures skipped (243). The intermittent finisher test was fixed in the Checkpoint B closeout (`edf8234`).
- **Web:** a fresh development build (`5dd8a4d`). Browser pass in a visible tab (~145 fps): New Game, the creator, the arrival (typewriter: a press mid-reveal finishes the line, the next advances; choices by keyboard), the first delve, the homecoming over the stepped-aside night panel, Continue after a reload, a full extraction, service with Boog on the grill, the first takings and Boog's offer accepted, and the bomb found in the second cleared room ("found: Boog's bomb!"). No new console errors (only the browser extension's own messages). The owner completed the rest of the playthrough themselves and reported it fine (2026-10-06).
- **Found in the browser (balance, not changed):** if the first delve ends in death, the keeper has no food and 0 gold, so the first evening has to stay shut, and the first takings and Boog's offer wait at least a day. Worth a look when balancing the opening.
- **Dungeon debug keys in the day loop:** F4 (refill Essence) and F6 (god mode) now also work in the Dungeon scene of the real day loop, in development builds only (`DungeonDebugKeys`).

### The Kariaston cast (2026-10-07, the owner's request; docs only)

Seven people join the cast (GDD §2.10, Decided 51): villagers **Kaloren Frosthand**, **Maximo** (Kariaston named for his lost friend **Karias**), **Grim and Ogrin**, **Bart**, with **Orik** confirmed; and the first **Hollowers**, **Gimp** and **Glimmer**. Character roots drawn from the owner's campaign notes (Maximo's tannery past, his songs and speeches, his mentoring of Karias; Gimp's gunpowder and friendship with Boog), without importing the campaigns' wider lore. A **4g Checkpoint D** (Steps 9–12: the cast as data, Gimp at the bar after Boog's Bomb, one Glimmer glimpse in the Cellars, the village named in Boog's and Orik's talk) is proposed in `docs/PLAN_4G.md`, awaiting approval; the alternative is closing 4g as built and taking the cast to 4h, whose four households would be Maximo, Kaloren, Grim and Ogrin, and Bart. Open questions for the owner: GDD §13 Open 12 (when Kariaston got its name, Karias elf or half-elf, Kaloren's secret, Glimmer and the Warden Below, close names, Gimp's kind, firearms). Nothing built.

**Old Phi (2026-10-07, the owner's request):** Old Tamsin is now **Old Phi** (Phi'rai, a drow, of the Fortunate Five; GDD Decided 52). Entries above keep the old name. *(Done in the 4g closeout patch: the nine lines now say Phi, and no stable id ever named her.)*

**Orik's history (2026-10-07, the owner's request):** Phi's old friend; ran Tally Ho! in her absence, left when the Hollows grew too dangerous, sought out and rehired by Phi (GDD Decided 53). *(Replaced in the 4g closeout patch at the owner's request: three short lines, heard only if asked.)*

### 4h Checkpoint A: I can live here (2026-10-07, approved)

The free daytime is walked, not a panel. The keeper wakes upstairs by the bed, walks down, and goes out of Tally Ho!'s front door into **Kariaston** (its own scene, loaded beside the tavern for the daytime; the door is a short fade). A soft **surface clock** runs from 8:00 am to 5:00 pm in about 35 real minutes and stands still for talking, menus, transitions, decorating and lost focus. At five the market packs up, the light turns gold, and Orik mentions it once; nothing forces the evening.

Every Morning-panel function moved to a place: the storeroom shelves (the stock), the Grill and the Tap (tonight's delve meal), the market cart in the square (8–5), the Decorate key indoors (Tab / View, shown under the clock), and the menu board by the door ("begin evening prep?"). Inspectables: Phi's portrait upstairs and the Karias memorial, one line each from the node editor.

Kariaston, 72×48 tiles: Tally Ho! at the top with its garden (four empty beds), the square (memorial, well, market), Maximo's house, Bart's wagon, Grim and Ogrin's cottage, Kaloren's tower, three fenced empty plots; nobody lives in it yet (C). Nothing from B–D: no Vigor, crops, save version 10 or mid-day saves, villagers or schedules.

**After your playtest (2026-10-07):** the clock runs 3× faster (8 am to 5 pm in about 12 minutes), its face has room for its digits, Kariaston has a pond between Maximo's house and the garden, the storeroom shelves are furniture you can move in Decorate Mode (starting below the barrels, out of the stairs' way), and the stairs' trigger covers the whole gap beside the stew pot (the stuck-stairs report wasn't reproduced; that was the likeliest cause).

Details, deviations and tests: `docs/PLAN_4H.md`, *As built: Checkpoint A*. Art: `docs/ASSET_MAP.md`, *Kariaston and Tally Ho!'s daytime places*.

**Known issues (Checkpoint A):**
- **The day restarts at 8:00 on Continue**: the clock isn't saved until B's version 10.
- **The village is empty**: no villagers until C; the houses are silhouettes, with no doors to knock on.
- **Seeded but unplaced:** `Inspect/Tankards` and `Inspect/Hatch` exist in the dialogue database, waiting for their spots.
- **The decorate reminder overlaps the room's top-left corner** in the main tavern room (under the clock, over the bar's back wall); upstairs and outdoors it's clear. Cosmetic; its place is a playtest call.
- **The tall market cart can hide the keeper** standing right behind it (correct Y-sorting, but the cart's canopy covers a whole figure).

### 4h Checkpoint B: a day's work (2026-10-07, approved)

Checkpoint A approved (2026-10-07). B adds **Vigor** (six pips a day beside the clock; planting a bed costs 2, tending 1, harvesting nothing; refilled by sleep; at 0 only strenuous work is refused) and the **garden**: four beds behind Tally Ho! (`garden_1`–`garden_4`) growing herbs (2 days, 3 herbs), onions (3 days, 3 onions) or barley (4 days, 2 malt), free seeds, tending once a day for a Fine harvest (at least half the growing days), forgiving (untended crops still grow; ripe ones wait), harvests straight into the storeroom as ordinary stacks. **Save version 10** (world seed, the day's minute and Vigor, the garden); mid-day Continue resumes at the saved minute; every garden action and *begin prep* save. Checkpoint A carry-overs: the decorate reminder moved to the bottom-left corner; the market cart's body reaches up behind its canopy.

Tests: EditMode 693/693, PlayMode 235 passed, 0 failed (25 explicit captures and a diagnosis skipped). Web: smoke-tested on your own save (migrated v9 → v10, planted, tended, reloaded, slept three nights); harvest and cooking checked in PlayMode only (hidden tab). Details, deviations and the playtest checklist: `docs/PLAN_4H.md`, *As built: Checkpoint B*.

**Known issues (Checkpoint B):**
- **No homegrown provenance** on ingredients (a deliberate stop: an origin tag would change stack identity everywhere); `ServedHomegrown` waits for your call.
- **A tended bed's grass rim darkens with its soil** (the tint covers the bed's whole autotile, edges included).
- **F8 through the evening** can carry the first-evening introduction (Orik, Boog) onto the delve's result screen (the debug skip, not play).

**After your B playtest (2026-10-07):** doorways go through when you push their way from anywhere in them (wider triggers; no bounce-back); the day is about 4 minutes; **Musashi** (your new canon: an elf of the Fortunate Five who lost his taste to a curse from the Hollows, brother Toshi missing below) stands at the market cart and talks (`Musashi/Hub`, a first draft for the node editor). Grim no longer runs the market; what he does now is an open question (GDD Open 12).

**Open question for you:** homegrown provenance (above): worth changing ingredient identity for Boog's callbacks, or tracked another way (a day's harvest log) later?

### 4h Checkpoint C: the village has people (2026-10-07, approved)

Checkpoint B approved (2026-10-07). C puts **Maximo, Kaloren, Grim, Ogrin and Bart** in Kariaston on a few broad beats a day (morning, midday, afternoon, from five), with **Musashi** at his cart and **Boog and Orik** at their posts under the same schedules. Where everyone is follows from the day, the clock, the story and the v10 world seed, and is never saved (`ScheduleDefinition`, `ScheduleRules`, `VillageDays`, `VillagePresence`). They walk when you can see them and are simply there when you can't; Maximo walks into Tally Ho! for lunch and out again (one of him at a time). **Kaloren brings Ogrin herbs every third day** (9:30, to the cottage door), wordless and visible, whether or not you're there. Ogrin has seeded good days (out: yard, pond, the green) and bad days (in bed, talked to through his window, a light inside). Five first-draft conversations (`Maximo/Hub` … `Bart/Hub`) in 4g's hub shape, seeded once for the node editor; portraits from the Portrait Generator. Details, schedules, art and deviations: `docs/PLAN_4H.md`, *As built: Checkpoint C*.

Tests: EditMode 724/724, PlayMode 246 passed, 0 failed (26 explicit captures and a diagnosis skipped). Web: built and smoke-tested on your own save (backed up and restored): the updated homecoming line, waking in your room with the hatch, Maximo walking into Tally Ho! for lunch at eleven and his first meeting with portrait, the clock held while talking, no console errors (the smoke test found one: a helper named `Update` with parameters, fixed and rebuilt). The village walk-through, the herb visit and a save/Continue in the browser are left to your playtest (they're covered in PlayMode).

**Deviations:** Bart is A Myriad of NPCs' orc in a cowboy hat, not the Wise Orc (an armoured warlord with twin swords at game scale). Ogrin's portrait is a halfling stand-in (no child parts). Activities and the herbs are shown with faces and icons, not text barks (D's layer). Grim's livelihood stays open.

**Known issues (Checkpoint C):** villagers walk through each other (their spots are apart); Kariaston's grid bakes on the first walk (a small hitch, once); a villager already walking keeps walking when Decorate Mode opens.

### 4h Checkpoint D: this is a community (2026-10-07; approved 2026-10-08 in the final 4h playtest)

Checkpoint C approved (2026-10-07). D, following your brief:
- **Gimp's night:** the first morning after a delve of your own (day 3 or later), he climbs out of the hatch in your room in the dark, by his old arrangement with Phi, asks where she is, and goes back down. Once per save; the morning (garden, Vigor, clock, save) is untouched. Afterwards he comes up to see Boog on irregular afternoons (seeded, never two days running), down the stairs from the hatch. He detests Maximo and won't say why.
- **The village among itself:** seven short overheard exchanges (Grim and Ogrin, Kaloren and Grim, Bart and Ogrin, Bart and Musashi, Maximo and Musashi, Maximo and Orik, Gimp and Boog), each pair once a day, with a quiet of 35 real seconds between any two, one bubble at a time, never during a conversation, a menu or Decorate Mode. Callbacks to the troll and Boog's bomb.
- **Familiar faces at dinner:** none to two named villagers an evening (seeded), as ordinary customers in their own looks.
- **Glimmer's light:** a small pale light at Ogrin's window on some evenings (about one in four, after Gimp has come up), unexplained; Ogrin calls it "my light" if asked.
- **C's issues:** villagers give way to each other, Kariaston's grid is baked at load, and nobody walks through Decorate Mode.

Tests: EditMode 750/750; PlayMode 252 passed and 2 failed in the full run (4g tests that didn't yet know about Gimp: his stand-in, and his night interrupting their day 3), both fixed and passing on rerun; 27 explicit captures skipped. Web: built (153 MB) and smoke-tested on your own save (backed up and restored): Continue mid-morning with no night scene, evening prep, service with Maximo walking in as a familiar face, the delve, the night, sleep, then Gimp's night on the next morning (dark room, lantern, silence, Vigor refilled, the clock held at 8:00), the morning restored, the beat saved, and a reload and Continue without a replay; no console errors. The browser tab was hidden (throttled), so walking the village, the overheard exchanges and Glimmer's light were checked in PlayMode only. Details, deviations and the final playtest checklist: `docs/PLAN_4H.md`, *As built: Checkpoint D*.

**Approved future direction (not built):** a fantasy calendar, festivals, smaller village events, birthdays and eventually seasons (GDD §11.1), as a post-4h milestone to plan.
**Open story question:** why Gimp hates Maximo, and whether he was at the sealing (GDD §13 Open 12); 4h only shows the hostility.

### Background music (2026-10-07, after Checkpoint C)

At the owner's request: HeatleyBros tracks, looping, crossfading over 1.5 s; music at 56% (a quarter down, then another after the owner found it loud), and **sound effects in the Hollows a quarter down** (through the listener during the delve; the music keeps its own level). **Arrival day is silent**: a new game's first music is "Otherworld", going down into the Hollows. The free day (Tally Ho! and Kariaston) plays "Quirkii"; Decorate Mode "Continue" (the day's tune pauses and picks up where it was after); the evening (Prep, service, the results) "Coastal Market"; the Cellars "Otherworld" (the only region so far). The menu and the night's summary are quiet. Tuning on `Data/Config/MusicConfig.asset`. **Licence:** an in-game credit with a working link is required before release (`docs/CREDITS.md`; the credits screen is 4i). Open: whether Prep should keep the day's tune instead of the evening's.

### 4h complete (approved and signed off 2026-10-08)

The owner approved Checkpoint D after the final 4h playtest, and with it **Phase 4h**: A "I can live here", B "A day's work" and C "The village has people" were approved on 2026-10-07, D "This is a community" on 2026-10-08. Also approved: the background music, the audio-listener and music-loading fix (Boot keeps a fallback listener between scene sets; music starts only once its clip has loaded; 2026-10-08) and Quirkii a quarter under the other tracks (each track now has its own level on `MusicConfig`).

**Verified at the final code commit** (`ffc06999`): EditMode **750/750**; PlayMode **255 passed, 0 failed**, 28 explicit captures skipped. Web: a development build of that commit (153 MB, 2026-10-08) smoke-tested on the owner's save (menu, Continue into day 3, the day's tune playing once at normal speed, no console errors; nothing done in the save). Earlier the same day, the 4h D smoke (Gimp's night end to end) and a new-game music check (Otherworld, then Quirkii, each once at normal speed) on the builds before the music changes. Tagged **`milestone-4h`**.

**Deferred, non-blocking** (the full table is in `docs/PLAN_4H.md`, *Deferred from 4h*): Bart's performance at dinner; daytime Visitors; Kaloren's not eating; Gimp's rifle layer (the firearms question); a walkable night; the remaining inspectables (Phi's chair, the trophy, Orik's incident book); Ogrin's stand-in portrait; wordless daytime activities; Prep's music; why Gimp hates Maximo (open story question); the bark bubble's browser position (owner-playtested, not separately browser-checked); the calendar, festivals, birthdays and seasons (Phase 5).

### After 4h: two fixes from the owner's runs (2026-10-08)

- **A slime could stay dark:** two sprite flashes on one enemy (its hit and another moment) each remembered the sprite's "own" colour, so one starting while the other showed took the other's tint for it and put that back. `MMF_SpriteFlash` now keeps one record per sprite (the resting colour, taken when no flash shows, restored when the last ends); the troll's frenzy tint goes through it (`SetResting`) so a hit flash can't undo it. `SpriteFlashTests`.
- **No idle head turn for the keeper:** Minifantasy's 16-frame idles turn the head over frames 8–10 and blink at 3, 5 and 13; the keeper's idles (all four bodies and their shadows) now hold frame 7 through 8–10, keeping the blinks and the loop's timing (`LookTestContent.StillHead`; `KeeperIdleTests`). The 4a look scenes share the townsfolk set, so their idle changes too. Villagers and customers keep theirs.
- **Considered and not built (the owner's call):** a campfire room between the last floor and the Larder Troll's arena.

### Overheard bubbles and the music (2026-10-08, the owner's playtest)

- **An exchange in the square followed the keeper into Tally Ho!** Overheard exchanges now end the moment the keeper goes through a door or the stairs (`KeeperEnteredArea`), end if a speaker goes off screen, and never start (or get used up for the day) when the first speaker isn't in view. The bubble also always uses the camera showing the world now, not one remembered from before a scene change.
- **Short answers went by too fast:** every line now stays up at least 3.5 s (was 2.4), with 0.6 s between lines (was 0.35); tunable on the bubble (`AmbientBarks`, in Boot).
- **Music (the owner's call):** "Quirkii" only out in Kariaston by day; Tally Ho! (and the keeper's room) quiet by day except Decorate Mode's "Continue"; Prep quiet; "Coastal Market" from the moment service starts, through the results; every tune waits 1.5 s before fading in (`MusicConfig.startDelay`).
- Tests: `Overheard_Stops_WhenTheKeeperGoesIndoors…`, the music rules (EditMode) and `TheMusic_FollowsTheDay…` (PlayMode) rewritten to the new rules.

### Kariaston: collision by what things are, and Ogrin's lit window (2026-10-08, the owner's request)

- **The pond was never solid:** its tilemap collider made 40 cell shapes, but merging them through the composite collider produced none. Now `DressingCollision` (on `Kariaston/Gameplay`, tool-owned) makes every tilemap holding colliding tiles collide cell by cell, as the scene loads: **water painted anywhere later is solid too** (and the villagers' walking grid paths round it). Ready for fishing from the bank.
- **Fences, sign posts and Tally Ho!'s board had no collision:** the same component gives every sprite showing one of those drawings a footprint fitted to that drawing's own base, measured from its pixels (a fence's whole run, a sign's post, the board's legs), as a child that moves with it. **No walls placed at spots:** a fence copied, moved or newly dragged in is solid wherever it stands; a prop that already carries its own collider is left alone. To make another drawing solid, add it in `KariastonBuilder.BuildDressingCollision` and run the village updater.
- **Ogrin's window is lit while he's in bed and dark while he's out:** the cottage's own window glass in the lamp posts' flame colours (`Tools/village/lit_window.py`, a derived overlay on the cottage, added once to the hand-owned scene by `KariastonBuilder.AddLitWindowBatch`), switched by his window anchor (`LitWindow`). The faint point light stays for dusk.
- Tests: `TheWater_IsSolid…`, `EveryFence_Sign_AndTheTavernBoard_IsSolidAtItsOwnBase`, `AFencePutAnywhere_IsSolid…`, `OgrinsWindow_IsLitWhileHesInBed…`. Not made solid (not asked): the haystack, bales, scarecrow, bucket, crate, trough and flower boxes; each is one line in the list if wanted.

### 4i plan (proposed 2026-10-08; approved the same day)

`docs/PLAN_4I.md`: the audit of what a stranger meets (menus, onboarding, saves, settings, audio, input, accessibility, credits, builds, performance, placeholders, playtest readiness) and four checkpoints: **4i-A** first impressions (a main menu that looks like a game, a pause menu, controls reference, device-aware hints, first-day orientation, saves the player can trust), **4i-B** settings and accessibility (persistent options, an audio mixer with volume sliders, feel and vibration, display, text speed, relaxed timing, a colour audit), **4i-C** presentation and polish (real sound effects, the audio balance, the credits screen with HeatleyBros' link, the known presentation bugs), **4i-D** a playtest-ready slice (release Web and Windows builds, a version number, a performance pass, save compatibility, a tester kit and an external playtest, then `milestone-4i`). Ten decisions wait for the owner (`PLAN_4I.md` §3). Nothing of 4i is built.

### 4i-B: settings and accessibility (2026-10-08; signed off 2026-10-08)

**Signed off by the owner (2026-10-08)** after the checklist, except the controller items (no real controller available): "everything works as described". Two notes from the playtest were fixed before sign-off (commit 5d20b427): the web fullscreen line didn't change until the tab was reopened (the browser switches a moment after it's asked; the line now shows the choice at once and follows the browser), and Musashi drew over the keeper standing in front of him (his figure had no sorting group; `FigureSortingTests` now checks every layered figure). After those fixes: EditMode 805 passed, 0 failed; PlayMode 283 passed, 0 failed. The sign-off's notes on relaxed timing and fast clicks were left blank. The owner has a controller since (2026-10-08): the controller checks (4i-A's, and 4i-B's items 1 and 4) are carried onto 4i-C's playtest checklist. Carried into 4i-C: fast clicks on an Options line. Not tagged: `milestone-4i` waits for 4i-D.

Approved as written (PLAN_4I §4i-B, with D4 and D6) with the owner's details. Details, changes from the plan, the full list of options and the playtest checklist: `docs/PLAN_4I.md`, *As built: 4i-B*. Save version still 10. Not tagged.

- **Options**, from the main menu (between New Game and Controls) and the pause menu: Audio, Feel, Display, Accessibility, Controls (4i-A's page). Controller-friendly lines; every change saved and applied at once.
- **The options file** (`options.json`, IndexedDB on the web), never in the save, read before the first frame, kept through New Game.
- **One audio mixer**: master, music and effects in 5% steps, heard at once. The Hollows' quarter-down on the mixer (the listener is left alone). Every sound routed (54 existing ones in place); no UI slider, since interface sounds are effects.
- **Feel**: shake off / low / full, flashes, hit-stop, vibration with strength and reduced vibration (a sample rumble on each change).
- **Display**: desktop fullscreen or windows in whole multiples of 320×180; the web's fullscreen.
- **Accessibility**: text speed slow / normal / instant, hold to hurry (never past a choice), relaxed cooking timing (the keeper's grill, tap, chopping and Butcher Block; no score penalty), patient customers. Both assists off by default.
- **Carried from 4i-A**: an Esc pressed during a fade opens the pause menu once it's done; the menu's save message on a full-width band.
- **Colour and contrast audit**: the Larder Troll's telegraph is lighter than the floor, not only redder; secondary text tones darkened to 4.5:1 on parchment (`UiPalette`, 196 applied in place); a contrast test over every text.

Tests: EditMode **804 passed, 0 failed** of 805 (one explicit report skipped; the one first-run failure, `Q` and `RB` not yet known as control labels, fixed and rerun), PlayMode **283 passed, 0 failed** (31 explicit captures skipped), including the new `OptionsTests` (20), `ContrastTests` (3) and `OptionsPlayTests` (4). Evening Report unchanged; `RenownAndBalanceTests` pass. Web: a development build (version `0.4i-b`) smoke-tested with the owner's save backed up first and restored exactly afterwards (same six records, the save the same 22,194 bytes): the menu shows Options between New Game and Controls; music lowered to 85% and effects to 90%; after a page reload both were still applied; the web Display tab offers fullscreen only; no console errors. Noticed: clicks on an option line in very quick succession were partly lost (five fast clicks moved music two steps; single clicks step once each), worth a look at a human pace in the playtest. Not driven in the browser (covered in PlayMode): the Hollows' level, relaxed timing, patient customers, text speed, a controller.

### 4i-A: first impressions (2026-10-08; signed off 2026-10-08)

**Signed off by the owner (2026-10-08)** after playing the whole checklist: the menu, the first morning, the hints, pausing in each part of the day, quit and Continue from each moment, switching between keyboard and controller, and 320×180, "all works as described". (The sign-off's note on whether a real controller was used was left blank; a controller check stays on 4i-B's list.) Carried into 4i-B: the menu's save message strip must read clearly over the backdrop (part of the colour and contrast audit), and an Esc pressed while a scene fades in should be queued rather than dropped. Not tagged: `milestone-4i` waits for 4i-D.

Following the approved plan and the owner's 4i brief (D1–D10, `docs/PLAN_4I.md` §5). Details, the phase-by-phase quit audit and the playtest checklist: `docs/PLAN_4I.md`, *As built: 4i-A*.

- **Main menu:** a still of Kariaston and Tally Ho! (rendered from the game at 320×180), Continue / New Game / Controls / Quit (desktop), the version (`0.4i-a`), no more `day {0}, {1}` flash. Options and Credits arrive with 4i-B and 4i-C.
- **Pause menu** (Esc / Start): resume, controls, quit to menu, quit game (desktop). Pauses the game, holds the clock and the keeper, gives back exactly what was on. On foot and on Prep, the results, the night and the delve's result; never over a station, panel, question, conversation, Decorate Mode or story scene (they own Esc / B).
- **Quitting by phase:** the day and the night save and resume at the same moment; the results and the delve's result bank as their buttons would; arrival day, Prep, service and a running delve ask first and leave the last save as it was. No mid-phase serialization; save version still 10.
- **Controls reference:** five pages, keyboard and controller side by side, the device in use lit.
- **Prompts** follow the last meaningful input (no flicker); stations show one binding each.
- **First free morning:** Orik and Boog, once, as the keeper comes downstairs on day 2 (`Act1/FirstMorning`, seeded once); one-time prompts at the garden, the market and the menu board.
- **Safer saves:** the last good save kept as a backup on every write; unreadable saves set aside and explained (Continue offers the backup); newer-version saves explained and never loaded; New Game asks before replacing any save file; a brief "saved" mark.

Tests: EditMode **778/778**; PlayMode **274 passed, 0 failed** (30 explicit captures skipped), including the new `FirstImpressionsTests` and `FirstImpressionsPlayTests` (18). Web: a development build (153 MB) smoke-tested on the owner's save (backed up first and restored exactly afterwards): the new menu (no Quit in the browser, version shown), Continue, Esc to pause and back, the controls page and its paging, quit to menu (the save kept at the same minute, the backup written), and a deliberately damaged save: the menu explained it and Continue loaded the backup. That test **found a real web bug**: the player had exception support "None" since the project began, so any exception, even a caught one, froze the page; fixed (`BuildTools` builds with explicitly thrown exceptions, and the save check no longer throws on damaged files). No console errors. Not driven in the browser (covered in PlayMode): the first-morning conversation, the prompts, Prep, service and the Hollows, and a controller.

### Phase 5 (direction approved; order proposed for discussion)

Approved direction (2026-10-07 and 2026-10-08): a proper **fantasy calendar**; recurring **village festivals** and community events; **villager birthdays**; eventually **seasons**; **fishing** and further optional daytime activities; the **remaining Hollows biomes**; **property expansion** (the owner's wording, "tavern, Sanctuary and Stronghold expansion", conflicts with the Stronghold direction dropped on 2026-10-04 and is an open question: `PLAN_4I.md` §3, decision 10); the **remaining story acts**. Already approved earlier: the Inn and Visitors, settling residents, ranching, farming depth. The calendar creates anticipation, never punishing deadlines, and story-critical events stay reachable; calendar and year lengths, season counts, birthdays and festival dates are not decided until the calendar milestone is planned. Proposed order (`PLAN_4I.md` §4): 5a story revision (design only) → 5b the calendar and the first festival → 5c Biome 2 → 5d the Inn and Visitors → 5e fishing (and foraging) → 5f settling residents → 5g seasons → 5h property expansion, ranching, farming depth → content build-out. Nothing of Phase 5 is built.

### Open design questions (Phase 4)

1. **Protagonist body:** the Human Townsfolk is a stand-in. See `docs/ASSET_MAP.md`.
2. **Patron requests in 4f versus quests in 4g** (raised 2026-10-03): 4f lists "customer requests", but persistent requests for parts or ingredients are now quests owned by Quest Machine (GDD §2.6), which arrives in 4g. Either 4f's requests stay within one evening (special orders owned by the tavern), with persistent requests moving to 4g, or 4f builds them behind a Hearthdelve interface that Quest Machine takes over in 4g. To decide before 4f is planned.
3. ~~**Love/Hate timing**~~: resolved 2026-10-06, in 4g (GDD Decided 37).
4. **Customization decisions** (raised 2026-10-04; GDD §13 Open 9), to prototype or bring back rather than settle silently: free or grid placement; editable walls, floors and doors; whether every functional station moves; ownership and quantity rules; duplicate drops; how decor discoveries survive extraction and death (a curio channel or simpler); whether boss trophies can be lost; the palette technique; furniture stat bonuses; decor's relationship effects; renaming any canonical character.
5. **Village life decisions** (raised 2026-10-04; GDD §13 Open 10), expensive to reverse, to prototype later: a ticking daytime clock or player-controlled phases; whether the nightly delve can be skipped; crops, seasons and farm size; ranch simulation depth; the fishing minigame; village size and layout and the number of villagers; Visitor generation depth; resident eligibility and promotion rules; Inn occupancy rules; building on the three plots; villagers or Visitors as staff; relationship and romance scope; seasons, weather and calendar length.
6. **Raised by the v0.5 direction** (GDD §13 Open 11): whether Morale stays a separate measure or derives from villagers' relationships; where the Night upgrade screen and the breakfast buff go in the new day (4d step 5); freshness tuning now that dungeon parts wait a day; the story revision for Acts II–IV and the canonical cast (GDD §2.9); whether people met in the dungeon can become Visitors.

### Decided at the end of 4a (2026-10-02)

1. **Resolution locked:** 320×180 at 8 PPU (CLAUDE.md, GDD §8.1).
2. **URP 2D lit sprites are the visual baseline.** The look test uses URP's lit sprite material everywhere, with simple representative lighting: in the dungeon, a cool, dim ambient light and two warm torch lights; in the tavern, a warm ambient light and a warm glow over the bar. The existing scenes and prefabs were updated in place (*Hearthdelve → Generate → Update Look Test Lighting*). The values are in `LookTestBuilder` and on the scene lights, so they can be tweaked in the editor. This is not a lighting system yet.
3. **SpriteSet animation** instead of Mecanim, presentation only (CLAUDE.md, GDD §10.1).
4. **Frame pacing:** TDE's `GameManager` defaults to a 300 fps target, which makes a web build run its main loop on a timer instead of `requestAnimationFrame`. The look-test scenes use -1 (the platform default) instead.

### Known issues

- **Character size:** at 320×180 a character is about 4% of screen height. This is the main thing to judge in the look test.
- **Placeholder UI in the look scenes:** the 4a look scenes keep their plain Essence bar and Unity's built-in font, as baselines (the game's scenes have the real HUD and Silver).
- **Look-scene death:** at zero Essence the 4a look rooms still just restart after 2.5 s; the death screen, Lockbox and result screen are on the test floor.
- **Enemy hits don't push the player back** (enemy knockback on the player was left out of 4b's simple stagger; the player's own knockback would need care not to fight input).
- **Shroom Cap and Spore Sac drop from nothing** now (they were the Cellar Shroom's, which became the spider). They wait for the Mushroom People; until then Shroom Skewer, Cellar Kebab, Cellar Stew and Offal Pottage can't be cooked from a delve. The recipe rework is 4f.
- **Enemies only hurt with telegraphed attacks**, so standing in a slime is harmless. A deliberate fairness choice (GDD §4.1); say if you want light contact damage back.
- **Hit-stop needs an `MMTimeManager`:** the test floor and newly generated scenes have one; the 4a look scenes (kept as baselines) don't, so hits there don't freeze.
- **Pathfinding ignores other enemies:** enemies path around walls and props only, and can bunch up on the way to the player.
- **Vendor prefabs with missing references** after the demo trim are listed in `docs/THIRD_PARTY.md`; we don't use them.
- **Data:** the enemies are the slime, bat and spider (plus the look room's training dummy). The Bat Wing icon is a placeholder (a documented art gap in `ASSET_MAP.md`).
- **No dungeon debug panel (developer-tooling gap):** the prototype's F1 panel (god mode, refill or drain Essence, shake and hit-stop toggles, restart) wasn't rebuilt. Not planned as a whole; individual controls come back when they're genuinely useful. The look-test overlay's F2–F4 keys still work.
- ~~**The main menu is a plain panel**~~ (4i-A: the village still and the title).
- ~~**No pause menu**~~ (4i-A built one).
- ~~**Settings aren't saved**~~ (4i-B: Options, in their own file).
- **Options on a Windows build is unchecked** (4i-D's build path; checked in the editor and on the web).
- ~~**Fast repeated clicks on an Options line can be lost on the web**~~ (4i-C: only clicks within one frame, as a browser driver sends them; a human pace steps every click, tested).
- ~~**Options' controls note wraps onto two lines**~~ (4i-C: it names one device, on one line).
- **The window-size line can't preview**: a desktop window change applies at once, with no "keep these settings?" countdown.
- ~~**An unreadable save hides Continue** without a message~~ (4i-A: explained, the backup offered, New Game asks).
- ~~**Station panels' controls lines overflow their boxes**~~ (4i-A: one binding each).
- ~~**The main menu's Continue detail shows `day {0}, {1}`**~~ (4i-A: fixed).
- **No pause menu inside Decorate Mode** (by design: its keys own Esc and Start; leave it, then pause).
- **Music memory on the web:** each track decodes whole (about 65 MB); the day's tune stays decoded under Decorate Mode's (4i-D).
- ~~**In-game credits are missing**~~ (4i-C: the Credits screen with HeatleyBros' link).
- **The day loop starts from `Boot`.** Playing `Tavern`, `Dungeon` or `Dungeon_TestFloor` on its own still gives the standalone evening or floor; playing `MainMenu` on its own loads Boot.
- ~~**No icons for Shroom Cap and Spore Sac**~~ (they've had icons since 4f; found stale in 4i-C).
- **Customers walk through each other:** they don't collide with each other or the player (on purpose: no shoving), and their paths ignore other customers, so two can overlap briefly in an aisle.
- **No sitting pose:** seated customers use their idle pose on the chair.
- **Silver doesn't cover Arabic or Hebrew** (it does cover Latin, Greek, Cyrillic, CJK and Thai). There is deliberately no fallback font; `TextStyleTests` checks every string against the font. A decision for when localization is planned.
- **Silver's license has a budget condition:** CC BY 4.0 (attribution to Poppy Works), but productions over $100,000 USD in total spend or earnings are asked to contact Poppy Works to license it (`docs/THIRD_PARTY.md`). Owner decision before release; no effect on development.
- ~~**The delve's controls line overlaps the satchel row**~~ (4i-C: retired in the day loop).
- ~~**The death screen with an empty satchel** shows its one button right of centre~~ (4i-C: centred).
- **A flaky tavern test:** `TavernServiceTests.Pip_OnServing_CarriesPlatesToWhoeverOrderedThem` failed once in a full PlayMode run during 4d step 3 (dish quality 0.909 against Pip's 0.85 cap) and passed alone three times and in the next full run. Not related to 4d; to look into with the tavern work (4f). It passed in all four complete PlayMode runs during 4d step 5.
- **Run length and the Essence budget are estimates** (4d step 5): the run log (development builds) measures them; your playtest should replace them.
- **The delve meal's drain drinks and slow burn are worth less** at a low drain (0.2/s since the playtest); a playtest question, not changed.
- **The day's story isn't saved:** a night resumed from a save shows only what that session played (the purse and Renown always).
- **Quitting mid-delve** loses that run (by design: no mid-run saves); Continue starts the night's delve again.
- **Not driven in the web smoke test:** combat, power choices, the death screen, the satchel swap and a controller (synthetic input can't fight); PlayMode tests cover them.
- **All sound is placeholder** (`PH_…`, generated), including the tavern feedback pass's moments. Rumble on real controllers is checked by you; web builds have no rumble (haptics no-op).

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

The order since 4d step 5 (GDD §3.1): daytime → evening (the tavern) → the night's delve → night → sleep.

1. **Start:** open `Assets/_Project/Scenes/Boot.unity` (or `MainMenu`, which opens Boot) and press Play.
   - **New Game** starts Day 1 with an empty storeroom and no gold, and goes straight into the first delve (then night, sleep, and day 2's daytime).
   - **Continue** loads the one save slot and resumes where it was saved: daytime (after sleeping), the night's delve (saved when the evening ended; the run starts again from the top), or night (saved when the delve ended).
2. **Daytime (tavern, a placeholder for the village day):**
   - The storeroom and tonight's delve bonuses are shown.
   - **Delve meal (optional, one dish):** pick a Grill or Tap dish the storeroom can make, then play its minigame. It uses one serving's ingredients, and its buff waits for tonight's delve.
     - Grill dishes add max Essence; Tap drinks slow Essence drain.
     - The dish's quality scales the buff. Stews aren't offered.
     - Esc puts the ingredients back.
   - **Open for the evening.**
3. **Evening (tavern):**
   - The usual prep screen and service, cooking from the storeroom.
   - If nothing can be cooked (or you'd rather not open), **stay shut tonight** goes straight to the delve.
   - On the results screen, **close up and head below** banks the takings (payments + tips), saves, and goes down to the Cellars.
4. **The night's delve (the generated Cellars run, `Dungeon`):**
   - Satchel slots and max Essence include your upgrades and the delve meal; run powers start empty.
   - Rooms, rewards, door signs, powers, the hole down and the rope out as in steps 1–4.
   - **Extract** at a rope room (stand at the rope, press **E**): the whole satchel and the run's Gold go home.
   - If Essence runs out, pick one Lockbox slot on the death screen. Only that stack goes home; the run's Gold is lost.
5. **Night (tavern):**
   - The day's summary (the evening, the delve, parts home, banked today), the purse and Renown, and the three **upgrades**, each with a few levels at rising cost.
   - The game **autosaved** as the delve ended, and saves after each purchase.
   - **Sleep:** storeroom stock loses a little freshness overnight, then it's the next daytime (and another autosave).

When `Tavern` or `Dungeon_TestFloor` (or `Dungeon`) is played on its own (no Boot), it behaves as before: a single evening with the debug fill, or a dungeon run that restarts.

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
| F4 | Refill Essence (also in the day loop's Dungeon scene, development builds) |
| F5 | Set Essence to 3 |
| F6 | God mode (also in the day loop's Dungeon scene, development builds) |
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

1. **Stronghold defense events:** still undecided. Not built; the Tavern floor is solid ground so combat could be added. *(Dropped 2026-10-04 with the Stronghold direction, GDD v0.5.)*
2. **Dropped plates:** a dropped plate currently re-queues the order if stock allows, costing the ingredients again; otherwise the customer leaves as sold out. (A dropped spare is just gone.) Is that the right cost?
3. **Staff scope:** one helper covers one station. Should later staff cover multiple stations, or share one?
