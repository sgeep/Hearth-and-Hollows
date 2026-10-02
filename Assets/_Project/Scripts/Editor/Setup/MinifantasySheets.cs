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
        const string k_Emotions = "Minifantasy_UI _Overhaul_v1.0/_Minifantasy_UI_Overhaul_Assets/_General_UI_Resources/Character_Emotions";
        const string k_Loot = "All_Exclusives_20261002/Icons/8x8px/Loot_Icons";
        const string k_Cooker = "Minifantasy_AMyriadOfNPCs_v.1.0/Minifantasy_NPCs_Assets/Premade_NPCs/Cooker";

        public const string Creatures = "Creatures";
        public const string Dungeon = "Dungeon";
        public const string TavernIndoor = "TavernIndoor";
        public const string UIOverhaul = "UIOverhaul";
        public const string LootIcons = "LootIcons";
        public const string MyriadOfNPCs = "AMyriadOfNPCs";

        static readonly Vector2 k_BottomLeft = Vector2.zero;

        public static readonly List<Sheet> All = Build();

        static List<Sheet> Build()
        {
            var sheets = new List<Sheet>();

            // Player (4a look test): the Human Townsfolk from Creatures, with its shadow sheets.
            foreach (string anim in new[] { "Idle", "Walk", "Attack", "Dmg", "Jump", "SpinDie" })
            {
                sheets.Add(Character($"{k_Townsfolk}/HumanTownsfolk{anim}.png", Creatures, $"HumanTownsfolk{anim}"));
                sheets.Add(Character($"{k_Townsfolk}/_Shadows/ShadowHumanoid{anim}.png", Creatures, $"ShadowHumanoid{anim}"));
            }

            // Green slime.
            foreach (string anim in new[] { "Idle", "JumpAttack", "Dmg", "Die" })
                sheets.Add(Character($"{k_Slime}/SlimeGreen{anim}.png", Creatures, $"SlimeGreen{anim}"));
            foreach (string anim in new[] { "Idle", "Jump", "Dmg", "Die" })
                sheets.Add(Character($"{k_Slime}/_Shadows/ShadowSlime{anim}.png", Creatures, $"ShadowSlime{anim}"));

            // Tavern NPC.
            sheets.Add(Character($"{k_Cooker}/Minifantasy_NPCsCookerIdle.png", MyriadOfNPCs, "CookerIdle"));

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
            sheets.Add(Tavern("base_building", new SheetRect("Room", 224, 8, 88, 88, k_BottomLeft)));
            sheets.Add(Tavern("floor2", new SheetRect("Floor", 228, 32, 80, 64, k_BottomLeft)));
            sheets.Add(Tavern("wall", new SheetRect("Wall", 228, 12, 80, 20, k_BottomLeft)));
            sheets.Add(Tavern("shadows", new SheetRect("Shadows", 224, 8, 88, 88, k_BottomLeft)));
            sheets.Add(Tavern("props",
                new SheetRect("Shelves", 252, 16, 48, 14, k_BottomLeft),
                new SheetRect("Sign", 233, 17, 14, 6, k_BottomLeft),
                new SheetRect("Bar", 242, 26, 59, 26, k_BottomLeft),
                new SheetRect("StoolA", 235, 33, 5, 6, k_BottomLeft),
                new SheetRect("StoolB", 235, 40, 5, 6, k_BottomLeft),
                new SheetRect("TableSetA", 232, 58, 24, 22, k_BottomLeft),
                new SheetRect("TableSetB", 280, 58, 24, 22, k_BottomLeft)));
            sheets.Add(Tavern("props2",
                new SheetRect("ShelfGoods", 252, 16, 48, 15, k_BottomLeft),
                new SheetRect("BarTop", 244, 31, 12, 12, k_BottomLeft)));

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

            return sheets;
        }

        static Sheet Character(string source, string pack, string file) =>
            new() { Source = source, Pack = pack, File = file, Mode = SliceMode.Grid, Cell = new Vector2Int(CharacterFrame, CharacterFrame), Pivot = FeetPivot };

        static Sheet Tavern(string layer, params SheetRect[] rects) =>
            new() { Source = $"{k_Tavern}/TavernIndoor_{layer}.png", Pack = TavernIndoor, File = $"TavernIndoor_{layer}", Mode = SliceMode.Rects, Rects = rects };

        public static Sheet Find(string assetPath)
        {
            foreach (Sheet sheet in All) if (sheet.AssetPath == assetPath) return sheet;
            return null;
        }
    }
}
