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
        // The garden (4h Checkpoint B): Farm's seeds-and-crops sheet (wheat), More Veggies (onion, spinach), Farm's action icons.
        public const string FarmCrops = "FarmCrops";
        public const string MoreVeggies = "MoreVeggies";
        public const string FarmActions = "FarmActions";
        // The Crossroads (2026-10-10): Medieval City's dirt and cobble autotiles and More Grass Variations' patches.
        public const string CityTiles = "CityTiles";
        public const string MoreGrass = "MoreGrass";

        /// <summary>A crop's cells: the sheet, the group's left edge and the row. Each cell is 8×16, bottom-pivoted, on a 16-px row.</summary>
        public static readonly (string name, string file, int x, int row)[] Crops =
        {
            ("Wheat", FarmCrops, 0, 4),     // Farm: Pumpkin, Eggplant, Berry, Beet, Wheat (left group)
            ("Onion", MoreVeggies, 80, 2),  // More Veggies: Cabbage, Zucchini, Onion, Green beans (middle group)
            ("Spinach", MoreVeggies, 0, 1), // More Veggies: Carrot, Spinach, Cucumber, Artichoke (left group): the herb bed
        };

        /// <summary>The stages of a crop, by its cell's offset in the group: seeds on the ground, three growth states, the grown icon.</summary>
        public static readonly (string stage, int offset)[] CropStages = { ("Seeds", 24), ("Grow1", 32), ("Grow2", 40), ("Grow3", 48), ("Icon", 64) };

        /// <summary>Farm's animated action icons (four 16×16 frames a row): seeding, watering, pulling up.</summary>
        public static readonly (string name, int row)[] Actions = { ("Seed", 4), ("Water", 2), ("Pull", 3) };

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
        const string k_FarmCrops = "Minifantasy_Farm_v3.0/Minifantasy_Farm_Assets/Crops/Minifantasy_FarmSeedsAndCrops.png";
        const string k_MoreVeggies = "All_Exclusives_20261002/Addons/Farm/More_Veggies/MoreVeggies.png";
        const string k_FarmActions = "Minifantasy_Farm_v3.0/Minifantasy_Farm_Assets/Actions/Minifantasy_FarmActionInProgress(16x16).png";

        // 4h Checkpoint C: Kariaston's people. Maximo is the blue Knight on foot (locked); Grim the Miner; Ogrin a Snowball Wars child
        // (body, boots and a red jumper, with its gathering animation); Kaloren and Bart are A Myriad of NPCs' layers (MinifantasySheets).
        public const string KnightPack = "KnightJousting";
        public const string MinerPack = "Miner";
        public const string SnowballPack = "SnowballWars";
        // 4h Checkpoint D: Gimp is Modern Soldiers' soldier_headband (locked) with a backpack; Glimmer's light is the Naughty Fairy.
        public const string SoldiersPack = "ModernSoldiers";
        public const string FairyPack = "NaughtyFairy";
        const string k_Soldiers = "All_Exclusives_20261002/Creatures/Modern_Soldiers";
        const string k_Fairy = "All_Exclusives_20261002/Creatures/Naughty_Fairy";
        const string k_Knight = "All_Exclusives_20261002/Addons/Medieval_Carnival/Knight_Jousting_Add-on_1.5/Knight On Foot";
        const string k_Miner = "All_Exclusives_20261002/Creatures/Miner";
        const string k_Snowball = "All_Exclusives_20261002/Seasonal_Content/Minifantasy_Snowball_Wars_Revamped_v1.0/Minifantasy_Snowball_Wars_Revamped_Assets/Characters/Separate_Layers";

        /// <summary>The cast's figure sheets: (pack, imported file, source under the Minifantasy folder).</summary>
        public static IEnumerable<(string pack, string file, string source)> CastFigures()
        {
            foreach (string a in new[] { "Idle", "Walk", "Attack" })
            {
                yield return (KnightPack, $"Knight{a}", $"{k_Knight}/Knight_{a}/Knight_{a}_blue.png");
                yield return (KnightPack, $"Knight{a}Shadow", $"{k_Knight}/Knight_{a}/Knight_{a}_Shadow.png");
            }
            foreach (string a in new[] { "Idle", "Walk", "Attack" })
                yield return (MinerPack, $"Miner{a}", $"{k_Miner}/Minifantasy_Miner{a}.png");
            yield return (MinerPack, "MinerIdleShadow", $"{k_Miner}/_Shadows/Minifantasy_IdleShadow.png");
            yield return (MinerPack, "MinerWalkShadow", $"{k_Miner}/_Shadows/Minifantasy_WalkShadow.png");
            yield return (MinerPack, "MinerAttackShadow", $"{k_Miner}/_Shadows/Minifantasy_MinerAttackShadow.png");
            foreach (string a in new[] { "Idle", "Walk", "Gather" })
            {
                yield return (SnowballPack, $"Child{a}", $"{k_Snowball}/{a}/Characters/{a}_human.png");
                yield return (SnowballPack, $"ChildBoots{a}", $"{k_Snowball}/{a}/Outfit/Boots/{a}_brown_boots.png");
                yield return (SnowballPack, $"ChildJumper{a}", $"{k_Snowball}/{a}/Outfit/Jumpers/{a}_red_jumper.png");
            }
            yield return (SnowballPack, "ChildIdleShadow", $"{k_Snowball}/Idle/_Shadows/IdleShadow.png");
            yield return (SnowballPack, "ChildWalkShadow", $"{k_Snowball}/Walk/_Shadows/Walk_Shadow.png");
            yield return (SnowballPack, "ChildGatherShadow", $"{k_Snowball}/Gather/_Shadows/Gather_Shadow.png");
            foreach (string a in new[] { "Idle", "Walk" })
            {
                yield return (SoldiersPack, $"Gimp{a}", $"{k_Soldiers}/{a}/Soldiers/{a}_soldier_headband.png");
                yield return (SoldiersPack, $"GimpPackBack{a}", $"{k_Soldiers}/{a}/Backpack/{a}_backpack1_b.png");
                yield return (SoldiersPack, $"GimpPackFront{a}", $"{k_Soldiers}/{a}/Backpack/{a}_backpack1_f.png");
                yield return (SoldiersPack, $"Gimp{a}Shadow", $"{k_Soldiers}/{a}/Soldiers/_Shadow/{a}_shadow.png");
            }
            yield return (FairyPack, "FairyFly", $"{k_Fairy}/Fly_Idle.png");
        }

        static readonly Vector2 k_Centre = new(0.5f, 0.5f);
        static readonly Vector2 k_Bottom = new(0.5f, 0f);

        /// <summary>The ground's autotile parts, in Forgotten Plains' tile cells: name → (column, row).</summary>
        public static readonly (string name, int column, int row)[] GroundCells =
        {
            ("Grass0", 1, 1), ("Grass1", 2, 1), ("Grass2", 3, 1), ("Grass3", 4, 1), ("Grass4", 2, 4), ("Grass5", 3, 5), ("Grass6", 4, 6), ("Grass7", 2, 7),
            // The Crossroads' meadow: one flat cell, and the sparse tufts the mockup scatters on it.
            ("GrassFlat", 61, 5), ("Sparse_2_3", 2, 3), ("Sparse_3_3", 3, 3), ("Sparse_4_3", 4, 3), ("Sparse_2_5", 2, 5), ("Sparse_3_5", 3, 5), ("Sparse_4_5", 4, 5),
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
            foreach (var (pack, file, source) in CastFigures())
                yield return new Sheet
                {
                    Source = source, Pack = pack, File = file, Mode = SliceMode.Grid,
                    Cell = new Vector2Int(MinifantasySheets.CharacterFrame, MinifantasySheets.CharacterFrame), Pivot = MinifantasySheets.FeetPivot,
                };

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
            yield return new Sheet { Source = k_Well, Pack = WellPack, File = Well, Mode = SliceMode.Rects, Rects = new[] { new SheetRect("Well", 0, 0, 24, 32, k_Bottom) } };
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
                    new SheetRect("BushRed", 355, 48, 26, 16, k_Bottom), new SheetRect("ShrubPurple", 218, 48, 20, 14, k_Bottom),
                }.Concat(Flowers.Select(f => new SheetRect(f.name, f.x, f.y, f.w, f.h, k_Bottom))).ToArray(),
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
                    new SheetRect("Planter", 67, 43, 11, 10, k_Bottom), new SheetRect("PlanterSmall", 83, 44, 10, 9, k_Bottom),
                    new SheetRect("BarrelSmall", 371, 171, 10, 11, k_Bottom),
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
            foreach (var (name, c, r) in ThinFence) farm.Add(new SheetRect(name, c * 8, r * 8, 8, 8, k_Bottom));
            yield return new Sheet { Source = k_FarmTiles, Pack = FarmPack, File = FarmTiles, Mode = SliceMode.Rects, Rects = farm.ToArray() };

            // The garden's crops (4h Checkpoint B): each stage an 8×16 cell, pivoted at its foot.
            foreach (string file in new[] { FarmCrops, MoreVeggies })
            {
                var cells = new List<SheetRect>();
                foreach (var (name, sheet, x, row) in Crops)
                    if (sheet == file)
                        foreach (var (stage, offset) in CropStages)
                            cells.Add(new SheetRect($"{name}_{stage}", x + offset, row * 16, 8, 16, k_Bottom));
                yield return new Sheet { Source = file == FarmCrops ? k_FarmCrops : k_MoreVeggies, Pack = FarmPack, File = file, Mode = SliceMode.Rects, Rects = cells.ToArray() };
            }
            var actions = new List<SheetRect>();
            foreach (var (name, row) in Actions)
                for (int f = 0; f < 4; f++)
                    actions.Add(new SheetRect($"{name}_{f}", f * 16, row * 16, 16, 16, k_Bottom));
            yield return new Sheet { Source = k_FarmActions, Pack = FarmPack, File = FarmActions, Mode = SliceMode.Rects, Rects = actions.ToArray() };
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
            // Lit windows (Tools/village/lit_window.py, 2026-10-08): a building's own window glass in the lamp post's flame colours, as an
            // overlay the size of the building (same pivot), shown while someone's home behind it.
            yield return new Sheet { Source = "derived:Tools/village/derived/BrownCottageWindowLit.png", Pack = TownsIIPack, File = "BrownCottageWindowLit", Mode = SliceMode.Single, Pivot = k_Bottom };
            // Phi's framed portrait (Tools/portraits/framed.py, from the Portrait Generator and Minifantasy's own frame colours).
            yield return new Sheet { Source = "derived:Tools/portraits/derived/phi_framed.png", Pack = MinifantasySheets.Portraits, File = "PhiFramed", Mode = SliceMode.Single, Pivot = k_Bottom };
            yield return new Sheet { Source = $"{k_Cart}/Idle_Shop_Open.png", Pack = MerchantPack, File = CartOpen, Mode = SliceMode.Rects, Rects = new[] { new SheetRect("Cart", 0, 0, 64, 64, k_Bottom) } };
            yield return new Sheet { Source = $"{k_Cart}/Idle_Shop_Closed_No_Merchant.png", Pack = MerchantPack, File = CartClosed, Mode = SliceMode.Rects, Rects = new[] { new SheetRect("Cart", 0, 0, 64, 64, k_Bottom) } };

            // The Crossroads (2026-10-10): Medieval City's autotiles (dark dirt on grass at 14,138; cobble set in dirt at 22,138;
            // the 3×3 block with its inner corners in the two rows below: the dirt has the two-corner pieces, the cobble doesn't).
            var city = new List<SheetRect>();
            foreach (var (c0, r0, pairs) in new[] { (14, 138, true), (22, 138, false) })
                for (int c = c0; c < c0 + 3; c++)
                for (int r = r0; r < r0 + 5; r++)
                    if (r < r0 + 3 || pairs || c < c0 + 2) city.Add(new SheetRect($"Cell_{c}_{r}", c * 8, r * 8, 8, 8, k_Centre));
            yield return new Sheet { Source = k_CityTiles, Pack = MinifantasySheets.MedievalCity, File = CityTiles, Mode = SliceMode.Rects, Rects = city.ToArray() };
            // More Grass Variations: the two patches the Crossroads lays (blocks at cells 1,3 and 5,3, laid out like the dirt).
            var grass = new List<SheetRect>();
            foreach (int c0 in new[] { 1, 5 })
                for (int c = c0; c < c0 + 3; c++)
                for (int r = 3; r < 8; r++)
                    grass.Add(new SheetRect($"Cell_{c}_{r}", c * 8, r * 8, 8, 8, k_Centre));
            yield return new Sheet { Source = k_MoreGrass, Pack = PlainsPack, File = MoreGrass, Mode = SliceMode.Rects, Rects = grass.ToArray() };

            // Every drawing's Minifantasy shadow, cut by the shadow's own extent and pivoted so it lies exactly under its drawing.
            foreach (var group in Shadows.GroupBy(s => s.file))
            {
                var (pack, source) = k_ShadowFiles[group.Key];
                yield return new Sheet { Source = source, Pack = pack, File = group.Key, Mode = SliceMode.Rects, Rects = group.Select(ShadowRect).ToArray() };
            }
        }

        // ------------------------------------------------------------------ the Crossroads (2026-10-10)

        const string k_CityTiles = "Minifantasy_Medieval_City_v1.1/Minifantasy_Medieval_City_Assets/Tileset/Tileset.png";
        const string k_MoreGrass = "All_Exclusives_20261002/Addons/Forgotten_Plains/More_Grass_Variations/MoreGrassVariations.png";

        /// <summary>Farm's thin fence (cells 50–54 × 15–19): corners, runs and each side's own post.</summary>
        public static readonly (string name, int column, int row)[] ThinFence =
        {
            ("Fence_TL", 50, 15), ("Fence_Top", 51, 15), ("Fence_TR", 54, 15), ("Fence_Left", 50, 16), ("Fence_Right", 54, 16),
            ("Fence_BL", 50, 19), ("Fence_Bottom", 51, 19), ("Fence_BR", 54, 19),
        };

        /// <summary>The flowers and tufts the Crossroads scatters (Plants &amp; Foliage, Forgotten Plains), each a whole drawing.</summary>
        public static readonly (string name, int x, int y, int w, int h)[] Flowers =
        {
            ("Flower_49_14", 49, 14, 14, 9), ("Flower_152_13", 152, 13, 24, 10), ("Flower_432_6", 432, 6, 24, 17),
            ("Flower_120_104", 120, 104, 24, 10), ("Flower_152_88", 152, 88, 24, 10), ("Flower_152_104", 152, 104, 24, 10),
            ("Flower_224_72", 224, 72, 24, 10), ("Flower_256_72", 256, 72, 24, 10), ("Flower_360_72", 360, 72, 24, 10),
            ("Flower_360_88", 360, 88, 24, 10), ("Flower_432_104", 432, 104, 24, 10), ("Flower_464_104", 464, 104, 24, 10),
        };

        public const string FoliageShadow = "PlainsFoliageShadow", BuildingShadow = "BuildingSamplesShadow", TowerShadow = "TowerExteriorShadow",
            WagonShadow = "CaravansAndWagonsShadow", WellShadow = "WellStaticShadow", MonumentShadow = "MonumentsShadow", CartOpenShadow = "CartOpenShadow",
            CartClosedShadow = "CartClosedShadow", CityPropShadow = "CityPropsShadow", FarmPropShadow = "FarmPropsShadow", TownsPropShadow = "TownsPropsShadow",
            FenceShadow = "FarmTilesetShadow";

        /// <summary>Each shadow sheet: its pack folder and source (each the size of its art's sheet, on the art's coordinates).</summary>
        static readonly Dictionary<string, (string pack, string source)> k_ShadowFiles = new()
        {
            [FoliageShadow] = (FoliagePack, "Minifantasy_Plants_&_Foliage_v1.0/Minifantasy_Plants_&_Foliage_Assets/Plains_And_Forests/_Shadow.png"),
            [BuildingShadow] = (TownsIIPack, "Minifantasy_Towns2_v1.5/Minifantasy_Towns2_Assets/Buildings/_Mix_And_Match_Samples/Minifantasy_TownsIIMoreBuildingSamplesShadows.png"),
            [TowerShadow] = (WizardTowerPack, "All_Exclusives_20261002/Addons/_Miscellany/Wizard_Tower/Exterior/Wizard_Tower_Exterior_shadows.png"),
            [WagonShadow] = (WagonsPack, "All_Exclusives_20261002/Addons/Medieval_Carnival/Caravans_And_Wagons/Tileset/CaravansAndWagonsShadows.png"),
            [WellShadow] = (WellPack, "All_Exclusives_20261002/Addons/Towns_I_II/Animated_Well/_Shadows/WellStaticFramesShadow.png"),
            [MonumentShadow] = (MonumentsPack, "All_Exclusives_20261002/Addons/Towns_I_II/Town_Monuments/Shadows.png"),
            [CartOpenShadow] = (MerchantPack, $"{k_Cart}/_Shadows/Shop_Open_Idle_Shadow.png"),
            [CartClosedShadow] = (MerchantPack, $"{k_Cart}/_Shadows/Shop_Closed_Idle_Shadow.png"),
            [CityPropShadow] = (MinifantasySheets.MedievalCity, "Minifantasy_Medieval_City_v1.1/Minifantasy_Medieval_City_Assets/Props/Shadows.png"),
            [FarmPropShadow] = (FarmPack, "Minifantasy_Farm_v3.0/Minifantasy_Farm_Assets/Props/Minifantasy_FarmPropsShadows.png"),
            [TownsPropShadow] = (TownsPack, "Minifantasy_Towns_v3.0/Minifantasy_Towns_Assets/Props/Minifantasy_TownsPropsShadows.png"),
            [FenceShadow] = (FarmPack, "Minifantasy_Farm_v3.0/Minifantasy_Farm_Assets/Tileset/Minifantasy_FarmTilesetShadowLayer.png"),
        };

        /// <summary>The pack folder a shadow sheet is imported into.</summary>
        public static string ShadowPack(string file) => k_ShadowFiles[file].pack;

        /// <summary>A drawing's shadow: the shadow sheet, the drawing's name, rectangle and pivot, and the shadow's own extent.</summary>
        public readonly struct ShadowCut
        {
            public readonly string file, name;
            public readonly RectInt art, shadow;
            public readonly Vector2 pivot;

            public ShadowCut(string file, string name, RectInt art, Vector2 pivot, RectInt shadow)
            {
                this.file = file;
                this.name = name;
                this.art = art;
                this.pivot = pivot;
                this.shadow = shadow;
            }
        }

        static ShadowCut Cut(string file, string name, int ax, int ay, int aw, int ah, Vector2 pivot, int sx, int sy, int sw, int sh) =>
            new(file, name, new RectInt(ax, ay, aw, ah), pivot, new RectInt(sx, sy, sw, sh));

        /// <summary>
        /// The shadows (extents measured by Tools/village/mockup/export_crossroads.py: the shadow pixels touching the drawing's
        /// rectangle; a fence tile's is its own cell). The statue figure has none: it stands on its pedestal.
        /// </summary>
        public static readonly ShadowCut[] Shadows = BuildShadows().ToArray();

        static IEnumerable<ShadowCut> BuildShadows()
        {
            Vector2 b = k_Bottom;
            yield return Cut(BuildingShadow, "ThatchedHall", 9, 264, 118, 105, new Vector2(0.5f, 18f / 105f), 9, 314, 47, 52);
            yield return Cut(BuildingShadow, "BlueRoofHall", 13, 28, 110, 93, new Vector2(0.5f, 17f / 93f), 11, 67, 45, 51);
            yield return Cut(BuildingShadow, "BrownCottage", 319, 160, 58, 64, b, 316, 184, 36, 40);
            yield return Cut(TowerShadow, "TowerExterior", 0, 0, 64, 136, b, 6, 85, 25, 45);
            yield return Cut(WagonShadow, "PaintedWagon", 302, 229, 28, 61, b, 301, 243, 25, 47);
            yield return Cut(WellShadow, "Well", 0, 0, 24, 32, b, 4, 23, 5, 9);
            yield return Cut(MonumentShadow, "Pedestal", 40, 16, 16, 27, b, 37, 22, 4, 21);
            yield return Cut(CartOpenShadow, "Cart", 0, 0, 64, 64, b, 12, 34, 46, 23);
            yield return Cut(CartClosedShadow, "Cart", 0, 0, 64, 64, b, 12, 34, 46, 19);
            yield return Cut(FoliageShadow, "TreeLarge", 16, 124, 118, 72, new Vector2(0.5f, 0.14f), 6, 141, 96, 55);
            yield return Cut(FoliageShadow, "TreeMedium", 157, 134, 71, 59, new Vector2(0.5f, 0.12f), 148, 157, 58, 36);
            yield return Cut(FoliageShadow, "TreeSmall", 243, 144, 54, 46, new Vector2(0.5f, 0.12f), 239, 168, 45, 22);
            yield return Cut(FoliageShadow, "Bush0", 43, 49, 26, 15, b, 41, 51, 17, 13);
            yield return Cut(FoliageShadow, "Bush2", 250, 48, 28, 16, b, 249, 51, 17, 13);
            yield return Cut(FoliageShadow, "Bush3", 459, 47, 26, 17, b, 457, 52, 17, 12);
            yield return Cut(FoliageShadow, "BushRed", 355, 48, 26, 16, b, 353, 53, 17, 11);
            yield return Cut(FoliageShadow, "Shrub0", 11, 50, 18, 12, b, 9, 53, 7, 9);
            yield return Cut(FoliageShadow, "ShrubPurple", 218, 48, 20, 14, b, 217, 55, 7, 7);
            foreach (var (name, sx, sy, sw, sh) in new[]
            {
                ("Flower_49_14", 48, 21, 10, 2), ("Flower_152_13", 152, 21, 19, 2), ("Flower_432_6", 433, 21, 18, 2), ("Flower_120_104", 122, 108, 18, 2),
                ("Flower_152_88", 153, 92, 18, 2), ("Flower_152_104", 153, 108, 18, 2), ("Flower_224_72", 227, 77, 16, 2), ("Flower_256_72", 258, 77, 16, 2),
                ("Flower_360_72", 362, 77, 16, 2), ("Flower_360_88", 360, 93, 18, 2), ("Flower_432_104", 434, 109, 17, 2), ("Flower_464_104", 465, 109, 17, 2),
            })
            {
                var f = Flowers.First(x => x.name == name);
                yield return Cut(FoliageShadow, name, f.x, f.y, f.w, f.h, b, sx, sy, sw, sh);
            }
            yield return Cut(CityPropShadow, "BenchLong", 24, 148, 16, 9, b, 23, 152, 2, 5);
            yield return Cut(CityPropShadow, "LampPost", 112, 98, 9, 30, b, 112, 116, 3, 12);
            yield return Cut(CityPropShadow, "Barrel", 346, 170, 12, 13, b, 345, 173, 5, 10);
            yield return Cut(CityPropShadow, "BarrelSmall", 371, 171, 10, 11, b, 370, 173, 2, 9);
            yield return Cut(CityPropShadow, "Crate", 272, 168, 16, 16, b, 271, 171, 13, 13);
            yield return Cut(CityPropShadow, "Planter", 67, 43, 11, 10, b, 66, 47, 8, 6);
            yield return Cut(CityPropShadow, "PlanterSmall", 83, 44, 10, 9, b, 82, 47, 8, 6);
            yield return Cut(FarmPropShadow, "Haystack", 83, 13, 20, 24, b, 82, 26, 9, 11);
            yield return Cut(FarmPropShadow, "HayPile", 11, 13, 33, 23, b, 10, 25, 26, 11);
            yield return Cut(FarmPropShadow, "Bales", 16, 48, 32, 8, b, 23, 55, 17, 1);
            yield return Cut(FarmPropShadow, "Scarecrow", 76, 48, 11, 15, b, 74, 57, 4, 6);
            yield return Cut(TownsPropShadow, "TankardBoard", 9, 86, 23, 17, b, 8, 102, 22, 1);
            yield return Cut(TownsPropShadow, "PostSign", 147, 94, 10, 9, b, 147, 102, 8, 1);
            foreach (var (name, c, r) in ThinFence) yield return Cut(FenceShadow, name, c * 8, r * 8, 8, 8, b, c * 8, r * 8, 8, 8);
        }

        /// <summary>A shadow's slice: its own rectangle, pivoted at its drawing's pivot (often outside it), so the two line up there.</summary>
        static SheetRect ShadowRect(ShadowCut s)
        {
            float px = s.art.x + s.pivot.x * s.art.width, py = s.art.y + s.art.height - s.pivot.y * s.art.height;   // sheet pixels, y down
            var pivot = new Vector2((px - s.shadow.x) / s.shadow.width, (s.shadow.y + s.shadow.height - py) / s.shadow.height);
            return new SheetRect(s.name, s.shadow.x, s.shadow.y, s.shadow.width, s.shadow.height, pivot);
        }
    }
}
