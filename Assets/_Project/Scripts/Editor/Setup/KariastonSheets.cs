using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// The Minifantasy sheets Kariaston is built from (4h Checkpoint A). Rectangles are pixels from each image's top-left
    /// corner, found with the catalogue and checked on the sheets (recorded in docs/ASSET_MAP.md, "Kariaston").
    /// </summary>
    public static class KariastonSheets
    {
        public const string PlainsPack = "ForgottenPlains";
        public const string TownsIIPack = "TownsII";
        public const string WizardTowerPack = "WizardTower";
        public const string WagonsPack = "CaravansAndWagons";
        public const string MonumentsPack = "TownMonuments";
        public const string WellPack = "AnimatedWell";
        public const string FoliagePack = "PlantsAndFoliage";
        public const string FarmPack = "Farm";
        public const string MerchantPack = "TravellingMerchant";

        public const string Tiles = "PlainsTiles";
        public const string Buildings = "BuildingSamples";
        public const string Tower = "TowerExterior";
        public const string Wagons = "CaravansAndWagons";
        public const string Monuments = "Monuments";
        public const string Well = "WellStatic";
        public const string Foliage = "PlainsFoliage";
        public const string CityProps = "CityProps";
        public const string FarmProps = "FarmProps";
        public const string FarmTiles = "FarmTileset";
        public const string TownsPack = "Towns";
        public const string TownsProps = "TownsProps";
        public const string CartOpen = "CartOpen";
        public const string CartClosed = "CartClosed";

        const string k_Plains = "Minifantasy_ForgottenPlains_v3.6_Commercial_Version/Minifantasy_ForgottenPlains_Assets/Tileset/Minifantasy_ForgottenPlainsTiles.png";
        const string k_Buildings = "Minifantasy_Towns2_v1.5/Minifantasy_Towns2_Assets/Buildings/_Mix_And_Match_Samples/Minifantasy_TownsIIMoreBuildingSamples.png";
        const string k_Tower = "All_Exclusives_20261002/Addons/_Miscellany/Wizard_Tower/Exterior/Wizard_Tower_Exterior.png";
        const string k_Wagons = "All_Exclusives_20261002/Addons/Medieval_Carnival/Caravans_And_Wagons/Tileset/CaravansAndWagons.png";
        const string k_Monuments = "All_Exclusives_20261002/Addons/Towns_I_II/Town_Monuments/Monuments.png";
        const string k_Well = "All_Exclusives_20261002/Addons/Towns_I_II/Animated_Well/WellStaticFrames.png";
        const string k_Foliage = "Minifantasy_Plants_&_Foliage_v1.0/Minifantasy_Plants_&_Foliage_Assets/Plains_And_Forests/Forgotten_Plains.png";
        const string k_CityProps = "Minifantasy_Medieval_City_v1.1/Minifantasy_Medieval_City_Assets/Props/Props.png";
        const string k_FarmProps = "Minifantasy_Farm_v3.0/Minifantasy_Farm_Assets/Props/Minifantasy_FarmProps.png";
        const string k_FarmTiles = "Minifantasy_Farm_v3.0/Minifantasy_Farm_Assets/Tileset/Minifantasy_FarmTileset.png";
        const string k_TownsProps = "Minifantasy_Towns_v3.0/Minifantasy_Towns_Assets/Props/Minifantasy_TownsProps.png";
        const string k_Cart = "All_Exclusives_20261002/Creatures/Travelling_Merchant/Merchant_On_Cart";

        static readonly Vector2 k_Centre = new(0.5f, 0.5f);
        static readonly Vector2 k_Bottom = new(0.5f, 0f);

        /// <summary>The ground's autotile parts, in Forgotten Plains' tile cells: name → (column, row).</summary>
        public static readonly (string name, int column, int row)[] GroundCells =
        {
            ("Grass0", 1, 1), ("Grass1", 2, 1), ("Grass2", 3, 1), ("Grass3", 4, 1), ("Grass4", 2, 4), ("Grass5", 3, 5), ("Grass6", 4, 6), ("Grass7", 2, 7),
            ("Dirt_TL", 7, 3), ("Dirt_T", 8, 3), ("Dirt_TR", 9, 3), ("Dirt_L", 7, 4), ("Dirt_C", 8, 4), ("Dirt_R", 9, 4),
            ("Dirt_BL", 7, 5), ("Dirt_B", 8, 5), ("Dirt_BR", 9, 5), ("Dirt_InNW", 7, 6), ("Dirt_InNE", 8, 6), ("Dirt_InSW", 7, 7), ("Dirt_InSE", 8, 7),
            ("Stone_TL", 12, 3), ("Stone_T", 13, 3), ("Stone_TR", 14, 3), ("Stone_L", 12, 4), ("Stone_C", 13, 4), ("Stone_R", 14, 4),
            ("Stone_BL", 12, 5), ("Stone_B", 13, 5), ("Stone_BR", 14, 5), ("Stone_InNW", 12, 6), ("Stone_InNE", 13, 6), ("Stone_InSW", 12, 7), ("Stone_InSE", 13, 7),
        };

        /// <summary>
        /// The lake's two animation frames (grass-banked), laid out exactly like the dirt autotile 18 and 22 columns to its right:
        /// "Water_TL" … for frame one, "Water2_TL" … for frame two.
        /// </summary>
        public static IEnumerable<(string name, int column, int row)> WaterCells() =>
            GroundCells.Where(g => g.name.StartsWith("Dirt_"))
                .SelectMany(g => new[] { ("Water" + g.name.Substring(4), g.column + 18, g.row), ("Water2" + g.name.Substring(4), g.column + 22, g.row) });

        public static IEnumerable<Sheet> Sheets()
        {
            var ground = new List<SheetRect>();
            foreach (var (name, c, r) in GroundCells.Concat(WaterCells())) ground.Add(new SheetRect(name, c * 8, r * 8, 8, 8, k_Centre));
            yield return new Sheet { Source = k_Plains, Pack = PlainsPack, File = Tiles, Mode = SliceMode.Rects, Rects = ground.ToArray() };

            // Towns II's premade buildings, whole (each a finished exterior with its door at the bottom). The two halls sort at the
            // foot of their wings' walls (their porch steps reach lower): someone standing beside the porch draws in front of the wall.
            yield return new Sheet
            {
                Source = k_Buildings, Pack = TownsIIPack, File = Buildings, Mode = SliceMode.Rects,
                Rects = new[]
                {
                    new SheetRect("StoneCross", 149, 12, 142, 108, k_Bottom),
                    new SheetRect("BlueRoofHall", 13, 28, 110, 93, new Vector2(0.5f, 17f / 93f)),
                    new SheetRect("PurpleCottage", 317, 44, 62, 60, k_Bottom),
                    new SheetRect("BrownStuccoHall", 15, 144, 106, 97, k_Bottom),
                    new SheetRect("BrownStoneHall", 151, 144, 138, 96, k_Bottom),
                    new SheetRect("BrownCottage", 319, 160, 58, 64, k_Bottom),
                    new SheetRect("ThatchedHall", 9, 264, 118, 105, new Vector2(0.5f, 18f / 105f)),
                    new SheetRect("ThatchedStoneHall", 145, 256, 150, 112, k_Bottom),
                    new SheetRect("ThatchedCottage", 313, 264, 70, 88, k_Bottom),
                },
            };
            yield return new Sheet { Source = k_Tower, Pack = WizardTowerPack, File = Tower, Mode = SliceMode.Single, Pivot = k_Bottom };
            yield return new Sheet { Source = k_Wagons, Pack = WagonsPack, File = Wagons, Mode = SliceMode.Rects, Rects = new[] { new SheetRect("PaintedWagon", 302, 229, 28, 61, k_Bottom) } };
            yield return new Sheet
            {
                Source = k_Monuments, Pack = MonumentsPack, File = Monuments, Mode = SliceMode.Rects,
                Rects = new[] { new SheetRect("Pedestal", 40, 16, 16, 27, k_Bottom), new SheetRect("Figure", 8, 201, 15, 17, k_Bottom), new SheetRect("Plaque", 296, 244, 16, 12, k_Bottom) },
            };
            yield return new Sheet { Source = k_Well, Pack = WellPack, File = Well, Mode = SliceMode.Rects, Rects = new[] { new SheetRect("Well", 0, 0, 24, 24, k_Bottom) } };
            yield return new Sheet
            {
                Source = k_Foliage, Pack = FoliagePack, File = Foliage, Mode = SliceMode.Rects,
                Rects = new[]
                {
                    // The trees' pivots sit where the trunk meets the ground, above the roots' spread.
                    new SheetRect("TreeLarge", 16, 124, 118, 72, new Vector2(0.5f, 0.14f)),
                    new SheetRect("TreeMedium", 157, 134, 71, 59, new Vector2(0.5f, 0.12f)),
                    new SheetRect("TreeSmall", 243, 144, 54, 46, new Vector2(0.5f, 0.12f)),
                    new SheetRect("TreeSapling", 306, 152, 34, 34, new Vector2(0.5f, 0.1f)),
                    new SheetRect("Bush0", 43, 49, 26, 15, k_Bottom), new SheetRect("Bush1", 147, 48, 26, 16, k_Bottom),
                    new SheetRect("Bush2", 250, 48, 28, 16, k_Bottom), new SheetRect("Bush3", 459, 47, 26, 17, k_Bottom),
                    new SheetRect("Shrub0", 11, 50, 18, 12, k_Bottom), new SheetRect("Shrub1", 115, 49, 18, 13, k_Bottom),
                },
            };
            yield return new Sheet
            {
                Source = k_CityProps, Pack = MinifantasySheets.MedievalCity, File = CityProps, Mode = SliceMode.Rects,
                Rects = new[]
                {
                    new SheetRect("Bench", 24, 113, 16, 7, k_Bottom), new SheetRect("BenchLong", 24, 148, 16, 9, k_Bottom),
                    new SheetRect("LampPost", 112, 98, 9, 30, k_Bottom), new SheetRect("FlowerBox", 99, 42, 10, 11, k_Bottom),
                    new SheetRect("FlowerBoxRed", 130, 43, 12, 10, k_Bottom), new SheetRect("Barrel", 346, 170, 12, 13, k_Bottom),
                    new SheetRect("Crate", 272, 168, 16, 16, k_Bottom),
                },
            };
            yield return new Sheet
            {
                Source = k_FarmProps, Pack = FarmPack, File = FarmProps, Mode = SliceMode.Rects,
                Rects = new[]
                {
                    new SheetRect("Haystack", 83, 13, 20, 24, k_Bottom), new SheetRect("HayPile", 11, 13, 33, 23, k_Bottom),
                    new SheetRect("Bales", 16, 48, 32, 8, k_Bottom), new SheetRect("Scarecrow", 76, 48, 11, 15, k_Bottom),
                    new SheetRect("Trough", 120, 40, 16, 8, k_Bottom), new SheetRect("Bucket", 121, 57, 6, 6, k_Bottom),
                },
            };
            var farm = new List<SheetRect>();
            farm.Add(new SheetRect("FenceRun", 448, 120, 24, 6, k_Bottom));
            farm.Add(new SheetRect("FencePost", 312, 0, 8, 24, k_Bottom));
            yield return new Sheet { Source = k_FarmTiles, Pack = FarmPack, File = FarmTiles, Mode = SliceMode.Rects, Rects = farm.ToArray() };
            // Towns' props: the standing signboards (Tally Ho!'s tankard board outside, the menu board inside) and the cupboard of jars
            // that is Tally Ho!'s storeroom shelves.
            yield return new Sheet
            {
                Source = k_TownsProps, Pack = TownsPack, File = TownsProps, Mode = SliceMode.Rects,
                Rects = new[]
                {
                    new SheetRect("TankardBoard", 9, 86, 23, 17, k_Bottom), new SheetRect("MenuBoard", 130, 92, 12, 11, k_Bottom),
                    new SheetRect("PostSign", 147, 94, 10, 9, k_Bottom), new SheetRect("CupboardJars", 66, 244, 11, 15, k_Bottom),
                },
            };
            // Phi's framed portrait (Tools/portraits/framed.py, from the Portrait Generator and Minifantasy's own frame colours).
            yield return new Sheet { Source = "derived:Tools/portraits/derived/phi_framed.png", Pack = MinifantasySheets.Portraits, File = "PhiFramed", Mode = SliceMode.Single, Pivot = k_Bottom };
            yield return new Sheet { Source = $"{k_Cart}/Idle_Shop_Open.png", Pack = MerchantPack, File = CartOpen, Mode = SliceMode.Rects, Rects = new[] { new SheetRect("Cart", 0, 0, 64, 64, k_Bottom) } };
            yield return new Sheet { Source = $"{k_Cart}/Idle_Shop_Closed_No_Merchant.png", Pack = MerchantPack, File = CartClosed, Mode = SliceMode.Rects, Rects = new[] { new SheetRect("Cart", 0, 0, 64, 64, k_Bottom) } };
        }
    }
}
