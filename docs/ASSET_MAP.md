# Asset Map

Which Minifantasy art the game uses, where it comes from, and what each sheet contains. Raw packs live outside the repo in `C:\Dev\Minifantasy`; only the files listed here are imported, into `Assets/ThirdParty/Minifantasy/<Pack>/`.

_Last updated: 2026-10-04 (4d step 1: the room gate)_

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

`HoleEntrances.png` has holes in the floor (three sizes, one with a stake, one with a ladder frame), not imported. `RopeClimbing_<race>.png` (8 frames of 32×32, back view) animates a climb, but only for the pack's bare base bodies; our player is the clothed Townsfolk, so the climb is shown by rising and fading instead.

### The room gate (Gladiator Arena, exclusive add-on → `GladiatorArena/Gate.png`)

Source: `All_Exclusives_20261002/Addons/Towns_I_II/Gladiator_Arena/Tileset/Animated Gate/Gate_open_close.png`, 128×48: 8 frames of 32×24 (4 per row), a portcullis in a sandstone arch. Frame 0 closed, 1–3 the bars sinking, 3 open (only the tips show), 4–7 rising again, 7 closed. Only each frame's **barred interior** is imported (`Gate0`–`Gate7`, 16×15 at frame (8, 9), pivot bottom centre): it fills a two-tile doorway cut through the Cellars' own grey north wall, so the sandstone arch never shows. The bars are copper on a transparent ground. Also in the folder: `Gate_shadows_in_exteriors.png` and `Gate_shadows_towards_indoor.png` (not imported) and `GIFs/Gate.gif`. The Dungeon tileset's own archway (cells 13–15, 5–6) has a one-tile opening, too narrow for these bars.

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

**No tankards or ale glasses** on any icon sheet: the Tap's drinks are potion flasks for now. (The Tavern Indoor `Glasses` row is decor, not an icon.) *More Food Recipes* (exclusive, 104×72) has more skewers, sushi and dishes.

## Fire (Dwarven Kingdom → `DwarvenKingdom/`)

| File | What |
|---|---|
| `FloorFireplace` (128×16, 8 frames of 16×16) | A small floor fire: under the **Stew Pot** cauldron (Dungeon `Props_Cauldron`) |
| `WallFireplace` (192×24, 8 frames of 24×24) | Despite the name, a small flame at the foot of a wall: one on the tavern's back wall, as a wall lamp |

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
| `UIOverhaul/Icons.png` (UI Overhaul, `_General_UI_Resources/Icons/Icons_Only.png`, 576×432) | `Icons_MagicSpark` 488,40 8×8 | The **Essence icon**, beside the bar (replaced the "Essence" label, 2026-10-04). The sheet holds 8×8 icons "to be used in menus and placed next to relevant overlay elements such as HP bars": general (settings, sound, save, lock, arrows, maths and media symbols) in white plus red, orange, yellow, green, blue and purple copies; a CHARACTER group (map, compass, book, scroll, chest, anvil, needle and thread; body, crossed swords, armour, shield, hammer, backpack, heart, magic spark, lightning, stamina, food, water); and social-media logos. Listed in its `_Info.txt`. |
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
| Mother Slime (boss) | Creatures (Slimes, green and blue) | Not imported |
| Mushroom People | Creatures (exclusive) | Deferred (4b decision): it has idle, jump, damage and die, but **no attack animation**. |

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

## Portraits (planned for 4g; not yet inspected)

NPC dialogue portraits will come from the **Minifantasy Portrait Generator** (GDD §8.1). When 4g begins, find it through the catalog, inspect it, and record here the workflow for producing a portrait and every portrait imported, as for any other sheet. Nothing is imported yet.
