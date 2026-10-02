# Hearthdelve — Progress

_Last updated: 2026-10-02_

## Phase 4 — Vertical slice, rebuilt top-down

### The pivot (2026-10-02)

Before Phase 4 the game changed from a side-scroller to a **top-down action roguelite with tavern management** (closer to Cult of the Lamb and Hades than to Dead Cells), rebuilt on a fresh Unity 6.6 project with third-party assets:

- **TopDown Engine 5.0** for the character controller, combat, AI, camera and rooms, with its bundled **MMFeedbacks** for game feel.
- **Nice Vibrations** (from Feel) for haptics.
- **Super Text Mesh** with uGUI for all UI and text, replacing UI Toolkit.
- **Minifantasy** (8×8, top-down) for all art.

The core loop, Harvest system, Essence, ingredients, recipes, tavern minigames, economy and story are unchanged. Design details are in `docs/GDD.md` (v0.2); working rules are in `CLAUDE.md`.

- The side-scroller prototype (Phases 1–3) is preserved at the tag **`v0-sidescroller-prototype`** and checked out read-only at `C:\Dev\Hearthdelve-v0`.
- The pivot is on the branch **`pivot/top-down`**. It merges to `main` only after the 4a look test is approved.
- Everything below the "Phase 1–3 history" heading describes the prototype at that tag, not the current project.

### Sub-milestones

Each is planned, approved, built and playtested separately. The web build must work at the end of each.

| | Sub-milestone | Contents |
|---|---|---|
| **4a** | Integration and look test | Project swap, port manifest, logic and tests ported, Nice Vibrations, Minifantasy import pipeline. One dungeon room and one tavern corner with real art at 320×180: TDE player with Essence, dodge and a melee combo; one enemy; one harvest drop; one combined hit feedback (visual + sound + haptic); one STM speech bubble; Y-sorting. `docs/ASSET_MAP.md`. |
| 4b | Dungeon migration | Phase 1 and 3 dungeon gameplay rebuilt on TDE (combat, harvest, Essence, death and Lockbox, extraction), with the dungeon haptics. |
| 4c | Tavern and UI migration | Top-down tavern, customers pathing to tables, 2D serving, Grill/Tap/Serving with their haptics, all UI rebuilt in uGUI + STM. |
| 4d | Biome 1 runs | Room-by-room structure, room rewards, run power-ups, 3 floors plus a boss arena. |
| 4e | Combat depth and boss | Harvest Finisher, Kitchen Arts, 3–4 more weapons with rarity and affixes, Essence Tonics, field cooking, Delve Marks and the Delver's Board, one relic, the Biome 1 boss. |
| 4f | Tavern Stage 1 content | Butcher Block minigame, all Biome 1 recipes, customer requests, Pip and Gundra, furniture and decor placement. |
| 4g | Story and character creation | Yarn Spinner with an STM dialogue presenter, the Act I opening, onboarding. |
| 4h | Menus, options and polish | Settings (screen shake, flash and vibration intensity), accessibility per GDD 12, audio system, web build. |

### 4a status

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

Tests: **243 project EditMode tests, all passing** in batch mode (the run reports 244; a package adds one stub test). No PlayMode tests yet.

**Next:**

1. Project setup: no gravity, Y-sort axis, Pixel Perfect Camera at 320×180 and 8 PPU, TDE `InputSystemManager` subclass over our action maps.
2. Minifantasy import pipeline and `docs/ASSET_MAP.md`.
3. Haptic pattern library and service; TDE-to-`EventBus` bridge; `EssenceHealth`.
4. The dungeon room and tavern corner, PlayMode tests, web build smoke test.

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

### Known issues

- **Character size:** at 320×180 a character is about 4% of screen height. This is the main thing to judge in the look test.
- **Vendor prefabs with missing references** after the demo trim are listed in `docs/THIRD_PARTY.md`; we don't use them.
- **Localization on WebGL:** the ported `Loc` uses synchronous lookups, which don't work on WebGL. 4a adds table preloading at boot.
- **Leftovers to remove in 4b:** `GamePause`'s hit-stop flag and the `HitStopRequested` event (hit-stop moves to MMFeedbacks).
- **Data assets** for enemies, ingredients and recipes still describe the old roster and have no icons.

### Regenerating and verifying (current project)

Close the editor first.

```
"C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe" -batchmode -nographics -quit -projectPath . -logFile BatchLogs/compile.log
"C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults BatchLogs/editmode.xml -logFile BatchLogs/editmode.log
```

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
