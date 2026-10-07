# 4h plan: walkable Kariaston and the daytime life-sim slice

> **Status: proposed 2026-10-06, awaiting the owner's approval.** Nothing of 4h is built. 4g is complete (signed off 2026-10-06, tag `milestone-4g`); its proposed Checkpoint D (the new cast) is folded into this plan. Locked and not re-litigated here: a soft daytime clock, the 5 PM world cutoff, player-chosen Evening Prep, and Vigor as the daytime productivity cap (the owner's 4h brief, 2026-10-06).

**The question 4h answers:** *does living in Tally Ho! and Kariaston feel good enough that I want to spend time there even when nothing is pushing me toward an objective?*

**Experience targets, one per place** (the owner's shorthand, used throughout as the test for every choice):

| Place | Pressure | Says |
|---|---|---|
| Kariaston | **Choice**: what do I spend today's Vigor on? | *stay awhile* |
| Tally Ho! | **Performance**: can I run tonight's service well? | *this is home* |
| The Hollows | **Risk and time**: how far before Essence runs out? | *you cannot stay here* |

Lenses behind the plan: the *Lens of the Toy* (walking around the village should be pleasant with no goal), *Lens of Time* (the clock drives the world's texture, not the player's anxiety), *Character Web* (people tied to each other before they're tied to Bram) and *Meaningful Choices* (Vigor creates trade-offs; the clock doesn't).

---

## 0. Decisions for the owner

Everything below is a recommendation unless marked locked. These are the calls I need from you before building; each has a recommendation, and the ones marked **expensive** are hard to undo later (§38).

| # | Decision | Recommendation | Cost to reverse |
|---|---|---|---|
| H1 | Scene structure | **Kariaston is its own scene, loaded additively beside `Tavern.unity` during the daytime**; doors are passages (a short fade), like the guest-room stairs (§7) | **expensive** |
| H2 | Checkpoints | **Four**, not three: A *I can live here*, B *a day's work* (Vigor and the garden), C *the village has people*, D *this is a community* (§3) | low |
| H3 | Where Bram sleeps and wakes | **The upstairs room** (today's guest room) is Bram's until the Inn takes guests in Phase 5; the bed is the Sleep interaction | low |
| H4 | Farm representation | **A few fixed garden beds with ids** (not a free tile grid) on Tally Ho!'s grounds (§11) | **expensive** |
| H5 | Crop maintenance rule | **Tending is optional and only helps** (quality); untended crops still grow. A tuning switch can make untended days pause growth if the playtest wants more care (§11) | low |
| H6 | Seeds | **Free and unlimited** for the three starter crops in 4h; costs are Vigor, bed space and days. A seed economy is Phase 5 | low |
| H7 | Market keeper | **Grim keeps the market stall** (he sells staples and spends it on Ogrin's remedies); the existing market's offers are unchanged | low |
| H8 | Art per character | As in §28 (King for Maximo, Myriad layers for Kaloren, Miner for Grim, a child from Snowball Wars/Summer Holidays for Ogrin, Wise Orc for Bart, Hunter for Gimp, Naughty Fairy for Glimmer); see `docs/plan_4h/cast_candidates.png` | medium |
| H9 | Bart's role | **The first Visitor who stayed**: came through years ago, never left, lives in a painted wagon on the green, plays the green by day and Tally Ho! some evenings (§21) | low |
| H10 | Gimp's visits | **Comes up the cellar hatch in the afternoon on visit days** (the first the day after Boog's bomb comes home, or day 6 at the latest; then about one day in three, never two in a row), sits at the bar with Boog, goes back down before Prep (§23) | low |
| H11 | Glimmer in 4h | **Surface hint only**: on some evenings a small light at Ogrin's window, which he calls "my light". No Hollows meeting, no explanation (§24) | low |
| H12 | Named villagers as evening patrons | **Yes, in Checkpoint D** (the roadmap's 4h item): 0–2 a night from whoever is free; generated customers become Visitors (§25) | medium |
| H13 | Village layout ownership | **The generator makes the first blockout once; after that the tilemaps are yours** (hand-edit in Unity like the dialogue graphs); the updater only adds or updates named gameplay objects (§7) | **expensive** |
| H14 | Save | **One bump, to version 10, in Checkpoint B** (surface time, Vigor, the garden, a per-game world seed) (§31) | medium |
| H15 | New assembly | Pure rules in `Shared`; village MonoBehaviours in a new **`Hearthdelve.Village`** assembly that may reference Tavern (never Dungeon) (§7) | medium |

Questions for you that don't block Checkpoint A (answer any time before C):

- Q1. Grim's history: how he came to look after Ogrin. I won't invent it; the dialogue can deflect until you choose (§20 offers two shapes).
- Q2. Is Kaloren the one who makes Ogrin's remedies (kind, refuses payment)? It ties three people together without defining the illness (§19).
- Q3. Maximo and the name: renamed an existing village when he became mayor (the GDD's recommendation), or founded it?
- Q4. A portrait of Phi over the bar (a Portrait Generator drow, obsidian skin, in a frame from the Towns props)? It makes her present in the room; it also fixes her face.
- Q5. Gimp's kind: human (the Hunter figure, matching the campaign's tanned woodsman) unless you say otherwise.

---

## 1. Current repo baseline relevant to 4h

What exists and how 4h uses it (read from the code at `milestone-4g`):

- **Day loop.** `GameFlow` (Boot, persistent) holds `GameState` and swaps **one** additive content scene: `MainMenu`, `Tavern` or `Dungeon` (`GameFlow.LoadContent`). `DayCycle`: `Daytime → Evening → Delve → Night → Daytime`. `DayRules` makes every change (pure, tested). Saves: after service, after the delve, after purchases, on sleep; **daytime is not saved** today ("daytime has nothing to lose").
- **Tavern phases.** `TavernDirector.TavernPhase`: `Daytime` (the placeholder **`MorningScreen`** panel: storeroom list, delve meal, market button, decorate button, "open for the evening"), `Prep`, `Service`, `Results`, `Night` (`NightScreen`: summary, upgrades, sleep), and `Arrival` (4g's walkable first day). The keeper walks only in `Service` and `Arrival` (`InputMaps.Tavern`); everything else is UI-only.
- **Property areas.** `PropertyArea` (`tavern`, `guest_room`), `AreaPassage` (the stairs: fade, move the keeper and camera, fade back), `AreaFurniture` and `DecorateMode` (enters in `Daytime`, `Prep`, `Night`). One furniture architecture for every area.
- **Interaction.** `TavernInteractable` + `InteractionRules.Pick` (nearest available in reach, sticky); `TavernInteractor` on the keeper. Staff talk through a `TavernInteractable` on `StaffAgent` → `StoryServices.Conversations.Talk(id)`.
- **Economy.** Market = `SupplySource` (`Supply_BrackenfordMarket`): five staples (bread, eggs, herbs, malt, onion) at 2 gold each, bought through `GameFlow.BuyFromMarket` → `DayRules.Buy` (Daytime only), shown by `MarketPanel`. Ingredients carry `IngredientSource` (Hollows/Surface). Onion is in 3 recipes (stew, pottage, broth), herbs in 4 (crispy wings, grilled leg, broth, steaks), malt in 2 (ale, gelbrew).
- **Delve meal.** Cooked at the Grill or Tap in the daytime (`TavernDirector.CookDelveMeal` → `KeeperWork`), its buff kept for that night's delve.
- **Story.** `CharacterDefinition` (id, kind incl. `Villager`, `Visitor`, `Resident`, `Story`; portrait; conversation; values; starting feelings), Love/Hate stand-ins keyed by id, `HH_` Lua functions (`StoryLua`), deeds via `RelationshipRules.Qualifies` and learners (`Staff`, `Named`), hubs (`Boog/Hub`, `Orik/Hub`), conversations seeded once (`DialogueSeeds.txt`) and then owned by the node editor. `StepAsideWhileTalking` hides panels during a conversation. `SpeechBubble` exists for world-anchored text.
- **Customers.** Three generated `CustomerProfile`s (adventurer, dwarf, villager) with `NpcAppearancePool` layers from *A Myriad Of NPCs*: effectively Visitors already. Visitor ids `visitor/<day>/<visit>` are never saved.
- **Pause.** `MenuPause` (nested; `Time.timeScale` or TDE's `MMTimeScaleEvent`).
- **Pathfinding.** Grid A* in `Core/Pathfinding` (pure, tested), used by customers and staff; `Arrival` handles low frame rates.
- **Save.** Version 9 (`SaveData`: day, phase, gold, renown, storeroom, upgrades, meal, bosses, furniture, story, questObjects). Migrations grant only what didn't exist, once.
- **Movement scale.** The keeper walks 6 tiles/s; patrons 2.8–3.2. The view is 40×22.5 tiles.
- **Tests at sign-off:** EditMode 627, PlayMode 219 (+24 explicit captures). Web build working.

---

## 2. Recommended 4h scope

**In 4h:**

1. Tally Ho! freely inhabitable in the daytime: walk, the upstairs room, talk to Boog and Orik, inspect things, decorate, the storeroom, the delve meal at the stations, out the front door.
2. A compact walkable Kariaston (one scene) with Tally Ho!'s exterior and grounds, the square and market, four households, a memorial, a green, and visible room to grow (the three empty plots, a road out).
3. The surface clock (8 AM → 5 PM, soft), the 5 PM wind-down, and a player-chosen *Begin Evening Prep*.
4. Vigor and a tiny garden: three crops, a few beds, multi-day growth, harvest into the storeroom.
5. The cast as data and in place: Maximo, Kaloren, Grim, Ogrin, Bart with homes, light schedules, first conversations and hubs; Karias, Old Phi and the Fortunate Five present through objects and talk.
6. A small schedule and presence system; NPC-to-NPC barks.
7. Gimp's visits (the Hollows-origin social character proof).
8. Named villagers as occasional evening patrons; Bart performing on some evenings (ambience only).
9. Save version 10, migrations, full regression, web.

**Not in 4h** (§39): fishing, ranching, seasons, weather, seed economy, more crops, farm expansion, Inn guests, Visitor promotion, residents on the plots, recruitment, any friendly-monster pipeline beyond Gimp, Kaloren's phylactery, Glimmer's questline or meeting, the Fortunate Five questline, romance, gift preferences, elaborate AI, building interiors other than Tally Ho!.

---

## 3. Checkpoint breakdown

The brief's A/B/C is sound, with one change: **split B**. Vigor and the garden are a mechanical, subjective test of *choice pressure*; the villagers are a social test. Built together, a bad playtest result couldn't say which half failed, and the checkpoint would be the size of 4g. Each checkpoint below is playable and reviewable alone.

| Checkpoint | Steps | Primary question |
|---|---|---|
| **A: I can live here** | 1–4 | Does moving between Tally Ho! and Kariaston feel good, and does Tally Ho! feel like home? |
| **B: A day's work** | 5–7 | Does Vigor plus a tiny garden produce relaxed but meaningful choices, and do multi-day crops create anticipation rather than chores? |
| **C: The village has people** | 8–10 | Do named villagers with simple routines make Kariaston feel specific and inhabited? |
| **D: This is a community** | 11–14 | Does Kariaston feel like people live here rather than waiting for Bram? (relationship web, patrons, Gimp, closeout) |

Why this order: A must replace the Morning panel completely (otherwise the storeroom, market, meal and decorate become unreachable), so every existing daytime function becomes physical in A. B's save bump lands once, with all the new state. C needs A's clock and B's world seed for schedules. D is the integration and closeout.

---

## 4. The daytime loop

```
Night: sleep (bed upstairs, or the Night panel's button)          [autosave, crops grow, Vigor refills]
  → 8:00 wake in Tally Ho!, upstairs
  → free daytime (clock running 8:00 → 17:00)
       Tally Ho!: Boog in the kitchen, Orik at the bar, Maximo's lunch, a Visitor with a drink,
                  storeroom, stations (delve meal), decorate, the upstairs room
       Kariaston: market (8–17), garden (Vigor), people, the green, the memorial, the wagon
  → 17:00 the village winds down (the clock stops; market packs up; people go home or to evening spots)
       still free: walk, talk to whoever's out, decorate, the storeroom, the delve meal, hang around
  → Begin Evening Prep (the menu board in Tally Ho!, one confirm)  ← the player decides, any time
  → Prep → open → service → Results → close                         [existing, unchanged]
  → the delve                                                        [existing, unchanged; Vigor irrelevant]
  → Night → sleep
```

Starting Prep before 5 PM is normal and must feel fine: the confirm says the day will end, nothing more. Kariaston unloads when Prep begins.

---

## 5. Tally Ho! free-roam plan

`TavernPhase.Daytime` becomes a walking phase (like `Arrival`): `InputMaps.Tavern`, the HUD shows the clock and Vigor, and the `MorningScreen` panel is retired. Each of its functions moves to a place in the room, reusing the same code underneath:

| Today (Morning panel) | 4h (in the room) | Reused |
|---|---|---|
| Storeroom list | **The storeroom shelves** in the kitchen corner: interact → the storeroom panel (the panel's stock list, standalone) | `MorningScreen` stock view, `SatchelSlotView` |
| Delve meal | **The Grill and Tap** offer "cook tonight's delve meal" in the daytime; the meal's bonus shows in a small HUD note | `CanCookDelveMeal`, `KeeperWork.CookDelveMeal`, `EatDelveMeal` |
| Market button | **Grim's stall** in Kariaston (8–17) | `MarketPanel`, `DayRules.Buy` |
| Decorate button | **The Decorate key** anywhere inside the property in the daytime and at Night, plus **a plans book on the bar** | `DecorateMode.Enter` (already allows Daytime and Night) |
| Open for the evening | **The menu board** (chalkboard) by the bar: "begin evening prep?" | `TavernDirector.OpenForEvening` → `GameFlow.StartEvening` |

Also in Tally Ho!:
- **Waking:** the keeper starts each day upstairs by the bed (H3). The bed at Night is a second way to Sleep (the Night panel keeps its button in 4h).
- **Boog and Orik:** daytime posts (Boog at the stove or the butcher block, Orik at the bar with the ledger), talkable as now; in C their posts become schedule entries.
- **Inspectables:** a small `Inspectable` interaction plays a one-line Dialogue System conversation (so the words stay in the node editor): Phi's chair, the Fortunate Five's tankards on a shelf, the trophy, the hatch, Orik's incident book. Kept to about six objects.
- **The front door:** a passage to Kariaston (fade, the keeper appears on the step outside).
- **The cellar hatch:** shown when Gimp comes up (D); never a way down in the daytime (the delve stays at night).
- **Night:** the Night panel stays as built in 4h (summary, upgrades, sleep); walking at night is a candidate for D's polish if A's free roam feels good.

---

## 6. Kariaston map and layout proposal

**Size:** about **72×48 tiles** (1.8 × 2.1 screens). At the keeper's 6 tiles/s, Tally Ho!'s door to the farthest home is about 12 s. A landmark should never be more than ~15 s away; the playtest decides whether it's too big or too small (the layout is data, H13).

```
 N                                                                         (road out: closed,
 ┌────────────────────────────────────────────────────────────────────┐    "the bridge is out" — 
 │  trees      [Kaloren's tower]         trees         ░ plot 3 ░     ═══  future expansion)
 │              ·  ·                                   ░ (empty) ░    │
 │ [Maximo's     ·    ┌──────────── the square ───────────┐            │
 │  house,       ·    │  Karias memorial    the well       │  ░ plot 2 ░│
 │  balcony]─────·────│        [Grim's market stall]      │  ░(empty) ░│
 │               ·    └───────────────┬───────────────────┘            │
 │ [Grim & Ogrin's    the green       │       [Bart's wagon]           │
 │  cottage,          (bench, tree,   │                     ░ plot 1 ░ │
 │  Ogrin's window)   Bart plays)     │                     ░(empty) ░ │
 │                                    │                                │
 │   [the garden: 4 beds,      ┌──────┴───────┐     hen run (decor)     │
 │    fence, scarecrow]────────│  TALLY HO!   │                         │
 │                             │  (exterior)  │   cart, barrels         │
 │                             └────door──────┘                         │
 └────────────────────────────────────────────────────────────────────┘
```

- **Tally Ho!** sits south-centre: the first thing outside the door is the green and the square to the north; the garden is on its own grounds, a few steps west (a home garden, not a farm).
- **The square** is the social centre: the market stall, the well (*Animated Well*), the memorial to Karias (*Town Monuments*), benches.
- **Four households**, each with a recognizable silhouette: Maximo's house (the largest, with flags), Kaloren's tower (*Wizard Tower*), Grim and Ogrin's cottage (small, with a window facing the path), Bart's painted wagon (*Caravans And Wagons*) on the green.
- **The three empty plots** (Decided 23) are visible now as fenced, empty lots with a sign: they cost nothing, tell the player the village has room, and fix where residents go later.
- **Edges:** trees all round, one road out east with a closed bridge (*Wooden Bridge*): the place 4i/Phase 5 expansion attaches.
- No building interiors besides Tally Ho! in 4h: a house door gives a knock bark from whoever's home ("not now, i'm being a wizard").

---

## 7. Scene architecture

**Recommendation (H1): `Kariaston.unity` is a new scene, loaded additively next to `Tavern.unity` for the daytime; both unload or load at phase changes through `GameFlow`.**

| Phase | Loaded content |
|---|---|
| Daytime | `Tavern` + `Kariaston` |
| Evening (Prep, service, Results) | `Tavern` |
| Delve | `Dungeon` |
| Night | `Tavern` |

- `GameFlow.LoadContent` grows from one scene to a **content set** (a small list), with the same cover/reveal. Tavern stays the authority on the property; Kariaston holds the village. They talk only through Shared (`SurfaceClock`, events, `SurfaceDoor` ids), and the keeper object (Tavern's `PlayerTavern`) simply walks between them.
- **World placement:** Kariaston sits at a fixed world offset (e.g. +200 tiles in x), so the two scenes' tilemaps, colliders and nav grids never overlap. A **`SurfaceDoor`** (the generalised `AreaPassage`) fades, moves the keeper and switches the camera bounds and lighting area. Doors are instant (no scene load), so going in and out is cheap enough to do on a whim, which is the whole point of A.
- **Lighting:** each scene keeps its own 2D lights; an `AreaLighting` switch enables only the active area's global light, because two global lights on the same sorting layers both apply. Proven first in Step 1.
- **Persistent state** lives in `GameState` (Boot), never in scene objects: the clock, Vigor, the garden, story. NPCs are re-placed from schedules whenever a scene loads (§15).
- **Layout ownership (H13):** a `KariastonBuilder` creates the scene **once** with a painted blockout from a small layout description; after that the tilemaps and decor belong to the scene and your hand edits. The idempotent updater touches only named gameplay objects (doors, anchors, beds, interactables, spawners), never tiles, the same rule as the dialogue graphs.
- **Assemblies (H15):** pure rules in `Hearthdelve.Shared` (`Shared/Surface`, `Shared/Farming`, `Shared/Schedules`); scene behaviour for the village in a new `Hearthdelve.Village` (references Shared, Core and Tavern, for the interaction system and the keeper; never Dungeon). UI stays in `Hearthdelve.UI`. Story adapters stay the only Pixel Crushers users.

**Alternatives considered:**
- *Swap scenes at the door* (Tavern ⇄ Kariaston): simpler to load, but every door costs a full Tavern rebuild (furniture, nav, staff), and NPCs who cross (Maximo coming in for lunch) need a hand-off. Rejected for feel.
- *One big surface scene* (the village inside `Tavern.unity`): one load, but it bloats the scene that the in-place updater manages, and every later village expansion touches the tavern. Rejected.

---

## 8. Surface clock architecture

**One source of truth:** `SurfaceClock`, pure C# in `Shared/Surface`, owned by `GameState.Today` and advanced by one `SurfaceTime` MonoBehaviour in Boot. Nothing else reads `Time.time` to decide the hour.

- **State:** minute of the day (0–1439), running flag, the day it belongs to. Tunables in a `SurfaceClockConfig` asset: day start 8:00, cutoff 17:00, **real seconds per game minute (default 3.9 s: 9 game hours ≈ 35 real minutes)**, display step 10 minutes, max real seconds per frame (0.1 s).
- **Advancing:** `Tick(realSeconds)` adds `min(dt, cap) / secondsPerMinute`; stops at the cutoff. `SurfaceTime` passes the scaled `Time.deltaTime`, so `MenuPause` and `Time.timeScale = 0` stop it for free.
- **Pause sources** (each a named reason, so tests can see why it's paused): a conversation (`StoryServices.Conversations.IsTalking`), a full-screen panel (`MenuPause`), a scene transition (`GameFlow.IsLoading` or the cover), Decorate Mode (tunable; default on, so decorating never feels rushed), story scenes, and the window losing focus (§40). Character creation happens before day 1's clock exists. Interiors: the clock keeps running in Tally Ho! by default; a tuning flag can pause it indoors, for the playtest to compare (the brief's open item).
- **Bands and events:** `SurfaceBand` {Morning 8–12, Afternoon 12–17, Evening 17+}. Crossing a band, and each schedule step (every 10 game minutes), publishes `SurfaceTimeChanged` on the `EventBus`; schedules, lighting, the market and barks listen to that.
- **Queries for content:** `HH_Time()` (minutes, for conditions like "after 15:00") and `HH_Band()` in Lua; `SurfaceClock.IsOpen(hours)` for the market.
- **Saving:** the minute is saved with the rest of the daytime state (version 10, B); Continue resumes the day at that minute, in Tally Ho!.
- **Lighting:** a `DaylightDirector` per scene sets the global light from a colour curve over the minute (soft morning, plain noon, gold toward five, dusk held after the cutoff). Readability first.

## 9. 5 PM behaviour

At 17:00 the clock stops and `SurfaceBand.Evening` begins:

- The market packs up (the stall's own *Stall Pack Back* animation), and the stall says "closed till morning".
- Schedules switch to their evening entries: most people go home (lights in windows), a few have evening spots (Bart on the green, Maximo at the memorial, sometimes Kaloren on his tower's step). Daytime Visitors leave.
- Garden work that needs daylight stops ("too dark to work the beds"); harvesting stays allowed (picking what's ready never feels like a penalty).
- A single gentle cue: the light turns to dusk and Orik says it once if the keeper is in the tavern ("that's five. the village is putting its boots by the door."). No banner, no timer.
- **Nothing forces Prep.** The keeper can walk, talk to whoever is out, decorate, check the storeroom, cook the delve meal, sit around. *Begin Evening Prep* at the menu board, whenever.

---

## 10. Vigor architecture and initial tuning

Pure rules in `Shared/Surface/Vigor` (no engine types):

```
VigorRules: Max(config, upgrades) · Current · CanAfford(cost) · Spend(cost) → bool · Refill() (on sleep)
VigorConfig (asset): basePips, activity costs (by activity id), whether harvesting costs
```

- **State:** `GameState.Today.VigorSpent` (current = max − spent), saved (version 10). Refilled in `DayRules.Sleep`, nowhere else (no food restores it in 4h).
- **Consumers in 4h:** only the garden (prepare and plant a bed; tend a bed). Activities are named data (`VigorActivity` ids) so fishing, ranching and gathering later only add a cost entry.
- **Never consumed by:** walking, talking, shopping, inspecting, the storeroom, decorating, Prep, service, the delve. A test asserts each of these leaves Vigor untouched.
- **At 0 Vigor:** strenuous actions show "too tired for that today" and do nothing; everything else works.
- **Presentation:** a row of small pips next to the clock (surface HUD only; never in the Hollows, where the Essence bar alone speaks), spending one dims it with a short feedback (squash, soft sound, a light haptic tick); 0 shows the row greyed.

**Initial tuning (for the B playtest, not locked):**

| | Start | Range to try |
|---|---|---|
| Daily Vigor | **6 pips** | 5–8 |
| Prepare and plant a bed | **2** | 1–2 |
| Tend a bed (once a day) | **1** | 1 |
| Harvest a ready bed | **0** | 0–1 |
| Beds in the starter garden | **4** | 3–6 |

With 4 beds and 6 pips, the keeper can't plant everything and tend everything on the same day: the decision the brief wants ("what do I spend today's Vigor on?"), with no wrong answer.

---

## 11. The farm and garden slice

- **Representation (H4):** a handful of **fixed garden beds**, each an object with a stable id (`garden_1`…`garden_4`), a 3×2-tile footprint and its own crop state. Not a free tile grid: fixed beds are simpler to save, test and present, and match the "fixed sites, not city-building" principle. More beds later are more ids.
- **The loop:**
  1. *Prepare and plant* (Vigor 2): pick a crop from a short list at the bed (seeds free, H6); the bed shows seeds.
  2. *Grow over days*: one growth day per night, at sleep.
  3. *Tend* (Vigor 1, optional, once per bed per day): the Farm pack's animated action icon plays over the bed, the soil darkens. Tending on at least half the growing days makes the harvest Fine instead of Standard.
  4. *Harvest* (free, when ready): the produce goes straight into the storeroom, fresh (like a market purchase); the bed is empty again. A small, satisfying pop and a one-line feed ("3 onions, fine").
- **Forgiving (H5):** an untended crop still grows; neglect only costs the Fine quality. A ready crop waits in the bed indefinitely (no rotting in 4h). Nothing dies. A tuning switch (`untendedPausesGrowth`) exists for the playtest to try the stricter rule.
- **Deterministic growth:** `GardenRules.GrowOvernight(beds, day)` runs inside `DayRules.Sleep`. Each bed remembers `lastGrownDay`; a bed grows only if `lastGrownDay < day`, so reloads, scene loads and repeated calls never double-grow. Pure, EditMode-tested.
- **Data:** `CropDefinition` (ScriptableObject: id, localized name, produced ingredient, growth days, stage sprites for seeds/1/2/3/ready, yield, Fine threshold, tending behaviour). `GardenConfig` for bed count and the switch.
- **The keeper's farming pose:** Minifantasy's *Farming Animations* exist only for the bare race bodies, not the four clothed keeper bodies (§29). The keeper faces the bed and plays a short existing pose; the Farm pack's animated action icons (plough, seed, water, 16×16) carry the action over the bed.

## 12. Initial crops and growth timings

All three feed recipes already on the menu, so a harvest is immediately cookable ("I grew part of tonight's menu"):

| Crop | Produces | Days | Yield | Art | Feeds |
|---|---|---|---|---|---|
| **Herb bed** | herbs | **2** (quick) | 3 | Farm/More Veggies leafy crop (spinach or celery stages, chosen on the sheet) | grilled spider leg, crispy bat wings, spider leg steaks, onion broth |
| **Onions** | onion | **3** (ordinary) | 3 | *More Veggies*: Onion | cellar stew, offal pottage, onion broth |
| **Barley (wheat art)** | malt | **4** (ordinary, longer) | 2 | Farm: Wheat | Kariaston ale, gelbrew |

Malt from barley is a simplification (the threshing and malting are off-screen); flagged in case you'd rather grow wheat and keep malt a market good. Bread and eggs stay market-only (baking and hens are Phase 5's ranching and processing).

---

## 13. Ingredient and economy integration

- Harvests enter the existing storeroom as ordinary `IngredientStack`s (Standard or Fine, fresh), so freshness, Prep, recipes, scoring and staff all work unchanged.
- **The hierarchy holds:** the garden only grows staples the market already sells. Its value is convenience, quality (Fine) and attachment, not profit: a full bed is worth about 6 gold at market prices. Delve nights stay the money (Decided 30); `BalanceReport` gains a "garden week" row and `RenownAndBalanceTests` a check that a garden-only week never beats a delve week at the same skill.
- Facts for the story: `CropPlanted`, `CropTended`, `CropHarvested(cropId, count, quality)` and `ServedHomegrown` (a dish served that used a harvested ingredient; tracked by an origin tag on the stack, which merges like quality). Boog notices homegrown herbs.

## 14. Market integration and hours

- **The stall** in the square is a `TavernInteractable`-style interactable whose use opens the existing `MarketPanel` with the existing `SupplySource`. No economy changes.
- **Hours:** `MarketHours` (pure) says open from 8:00 to 17:00; `DayRules.Buy` gains the hours check (still Daytime-only). Closed, the stall's hint says so and shows its packed-up sprite.
- **Grim (H7)** stands at the stall in the C checkpoint; in A and B the stall works with nobody behind it (a sign: "pay the jar"), which is also a nice small joke about Kariaston.
- Shopping costs no Vigor and no time (the panel pauses the clock).

---

## 15. NPC schedule and presence architecture

Small, authored, data-driven:

- **`ScheduleDefinition`** (ScriptableObject, one per character): a list of **blocks** `{from, to, anchor id, activity, condition}`; the first block whose time and condition match wins. Activities: `stand`, `sit`, `wander(radius)`, `work` (an animation), `home` (not visible), `visit`.
- **Conditions** (pure, small): `everyNthDay(n, offset)`, `chance(p)` (seeded by the world seed + day + character), `storyFlag` (a Hearth & Hollows flag or opening stage), `questObject(id, status)`, `weekdayless` by design (no calendar). Combined with AND only.
- **`ScheduleRules.Resolve(schedule, day, minute, world)`** → the block. Pure, EditMode-tested; the whole village's positions are a function of `day + minute + story`, so **no NPC transform is ever saved**.
- **Anchors:** named transforms in either scene (`square.memorial`, `tavern.bar.stool3`, `cottage.window`…). A block names an anchor; the scene that owns it hosts the character.
- **`SurfaceCast`** (one per loaded scene) spawns the characters whose current block is in its scene, and on each `SurfaceTimeChanged` re-resolves: if the new anchor is in the same scene and the character is visible, they **walk** there (grid A*, a few at a time); otherwise they **appear** there. Crossing scenes (Maximo going into Tally Ho! for lunch) is walk to the door, disappear, appear inside the door.
- **`VillagerAgent`**: thin MonoBehaviour, the existing `CharacterSpriteAnimator` and A* movement, a talk interactable bound to the character's `CharacterDefinition.conversation`, an optional bark bubble.
- **Discoverability:** important characters always have a findable daytime block; a hub conversation never depends on the minute unless the line is optional texture.

## 16. Recommended initial cast per checkpoint

| Checkpoint | Who appears | How much |
|---|---|---|
| A | Boog, Orik (daytime posts, existing hubs) | presence + talk only |
| B | (none new) | the garden is the test |
| C | Maximo, Kaloren, Grim, Ogrin, Bart | a definition, figure, portrait, schedule, a hub with a first meeting and an everyday branch, 2–4 callbacks, a few barks each |
| D | Gimp; Glimmer's hint; villagers as patrons; Bart's evening performance | the community layer |
| (throughout) | Old Phi, Karias, the Fortunate Five | objects, memorial, talk |

Smallest amount that makes Kariaston specific: five residents, one recurring visitor from below, two absent people.

---

## 17. Character relationship web

The rule: everyone has opinions of someone other than Bram. *Proposed* entries are first readings for you to correct; locked facts are marked.

| Pair | What's there before Bram | Shown in 4h by |
|---|---|---|
| **Phi ↔ Orik** | *(locked)* old friends; he ran Tally Ho! in her absence, left when the Hollows got bad, she found him again | his hub; her chair; the ledger in two hands |
| **Phi ↔ Maximo** | *Proposed:* he considered her the only real adventurer in Kariaston, a "fellow member of the profession"; she humoured him and once told him no, kindly, about going down together | his talk; a toast he still makes |
| **Phi ↔ Boog** | *(built)* she let him keep his bomb; *proposed:* she's the only one who ever ate his experiments first | Boog's lines |
| **Fortunate Five ↔ village** | *Proposed:* five tankards on Tally Ho!'s shelf that nobody drinks from; Bart knows half a song about them and refuses to sing the half he doesn't know | inspectable; Bart bark |
| **Maximo ↔ Karias** | *(locked)* mentor and friend; Karias died in the Hollows; Maximo never went back; the village bears his name | memorial; Maximo's grief under the comedy |
| **Maximo ↔ Orik** | *Proposed:* the mayoral account, a tab Orik keeps "in a separate book, for my health" | barks; Orik's talk |
| **Maximo ↔ Boog** | *Proposed:* Maximo wants Boog's explosions for civic fireworks; Boog wants a civic budget for explosions | NPC barks |
| **Maximo ↔ Bart** | *(GDD proposal)* Maximo thinks they're fellow artists; Bart thinks Maximo is a fan | barks on the green |
| **Kaloren ↔ Kariaston** | *Proposed:* the helpful wizard everyone asks to warm their ovens, who never does it with fire | talk; barks |
| **Kaloren ↔ the Hollows** | *(locked, hidden)* became a lich there, came back, phylactery below | near-misses only (§19) |
| **Kaloren ↔ Maximo** | *Proposed:* two survivors of the Hollows, one who went down and came back strange, one who never went back; neither knows the other's half | they share a bench; one optional exchange |
| **Grim ↔ Ogrin** | *(locked)* the dwarf who looks after the sick orphan boy | together on good days; the window on bad ones |
| **Grim ↔ Orik** | *Proposed:* two dwarves who agree on nothing but weights and measures; Grim weighs, Orik counts | NPC barks at the stall |
| **Boog ↔ Gimp** | *(locked)* old friends; explosives | D's visit |
| **Bart ↔ Tally Ho!** | *Proposed:* plays there some evenings for supper; Orik pays him in stew and complaints | D's performance |
| **Ogrin ↔ Glimmer** | *(locked, future)* her questline; *4h:* "my light" at his window, unexplained | D's hint |
| **Ogrin ↔ Boog** | *Proposed:* Ogrin's favourite person to watch; Boog promises him "a small explosion, for your birthday" | barks |

Love/Hate: Maximo, Kaloren, Grim, Ogrin, Bart and Gimp get tracked `CharacterDefinition`s with values (craft, nerve, warmth) so the existing deeds reach them through the same rules. Proposed values: Maximo nerve 80, warmth 60, craft 0 (admires the deed, not the technique); Kaloren warmth 70, craft 60, nerve −20; Grim craft 70, warmth 30, nerve 0; Ogrin nerve 70, warmth 60; Bart warmth 50, nerve 40 (a good story); Gimp nerve 90, craft 70. Deed learners grow a `Villagers` group (by kind) so the troll's fall reaches the village. NPC-to-NPC feelings stay authored text, not simulated (no rumour networks, Decided 37).

## 18. Maximo and Karias

- **Look:** the King figure (old, white beard, a crown he insists is a "chain of office", red cape), H8. His old armour (the Jousting Knight) is a later costume for a civic occasion.
- **Routine:** a proclamation at the memorial mid-morning (a bark to the square, audible if near), lunch at Tally Ho! (sits at a table in the daytime tavern: someone is always sitting in Tally Ho! at noon), afternoon "inspecting the defences" (a fence), home at five; after five on some days he stands at the memorial alone.
- **The contradiction, shown not told:** he talks about adventure like scripture and **walks round the hatch** when he's in Tally Ho!. His hub reads `HH_TimesDefeated` and the keeper's delve count: first delighted ("a delver! under my village!"), later, at a threshold, one quiet line that is about Karias without naming him.
- **Karias:** present through the memorial (an inscription line, inspectable), Maximo's stories (his staff "held like a wish come true", from your notes), the village's name, and nothing else. No twist, no survival.
- **First meeting:** a grand welcome speech with one honest sentence in the middle.

## 19. Kaloren: presentation and foreshadowing

- **What the player knows:** a courteous, helpful wizard in the tower; slightly cold to the touch; gloves in summer; orders at Tally Ho!, admires the plate and doesn't eat (D); knows a little too much about how the cellars were built.
- **What stays hidden:** that he's a lich, that the Hollows made him, the phylactery. Nothing in 4h confirms any of it.
- **Near-misses (three or four, each once):** forgetting to breathe while listening; a comment about the Cellars' stonework "before the third collapse"; looking at the hatch too long (the opposite of Maximo, who won't look at it); declining to be warmed by the fire ("i'm fine. i'm always fine").
- **Look:** a Myriad NPC composition (old human, albino skin, long white hair and beard, the long hat, a robe colourway) in the layered NPC pipeline already used for patrons. The Lich figure is kept for whenever the story wants a reveal.
- **Kindness first:** his hub is mostly him being useful and gentle; the strangeness is seasoning. If Q2 is yes, he makes Ogrin's remedies and won't take payment.

## 20. Grim and Ogrin

- **Grim** (H7): keeps the market stall; gruff, fair, never haggles, charges the same to everyone; every coin goes to remedies. Not only "worried guardian": he has a trade, a rivalry (Orik), opinions (about the keeper's knife, about Bart's volume) and a dry warmth he hides.
  - *History (Q1) is yours.* Two shapes to choose from later, neither used in 4h: (a) Ogrin's parents were his friends; (b) he found Ogrin, the way you find a stray, and never decided to keep him, he just did. Until then Grim deflects: "he's mine. that's the whole story."
- **Ogrin:** bright, curious, opinionated; draws maps of the Hollows from what he overhears and asks the keeper to correct them; collects the keeper's stories ("what did you fight?", reading the delve facts and curios brought home); loves Bart's songs and watching Boog's explosions from a safe distance; hates onion broth and being called brave.
  - *Agency:* he wants to see the hatch; he gives the keeper names for monsters ("that's not a slime, that's a Gerald"); he trades his maps for stories.
  - *Good and bad days:* seeded by the world seed (about two good days in three). Good days he's at the stall or on the green with Grim; bad days he talks to the keeper **through his window**. The illness is never named or described (your call).
- **Looks:** Grim the Miner figure (distinct from Orik's yellow beard and the dwarf keeper); Ogrin a child figure from *Snowball Wars* or *Summer Holidays* (8 px tall against the keeper's 10), chosen on the sheet in Step 8.

## 21. Bart

- **Proposal (H9): the first Visitor who stayed.** He came through Kariaston years ago, played one night at Tally Ho! and never left; he lives in a painted wagon on the green. That makes him the living version of the path residents will take in Phase 5 (Visitor → resident), without building that path.
- **Where:** the green in the late morning (playing; music notes, passers-by barks), the market at midday (gossip with Grim), Tally Ho! some evenings in D (a performance spot by the hearth: ambience, no gameplay effect in 4h).
- **How he differs:** Maximo makes speeches about heroes; Bart sings about ordinary people and gets the facts slightly wrong on purpose. Boog is enthusiasm, Orik is precision, Bart is the village's memory and gossip.
- **Hooks:** he hears about the keeper's deeds (through the existing deed memories) and they turn up in his songs (callbacks, "the ballad of the troll, verse two"); later, a commissioned song or a rumour that points a delve. No new systems.
- **Look:** the Wise Orc (an old orc with presence; reads as an orc at 8 px). The True Heroes II Bard has playing animations but is a human; an orc-skin recolour from Minifantasy's ramps is the fallback if you prefer the instrument (H8).

## 22. Old Phi and Orik

- Orik's history stays as built (the three lines heard if you ask). In 4h it gains **places**: her chair by the hearth (inspectable; he dusts it, "don't tell Boog"), the ledger written in two hands, the five tankards.
- Orik's daytime hub gets one or two lines tied to the new world: the village asking after her (Maximo, Bart), and the first time the keeper sleeps upstairs ("that was her room once. it's a room. sleep in it.").
- Not in 4h: why she went back down, the Fortunate Five's history, any letter beyond the one built.

## 23. Gimp: the visitor proof

**The experience:** the Hollows have neighbours, and Boog had a life before you.

- **Visit rule (H10, pure and seeded):** first visit the day after Boog's bomb comes home, or day 6, whichever is first; then about one day in three, never two days running. Derived from `day + world seed + story`; nothing saved but a Dialogue System "has met" variable.
- **The visit:** at about 14:00 the cellar hatch in Tally Ho! opens and Gimp climbs out (the hatch prop already exists from the opening). Boog leaves the stove; the two sit at the bar with a ledger of blast radii. They bark to each other (pairs, §27). At 16:30, or when Prep begins, he goes back down.
- **Talking:** `Gimp/Hub` (seeded once): first meeting (Boog introduces his oldest friend from below, mid-argument), an everyday conversation (fuses, the upper Hollows, Boog chiming in), and a few once-only callbacks to deeds he hears of (the troll, the bomb). Hearth & Hollows owns when he's there; the graph owns what he says.
- **Architecture proof:** Gimp is a `CharacterDefinition` (a new kind value `Hollower`, H8 note: shown to the player only as a person, never a category), with a schedule whose blocks are conditional visits. That is the whole of *encounter → recurring visitor* the architecture needs; *guest* and *resident* stay Phase 5.
- **Look:** the Hunter (a tanned woodsman, your campaign Gimp), distinct from Boog's Goblin Sapper. His portrait: a human with the "engineer" goggles from the Portrait Generator.

## 24. Glimmer foreshadowing

**Recommendation (H11):** surface only. On some evenings (seeded, after 17:00, never before Gimp's first visit), a small drifting light (the Naughty Fairy's fly loop, desaturated) hovers at Ogrin's window. If the keeper talks to Ogrin then, he says "my light came again" and won't explain. That's all: no name, no meeting, nothing about guardians or seals.

*Alternative:* the old Checkpoint D's one-time meeting in the Cellars' second floor. It's a bigger reveal and lives in the Dungeon, which 4h otherwise doesn't touch; I'd keep it for when her questline is planned.

## 25. Visitors and patrons

- **Today's generated customers become Visitors** in name and id (they already use `visitor/<day>/<visit>` ids and are never saved). No behavioural change.
- **Named villagers as patrons (H12, in D):** at Prep, a pure `PatronRules.Tonight(day, world, schedules)` picks 0–2 named villagers who are free that evening (Maximo most nights, Bart on performance nights, Grim now and then, Kaloren rarely: he orders, admires the plate, pays, doesn't eat). Each has a `CustomerProfile` with his figure and preferences; service rules don't change. A villager patron publishes the same facts with their stable id, so dialogue can remember the evening.
- **Daytime Visitors:** 0–2 generated Visitors sitting in Tally Ho! with a drink in the daytime (ambience; no service), and one or two crossing the square in the morning (the market has customers). Transient, never saved.

## 26. Friendly-Hollower future compatibility

- Species-agnostic everything: `CharacterDefinition` by stable id, figure by prefab, dialogue by hub, relationships by stand-in. A Mushroom Person (Decided 50) or any authored creature needs content, not systems.
- Schedules already express "comes up some days"; *Inn guest* will be another block kind (a room anchor) and *resident* another home anchor on one of the three plots.
- Never generated, never an enemy definition reused as a person.

## 27. Existing system integration

| System | 4h integration | Rule |
|---|---|---|
| Market | physical stall, hours | `SupplySource`, `MarketPanel`, `DayRules.Buy` unchanged except the hours check |
| Storeroom | shelves → panel | the existing stock view |
| Delve meal | stations in the daytime | `CookDelveMeal` unchanged |
| Decorate Mode | key and plans book; pauses the clock | unchanged; Kariaston is **not** a decoratable area in 4h |
| Guest room | Bram's room in the daytime (bed = Sleep at Night) | still a `PropertyArea` with its saved layout; the bed is furniture |
| Prep | from the menu board | `StartEvening` unchanged; Kariaston unloads |
| Service | named patrons added in D | `ServiceSession` unchanged |
| Conversations | new hubs; the clock pauses while talking | `IConversationService`; new `HH_` functions only as needed: `HH_Time`, `HH_Band`, `HH_Here(id)`, `HH_Harvested(crop)` |
| Barks | NPC-to-NPC pairs near the keeper | Dialogue System bark conversations (`Bark/<pair>`) shown in `SpeechBubble`s, chosen by a small `AmbientBarks` director (a pair co-located, the keeper within ~8 tiles, cooldowns) |
| Visitors | renamed concept; daytime ambience | ids unchanged, never saved |
| Phase transitions | content sets per phase | `GameFlow` |
| Save | version 10 in B | `SaveSystem` the only save |
| Night | unchanged panel; the bed also sleeps | `NightScreen` |
| Facts | new: `SurfaceTimeChanged`, `PrepStarted`, `CropPlanted/Tended/Harvested`, `ServedHomegrown`, `VisitStarted/Ended(characterId)`, `CharacterMet` | `EventBus`, stable ids |

---

## 28. Minifantasy assets proposed

Catalog first (`C:\Dev\Minifantasy\List`), sheets inspected for this plan; exact cells are recorded in `docs/ASSET_MAP.md` as each is imported. The figure choices are on `docs/plan_4h/cast_candidates.png` (true scale, with the keeper and Orik for reference).

**Tally Ho! and grounds**
- Exterior: *Towns II* wooden plank or stucco building tileset (walls, roof, door, windows), *Slate Roof And Humble Chimney*, the tankard hanging sign (*Towns* props / *Shop Signs Medieval City*), *Outdoor Lanterns*.
- Grounds: *Farm* tileset and props (fences, scarecrow, hay), *Hen Nests* and *Towns* hens and chicks (decor only), *Cart*, barrels.
- Interior additions: a bed for the upstairs room (*Towns II* props / *Tavern Indoor*), shelves (existing catalogue), a chalkboard (*Tavern Indoor*), the five tankards (*Tavern Indoor* props), Phi's chair (an existing armchair, Q4's framed portrait optional).

**Kariaston**
- Ground and paths: *Towns* tileset (cobbles, dirt, grass transitions), *Forgotten Plains* and *More Grass Variations*, *Plants & Foliage: Plains and Forests* (trees, bushes), *Wall Of Trees* for the edges.
- Buildings: *Towns II* brick, stucco and plank buildings (Maximo's house, Grim's cottage), *Wizard Tower* (Kaloren), *Caravans And Wagons* (Bart), *RTS Houses* or *Wooden Hut* for variety; *8x8 Flags* on the mayor's house.
- Square: *Animated Well*, *Town Monuments* (Karias's memorial), *Towns Fountains* (alternative), benches and lamps (*Towns*, *Medieval City* props).
- Market: *Merchant*'s stall (`Stall`, `Stall Setup`, `Stall Pack Back`: the 8:00 and 17:00 moments) or *Travelling Merchant*'s shop with opening and closing frames; produce on the counter from *Farm* crop icons.
- Edges: *Wooden Bridge* (the closed road), *Damaged Signs* ("bridge out").
- Empty plots: *Farm* fences, a *More Signage* sign.

**Garden**
- *Farm*: `SeedsAndCrops` (wheat stages; 15 crops with seed, 3 growth stages and ready), `ActionInProgress` (16×16 animated plough, seed, water icons), tileset (tilled soil).
- *More Veggies*: Onion; a leafy crop for the herb bed (spinach or celery).

**People** (H8)
| Character | Figure | Portrait (Portrait Generator) |
|---|---|---|
| Maximo | *King* (walk, idle; crown and cape) | human, old, white beard, a hat or none (no crown in the generator) |
| Kaloren | *A Myriad Of NPCs* layers (human albino, long white hair and beard, long hat, robe) | human, albino skin, robe, white beard |
| Grim | *Miner* | dwarf, gruff |
| Ogrin | *Snowball Wars* or *Summer Holidays* child figure | gap: no child parts; the smallest human or halfling face with "innocent" eyes as an approximation |
| Bart | *Wise Orc* (fallback: *True Heroes II* Bard with an orc skin ramp) | orc |
| Gimp | *Hunter* | human with the "engineer" goggles |
| Glimmer | *Naughty Fairy* (fly, appear, disappear; desaturated) | none: she's a light |
| Merchant stall extras (D) | Visitors from the existing *Myriad* pools | none |

## 29. Real art gaps

1. **The keeper's farming animations.** *Farming Animations* (plough, seed, water) and *Writing Down* and *Sleeping* exist only for the bare race bodies, not for the four clothed keeper bodies. **Workaround:** the keeper faces the bed and plays an existing pose; the Farm pack's animated action icons show the action. Good enough for a small garden; worth revisiting only if farming grows.
2. **A child portrait for Ogrin.** The Portrait Generator has no child parts. **Workaround:** the closest small human or halfling face; or Ogrin speaks without a portrait (a framed silhouette), which suits a boy you mostly see through a window. Your call in Step 8.
3. **An orc playing an instrument.** No orc figure has a playing animation. **Workaround:** the Wise Orc with music-note emotes and an instrument prop beside him (*Musical Instrument Icons*), or the human Bard recoloured.
4. **Ogrin in bed.** *Sleeping Animations* are adult base bodies. **Workaround:** the window (we never see inside the cottage in 4h).
5. **No dedicated mayor.** The King's crown is the joke ("chain of office"); if it reads wrong, the Myriad layers make an elderly man with a doublet.

## 30. Optional unowned purchases

**None recommended.** The village, garden, market and every named character are achievable with the owned library. The one real gap (farming animations for the clothed keeper bodies) isn't filled by any pack I could verify in the catalogue: Minifantasy's farming animations are drawn for base bodies across the line. If you know of a pack with clothed-body farming or a child character set, it would solve gaps 1 and 2; otherwise the workarounds above stand.

---

## 31. Save and schema implications

**Version 10 (H14), in Checkpoint B.** New fields:

```
SaveData.world   { seed }                                   // per-game; schedules, good days, visits, Glimmer
SaveData.surface { minute, vigorSpent }                     // today's clock and Vigor (meaningful only in Daytime)
SaveData.garden  { initialized, beds: [ { id, crop, plantedDay, growthDays, tendedDays, lastTendedDay, lastGrownDay } ] }
```

Not saved, by design: NPC positions (derived from day + minute + story), visits (derived; "has met" is a Dialogue System variable already saved with the story), Visitors, ambient barks.

**When it saves:** the existing points, plus a daytime autosave after each Vigor action, each purchase (as now) and *Begin Evening Prep*. Continue mid-day restores the minute and Vigor and starts the keeper in Tally Ho!.

A Checkpoint A save is version 9 (the clock isn't saved yet: Continue starts the day at 8:00, which is harmless before Vigor exists).

## 32. Migration plan

- **9 → 10:** world seed generated once (stored, so it's stable from then on); surface minute = day start, Vigor full; garden initialized with the starter beds empty. Only what didn't exist; once (`initialized`), like 4f's furniture.
- **7 → 10 and 8 → 10:** through the existing chain, then the step above.
- **Story:** old saves are past the opening (as now); the new villagers' first meetings play normally (they're new people, not missed story).
- **Tests:** v7, v8, v9 and v10 round trips; a v9 save loaded twice gives the same seed and the same village.

---

## 33. EditMode test plan

- **Clock:** advancing by real seconds; the per-frame cap; stopping at the cutoff; pause reasons (each); bands and their events; minute formatting; restore from save.
- **5 PM:** cutoff stops the clock; nothing changes phase; market closed; evening blocks resolve.
- **Vigor:** spend; insufficient; zero blocks only strenuous activities; refill only on sleep; walking, talking, shopping, decorating, Prep, service and the delve never touch it (rule-level).
- **Garden:** plant (cost, bed state); growth per night; `lastGrownDay` prevents a double grow (repeated `Sleep`, reload); tending and Fine quality; the untended switch; harvest into the storeroom (count, quality, freshness, origin tag); a ready crop waits.
- **Market hours:** open window, closed window, `DayRules.Buy` refuses when closed.
- **Schedules:** resolution by time; conditions (every-Nth, chance, story flag, quest object); determinism for a seed; Gimp's visit rule (never before the bomb or day 6, the first guaranteed, never two in a row); Ogrin's good days; patrons tonight.
- **Save:** version 10 round trip; 9 → 10, 8 → 10, 7 → 10 migrations; the seed stable across loads; no garden double grant.
- **Story data:** every new character complete (id, kind, name in the table, portrait, conversation, values); faction database matches; hubs follow the priority order; once-only flags use `~= true`; new English fits its boxes and passes the style and glyph checks; Tamsin and other old names absent.
- **Balance:** a garden-only week never beats a delve week (`RenownAndBalanceTests`).

## 34. PlayMode test plan

- Waking in Tally Ho! upstairs; walking (Tavern map active in the daytime); the stairs; the front door to Kariaston and back; the camera and lighting switch with the area.
- The storeroom shelves open the panel; the delve meal at a station; Decorate from the key and the book (clock paused).
- Talking pauses the clock (and resumes after); a full-screen panel pauses it; the transition cover pauses it.
- Market open before 17:00, closed after; a purchase saves.
- 17:00 doesn't start Prep; the keeper can still walk and talk; *Begin Evening Prep* starts Prep and unloads Kariaston.
- Vigor HUD shows and dims; planting and tending spend; 0 Vigor refuses planting; walking and talking still work at 0.
- Crops grow over several sleeps; harvest reaches the storeroom; a harvested ingredient cooks in service (`ServedHomegrown`).
- Service and the delve unaffected by Vigor (Essence, damage, drain identical with 0 and full Vigor).
- NPC presence changes with the clock (Maximo at the memorial, then lunch in Tally Ho!); talking to villagers at different times.
- Gimp's visit (hatch, bar, leaves before Prep); villager patrons in service.
- Save mid-day, quit, Continue: same minute, Vigor and beds; a version 9 save continues into a working village.
- Full regression: every existing Dungeon, Tavern and Story suite.

## 35. Web test plan

- A fresh development build at each checkpoint's end; a short visible-tab smoke test (not a full replay): New Game → arrival → day 2 morning → out the door → market → garden → back → Prep; save and Continue; one migrated old save.
- **Hidden-tab safety:** the clock never advances from wall time; a capped per-frame step plus a pause while the page is unfocused/hidden (`OnApplicationFocus`, `OnApplicationPause`) means a suspended tab stops the day instead of skipping it. An EditMode test feeds a 300-second frame and expects at most the cap. A hidden tab is never used to judge timing.
- Additive scene loading time measured on web (the door must stay instant; only phase changes load).
- Save persistence (IDBFS flush after daytime autosaves).
- Controller fallback and haptics degrading silently on web.
- Pixel-perfect presentation in the village; performance with all villagers in view.
- Back up the owner's browser save before testing and restore it after.

## 36. Performance risks

- **Two scenes loaded in the daytime:** memory and load time on web. Mitigation: Kariaston is mostly tilemaps; measure in Step 1; the door is a passage, not a load.
- **NPC count:** at most ~8 named characters, 2–4 transient Visitors. A* only when a block changes; off-screen characters appear rather than walk; animators idle off-screen.
- **Lighting:** a handful of local lights in the village (lamps, windows after 17:00), one global light active at a time.
- **Barks:** event-driven, cooldowns; no per-frame polling of pairs beyond a cheap distance check on band ticks.

## 37. 320×180 UI and presentation concerns

- **Surface HUD:** clock ("2:40 pm", Body 1×) and Vigor pips in one corner, nothing else; it fits a 12-pixel line. Hidden in the Hollows, so the three pressures never share a screen.
- **Prompts:** hints already fit (Silver 1×); new hint strings go through `TypographyTests`.
- **Dialogue box** covers the bottom third: speakers near the bottom edge of the view need the camera to allow it (village edges have margin).
- **Bark bubbles** can crowd the square: one bubble at a time per pair, short lines, cooldowns.
- **Small figures:** Ogrin is 8 px tall; his window needs to read at 1× (a lit window frame).
- **Crops:** stages must read at 8×8; the Farm stages were made for it.
- **Daylight:** the colour curve can't reduce text or sprite contrast (readability rule).

## 38. Expensive-to-reverse decisions

| Decision | Recommendation | Why it's expensive |
|---|---|---|
| Kariaston scene structure (H1) | separate scene, additive beside Tavern in the daytime | every village system, test and transition is built on it |
| Tally Ho! exterior and interior | exterior in Kariaston, interior stays `Tavern.unity` | moving the exterior later means re-doing the door, light and nav seams |
| Surface clock authority | `SurfaceClock` in `GameState`, one ticking component in Boot | schedules, lighting, market and saves all read it |
| NPC schedule representation | data blocks with anchor ids, resolved from day + minute + story | content authored against it; saves rely on positions not being stored |
| Farm representation (H4) | fixed beds with ids | save shape and content; a tile grid later would migrate |
| Crop state saving | per bed id with `lastGrownDay` | the duplicate-growth guarantee rests on it |
| Persistent and visiting characters | `CharacterDefinition` + schedule; visits derived, never saved | the Hollower → resident path builds on it |
| World seed (H14) | one per game, saved | every seeded rule depends on its stability |
| Layout ownership (H13) | generated once, then hand-owned tiles | reversing it means losing hand edits or regenerating |
| Map expansion boundary | the east road and the three plots | future areas attach there |

Already decided and **not** reopened: the soft clock, the 5 PM cutoff, player-chosen Prep, Vigor as the productivity cap.

Tunable, not foundational: clock speed, day start, Vigor capacity and costs, crop durations, schedule times, whether interiors pause time, bed count.

## 39. Deferred to Phase 5

Fishing (and its minigame), ranching, foraging and gathering; seasons, weather, a calendar; a seed economy, more crops, crop quality tiers beyond Fine, farm expansion, tools and upgrades; Vigor food, upgrades and perks; Inn guests and occupancy; Visitor promotion; residents on the three plots; recruitment; the full friendly-Hollower path; Glimmer's meeting and questline; Kaloren's lichdom and phylactery; Phi's reasons and the Fortunate Five's story; Karias's fate below; gifts and preferences at scale; romance; building interiors; a walkable night; NPC autonomy beyond authored blocks; decor relationship effects; Bart's performance as a mechanic; Morale's role.

## 40. Manual Unity and editor work for you

1. **Portraits** (Steps 8, 12): compose Maximo, Kaloren, Grim, Ogrin (or decide none), Bart and Gimp in the Portrait Generator app if the recipe route (`Tools/portraits`) can't express them; I'll give exact recipes and steps.
2. **Dialogue:** edit the seeded first drafts in the node editor as you like (hubs, first meetings, barks, inspectables).
3. **Kariaston's tiles:** after the first generation (Step 2), hand-paint and arrange as you wish; the updater won't touch tiles.
4. **Playtests** at each checkpoint's end (the acceptance criteria below), including a controller pass for the garden's haptics.
5. Answers to Q1–Q5 when convenient.

## 41. Implementation order

**Checkpoint A: I can live here**
1. *Surface architecture:* `GameFlow` content sets; `Kariaston.unity` created once with a blockout; `SurfaceDoor` passages; area camera bounds and lighting switch; `Hearthdelve.Village` assembly. Prove loading, doors and lights on web early.
2. *Kariaston in real art:* the layout above with Towns/Farm/Plains art, Tally Ho!'s exterior, the square, the four households (unoccupied), the garden's beds (inert), plots, edges. ASSET_MAP records.
3. *Tally Ho! as home:* Daytime walking; waking upstairs; storeroom shelves; stations for the delve meal; Decorate key and book; the menu board → Prep; Boog and Orik daytime posts; inspectables; the market stall (no keeper yet) with its panel; `MorningScreen` retired.
4. *The clock:* `SurfaceClock`, pause reasons, bands, the 5 PM wind-down, daylight curve, HUD clock, market hours, `HH_Time`. Tests; web build; **stop for the A playtest.**

**Checkpoint B: A day's work**
5. *Vigor:* rules, config, HUD pips, facts, tests.
6. *The garden:* `CropDefinition` (herbs, onion, barley), beds, plant/tend/harvest, overnight growth, storeroom delivery, feedback (MMF + named haptics), balance row.
7. *Save version 10* and migrations; daytime autosaves; Continue mid-day. Tests; web; **stop for the B playtest.**

**Checkpoint C: The village has people**
8. *The cast as data:* `CharacterDefinition`s (Maximo, Kaloren, Grim, Ogrin, Bart, plus Karias and Phi as untracked story ids), figures, portraits, faction regeneration, values, the `Villagers` learner group.
9. *Schedules and presence:* `ScheduleDefinition`, `ScheduleRules`, anchors, `SurfaceCast`, `VillagerAgent`; Boog and Orik moved onto schedules; Grim at the stall; Ogrin's good days and window.
10. *First conversations:* hubs seeded once (first meeting, everyday, a few callbacks each), memorial and other inspectables, `HH_Here`, `HH_Band`. Tests; web; **stop for the C playtest.**

**Checkpoint D: This is a community**
11. *The web between them:* NPC-to-NPC barks (`AmbientBarks`, pairs), deed callbacks for villagers, Maximo's delve-count lines, Orik's new lines.
12. *Gimp:* `Hollower` kind, visit rule, the hatch, the bar with Boog, `Gimp/Hub`.
13. *Evenings:* named villager patrons; Bart's performance spot; Glimmer's light; daytime Visitors.
14. *Closeout:* full regression, web build and visible-tab smoke test, docs, the report; **stop for the final 4h playtest.**

## 42. Playtest acceptance criteria

**A: I can live here**
- I can wake, walk all of Tally Ho! and upstairs, step out, cross Kariaston and come back without friction; the door feels instant.
- Every former Morning-panel function is reachable in the room, and I never miss the panel.
- I'd happily walk to the square for no reason. The village is neither too big to cross nor too small to matter.
- The clock reads clearly, never feels like pressure, stops while I talk or browse, and 17:00 feels like the village winding down.
- After five there's still something I want to do; I sometimes start Prep before five because I want to.
- Web: doors and clock behave; a hidden tab doesn't move the day.

**B: A day's work**
- Each morning I make a small real choice with Vigor, and no choice feels wrong.
- Vigor never makes me avoid walking, talking or exploring.
- Planting and harvesting feel tactile (feedback, haptics on gamepad); waiting a few days creates anticipation, not a chore.
- I cook something I grew, and it feels different from buying it.
- The Hollows still feel like the urgent part of the day; the garden never replaces the delve.
- Save mid-day and Continue: exactly where I was; an old save works.

**C: The village has people**
- I can find each villager without a guide and remember who they are after one meeting.
- Routines make the village feel alive (I notice Maximo has moved) and are never annoying (important people are findable).
- Each character has a distinct voice; Maximo is funny and sad, Kaloren kind and slightly wrong, Grim gruff and fond, Ogrin his own person, Bart unlike Maximo.
- None of the mysteries is answered; several are now questions I'm asking.

**D: This is a community**
- People talk to each other, not only to me, and I overhear things I wasn't meant to.
- Gimp coming up the hatch makes the Hollows feel like part of the world, and Boog's friendship feels older than my arrival.
- Familiar faces at dinner make service feel like the village's evening.
- Ogrin's light makes me curious, and nothing explains it.
- Full regression green, web build working, the save chain intact.

---

## Documentation during 4h

- `docs/PLAN_4H.md` (this file): approval record, then "As built" per checkpoint.
- `docs/PROGRESS.md`: each checkpoint's build, tests, deviations, open questions.
- `docs/GDD.md`: §3.1 and §3.4 (the time model as built), §6A.1–6A.2, §2.10 (the cast as built, proposals confirmed), §10.2 (scene structure), §10.6 (save), Decided entries for H1–H15 as approved.
- `CLAUDE.md`: locked rules that come out of approval (scene structure, the clock authority, Vigor's never-list, schedule rule, layout ownership, save version 10).
- `docs/ASSET_MAP.md`: every imported sheet and cell; the gaps and workarounds.
- `docs/THIRD_PARTY.md`: unchanged unless a Pixel Crushers support component is added.
- `docs/CREDITS.md`: Minifantasy packs newly used.
