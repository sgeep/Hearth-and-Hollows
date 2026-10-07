# Asset Map

Which Minifantasy art the game uses, where it comes from, and what each sheet contains. Raw packs live outside the repo in `C:\Dev\Minifantasy`; only the files listed here are imported, into `Assets/ThirdParty/Minifantasy/<Pack>/`.

_Last updated: 2026-10-05 (4f planning: furniture and decor survey; tankards found in Miscellany Icons)_

## How art gets into the project

1. Find it in the catalog (`C:\Dev\Minifantasy\List\Minifantasy_Asset_Catalog.csv`) by name, category or biome tag.
2. Add a `Sheet` entry to `MinifantasySheets.cs` (`Assets/_Project/Scripts/Editor/Setup/`): the source path, the pack folder, and how it is sliced.
3. Run **Hearthdelve → Art → Import Minifantasy** (also part of **Generate → 4a Look Test (All)**). The file is copied in and the import postprocessor sets it up.
4. Record what the sheet contains here, so nobody has to inspect it again.

Every texture under `Assets/ThirdParty/Minifantasy` is imported as a sprite at **8 pixels per unit**, point filtered, uncompressed, without mipmaps. An EditMode test (`MinifantasyImportTests`) checks this.

**Slicing modes**

| Mode | Sprite names | Used for |
|---|---|---|
| Grid | `File_column_row`, row 0 at the **top** | Character sheets (32×32 cells), tilesets and icon sheets (8×8 cells) |
| Rects | `File_Name`, rectangles given in pixels from the top-left | Prop sheets and premade layouts |
| Single | the file name | Whole images |

Sprite ids are derived from sprite names, so re-slicing a sheet never breaks references.

## Characters

All character sheets use **32×32 frames** with the body (about 8×8) in the middle. The feet are 13 px above the bottom of the frame, so the pivot is (0.5, 13/32) and sprites sort by Y at the feet.

Rows are the four drawn facings, top to bottom: **front-right, front-left, back-right, back-left**. Death sheets have one row, used for every facing. Each pack's `_AnimationInfo.txt` gives the frame durations: 200 ms for idle and walk, 100 ms for everything else.

Shadows are separate sheets with the same layout, drawn under the body.

| Use | Pack (folder) | Sheets | Frames per row | Notes |
|---|---|---|---|---|
| Player | Creatures → `Creatures/` | `HumanTownsfolk` Idle, Walk, Attack, Dmg, Jump, SpinDie, ChargedAttack; `ShadowHumanoid…` | 16, 4, 4, 4, 4, 12, 6 | Jump is used for the dodge roll and has **one row** (its shadow sheet has four). **ChargedAttack's three rows are stages, not facings:** wind-up, the charged loop (sparkles), and a 360° spin release, the same for every facing. It is the heavy attack. |
| Green Slime | Creatures → `Creatures/` | `SlimeGreen` Idle, JumpAttack, Dmg, Die; `ShadowSlime…` | 8, 4, 4, 9 | JumpAttack is its movement (200 ms) and, faster (100 ms), its leap attack. |
| Bat | Creatures (Beasts) → `Creatures/` | `Bat` FlyIdle, Attack, Dmg, Die, Sleep; `ShadowBat` Fly, Attack, Dmg, Die, Sleep | 2, 4, 4, 9, 8 | FlyIdle (64×128) is both idle and flight. **BatSleep's three rows are stages:** hanging asleep (8 frames), waking (5), falling asleep (5). The swoop lands on Attack frame 2. **The sleep pose hangs from a wall:** place a sleeping bat at the top of the floor tile directly under a wall (feet 0.55 tiles up the tile) and the pose sits on the brick face. |
| Giant Spider | Exclusive `Creatures/Giant_Spider` → `GiantSpider/` | `GiantSpider` Idle, Walk, Attack, Dmg, Die, ShotWebDiagonal; shadows Idle, Walk, Attack, Dmg, Die, WebShot | 17, 6, 7, 4, 33, 14 | No frame-timing notes in the pack: 100 ms throughout. The body is about 20 px wide (legs spread wider), so its collider is the body only (0.9×0.5 tiles) and it fits through doorways. The bite lands on Attack frame 4; the web leaves on ShotWebDiagonal frame 9. `ShotWebOrthogonal` exists for the four straight directions; we only draw four diagonal facings, so it is not imported. |
| Tavern cook (NPC) | A Myriad of NPCs → `AMyriadOfNPCs/` | `CookerIdle` | 16 | Premade NPC. Walk, Dmg, Die and Working (8 frames × 1 row) exist but are not imported. |

### Tavern customers (A Myriad of NPCs, layered → `AMyriadOfNPCs/`)

`Generic_NPCs/{Idle,Walk}` hold one sheet per layer variant: idle is 16 frames, walk 4, both 32×32 with the four facings as rows (the same layout as the player). Layers: `_Characters/{Human,Elf,Orc}` (bodies, by skin), `Body/` (Blouses, Doublets, Gloves, Jacket, Shirt, Shoes, ShoulderPads, Togas, Trousers, each in 15 colours), and `Head/` (Facial_Hair: 5 styles; Hairstyles: 8, drawn once as `HumanHair` for every race; Hats: 8). Only idle, walk, damage and die exist: **no sitting pose**. The "Short" hairstyle's file names have a space before the colour.

Imported for 4c, curated for readability at 320×180 (`MinifantasySheets.NpcLayers`, as `Npc{Idle|Walk}_{category}_{kind}_{variant}`):

| Layer | Variants |
|---|---|
| Body | Human pale, white, brown, black skin; Elf elfskin, albino |
| Top | Shirt red, blue, yellow, white, orange; Doublet purple, turquoise, red; Jacket blue, magenta (no greens or browns: they vanish on the tavern floor) |
| Trousers | black, grey, blue |
| Hair | Short, PonyTail, Long, Bold in black, brown, blonde, red, white |
| Hat | Hood blue, red, purple; RangerHat blackleather |
| Beard | LongBeard in black, brown, blonde, red, white |

Orc bodies (green skins) aren't used: they read poorly on the green floor. Drawn back to front: body, trousers, top, beard, head. Plus `NpcShadow{Idle,Walk}` (`Shadows/ShadowHumanoid…`).

**Appearance pools** (`Data/Customers/Appearance_*`): villagers wear shirts and hair, adventurers jackets, doublets, hoods and hats, dwarves always have a long beard. **There is no dwarf body** in the pack: dwarves are humans in stocky colours with a beard.

**Pip's stand-in:** the premade `Butcher` (`Premade_NPCs/Butcher`, idle and walk; apron, bright blonde hair), until Pip's own look in 4f. The other premades are Alchemist, Blacksmith, Carpenter, Cooker (the 4a cook), Dyer, Furrier, Jeweller and Tailor, each with idle, walk, damage, die and a working loop.

### Emotes (UI Overhaul → `UIOverhaul/Emotions`)

