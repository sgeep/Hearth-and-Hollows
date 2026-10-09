# Test suite review: redundancy and cost (2026-10-09)

A read-only review of every EditMode and PlayMode test (57 + 54 files, about 28,000 lines), checked against `main` at `95a5bb93` (4i-C closed). **Nothing in the code was changed.** Unity wasn't available, so every time below is an **estimate read from the code** (waits, scene loads, fades, days walked), not a measurement. Treat the seconds as order-of-magnitude; the Unity session should measure before and after.

Confidence: **high** = read directly from the code and checked; **medium** = clear from the code but depends on runtime behaviour; **low** = plausible, worth a look.

---

## 1. The short version

The suite isn't slow because of duplicates. It's slow because about 110 PlayMode tests **boot the whole game and live through it in real time**: scene loads with their fades, a throwaway delve and night just to reach day 2, villagers walking at normal speed, and real days slept through. Duplicates exist, but removing them saves minutes, not tens of minutes.

Estimated wins, biggest first (PlayMode unless noted):

| # | Change | Estimated saving | Confidence |
|---|---|---|---|
| 1 | **Start daytime tests from a written day-2 save + Continue** instead of new game → delve → night → sleep (about 60 calls of the copied `Daytime()` helper in 8 classes) | 5–10 min | medium |
| 2 | **Turn the fades off in the test harness** (`Flow.Transition = null`), keeping them only in the tests about fades | 5–9 min | medium |
| 3 | **Pin the seed in the remaining day-walking tests** (VillageCheckpointC) and lower the two 10-minute timeouts | 1–4 min, and no more random length | high |
| 4 | **Merge tests that pay a full boot for a few assertions** (about 30 candidates, §4) | 6–10 min in total | medium |
| 5 | **One shared scan of the scenes in EditMode** (34 scene opens → 6) | 40–60 s of EditMode | medium |
| 6 | Villager walks and customer walks at timeScale 4 where the walk isn't what's tested | 1–3 min | medium |
| 7 | One shared `BootFixture` with a complete teardown (§3) | makes runs repeatable; unknown time | high (that it's needed) |

Together, items 1–4 plausibly take **15–25 minutes off the hour** without losing what's covered, as long as each area keeps one test on the real path (§6).

### Already fixed since this review started (`6851778d`, `fc5aeacf`)

- **The time-step leak** (found independently by this review and by the 4i-C session): TDE's `MMTimeManager` scaled `Time.maximumDeltaTime` and `fixedDeltaTime` during pauses and hit-stops, and each scene's copy took the changed values as its baseline. Fixed in the game by `TimeBaseline` (a persistent object created before the first scene, restoring the project's values whenever time runs at 1×). This was a real game bug, not only a test problem.
- **Pinned seeds** for `VillageCheckpointDTests`' Gimp visit, Ogrin's light and Ogrin's window (the `seed:` argument to `Daytime`). Their day loops remain as fallbacks but now finish at once.
- A `[Diagnose]` log in `VillageCheckpointDTests` that records frame time, time settings, vSync, memory and object counts per test: keep it while the suite is being trimmed.

---

## 2. Real problems in the tests themselves (fix whatever else happens)

These aren't about speed: they're tests that can pass while checking less than they claim, fail by chance, or depend on the machine.

| Problem | Where | Confidence | Fix |
|---|---|---|---|
| **The box-fit check skips about 100 newer strings.** Its English dictionary is built from `LocKeys`, `TavernLocKeys`, `LoopLocKeys`, `DecorateLocKeys`, `StoryLocKeys` and content only, and any key it can't find is silently skipped (line 196). So the pause menu, options, controls, surface, garden, creator, onboarding and credits text never get the box-fit or heading check that CLAUDE.md says every fixed string gets. | `EditMode/TypographyTests.cs` `English` (29–31), `EveryFixedString_FitsItsBox_AtItsStyle` (186) | high | Build the dictionary from every `*LocKeys` source (the same source as `TextStyleTests.CodeEnglish`), and fail on an unknown key instead of skipping it. **It may expose real overflows.** |
| **An assertion that can't fail.** The test walks days to see Ogrin on a good day *and* a bad day, then asserts `sawGood \|\| sawBad`, which is true after one day. If every day is good, the bad-day window, glow and talk position are never checked. | `PlayMode/VillageCheckpointCTests.cs` `Ogrin_IsOutOnGoodDays_AndAtHisWindowOnBadOnes_TalkableEitherWay` (345–370) | high | `sawGood && sawBad`, with a pinned seed that gives one of each on days 2 and 3 (as D's window test now does). |
| **Still a random number of days.** `VillageCheckpointCTests` wasn't given the seed argument: the Ogrin test walks up to 8 days, the herb test up to 3. | `VillageCheckpointCTests.cs` 299, 345 | high | Use the same `seed:` pattern as D. |
| **Tests read the developer's own options file.** Only `OptionsPlayTests` and `PresentationPlayTests` point `GameOptions.DirectoryOverride` at a temp folder. Everywhere else, the owner's saved text speed, relaxed timing, patient customers, hit-stop and vibration settings apply. `StoryCheckpointATests` A2 and `StoryCheckpointBTests` B2 assert `Box.IsRevealing`, which fails on a machine set to instant text. | every Boot fixture except those two; `GameOptions.cs` 78–97 | medium-high | Temp options folder for the whole run, in the shared fixture. |
| **vSync left off for the rest of the run.** `vSyncCount = 0` is set and never restored; `targetFrameRate` is restored only in some paths. The class runs first alphabetically. `RenderPipelineSetupTests` asserts vSync is on. | `PlayMode/CameraFollowTests.cs` 102–103, 160–161, 228–229 | high | Restore both in teardown. |
| **The current input device stays "gamepad".** Two tests leave `InputDevices.Current` on Gamepad; later prompt assertions (`TavernSceneTests` 231, `Does.StartWith("E")`) pass only if a later test pressed a key. | `PlayMode/PresentationPlayTests.cs` (gamepad tests); `FirstImpressionsPlayTests` resets it, the base doesn't | medium | Reset in the base teardown. |
| **No check that the real scenes' localized keys exist.** Only the 4a look-test scene and the credits are checked at runtime; the fit check skips unknown keys (above). | — | medium | Add to the shared EditMode scan (§5). |
| **Lighting is only checked on the 4a look-test scenes**, not Tavern, Kariaston or Dungeon, so CLAUDE.md's "one enabled global light per sorting layer" isn't guarded. | `LookTestSceneTests.BothScenes_UseLitSprites_AndRepresentativeLighting` (583) | medium | Check the real scenes in the shared EditMode scan. |
| **A test that never sees the case it should.** The door-marker switch has no curio case; it passes only because this seed's route has no curio room. | `PlayMode/DungeonRoomTests.cs` `EachDoor_ShowsWhatItsRoomPromises` (364–370) | low | Add the curio case. |

Also flagged by the 4i-C session itself: `Pip_OnServing…` reaching 0.861 against the 0.85 staff cap "now and then". Over the cap means staff beat the cap at least once (a locked rule); look for a frame-rate dependence rather than treating it as a flake.

---

## 3. Leaks and isolation (cross-cutting)

`LookTestFixture` (`PlayMode/LookTestSceneTests.cs` 58–153) is the only shared base. It resets the EventBus, the virtual keyboard and mouse, two vibration settings and `Time.timeScale`. Every Boot fixture writes its own copy of `BootToMenu` / `Revealed` / `Daytime` / `Sleep` / `NextDay` / temp-save setup (about 16 files), and their teardowns differ.

Game code is clean on inspection: no `DontDestroyOnLoad` in `Assets/_Project/Scripts`, static registries are released in `OnDisable`/`OnDestroy`, `EventBus` registers each clearer once, music clips are compressed in memory and don't pile up in the editor. TDE's `GameManager` and `MMSoundManager` are persistent singletons (one copy each).

What a test can leave behind for the next one:

| Leftover | Where | Confidence | Fix (in the shared teardown) |
|---|---|---|---|
| `MenuPause` held after a test ends with a result, swap, death or power screen open (five tests in DungeonRoom and HarvestFinisher end that way; SatchelTests never clears) | many Dungeon/Tavern fixtures | high | `MenuPause.Clear()` while the scene is alive |
| `SurfacePause`, `MusicHolds`, `SurfaceTime.SettingsOverride` not cleared by DayLoop, CheckpointDDayLoop and VillageCheckpointC, though they enter Decorate Mode | those teardowns | medium | clear all three |
| `AmbientMoments` cooldown and "heard today" sets reset only by VillageCheckpointD | `AmbientMoments.cs` 37–59 | low-medium | `ResetForTests()` in the base |
| `ScreenReservations.Add` not in `try/finally` | `HollowsReadabilityTests` 72 | low (only on failure) | `finally` |
| `FurnitureRecolour.s_Cache` and `AreaFinishes.s_Recoloured`: static caches of runtime textures never cleared; `EveryColorPanelText_FitsItsRow` bakes thousands | `FurnitureRecolour.cs` 14, `AreaFinishes.cs` 22 | medium (bounded by key, but large) | clear between fixtures, or shrink that test (§4) |
| A real asset changed during a test (`enemyDropChance` on the real `RunSettings`), restored in a coroutine `finally` that a timeout may skip | `PlayMode/CheckpointCTests.cs` 172–194 | low-medium | use a copy of the asset |
| Pixel Crushers' save component goes `DontDestroyOnLoad`, so the first Boot's copy outlives every later test (later copies destroy themselves) | `Story/Editor/StoryScene.cs` 69–71 (vendor `SaveSystem.Awake`) | medium (harmless today, but cross-test) | note it; A5's "one save component" passes only because of self-destruction |
| `PixelCrushers.GameTime` left at the last test's day | StoryCheckpointA A3 (day 8) | low | reset in teardown |
| Orphaned `ScriptableObject`s / profile clones | StoryCheckpointA 403, TavernService 55, TavernCustomer 38 | low | destroy them |

**Proposed `BootFixture : LookTestFixture`:**
- **SetUp:** temp save and options folders, then `GameOptions.Reload()`; `AmbientMoments.ResetForTests()`; a snapshot of vSync and `targetFrameRate`; a fixed seed.
- **UnityTearDown, in this order:**
  1. Close conversations and the pause menu.
  2. `MenuPause.Clear()`.
  3. Clear `SurfacePause`, `MusicHolds`, `SurfaceTime.SettingsOverride`, the `RoomRunner` overrides and `DecorateMode.Sandbox`.
  4. Set `InputDevices` back to keyboard.
  5. Restore `timeScale`, vSync and `targetFrameRate`.
  6. Delete the temp folders.
  7. Log the `[Diagnose]` line.
- **Helpers:** one `Boot()`, `Revealed()`, `Frames()`, `StartDaytime(seed, opening)` (a written save + Continue by default), `NextDay()`, `InTavern()`, `InDungeon()`, and `InstantTransitions` on by default.
- `PauseRules` has no public `Clear()`; adding one is a small code change.

---

## 4. Findings by area

Times are per run, estimated. "Merge" always means the surviving test takes the removed test's unique assertions (§6).

### 4.1 Day, surface and village (the most expensive area: about 22–28 min alone)

Unit costs: `Daytime()` ≈ 12–15 s (Boot, menu, new game, the generated Dungeon, a Night tavern, sleep into Tavern + Kariaston); `NextDay()` ≈ 12–15 s (Prep, `SkipService` which loads the Dungeon, the delve's end, sleep).

| Finding | Tests (file:line) | Conf. | Action | Saves |
|---|---|---|---|---|
| Every surface test goes through a throwaway delve to reach day 2 | `Daytime()` in SurfaceCheckpointA (68), SurfaceCheckpointB (87), VillageCheckpointC (100), VillageCheckpointD (84), FirstImpressionsPlay (92), OptionsPlay, PresentationPlay | high | speed up: written day-2 save + `Continue` (one content load instead of three); keep the real path in DayLoop, CheckpointDDayLoop and the Gimp-night test | 5–10 min |
| Villagers walk at timeScale 1 behind 30–45 s waits | VillageC 242, 272–288, 309, 381, 398, 537; VillageD 263, 304, 235, 245, 397, 546, 679 | medium | timeScale 4 around walk waits (the clock is unscaled and held, so it won't move); or keep the keeper out of sight so `VillagePresence` places them | 1–2 min |
| Three tests walk the days around Ogrin's window separately | VillageC `Ogrin_IsOutOnGoodDays…`, VillageD `OgrinsWindow_IsLit…`, VillageD `OgrinsLight_ShowsAtHisWindow…` | medium-high | merge into one test over two pinned days (one good, one bad, one with a Glimmer evening) | 1–3 min |
| `TheStairs_StillGoBothWays` is covered by `Doorways_GoThroughWhenPushed…` and `AfterKariaston…` | SurfaceA 141 | high | retire | ~13 s |
| `Prep_CanBeginBeforeFive` is done by `DaytimeActions.BeginEvening` in four other tests; its unload check is in `FiveOClock…` | SurfaceA 467 | high | retire | ~16 s |
| `DecoratingAtNight_IsSaved…` makes the same barrel move and Continue as CheckpointDDayLoop | DayLoop 634 | high-medium | merge its night-panel, view and NavGrid checks into CheckpointDDayLoop, retire | ~20 s |
| `TheDelve_InTheDayLoop_IsTheGeneratedRun…` and `EveryFrameOfATransition_HasACameraDrawingTheCover` are each just Boot + new game | DayLoop 431, 589 | high-medium | fold both into `QuittingMidDelve…` | ~15 s |
| `EssenceUpgrade_AndTheStartingFurniture…`: the wiring is already proven in `AWholeDay` | DayLoop 398 | medium | move the seat/rail rows into `AWholeDay`'s Prep; "seat upgrade retired" as EditMode | ~25 s |
| Two tests save and resume the same day | SurfaceB `AMidDaySave_Continues…` (288), `EachGardenAction_AndTheEvening_SaveTheDay` (312) | medium | one test | ~15 s |
| Musashi at his cart is already asserted in VillageC `EveryoneLivesInKariaston…` (202) | SurfaceB `Musashi_StandsByTheCart…` (332) | medium | add Musashi to C's talk loop, retire | ~13 s |
| `Overheard_NeverPlays_InDecorateMode_OrAMenu` tests a static formula | VillageD 327 | medium | EditMode, or fold into `DecorateMode_Holds…` | ~13 s |
| Two music tests share their setup | VillageC `OneListener_OnEveryFrame…` (505), `TheMusic_FollowsTheDay…` (456) | medium-high | merge | ~20 s |
| `AfterKariaston_TheKeeperWalksBackUpTheStairs` continues where `Doorways…` ends | SurfaceA 157 | medium | merge | ~15 s |
| `GimpComesUpTheHatch…`: the final "nor the next morning" `NextDay` re-proves an EditMode case | VillageD 212–215 | medium-high | trim | ~15 s |
| Kariaston's collision checks are static scene data behind a full `Daytime()` each | VillageD `TheWater_IsSolid…` (473), `EveryFence_Sign…` (493), `AFencePutAnywhere…` (515) | high to merge, medium for EditMode | merge into one; better, open `Kariaston.unity` in EditMode and call `DressingCollision.Apply()` | ~26 s |
| Two static checks behind a full `Daytime()` | SurfaceB `TheMarketCart_LetsNobodyStand…` (356) | medium-high | EditMode, or fold into a daytime test | ~13 s |
| Smaller trims | SurfaceB `AtZeroVigor…` 233–243 (Prep + Dungeon for one assert); `Crops_GrowOverDays…` (second `NextDay`) | low-medium | trim | ~20 s |
| Fixed waits that could be `WaitUntil` | VillageC 314 (3.5 s); VillageD 399/407 (1.6 s a day), 209/214 | low-medium | `WaitUntil` | 3–10 s |
| Stale | DayLoop 351–355 orphan summary (retired seat upgrade); "Gunta" in CheckpointDDayLoop 38/146/174/187; "Bram's room" SurfaceA 124; "breakfast" in EditMode DayLoop 480 | high/low | fix the text | — |

EditMode files here (DayLoop, SurfaceCheckpointB, SurfaceClock, VillageSchedule, Community, Arrival, KeeperIdle) are cheap and clean.

### 4.2 UI, options, typography, sound, credits

| Finding | Tests (file:line) | Conf. | Action | Saves |
|---|---|---|---|---|
| Four FirstImpressions tests each pay a full `Daytime()` and never quit | `Pausing_InTheDay…` (128), `TheGarden_MarketAndMenuBoardPrompts…` (391), `Saving_ShowsTheSavedMark_Briefly` (470: two fixed 2 s waits), `Prompts_NameTheDeviceInUse` (538) | medium | merge into one or two | 30–50 s |
| `Prompts_NameTheDeviceInUse` needs no scene | FirstImpressionsPlay 538 | medium | EditMode (check `InputSystem.actions` is set there), or start from `Menu()` | 10–20 s |
| Three tests reach the same first-morning conversation | FirstImpressions `TheFirstMorning…` (360), Options `TextSpeed_Instant…` (225), Presentation `Walking_IsHeard…` (182) | medium | merge text speed + blips/duck into one "dialogue box" test; keep "once" separate | 10–20 s |
| The options-survive-New-Game tail waits for the whole first Dungeon | Options `TheMainMenu_OpensOptions…` 159–164 | medium | assert right after `QuickNewGame` (persistence is covered in EditMode) | ~5 s |
| Credits from the pause menu reaches the delve only to open the pause menu | Presentation `TheCredits_OpenFromThePauseMenu` (140) | low-medium | fold into the Hollows mixer test | 8–12 s |
| Three tests load Tavern separately | TypographyLayout (all three); `SpiderLegSteaks…` pages Prep exactly as TextOverlap (106) does | medium | fold into TextOverlap's sweep and the results test | 5–10 s |
| Furniture walk-ups on the frozen 4a scene; the real tavern's walk-ups cover every piece | LookTest `Bar_BlocksThePlayer…` (507), `Tables_BlockThePlayer…` (519) | medium | retire; move `SolidFurniture_FootprintEndsAtItsSortPoint` (536) to EditMode | ~10 s |
| LookTest dungeon tests repeat combat-floor coverage | `Keyboard_MovesThePlayer…`, `Walls_BlockThePlayer`, `Dodge_Rolls…`, `EnemyAttack_Drains…`, `Combo_KillsTheSlime…`, `EssenceAtZero…` | low | retire the overlaps; keep `Scene_UsesTheLockedTopDownSetup` and `Gameplay_DoesNotDependOnTheSpriteAnimator` | 10–15 s |
| TextStyle's scene check repeats Typography's | `TextStyleTests.TheDayLoopsScenes_DrawEveryTextInTheGameFont…` (223) vs `TypographyTests.EveryTextInTheGame_IsStyled…` (153) | high | move the `PixelCanvasScaler` check into Typography, retire the loop | 4 scene opens |
| Stale | CLAUDE.md's Music section still says the Hollows' quarter-down is `AudioListener.volume` with no mixer (4i-B moved it to the mixer; the tests are right); `MusicTests.EveryCue_HasAVorbisTrack_ThatLoops…` never checks looping; two misplaced summaries in TextStyleTests (100–107, 181–185); a no-op `Replace` in CreditsTests 37; `OptionsTests` re-asserts the save version (147–149) | high/low | fix CLAUDE.md and the text | — |

### 4.3 Tavern

| Finding | Tests (file:line) | Conf. | Action | Saves |
|---|---|---|---|---|
| Customer walks and staff waits at timeScale 1 | `TavernServiceTests.Order()` (49–61, all 8 tests), `Pip_AtTheGrill…` (237, up to 30 s), `TavernCustomerTests.WhenEverySeatIsTaken…` (77, ~15 s), `WhenPatienceRunsOut` (112), `EndingService` (206), TavernEvening 128/166, CheckpointD 117/165/181 | high | timeScale 4 in those helpers; keep 1× only where frames matter (the clipping loop, layer sync, queue idle, Pip's post) | 30–50 s |
| "Boog grills, Orik carries" runs four times | CheckpointD `GuntaAndPip_MeetASpecialRequest…` (192), `Pip_Serves_AtThreeFramesASecond` (261), CheckpointC `Gunta_TakesTheStation…` (469), CheckpointDDayLoop day 2 | high / medium | retire the first (move `RequestOutcome.Completed` into the 3 fps test); optionally slim the third | 8–18 s |
| Two rearranged-room services seat six customers | DecorateMode `AServiceRuns_InARearrangedTavern` (325), CheckpointB `AServiceRuns_InATavernFurnishedFromTheCatalogue` (438) | medium | move the pure-grid vs baked-grid comparison into the catalogue test; drop the Decorate service | 5–8 s |
| Checkpoint C's Boot → delve tests repeat DayLoop's troll and death paths | CheckpointC `TheTrollsTusks_AreGranted…` (238), `Dying_LosesEveryCurio…` (211) | medium | move the curio and trophy UI checks into DayLoop's troll and death tests; retire | 25–30 s |
| A whole delve only to make a save, then three boots | CheckpointC `ASaveThatBeatTheTrollBeforeTrophies…` (289) | medium | write the v6 save directly; drop the third boot (EditMode covers "once") | 15–20 s |
| The colour panel tries 16 options on every row of every piece, baking a texture each time | CheckpointB `EveryColorPanelText_FitsItsRow` (325) | medium | real option counts only, or EditMode text widths | tens of s (low conf.) |
| Catalogue and dish-card text walked twice or three times | CheckpointB `EveryCatalogueText_FitsItsLines` (287) + TextOverlap `DecorateTheCatalogAndTheCheck…` (128, twice); CheckpointD `EveryDishCard…` (280) ⊇ TypographyLayout `SpiderLegSteaks…` (146) | medium | one pass per screen checking wrap and overlap | 10–30 s (low conf.) |
| `Seating_IsThePlacedChairsFacingTables` is covered by the snapshot, `LayoutRulesTests` and `WhenEverySeatIsTaken` | TavernCustomer 196 | high | retire | ~3 s |
| `Room_HasItsStations_SeatsAndGrid` mostly repeats the snapshot | TavernScene 40 | medium | keep only the character lit-material and Hatch checks, inside FurnitureSceneTests | ~3 s |
| Walking into ten footprints at 0.6 s each | TavernScene `Furniture_StopsThePlayerInFront…` (88) | medium | EditMode check of every piece's footprint vs sort point; keep one or two PlayMode walks | ~5 s, and better coverage |
| Three starting-layout validity tests | EditMode FurnitureTests 351, CatalogueTests 88, LayoutRulesTests 138 | medium | retire the FurnitureTests one (check `Check` rejects a half-tile nudge first) | — |
| Haptic id check duplicated | EditMode `TavernFeedbackRulesTests.EveryTavernPattern_IsANamedPattern…` (95) ⊂ `HapticLibraryAssetTests` | high | retire | — |
| Smaller | FeedbackTests `ACleanKill…` loads the floor twice (123); `ATelegraph…` waits up to 6 s (160); CheckpointD `ASpecialRequest_IsMarked…` tail (156–167) | low | trim | ~10 s |
| Stale | orphan summary TavernScene 143; no-op version replace CheckpointC 302; "6 seats before upgrades" TavernCustomer 81; `Pip_`/`Gunta_` names; `TheDodge_FacesTheWayItGoes` in FeedbackTests; IngredientContentTests guards a one-time 4b rename | high/low | text, or retire later | — |

### 4.4 The Hollows, combat, satchel, camera, input

| Finding | Tests (file:line) | Conf. | Action | Saves |
|---|---|---|---|---|
| The fixed seed walks the same routes again | DungeonRoom: the two hole-down tests (299, 319); the campfire before the boss (255) inside `AFullRun_…` (218); door markers (355) and camera bounds (531) inside the full run's walk; power spark (422) and backing out (450) | medium | merge, keeping specific messages | 40–60 s |
| Run gold banking repeats EditMode DayLoop rules | DungeonRoom `RunGold_ComesHome…` (502), `RunGold_IsLost_OnDeath` (514) | medium | fold the gold into `ARope_EndsTheRun` and a DelveEnd death test | 6–8 s |
| One geometry check boots the whole game | HollowsReadability `ADoorsSign_StepsOutFromUnderTheHud…` | medium | load `Dungeon` with a fixed seed | 12–20 s |
| The troll's entrance asserted three times | LarderTroll `TheFirstMeeting_GetsTheFullReveal` (102), `TheEntrance_ShowsTheTroll…` (110), `TheTroll_IsFoundEating…` | high / medium | fold into the last | 7–10 s |
| Waiting for the troll's fight only to spawn slimes | HarvestFinisher `ALowFreshlyHitEnemy…`, `TheMoment_Passes…` (128; also an EditMode duplicate) | medium | freeze the troll at once, or use the test floor; merge the second | 7–9 s |
| Waiting for the troll to cross the arena | LarderTroll `APartOutOfReach…` (164) | medium | teleport him near the wall | ~7 s |
| Four tests reload the floor to check the restart | DelveEnd via `ExpectFreshFloor` (82–92) | high | check the restart once | 5–8 s |
| Three chases, three loads | TestFloorNavigation (59–74) | medium | one test, one load (keep the real-physics chase) | 4–6 s |
| Type check loads the Tavern | Gamepad `EveryCharacterPrefab_UsesTheFloorController` (181) | high | EditMode prefab scan | 3–5 s |
| Fixed 2 s freshness waits | Hud 85, Satchel 105, 155 | medium | call `Decay` directly / shorter windows | 4–5 s |
| One camera case covered twice | CameraFollow `ScrollModes_…(PixelPerfect)` vs `Follow_…` | low-medium | drop that value | ~3 s |
| EditMode generation repeated | RunGeneratorTests: ~3,500 `Generate` calls each re-parsing every room | low | build graphs once per class | 3–10 s EditMode |
| A plain `[Test]` in PlayMode | Hud `HarvestFeed_PicksTheRightWords` (110) | high | move to EditMode | — |
| Stale: dead code under test | EditMode ComboLogicTests (7 tests; `ComboLogic` isn't used, the combo is TDE's); CombatTests (`DamageCalculator.Apply` is never called; `Scale`, which is, is untested) | high / medium | owner's call: retire with the dead code, or test `Scale` | — |
| Stale text | DungeonRoom 251 ("half" vs a quarter); CameraFollow summary (describes grid snapping; smooth scrolling is locked); `ARope_EndsTheRun` never uses the rope; Giant Rat in HarvestRules comments; file names that don't match their classes | high/low | fix | — |

### 4.5 Story

| Finding | Tests (file:line) | Conf. | Action | Saves |
|---|---|---|---|---|
| C1 repeats A1's troll-to-Continue chain | StoryC `TheTrollsFall_BoogLoudly_OrikQuietly…` (167) vs StoryA `TheTusks_ReachBoog…` (232) | high | move C1's Orik memory, lines and choices into A1; retire C1 | 35–45 s |
| C4 is one assertion short of A1 | StoryC `TheTusks_AreRemarkedOnce…` (313) | high | add the "about your bomb..." check to A1; retire C4 | 15–20 s |
| C5 fits inside A2's night conversation | StoryC `TheNightsPanel_StepsAside…` (336) | high | merge | ~15 s |
| Two old-save Continue tests reach the same state | StoryA `ASaveFromBefore4g_Continues…` (452), StoryB `ACheckpointASave_Continues…` (632) | medium-high | one test with both sets of assertions | 25–30 s |
| A whole delve just to stand at night | StoryC C2, C3; StoryA A2 | medium | write a night save and Continue (check starter furniture is granted) | 20–30 s |
| B3's mid-opening reload re-proves a save covered in EditMode | StoryB `TheFirstEvening_TeachesTheLoop…` (420–425) | medium | drop the reload, rename | 8–12 s |
| B4 (the bomb quest): 3 delves, ~1.5–2.5 min, no explicit timeout | StoryB `BoogsBomb_DeclinedThenTaken…` (515) | medium | **keep** (its final Continue is the only test of Quest Machine state across a reload); tag Slow, explicit timeout | — |
| The dialogue database walked ~20 times, string tables three times (EditMode) | StoryTests, StoryB, StoryC, TextStyle, Community, VillageSchedule | high (cheap) | one cached entry list | 2–3 s |
| Stale | StoryA summary promises a retired proof quest; dead `ListenAsAtEndOfFrame`; an A1 comment about re-hanging the tusks that the code doesn't do (320–324, 339); StoryTests says "version 8" | high | fix | — |

### 4.6 Captures

All 14 `*Captures.cs` classes are `[Explicit]` at class level and have no one-time setup, so **none of their code runs in a normal run**; retiring them saves nothing. Two are load-bearing: `CheckpointBCaptures.Furnish`/`DwarvenHall` (used by CheckpointB 441) and `TavernBaselineCaptures` (refreshes `TavernStarting.txt`, read by FurnitureSceneTests and EditMode LayoutRulesTests). `DayLoopCaptures` is stale (it drives the pre-4h morning panel). Seven more `[Explicit]` methods sit inside runnable classes; they're skipped too.

---

## 5. EditMode: one shared scan

Eleven EditMode tests open the game's scenes separately: **34 opens of 6 scenes** per run (Tavern 10, Boot 8, MainMenu 6, Dungeon 6, Kariaston 3, Dungeon_TestFloor 1). Super Text Mesh is `[ExecuteInEditMode]`, so every open rebuilds every text (291 in Tavern alone).

| Test | Scenes |
|---|---|
| `TypographyTests.EveryTextInTheGame_IsStyled…`, `EveryFixedString_FitsItsBox…`, `TheTitleRow…` | Tavern, Dungeon, Boot, MainMenu (twice) + Tavern |
| `TextStyleTests.TheDayLoopsScenes_DrawEveryText…` | Tavern, Dungeon_TestFloor, Boot, MainMenu |
| `ContrastTests.EveryText_StandsOutFromItsPanel` | Boot, MainMenu, Tavern, Dungeon |
| `OptionsTests.EverySoundInTheGamesScenes…`, `Options_SitsBetween…` | Boot, MainMenu, Tavern, Kariaston, Dungeon + MainMenu, Boot |
| `SoundTests.OnlyThePlaceholders…`, `DoorsStairsBlips…` | Boot, Tavern, Dungeon + Tavern, Boot |
| `CreditsTests.EveryChangeOfPlace_UsesTheOneTiming` | Tavern, Kariaston, Dungeon |
| `FigureSortingTests.EveryLayeredFigure…` | Kariaston, Tavern |

**Proposal:** a lazily built static `ProjectScan` in `Tests/EditMode` that opens each scene once (and walks the 36 prefabs once), records **plain data only** (strings, numbers, colours; never object references, since the scene closes), and lets each test assert its own slice. A lazy static works in any order and when a single test is run, unlike `[OneTimeSetUp]`. Add cached table strings, code English (also fixes the Typography gap in §2) and script text. **34 opens → 6, about 40–60 s.** It's also the cheap place for the missing-key and real-scene lighting checks (§2).

Other EditMode costs worth a look: `MinifantasyImportTests` loads all 336 Minifantasy textures to check import settings (reading the importer would avoid the loads); PixelStepping and PathFollowing build interpolated assert messages every frame (~150k calls; `if (…) Assert.Fail(…)` instead).

---

## 6. What must stay (gap risks)

- **One real path per area:** a real new game → delve → night → sleep (`DayLoopTests.AWholeDay`, `CheckpointDDayLoopTests.ThreeDays…`; tag both Slow, don't merge them); one real `Sleep` through `GameFlow` for garden growth and Vigor (`Crops…`); one real sleep into Gimp's night; one seen villager walk and one across scenes (`AtABeat…`, `Maximo_GoesInToTallyHo…`); one Continue that rebuilds the village from the clock (`ASaveAndContinue…`); on-foot doorway pushes; five o'clock never starting Prep.
- **Story:** B4's final Continue (the only test of Quest Machine state and the delivered-once guard across a reload); C1's Orik memory, lines and choices if it's merged into A1; both "no opening plays" checks if the old-save tests merge.
- **Tavern:** `Pip_Serves_AtThreeFramesASecond` (the slow-frame regression); the station-never-shared and Grill-first checks; the death screen's and delve result's curio texts (exist only in PlayMode: move them, don't drop them); "saved in the new version at once"; the pure-vs-baked grid comparison; `FurnitureSceneTests.EveryPiece_IsBuiltOnce` (the snapshot compares sets, so it wouldn't catch a doubled piece).
- **Hollows:** every assertion in the merged DungeonRoom walk (new-room campfire, hole-down drain stop, one power per spark, ≥12 rooms); one real-physics chase; one `ExpectFreshFloor`.
- **Captures:** keep `Furnish`/`DwarvenHall` and `TavernBaselineCaptures`, or move the helpers first.
- **Dead-code tests** (ComboLogic, `DamageCalculator.Apply`): retiring them loses nothing that ships.

---

## 7. Prompt for the Unity session

> Read `docs/TEST_REVIEW.md` on the `claude/sharp-edison-1acftb` branch (a read-only review of every test against `95a5bb93`; its times are estimates from the code). Apply it in this order, measuring as you go:
>
> 1. **Measure first.** Run both full suites and record per-test times from the results XML: the 25 slowest tests, the totals per file, and the `[Diagnose]` lines.
> 2. **Fix the test problems in §2** whatever else happens: the Typography dictionary and unknown keys (report any real overflows it exposes before fixing them), Ogrin's `sawGood && sawBad` with a pinned seed, VillageCheckpointC's seeds, a temp options folder for every fixture, vSync and the input device restored, and the `Pip_OnServing…` cap overrun (find its cause).
> 3. **Build the shared `BootFixture`** (§3) with the full teardown, then the three biggest speed-ups: `StartDaytime` from a written day-2 save + Continue, instant transitions in tests (keep fades only where they're tested), and timeScale 4 for walks that aren't what's tested. Lower the two 10-minute timeouts once the seeds are pinned.
> 4. **The shared EditMode scan** (§5), adding the missing-key and real-scene lighting checks.
> 5. **Merges and retirements** from §4, area by area. For each one, confirm the test that stays checks every assertion of the one removed (§6 lists the ones that must survive). Don't delete a test only to save time. Ask me before retiring the ComboLogic and `DamageCalculator.Apply` tests, since that means deleting dead code.
> 6. **Tag the slow tests** `[Category("Slow")]` (multi-day, the bomb quest, the full run) so a quick run skips them; the full suite still runs before every push to main and at each checkpoint's report.
> 7. **Report:** before and after times for both suites, test counts, what each merge moved where, any real bug the stricter checks found, and anything in the review you found wrong. Fix CLAUDE.md's stale Music line (the Hollows' level is on the mixer now). Change no gameplay code except to fix a real bug.
