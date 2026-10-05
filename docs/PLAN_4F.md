# 4f plan: Tavern Stage 1 content and the customization foundation

_Proposed and **approved 2026-10-05**, with the decisions in §22 locked (D2 and D20 changed by you, D22 chosen). Work proceeds in four checkpoints (§20); `docs/PROGRESS.md` records progress._

**The experience 4f targets:** *"This is my tavern. I chose how it looks, I earned the strange things inside it, and the room itself tells the story of what I've done"* (GDD §6.6). Around that sits a richer evening: a menu that mixes everyday food from the market with strange food from the Hollows, a butcher's block that turns a good monster part into several portions, a cook and a server who feel like people, and patrons who sometimes want something particular.

**What's essential to that experience:** pieces that look good together and can be arranged freely, enough of them that two players' taverns look different, things found in the Hollows standing in the room afterwards, and a layout that still works for service. **What isn't:** simulating carpentry, per-tile wall building, numerical decor bonuses.

Sections 1–21 are the report the plan was asked for. Section 22 lists the decisions I need from you before building, each with a recommendation.

---

## 1. Current tavern architecture 4f can reuse

What exists (4c–4e), and how 4f uses it:

| Piece | Today | In 4f |
|---|---|---|
| `TavernBuilder` (editor) | Builds the one 28×17-tile room in `Tavern.unity`: the premade Tavern Indoor shell stretched cell by cell, then furniture placed by code at fixed positions (`k_Bar`, `Tables[]`, barrels…), each with a `BoxCollider2D` footprint ending at the bottom of its art | The shell stays built by the editor; **the furniture moves out of the scene into data** (a starting layout asset). The builder's footprint rules and station setup move into furniture definitions |
| `TavernLayout` | Scene references: door, queue spots, `TavernSeat[]`, staff posts (Serving, Grill, Tap, StewPot, rest) | Becomes **derived from the placed furniture** each time the layout changes: seats from chairs facing tables, posts from wherever the stations stand, queue spots found from the door |
| `TavernSeat` | A chair: sit point, approach point, facing, the furniture it belongs to | Kept; created by the chair's furniture definition (its seat anchors) instead of the builder |
| `TavernInteractable` | Stations, the pass, seats, the door: use offsets, reach, highlight | Kept; configured from the definition's function (station kind, use offsets) |
| `NavGrid` | Baked from obstacle colliders; `Invalidate`, `Rebuild`, `Version`; `NavigationLayoutChanged` event; path followers re-plan | Used as is: placing, moving or removing furniture publishes `NavigationLayoutChanged`. **4c built this for 4f**, so nothing about pathing assumes a fixed room |
| `GridPathfinder`, `GridMap` (pure) | A* on a grid, EditMode-tested | Also drives **layout validation** (reachability) as pure logic on a `GridMap` built from footprints, without physics |
| `TavernSeat` + `Upgrade_TavernSeats` | Seats bought as an upgrade (120 and 220 Gold) switch on hidden tables | **Conflicts with customization**: seats should come from placing tables and chairs. Decision D16 |
| Service (`ServiceSession`, `CustomerLogic`, `StaffAgent`, `KeeperWork`) | Customers path to seats; staff work posts; the keeper uses interactables | Unchanged in principle: it reads seats and posts from the layout. The pathing was built to tolerate layout changes |
| `Storeroom`, `RecipeMatcher`, `PrepRules` | Ingredient stacks, recipe slots by ingredient or category, prep steps per station | The recipe rework adds data, not structure. `PrepRules.Steps` already anticipates more stages (the Butcher Block) |
| `SaveSystem` (v3) | Versioned JSON with migrations; unknown ids dropped with a warning | v4 adds furniture ownership and layouts per area (§21) |
| `RunLoot`, `RewardKind`, `RoomRunner.GrantReward`, `DelveReport` | Run Gold and bosses defeated travel home; documented extension point for "furnishing discoveries" | Discoveries join exactly there (§14) |
| `BossDefinition.trophyId`, `BossFirstCleared` event, `GameState.BossClears` | The 4e hook for the trophy | The Larder Troll's trophy plugs in (§16) |
| `SpeechBubble`, UI Overhaul emotes, `MMF_Player` feedback, named haptics, `UiFeedback` | Order bubbles, emotes, combined feedback | Decorate Mode, discoveries and the Butcher Block get feedback passes in the same style |
| Daytime placeholder (`MorningScreen`) and `NightScreen` | Panels in the tavern scene | Decorate Mode, the market and the catalogue open from them until 4h replaces the daytime |

Smaller findings that shape the plan:

- **Two of the seven ingredients can't be found.** Shroom Cap and Spore Sac lost their monster in 4b, so Shroom Skewer, Cellar Kebab, Cellar Stew and Offal Pottage can't be cooked from a delve (Known issues). The recipe rework fixes this (§18).
- **The Bat Wing is in no recipe** except as "any meat" in the Cellar Kebab.
- **Pip has a stand-in look** (the premade Butcher), accepted in 4c until 4f. There is no dwarf body in A Myriad Of NPCs (dwarves are stocky humans with beards), and none is needed for Gunta in the same way.
- **A flaky tavern test** (`Pip_OnServing_CarriesPlatesToWhoeverOrderedThem`) was deferred "to the tavern work (4f)". It gets fixed in step 1.

## 2. Customization data architecture

Names below are the ones Step 1 builds; later steps may add fields.

**`FurnitureDefinition`** (ScriptableObject, one per catalog piece; generated from a source table, §9):