`_Emotions.png` (152×104) is a 16 px grid of 8×8 faces (a cell's face at its +8,+8), made to sit inside the speech bubble (`Bubble_Body`). Used: `Thinking` (136,88: "…", reading the menu) and `Angry` (72,40: walking out). Others include laughing, crying, a heart and a music note.

**Player body: placeholder choice.** The Human Townsfolk is a clothed body with a full attack set, picked so the look test shows a dressed character. The Dungeon pack's "Human" is an unclothed base body. The final protagonist (a pre-clothed body with palette swaps) is still to be chosen; True Heroes and the Weapons pack are the candidates.

## Dungeon (`Dungeon/`)

### `Tileset.png` — 184×112, 23×14 cells of 8 px

Cells are (column, row) from the top-left.

| Cells | What |
|---|---|
| (13,2) (14,2) | Plain flagstone floor |
| (15,2) (16,2) (18,2) | Cracked floor variants |
| (17,2) | Floor with a dirt corner |
| (19,2) | Floor drain |
| (13–18,3) | Small-tile floor with dirt patches |
| (19,3) | Water grate |
| (21,2) (21,3) | Dark pit |
| (4–10, 5–12) | Wall sample: two rooms side by side. Light blocks are wall tops, dark bricks are front faces. |
| (4,5) / (10,5) | Top-left / top-right corner |
| (5,5) (6,5) | Top wall, top face |
| (5,6) (6,6) | Top wall, brick front |
| (4,6) (10,6) | Side wall beside the brick front |
| (4,7) / (10,7) | West / east side wall |
| (7,5–12) | Dividing wall between the two sample rooms |
| (4,11) (5,11) (6,11) (10,11) | Bottom wall, top face (corners at 4 and 10) |
| (4,12) (5,12) (6,12) (10,12) | Bottom wall, outer brick face |
| (1,2–3), (1,7–10), (6–8,2–3) | Free-standing pillars and short wall pieces |
| (13–15,5–6) | Archway / doorway |
| (17,5–6) | Wall banner |
| (13–15,9) | Cracked brick wall |
| (13–15,11–12) | Wall with vines |

**Wall auto-tiling (4b test floor).** `TestFloorBuilder` picks wall tiles from the sample by what is open around each wall tile:

| Situation | Tiles |
|---|---|
| Brick face (floor below) / top face (brick face below) | (5–6, 6) / (5–6, 5), alternating by column |
| Bottom wall: top face (floor above) / outer brick face | (5–6, 11) / (5–6, 12) |
| Side wall with floor to the east / west / both sides | (4,7) / (10,7) / (7,7) |
| Corner at the west / east end of a face, or between two runs | column 4 / 10 / 7, on the face's row |
| T-junction (wall above and below) | rows 8 (top face) and 9 (brick face) instead of 5 and 6 |
| Pillar: top face over brick base | (1,2) over (1,3) |

`Shadows.png` (same layout) is not imported yet.

### The rope out (Hole Entrances And Ropes, exclusive add-on → `Dungeon/`)

| File | Sprite | What |
|---|---|---|
| `Ropes.png` (200×80) | `Ropes_Hanging` 12,8 9×27 | A rope hanging from above with its coil on the floor (the pivot): the way back up to the tavern. The rest of the sheet is a stake with a rope going down a hole, in wood and stone. |
| `RopesShadows.png` | `RopesShadows_Hanging` 11,30 7×5 | The coil's shadow. |

`HoleEntrances.png` (200×80) has holes in the floor in two rows: three plain dirt holes (small, medium, double), one with a stake and rope, and one with a **wooden ladder frame**. The ladder-framed hole is imported as the way down to the next floor (4d): `Holes_Ladder` 177,14 15×11, pivot centre, drawn on the floor (sorting `Ground`, order 3). The plain holes read as dirt pits on the Cellars' stone; the frame reads as a way down. `RopeClimbing_<race>.png` (8 frames of 32×32, back view) animates a climb, but only for the pack's bare base bodies; our player is the clothed Townsfolk, so the climb is shown by rising and fading instead.

### The room gate (Gladiator Arena, exclusive add-on → `GladiatorArena/Gate.png`)

Source: `All_Exclusives_20261002/Addons/Towns_I_II/Gladiator_Arena/Tileset/Animated Gate/Gate_open_close.png`, 128×48: 8 frames of 32×24 (4 per row), a portcullis in a sandstone arch. Frame 0 closed, 1–3 the bars sinking, 3 open (only the tips show), 4–7 rising again, 7 closed. Only each frame's **barred interior** is imported (`Gate0`–`Gate7`, 16×15 at frame (8, 9), pivot bottom centre): it fills a two-tile doorway cut through the Cellars' own grey north wall, so the sandstone arch never shows. The bars are copper on a transparent ground. Also in the folder: `Gate_shadows_in_exteriors.png` and `Gate_shadows_towards_indoor.png` (not imported) and `GIFs/Gate.gif`. The Dungeon tileset's own archway (cells 13–15, 5–6) has a one-tile opening, too narrow for these bars.

### Ancient Troll (exclusive → `AncientTroll/`): the Larder Troll, 4e

Source: `All_Exclusives_20261002/Creatures/Ancient_Troll/`, 32×32 frames, shadows in `Shadows/`. No timing notes in the pack (100 ms used; idle 150 ms, walk 120 ms). Body about 15×16 pixels.
- `AncientTrollIdle` 576×64: 18 frames, **two rows, front-right and front-left only**; the back facings use the walk's first frame.
- `AncientTrollWalk` 192×128: 6 frames, four facings.
- `AncientTrollAttack` 224×128: 7 frames, four facings: arms up (0–3), both fists into the floor with an impact ring (4), dust (5–6). Used for the ground slam, released on frame 4.
- `AncientTrollDmg` 128×128: 4 frames, four facings, red outline on frames 0 and 2. Held as the dazed pose after a charge into a wall.
- `AncientTrollDie` 672×32: 21 frames, one row.
- `AncientTrollEat` 352×32: 11 frames, one row (front): looped while it's found eating; step 2's eat-the-drops.

### True Heroes I & II skill icons (exclusive → `SkillIcons/SkillIcons.png`)

Source: `All_Exclusives_20261002/Icons/16x16px/True_Heroes_I&II_16x16px_Skill_Icons/16x16px_Skill_Icons.png` (208×336): 37 icons of 16×16 on a dark tile, 32 px apart starting at 16,16, one row per hero (barbarian, druid with its shape-shift pairs on a second row, rogue, bard with its four enhancement songs in a column on the right, cleric, paladin), listed in the pack's `Info.txt` and `_Guide.png`. A version without the dark tile sits beside it. The run powers (4d step 4), by top-left corner: deep reserves `BardBallad` (heart) 112,144; slow burn `ClericDivineFire` 48,272; thick hide `BardDefense` (shield) 176,176; keen edge `BardMelee` (sword) 176,144; heavy hand `PaladinHolyHammer` 112,304; light feet `RogueDodge` 80,112; second wind `ClericHealingWords` (green cross) 80,272; butcher's eye `RogueAttack` (dagger) 16,112. The True Heroes III & IV and True Villains sets in the same folder are uninspected.

### Miscellany Icons (exclusive → `MiscellanyIcons/Miscellany.png`)

Source: `All_Exclusives_20261002/Icons/8x8px/_Miscellany_Icons_(Coins, Torches, MMO_UI, etc.)/Miscellany_1.png` (72×136, 8×8 cells). Rows per the pack: 1 coins (gold, silver, copper; big and small; stacks), 2 torches (new, lit, used), 3 a pick-up animation, 4 a drink-potion animation, 5 party, 6 class icons (barbarian, druid, rogue), 7 woodwork, 8 log in/out and new character, 9 beer (tankards: full, half, empty, each big and small, at y 64), 10 card frames. Imported: `Miscellany_GoldCoin` 0,0 8×8 (the big gold coin): run Gold on the HUD, the Gold room door sign and the Gold pickup (4d step 3).

### `Props.png` — 232×88

Rectangles are x, y, width, height from the top-left.

| Sprite | Rect | What |
|---|---|---|
| `Props_Table` | 56,8 16×8 | Long table |
| `Props_Crate` | 104,8 8×8 | Crate |
| `Props_Barrel` | 200,24 8×8 | Barrel |
| `Props_BarrelOpen` | 216,24 8×8 | Open barrel |
| `Props_Cauldron` | 202,43 12×11 | Cauldron on a fire |
| `Props_Statue` | 8,58 8×12 | White statue |

Also on the sheet, not sliced yet: chairs (8,8) (24,8) (40,8), broken table (80,8), sacks and pots (129,8 onwards), small gems (154–170,10), boots (189,9) (201,9), candle (217,9), wardrobe (8,24 16×16), broken wardrobe (32,24), doors (56–88,24), red banners (104,30 32×9), headstones (136–177,30), a small torch (190,28), and statues in grey and green (rows from y=48).

### `Torch.png` — 128×24, 8 frames of 16×24, 200 ms

The visible torch is about 10 px tall, 4 px down from the top of its frame. `Torch_Light.png`, `Candle.png` and `Candle_Light.png` are not imported yet.

## Tavern Indoor add-on (`TavernIndoor/`)

Source: `All_Exclusives_20261002/Addons/Towns_I_II/Tavern_Indoor/Separate_Layers/`. Every layer is 320×104. The left part is a prop sheet; the right part (x 224–312, y 8–96) is a **premade 11×11-tile tavern room**. The look test uses the premade room, cut from each layer.

| Layer file | Sprites cut | What |
|---|---|---|
| `TavernIndoor_base_building` | `Room` 224,8 88×88 | Stone shell of the room |
| `TavernIndoor_floor2` | `Floor` 228,32 80×64 | Green and brown diamond floor. (`floor` is a white and grey checker; not imported.) |
| `TavernIndoor_wall` | `Wall` 228,12 80×20 | Wood-panelled back wall |
| `TavernIndoor_shadows` | `Shadows` 224,8 88×88 | Shadows for the premade room |
| `TavernIndoor_props` | `Shelves` 252,16 48×14; `Sign` 233,17 14×6; `Bar` 242,26 59×26; `StoolA` 235,33 5×6; `StoolB` 235,40 5×6; `TableSetA` 232,58 24×22; `TableSetB` 280,58 24×22 | Back-bar shelves, a sign, the L-shaped bar with its seven stools, two loose stools, and two round tables with three chairs each |
| `TavernIndoor_props2` | `ShelfGoods` 252,16 48×15; `BarTop` 244,31 12×12 | Bottles and glasses on the shelves; taps and glasses on the bar |

**The 4c tavern (`Tavern` scene) is the premade room stretched.** The premade room sits exactly on the 8 px grid: 11×11 cells from sheet pixel (224, 8), its door in column 5. Its interior columns and floor rows repeat exactly, so the 28×17-tile room is cut cell by cell (`Cell_column_row` on `base_building`, `wall` and `floor2`): edges from the edge cells, everything else from column 2 and floor row 5, the door from column 5. The side walls are half a tile thick. The front wall is the `base_building` stone band (row 9) and dark outside (row 10), with a 5 px door gap at x 266–270. `floor2`'s 8×8 sample at (8, 48) is *not* the room's floor tile; the room cells are.

**The prop half (x < 224), measured for 4c** (sheet pixels x, y, w×h; all on `TavernIndoor_props` unless noted):

| Sprite | Rect | What |
|---|---|---|
| `TableRoundA` / `TableRoundB` | 90,42 / 106,42, 12×12 | Big round tables (the dining tables) |
| `TableRoundSmallA` / `B` | 88,24 / 104,24, 8×8 | Small round tables |
| `TableSquare` | 50,25 20×23 | Square table, drawn to sit inside four benches |
| `BenchBack`, `BenchFront` | 51,17 / 51,49, 18×7 | Benches above and below the square table |
| `BenchLeft`, `BenchRight` | 42,26 / 73,26, 5×22 | Benches either side of it |
| `LongTableH` | 130,40 28×8 | Long table (the pass) |
| `LongTableV` | 169,34 6×22 | Long table, vertical |
| `ChairFacingN` / `S` / `E` / `W` | 137,26 6×6; 145,24 / 153,24 / 161,24, 6×8 | Chairs by the way the sitter faces (N shows the backrest in front) |
| `StoolRedA`, `StoolRedB`, `StoolPlain` | 178,25 / 185,25 5×6; 194,25 4×6 | Stools |
| `ShelfTall`, `ShelfLow` | 44,64 16×14; 68,72 16×6 | Shelf units |
| `SignSmall` | 185,65 14×6 | Small sign |
| `props2`: `Taps` | 193,40 7×5 | A row of three taps (with a corner piece at 188,47 and a column at 200,47, not sliced) |
| `props2`: `BottlesA` / `B` / `C`, `Glasses` | 93,66 / 117,66 / 141,66 13–14×5; 165,66 14×4 | Rows of bottles and glasses for shelves (a second, identical row sits 7 px lower) |

Not sliced: a small red cushion (122,26 4×4).

## Kitchen (Crafting And Professions II → `CraftingAndProfessions/`)

| File | What |
|---|---|
| `Kitchen` (`KitchenProp`, 32×32) | A stone oven (1,4 11×18) behind a range with pans (12,21 16×8): the **Grill station** at rest |
| `KitchenShadow` | Its shadow |
| `KitchenWorking` (256×32, 8 frames of 32×32) | The same kitchen at work: fire under the oven, sizzling pans and smoke. For the Grill in use (4c step 3) |

The pack's `Characters/KitchenWorking_<race>` sheets are unclothed base bodies working at the kitchen (arms raised), like Carrying Animations: they don't fit the clothed stand-in.

## Dish icons (→ `CraftingAndProfessions/DishIcons`, `PotionIcons`)

8×8 icons on an 8 px grid, shown on the pass, over a carrier's head and in a waiting customer's bubble.

| Recipe | Icon | Source |
|---|---|---|
| Cellar Kebab | `MeatSkewer` 64,8 | Crafting And Professions II `Craftable_Item_Icons/…Recipes.png` (160×80): row 1 is skewers and roasts, row 2 braised plates, row 3 sushi, rows 5–7 bread, burgers, pies and the like, row 8 seven soup bowls |
| Shroom Skewer | `GreenSkewer` 96,8 | same |
| Grilled Spider Leg | `Drumstick` 136,8 | same |
| Cellar Stew | `BrownStew` 40,64 | same |
| Offal Pottage | `RedStew` 64,64 | same |
| Core Tonic | `BlueFlask` 72,40 | Crafting And Professions I `Craftable_Item_Icons/…PotionIcons.png` (216×152): vials, round flasks and gems in many colours |
| Gelbrew | `GreenFlask` 72,56 | same |

**Tankards exist** (corrected 2026-10-05): Miscellany Icons row 9 (`Miscellany_1.png`, pixel y 64) has a full, a half-full and an empty tankard, each big and small (x 0/8, 16/24, 32/40). The Tap's drinks are still potion flasks; the ale dishes in 4f use the tankards. (The Tavern Indoor `Glasses` row is decor, not an icon.) *More Food Recipes* (exclusive, 104×72) has more skewers, sushi and dishes.

## Fire (Dwarven Kingdom → `DwarvenKingdom/`)

| File | What |
|---|---|
| `FloorFireplace` (128×16, 8 frames of 16×16) | A small floor fire: under the **Stew Pot** cauldron (Dungeon `Props_Cauldron`) |
| `WallFireplace` (192×24, 8 frames of 24×24) | Despite the name, a small flame at the foot of a wall: one on the tavern's back wall, as a wall lamp |

## Furniture and decor survey (4f planning, 2026-10-05; nothing imported yet)

Inspected for the customization catalog (`docs/PLAN_4F.md` §9). Paths are under `C:\Dev\Minifantasy`; `AE` = `All_Exclusives_20261002`. Sizes are whole-sheet pixels. Nothing here is sliced or imported yet; the rects get measured when a piece enters the catalog.

**A recurring pattern:** Minifantasy draws furniture in **authored colourways** (the same piece in several palettes), and draws **each facing as its own sprite** (chairs facing N/S/E/W, horizontal and vertical long tables, beds head-up and side-on). So "rotation" means switching between drawn facings, and many recolours already exist as art.

| Sheet | Size | What's on it |
|---|---|---|
| `AE/Addons/Towns_I_II/Tavern_Indoor/Separate_Layers/TavernIndoor_props.png` (+ `props2`) | 320×104 | Already in use and measured above: the L-bar, round, small round, square and long tables, benches, chairs by facing, stools, shelves, bottles, glasses, signs |
| `AE/Addons/Towns_I_II/Shop_Indoor/Separate_Layers/ShopIndoor_props.png` (+ `counter`, `wall`, `floor`, `basebuilding`) | 456×256 | A second interior shell (green or cream walls; purple or teal floors), L-counters, banners in three colours, cupboards and crates, mannequins in six outfits, display tables and racks, **candles in eight colours** and several heights, potted plants, potion rows, a barrel |
| `AE/Addons/Towns_I_II/Plant_Pots/PlantPots.png` | 288×224 | **Eight pot colours** × about sixteen plants (small, large, flowering, leafy, knocked over) |
| `AE/Addons/_Miscellany/Barracks_Props/_Barrack_Props.png` | 776×760 | **Beds** in wooden and iron frames with red, blue and plain blankets, drawn side-on and head-on; weapon racks; armour stands; many training dummies; a roped ring |
| `AE/Addons/Towns_I_II/Church/Church_Indoor/ChurchIndoor_props.png` (+ `Candles/`) | 544×272 | Pews, an organ, candle racks, a lectern, crosses, standing candles (animated, with light layers) |
| `AE/Addons/_Miscellany/Astronomical_Observatory/Props/Props.png` | 296×232 | Wall and standing telescopes, a globe, a star chart, a star-map table, orreries, small lamps (curio material) |
| `AE/Addons/_Miscellany/Wizard_Tower/Props/Wizard_Tower_Props.png` | 72×168 | Bookcases (full, half, empty), a round table, a padded bench, book stacks, scrolls, a gold orrery |
| `AE/Addons/_Miscellany/Chests/Chests.png` | 288×285 | Chests and trunks in four sizes, in **eight colourways** |
| `AE/Addons/_Miscellany/Piles_Of_Loot_And_Stuff/PilesOfLootAndStuff.png` | 112×200 | Gold hoards, crates spilling gold, scrap, rubble, **meat-and-bone piles**, bones, bloody bones, each in four sizes |
| `Minifantasy_Medieval_City_v1.1/…/Props/Props.png` (+ `Animated_Props/Fireplace/`) | 432×192 | Wardrobes, dressers and chests of drawers in **blue, brown and pink**, beds, a **stone chimney fireplace** (animated version with a light layer), window flower boxes, street lamps, benches, barrels, crates |
| `Minifantasy_CastlesAndStrongholds_v.2.0/…/Props/Props.png` | 264×256 | **Two colourways** (gold-and-red, silver-and-red): banners, framed paintings, crossed weapons, shields, **mounted antler heads**, thrones, banquet table and chairs (four facings), braziers, rugs, armour stands, a candlestick |
| `Minifantasy_Towns2_v1.5/…/Props/Minifantasy_TownsIIProps.png` (+ three `…IndoorTileset.png`: brick, stucco, plank) | 264×120 | Sofas, room dividers, cabinets, **rugs in five colours** (with matching swatches), a mirror, a bathtub, a washstand, tableware, lamps. The indoor tilesets are further room shells |
| `Minifantasy_Towns_v3.0/…/Props/Minifantasy_TownsProps.png` | 224×384 | Hanging shop signs (including a tankard sign), a forge, potted plants, framed pictures, wall-mounted weapons, shelves and **bookcases with many fills**, cupboards, dressers, beds with red blankets, round tables with white cloths, stools |
| `AE/Seasonal_Content/Minifantasy_Haunted_House_v1.0/…/Props/HauntedHouseProps.png` | 160×240 | Cobwebs, pumpkins and jack-o'-lanterns, patterned carpets, mirrors, **six haunted portraits**, wingback armchairs, sofas, tall cabinets, curtains, chairs |
| `Minifantasy_DwarvenKingdom_v1.0/…/Props/Decoration/Props.png` | 576×232 | Tables, stools and benches in **six materials** (wood, stone, violet, ice, slate, moss), barrels, kegs and a great tun, mine carts, lanterns in three colours, rugs (red, ochre, teal), statue niches, banners, rune panels |
| `Minifantasy_ElvenKingdom_v.1.0/…/Props/Props.png` | 296×320 | **Five colourways** (green, blue, red, purple, gold): bunting, banners, chests and cabinets, lamps, round tables and stools, rugs, tree banners |
| `AE/Seasonal_Content/Minifantasy_Lunar_New_Year_Festival_v1.0/Tiles And Props/LunarNewYear_props.png` | 376×144 | Paper lanterns and lantern stands, firecrackers, hangings, a gong, lucky-cat and tiger figures in four colours |
| `AE/Addons/Desolate_Desert/Giant_Bones/Bones.png` | 136×80 | Great curved horns or tusks, a long bone, small bones |
| `Minifantasy_CraftingAndProfessions2_v1.0/…/Food_Preparation/…PreparationTableProp.png` (+ `…Working.png`) | 32×32 | A preparation counter with vegetables and raw meat, with a working animation: the **Butcher Block** candidate |
| `Minifantasy_CraftingAndProfessions2_v1.0/…/Craftable_Item_Icons/…PreparationTableIngredients.png` | 96×88 | 8×8 icons: onion, beet, tomato, herbs, bread, dough, fish, raw cuts (surface-staple icons) |
| `AE/Icons/8x8px/Home_Icons/HomeIcons.png` | 88×112 | House icons only (map markers), not furniture |

**Not yet opened, worth a look when the catalog grows:** Stained Glass Windows (seven colours), 8×8 Flags, Chamber Of Secrets, Skull Hideout, Lost Civilization crystals, Glowing Mushrooms, Deep Caves props, Spooky Graveyard, More Lovecraftian Statues, Painter Studio, Animated UI Book (a catalogue UI candidate).

**Gaps found:** no mounted troll head or troll-sized trophy (the Larder Troll's trophy needs a composite, `docs/PLAN_4F.md` §16); no tabletop clutter sized for the round tables beyond candles and tableware. (Tankards do exist, as icons: Miscellany Icons row 9.)

## Furniture catalogue (4f Checkpoint B, 2026-10-05)

The catalogue's art is defined by `Assets/_Project/Data/Furniture/Catalog/catalog.json` (sheets, rects per facing, colourways) and generated by `FurnitureCatalog`. Each source sheet is copied into `ThirdParty/Minifantasy/Furniture/Furniture_<sheet>.png` **with only the named drawings kept** (everything else cleared), so only the chosen sprites enter the repo; the slicing is in `catalog_sprites.json`. Contact sheets: `python Tools/furniture/contact.py` (`BatchLogs/catalog/contact_<category>.png`). Paths below are under `C:\Dev\Minifantasy`; `AE` = `All_Exclusives_20261002`.

| Sheet key | Source | Pieces taken |
|---|---|---|
| tavern, tavern2 | `AE/Addons/Towns_I_II/Tavern_Indoor/Separate_Layers/TavernIndoor_props.png`, `props2` | stool, bench (4 facings), small round table (2 woods), big square table, long table (2 facings), tall shelf, small sign, rows of bottles (3) |
| pots | `AE/Addons/Towns_I_II/Plant_Pots/PlantPots.png` | 8 plants (4 small for surfaces, 4 large), each in 6 glazes: blocks 144 px apart across, 72 down |
| dwarven | `Minifantasy_DwarvenKingdom_v1.0/…/Props/Decoration/Props.png` | chairs (4 facings), round, side, long (2 facings) and great tables, all in 6 materials (wood, stone, violet, slate, ice, moss: offsets (0,0) (104,0) (0,72) (104,72) (0,144) (104,144)); tankards (4 facings); keg, stacked barrels, great tun, barrel pyramid; chest (6 materials, 40/16 apart); rugs (red, ochre, teal, 24 apart); banners (3 colours); lantern (3 colours); cot (3 colours) |
| elven | `Minifantasy_ElvenKingdom_v.1.0/…/Props/Props.png` | chair (4 facings), ring table, stump table, bookcase, rug, bunting, planter, lamp post, tree banner; 5 colourways 64 px apart (green, blue, red, purple, gold) |
| castle (+ torch sheets) | `Minifantasy_CastlesAndStrongholds_v.2.0/…/Props/Props.png`, `Props/Torch/Torch_stand*.png`, `Torch_wall*.png` | banquet chair (4 facings), high-backed chair, banquet table (2 facings), banners (6), portraits (3), wall arms (4), mounted antlers and bear head, rug, runner, candlestick, royal bed (gold or silver colourway, 128 px apart); standing and wall torches (gold or iron, 8-frame strips) |
| haunted | `AE/Seasonal_Content/Minifantasy_Haunted_House_v1.0/…/Props/HauntedHouseProps.png` | wingback armchair (4 facings, whole or clawed), sofa (3 facings), creaking chair (4 facings), portraits (6), cobwebs (3) and a great web, carpets (2), gilt mirror, jack-o'-lanterns (4), tattered curtains |
| towns | `Minifantasy_Towns_v3.0/…/Props/Minifantasy_TownsProps.png` | framed pictures (7), cupboard (5 fills), bookcase (3 fills), cloth-covered table (4 settings), hanging signs (tankard, potion, swords, helm) |
| towns2 | `Minifantasy_Towns2_v1.5/…/Props/Minifantasy_TownsIIProps.png` | rugs (5 colours, 24 apart), leather sofa and armchair (3 facings), washstand, standing mirror, bathtub |
| medieval | `Minifantasy_Medieval_City_v1.1/…/Props/Props.png` | single bed (4 facings) and double bed (3 facings) in linen or brown (56 / 64 apart), nightstand and chest of drawers (blue, brown, pink, 80 apart), wardrobe (3), large round table |
| fireplace | `…/Medieval_City…/Props/Animated_Props/Fireplace/Fireplace.png` | the stone fireplace (8 frames, 40×48) |
| chests | `AE/Addons/_Miscellany/Chests/Chests.png` | chest, travelling trunk, mimic chest; 8 colourways (72 across, 144 down) |
| observatory, wizard | `AE/Addons/_Miscellany/Astronomical_Observatory/Props/Props.png`, `Wizard_Tower/Props/Wizard_Tower_Props.png` | globe, brass telescope, star chart; stack of books, golden orrery, grimoire shelf (3 fills) |
| lunar | `AE/Seasonal_Content/Minifantasy_Lunar_New_Year_Festival_v1.0/Tiles And Props/LunarNewYear_props.png` | lucky cat (4 coats), paper lantern stand |
| dungeon, candle | `Minifantasy_Dungeon_v2.3…/Props/Props.png`, `Props/Animated_Props/Candle.png` | knight statue (4 facings, marble, bluestone, greenstone 56 apart); candle (8 frames) |
| church_rack, church_stand | `AE/Addons/Towns_I_II/Church/Church_Indoor/Candles/Candles.png`, `CandleStand.png` | candle rack, tall candle (4 frames each) |
| ships_lamp | `Minifantasy_Ships And Docks v1.1/…/Tileset/Props/Lamp/Lamp.png` | ship's lamp (8 frames, trimmed to the lamp) |

**Room shells and finishes** (imported whole, sliced by cell like the tavern's room): `Shop_Indoor/Separate_Layers/ShopIndoor_basebuilding/wall/floor.png` (two 17×15-cell premade rooms at (48,128) and (272,128); cells `L_c_r` / `R_c_r`; the right room is the guest room's shell, its door the bottom row's column 8), `TavernIndoor_floor.png` (the pale chequer floor), `CastlesAndStrongholds/IndoorTileset.png` (single repeating cells: blue tiles (24,24), sage tiles (24,72), parquet (48,144)); the tavern's stairs up to the guest room are Medieval City's slim wooden interior flight, `Premade/Premade_Interior/Separate_Layers/Premade_Interior_c-stairs.png` (16,23) 9×17, mirrored (the sheet also has a longer 9×21 flight at (200,75) and two stairwell openings; the Towns II stucco staircase was tried first and replaced as too large). These sheets were imported whole, as the tavern's room layers were.

**Derived art:** none written to disk. Recolours (D11) and the walnut, birch and ash panelling are baked at runtime from the drawn art through palette ramps whose colours come from Minifantasy's own colourways (`Data/Furniture/PaletteLibrary.asset`): woods from the Elven tables (walnut, olive wood, ash, rosewood, birch) and the Dwarven materials (pine, slate black, grey stone); cushions from the Elven rugs (teal, green, plum, amber, rose, gold) and a Towns II rug (navy).

**Gaps found:** no clothed sitting pose (customers still stand at seats); no back-facing art for the charged attack; no floor tiles in the Elven or Dwarven tilesets that repeat as a whole floor (Elven floors are radial tree rings); the Barracks sheet has footlockers, not beds (the survey above guessed beds); the Shop Indoor candles are unlit wax stubs, so the catalogue's candles come from the Dungeon and Church packs.

## Checkpoint C art (4f, 2026-10-05)

**Furnishing discoveries** (curios; catalogue entries with `sources="Discovery"`, generated like the rest of the catalogue):

| Piece | Sheet key | Source |
|---|---|---|
| skull candle (8-frame flame) | skull_candles | `AE/Addons/Dungeon/Sacrifice_Altars/Sacrifice_Candels/Minifantasy_SacrificeCandels_skulls.png` (0,0) 32×32 |
| slime jars, iron cage (shut or open), spider brazier (with a light), tattered banner (ragged or shredded) | dungeon | the Dungeon pack's `Props/Props.png` (the sheet the knight statue already uses) |
| cellar stores, delver's junk heap | piles | `AE/Addons/_Miscellany/Piles_Of_Loot_And_Stuff/PilesOfLootAndStuff.png` (13,55) 23×16 and (11,80) 26×15 |
| cobweb, great cobweb, mimic chest | haunted, chests | already in the catalogue; now found, not bought |

**The Larder Troll's tusks (derived, D22).** `Tools/furniture/trophy.py` writes `Tools/furniture/derived/trophy_larder_troll.png` (26×21), which the catalogue reads through a `derived:` sheet source (a repo path instead of a pack path; `FurnitureCatalog`, `MinifantasyImporter` and `contact.py` all accept it). It is a composite of two Minifantasy drawings:
- the plaque of the Castles and Strongholds mounted antler head (`Minifantasy_CastlesAndStrongholds_v.2.0/…/Props/Props.png`, crop (12,82)–(26,96)): its clean left half mirrored over the stag's head, the middle filled with the plaque's own wood, the gold tab kept;
- a curved horn from Giant Bones (`AE/Addons/Desolate_Desert/Giant_Bones/Bones.png`, crop (73,13)–(96,40)), halved to its tip, re-outlined with a 1 px margin, its bone ramp remapped onto yellowed ivory (five colours, dark to light), mirrored into a pair.
No new colours outside those ramps. Rerun the script to rebuild it.

**Pip and Gunta (derived; both retired 2026-10-06: Gunta became Boog, Pip became Orik, below).** `Tools/characters/staff_looks.py` writes `Tools/characters/derived/{PipIdle,PipWalk,GuntaIdle,GuntaWalk}.png` from the Creatures pack's base humanoids (`Minifantasy_Creatures_v3.3_Commercial_Version/…/Base_Humanoids/`), imported as the `Staff` pack (Idle 16 frames × 4 directions, Walk 4 × 4):
- **Gunta Ashbelly (retired 2026-10-06; the cook is now Boog, below):** the yellow-bearded dwarf, palette-remapped: the grey helmet becomes a white cook's cap, the orange beard deep auburn, the clothes a cook's red (the apron).
- **Pip Marrowby:** the base halfling (one cream ramp throughout), coloured by where each pixel sits: brown curls on the head, the face as drawn, a green waistcoat, brown breeches. Outline and shading ramps kept.
Minifantasy has no dressed halfling, cook or apron layer; these are recolours, not new drawings.

**The kitchen and the market.**
- The Butcher Block is Crafting And Professions II's preparation table: `…/Crafting_Professions/Food_Preparation/Minifantasy_CraftingAndProfessions2PreparationTableProp.png` (idle) and `…PreparationTableWorking.png` (12 frames of the knife at work), 32×32 cells; the table's drawing is (2,11)–(29,26) in its frame.
- Staple icons: `…/Craftable_Item_Icons/Minifantasy_CraftingAndProfessions2PreparationTableIngredients.png` (bread (8,8), onion (16,8), herbs (40,8), mushroom (64,8), steak (8,56) for spider-leg cuts, slices (8,72) for bat-wing cuts); eggs from `AE/Icons/8x8px/Farm_Animal_Product_Icons/FarmAnimalProductIcons.png` (16,40); malt as wheat from `Minifantasy_Farm_v3.0/…/Crops/Minifantasy_FarmSeedsAndCrops.png` (64,72); spore sacs from Crafting And Professions' potion herbs (24,80).
- Dish icons: Brackenford ale is Miscellany's full tankard (0,64); onion broth (56,64), eggs on toast (16,56), crispy bat wings (128,16), spider-leg steaks (112,16) and the bat-wing platter (120,16) from `DishIcons`.
- UI Overhaul: Icons chest (496,24) for curios, book (416,24) for Orik's ledger; Emotions (16 px grid, centre (16c+8, 16r+8)) heart (104,88), happy (8,8), surprised (40,56), content (72,72), frown (88,24), sweat (24,88) for staff and patron beats.

**Special requests (Checkpoint D):** UI Overhaul Icons' sparkle (488,40) on the patron's bubble and the order rail; the met and missed faces are the Emotions heart and frown already imported.

**Sound:** all placeholders (`PH_Discovery`, `PH_Homecoming`, `PH_KnifeIn`, `PH_Cleave`, `PH_ChopRagged`, `PH_ButcherDone`).

**Boog (2026-10-06).** The cook is the **Goblin Sapper** from All Exclusives (`All_Exclusives_20261002/Creatures/Goblin_Sapper`), used as drawn: `Idle.png` (20 frames) and `Run.png` (10 frames, used as his walk), 32×32 frames at 100 ms in Minifantasy's four facings, with `_Shadows/Idle_Shadow.png` and `Run_Shadow.png` as his own shadow. Imported to `Assets/ThirdParty/Minifantasy/GoblinSapper/`; `Dmg`, `Die` and `Only_Bomb` aren't imported. He carries the sapper's lit bomb on his back, which is the look's joke. Gunta's derived sheets stay in `Tools/characters/derived/` as history; their imported copies were removed.

**Orik (2026-10-06).** The server who replaced Pip (stable id `pip`) is the Creatures pack's **yellow-bearded dwarf** (`Base_Humanoids/Dwarf/Dwarf_Yellow_Beard/YellowBeardIdle.png`, `YellowBearWalk.png`: idle 16 frames and walk 4, in four facings, the same layout Pip's sheets had), remapped by `Tools/characters/staff_looks.py` into `Tools/characters/derived/{OrikIdle,OrikWalk}.png`: his ginger hair and beard as drawn, the grey belt brown leather, the brown-red clothes green (the server's colours). Imported as the `Staff` pack's `OrikIdle`/`OrikWalk`; Pip's derived sheets stay in `Tools/characters/derived/` as history, their imported copies removed. (The All Exclusives Dwarven Marksman was the other dressed dwarf; it carries a crossbow.)

**Gaps:** no sitting or eating pose for Boog and Orik; no apron or chef's-hat layer (hence the recolour); no "found" chest that differs from the treasure chest icon.

## Selectors (UI Overhaul → `UIOverhaul/Selectors`)

| Sprite | Rect | What |
|---|---|---|
| `Brackets` | 255,95 18×18 | White corner brackets (a 9-sliced SpriteRenderer drops its top row and right column, so it isn't used) |
| `CornerTL` / `TR` / `BL` / `BR` | 255,95 / 269,95 / 255,109 / 269,109, 4×4 | The bracket corners on their own, tinted gold and placed at a station's corners: the target highlight |
| `Marker` | 84,254 8×5 | A small white down marker, tinted gold, bobbing above the target |

The sheet has the same frames in black, red and green, dashed and dotted, and arrows in four directions.

### Giant Spider web (`GiantSpider/GiantSpiderWeb.png`, from `Minifantasy_GiantSpiderWebProjectiles.png`, 96×96)

One small sprite per direction, cut by measured rectangles (x, y, w, h from the top-left): E 87,46 9×3; NE 86,9 7×7; N 43,0 3×9; NW 3,9 7×7; W 0,46 9×3; SW 3,80 7×7; S 43,82 3×9; SE 86,80 7×7. N and S each have a second, slightly offset copy (50,0 and 50,82), not used.

## UI

| File | Sprites | What |
|---|---|---|
| `UIOverhaul/Bubble.png` (UI Overhaul, `Character_Emotions/Bubble_Only.png`, 280×72) | `Bubble_Body` 31,7 10×10 (9-sliced, 3 px border); `Bubble_Tail` 33,17 4×3 | Speech bubble. The sheet holds the same bubble with its tail on each side, in two sizes. `_Emotions.png` holds faces only (no "!"); not imported. |
| `UIOverhaul/ClassicUI.png` (UI Overhaul, `Classic_Minifantasy_UI/_Classic_UI.png`, 1872×848; built from 16 px pieces) | `ClassicUI_Panel` 64,48 48×48 (9-sliced, 8 px); `ClassicUI_Bar` 64,16 48×16 (9-sliced, 6 px); `ClassicUI_Slot` 561,30 14×17; `ClassicUI_SlotSelected` 593,30 14×17; `ClassicUI_BarTrough` 208,562 48×12; `ClassicUI_BarFillBlue` 596,709 40×6; `ClassicUI_BarFillRed` 340,709 40×6 | Parchment panel with a red inner border (the swap prompt), a pill bar (its button), and a 14×14 slot, plus the same slot with a ▼ marker tab above it for the selected one (selection that doesn't rely on colour). The Essence bar is the dark trough with the blue fill inside it (red when low); the sheet has troughs in four heights and fills in red, blue and yellow, each in four thicknesses. The sheet also has Grim and Stylized versions, scroll and book panels, bars and frames. |
| (buttons) | — | Since 4c step 6, text buttons are a plain parchment face with a one-pixel dark edge (`DungeonUI.ButtonFace`, drawn from a white pixel), because the pill bar's art fought with the text. `ClassicUI_Bar` stays imported. |
| `UIOverhaul/Icons.png` (UI Overhaul, `_General_UI_Resources/Icons/Icons_Only.png`, 576×432) | `Icons_MagicSpark` 488,40 8×8; `Icons_ArrowUp` 32,64, `Icons_ArrowDown` 64,64, `Icons_Swords` 392,40, `Icons_Food` 536,40, `Icons_Lightning` 504,40 (all 8×8, white except the orange lightning) | The **Essence icon**, beside the bar (replaced the "Essence" label, 2026-10-04). Over room exits (4d door previews): ↑ the rope out, ↓ deeper, crossed swords the arena, the food icon an ingredient room, the lightning a power room (also the power spark on the floor, 4d step 4). The sheet holds 8×8 icons "to be used in menus and placed next to relevant overlay elements such as HP bars": general (settings, sound, save, lock, arrows, maths and media symbols) in white plus red, orange, yellow, green, blue and purple copies; a CHARACTER group (map, compass, book, scroll, chest, anvil, needle and thread; body, crossed swords, armour, shield, hammer, backpack, heart, magic spark, lightning, stamina, food, water); and social-media logos. Listed in its `_Info.txt`. |
| `UserInterface/GuiEmoticons.png` (User Interface pack, `Miscellany/Emoticons/Minifantasy_GuiEmoticons.png`, 176×160, 16 px cells) | `GuiEmoticons_AlertRed` 22,116 5×10 | The red "!" shown over an enemy winding up an attack. Rows 7 and 8 (from 0) hold "!", "?" and "X" marks in six colours. The rest of the sheet is faces. Drawn with the **unlit** sprite material so it reads in the dark. |

## Icons

`LootIcons/LootIcons.png` — 200×104, 25×13 cells of 8 px. Layout per the pack's `IconsInfo.txt`.

| Cells | What | Used for |
|---|---|---|
| (5,11) (6,11) | Green slime blob, large and small | (5,11) is the **Slime Gel** icon |
| (7,11) (8,11) | Blue slime blob, large and small | (7,11) is the **Slime Core** icon (placeholder) |
| Column 1 | Orc, goblin, elf, human, dwarf, halfling ears and fingers | — |
| Column 2 | Troll, cyclops, minotaur, yeti, warg parts; slimes in the last row | — |
| Column 3 | Giant spider, bear, snake, wraith, vampire, beholder parts | (13,1) **Spider Leg**, (16,1) **Venom Sac** (the spider's poison); (14,1) fangs and (15,1) eyes unused. (14,9) the vampire's cape stands in for the **Bat Wing**. |

## Biome 1 roster (decided for the pivot; art still to import)

| Enemy | Pack | Status |
|---|---|---|
| Green Slime | Creatures | Imported; leap attack (4b) |
| Bat | Creatures (Beasts) | Imported; sleeps, wakes, swoops (4b) |
| Giant Spider | Exclusive (`Giant_Spider`) | Imported; bite and web (4b). Huntsman Spider and Spider Queen exist too (same sheet set). |
| Larder Troll (boss, 4e) | Exclusive (`Ancient_Troll`) | Imported in 4e step 1 (see below). Replaces the Mother Slime candidate (2026-10-05). |
| Mushroom People | Creatures (exclusive) | Deferred (4b decision): it has idle, jump, damage and die, but **no attack animation**. |

## Kariaston and Tally Ho!'s daytime places (4h Checkpoint A, imported)

Registered in `Editor/Setup/KariastonSheets.cs` (rects in pixels from each image's top-left), imported to `Assets/ThirdParty/Minifantasy/<Pack>/`. Built into `Kariaston.unity` once by `KariastonBuilder` (the blockout is hand-owned after that) and into the tavern by `SurfaceBuilder`.

| Pack folder / file | Source | What we cut |
|---|---|---|
| `ForgottenPlains/PlainsTiles` | Forgotten Plains `Tileset/Minifantasy_ForgottenPlainsTiles.png` | Grass (eight cells: (1–4,1), (2,4), (3,5), (4,6), (2,7)); the dirt-on-grass autotile (cells 7–9 × 3–5: corners and edges, (8,4) solid; inner corners (7,6) NW, (8,6) NE, (7,7) SW, (8,7) SE); the stone square, same layout at cells 12–14. Paths and the beds use dirt, the square stone; regions at least two cells wide. |
| `TownsII/BuildingSamples` | Towns II `Buildings/_Mix_And_Match_Samples/…MoreBuildingSamples.png` | Nine whole premade buildings. Used: **ThatchedHall** (9,264 118×105) = Tally Ho!'s outside; **BlueRoofHall** (13,28 110×93) = Maximo's house; **BrownCottage** (319,160 58×64) = Grim and Ogrin's cottage. The two halls sort at their wings' wall foot (18 px and 17 px above the art's base: the porch steps reach lower), so someone beside the porch draws in front of the wall. |
| `WizardTower/TowerExterior` | Wizard Tower add-on `Exterior/Wizard_Tower_Exterior.png` (64×136) | Kaloren's tower, whole. |
| `CaravansAndWagons/CaravansAndWagons` | Caravans And Wagons add-on `Tileset/CaravansAndWagons.png` | **PaintedWagon** (302,229 28×61: the green wagon from the front, door and steps) = Bart's wagon. |
| `TownMonuments/Monuments` | Town Monuments add-on | Karias's memorial: **Pedestal** (40,16 16×27), the weathered bronze **Figure** (8,201 15×17) on it, a bronze **Plaque** (296,244 16×12) in front. A sorting group keeps them one object. |
| `AnimatedWell/WellStatic` | Animated Well add-on `WellStaticFrames.png` | The first frame (0,0 24×24). |
| `PlantsAndFoliage/PlainsFoliage` | Plants & Foliage `Plains_And_Forests/Forgotten_Plains.png` | Four tree sizes (pivots where the trunk meets the ground) and six bushes/shrubs. |
| `MedievalCity/CityProps` | Medieval City `Props/Props.png` | Bench, long bench, lamp post, two flower boxes, barrel, crate. |
| `Farm/FarmProps`, `Farm/FarmTileset` | Farm `Props`, `Tileset` | Haystack, hay pile, bales, scarecrow, trough, bucket; the low fence run (448,120 24×6) and a post. The tileset's soil ring was tried for the beds and left a grass hole in each: the beds use the plains dirt autotile instead. |
| `Towns/TownsProps` | Towns `Props/Minifantasy_TownsProps.png` | **TankardBoard** (9,86 23×17): Tally Ho!'s standing sign outside; **MenuBoard** (130,92 12×11): the menu board inside the door; **PostSign** (147,94 10×9): the plots' and the closed road's signs; **CupboardJars** (66,244 11×15): the storeroom shelves. |
| `TravellingMerchant/CartOpen`, `CartClosed` | Travelling Merchant `Merchant_On_Cart/Idle_Shop_Open.png`, `Idle_Shop_Closed_No_Merchant.png` | Frame 0 (64×64) of each: the market cart open (its "shop" sign up, counter down) and packed up after five. The Merchant creature's own stall includes its seller, so the cart (with its pony) stands in until Grim keeps the stall (Checkpoint C). |
| `Portraits/PhiFramed` | derived: `Tools/portraits/framed.py` | Phi's portrait (`Tools/portraits/phi.json`: Portrait Generator elf, the purple skin as a drow, long white hair, leather vest), cropped to the face (7,7–25,27) and framed in the colours of Minifantasy's own picture frame (Castles and Strongholds props, the noble portrait); 22×24, hung upstairs. |

**Village layout** (village cells, origin (200, 0), 72×48; `KariastonBuilder`): Tally Ho! north-centre (porch steps at (36, 30.75)); a dirt path down to the stone square (29–42 × 11–22: the memorial, the well, the market cart, benches, lamps); the main road along y 7–9 with spurs up to Maximo's door (west), Grim and Ogrin's (east of the square) and Kaloren's tower (far east); Bart's wagon on the green west of the square; the garden (four 3×2 beds, fences, scarecrow, hay) west of Tally Ho!; three fenced empty plots east of it and south of the road; the road east closed by a fence and a sign; trees round the edges, bushes along the south.

**Gaps noted in 4h A:** no clash-free wall spot in the main room for the five tankards (deferred); no plans sprite chosen (the Decorate key covers it); the cellar hatch isn't shown in the daytime (its look is deferred).

## 4h survey (planning, 2026-10-06)

Inspected for `docs/PLAN_4H.md` §28–30; the sheet `docs/plan_4h/cast_candidates.png` shows the figures at true scale beside the keeper and Orik.

**Figures (approved 2026-10-06):**
- **Maximo (locked):** *Knight Jousting Add-on 1.5* › `Knight On Foot` › `Knight_Idle/Walk/Attack/Dmg/Die_blue.png` (32×32 frames; idle and walk 200 ms, the rest 100 ms). No crown, no King figure.
- **Gimp (locked):** *Modern Soldiers* (All Exclusives › Creatures) › `Idle/Walk/Dmg/Die/Soldiers/*_soldier_headband.png`; its `Shot_Diagonal/Orthogonal` animations fire a rifle (unused until the firearms question is answered, GDD §2.10 question 7). Portrait: a half-elf treatment, not a human recipe.
- **Grim (candidate):** *Miner* (`Minifantasy_MinerIdle/Walk…`), distinct from Orik's yellow-bearded dwarf and the dwarf keeper.
- **Ogrin (provisional):** a child figure, 8 px tall against the keeper's 10: *Snowball Wars Revamped* (`Characters/Separate_Layers`, human with outfit layers) or *Summer Holidays* (`SummerHolidaysHuman1–3`). Chosen at game scale in 4h Step 8.
- **Bart (candidate):** *Wise Orc*; fallback *True Heroes II* Bard (human, with ballad and singing animations) recoloured with an orc skin ramp.
- **Kaloren:** *A Myriad Of NPCs* layers (old human); the *Lich* figure (idle, fly, spellcasts) held for a reveal.
- **Glimmer (current treatment):** *Naughty Fairy* (`Fly_Idle`, `Appear`, `Disappear`).
- **Phi's portrait:** Portrait Generator elf base with the obsidian skin colourway, in a Towns frame.

**Village, garden, market:** *Towns* (tileset, props, hens), *Towns II* (brick, stucco and plank buildings, windmill, bridge), *Medieval City* (props), *Forgotten Plains* and *More Grass Variations*, *Plants & Foliage* (Plains and Forests), *Wall Of Trees*, *Wizard Tower* (Kaloren), *Caravans And Wagons* (Bart), *Animated Well*, *Town Monuments* (Karias's memorial), *Wooden Bridge*, *8x8 Flags*, *Outdoor Lanterns*; *Farm* (`SeedsAndCrops`: pumpkin, eggplant, berry, beet, wheat, tomato, sunflower, corn, rice, lettuce, potato, radish, garlic, cauliflower, pepper; `ActionInProgress` 16×16 animated action icons; tileset), *More Veggies* (carrot, spinach, cucumber, artichoke, cabbage, zucchini, **onion**, green beans, broccoli, celery, asparagus, ginger); the market from *Merchant* (`Stall`, `Stall Setup`, `Stall Pack Back`) or *Travelling Merchant* (shop opening and closing).

**Gaps found (4h):** *Farming Animations*, *Writing Down* and *Sleeping Animations* exist only for the bare race bodies, not the four clothed keeper bodies (workaround: an existing pose plus the Farm action icons); no child parts in the Portrait Generator (Ogrin); no orc playing an instrument (Bart). No unowned pack verified to fill them.

## Known gaps

- **No rat with an attack** (the reason the Giant Rat was replaced).
- **No pixel font in Minifantasy.** Resolved in 4c with **Silver** by Poppy Works (not a Minifantasy asset; `Assets/_Project/Fonts/silver/`, license in `docs/THIRD_PARTY.md`). The 4a look scenes keep the built-in font as baselines.
- **No audio of any kind.** Sounds are generated placeholders (`Assets/_Project/Audio/SFX/PH_*.wav`).
- **No clothed carrying pose.** Carrying Animations (exclusive) has carry idle, walk and damage for six races, but only as unclothed base bodies with the load as a separate layer (wood, planks, ore, ingots; no plates). 4c draws the dish icon above the player's head instead (decision 2).
- **No sitting pose** for customers: seated customers will use their idle pose at the chair.
- **No cauldron or cooking pot in the cooking packs.** The Stew Pot is the Dungeon pack's cauldron over a Dwarven Kingdom floor fire.
- **No clothing or hair layers for attack animations** (A Myriad of NPCs only layers idle, walk, damage and die).
- **No mallet or frying-pan weapon.**
- **No bat parts** on the Loot Icons sheet: the Bat Wing uses the vampire's cape icon.
- **No Shroom Cap or Spore Sac icons** in use: they belong to the Mushroom People (deferred), so their slots show only count, quality and freshness.
- **No rope-climb animation for a clothed body** (only the base bodies): extraction rises and fades.
- **No dungeon gate in the dungeon packs** (searched the catalog for gate, bars, portcullis, grate, door): the room gates borrow the Gladiator Arena gate's bars. A gate drawn for the Cellars' stone would replace them.
- **No doorway art for side or south walls:** room exits are on the north wall only, and the entrance is a plain gap in the south wall.

## Portraits (Minifantasy Portrait Generator, 4g Checkpoint A → `Portraits/`)

**The pack.** `Minifantasy_Portrait_Generator_Graphical_Assets_v1.0/Portrait_Generator/` has every part as a 32×32 layer, per race in `single_images/<race>/` (dwarf, elf, goblin, halfling, human, orc; four skins each, e.g. goblin desert/grassland/swamp/tundra, halfling apple/blueberry/chestnut/wheat): `<race>_<skin>_base.png`, `_ears_<variant>` (10), `_nose_<variant>` (10), `_mouth_<variant>` (12), eyes in `<race>_eyes/<race>_eyes_<skin>_<variant>_1.png` (open) and `_2.png` (blinking), and four talking mouths in `<race>_mouth/<race>_mouth_<skin>_talking_1..4.png`. `single_images/common/` holds the coloured layers: `common_bg_<colour>` (26), `common_brows|hair|beard|moustache_<colour>_<variant>` (10 colours), `common_clothes_<colour>_<variant>` (breastplate, brute, doublet, robe, vest in 19 colours), `common_hat_<colour>_<variant>` (12 hats, e.g. `engineer`: goggles). Also Aseprite sources and per-category sprite sheets (the halfling folder is spelt `halfing` there). The app (`Minifantasy_Portrait_Generator_app_v1.0/app/portrait-generator.html`, by Pixel_Pincher) stacks them in this order: bg, base, eyes, nose, hair, ears, brows, clothes, beard, mouth, moustache, hat; talking cycles the face's own mouth then talking 1–4; blinking swaps the eyes' `_2`; body height lifts every layer but the background.

**The workflow.** A recipe per character, `Tools/portraits/<id>.json` (race, skin, height and the layers: type, variant, colour, the same choices the app offers), composed by `Tools/portraits/compose.py` with the app's own rules into `Tools/portraits/derived/<id>_portrait.png`: six 32×32 frames (still, blink, talking 1–4). `Hearthdelve → Story → Update Story Content` imports the strips (`derived:` sheets, `MinifantasySheets.PortraitSheet`) and points `Data/Story/Portraits/Portrait_<id>.asset` at them; the dialogue box shows them at 2×. To change a face: edit the recipe (`compose.py --preview <race> <skin> <layer> <variants> --out <png> <recipe>` lays a layer's options side by side), compose, update. The app is also fine for exploring: copy its choices into a recipe.

- **Boog** (`gunta.json`): goblin, grassland skin; round eyes, pointy nose, big ears, happy black brows, a toothy grit; a red doublet (the Sapper's red scarf); brown leather engineer goggles pushed up; light khaki background.
- **Orik** (`pip.json`, named by his stable id): dwarf, rosacea skin; happy eyes, a ball nose, normal ears, ginger (`red`) worried brows, short ginger hair, a bushy ginger `lumberjack` beard, a green vest, a smile; light green background. (Until 2026-10-06 the recipe was Pip's: a halfling with brown curls.)

No portrait for the keeper in 4g (decision D4). License: the pack's commercial license allows use and editing in our game; credit Krishna Palacio (art) as for all Minifantasy, and the app is by Pixel_Pincher (`docs/CREDITS.md`).

## The keeper's bodies and the bomb (4g Checkpoint B → `Creatures/`, `GoblinSapper/`)

**Bodies** (Creatures v3.3, `Base_Humanoids`; 32-pixel frames, the Townsfolk's layout: idle 16×4, walk, attack, damage 4×4, charged attack 6×3 stages): imported readable, for recolouring.
- **Human Townsfolk** (the keeper since 4a): its jump is one row (mirrored for the left), its death `SpinDie` (12).
- **Human Amazon** ("warrior" in the creator): four-facing jump (4×4), `Die` (12).
- **Wild Orc** ("orc"): as the Amazon. Every orc in the packs (Base Orc, Wild Orc, the Wise Orc's skin) shares one green.
- **Dwarf Yellow Beard** ("dwarf"): the pack spells its files `YellowBeardIdle` and `YellowBear<Anim>`, and its shadows `ShadowDwarf<Anim>` or `ShadowDwar<Anim>` (walk, jump, spin); the shadow spin has 11 frames to the body's 12. Imported as `DwarfYellowBeard<Anim>` and `ShadowDwarf<Anim>`.

The other base humanoids (elf, goblin, halfling and the unclothed bases) are out: unclothed, or missing the keeper's animations.

**Colour channels** (sampled from each body's idle sheet; `Editor/Generation/KeeperContent.cs`):
- Townsfolk: skin `cdac85 ddb78f eec39a facba6`; hair `49240b 5a2b0c 743810 924614 a3511b`; shirt `404e8f 4757a2 5062b3` (the grey legs stay).
- Amazon: skin as the Townsfolk; hair `a76224 d37631 e99166`; tunic `17461f 2e7238 3d924a`.
- Dwarf: skin `cb9883 dba28e eeae9a fdb8a8`; hair and beard `a25c0f c27117 dd8a2f`; legs `792e0d 9c3820` (the belt stays).
- Orc: skin `2e6d34 36803b 3c8f40 439f43 4fad4b`; hair `660f1b 861626 aa2f41`; loincloth `884524 a0522c b35526`.

**Ramps** (`Data/Characters/Keeper/KeeperPalettes.asset`), all from Minifantasy drawings: skins from A Myriad of NPCs' skin layers (the top four of white, pale, brown and black; named light, pale, tan, brown) and the dwarf's own (rosy); orc skins from the orcs and the goblins (moss green, leaf green); hair from A Myriad of NPCs' hair layers (black, blonde, brown, red, white) and the bodies' own (chestnut, ginger, golden, crimson); clothes from A Myriad of NPCs' tops (red, sky blue, purple, turquoise, magenta, orange, yellow, white) and the bodies' own (navy, forest green, rust, leather). A Myriad of NPCs has more skin tones (elf) and the base races more (halfling); not used yet.

**Boog's bomb**: `All_Exclusives/Creatures/Goblin_Sapper/Only_Bomb.png` (320×96, 32-pixel frames): row 0 is the bomb with its fuse sputtering (10 frames, 100 ms), row 1 the fuse burning down to a flash, row 2 the explosion. Only row 0 is used (the quest object on the floor; its first frame is the harvest feed's icon). Imported as `GoblinSapperBomb`.

**The cellar hatch** on arrival day reuses the Dungeon pack's ladder hole (`Holes`, `Ladder`).

**The bomb's marker** (4g Checkpoint C): the UI Overhaul `Selectors/Marker` arrow (as over a station), tinted the fuse's orange, bobbing over it.

**A future friendly-monster candidate** (4h / Phase 5 note, nothing built): the Mushroom People (Creatures, exclusive) have idle, jump, damage and die but no attack, which ruled them out as enemies in 4b and makes them a natural first non-hostile creature character.

