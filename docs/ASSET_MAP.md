# Asset Map

Which Minifantasy art the game uses, where it comes from, and what each sheet contains. Raw packs live outside the repo in `C:\Dev\Minifantasy`; only the files listed here are imported, into `Assets/ThirdParty/Minifantasy/<Pack>/`.

_Last updated: 2026-10-02 (4b step 2: enemies and heavy attack)_

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

The prop-sheet half (individual tables, chairs, stools, benches, shelf units, bottle rows) is not sliced yet. It is what 4c and 4f need for free furniture placement.

### Giant Spider web (`GiantSpider/GiantSpiderWeb.png`, from `Minifantasy_GiantSpiderWebProjectiles.png`, 96×96)

One small sprite per direction, cut by measured rectangles (x, y, w, h from the top-left): E 87,46 9×3; NE 86,9 7×7; N 43,0 3×9; NW 3,9 7×7; W 0,46 9×3; SW 3,80 7×7; S 43,82 3×9; SE 86,80 7×7. N and S each have a second, slightly offset copy (50,0 and 50,82), not used.

## UI

| File | Sprites | What |
|---|---|---|
| `UIOverhaul/Bubble.png` (UI Overhaul, `Character_Emotions/Bubble_Only.png`, 280×72) | `Bubble_Body` 31,7 10×10 (9-sliced, 3 px border); `Bubble_Tail` 33,17 4×3 | Speech bubble. The sheet holds the same bubble with its tail on each side, in two sizes. `_Emotions.png` holds faces only (no "!"); not imported. |
| `UserInterface/GuiEmoticons.png` (User Interface pack, `Miscellany/Emoticons/Minifantasy_GuiEmoticons.png`, 176×160, 16 px cells) | `GuiEmoticons_AlertRed` 22,116 5×10 | The red "!" shown over an enemy winding up an attack. Rows 7 and 8 (from 0) hold "!", "?" and "X" marks in six colours. The rest of the sheet is faces. Drawn with the **unlit** sprite material so it reads in the dark. |

## Icons

`LootIcons/LootIcons.png` — 200×104, 25×13 cells of 8 px. Layout per the pack's `IconsInfo.txt`.

| Cells | What | Used for |
|---|---|---|
| (5,11) (6,11) | Green slime blob, large and small | (5,11) is the **Slime Gel** icon |
| (7,11) (8,11) | Blue slime blob, large and small | (7,11) is the **Slime Core** icon (placeholder) |
| Column 1 | Orc, goblin, elf, human, dwarf, halfling ears and fingers | — |
| Column 2 | Troll, cyclops, minotaur, yeti, warg parts; slimes in the last row | — |
| Column 3 | Giant spider, bear, snake, wraith, vampire, beholder parts | Giant Spider parts for Biome 1 |

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
- **No pixel font.** Text uses Unity's built-in font through Super Text Mesh. It is sharp but not pixel art.
- **No audio of any kind.** Sounds are generated placeholders (`Assets/_Project/Audio/SFX/PH_*.wav`).
- **No plate-carrying overlay** for the player. The exclusive "Carrying Animations" add-on (107 catalog entries) is worth checking for 4c.
- **No clothing or hair layers for attack animations** (A Myriad of NPCs only layers idle, walk, damage and die).
- **No mallet or frying-pan weapon.**