- `id` (stable, saved), localized name and description key, `category` (Seating, Tables, Bar and storage, Lighting, Wall decor, Floor decor, Plants, Curios, Bedroom, Stations), theme tags (tavern, dwarven, elven, castle, haunted, Cellars…).
- **Geometry, in the piece's own footprint space** (tiles, origin at the footprint's bottom-left corner, as the piece stands unrotated): the **footprint** (whole tiles: what snaps and what may not overlap another blocking piece), the **bodies** (pixel-exact collider rectangles: what physically blocks and what the walkable grid bakes, as in 4e), **seat anchors** (sit point, approach point, facing), **use points** (where people stand to use a station; the first is the staff post), **surface anchors** (where Surface items sit), **light points**, and the **art layers** (sprite or animation frames, offset, sorting order, sorting layer). One pure transform (`FurnitureGeometry`) rotates and flips all of it together, EditMode-tested.
- **Rotation mode (D2):** `None` (never turns: most wall-bound pieces and art whose baked light or perspective would look wrong), `AuthoredFacings` (each quarter turn that Minifantasy drew is its own entry with its own art and geometry; turns that weren't drawn are skipped), or `QuarterTurnSprite` (one drawing, really rotated to 0°, 90°, 180° or 270°; footprint, bodies, anchors, use points, surface anchors and lights rotate with it). Where Minifantasy drew a better facing, the piece uses `AuthoredFacings`. Orientation is saved as quarter turns counter-clockwise: 0 = facing the camera (south), 1 = east, 2 = north, 3 = west.
- `layer`: **Floor** (rugs: never block, drawn under everything), **Standing** (blocks its footprint, Y-sorted), **Wall** (hangs on the back wall band, never blocks), **Surface** (small things on a table or shelf; see D4).
- `flippable` (D3: opt-in per piece, off by default; flipping mirrors the geometry before rotation), `light` (optional `Light2D` settings: candles, braziers, fireplaces light the room).
- `function`: None, **Seat** (one or more seat anchors with facing), **Table** (a surface seats face), **Station** (Grill, Tap, StewPot, ButcherBlock, with use offsets), **Pass**, **Storeroom** (later), and `wallBound` (a station that must stand against the back wall, like the kitchen range).
- **Economy and ownership:** `price` (0 = not for sale), `sellBack` share, `rarity`, `unique` (at most one owned), `catalogTier` (the Renown unlock, §13), `source` flags (Bought, Hollows discovery, Boss, Story, Starter).
- **Recolouring:** `paletteGroup` (which channels it has: wood, cloth, metal, accent…) and authored `variants` (§11).

**`PlacedFurniture`** (plain data, saved): `definitionId`, `cell` (the footprint's bottom-left tile in the area), `turns` (0–3), `flipped`, `nudge` (a pixel offset of up to half a tile, −4…+3 px per axis: quarter-tile steps for non-blocking decor under D1, and the exact 4e positions in the starting layout; blocking pieces placed in Decorate Mode snap to whole tiles with no nudge), `palette` (a palette id per channel, or a variant id), `host` (the piece a Surface item sits on), and an instance id.

**`PropertyArea`** (scene component plus `AreaDefinition` data): an area id (`tavern`, `guest_room_1`), its floor rectangle, its back-wall band, its doors and **reserved tiles** (the entrance and the tile inside it, stairs), its `NavGrid`, and its finishes (floor and wall style, if D5 is approved). The tavern and the guest room are two areas; Bram's quarters and later rooms are more of the same.

**`FurnitureLayout`** (pure logic): the placed pieces of one area and the rules: `CanPlace(def, facing, cell)` with a reason ("overlaps the bar", "blocks the door", "only against the back wall"), `Place`, `Move`, `Remove`, a footprint occupancy grid, and a `GridMap` for validation. No Unity physics: EditMode-tested.

**`FurnitureInventory`** (pure, in `GameState`): owned counts per definition, unlocked definitions, discovered (for the catalogue's "found" marks), and what's in storage = owned − placed across all areas.

**`LayoutView`** (MonoBehaviour, thin): spawns a `FurnitureView` per placed piece (sprite, sort point, footprint collider on the obstacle layer, light, interactable), rebuilds derived seats and posts, and publishes `NavigationLayoutChanged`. One spawner for every area.

**Assemblies:** definitions and pure layout logic in `Hearthdelve.Shared` (Customization), because the Hollows drop furniture and the tavern places it, and Dungeon and Tavern never reference each other. Views in `Hearthdelve.Tavern`. Decorate Mode UI in `Hearthdelve.UI`.

## 3. Placement UX recommendation

**Recommended: a cursor-based Decorate Mode, controller first, with the keeper present but not carrying.**

- **Why not carrying furniture by hand?** It's the more immersive option, and I'd normally prefer it, but rearranging twenty pieces by walking to each, lifting and walking back is clearly slower and more repetitive. Animal Crossing started with only the hands-on version and later added a cursor mode for exactly that reason. **Flagged tradeoff:** I recommend the cursor. Immersion stays in the presentation: pieces lift with a little hop and shadow, settle with a thud and dust, the room stays lit and alive, and Pip or Gunta might glance over.
- **The room fits one screen** (decided in 4c), so the cursor never needs a camera pan in the tavern. The guest room is smaller still.
- **Controls** (all rebindable through the UI map):
  - Gamepad: stick or d-pad moves the tile cursor (with key-repeat); **A** picks up or places; **B** cancels (the piece goes back where it was); **X** rotates (to the next drawn facing, or a real quarter turn); **Y** flips (where allowed); **LB/RB** cycle what's under the cursor (rug, table, candle on it); **Start** opens the catalogue and storage; **Select** shows the layout check.
  - Keyboard: WASD or arrows, E place, Q/R rotate, F flip, Delete to storage, Tab catalogue.
  - Mouse: the cursor follows the pointer's tile; click picks and places; the wheel rotates; right-click cancels.
- **The ghost:** the carried piece drawn over the room at its placement, with its footprint tiles tinted (green fits, red doesn't) and the reason in a line of text. Invalid placement is never silent.
- **Undo** (a stack for the session) and **"put it all back"** (discard every change since entering). Both are cheap because the layout is data.
- **Exit** runs the layout check (§6) and saves.

**When:** in the daytime and at night, never during service (the daytime placeholder and the Night screen get a "decorate" button; 4h moves it into the walkable day). **Snapping:** decision D1.

## 4. Ownership and storage recommendation

- **Owned copies, not unlimited-after-unlock** (D7). Buying a chair gives one chair. Placing it takes it from storage, removing it puts it back. Why: furnishing a room is then a real Gold decision, duplicate discoveries stay meaningful (a second rare lantern is a second lantern), and the room shows what you've earned. Unlimited copies would make Gold stop mattering for customization after one purchase of each.
- **Unique pieces** (boss trophies, some discoveries) are capped at one.
- **Storage** is property-wide (one shed for every area), unlimited, shown in Decorate Mode beside the catalogue with counts. Removing a piece never destroys it (D12).
- **Selling back:** bought pieces at half price; discoveries and uniques can't be sold (they're memories, and selling them would make them a Gold source to farm).
- **The starting tavern** (today's layout) is owned from a new game, at no cost, as Starter pieces.

## 5. Functional-furniture approach

Functional pieces are ordinary furniture definitions with a `function` (§2), so moving them is the same act as moving a candle.

| Piece | Recommendation (D6) |
|---|---|
| Tables and chairs | Fully movable. A chair is a **seat** when it faces a table on an adjacent tile; its approach tile is the free tile beside it. Benches carry two or three seat anchors |
| The pass | Movable. Usable from either long side, as now |
| The Stew Pot | Movable anywhere (it's a cauldron over a floor fire) |
| The bar with its taps (the Tap) | Movable as one piece (the premade L-bar has its stools drawn in, so they move with it) |
| The kitchen range (the Grill) | **Wall-bound:** slides along the back wall only, because the oven and chimney are drawn against a wall |
| The Butcher Block | Movable (new in 4f) |
| The entrance | Fixed (part of the shell) |

- **One of each station** in 4f. The data allows more later (a second Grill as an upgrade), but staffing and the prep screen assume one per kind today.
- **Stations can go to storage.** The layout check then refuses to open the doors ("there's no Grill out"). That's clearer than forbidding the action.
- Staff posts and use points move with their station, so Pip and Gunta keep working wherever things stand.

## 6. Service-validation approach

Validation is pure logic over the layout's occupancy grid plus `GridPathfinder`, run live in Decorate Mode (a status line: "all good", or "2 problems") and on leaving, and again before the doors open. It checks **what service needs**, never resemblance to the authored layout.

**Placement-time rules** (the ghost turns red, with the reason):

1. Inside the area's floor; Wall pieces only on the back-wall band; wall-bound stations against the back wall.
2. No overlapping blocking footprints. Floor pieces may sit under anything; Wall pieces may overlap each other only if D2 allows.
3. Never on reserved tiles (the entrance, the tile inside it, the stairs).

**Layout check** (problems named in plain words, and selecting one moves the cursor to the culprit):

| Check | Severity |
|---|---|
| The entrance can reach the room ("the entrance is blocked") | **Blocks opening** |
| Each station is out and has a reachable use point ("the Grill can't be reached") | **Blocks opening** |
| The pass is out and reachable from the stations and the seats | **Blocks opening** |
| At least one seat is reachable | **Blocks opening** |
| Every seat's approach is reachable from the entrance ("2 seats can't be reached") | Warning: those seats aren't used |
| Staff can reach their posts ("Pip can't reach the pass") | Warning: that staff member idles |
| Enough queue space by the entrance | Warning: newcomers wait outside |

**Recommendation (D13):** block only what makes service impossible; warn about the rest, and let unusable pieces simply go unused. Arbitrary rules such as minimum aisle widths aren't needed: the reachability checks catch what matters, and the keeper's own walk shows the rest.

**Queue spots** become dynamic: the free tiles nearest the entrance, in walking order, skipping seat approaches and station use points.

## 7. Nav and pathfinding integration

- Every placement change publishes `NavigationLayoutChanged`. `NavGrid` invalidates and rebakes on next use, and followers re-plan (all from 4c).
- **Footprints are whole tiles** (D1), so the baked grid matches the validation grid exactly, and the art can overhang its footprint (as the kitchen's does today).
- Validation doesn't need physics: it builds its `GridMap` from footprints directly, so it's EditMode-testable. A PlayMode test checks that both agree on real layouts.
- Decorate Mode never runs during service, so nobody re-plans mid-stride. A layout change still re-plans safely (the 4c tests cover it).
- Each area has its own `NavGrid`; NPCs stay in the tavern area in 4f.

## 8. Guest-room proof

**Purpose:** prove the tavern and the Inn use one decorating system. No guests, occupancy, prices, Visitors, residents or ratings.

- **One small room** (about 12×9 tiles, one screen) with its own shell, to prove an area isn't tied to the Tavern Indoor art. Candidates: a Towns II indoor shell (plank, brick or stucco) or the Shop Indoor shell (cream or green walls). Recommended: the Towns II **wooden plank** shell, which suits an inn's upstairs.
- **Where:** in the `Tavern` scene, beside the main room, reached by **stairs** on the tavern's back wall (a reserved, fixed fixture). Walking onto the stairs fades to the guest room, where the camera holds on it (the dungeon's room-fade pattern), and back. In Decorate Mode, an area switch (LB/RB on the area tabs, or Tab) jumps between areas without walking.
- **Same everything:** the same definitions, inventory and storage, the same `FurnitureLayout` rules, recolouring and save path. Its starting layout is a bed, a chest and a rug (Starter), so it isn't an empty box.
- **Its own validation profile:** none of the service checks apply. A guest room only needs its doorway reachable. Validation profiles are per area kind, which the Inn will need anyway.
- **Bedroom pieces** join the catalogue for it (beds, nightstands, wardrobes, dressers, a washstand, a mirror, a bathtub), and can be placed in the tavern too. Nothing stops a bed in the dining room; the patrons may have opinions in 4g.

## 9. Minifantasy furniture and decor catalog findings

I searched the catalog (393 "Buildings and props" families, plus the prop sheets filed under "Other assets" and "Crafting and resources") and opened every interior-relevant sheet. Full notes, paths and sizes are in `docs/ASSET_MAP.md` ("Furniture and decor survey").

**What exists, by use:**

| Use | Where | Notes |
|---|---|---|
| Tavern seating and tables | Tavern Indoor, Dwarven Kingdom (six materials), Elven Kingdom (five colourways), Castles (banquet set, two colourways), Haunted House (wingback armchairs, sofas), Towns II (sofas), Church (pews), Towns (cloth-covered round tables) | Chairs exist per facing, so seating rotates properly |
| Bar, shelves, storage | Tavern Indoor (bar, shelves, bottles, glasses), Shop Indoor (counters, cupboards, potion rows), Towns (bookcases with many fills, cupboards), Medieval City (wardrobes and dressers in three colours), Chests (eight colourways), Dwarven (barrels, kegs, a great tun), Wizard Tower (bookcases) | Plenty |
| Lighting | Shop Indoor (candles in eight colours), Church (candle racks, standing candles with light layers), Castles (braziers, candlestick), Dwarven (lanterns in three colours, torches), Lunar New Year (paper lanterns), Medieval City (stone fireplace, animated, with a light layer), Dungeon (animated candle and torch) | Lights double as gameplay-free mood: a placed brazier adds a `Light2D` |
| Wall decor | Castles (banners, paintings, crossed weapons, shields, **mounted antler heads**), Haunted House (**six haunted portraits**, cobwebs, mirrors), Towns (framed pictures, wall weapons, shop signs incl. a tankard sign), Dwarven (banners, rune panels), Elven (banners, bunting) | Strong; this is where "the room tells my story" lives |
| Floor decor | Towns II (rugs in five colours), Castles (red rugs), Elven (rugs in five colourways), Dwarven (rugs in three colours), Haunted House (patterned carpets) | Rugs are cheap to place and transform a room |
| Plants | Plant Pots (**eight pot colours × ~16 plants**), Shop Indoor pots, Medieval City flower boxes | Huge variety on its own |
| Curios and monster decor | Observatory (globe, orreries, star charts, telescopes), Wizard Tower (book stacks, scrolls, a gold orrery), Piles of Loot (gold hoards, **meat-and-bone piles**, bones), Dungeon (statues in three stones, tombstones), Giant Bones (great horns or tusks), Lunar New Year (lucky cats), Haunted House (pumpkins) | The natural pool for discoveries from the Hollows |
| Bedroom | Barracks (beds in two frames × three blankets, two facings), Medieval City (beds, wardrobes, dressers), Towns (beds, dressers), Towns II (mirror, bathtub, washstand) | Enough for the guest room |
| Stations | Crafting And Professions II (the kitchen, in use), Dungeon cauldron, **C&P II preparation table** (with a working animation: the Butcher Block) | |
| Room shells | Tavern Indoor (two floors), Shop Indoor (two wall and two floor colours), Towns II indoor tilesets (brick, stucco, plank) | Enough for floor and wall finishes (D5) |

**Two findings that shape the design:**

1. **Minifantasy draws each facing separately and doesn't draw every facing for every piece.** Pieces with drawn facings use them; others can still turn through real quarter-turn rotation where the art reads well turned (D2, as locked).
2. **Minifantasy already ships many authored colourways** (pots ×8, chests ×8, candles ×8, Dwarven tables ×6, Elven sets ×5, Towns II rugs ×5, Castles ×2, beds ×3). A large share of "recolouring" can be curated variants of real art, which keeps the look coherent. A true palette swap is still worth proving where no variants exist (§11).

**Gaps:** no mounted troll head or troll-sized trophy (§16 composes one); little tabletop clutter sized for the tables.

## 10. Recommended initial furniture catalog

**Target: about 70 distinct pieces, about 170 placeable entries counting colourways and facings**, chosen so a cosy wood tavern, a dwarven stone hall, a castle banquet room, an elven green room and a spooky haunted den are all achievable from the first catalogue. Everything goes through the data pipeline: a source table in the repo (`Assets/_Project/Data/Furniture/catalog.csv` or JSON; one row per piece: id, sheet, sprite rect per facing, footprint, layer, category, price, tier, variants, channels), and an editor generator (*Hearthdelve → Generate → Furniture Catalog*) that slices the sheets, builds the `FurnitureDefinition` assets and a contact-sheet PNG for review. Adding the hundredth chair is a row, not code.

To make measuring 170 rects bearable: an **island finder** in the generator proposes rectangles (connected non-transparent pixels, merged within a small gap) and numbers them on a contact sheet, and the table refers to island numbers or explicit rects.

| Category | First catalogue (pieces; entries) | Sources |
|---|---|---|
| Seating | Tavern chairs (4 facings), stools ×3, benches (4 facings), Dwarven stools (6 materials), Castle chairs (2 colourways × facings), wingback armchair, sofa ×2, pew (≈10; ≈36) | Tavern Indoor, Dwarven, Castles, Haunted, Towns II, Church |
| Tables | Round A/B, small round, square, long (2 facings), Dwarven tables (6 materials), Elven round (5), banquet table, cloth-covered round (≈9; ≈22) | Tavern Indoor, Dwarven, Elven, Castles, Towns |
| Bar and storage | Bottle shelves, low shelf, tall shelf, bookcase (4 fills), cupboard, wardrobe (3 colours), dresser (3), chest (8 colourways), barrel, keg, great tun, crates (≈12; ≈30) | Tavern Indoor, Towns, Medieval City, Chests, Dwarven |
| Lighting | Candle (8 colours), candle rack, standing candle, brazier (2), lantern (3), paper lantern, stone fireplace, wall torch (≈8; ≈19) | Shop Indoor, Church, Castles, Dwarven, Lunar, Medieval City |
| Wall decor | Banner (Castle ×2, Elven ×5, Dwarven), painting ×4, haunted portrait ×6, crossed weapons, shield, mounted antlers, tankard sign, cobweb ×2 (≈10; ≈25) | Castles, Elven, Dwarven, Towns, Haunted |
| Floor decor | Rug (Towns II ×5, Elven ×5, Dwarven ×3, Castle ×2), haunted carpet (≈5; ≈16) | Towns II, Elven, Dwarven, Castles, Haunted |
| Plants | 6 plants × pot colours (offered as one piece with a pot-colour choice) (≈6; ≈12 shown, 48 possible) | Plant Pots |
| Curios | Globe, orrery, star chart, book stack, lucky cat (4), statue (3 stones), pumpkin (≈7; ≈12) | Observatory, Wizard Tower, Lunar, Dungeon, Haunted |
| Bedroom | Bed (2 frames × 3 blankets), nightstand, washstand, mirror, bathtub (≈5; ≈10) | Barracks, Medieval City, Towns II |
| Stations | Kitchen range, stew pot, bar with taps, pass, butcher block (5; 5) | as today, plus C&P II |

The **Cellars discovery pool** (§14) and the **troll trophy** (§16) come on top. Prices in a first pass: chairs and stools 10–20 Gold, tables 30–60, rugs 25–50, lights 15–60, wall decor 20–80, large pieces (great tun, fireplace, banquet table) 120–200. Tunable on each definition; a good evening earns roughly 60–120 Gold today, so a chair is a small treat and a fireplace a goal.

The catalogue is split into **tiers** (§13): the starting tier is wood-tavern basics plus rugs, plants and candles. Dwarven, Elven, Castle and Haunted collections unlock with Renown.

## 11. Recolouring prototype recommendation

**Recommended (D11): a palette-channel system that remaps an authored sprite's own colours, with the result baked into a cached texture, so every piece keeps URP's `Sprite-Lit-Default` material** (a locked rule in CLAUDE.md, Lighting).

- **Authoring:** for a piece's sprite, an editor tool lists its colours. Each colour ramp is assigned to a **channel** (wood, cloth, metal, accent), stored as a small `PaletteMap` asset.
- **Palettes:** curated `PaletteDefinition`s per channel: ramps of three or four colours **taken from Minifantasy's own colourways** (the Dwarven materials, the Elven sets, the Towns II rugs), so recolours look like Minifantasy art. Presets (named schemes such as "dwarven slate" or "elven blue") set several channels at once.
- **Runtime:** `(sprite, palette per channel)` → a remapped copy of the sprite's pixels in a `Texture2D`, cached and shared by every piece with the same combination, made into a sprite with the same pivot at 8 PPU, point filtered. Pure logic does the colour mapping (EditMode-testable); a thin cache owns the textures.
- **Why not a shader:** a palette shader would need a custom lit sprite material, against the locked material rule and the lighting baseline. Baking keeps lighting, sorting and batching unchanged. **Why not tint:** full-sprite tinting is what GDD §6.6 rules out.
- **Authored variants stay first-class:** where Minifantasy already drew colourways, the piece offers them as **variants** through the same palette UI ("blue", "pink"). The player sees one recolouring tool; behind it some options are palette maps and some are drawn variants.
- **The prototype group:** the Tavern Indoor round tables, chairs, benches and stools (channels **wood** and **cushion**), and the Plant Pots (**pot** colour as variants). That covers both techniques on the most-placed pieces.
- **The UI:** in Decorate Mode, with a piece selected, **Y (hold) / C** opens the colour panel: channel tabs, swatches, presets, **copy colours** from the last piece edited and **apply to all of this kind in the area**. That's the first cut of GDD §6.6's "copy, apply-to-set, presets".

## 12. Purchasing and economy approach

- **The catalogue** opens in Decorate Mode (Start / Tab) and from the daytime panel. Pages by category; each entry shows the piece, its price, how many you own and how many are placed; locked tiers show their Renown requirement; discoveries show as "found in the Hollows". Presentation: the **Animated UI Book** (exclusive pack) as the catalogue's frame is a candidate, to be judged when the UI is built.
- **Buying** costs Gold at once and puts the piece in storage, or straight onto the cursor if bought from Decorate Mode, so buying and placing is one flow. A combined purchase haptic and sound cue fires.
- **Delivery:** immediate. Next-day delivery would add anticipation but also friction; I'd revisit it with the village shops in 4h.
- **Selling back** at half price (bought pieces only).
- **Balance intent (GDD §7.3):** a good delve still funds roughly one meaningful upgrade, and furnishings become the steady Gold sink between upgrades. First prices in §10, tuned in playtesting; all on the definitions.

## 13. Renown's first gameplay use

**Recommended (D14): Renown unlocks catalogue tiers.** As word spreads about the Sunken Flagon, better craftsmen take Bram's orders:

| Renown | Unlocks (proposal, tunable) |
|---|---|
| 0 | The tavern basics: Tavern Indoor pieces, rugs, plants, candles, bedroom basics |
| 25 | The Dwarven collection (stone and metal tables, kegs, lanterns, rugs) |
| 60 | The Elven and Castle collections |
| 100 | The Haunted collection and the great pieces (great tun, fireplace, banquet table) |

**Why this rather than a customer tier:** it's visible progress the player can see and touch, it feeds the pillar 4f is about, and it stays separate from Gold (Renown is earned, never spent; GDD §7.1). GDD §7.1 also names customer tiers as Renown's role, and those still come, but **4h reworks the customer population** (named villagers plus Visitors), so a customer-tier system built now would be rebuilt in a few weeks. The tier unlock shows on the Night screen ("word is spreading: the dwarven joiner will take your orders") with a feedback moment.

Thresholds are tunable. Today's numbers (about +2 Renown for a delighted customer) put the first unlock around the third or fourth good evening.

## 14. Furnishing discovery (curio) options

**The experience:** "Oh! It dropped something new for my tavern." Discoveries never use Satchel slots (locked, GDD §13 Decided 15).

| Option | How it plays | For | Against |
|---|---|---|---|
| **A. Curio channel** (recommended) | Enemies rarely drop a glinting curio; walk over it to pick it up; a curio counter appears beside the run Gold; extracting brings it home and owns it; dying loses it | Keeps extraction tension for something the player wants; no ingredient space stolen; mirrors run Gold, which players already understand | Losing a pretty thing on death can sting (that's the point, in moderation) |
| B. Permanent on pickup | Picking it up owns it at once | Simplest; no loss | No reason to care about getting home; less exciting |
| C. A curio slot in the Lockbox choice | Death lets you keep one satchel stack *or* one curio | More choice at death | Muddles a clean, locked rule |
| D. Curios survive death "damaged" | Restore later for Gold | Softens loss | Adds a repair system for little gain |

**Recommended: A**, plus **two sources**:

1. **Rare enemy drops** from a small Cellars pool (about 4% per kill to start, at most two per delve; tunable in `RunTuning`).
2. **A "curio" room reward** (`RewardKind.Curio`), shown on the door sign like the others. Choosing a room for a curio over Gold or ingredients is a real decision, and it reuses the 4d reward system as designed. Its cache sits in the cleared room like the Gold.

**Feedback pass:** a distinct drop glint and colour (not Gold, not an ingredient), a pickup jingle, flash and a named haptic (a light "discovery" pattern), a toast naming the piece ("found: a web-draped candelabra"), the run HUD counter, the delve result listing "found for the Sunken Flagon", the death screen listing what was lost, and in Decorate Mode a "new" badge in storage.

**Cellars pool (about 10 pieces), themed by who drops them:**

| From | Pieces (art) |
|---|---|
| Spiders | Cobweb drapes ×2, a web-draped candle rack (Haunted, Church) |
| Slimes | A glowing green crystal lamp (Lost Civilization crystal), a slime-jar shelf (Shop Indoor potion row, green) |
| Bats | A tattered bat banner (Dungeon red bunting or a Castle banner in a dark palette) |
| Room curio caches | A cellar keg (Dwarven), a dusty statue (Dungeon), a bone pile (Piles of Loot), an old chest (Chests, dark) |

## 15. Death and duplicate options

**Normal discoveries (D9), recommended:** lost on death, like run Gold. The Lockbox stays a satchel rule. Picking up a curio is immediate, and getting home is the stake.

**Duplicates (D8), recommended:**

- A piece where copies are useful (chairs, candles, lamps, kegs) **can drop again**, and each copy is owned and placeable.
- **Unique pieces** already owned **are removed from the roll.** When the pool has nothing left to give, the roll **becomes run Gold** instead (no new currency).
- A **first-copy bias**: unowned pieces are weighted up (×3), so collections fill before copies pile up.

**Boss trophies (D10), recommended: never lost.** A first-clear trophy is granted **the moment the boss falls** and recorded like the boss-clear record (4e records the victory whatever the delve's end). The delve result says "the Larder Troll's tusks will hang in the Sunken Flagon", even after a death on the way out. Losing it would punish the biggest victory, and "I beat that thing, and my tavern remembers it" shouldn't depend on the rope.

## 16. Larder Troll trophy plan

**The experience:** "I beat that thing, and now my tavern remembers it."

- **The piece:** Minifantasy has no troll trophy, so it's a **composite of existing pixels**, recorded in `ASSET_MAP.md` as derived art:
  - **Chosen (D22):** *the Larder Troll's tusks*: the great curved horns from Giant Bones, recoloured to the troll's yellowed ivory, mounted on the wooden plaque of the Castles antler head. A **Wall** piece, about 3×2 tiles, unique.
  - *(Not in 4f, by decision D22: a stone troll statue for repeated clears.)*
- **Wiring:** `BossDefinition.trophyId = "trophy_larder_troll"`. On `BossFirstCleared` the furniture inventory grants it (unique, Boss source) and the delve result announces it. The furniture database is in Shared, so Dungeon never references Tavern.
- **The homecoming moment:** the first time the player opens Decorate Mode after earning it, the trophy is already **on the cursor**, with a line ("the Larder Troll's tusks: where should they hang?"). Placing it gets a bigger feedback beat (thud, dust, a warm flash, a satisfied rumble).
- **Reactivity:** contextual barks belong to Dialogue System (4g), so 4f doesn't write lines about the trophy. It **publishes the facts** (`FurniturePlaced`, which pieces are in the room, `TrophyDisplayed(bossId)`) for 4g's adapters. Patrons give a wordless **emote** (UI Overhaul "!" or a heart) on noticing it, as presentation only (D18).
- **No stat bonus** (D15).

## 17. Butcher Block plan

**The experience:** a big, strange monster part, a heavy knife, clean lines, and the satisfaction of a good cut giving more portions. Precision is the skill (GDD §6.2).

**Where it sits (D17), recommended: mise en place before the doors open.** At evening Prep, the player can walk to the Butcher Block and break down large parts into **portions** before service:

- **Why before service:** it adds a skill-based, tactile step with a real payoff (yield) without crowding the service rush, where the Grill, Tap, Stew and serving already compete. It also creates a choice: which parts to break down, and whether to spend a Premium leg on yield or keep it whole for a better dish.
- **The rule:** a butcherable part (Spider Leg, Bat Wing; later the troll-size parts) becomes **1–3 portions** depending on accuracy (a clean cut gives 3, a ragged one 1). Portions keep the part's quality and freshness. In data: `IngredientDefinition.butchering` (the portion ingredient, the yield curve). **Portions are their own ingredients** ("spider leg cuts"), so recipes ask for cuts or whole parts explicitly, and storage needs no new state.
- **Recipes:** a few dishes use cuts (§18). Whole-part dishes stay valid, so butchering is an investment, never a gate.
- **The minigame (`IMinigame`, Begin/Tick/Evaluate 0–1):** the part shown large on the panel (its 8×8 icon at 4×, or a drawn panel sprite), dotted cut lines; the knife follows the line under the stick or mouse while a pressure bar asks for steady speed. Accuracy along each line and the number of clean cuts set the score; score → yield. Five to ten seconds, like the others. Staff auto-resolve it at their cap (Gunta, §19).
- **Feedback:** the existing `Haptic_Cut_Ragged` and a new "clean cut" pattern, a thunk per cut, flecks, the portions sliding apart, the yield shown as pieces appearing.
- **The station:** the C&P II preparation table, movable (§5), with its working animation while used.
- **Tests:** score → yield mapping, cuts per part, staff resolution (EditMode); the station flow (PlayMode).
- **Not now:** butchering during service as a second stage of a dish. Recipe data stays open to it (`PrepRules.Steps`), and it's a natural Phase 5 signature-dish idea.

## 18. Recipe and surface-staple rework

**The model:** surface food is dependable and everyday; food from the Hollows is unusual, magical, monstrous or valuable (GDD §5.5). Farming, ranching and fishing are **not** built; a market stands in.

**Surface staples (bought), about five,** with icons from C&P II's preparation-table ingredients and the Farm animal-product icons:

| Staple | Category | Used for |
|---|---|---|
| Onions | Plant | Stews and pottage |
| Herbs | Plant (new flavour: Herbal? D19) | Grill and stew seasoning |
| Bread | Grain (new category) | Trenchers, toast, kebab bread |
| Eggs | Egg | Grill dishes |
| Malt | Grain | **Brackenford ale**, the tavern's own drink |

**How they enter the economy (D19), recommended: a market list in the daytime panel** ("the Brackenford market"): buy staples with Gold, delivered to the storeroom at once at **Standard quality, full freshness**, stacking and ageing like everything else (bread goes stale). Prices make staple-only dishes cheap and modestly profitable. `IngredientDefinition.source` (Hollows, Market; later Farm, Ranch, Fishing, Villager) is added as GDD §5.1 describes, and a `SupplySource` asset lists what the market sells and at what price. When 4h and Phase 5 bring the village shop and farming, they become more `SupplySource`s feeding the same storeroom; **recipes never name a source**, only ingredients or categories, so nothing is rewritten.

**Fixing the missing mushrooms:** Shroom Cap and Spore Sac join the Cellars' **ingredient room rewards** as foraged caches (the 4d pool), until the Mushroom People arrive.

**The Biome 1 menu (about 12 dishes), by tier (GDD §5.4):**

| Tier | Dish | Station | Ingredients |
|---|---|---|---|
| Everyday (surface only) | Brackenford ale (the full-tankard icon, Miscellany Icons row 9) | Tap | Malt |
| Everyday | Onion broth | Stew Pot | Onions ×2, herbs (optional) |
| Everyday | Eggs on toast | Grill | Eggs, bread |
| Better (one thing from below) | Gelbrew (slime-gel ale) | Tap | Slime gel, malt |
| Better | Grilled spider leg | Grill | Spider leg, herbs (optional) |
| Better | Crispy bat wings | Grill | Bat wing ×2, herbs (optional) |
| Better | Shroom skewer | Grill | Shroom cap ×2, spore sac (optional) |
| Better | Cellar stew | Stew Pot | Spider leg, onions, shroom cap (optional) |
| Better | Offal pottage | Stew Pot | Venom sac, onions, bread (optional) |
| Better | Cellar kebab | Grill | Any meat or offal, shroom cap, bread |
| Signature | Core tonic | Tap | Slime core, slime gel |
| Signature (butchered) | Spider-leg steaks | Grill | Spider leg cuts ×2, herbs |
| Signature (butchered) | Bat-wing platter | Grill | Bat wing cuts ×3, bread, spore sac |

Values follow the tiers (everyday about 5–8, better 9–16, signature 20–30), all on the assets. The delve meal keeps working: Grill and Tap dishes keep their buffs, re-tuned for the new list.

## 19. Pip, Gunta and customer-request scope

**Pip Marrowby (halfling, server and bookkeeper)** and **Gunta Ashbelly (dwarf, head cook)**, Stage 1. Dialogue, portraits and their story intros are 4g; 4f gives them presence, roles and personality through behaviour:

- **Looks:** Pip gets a halfling-proportioned look from A Myriad Of NPCs layers (smaller and rounder than the customers, a waistcoat and apron, a ledger tone), replacing the Butcher stand-in. Gunta gets a stocky bearded dwarf look in cook's whites and a red apron (the pack has no dwarf body; customers' dwarves already use this approach). Both are canonical: fixed names, not renameable.
- **Roles:** Pip serves (as now) and **keeps the books**: the Results screen's takings come "from Pip's ledger" (a small framing line and an icon). Gunta **cooks**: at Prep you choose her station (Grill, Stew Pot, or the Butcher Block during mise en place), and she runs it at her skill and quality cap, like Pip's serving. A **staff assignment row** on the Prep screen shows who's where.
- **Personality in behaviour (no dialogue):** Gunta tastes the stew and nods or frowns (an emote after a good or poor pot); Pip wipes a table after a guest leaves and straightens chairs; both react to a dropped plate. Idle beats use their animations plus UI Overhaul emotes.
- **Arrival:** both are present from a new game's first evening. Their story introductions come with the Act I opening in 4g (D20).
- **Hooks for 4g:** stable character ids (`pip`, `gunta`), the facts published as events (a dish served by Gunta, a perfect pot, a request met) for the dialogue adapters later.

**Customer requests (D21), recommended: in-evening special requests only, owned by the tavern service.** Persistent "bring me X" requests are quests and wait for Quest Machine in 4g (the open question from 4c, settled this way).

- Now and then (tunable chance), a seated customer asks for something particular instead of reading the menu: a **specific ingredient** ("something with spider in it"), a **quality** ("make it a good one": Fine or better), a **flavour** ("something earthy"), or a **performance** ("a clean pour"). The bubble shows it with icons.
- Met: a bonus tip and extra Renown with a happy feedback moment. Not met: they still eat and pay normally; nobody is punished for an impossible request. Requests are only made when something on tonight's menu can meet them.
- Data: `CustomerRequestDefinition` (kind, condition, reward multipliers, weight). Profiles can favour kinds (dwarves ask for meat, adventurers for strong ale).

## 20. Proposed playtestable 4f steps

**Checkpoints (2026-10-05).** The ten steps are grouped into four checkpoints, each ending with your playtest and approval:

- **Checkpoint A, the customization foundation (steps 1–2):** furniture as data and Decorate Mode; it locks the expensive architecture (placement, rotation, footprints, saves, validation, nav, movable stations).
- **Checkpoint B, rich customization (steps 3–6):** the catalogue pipeline and the large first import, buying, storage and Renown tiers, recolouring, the guest room. The question: can I meaningfully personalize the Sunken Flagon and another room?
- **Checkpoint C, the loop (steps 7–9):** discoveries and the Larder Troll's trophy, the market and recipe rework, the Butcher Block, Gunta and Pip. The question: delve → bring strange things home → cook and use them → improve the tavern.
- **Checkpoint D, sign-off (step 10):** requests, integration, balance, saves, web and regression testing.

Within a checkpoint I go on from one step to the next when its tests pass and the work stays inside the approved plan. I stop early only if an approved expensive decision needs to change, the work reveals a major architectural problem, scope would grow materially, saves or existing gameplay regress, or a subjective art or UX judgment from you blocks further work. The web build is checked at the end of each checkpoint, and at any step that changes saves.

| Step | Contents | You playtest |
|---|---|---|
| **1. Furniture as data** | `FurnitureDefinition`, `PlacedFurniture`, `FurnitureLayout`, `FurnitureInventory`, `PropertyArea`; today's tavern converted to a **starting layout asset** spawned by `LayoutView`; seats derived from chairs and tables; posts and queue derived; save **v4** with migration (and the seat upgrade decision, D16); fix the flaky Pip test | The tavern looks and plays exactly as before (a regression step), and old saves load |
| **2. Decorate Mode** | Enter and exit (daytime, night), cursor for gamepad, keyboard and mouse, pick up / move / rotate / flip / store / place, the ghost and reasons, undo and put-it-all-back, nav rebuild, the layout check with blocking and warnings, saved layouts, feedback pass | Rearranging the tavern, then running a service in your layout |
| **3. Catalogue pipeline and the first import** | Source table, island finder, generator, contact sheets; the first catalogue (§10) imported and recorded in `ASSET_MAP.md` | The pieces look right in the room (a sandbox "place anything" toggle for this step only) |
| **4. Buying, storage and Renown tiers** | Catalogue UI, purchase, storage, sell-back, starter ownership, tiers by Renown with their Night-screen moment | Furnishing the tavern from your purse; visibly different taverns |
| **5. Recolouring** | Palette maps, palettes and presets, baking cache, authored variants through one UI, copy and apply-to-all; floor and wall finishes if D5 is approved | Recolouring the tables and chairs; whether it still looks like Minifantasy |
| **6. The guest room** | A second area, its shell, the stairs and fade, area switching in Decorate Mode, its validation profile, its starting layout, saves | Decorating the guest room with the same tools |
| **7. Discoveries and the troll trophy** | Curio drops, the curio room reward, run HUD, result and death screens, ownership on extraction, the Cellars pool, the trophy composite and first-clear grant, the homecoming moment, feedback pass | A delve that brings something home, then placing it; beating the troll and hanging its tusks |
| **8. Recipes and the market** | Staples, the market, sources, mushrooms as forage, the new menu and values, delve-meal re-tune | An evening that mixes market and Hollows food |
| **9. Butcher Block and Gunta** | The minigame, butchering at Prep, cuts and yields, the station, Gunta's look, role and staffing, Pip's look and ledger, their idle behaviours | Mise en place and a service with both staff |
| **10. Requests and integration** | Customer requests, the reactivity facts for 4g, the full loop through `GameFlow`, balance pass, web build, docs | A full day or two of play; sign-off |

Steps 3–5 are the biggest; if the catalogue import runs long I'll split step 3 (pipeline first, the import second) rather than shrink the catalogue.

## 21. Tests, web and save coverage

**EditMode (pure logic):**

- Placement rules: bounds, layers, overlap, reserved tiles, wall-bound stations, facings and flip permissions.
- Seat derivation (chair facing a table, benches with several anchors), dynamic queue spots.
- Validation: each blocking check and warning on synthetic layouts, reachability, the starting layout passing, per-area profiles.
- Inventory: owned and placed counts, storage, uniques, buying and selling, starter grants, catalogue tiers by Renown.
- Palette remap: channels, ramps, presets, cache keys.
- Discoveries: drop pool rolls (seeded), first-copy bias, unique exclusion and the Gold fallback, extraction keeps them, death loses them, the trophy granted on first clear and never lost.
- Save v4 round trip; v3 → v4 migration (starting layout granted, the seat upgrade handled per D16); unknown furniture ids dropped with a warning.
- Market purchases, sources, recipes with staples and cuts, butcher score → yield, requests (generation only when satisfiable, evaluation, rewards).
- Text style for every new string (`TextStyleTests` covers new keys; gold in lower case).

**PlayMode:**

- The starting layout spawns the 4e tavern exactly.
- A customer paths around a moved table; a service runs to the end in a rearranged tavern.
- Decorate Mode flows with gamepad and mouse; the layout check blocks opening with the Grill stored.
- Walking to the guest room and back.
- A curio picked up in the dungeon, extracted and found in storage; one lost on death.
- The troll trophy granted on first clear.
- The Butcher Block panel giving portions.
- Gunta working her station.
- A full day through `GameFlow`.

**Captures:** two contrasting taverns and the guest room at 320×180, for the review.

**Web:** a build and smoke test at steps 2, 6 and 10: Decorate Mode with the mouse, buying, saving and reloading a layout in browser storage, the guest room, a delve with a curio.

**Save:** **v4**, adding `furniture` (owned counts, unlocked, discovered), `areas` (per area: placed pieces, finishes) and `market` state if any. `SaveSystem` stays the only save; layouts autosave on leaving Decorate Mode, after purchases and at the existing boundaries.

## 22. Decisions (locked 2026-10-05)

| # | Decision | Locked |
|---|---|---|
| D1 | Grid and snapping | Blocking furniture snaps to whole tiles with whole-tile footprints; non-blocking decor may use quarter-tile snapping. (Bodies stay pixel-exact.) **Amended after the Checkpoint A playtest, at your request:** snapping by default; holding the free-placement key (Shift on keyboard, LB on a gamepad) places to the art pixel (the mouse, or a pixel per arrow or d-pad press), stored as the whole-tile cell plus a nudge of under half a tile, so the rules, the walkable grid and saves are unchanged |
| D2 | Rotation | **Changed:** explicit rotation modes per piece: `None`, `AuthoredFacings`, `QuarterTurnSprite` (real 0°/90°/180°/270°). Rotation turns the footprint, bodies, seat anchors, use points, surface anchors and lights together. Authored facings are used where Minifantasy drew better ones; pieces that would look wrong turned (wall-bound pieces, strong baked light or perspective) opt out individually. Not limited globally to drawn facings |
| D3 | Flipping | Per-piece opt-in, off by default; only where mirrored art still reads correctly |
| D4 | Surface items | The simple Surface layer is in 4f (candles, bottles, tabletop clutter) |
| D5 | Walls, floors, doors | Area-wide floor and wall finishes; no per-tile painting; structure, doors and the entrance fixed in 4f |
| D6 | Functional furniture | All stations movable where practical; the Grill wall-bound; one of each station; stations may be stored; the layout check refuses to open without the required stations reachable |
| D7 | Ownership | Owned copies, not unlimited-after-unlock; uniques capped at one |
| D8 | Duplicate discoveries | Copies where useful; owned uniques excluded; exhausted pool → run Gold; first-copy bias ×3, tunable |
| D9 | Normal discoveries on death | Run-bound; kept on extraction; lost on death; not part of the Lockbox |
| D10 | Boss trophy | Granted when the boss falls; never lost afterwards |
| D11 | Recolouring | Authored palette-channel remapping, baked and cached textures; Minifantasy colourways as variants in the same UI; the lit sprite material and lighting path unchanged |
| D12 | Storage and removal | Property-wide unlimited storage; removing never destroys; bought pieces sell back at half price; discoveries and uniques can't be sold |
| D13 | Validation | Block only layouts that make service impossible; warn about partial problems; no aisle-width or style rules |
| D14 | Renown | Unlocks furniture catalogue tiers in 4f; 25 / 60 / 100 are tuning values; customer tiers wait for the village and Visitor work |
| D15 | Stat bonuses | None in 4f |
| D16 | Seat upgrade | `Upgrade_TavernSeats` retired; seating comes from placed, usable seating; bought levels refunded cleanly; every save gets the starting layout |
| D17 | Butcher Block | A Prep (mise en place) activity, not a mid-service stage; portions are ordinary ingredient data so later recipe stages can reuse them |
| D18 | Patron reactions before 4g | Wordless emotes only; no decor dialogue or barks until Dialogue System owns them |
| D19 | Surface staples | A temporary Brackenford market list in the daytime panel: Standard quality, full freshness, immediate delivery, normal ageing; `SupplySource` so village shops, farming, ranching and fishing replace or supplement it later; the Grain category |
| D20 | **Gunta Ashbelly** | **Changed:** Gundra Ashbelly is renamed **Gunta Ashbelly**, stable id `gunta` (nothing in code, data or saves used the old name, so there is no legacy id). Present from the first evening; her story introduction comes with Act I in 4g |
| D21 | Customer requests | Short in-evening service requests; persistent authored requests wait for Quest Machine in 4g; only requests tonight's menu can satisfy; failing one is never a punitive dead end |
| D22 | Larder Troll trophy | **The Larder Troll's tusks:** the Giant Bones horns on the Castles antler plaque, the unique first-clear Wall trophy, recorded as derived art in `ASSET_MAP.md`. No repeated-clear statue in 4f |
| D23 | `Tavern.unity` | Modified only through the in-place updater: never regenerated; idempotent (re-running never duplicates layout roots, furniture, guest-room structures or anything else it makes); unrelated hand-made scene changes preserved |

**Checkpoint B as built (2026-10-05):** 98 catalogue pieces (394 entries) plus the 13 starting pieces; tiers 0/25/60/100; recolouring by palette channels with baked copies plus Minifantasy colourways in one panel; 13 area-wide finishes; the guest room from the Shop Indoor shell (18×13) reached by the stucco stairs, which also work during service because the keeper only walks then until the village milestone (see `docs/PROGRESS.md`, Checkpoint B, for every deviation).

**Also confirmed:** the ~70-piece / ~170-entry first catalogue (not a token proof); the guest room as a proof of one architecture across areas (no guests, ratings, occupancy, recruitment or hotel systems); the loop Hollows drop → pickup → extract → ownership → Decorate Mode → placement; the six-slot Satchel ingredient-only; the troll trophy as the guaranteed first-clear reward; customer and decor dialogue deferred to 4g.

**What I'll need from you in the editor** (at each checkpoint): playtesting; judging the recolours and the trophy composite by eye; checking Decorate Mode on a real controller with rumble; and, in Checkpoint B, a quick review of the catalogue contact sheets (which pieces read well at 320×180).
