# 4h plan: walkable Kariaston and the daytime life-sim slice

> **Status: approved 2026-10-06** (the owner's decisions on H1–H15 and the canon in §0.1, below). **Checkpoint A is approved (2026-10-07); Checkpoint B is built (2026-10-07) and waiting for the owner's playtest**; see *As built* near the end. C and D are not started. 4g is complete (signed off 2026-10-06, tag `milestone-4g`). Locked and not re-litigated here: a soft daytime clock, the 5 PM world cutoff, player-chosen Evening Prep, and Vigor as the daytime productivity cap (the owner's 4h brief, 2026-10-06).

**The question 4h answers:** *does living in Tally Ho! and Kariaston feel good enough that I want to spend time there even when nothing is pushing me toward an objective?*

**Experience targets, one per place** (the owner's shorthand, used throughout as the test for every choice):

| Place | Pressure | Says |
|---|---|---|
| Kariaston | **Choice**: what do I spend today's Vigor on? | *stay awhile* |
| Tally Ho! | **Performance**: can I run tonight's service well? | *this is home* |
| The Hollows | **Risk and time**: how far before Essence runs out? | *you cannot stay here* |

Lenses behind the plan: the *Lens of the Toy* (walking around the village should be pleasant with no goal), *Lens of Time* (the clock drives the world's texture, not the player's anxiety), *Character Web* (people tied to each other before they're tied to Bram) and *Meaningful Choices* (Vigor creates trade-offs; the clock doesn't).

---

## 0. Decisions (approved 2026-10-06)

| # | Decision | As approved | Cost to reverse |
|---|---|---|---|
| H1 | Scene structure | **Locked.** Kariaston is its own scene, loaded additively beside `Tavern.unity` in the daytime; doors are a short-fade passage, and stepping outside must feel essentially instant (§7). Reopened only if the Step 1 architecture proof finds a serious technical problem | **expensive** |
| H2 | Checkpoints | **Four:** A *I can live here*, B *a day's work*, C *the village has people*, D *this is a community* (§3) | low |
| H3 | Where Bram sleeps and wakes | **Upstairs in Tally Ho!**: the guest room is effectively Bram's room until the Inn's guests need it (Phase 5) | low |
| H4 | Farm representation | **Four fixed garden beds with stable ids** for the slice, and a deliberate Hearth & Hollows farming model unless later playtesting makes a compelling case for free-grid farming; built so that a migration stays possible (§11) | **expensive** |
| H5 | Crop maintenance | **Tending is optional and improves quality; ordinary crops never die from missed tending.** "Untended pauses growth" is a tuning option only (§11) | low |
| H6 | Seeds | **Free and unlimited** starter seeds in 4h; a seed economy is later production scope | low |
| — | Vigor and crop tuning | **Approved as test values, not balance:** 6 Vigor; prepare and plant a bed 2; tend 1; harvest 0; herbs 2 days, onions 3, barley 4. Tuned only after the Checkpoint B playtest (§10, §12) | low |
| H7 | Market keeper | ~~Grim runs the market stall~~ **Superseded 2026-10-07:** **Musashi** keeps the market (the owner's canon; GDD Decided 61); Grim's livelihood is open (§14, §20) | low |
| H8 | Figures | Maximo **blue Knight on foot (locked)**; Kaloren old wizard from NPC layers, the Lich held for a reveal; Grim **Miner** (candidate); Ogrin the better child of Snowball Wars / Summer Holidays, **provisional until seen at game scale**; Bart **Wise Orc** (candidate); Gimp **`soldier_headband` (locked)**; Glimmer **Naughty Fairy** (current treatment) (§28) | medium |
| H9 | Bart | **The first Visitor who stayed**: came through, remained, lives in the painted wagon on the green, sometimes performs at Tally Ho!. No large backstory in 4h (§21) | low |
| H10 | Gimp's visits | **First visit after Boog's bomb comes home, day 6 at the latest; after that irregular and nomadic** ("Gimp shows up when Gimp shows up"), seeded underneath, never two days running (§23) | low |
| H11 | Glimmer | **An unexplained light at Ogrin's window only.** No name, guardian story, fusion, questline or meeting in 4h (§24) | low |
| H12 | Villagers as patrons | **Yes, in Checkpoint D**, lightly: about 0–2 familiar faces a service (§25) | medium |
| H13 | Village layout ownership | **Generated once; the tilemaps are then hand-owned Unity content.** Tooling maintains named gameplay objects only and never regenerates over manual edits (§7) | **expensive** |
| H14 | Save | **One bump to version 10 in Checkpoint B**: the per-game world seed, the daytime clock, Vigor, the garden. NPC locations stay derived from day + time + story (§31) | medium |
| H15 | Assemblies | **Pure clock, Vigor, farming and schedule rules in `Shared`; village MonoBehaviours in a new `Hearthdelve.Village`.** Village and Tavern code never depend on Dungeon implementation details (§7) | medium |
| Q4 | Phi's portrait | **Approved:** a framed portrait of Phi'rai in Tally Ho!, made with the Portrait Generator's drow treatment (§22) | low |

Questions answered at approval: Grim's history (§20, locked), Kaloren and Ogrin (§19, locked), how Kariaston got its name (founded by Maximo, §0.1), Phi's portrait (approved), Gimp's kind (half-elf, locked).

### 0.1 Canon locked at approval (2026-10-06)

Recorded in the GDD (§2.1, §2.2, §2.5, §2.10, Decided 54–60) and CLAUDE.md. 4h reveals this through people, places and partial stories, never as a cosmology lecture.

**Kariaston exists because of the Hollows.** Long ago a source of evil opened beneath this region; an enormous force poured out of it into the surface world and caused a great war. **Karias**, a great wizard and once **Maximo**'s apprentice, gave his life to seal it; Maximo was among those who performed the sealing. The seal held, but not perfectly: small remnants of what lies beyond still seep through, and those remnants are what people now call **the Hollows**. Afterwards Maximo **founded Kariaston**, named it for Karias, and vowed to watch over the Hollows for as long as he lives. People gathered around that watch: glory-seekers, fortune-seekers, people who supply delvers, ordinary settlers and, in time, a few friendly monsters and stranger neighbours. All of them needed somewhere to drink, which is part of how Tally Ho! came to matter. Maximo will not go back into the Hollows, and probably **cannot**; the reason is a future story decision.

**Grim and Ogrin.** Grim is a dwarf and a **former delver**. On a delve he found a human-looking infant, alive, beside two dead adults he assumes were the parents, and brought him home to Kariaston. That child is Ogrin. Ogrin aged impossibly fast (to about ten in a year or two), then stopped abruptly; since then he has been chronically ill, with bouts of severe exhaustion. Grim doesn't know what Ogrin is, why he aged so, or what happened below; he loves him deeply. Ogrin calls him **Grim**. A future beat where Ogrin first calls him **Dad** is reserved and never spent in everyday dialogue.

**Kaloren and Ogrin.** Kaloren brings Ogrin herbs that make him feel better, **once every three days**; they ease the symptoms and cure nothing. Not connected (yet) to Kaloren's lichdom.

**Gimp.** A **half-elf** hunter and ranger: nomadic, abrasive, something of a nutjob, dislikes most people, hates cities, loves guns (rifles above all). The exceptions are **Boog** (dangerous explosives, mutual enthusiasm) and **Phi** (he knew and liked her: since he likes almost no one, that says something about her). He starts standoffish toward Bram and doesn't warm up because Bram is the protagonist. He lives in or around the Hollows but wanders: below for a while, in the forest or up a tree, gone for stretches, out of the area entirely, back to see Boog.

**Every major character has their own relationship with the Hollows**, and that is the centre of Kariaston's history: Maximo helped seal the evil and founded the village to guard it; Karias died sealing it; Phi went back into it after retiring; Grim went in and brought Ogrin out; Ogrin seems changed by something connected to it; Kaloren was transformed by it and left part of himself below; Gimp chooses to live around it; Bram keeps choosing to go down. The Hollows are the settlement's gravitational centre, not a dungeon attached to a farming town.

### 0.2 Still open (none blocks Checkpoint A)

| Question | Needed by | Note |
|---|---|---|
| **Firearms in the world.** Gimp loves rifles and his figure carries one; the GDD said "bombs and gunpowder, no firearms" | Checkpoint D (Gimp) | Recommendation: rifles exist as rare personal property of odd people like Gimp (seen, talked about), never a player weapon in this phase |
| **When the sealing happened, and Maximo's age.** Orik's built line ("my family went down for three hundred years") puts the Hollows at least three centuries old, and Tally Ho! has decades of history, so Maximo, a human, has watched for a very long time | Checkpoint C (Maximo's lines) | Leave unspecified in 4h dialogue ("long ago"); it's tied to the future decision about why he can't go back |
| **Glimmer and the sealing.** She was a guardian meant to help seal or protect against the Hollows and failed; Maximo's sealing held | before Glimmer's questline (not 4h) | Same event, an earlier attempt, or a different guard; also "the Warden Below" (GDD Open 12) |
| **Ogrin's figure** | Checkpoint C (Step 8) | seen at game scale |
| **Karias: elf or half-elf** | Checkpoint C (the memorial's inscription) | the docs say elf |

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

**Initial tuning (approved as test values for the B playtest, not balance; tuned only after it):**

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

- **Representation (H4, approved):** **four fixed garden beds**, each an object with a stable id (`garden_1`…`garden_4`), a 3×2-tile footprint and its own crop state. This is **a deliberate Hearth & Hollows farming model**, kept unless later playtesting gives a compelling reason to move to free-grid farming. More beds later are more ids.
- **Keeping a grid migration possible:** the rules never assume beds are few, fixed or rectangular. `GardenRules` works on a list of `PlotState`s keyed by a plot id string (a bed today; a cell id such as `garden@12,4` could be one later); a bed's footprint is data on the bed, not in the rules; the save stores plots by id, not by bed index; crops know nothing about the plot's shape; and the interaction finds "the plot under the keeper" through one query that a grid could answer too. What a grid would add later (tilling, per-tile placement) is new code, not a rewrite.
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
- **Conditions** (pure, small): `everyNthDay(n, offset)`, `chance(p)` (seeded by the world seed + day + character), `irregular(minGap, maxGap)` (a seeded sequence of gaps, so a visitor's days have no visible period), `storyFlag` (a Hearth & Hollows flag or opening stage), `questObject(id, status)`. No calendar by design. Combined with AND only.
- **Routines between characters** are ordinary blocks on both schedules. The first: **Kaloren's herbs** (§19), every third day (`everyNthDay(3)`, offset by the world seed): mid-morning he leaves the tower, walks to Grim and Ogrin's cottage, hands over the herbs (a short bark exchange), and goes on with his day. The player may happen to see it; nothing requires them to.
- **`ScheduleRules.Resolve(schedule, day, minute, world)`** → the block. Pure, EditMode-tested; the whole village's positions are a function of `day + minute + story`, so **no NPC transform is ever saved**.
- **Anchors:** named transforms in either scene (`square.memorial`, `tavern.bar.stool3`, `cottage.window`…). A block names an anchor; the scene that owns it hosts the character.
- **`SurfaceCast`** (one per loaded scene) spawns the characters whose current block is in its scene, and on each `SurfaceTimeChanged` re-resolves: if the new anchor is in the same scene and the character is visible, they **walk** there (grid A*, a few at a time); otherwise they **appear** there. Crossing scenes (Maximo going into Tally Ho! for lunch) is walk to the door, disappear, appear inside the door.
- **`VillagerAgent`**: thin MonoBehaviour, the existing `CharacterSpriteAnimator` and A* movement, a talk interactable bound to the character's `CharacterDefinition.conversation`, an optional bark bubble.
- **Discoverability:** important characters always have a findable daytime block; a hub conversation never depends on the minute unless the line is optional texture.

## 16. Initial cast per checkpoint

| Checkpoint | Who appears | How much |
|---|---|---|
| A | Boog, Orik (daytime posts, existing hubs); Phi's portrait | presence + talk only |
| B | (none new) | the garden is the test |
| C | Maximo, Kaloren, Grim, Ogrin, Bart | a definition, figure, portrait, schedule, a hub with a first meeting and an everyday branch, 2–4 callbacks, a few barks each; Kaloren's herb routine |
| D | Gimp; Glimmer's light; villagers as patrons; Bart's evening performance | the community layer |
| (throughout) | Old Phi, Karias, the Fortunate Five | the portrait, objects, the memorial, talk |

Smallest amount that makes Kariaston specific: five residents, one wandering friend from below, two absent people.

## 17. Character relationship web

The rule: everyone has opinions of someone other than Bram, and almost everyone has their own history with the Hollows (§0.1). *Locked* entries are canon; *proposed* ones are first readings for the owner to correct.

| Pair | What's there before Bram | Shown in 4h by |
|---|---|---|
| **Phi ↔ Orik** | *Locked:* old friends; he ran Tally Ho! in her absence, left when the Hollows got bad, she found him again | his hub; her chair; her portrait; the ledger in two hands |
| **Phi ↔ Maximo** | *Proposed:* the founder and the delver who came home; he counted her among the few who understood what the village is for; she once told him no, kindly, when he asked her to promise she'd stop going down | his talk; a toast he still makes |
| **Phi ↔ Boog** | *Built:* she let him keep his bomb; *proposed:* she's the only one who ever ate his experiments first | Boog's lines |
| **Phi ↔ Gimp** | *Locked:* he knew and liked her, which almost no one can say | one grudging line from Gimp |
| **Fortunate Five ↔ village** | *Proposed:* five tankards on Tally Ho!'s shelf that nobody drinks from; Bart knows half a song about them and refuses to sing the half he doesn't know | inspectable; Bart bark |
| **Maximo ↔ Karias** | *Locked:* master and apprentice; Karias died sealing the source; Maximo helped seal it, founded Kariaston in his name and keeps watch | the memorial; the founder's grief under the comedy |
| **Maximo ↔ the village** | *Locked:* founder and watchman; *proposed:* most villagers love him and humour him, few know what he actually did | barks; his proclamations |
| **Maximo ↔ Orik** | *Proposed:* the mayoral account, a tab Orik keeps "in a separate book, for my health" | barks; Orik's talk |
| **Maximo ↔ Boog** | *Proposed:* Maximo wants Boog's explosions for civic fireworks; Boog wants a civic budget for explosions | NPC barks |
| **Maximo ↔ Bart** | *Proposed:* Maximo thinks they're fellow artists; Bart thinks Maximo is a fan, and doesn't know he's singing to the man who saved the world | barks on the green |
| **Kaloren ↔ Kariaston** | *Proposed:* the helpful wizard everyone asks to warm their ovens, who never does it with fire | talk; barks |
| **Kaloren ↔ the Hollows** | *Locked, hidden:* became a lich there, came back, phylactery below | near-misses only (§19) |
| **Kaloren ↔ Ogrin and Grim** | *Locked:* the herbs every third day; *proposed:* Grim trusts him with Ogrin and never asks where the herbs come from | the routine (§15) |
| **Kaloren ↔ Maximo** | *Proposed:* two survivors of the Hollows, one who came back changed, one who can't go back; neither knows the other's half | they share a bench; one optional exchange |
| **Grim ↔ Ogrin** | *Locked:* the former delver who found him below and raised him | together at the stall on good days; the window on bad ones |
| **Grim ↔ the Hollows** | *Locked:* a former delver; *proposed:* he doesn't talk about it, and looks at the keeper's satchel the way old soldiers look at a uniform | his talk with the keeper |
| **Grim ↔ Orik** | *Proposed:* two dwarves who agree on nothing but weights and measures; Grim weighs, Orik counts | NPC barks at the stall |
| **Boog ↔ Gimp** | *Locked:* old friends; dangerous explosives | D's visit |
| **Gimp ↔ everyone else** | *Locked:* dislikes most people, hates cities | he avoids the square; short barks |
| **Bart ↔ Tally Ho!** | *Proposed:* plays there some evenings for supper; Orik pays him in stew and complaints | D's performance |
| **Ogrin ↔ Glimmer** | *Locked, future:* her questline; *4h:* "my light" at his window, unexplained | D's hint |
| **Ogrin ↔ Boog** | *Proposed:* Ogrin's favourite person to watch; Boog promises him "a small explosion, for your birthday" | barks |

Love/Hate: Maximo, Kaloren, Grim, Ogrin, Bart and Gimp get tracked `CharacterDefinition`s with values (craft, nerve, warmth) so the existing deeds reach them through the same rules. Proposed values: Maximo nerve 80, warmth 60, craft 0 (admires the deed, not the technique); Kaloren warmth 70, craft 60, nerve −20; Grim craft 70, warmth 30, nerve 20 (he knows what nerve costs); Ogrin nerve 70, warmth 60; Bart warmth 50, nerve 40 (a good story); Gimp nerve 90, craft 70, warmth −20, with a **low starting affinity** toward the keeper so he begins standoffish. Deed learners grow a `Villagers` group (by kind) so the troll's fall reaches the village. NPC-to-NPC feelings stay authored text, not simulated (no rumour networks, Decided 37).

## 18. Maximo, Karias and the founding

- **Who he is (locked, §0.1):** the founder of Kariaston and its watchman over the Hollows, who helped seal the evil that once nearly destroyed the world and lost his apprentice Karias doing it. He keeps a vow to watch for as long as he lives. He does not, and probably cannot, go back into the Hollows; **why he can't is a future story decision** and nothing in 4h hints at a specific reason.
- **How he behaves:** still the Don Quixote: theatrical, heroic, eccentric, romantic about adventure, funny. The contrast is now sharper: under the speeches is a man who really did save the world, and almost nobody in his own village quite believes how much.
- **Look (locked):** the **blue Knight on foot** from *Knight Jousting* (helmeted, always in his old armour). His age and face live in his portrait. The humour comes from him, not from the costume.
- **Routine:** a proclamation at Karias's memorial mid-morning (a bark to the square), lunch at Tally Ho! (someone always sitting in Tally Ho! at noon), afternoon "inspecting the watch": he walks the village's edge and looks toward the hatch from the doorway of Tally Ho!, never closer; home at five; on some evenings he stands at the memorial alone.
- **The keeper's delves:** his hub reads the keeper's delves (`HH_TimesDefeated`, the delve count). First delighted and grand ("a delver! keeping watch with me, from below!"); later, past a threshold, one quiet line about what the Hollows cost, about Karias without naming the sealing.
- **Karias:** present through the memorial (a short inscription), Maximo's stories and the village's name. 4h never explains the sealing's cosmology, the war, or what lies beyond the seal; a partial line or two (the memorial, Maximo on a good day) is the most it says. No twist, no survival.
- **First meeting:** a grand welcome speech for the new keeper of "the watch's own tavern", with one honest sentence in the middle.

## 19. Kaloren: presentation and foreshadowing

- **What the player knows:** a courteous, helpful wizard in the tower; slightly cold to the touch; gloves in summer; orders at Tally Ho!, admires the plate and doesn't eat (D); knows a little too much about how the cellars were built; brings Ogrin herbs.
- **What stays hidden:** that he's a lich, that the Hollows made him, the phylactery. Nothing in 4h confirms any of it, and **nothing ties his nature to Ogrin's condition**.
- **The herbs (locked, §0.1):** every third day he walks from his tower to Grim and Ogrin's cottage with herbs that ease Ogrin's symptoms and cure nothing, then goes on with his day (§15). The player may witness it; it happens whether or not they do. If asked, he's kind and vague about where the herbs grow; if asked whether they cure Ogrin, he's honest that they don't.
- **Near-misses (three or four, each once):** forgetting to breathe while listening; a remark about the Cellars' stonework "before the third collapse"; looking at the hatch too long (the opposite of Maximo, who won't come near it); declining to be warmed by the fire ("i'm fine. i'm always fine").
- **Look:** a Myriad NPC composition (old human, albino skin, long white hair and beard, the long hat, a robe colourway) in the layered NPC pipeline already used for patrons. The Lich figure is held for whenever the story wants a reveal.
- **Kindness first:** his hub is mostly him being useful and gentle; the strangeness is seasoning.

## 20. Grim and Ogrin

**Locked history (§0.1):** Grim, a dwarf and former delver, found Ogrin as an infant below, beside two dead adults, and raised him in Kariaston. Ogrin aged to about ten in a year or two, then stopped, and has been chronically ill and often exhausted since. Grim doesn't know what Ogrin is or what happened; he loves him. Ogrin calls him Grim; "Dad" is reserved for a later beat.

- **Grim** keeps the **market stall** as his livelihood (H7): gruff, fair, never haggles, charges everyone the same, proud of good onions. More than a worried guardian: a trade, a past (a delver who stopped), a rivalry with Orik, opinions about the keeper's knife and Bart's volume, and a dry warmth he hides. His past comes out slowly: he knows the Hollows and won't romanticise them; he looks at the keeper's satchel like a man remembering. He doesn't tell the finding story in 4h beyond, at most, one guarded line once trusted ("i found him. down there. that's all i know, and it's more than i'd like.").
- **Ogrin** is his own person first:
  - bright, curious, opinionated; draws **maps of the Hollows** from what he overhears and asks the keeper to correct them; collects the keeper's stories ("what did you fight?", from the delve facts and curios brought home);
  - loves Bart's songs and watching Boog's explosions from a safe distance; hates onion broth and being called brave or fragile;
  - *agency:* he wants to see the hatch; he names the keeper's monsters ("that's not a slime, that's a Gerald"); he trades his maps for stories;
  - *the mystery, lightly:* he doesn't remember being a baby "because i was only one for a bit"; he finds the hatch fascinating, not frightening. Nothing explains it.
- **Good and bad days:** seeded by the world seed (about two good days in three; tunable). Good days: at the stall with Grim or on the green. Bad days: in bed, talking to the keeper **through his window**. The days after Kaloren's herbs lean good. The illness and exhaustion are shown, never named or explained.
- **Dialogue rule:** no 4h line has Ogrin call Grim "dad" or "father", and no line resolves what he is. (A validation test guards the first.)
- **Looks:** Grim the **Miner** figure (current candidate; distinct from Orik's yellow beard and the dwarf keeper). Ogrin a child figure from *Snowball Wars* or *Summer Holidays* (8 px tall against the keeper's 10), **provisional until both are seen at game scale** in Step 8.

## 21. Bart

- **Proposal (H9): the first Visitor who stayed.** He came through Kariaston years ago, played one night at Tally Ho! and never left; he lives in a painted wagon on the green. That makes him the living version of the path residents will take in Phase 5 (Visitor → resident), without building that path.
- **Where:** the green in the late morning (playing; music notes, passers-by barks), the market at midday (gossip with Grim), Tally Ho! some evenings in D (a performance spot by the hearth: ambience, no gameplay effect in 4h).
- **How he differs:** Maximo makes speeches about heroes; Bart sings about ordinary people and gets the facts slightly wrong on purpose. Boog is enthusiasm, Orik is precision, Bart is the village's memory and gossip.
- **Hooks:** he hears about the keeper's deeds (through the existing deed memories) and they turn up in his songs (callbacks, "the ballad of the troll, verse two"); later, a commissioned song or a rumour that points a delve. No new systems.
- **Look:** the Wise Orc (an old orc with presence; reads as an orc at 8 px). The True Heroes II Bard has playing animations but is a human; an orc-skin recolour from Minifantasy's ramps is the fallback if you prefer the instrument (H8).

## 22. Old Phi and Orik

- Orik's history stays as built (the three lines heard if you ask). In 4h it gains **places**: her chair by the hearth (inspectable; he dusts it, "don't tell Boog"), the ledger written in two hands, the five tankards, and **her portrait** (approved).
- **Phi's portrait (Checkpoint A):** a framed Portrait Generator drow (elf ears, obsidian skin, consistent with her established look) hung in Tally Ho!'s main room, inspectable: "Phi'rai. Old Phi, to anyone who wanted to keep their teeth." It makes her present in the room and explains nothing about her disappearance. The portrait itself is made in the Portrait Generator (§40); the frame is a Towns prop.
- Orik's daytime hub gets one or two lines tied to the new world: the village asking after her (Maximo, Bart), and the first time the keeper sleeps upstairs ("that was her room once. it's a room. sleep in it.").
- Gimp's single grudging line about her (D) is the strongest hint 4h gives that she was more than a landlady.
- Not in 4h: why she went back down, the Fortunate Five's history, any letter beyond the one built.

## 23. Gimp: the visitor proof

**The experience:** the Hollows have neighbours, Boog had a life before you, and not everyone is glad you're here.

**Locked canon (§0.1):** a half-elf hunter and ranger; nomadic, abrasive, something of a nutjob; dislikes most people and hates cities; loves rifles; likes Boog (explosives) and liked Phi; starts standoffish toward Bram.

- **Visit rule (H10):** the first visit is the day after Boog's bomb comes home, or day 6, whichever is first. After that, an `irregular` seeded rule: gaps of roughly 1–5 days, never two days running, with an occasional longer absence ("he's gone off somewhere"); the player's impression is that Gimp shows up when Gimp shows up. Derived from `day + world seed + story`; nothing saved but Dialogue System variables.
- **The visit:** in the afternoon the cellar hatch in Tally Ho! opens and Gimp climbs out (the hatch prop already exists from the opening). Boog drops whatever he's doing; the two sit at the bar arguing blast radii. He leaves back down the hatch before Prep (or at about 16:30), without saying goodbye to anyone but Boog.
- **The first meeting (seeded once, then yours):** Boog is overjoyed and introduces his oldest friend from below; Gimp looks at the keeper as an unwanted complication ("this is the new one? smaller than i pictured. no. the same size. i just pictured them gone."), asks Boog why the keeper is still standing there, and turns back to the fuse. The keeper's choices can't win him over today.
- **After:** `Gimp/Hub` grows slowly: an everyday branch that is mostly Gimp talking to Boog while the keeper is tolerated; once-only callbacks to deeds he respects (the troll, through his values: nerve and craft); one line about Phi, gruff and unexpectedly fond. His affinity starts low; warming up is earned through deeds he values, never automatic.
- **Where he isn't:** he never walks into the square (cities, people); if he leaves by the front door at all, it's straight for the trees.
- **Architecture proof:** Gimp is a `CharacterDefinition` with a new kind value `Hollower` (never shown to the player as a category) and a schedule whose blocks are irregular visits. That is all of *encounter → recurring visitor* the architecture needs; *guest* and *resident* stay Phase 5.
- **Look (locked):** `soldier_headband` from *Modern Soldiers* (All Exclusives › Creatures): idle, walk, damage, death; its rifle-firing animations are not used in 4h (see the firearms question, §0.2). **Portrait:** not a human recipe by default: the closest Portrait Generator treatment for a half-elf (elf ears with a human face and tanned skin, or the elf base with a rougher colourway), chosen in Step 12.

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

Catalog first (`C:\Dev\Minifantasy\List`), sheets inspected for this plan; exact cells are recorded in `docs/ASSET_MAP.md` as each is imported. The figures as approved are on `docs/plan_4h/cast_candidates.png` (true scale, with the keeper and Orik for reference; regenerated at approval).

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
| Maximo | **blue Knight on foot** (*Knight Jousting Add-on 1.5*, `Knight On Foot`: idle, walk, attack, damage, die), **locked** | human, old, white beard |
| Kaloren | *A Myriad Of NPCs* layers (human albino, long white hair and beard, long hat, robe) | human, albino skin, robe, white beard |
| Grim | *Miner* (current candidate) | dwarf, gruff |
| Ogrin | *Snowball Wars* or *Summer Holidays* child figure, **provisional** until both are seen at game scale | gap: no child parts; the smallest human or halfling face with "innocent" eyes as an approximation |
| Bart | *Wise Orc* (current candidate; fallback *True Heroes II* Bard with an orc skin ramp) | orc |
| Gimp | **`soldier_headband`** (*Modern Soldiers*, All Exclusives › Creatures: idle, walk, damage, die), **locked** | half-elf: the closest Portrait Generator treatment (elf ears, tanned human face), chosen in Step 12 |
| Glimmer | *Naughty Fairy* (fly, appear, disappear; desaturated), the current treatment | none: she's a light |
| Old Phi | none (absent) | **a drow** (elf, obsidian skin) for her framed portrait in Tally Ho! (approved) |
| Merchant stall extras (D) | Visitors from the existing *Myriad* pools | none |

## 29. Real art gaps

1. **The keeper's farming animations.** *Farming Animations* (plough, seed, water) and *Writing Down* and *Sleeping* exist only for the bare race bodies, not for the four clothed keeper bodies. **Workaround:** the keeper faces the bed and plays an existing pose; the Farm pack's animated action icons show the action. Good enough for a small garden; worth revisiting only if farming grows.
2. **A child portrait for Ogrin.** The Portrait Generator has no child parts. **Workaround:** the closest small human or halfling face; or Ogrin speaks without a portrait (a framed silhouette), which suits a boy you mostly see through a window. Your call in Step 8.
3. **An orc playing an instrument.** No orc figure has a playing animation. **Workaround:** the Wise Orc with music-note emotes and an instrument prop beside him (*Musical Instrument Icons*), or the human Bard recoloured.
4. **Ogrin in bed.** *Sleeping Animations* are adult base bodies. **Workaround:** the window (we never see inside the cottage in 4h).
5. **Maximo's face.** The Knight's helmet hides his face and age at 8 px; his portrait carries both, and his voice does the rest. No gap to fill.
6. **Gimp's rifle.** `soldier_headband`'s attack animations fire a rifle; whether a rifle exists in the world (carried, mentioned, never a player weapon) is the firearms question (§0.2).

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
- **Story data:** every new character complete (id, kind, name in the table, portrait, conversation, values); faction database matches; hubs follow the priority order; once-only flags use `~= true`; new English fits its boxes and passes the style and glyph checks; Tamsin and other old names absent. No 4h line has Ogrin call Grim "dad" or "father" (the reserved beat).
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

1. **Portraits** (Steps 3, 8, 12): Phi's drow portrait for the frame (Step 3); Maximo, Kaloren, Grim, Ogrin (or decide none), Bart and Gimp (half-elf) in the Portrait Generator app if the recipe route (`Tools/portraits`) can't express them; I'll give exact recipes and steps.
2. **Ogrin's figure:** pick between the two children once both are in the village (Step 8).
3. **Dialogue:** edit the seeded first drafts in the node editor as you like (hubs, first meetings, barks, inspectables).
4. **Kariaston's tiles:** after the first generation (Step 2), hand-paint and arrange as you wish; the updater won't touch tiles.
5. **Playtests** at each checkpoint's end (the acceptance criteria below), including a controller pass for the garden's haptics.
6. The open questions in §0.2 before the checkpoint that needs each.

## 41. Implementation order

**Checkpoint A: I can live here**
1. *Surface architecture:* `GameFlow` content sets; `Kariaston.unity` created once with a blockout; `SurfaceDoor` passages; area camera bounds and lighting switch; `Hearthdelve.Village` assembly. Prove loading, doors and lights on web early.
2. *Kariaston in real art:* the layout above with Towns/Farm/Plains art, Tally Ho!'s exterior, the square, the four households (unoccupied), the garden's beds (inert), plots, edges. ASSET_MAP records.
3. *Tally Ho! as home:* Daytime walking; waking upstairs; Phi's framed portrait; storeroom shelves; stations for the delve meal; Decorate key and book; the menu board → Prep; Boog and Orik daytime posts; inspectables; the market stall (no keeper yet) with its panel; `MorningScreen` retired.
4. *The clock:* `SurfaceClock`, pause reasons, bands, the 5 PM wind-down, daylight curve, HUD clock, market hours, `HH_Time`. Tests; web build; **stop for the A playtest.**

**Checkpoint B: A day's work**
5. *Vigor:* rules, config, HUD pips, facts, tests.
6. *The garden:* `CropDefinition` (herbs, onion, barley), beds, plant/tend/harvest, overnight growth, storeroom delivery, feedback (MMF + named haptics), balance row.
7. *Save version 10* and migrations; daytime autosaves; Continue mid-day. Tests; web; **stop for the B playtest.**

**Checkpoint C: The village has people**
8. *The cast as data:* `CharacterDefinition`s (Maximo, Kaloren, Grim, Ogrin, Bart, plus Karias and Phi as untracked story ids), figures (Ogrin's chosen at game scale), portraits, faction regeneration, values, the `Villagers` learner group.
9. *Schedules and presence:* `ScheduleDefinition`, `ScheduleRules`, anchors, `SurfaceCast`, `VillagerAgent`; Boog and Orik moved onto schedules; Grim at the stall; Ogrin's good days and window; Kaloren's herb routine.
10. *First conversations:* hubs seeded once (first meeting, everyday, a few callbacks each), memorial and other inspectables, `HH_Here`, `HH_Band`. Tests; web; **stop for the C playtest.**

**Checkpoint D: This is a community**
11. *The web between them:* NPC-to-NPC barks (`AmbientBarks`, pairs), deed callbacks for villagers, Maximo's delve-count lines, Orik's new lines.
12. *Gimp:* `Hollower` kind, the irregular visit rule, the hatch, the bar with Boog, `Gimp/Hub` (standoffish first meeting), his half-elf portrait.
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
- Each character has a distinct voice; Maximo is funny and, underneath, the man who kept watch; Kaloren kind and slightly wrong; Grim gruff and fond, more than Ogrin's guardian; Ogrin his own person, not his illness; Bart unlike Maximo.
- I've seen (or could have seen) Kaloren take Ogrin his herbs without it being about me.
- The Hollows feel like the reason the village exists, learned from people and places, not from a lecture.
- None of the mysteries is answered; several are now questions I'm asking.

**D: This is a community**
- People talk to each other, not only to me, and I overhear things I wasn't meant to.
- Gimp coming up the hatch makes the Hollows feel like part of the world, Boog's friendship feels older than my arrival, and Gimp's coldness toward me feels like his, not a bug; his visits feel irregular, not scheduled.
- Familiar faces at dinner make service feel like the village's evening.
- Ogrin's light makes me curious, and nothing explains it.
- Full regression green, web build working, the save chain intact.

---

## As built: Checkpoint A, "I can live here" (2026-10-07, built; waiting for the owner's playtest)

Steps 1–4. Nothing of B, C or D is built: no Vigor, crops, save version 10, world seed, mid-day saving, resident villagers, schedules, Gimp, Glimmer, named patrons, fishing, ranching, seasons or weather. `milestone-4h` is not tagged.

### Scenes and the way between them

- **`Kariaston.unity`** is its own scene, loaded additively beside `Tavern.unity` in the free daytime (H1). `GameFlow` now loads a **content set**: the daytime (after the opening's arrival) is {Tavern, Kariaston}, a delve is {Dungeon}, everything else is {Tavern}; one cover and reveal per set. `GameFlow.LoadedScenes` and `IsLoaded(scene)` replace the single loaded scene (`LoadedScene` stays: the first of the set).
- **World placement:** Kariaston's origin is (200, 0), 72×48 tiles, so its tilemaps, colliders and grid never overlap the tavern's.
- **The front door** (`SurfaceDoor`, Tavern assembly): two doors that find each other by id (`tallyho.front.inside` at the tavern's door, `tallyho.front.outside` on Tally Ho!'s step in the village). Walking into one fades (0.18 s), moves the keeper to its partner's arrival point, enters that place's `SurfaceArea` and publishes the same passage events as the stairs. Only in the free daytime, never while decorating or loading. Doors are instant: no scene loads.
- **`SurfaceArea`** (Tavern assembly): one per place (`tavern`, `guest_room`, `kariaston`), saying whether it's indoors, how the camera behaves there (*Hold* on a point for the rooms; *Follow* the keeper clamped to bounds in the village) and which 2D lights belong to it. Entering one switches lights **off before on**, because URP's 2D renderer allows only one enabled global light per sorting layer: Kariaston's daylight is authored **disabled** and lit only when the keeper is outside.
- **`SurfaceCamera`** (order −200) places the tavern's virtual camera transform directly each frame (*Hold* or *Follow* through `ViewBounds.Clamp`). It doesn't use Cinemachine's own follow, so the tavern's camera and the village's share one vcam with no blend; the smooth-scrolling rules (no damping, LateUpdate brain, the presentation anchor) still hold.
- **Layout ownership (H13):** `KariastonBuilder` (*Hearthdelve → Generate → Kariaston*) painted the blockout once; tiles and dressing now belong to the scene. The idempotent updater (`SurfaceBuilder.UpdateSurfaceBatch`, also run with the tavern updater) rebuilds only `Kariaston/Gameplay`: the daylight, the area, the door, the market stall, the memorial's look and the edges. `-rebuildScene` regenerates the whole scene, only with the owner's go-ahead.
- **Assemblies (H15):** pure rules in `Shared/Surface` (`SurfaceClock`, `MarketHours`, `SurfacePause`); `SurfaceTime` in `Shared/Game` (Boot); the new `Hearthdelve.Village` holds the village's own behaviours (`MarketStall`, `SurfaceDaylight`) and references Core, Shared and Tavern, never Dungeon or Pixel Crushers (`StoryTests` checks). `SurfaceArea`, `SurfaceDoor` and `SurfaceCamera` live in Tavern because both scenes use them and the keeper is Tavern's.

### Kariaston

(A capture of the whole map: `KariastonBuilder.CaptureBatch` writes `BatchLogs/kariaston.png`.)

72×48 tiles of Minifantasy plains and paths. **Tally Ho!** stands at the top centre (its door at (36, 30.9) in village tiles) with a path straight down to **the square** (14×12 of flagstones): the Karias memorial, the well, lamps, benches and **the market cart**. Around the square: **Maximo's house** (the large blue-roofed hall, west), **Bart's painted wagon** beside it, **Grim and Ogrin's cottage** (the dark timber house, east) and **Kaloren's tower** (far east). **The garden**, four empty dirt beds behind a fence with a scarecrow, is on Tally Ho!'s own ground to the west; **three empty plots** are fenced (two north-east, one south-east). Trees and flowers close every edge; there's no road out yet.

This flips the plan's sketch north–south (Tally Ho! at the top, the square below it): the door opens straight onto the path to the square, which read better on screen.

**Traversal** at the keeper's 6 tiles/s, along the paths: the door to the square under 2 s, to the market about 3 s, to the garden about 4 s, to Maximo's house or Kaloren's tower about 7 s. Nothing is near the plan's 12–15 s ceiling; the playtest decides whether the village wants more room (the layout is the scene's now).

### The surface clock

- `SurfaceClock` (pure, `Shared/Surface`) holds the minute of the day; `SurfaceTime` (Boot) advances it with **`Time.unscaledDeltaTime`, capped at 0.1 s a frame**, never the wall clock (the Web build can't jump it). Tunables are on `Data/Config/SurfaceClockConfig.asset`: day 08:00, cutoff 17:00, afternoon from 12:00, **3.89 real seconds per game minute (9 hours ≈ 35 real minutes)**, a 10-minute display step, market 08:00–17:00, and `pauseIndoors` (off; the playtest's comparison switch).
- **It stands still** (each a named reason, `SurfaceTime.Instance.Still`): outside the free daytime or the opening's arrival, at the cutoff, while loading or covered by a transition, while talking, under a full-screen menu (`MenuPause`), while held (`SurfacePause`: Decorate Mode, the storeroom and meal panel, the market, the Prep question), and when the window loses focus. Deviation from §8: the unscaled time and explicit reasons instead of scaled time, so a frozen `timeScale` (dialogue sets it to 0) and a held panel are told apart in tests and in the HUD.
- **Bands:** morning, afternoon, evening (17:00). `SurfaceTimeChanged` (each display step) and `SurfaceBandStarted` go on the `EventBus`.
- **Not saved in A:** the clock is `GameState.Surface`, outside the save. Every new day (new game, Continue, Sleep) starts at 08:00; quitting mid-day and continuing restarts the day at 08:00 (mid-day saving is B's, with version 10).
- **The face:** a quiet "8:40 am" tab in the top corner in the daytime only, moving in 10-minute steps, with "Tab: decorate" under it while indoors.
- **Daylight:** `SurfaceDaylight` colours Kariaston's global light over the day (soft morning, plain noon, gold toward five, dusk held after).

### 5 PM

The clock stops at 17:00; the market cart closes (its closed look; using it says "the market's packed up till morning"); Kariaston's light holds at dusk. If the keeper is in Tally Ho!, Orik says it once (`Orik/Five`: "that's five. the village is putting its boots by the door." / "we open when you say so. i'll be here, counting."); outdoors, the cue waits until they come in. **Nothing forces Prep**: the keeper can walk, decorate, check the shelves, cook the meal, and begin the evening whenever they choose.

### Where each of the Morning panel's functions went

| Morning panel | Checkpoint A |
|---|---|
| Storeroom | **The storeroom shelves** (a cupboard of jars in the kitchen corner): the panel opens on the stock; Escape, B or *back* closes it |
| Delve meal | **The Grill and the Tap** in the daytime: each opens the meal list for its own station; cooking is the same minigame |
| Market | **The market cart** in Kariaston's square, 08:00–17:00 |
| Decorate | **The Decorate key** (Tab / View) anywhere inside Tally Ho! in the daytime and at night, shown under the clock |
| Open for the evening | **The menu board** by the door: "begin evening prep? the rest of the day goes by." → *begin prep* / *not yet* |

The panel itself is kept as the storeroom-and-meal view (`MorningScreen`, modes *Storeroom* and *Meal*); it no longer opens on its own.

### Waking and the house

- The keeper wakes **upstairs**, in front of the bed in the guest room (H3; `TavernDirector.WakeUpstairs`), and walks down the stairs.
- **Inspectables:** Phi's portrait upstairs (the Portrait Generator's elf, framed: `Tools/portraits/phi.json`, `framed.py`) and the Karias memorial in the square, each a one-line conversation in the node editor (`Inspect/PhiPortrait`, `Inspect/Memorial`), spoken by an unnamed **narration** voice (the box hides the speaker for `Inspect/` titles).

### Deviations from the plan

- **The portrait hangs upstairs**, not by the bar: the bar's wall holds the trophy spot and the tavern's wall decor. Its line still lands where the keeper wakes.
- **No plans book**: the Decorate key with its reminder under the clock does the job; a book on the bar would have been a second way to the same key.
- **The tankards and the hatch** are seeded as conversations (`Inspect/Tankards`, `Inspect/Hatch`) but not placed: the shelf and the hatch need their own art and spots, better decided with the room's decor in C/D.
- **The market is a cart** (*Towns Props*' open and closed cart) standing in for Grim's stall: Grim isn't in A, so the stall is just the market.
- **The camera** moves the vcam transform rather than using Cinemachine's follow (above).
- **Inspectables in the plan's list** (Phi's chair, the trophy, Orik's incident book) aren't built; two are, to test the idea.
- **The bed as a second way to sleep** isn't built (the Night panel's button stays the only way).
- **The early Web smoke** after Step 1 wasn't run on its own: the Web build was smoke-tested once, at the end, covering both lists.

### Tests

- EditMode: `SurfaceClockTests` (33: the clock's advance, cap, cutoff, bands, face, market hours, pause holds, the scene-free rule that nothing reads `Time.time` for the hour).
- PlayMode: `SurfaceCheckpointATests` (9: waking upstairs; the stairs; the front door, its lights and camera; the clock's pauses; the Decorate key indoors only; the market before and after five; five o'clock with no forced Prep and the board's confirmation; Prep before five; the storeroom and the delve meal).
- **Web smoke (2026-10-07):** from a save temporarily set to the daytime (the owner's save backed up first and restored after): woke upstairs at 8:00 am with Phi's portrait on the wall; down the stairs; the Grill offered the delve meal; the menu board asked "begin evening prep?" and *not yet* backed out; out of the front door into daylit Kariaston with the camera following; read the memorial; opened the market at the cart. It found one bug, fixed before handover: a nameless line (the look) showed `#tavern.plain` as its speaker; the clock test now checks the box for missing strings, and the rebuilt Web build shows Phi's portrait line with no name. The page was a hidden tab, so the clock correctly stood still as unfocused; its running is covered by the PlayMode tests.
- The day-loop, Checkpoint C/D, text-overlap and capture tests now walk the day the player's way (`DaytimeActions`: the menu board and yes, the stall, the shelves) instead of the retired buttons; the tavern's recorded starting room adds the door, the board and the shelves.


### After the owner's Checkpoint A playtest (2026-10-07)

The owner's verdict: everything else works well. Five changes:

- **The clock runs 3× faster:** 1.30 real seconds per game minute, so 8:00 to 17:00 takes about 12 real minutes (was 35). The asset was migrated once (only if it still held the old default); `SurfaceClockSettings.Default` and `SurfaceClockTests` follow.
- **The clock's face was cramped:** its digits ran into the parchment tab's red rule (the panel's visible frame is about 4 px deep with rounded corners, and the tab was 16 px around a 12-px line). The tab is now 64×24, and the decorate reminder sits 8 px lower.
- **A pond:** Forgotten Plains' own lake tiles (two frames, animated, solid), stepped 12×4 in the meadow between Maximo's house and the garden path, a tree on its north-west bank and the bench to its east. Added to the hand-owned scene once by `KariastonBuilder.AddPondBatch` (it touches nothing else, and does nothing if a pond is there); a freshly generated village paints it too.
- **The storeroom shelves are furniture now** (they stood in the way, by the stairs' corridor): a unique working piece (`storeroom_shelves`, *Stations* in the catalog, a new `FurnitureFunction.Storeroom`) that Decorate Mode moves like the stations, starting below the barrels against the east wall (26, 7). A save from before gets them once, where the starting room has them or on the nearest free tile (`FunctionalGrants`; a unique piece can't be sold or lost, so "owns none" means "never had them": no save-version bump). The fixed fixture, its reserved tile and its layout fixture are gone.
- **Back up the stairs after a trip outside:** not reproduced in automated play (walking through both doors on foot, then along the corridor and up, passes, with the shelves in either place), so the likeliest cause was fixed: the stairs' trigger covered only the right two-thirds of the one-tile gap beside the stew pot, so a keeper walking up along the stew pot slid past it. It now covers the whole foot tile (`GuestRoomBuilder`), and `AfterKariaston_TheKeeperWalksBackUpTheStairs` walks it. If it still happens, it needs the exact route.

## As built: Checkpoint B, "A day's work" (2026-10-07, built; waiting for the owner's playtest)

Steps 5–7. Nothing of C or D: no villagers, schedules, cast, Gimp or Glimmer; the world seed exists and nothing reads it yet. `milestone-4h` is not tagged.

### Vigor

- **Pure** (`Shared/Surface/Vigor`): `Max`, `Spent`, `Current`, `CanAfford(cost)`, `Spend(cost)` (all or nothing), `Refill()`, `Restore(spent)`. Only `Spent` is state (saved); the maximum and every cost are tuning, so a retune never strands a save.
- **Tuning** (`Data/Config/VigorConfig.asset`, `VigorSettings`): **6 a day; prepare and plant a bed 2; tend a bed 1; harvest 0** (the approved B test values). Costs are named by `VigorActivity` (PlantBed, TendBed, HarvestBed); a later activity adds an entry and a cost, nothing else.
- **Refill:** only in `DayRules.Sleep`. Nothing else restores it in 4h.
- **Never spent by** walking, the stairs, the doors, talking, looking, shopping, the storeroom, the delve meal, decorating, the menu board and Prep, service or the delve (tests walk each at 0). At 0 nothing ends, advances, forces Prep, weakens service or touches Essence: an empty bed says "too tired to dig a bed today" and a growing one just says when it'll be ready.
- **HUD:** six small pips right of the clock tab (warm when full, dark when spent), only in the free daytime; spending pops the spent pips (a 0.22 s squash), with a soft tick and a light tap (`UiMoment.Vigor`: `PH_UiTick` + Tap.Light at 0.3, through the named-haptics path; a no-op on the Web).

### The garden

- **Four fixed beds** on Tally Ho!'s grounds (Checkpoint A's soil), stable ids **`garden_1`** (top left) … **`garden_4`** (bottom right), listed in `Data/Garden/GardenConfig.asset`. State is `GardenState` (beds by id) of `BedState`: `Crop` (id, empty when bare), `PlantedDay`, `Grown` (nights grown), `TendedDays`, `LastTendedDay`, `LastGrownDay`. A bed knows nothing of its shape; the scene's `GardenBed` (Hearthdelve.Village, built by the Kariaston updater) shows it.
- **Rules** (`GardenRules`, pure): plant (empty bed, Vigor 2) → tend (growing, once per bed per day, Vigor 1; the planting day counts) → grow (one step per new day) → harvest (ready, free) → empty. Every refusal changes nothing. **Forgiving:** an untended crop still grows, never dies or rots; a ready crop waits indefinitely. `GardenSettings.untendedPausesGrowth` (off) is the stricter rule for the playtest to try.
- **Growth** runs inside `DayRules.Sleep`, keyed to the new day: a bed grows only if `LastGrownDay < day`, so reloads, scene loads and repeated calls never double-grow, and `Sleep` itself only runs from the Night.
- **The night, in order:** storeroom freshness; the new day; the garden's growth (once); Vigor full; then (`GameFlow`) the surface clock's morning.
- **Interaction:** an empty bed opens the planting choice (`GardenPanel`: the three crops with their seedlings and days, then "not now"; up and down wrap; Escape or B backs out; the clock is held); a growing bed offers "tend the bed" until tended today, then "onions: ready in 2 days" (a new `TavernHintKind.Growing`); a ready bed offers "harvest". The keeper turns to the bed (`PlayerLook.Hold`) while Farm's animated icon plays over it (seeding, watering, pulling); tended today, the bed's soil is darker; each bed shows one plant per tile at the crop's stage (seeds, sprouting, growing, ready).

### Crops (`Data/Garden/Crop_*.asset`, `CropDefinition`)

| Crop | Produces | Days | Yield | Art | Fine with |
|---|---|---|---|---|---|
| herbs | herbs | 2 | 3 | More Veggies: Spinach | 1 tended day |
| onions | onion | 3 | 3 | More Veggies: Onion | 2 tended days |
| barley | malt | 4 | 2 | Farm: Wheat | 2 tended days |

**Tending → quality:** tended on at least half its growing days (`fineTendedShare` 0.5, rounded up) → **Fine**, otherwise **Standard** (the existing `Quality` enum; no new tiers).

### Into the storeroom

A harvest is an ordinary `IngredientStack` (the crop's ingredient, Fine or Standard, fresh) added to the storeroom with the existing merge rules: no farm inventory. It's cookable at once (a test cooks the delve meal at the Grill from homegrown herbs). A one-line note says what went in ("3 fine herbs into the storeroom"), with a small chime and pulse (`UiMoment.Harvest`).

**Provenance (deviation, as the brief allowed):** not built. An ingredient's identity (definition, quality, prep) is what stacks merge on; an origin tag would have to join it and would ripple through equality, saves, the satchel, the storeroom, recipe matching and staff, and split homegrown Standard herbs from market ones. `CropHarvested` doesn't need it; `ServedHomegrown` waits for a decision (§ open questions).

**Economy:** unchanged prices. `BalanceReport` has a garden row (its best week, every bed harvested back to back, at market prices: **72 gold**, 4 beds of herbs); `SurfaceCheckpointBTests` checks that a garden-only week (a market night every night plus that) never beats a delve week, and that the garden's best week is under a quarter of one.

### Save version 10

```
world   { seed }                                     // one per game, made once
surface { minute, vigorSpent }                       // the day in progress
garden  { initialized, beds: [ { id, crop, plantedDay, grown, tendedDays, lastTendedDay, lastGrownDay } ] }
```

- **v9 → v10:** the world seed from a stable hash of the save's own text (FNV-1a; the same old file always gives the same seed until it's re-saved as v10), the surface minute at 8:00, Vigor full, and the starter beds empty (`GardenState.Ensure` on restore: once, and any bed added to the tuning later arrives empty the same way). Nothing that existed changes. v7 and v8 go through the existing chain, then this step.
- **New games:** a fresh seed, the four beds, Vigor full.
- **Mid-day Continue:** the daytime resumes at its saved minute with its Vigor and garden; the keeper wakes in Tally Ho! (upstairs, as every daytime starts); no exact position is saved.
- **Autosaves added:** after planting, tending, harvesting, and on *begin prep* (the day as it was left, saved just before the evening begins; the evening itself isn't a resume point). Market purchases already saved. Nothing saves on walking or the clock's ticking.
- **Never saved:** derived looks (stages, tints), NPC positions.

### Checkpoint A's carry-overs

- **The decorate reminder** now sits in the bottom-left corner (the room never reaches it); the clock tab moved up 2 px, and the Vigor pips sit beside it in the top margin.
- **The market cart's** solid body reaches 4.6 tiles up behind it (was 2.2), so nobody stands where the canopy (6¼ tiles of art) hides them whole.

### Deviations

- No provenance (above).
- The tend action has no keeper body animation (the approved workaround: facing plus the action icon).
- The planting choice is a small panel on the tavern canvas (like the menu board's question), not a world-space menu.

### Tests

- EditMode `SurfaceCheckpointBTests` (30): tuning; Vigor's spend, exact cost, refusal, floor, refill, sleep and that the rest of the day's rules never touch it; planting, refusal at low Vigor, occupied and unknown beds, daytime only; tending once a day; maturity for herbs, onions and barley untended; growth once per day and across a save; ripe crops waiting; the stricter switch; the Fine threshold per crop; harvest (free, fresh, into the storeroom, bed emptied); v10 round trip and stability; unknown crops; v9, v8 and v7 → v10; the migrated seed's stability; no re-grant; a new bed in the tuning; the garden-week balance.
- PlayMode `SurfaceCheckpointBTests` (6): pips and planting and tending through the bed and the panel (selection, navigation, cancel, feedback fact, visuals); everything at 0 Vigor (walking, the clock, the doors, the stairs, talking, decorating, the market, Prep, the delve with Essence untouched, no Vigor beside Essence); growth across days, the harvest into the storeroom and the delve meal cooked from it; mid-day save, quit and Continue; the autosaves; the cart and the reminder.
- Two older story tests asserted the save "stays version 9"; they now assert the current version with the same meaning. `TextOverlapTests` now also checks the planting choice and the HUD.
- **Results:** EditMode 693/693; PlayMode 235 passed, 0 failed, 25 skipped (the explicit captures and the stairs diagnosis).

### Web smoke (2026-10-07)

On the owner's own browser save (a genuine version 9 save, day 2, daytime; backed up to IndexedDB `hh_backup_4hb` first and restored after): it migrated to version 10 on Continue (seed made, morning, Vigor full, four empty beds; the storeroom shelves arrived in the owner's rearranged tavern by themselves); the HUD at 320×180 (the clock clear of its frame, six pips beside it, the decorate reminder bottom left); down the stairs, out of the door, along the path to the garden; "plant a crop" → the choice (keyboard: down to onions, Enter; the same UI navigation a controller uses) → two pips spent, the seeding icon, seeds in the bed; "tend the bed" → one pip, the watering icon, darker soil; a full page reload and Continue → 8:10 am, three pips, the bed as left (the IDBFS file read back as version 10 with its seed, minute, Vigor and the bed); then three nights (F8 and the Night panel's sleep): the save showed the onions growing once a night (grown 1, 2, 3 with `lastGrownDay` 3, 4, 5), Vigor full and 8:00 each morning. It found one bug, fixed before handover: the planting choice's first button covered its title (the panel is now taller, and the overlap test checks it).

Not done in the browser: walking back to harvest (the tab was hidden, and the game throttled to a crawl), so the harvest into the storeroom and cooking from it are covered by PlayMode only; a real gamepad (keyboard navigation drives the same UI path); haptics (the Web build has none: the named patterns no-op). Hidden-tab time can't distort growth (growth is per new day, never per second) and the clock stands still while unfocused. The fresh build after the panel fix was not smoke-tested again.


### After the owner's Checkpoint B playtest (2026-10-07)

The garden works; three changes:

- **Doorways without an exact line.** Every doorway (the stairs, the guest room's door, both halves of the front door) now goes through when the keeper is in it and pushes its way: by the stick or keys or by moving, and even when a wall or the stairs' flight stops them (`Doorway`, `AreaPassage`/`SurfaceDoor` `through`). Standing in one does nothing, and someone arriving walks away from the door they came out of, so nothing bounces back. Their triggers are wider (the stairs' foot reaches into the row below). `Doorways_GoThroughWhenPushed_FromAnywhereInThem_AndNeverBounceBack` walks each at its edge.
- **A day of about 4 minutes:** 0.43 real seconds per game minute (35 minutes, then 12, now about 4), while the day has little to fill it; to lengthen as activities arrive. The asset migrates from either earlier default.
- **Musashi** (the owner's new canon; GDD §2.10, Decided 61): the elf who keeps the market stands at the cart's front-left corner (A Myriad of NPCs' elf, black ponytail, white shirt), talkable in the daytime (`Villager`, the `Person` interaction), his first-draft hub `Musashi/Hub` seeded once (the first meeting: who he is, the Fortunate Five, the taste the Hollows took; then four things to ask, Toshi named only), his portrait from `Tools/portraits/musashi.json`, a tracked Love/Hate character (values craft 80, nerve 30, warmth 60). The cart is still where you buy; he doesn't move yet (Checkpoint C's schedules). His quest for Toshi is later.

### More from the owner's playtests (2026-10-07)

- **The stew pot** starts left of the range (17, 12); at (24, 11) it blocked the stairs. A layout made before keeps its pieces, except any on a newly reserved tile, which moves once to its starting spot or the nearest free one (`LayoutRepair`).
- **The stairs** are entered as drawn: from the flight's left, where its bottom step is (pushing right or up), with the arrival beside it at (24.9, 12.3) and the approach tiles reserved.
- **The keeper's room:** the upstairs room is the keeper's (player-facing "your room"), with **the hatch to the Hollows in its floor** at (14, 4) (its tiles reserved): the way down on arrival day, a look ("the way down, in the floor of your room...") on any free daytime after. Six tutorial and everyday lines say where it is now (a one-off author's edit, each still as written). A house of their own comes later (direction).
- **Door signs never under the HUD:** the HUD's top-left block (the Essence icon and bar, the run's gold) registers its screen area (`ScreenReservation`, `Core.Presentation.ScreenReservations`), and a door's reward sign that would be covered steps straight down just clear of it, back home when uncovered.
- **Bats in the open:** an encounter puts a bat on a ground spawn (flying, awake-looking) instead of a wall perch 35% of the time (`FloorTuning.batInOpenChance`), and always when no perch is left.

---

## Documentation during 4h

- `docs/PLAN_4H.md` (this file): approval record, then "As built" per checkpoint.
- `docs/PROGRESS.md`: each checkpoint's build, tests, deviations, open questions.
- `docs/GDD.md`: §3.1 and §3.4 (the time model as built), §6A.1–6A.2, §2.10 (the cast as built, proposals confirmed), §10.2 (scene structure), §10.6 (save), Decided entries for H1–H15 as approved.
- `CLAUDE.md`: locked rules that come out of approval (scene structure, the clock authority, Vigor's never-list, schedule rule, layout ownership, save version 10).
- `docs/ASSET_MAP.md`: every imported sheet and cell; the gaps and workarounds.
- `docs/THIRD_PARTY.md`: unchanged unless a Pixel Crushers support component is added.
- `docs/CREDITS.md`: Minifantasy packs newly used.
