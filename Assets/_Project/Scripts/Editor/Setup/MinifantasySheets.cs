using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Editor
{
    public enum SliceMode
    {
        /// <summary>The whole image is one sprite.</summary>
        Single,
        /// <summary>Equal cells. Sprites are named <c>File_column_row</c>, with row 0 at the top.</summary>
        Grid,
        /// <summary>Named rectangles, given in pixels from the image's top-left corner.</summary>
        Rects,
    }

    /// <summary>A named sprite cut from a sheet. Coordinates are pixels from the top-left corner, as an image editor shows them.</summary>
    public sealed class SheetRect
    {
        public string Name;
        public RectInt Rect;
        /// <summary>Normalized pivot inside the rectangle.</summary>
        public Vector2 Pivot;
        /// <summary>9-slice border in pixels: left, bottom, right, top.</summary>
        public Vector4 Border;

        public SheetRect(string name, int x, int y, int width, int height, Vector2? pivot = null, Vector4? border = null)
        {
            Name = name;
            Rect = new RectInt(x, y, width, height);
            Pivot = pivot ?? new Vector2(0.5f, 0f);
            Border = border ?? Vector4.zero;
        }
    }

    /// <summary>One image we import: where it comes from, where it goes, and how it is sliced.</summary>
    public sealed class Sheet
    {
        /// <summary>Path under the raw Minifantasy folder (outside the repo).</summary>
        public string Source;
        /// <summary>Folder name under Assets/ThirdParty/Minifantasy.</summary>
        public string Pack;
        /// <summary>File name inside the pack folder, without extension. Also the sprite name prefix.</summary>
        public string File;
        public SliceMode Mode = SliceMode.Single;
        public Vector2Int Cell;
        public Vector2 Pivot = new(0.5f, 0.5f);
        public SheetRect[] Rects;

        public string AssetPath => $"{EditorPaths.Minifantasy}/{Pack}/{File}.png";
    }

    /// <summary>
    /// Every Minifantasy image the project imports. Add a sheet here, run
    /// Hearthdelve → Art → Import Minifantasy, and record what the sheet contains in
    /// docs/ASSET_MAP.md. Only what we use is imported (CLAUDE.md).
    /// </summary>
    public static class MinifantasySheets
    {
        /// <summary>World scale: 8 pixels per unit, 1 unit = 1 tile.</summary>
        public const int PixelsPerUnit = 8;
        public const int Tile = 8;
        public const int CharacterFrame = 32;
        /// <summary>
        /// Characters are drawn in the middle of a 32×32 frame with their feet 13 px above the
        /// bottom edge; the pivot sits there so sprites sort by Y at the feet.
        /// </summary>
        public static readonly Vector2 FeetPivot = new(0.5f, 13f / 32f);

        const string k_Creatures = "Minifantasy_Creatures_v3.3_Commercial_Version/Minifantasy_Creatures_Assets";
        const string k_Townsfolk = k_Creatures + "/Base_Humanoids/Human/Human_Townsfolk";
        const string k_Slime = k_Creatures + "/Slimes/Green_Slime";
        const string k_Dungeon = "Minifantasy_Dungeon_v2.3_Commercial_Version/Minifantasy_Dungeon_Assets";
        const string k_Tavern = "All_Exclusives_20261002/Addons/Towns_I_II/Tavern_Indoor/Separate_Layers";
        const string k_Ropes = "All_Exclusives_20261002/Addons/_Miscellany/Hole_Entrances_And_Ropes";
        const string k_ClassicUI = "Minifantasy_UI _Overhaul_v1.0/_Minifantasy_UI_Overhaul_Assets/Classic_Minifantasy_UI";
        const string k_Emotions = "Minifantasy_UI _Overhaul_v1.0/_Minifantasy_UI_Overhaul_Assets/_General_UI_Resources/Character_Emotions";
        const string k_Loot = "All_Exclusives_20261002/Icons/8x8px/Loot_Icons";
        const string k_Cooker = "Minifantasy_AMyriadOfNPCs_v.1.0/Minifantasy_NPCs_Assets/Premade_NPCs/Cooker";
        const string k_Npcs = "Minifantasy_AMyriadOfNPCs_v.1.0/Minifantasy_NPCs_Assets";
        const string k_Bat = k_Creatures + "/Beasts/Bat";
        const string k_Spider = "All_Exclusives_20261002/Creatures/Giant_Spider";
        const string k_GuiEmoticons = "Minifantasy_UserInterface_v1.0/Minifantasy_Userinterface_Assets/Miscellany/Emoticons";
        const string k_Cooking = "Minifantasy_CraftingAndProfessions2_v1.0/Minifantasy_CraftingAndProfessions2_Assets/Crafting_Professions/Cooking";
        const string k_DishIcons = "Minifantasy_CraftingAndProfessions2_v1.0/Minifantasy_CraftingAndProfessions2_Assets/Craftable_Item_Icons";
        const string k_PotionIcons = "Minifantasy_CraftingAndProfessions_v1.0/Minifantasy_CraftingAndProfessions_Assets/Craftable_Item_Icons";
        const string k_Fireplaces = "Minifantasy_DwarvenKingdom_v1.0/Minifantasy_DwarvenKingdom_Assets/Props/Ilumination";
        const string k_Selectors = "Minifantasy_UI _Overhaul_v1.0/_Minifantasy_UI_Overhaul_Assets/_General_UI_Resources/Selectors";
        const string k_UiIcons = "Minifantasy_UI _Overhaul_v1.0/_Minifantasy_UI_Overhaul_Assets/_General_UI_Resources/Icons";
        const string k_GladiatorGate = "All_Exclusives_20261002/Addons/Towns_I_II/Gladiator_Arena/Tileset/Animated Gate";
        const string k_Miscellany = "All_Exclusives_20261002/Icons/8x8px/_Miscellany_Icons_(Coins, Torches, MMO_UI, etc.)";

        public const string Creatures = "Creatures";
        public const string Dungeon = "Dungeon";
        public const string TavernIndoor = "TavernIndoor";
        public const string UIOverhaul = "UIOverhaul";
        public const string LootIcons = "LootIcons";
        public const string MyriadOfNPCs = "AMyriadOfNPCs";
        public const string GiantSpider = "GiantSpider";
        public const string UserInterface = "UserInterface";
        public const string CraftingAndProfessions = "CraftingAndProfessions";
        public const string DwarvenKingdom = "DwarvenKingdom";
        public const string GladiatorArena = "GladiatorArena";
        public const string MiscellanyIcons = "MiscellanyIcons";

        /// <summary>The 4d room gate: frames 0–3 open it, 4–7 close it.</summary>
        public const int GateFrames = 8;

        /// <summary>
        /// The A Myriad of NPCs layers imported for 4c customers, curated for readability at 320×180:
        /// tops only in colours that stand out against the tavern floor (no greens or browns), few layers.
        /// (category, folder under Body or Head, file kind, variants). Each is imported as Idle and Walk:
        /// <c>Npc{Idle|Walk}_{category}_{kind}_{variant}</c>.
        /// </summary>
        public static readonly (string category, string folder, string kind, string[] variants)[] NpcLayers =
        {
            ("Body", "Human", "Human", new[] { "paleskin", "whiteskin", "brownskin", "blackskin" }),
            ("Body", "Elf", "Elf", new[] { "elfskin", "albinoskin" }),
            ("Top", "Shirt", "Shirt", new[] { "red", "blue", "yellow", "white", "orange" }),
            ("Top", "Doublets", "Doublet", new[] { "purple", "turquoise", "red" }),
            ("Top", "Jacket", "Jacket", new[] { "blue", "magenta" }),
            ("Trousers", "Trousers", "Trousers", new[] { "black", "grey", "blue" }),
            ("Hair", "Short", "Short", new[] { "black", "brown", "blonde", "red", "white" }),
            ("Hair", "PonyTail", "PonyTail", new[] { "black", "brown", "blonde", "red", "white" }),
            ("Hair", "Long", "Long", new[] { "black", "brown", "blonde", "red", "white" }),
            ("Hair", "Bold", "Bold", new[] { "black", "brown", "blonde", "red", "white" }),
            ("Hat", "Hood", "Hood", new[] { "blue", "red", "purple" }),
            ("Hat", "RangerHat", "RangerHat", new[] { "blackleather" }),
            ("Beard", "LongBeard", "LongBeard", new[] { "black", "brown", "blonde", "red", "white" }),
        };

        /// <summary>The imported file name of an NPC layer.</summary>
        public static string NpcFile(string anim, string category, string kind, string variant) => $"Npc{anim}_{category}_{kind}_{variant}";

        static string NpcSource(string anim, string category, string folder, string kind, string variant)
        {
            string a = $"{k_Npcs}/Generic_NPCs/{anim}";
            string prefix = $"Minifantasy_NPCs{anim}";
            return category switch
            {
                "Body" => $"{a}/_Characters/{folder}/{prefix}_{kind}_{variant}.png",
                "Top" or "Trousers" => $"{a}/Body/{folder}/{prefix}_{kind}_{variant}.png",
                // The pack's "Short" hairstyle files have a space before the colour.
                "Hair" => $"{a}/Head/Hairstyles/{folder}/{prefix}_HumanHair_{kind}{(kind == "Short" ? " " : "")}_{variant}.png",
                "Hat" => $"{a}/Head/Hats/{folder}/{prefix}_Hat_{kind}_{variant}.png",
                _ => $"{a}/Head/Facial_Hair/{folder}/{prefix}_FacialHair_{kind}_{variant}.png",
            };
        }

        /// <summary>The premade Tavern Indoor room is an 11×11 grid of 8 px cells starting at this sheet pixel.</summary>
        public static readonly Vector2Int TavernRoomOrigin = new(224, 8);
        /// <summary>Premade-room cells the 4c tavern is stretched from, per layer: (columns, rows). Named <c>Cell_column_row</c>.</summary>
        public static readonly (string layer, int[] columns, int[] rows)[] TavernRoomCells =
        {
            ("base_building", new[] { 0, 2, 5, 10 }, new[] { 0, 1, 2, 5, 9, 10 }),
            ("wall", new[] { 0, 2, 10 }, new[] { 0, 1, 2 }),
            ("floor2", new[] { 0, 2, 10 }, new[] { 5 }),
        };

        static readonly Vector2 k_BottomLeft = Vector2.zero;
        static readonly Vector2 k_Centre = new(0.5f, 0.5f);

        public static readonly List<Sheet> All = Build();

        static List<Sheet> Build()
        {
            var sheets = new List<Sheet>();

            // Player (4a look test): the Human Townsfolk from Creatures, with its shadow sheets.
            foreach (string anim in new[] { "Idle", "Walk", "Attack", "Dmg", "Jump", "SpinDie", "ChargedAttack" })
            {
                sheets.Add(Character($"{k_Townsfolk}/HumanTownsfolk{anim}.png", Creatures, $"HumanTownsfolk{anim}"));
                sheets.Add(Character($"{k_Townsfolk}/_Shadows/ShadowHumanoid{anim}.png", Creatures, $"ShadowHumanoid{anim}"));
            }

            // Green slime.
            foreach (string anim in new[] { "Idle", "JumpAttack", "Dmg", "Die" })
                sheets.Add(Character($"{k_Slime}/SlimeGreen{anim}.png", Creatures, $"SlimeGreen{anim}"));
            foreach (string anim in new[] { "Idle", "Jump", "Dmg", "Die" })
                sheets.Add(Character($"{k_Slime}/_Shadows/ShadowSlime{anim}.png", Creatures, $"ShadowSlime{anim}"));

            // Bat (Creatures, Beasts): rows are the four facings; BatSleep's rows are sleep, wake up, fall asleep.
            foreach (string anim in new[] { "FlyIdle", "Attack", "Dmg", "Die", "Sleep" })
                sheets.Add(Character($"{k_Bat}/Bat{anim}.png", Creatures, $"Bat{anim}"));
            foreach (string anim in new[] { "Fly", "Attack", "Dmg", "Die", "Sleep" })
                sheets.Add(Character($"{k_Bat}/_Shadows/ShadowBat{anim}.png", Creatures, $"ShadowBat{anim}"));

            // Giant Spider (exclusive). Its web shot uses the diagonal sheet: our characters face four diagonals.
            foreach (string anim in new[] { "Idle", "Walk", "Attack", "Dmg", "Die", "ShotWebDiagonal" })
                sheets.Add(Character($"{k_Spider}/Minifantasy_GiantSpider{anim}.png", GiantSpider, $"GiantSpider{anim}"));
            foreach (string anim in new[] { "Idle", "Walk", "Attack", "Dmg", "Die", "WebShot" })
                sheets.Add(Character($"{k_Spider}/Shadows/Minifantasy_GiantSpider{anim}Shadow.png", GiantSpider, $"GiantSpider{anim}Shadow"));
            // The web projectile, drawn once per direction (rectangles measured from the sheet).
            sheets.Add(new Sheet
            {
                Source = $"{k_Spider}/Minifantasy_GiantSpiderWebProjectiles.png", Pack = GiantSpider, File = "GiantSpiderWeb", Mode = SliceMode.Rects,
                Rects = new[]
                {
                    new SheetRect("E", 87, 46, 9, 3, k_Centre), new SheetRect("NE", 86, 9, 7, 7, k_Centre),
                    new SheetRect("N", 43, 0, 3, 9, k_Centre), new SheetRect("NW", 3, 9, 7, 7, k_Centre),
                    new SheetRect("W", 0, 46, 9, 3, k_Centre), new SheetRect("SW", 3, 80, 7, 7, k_Centre),
                    new SheetRect("S", 43, 82, 3, 9, k_Centre), new SheetRect("SE", 86, 80, 7, 7, k_Centre),
                },
            });

            // The way out: a rope hanging from a hole in the ceiling, with its coil's shadow (Hole Entrances And Ropes).
            sheets.Add(new Sheet { Source = $"{k_Ropes}/Ropes.png", Pack = Dungeon, File = "Ropes", Mode = SliceMode.Rects, Rects = new[] { new SheetRect("Hanging", 12, 8, 9, 27) } });
            sheets.Add(new Sheet { Source = $"{k_Ropes}/RopesShadows.png", Pack = Dungeon, File = "RopesShadows", Mode = SliceMode.Rects, Rects = new[] { new SheetRect("Hanging", 11, 30, 7, 5) } });
            // The way down to the next floor (4d): the hole with a wooden ladder frame, pivoted at its centre.
            sheets.Add(new Sheet { Source = $"{k_Ropes}/HoleEntrances.png", Pack = Dungeon, File = "Holes", Mode = SliceMode.Rects, Rects = new[] { new SheetRect("Ladder", 177, 14, 15, 11, new Vector2(0.5f, 0.5f)) } });

            // Tavern NPC.
            sheets.Add(Character($"{k_Cooker}/Minifantasy_NPCsCookerIdle.png", MyriadOfNPCs, "CookerIdle"));

            // 4c customers: the curated layers, idle and walking, and the NPC shadow.
            foreach (string anim in new[] { "Idle", "Walk" })
            {
                foreach (var (category, folder, kind, variants) in NpcLayers)
                foreach (string variant in variants)
                    sheets.Add(Character(NpcSource(anim, category, folder, kind, variant), MyriadOfNPCs, NpcFile(anim, category, kind, variant)));
                sheets.Add(Character($"{k_Npcs}/Shadows/Minifantasy_NPCsShadowHumanoid{anim}.png", MyriadOfNPCs, $"NpcShadow{anim}"));
                // Pip's stand-in until 4f: the premade Butcher (apron, bright blonde hair).
                sheets.Add(Character($"{k_Npcs}/Premade_NPCs/Butcher/Minifantasy_NPCsButcher{anim}.png", MyriadOfNPCs, $"Butcher{anim}"));
            }
            // Emotes for speech bubbles (8×8 faces on a 16 px grid): reading the menu, and walking out.
            sheets.Add(new Sheet
            {
                Source = $"{k_Emotions}/_Emotions.png", Pack = UIOverhaul, File = "Emotions", Mode = SliceMode.Rects,
                Rects = new[] { new SheetRect("Thinking", 136, 88, 8, 8, k_Centre), new SheetRect("Angry", 72, 40, 8, 8, k_Centre) },
            });

            // Dungeon room.
            sheets.Add(new Sheet { Source = $"{k_Dungeon}/Tileset/Tileset.png", Pack = Dungeon, File = "Tileset", Mode = SliceMode.Grid, Cell = new Vector2Int(Tile, Tile) });
            sheets.Add(new Sheet
            {
                Source = $"{k_Dungeon}/Props/Props.png", Pack = Dungeon, File = "Props", Mode = SliceMode.Rects,
                Rects = new[]
                {
                    new SheetRect("Table", 56, 8, 16, 8),
                    new SheetRect("Crate", 104, 8, 8, 8),
                    new SheetRect("Barrel", 200, 24, 8, 8),
                    new SheetRect("BarrelOpen", 216, 24, 8, 8),
                    new SheetRect("Cauldron", 202, 43, 12, 11),
                    new SheetRect("Statue", 8, 58, 8, 12),
                },
            });
            sheets.Add(new Sheet { Source = $"{k_Dungeon}/Props/Animated_Props/Torch.png", Pack = Dungeon, File = "Torch", Mode = SliceMode.Grid, Cell = new Vector2Int(16, 24), Pivot = new Vector2(0.5f, 0f) });

            // Tavern corner: the premade room from the Tavern Indoor add-on, layer by layer.
            sheets.Add(Tavern("base_building", WithRoomCells("base_building", new SheetRect("Room", 224, 8, 88, 88, k_BottomLeft))));
            sheets.Add(Tavern("floor2", WithRoomCells("floor2", new SheetRect("Floor", 228, 32, 80, 64, k_BottomLeft))));
            sheets.Add(Tavern("wall", WithRoomCells("wall", new SheetRect("Wall", 228, 12, 80, 20, k_BottomLeft))));
            sheets.Add(Tavern("shadows", new[] { new SheetRect("Shadows", 224, 8, 88, 88, k_BottomLeft) }));
            sheets.Add(Tavern("props", new[]
            {
                new SheetRect("Shelves", 252, 16, 48, 14, k_BottomLeft),
                new SheetRect("Sign", 233, 17, 14, 6, k_BottomLeft),
                new SheetRect("Bar", 242, 26, 59, 26, k_BottomLeft),
                new SheetRect("StoolA", 235, 33, 5, 6, k_BottomLeft),
                new SheetRect("StoolB", 235, 40, 5, 6, k_BottomLeft),
                new SheetRect("TableSetA", 232, 58, 24, 22, k_BottomLeft),
                new SheetRect("TableSetB", 280, 58, 24, 22, k_BottomLeft),
                // 4c: the separate furniture on the left half of the sheet (measured; docs/ASSET_MAP.md).
                new SheetRect("TableRoundA", 90, 42, 12, 12),
                new SheetRect("TableRoundB", 106, 42, 12, 12),
                new SheetRect("TableRoundSmallA", 88, 24, 8, 8),
                new SheetRect("TableRoundSmallB", 104, 24, 8, 8),
                new SheetRect("TableSquare", 50, 25, 20, 23),
                new SheetRect("LongTableH", 130, 40, 28, 8),
                new SheetRect("LongTableV", 169, 34, 6, 22),
                new SheetRect("BenchBack", 51, 17, 18, 7),
                new SheetRect("BenchFront", 51, 49, 18, 7),
                new SheetRect("BenchLeft", 42, 26, 5, 22),
                new SheetRect("BenchRight", 73, 26, 5, 22),
                // Chairs by the way a sitter faces: N shows the backrest in front, S the seat, E and W in profile.
                new SheetRect("ChairFacingN", 137, 26, 6, 6),
                new SheetRect("ChairFacingS", 145, 24, 6, 8),
                new SheetRect("ChairFacingE", 153, 24, 6, 8),
                new SheetRect("ChairFacingW", 161, 24, 6, 8),
                new SheetRect("StoolRedA", 178, 25, 5, 6),
                new SheetRect("StoolRedB", 185, 25, 5, 6),
                new SheetRect("StoolPlain", 194, 25, 4, 6),
                new SheetRect("ShelfTall", 44, 64, 16, 14, k_BottomLeft),
                new SheetRect("ShelfLow", 68, 72, 16, 6, k_BottomLeft),
                new SheetRect("SignSmall", 185, 65, 14, 6, k_BottomLeft),
            }));
            sheets.Add(Tavern("props2", new[]
            {
                new SheetRect("ShelfGoods", 252, 16, 48, 15, k_BottomLeft),
                new SheetRect("BarTop", 244, 31, 12, 12, k_BottomLeft),
                new SheetRect("Taps", 193, 40, 7, 5),
                new SheetRect("BottlesA", 93, 66, 13, 5, k_BottomLeft),
                new SheetRect("BottlesB", 117, 66, 14, 5, k_BottomLeft),
                new SheetRect("BottlesC", 141, 66, 13, 5, k_BottomLeft),
                new SheetRect("Glasses", 165, 66, 14, 4, k_BottomLeft),
            }));

            // 4c kitchen (Crafting And Professions II): a stone oven and a range with pans, one 32×32 frame.
            // The idle prop, and the 8-frame working loop (fire, sizzling pans, smoke) for the Grill in use.
            sheets.Add(new Sheet { Source = $"{k_Cooking}/Minifantasy_CraftingAndProfessions2KitchenProp.png", Pack = CraftingAndProfessions, File = "Kitchen", Pivot = k_BottomLeft });
            sheets.Add(new Sheet { Source = $"{k_Cooking}/Minifantasy_CraftingAndProfessions2KitchenPropShadow.png", Pack = CraftingAndProfessions, File = "KitchenShadow", Pivot = k_BottomLeft });
            sheets.Add(new Sheet { Source = $"{k_Cooking}/Minifantasy_CraftingAndProfessions2KitchenWorking.png", Pack = CraftingAndProfessions, File = "KitchenWorking", Mode = SliceMode.Grid, Cell = new Vector2Int(32, 32), Pivot = k_BottomLeft });
            // 4c dish icons (8×8, on an 8 px grid): grilled skewers and a drumstick, soup bowls (Crafting And
            // Professions II recipes), and potion flasks for the Tap's drinks (Crafting And Professions I).
            sheets.Add(new Sheet
            {
                Source = $"{k_DishIcons}/Minifantasy_CraftingAndProfessions2Recipes.png", Pack = CraftingAndProfessions, File = "DishIcons", Mode = SliceMode.Rects,
                Rects = new[]
                {
                    new SheetRect("MeatSkewer", 64, 8, 8, 8, k_Centre),
                    new SheetRect("GreenSkewer", 96, 8, 8, 8, k_Centre),
                    new SheetRect("Drumstick", 136, 8, 8, 8, k_Centre),
                    new SheetRect("BrownStew", 40, 64, 8, 8, k_Centre),
                    new SheetRect("RedStew", 64, 64, 8, 8, k_Centre),
                },
            });
            sheets.Add(new Sheet
            {
                Source = $"{k_PotionIcons}/Minifantasy_CraftingAndProfessionsPotionIcons.png", Pack = CraftingAndProfessions, File = "PotionIcons", Mode = SliceMode.Rects,
                Rects = new[] { new SheetRect("BlueFlask", 72, 40, 8, 8, k_Centre), new SheetRect("GreenFlask", 72, 56, 8, 8, k_Centre) },
            });

            // Fire (Dwarven Kingdom): a floor fire under the stew cauldron, and a wall fireplace.
            sheets.Add(new Sheet { Source = $"{k_Fireplaces}/FloorFireplace/FloorFireplace.png", Pack = DwarvenKingdom, File = "FloorFireplace", Mode = SliceMode.Grid, Cell = new Vector2Int(16, 16), Pivot = new Vector2(0.5f, 0f) });
            sheets.Add(new Sheet { Source = $"{k_Fireplaces}/WallFireplace/WallFireplace.png", Pack = DwarvenKingdom, File = "WallFireplace", Mode = SliceMode.Grid, Cell = new Vector2Int(24, 24), Pivot = new Vector2(0.5f, 0f) });
            // The target highlight (UI Overhaul selectors): a corner-bracket frame (its corners cut separately) and a small down marker.
            sheets.Add(new Sheet
            {
                Source = $"{k_Selectors}/_Selectors.png", Pack = UIOverhaul, File = "Selectors", Mode = SliceMode.Rects,
                Rects = new[]
                {
                    new SheetRect("Brackets", 255, 95, 18, 18, k_Centre, new Vector4(5, 5, 5, 5)),
                    new SheetRect("Marker", 84, 254, 8, 5, new Vector2(0.5f, 0f)),
                    // The frame's four corners on their own, each pivoted at its outer corner.
                    new SheetRect("CornerTL", 255, 95, 4, 4, new Vector2(0f, 1f)),
                    new SheetRect("CornerTR", 269, 95, 4, 4, new Vector2(1f, 1f)),
                    new SheetRect("CornerBL", 255, 109, 4, 4, new Vector2(0f, 0f)),
                    new SheetRect("CornerBR", 269, 109, 4, 4, new Vector2(1f, 0f)),
                },
            });

            // The room gate (4d): the Gladiator Arena animated gate's barred interior only (16×15 of each 32×24 frame), so it
            // fits a two-tile doorway in the Cellars' own grey wall; its sandstone arch is left out.
            var gate = new SheetRect[GateFrames];
            for (int i = 0; i < GateFrames; i++)
                gate[i] = new SheetRect($"Gate{i}", i % 4 * 32 + 8, i / 4 * 24 + 9, 16, 15, new Vector2(0.5f, 0f));
            sheets.Add(new Sheet { Source = $"{k_GladiatorGate}/Gate_open_close.png", Pack = GladiatorArena, File = "Gate", Mode = SliceMode.Rects, Rects = gate });

            // UI Overhaul's 8×8 icons, "to be placed next to overlay elements such as HP bars": the magic spark marks
            // the Essence bar (CHARACTER group, 8th icon of the second row).
            sheets.Add(new Sheet
            {
                Source = $"{k_UiIcons}/Icons_Only.png", Pack = UIOverhaul, File = "Icons", Mode = SliceMode.Rects,
                Rects = new[]
                {
                    new SheetRect("MagicSpark", 488, 40, 8, 8, k_Centre),
                    // 4d step 2's temporary route markers over room exits: out (up), deeper (down), the arena (crossed swords).
                    new SheetRect("ArrowUp", 32, 64, 8, 8, k_Centre),
                    new SheetRect("ArrowDown", 64, 64, 8, 8, k_Centre),
                    new SheetRect("Swords", 392, 40, 8, 8, k_Centre),
                    // 4d step 3: an ingredient room's door sign (CHARACTER group, food).
                    new SheetRect("Food", 536, 40, 8, 8, k_Centre),
                },
            });

            // Miscellany Icons (coins, torches, MMO UI): the big gold coin, for run Gold (4d step 3).
            sheets.Add(new Sheet
            {
                Source = $"{k_Miscellany}/Miscellany_1.png", Pack = MiscellanyIcons, File = "Miscellany", Mode = SliceMode.Rects,
                Rects = new[] { new SheetRect("GoldCoin", 0, 0, 8, 8, k_Centre) },
            });

            // UI and icons.
            sheets.Add(new Sheet
            {
                Source = $"{k_Emotions}/Bubble_Only.png", Pack = UIOverhaul, File = "Bubble", Mode = SliceMode.Rects,
                Rects = new[]
                {
                    new SheetRect("Body", 31, 7, 10, 10, new Vector2(0.5f, 0.5f), new Vector4(3, 3, 3, 3)),
                    new SheetRect("Tail", 33, 17, 4, 3, new Vector2(0.5f, 1f)),
                },
            });
            sheets.Add(new Sheet { Source = $"{k_Loot}/LootIcons.png", Pack = LootIcons, File = "LootIcons", Mode = SliceMode.Grid, Cell = new Vector2Int(Tile, Tile) });
            // Panels and slots (UI Overhaul, Classic style): the swap prompt, and later the HUD and death screen.
            sheets.Add(new Sheet
            {
                Source = $"{k_ClassicUI}/_Classic_UI.png", Pack = UIOverhaul, File = "ClassicUI", Mode = SliceMode.Rects,
                Rects = new[]
                {
                    new SheetRect("Panel", 64, 48, 48, 48, k_Centre, new Vector4(8, 8, 8, 8)),
                    new SheetRect("Bar", 64, 16, 48, 16, k_Centre, new Vector4(6, 6, 6, 6)),
                    // A 14×14 slot, and the same slot with a marker tab on top for the selected one.
                    new SheetRect("Slot", 561, 30, 14, 17),
                    new SheetRect("SlotSelected", 593, 30, 14, 17),
                    // A bar's dark trough, and the fill that sits inside it (blue, and red for low).
                    new SheetRect("BarTrough", 208, 562, 48, 12, k_Centre),
                    new SheetRect("BarFillBlue", 596, 709, 40, 6, k_Centre),
                    new SheetRect("BarFillRed", 340, 709, 40, 6, k_Centre),
                },
            });
            // The red "!" over an enemy winding up an attack (User Interface pack, GUI emoticons; 16 px cells).
            sheets.Add(new Sheet
            {
                Source = $"{k_GuiEmoticons}/Minifantasy_GuiEmoticons.png", Pack = UserInterface, File = "GuiEmoticons", Mode = SliceMode.Rects,
                Rects = new[] { new SheetRect("AlertRed", 22, 116, 5, 10) },
            });

            return sheets;
        }

        /// <summary>Adds the premade room's cells used by the 4c tavern to a Tavern Indoor layer's rects.</summary>
        static SheetRect[] WithRoomCells(string layer, params SheetRect[] rects)
        {
            var all = new List<SheetRect>(rects);
            foreach (var (cellLayer, columns, rows) in TavernRoomCells)
            {
                if (cellLayer != layer) continue;
                foreach (int c in columns)
                foreach (int r in rows)
                    all.Add(new SheetRect($"Cell_{c}_{r}", TavernRoomOrigin.x + c * Tile, TavernRoomOrigin.y + r * Tile, Tile, Tile, k_Centre));
            }
            return all.ToArray();
        }

        static Sheet Character(string source, string pack, string file) =>
            new() { Source = source, Pack = pack, File = file, Mode = SliceMode.Grid, Cell = new Vector2Int(CharacterFrame, CharacterFrame), Pivot = FeetPivot };

        static Sheet Tavern(string layer, SheetRect[] rects) =>
            new() { Source = $"{k_Tavern}/TavernIndoor_{layer}.png", Pack = TavernIndoor, File = $"TavernIndoor_{layer}", Mode = SliceMode.Rects, Rects = rects };

        public static Sheet Find(string assetPath)
        {
            foreach (Sheet sheet in All) if (sheet.AssetPath == assetPath) return sheet;
            return null;
        }
    }
}
