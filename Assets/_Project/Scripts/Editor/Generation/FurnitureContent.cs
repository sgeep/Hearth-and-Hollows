using System;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Progression;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using UnityEditor;
using UnityEngine;

namespace Hearthdelve.Editor
{
    /// <summary>
    /// 4f step 1: the tavern's furniture as data. Builds a <see cref="FurnitureDefinition"/> for each piece of the 4e
    /// room (from the same sprites, footprints and use points <c>TavernBuilder</c> used), the starting layout that puts
    /// them back exactly where 4e had them (the fourth table, which only the retired seat upgrade brought out, is left
    /// for the catalogue), and the presentation asset (highlights, materials). Adds them to the game database and
    /// retires the seat upgrade asset (D16). Safe to run again: everything is created or updated in place.
    /// </summary>
    public static class FurnitureContent
    {
        public const string Folder = EditorPaths.Data + "/Furniture";
        public const string StartingLayoutPath = Folder + "/FurnitureStartingLayout.asset";
        public const string PresentationPath = Folder + "/FurniturePresentation.asset";
        const string k_DatabasePath = EditorPaths.Data + "/GameDatabase.asset";
        const string k_SeatUpgradePath = EditorPaths.Data + "/Upgrades/Upgrade_TavernSeats.asset";
        const string k_UnlitSprite = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        const float k_Ppu = MinifantasySheets.PixelsPerUnit;

        static readonly Color k_Warm = new(1f, 0.66f, 0.36f);
        static readonly Color k_Lamp = new(1f, 0.8f, 0.55f);

        public sealed class Built
        {
            public readonly Dictionary<string, FurnitureDefinition> Definitions = new();
            public FurnitureStartingLayout StartingLayout;
            public FurniturePresentation Presentation;
            public GameDatabase Database;
        }

        [MenuItem("Hearthdelve/Generate/Furniture (4f)", priority = 4)]
        public static void BuildMenu() => Build();

        public static Built Build()
        {
            EditorPaths.Ensure(Folder);
            var built = new Built();
            foreach (FurnitureDefinition d in Definitions()) built.Definitions[d.id] = d;
            built.StartingLayout = StartingLayout();
            built.Presentation = Presentation();
            built.Database = AssetDatabase.LoadAssetAtPath<GameDatabase>(k_DatabasePath);
            if (built.Database != null)
            {
                foreach (FurnitureDefinition d in built.Definitions.Values)
                    if (!built.Database.furniture.Contains(d)) built.Database.furniture.Add(d);
                built.Database.furniture.RemoveAll(d => d == null);
                built.Database.startingFurniture = built.StartingLayout;
                RetireSeatUpgrade(built.Database);
                EditorUtility.SetDirty(built.Database);
            }
            AssetDatabase.SaveAssets();
            return built;
        }

        /// <summary>D16: seating comes from placed tables and chairs; the upgrade leaves the Night screen and the project.</summary>
        static void RetireSeatUpgrade(GameDatabase database)
        {
            database.upgrades.RemoveAll(u => u == null || u.id == Hearthdelve.Shared.Save.SaveSystem.RetiredSeatUpgrade);
            if (AssetDatabase.LoadAssetAtPath<TavernUpgradeDefinition>(k_SeatUpgradePath) != null) AssetDatabase.DeleteAsset(k_SeatUpgradePath);
        }

        // ------------------------------------------------------------------ helpers

        static Sprite Tavern(string layer, string name) => MinifantasyImporter.Sprite(MinifantasySheets.TavernIndoor, $"TavernIndoor_{layer}", name);
        static Sprite Single(string pack, string file) => MinifantasyImporter.Sprites(pack, file).Values.First();

        static FurnitureArt Art(string name, Sprite sprite, Vector2 position, string layer = SortingLayers.YSorted, int order = 0) =>
            new() { name = name, sprite = sprite, position = position, sortingLayer = layer, order = order };

        /// <summary>
        /// A body that starts at the bottom of the art, in art pixels from its bottom-left corner (as TavernBuilder's
        /// footprints): placed in footprint space with the art at <paramref name="artPosition"/>.
        /// </summary>
        static Rect Body(Sprite sprite, Vector2 artPosition, float x, float width, float height, float y = 0f)
        {
            Bounds art = sprite.bounds;
            return new Rect(artPosition.x + art.min.x + x / k_Ppu, artPosition.y + art.min.y + y / k_Ppu, width / k_Ppu, height / k_Ppu);
        }

        static Rect FullBody(Sprite sprite, Vector2 artPosition)
        {
            Vector2 size = sprite.bounds.size * k_Ppu;
            return Body(sprite, artPosition, 0f, size.x, size.y);
        }

        static FurnitureLight Light(Vector2 position, Color color, float intensity, float radius) =>
            new() { position = position, color = color, intensity = intensity, innerRadius = radius * 0.2f, outerRadius = radius, falloff = 0.6f };

        static Rect Offset(Rect r, Vector2 by) => new(r.position + by, r.size);

        static FurnitureDefinition Define(string id, string nameKey, FurnitureCategory category, Action<FurnitureDefinition> apply) =>
            LookTestContent.CreateOrUpdate<FurnitureDefinition>($"{Folder}/Furniture_{id}.asset", d =>
            {
                d.id = id;
                d.nameKey = nameKey;
                d.category = category;
                d.layer = FurnitureLayer.Standing;
                d.rotation = RotationMode.None;
                d.flippable = false;
                d.wallBound = false;
                d.function = FurnitureFunction.None;
                d.station = StationKind.None;
                d.useNameKey = null;
                d.reach = 1f;
                d.unique = false;
                d.catalogTier = 0;
                d.sources = FurnitureSource.Starter;
                d.facings = new List<FurnitureFacing>();
                apply(d);
            });

        // ------------------------------------------------------------------ the 4e pieces

        static IEnumerable<FurnitureDefinition> Definitions()
        {
            // The bar and its taps: the Tap station, used from the customer side. The premade L-shape has its stools drawn
            // in, and its lamp hangs over it. Doesn't turn: the drawn stools and baked light only read one way.
            yield return Define("tavern_bar", "furniture.tavern_bar", FurnitureCategory.Stations, d =>
            {
                d.function = FurnitureFunction.Station;
                d.station = StationKind.Tap;
                d.useNameKey = TavernLocKeys.StationTap;
                d.reach = 1f;
                Sprite bar = Tavern("props", "Bar");
                var at = new Vector2(0.5f, 0f);
                d.facings.Add(new FurnitureFacing
                {
                    size = new Vector2Int(8, 3),
                    art = { Art("Bar", bar, at), Art("Taps", Tavern("props2", "BarTop"), at + new Vector2(0.25f, 1.125f), SortingLayers.YSorted, 1) },
                    grouped = true,
                    groupPoint = at,
                    bodies = { Body(bar, at, 4f, 55f, 14f), Body(bar, at, 0f, 5f, 22f) },
                    interactPoint = at,
                    usePoints = { at + new Vector2(1.1f, -0.55f) },
                    highlight = Offset(new Rect(0.1f, 1f, 1.8f, 1.75f), at),
                    lights = { Light(new Vector2(4f, 2f), k_Lamp, 0.6f, 6f) },
                });
            });

            // The kitchen: the Grill. The oven stands against the back wall, so it only moves along it. Both bodies start
            // 3 px up, on a tile edge, so the row in front stays open and anyone stopped there draws in front (4c step 4).
            // It can be used from behind the range too, like a cook at a stove (4c step 2 playtest).
            yield return Define("kitchen_range", "furniture.kitchen_range", FurnitureCategory.Stations, d =>
            {
                d.function = FurnitureFunction.Station;
                d.station = StationKind.Grill;
                d.useNameKey = TavernLocKeys.StationGrill;
                d.reach = 1.5f;
                d.wallBound = true;
                Sprite kitchen = Single(MinifantasySheets.CraftingAndProfessions, "Kitchen");
                var at = new Vector2(0f, -0.375f);
                FurnitureArt art = Art("Kitchen", kitchen, at);
                art.workingFrames = MinifantasyImporter.Row(MinifantasySheets.CraftingAndProfessions, "KitchenWorking", 0, 8);
                d.facings.Add(new FurnitureFacing
                {
                    size = new Vector2Int(4, 2),
                    art = { art, Art("Shadow", Single(MinifantasySheets.CraftingAndProfessions, "KitchenShadow"), at, SortingLayers.Floor, 5) },
                    bodies = { Body(kitchen, at, 12f, 16f, 8f, 3f), Body(kitchen, at, 1f, 11f, 25f, 3f) },
                    interactPoint = at,
                    usePoints = { at + new Vector2(1.8125f, -0.2f), at + new Vector2(2.5f, 1.875f) },
                    highlight = Offset(new Rect(0.125f, 0.375f, 3.375f, 3.125f), at),
                    lights = { Light(new Vector2(1f, 1.125f), k_Warm, 0.7f, 4.5f) },
                });
            });

            // The stew pot: the Dungeon cauldron over a Dwarven Kingdom floor fire, sorted as one, usable from the front
            // or from behind (4c step 2 playtest). Its simmer bar and helpings hang over it.
            yield return Define("stew_pot", "furniture.stew_pot", FurnitureCategory.Stations, d =>
            {
                d.function = FurnitureFunction.Station;
                d.station = StationKind.StewPot;
                d.useNameKey = TavernLocKeys.StationStewPot;
                d.reach = 1f;
                Sprite cauldron = MinifantasyImporter.Sprite(MinifantasySheets.Dungeon, "Props", "Cauldron");
                var at = new Vector2(0.75f, 0.5f);
                var fire = Art("Fire", null, at + new Vector2(0f, -0.125f));
                fire.frames = MinifantasyImporter.Row(MinifantasySheets.DwarvenKingdom, "FloorFireplace", 0, 8);
                fire.frameSeconds = 0.12f;
                d.facings.Add(new FurnitureFacing
                {
                    size = new Vector2Int(2, 2),
                    art = { fire, Art("Cauldron", cauldron, at, SortingLayers.YSorted, 1) },
                    grouped = true,
                    groupPoint = at,
                    bodies = { FullBody(cauldron, at) },
                    interactPoint = at,
                    usePoints = { at + new Vector2(0f, -0.6f), at + new Vector2(0f, 1.95f) },
                    highlight = Offset(new Rect(-0.75f, 0f, 1.5f, 1.4f), at),
                    statusPoint = at + new Vector2(0f, 2.1f),
                    lights = { Light(at + new Vector2(0f, 0.25f), k_Warm, 0.6f, 3f) },
                });
            });

            // The pass: a long table where plates wait, usable from either side; the server waits just below it.
            yield return Define("pass_table", "furniture.pass_table", FurnitureCategory.Stations, d =>
            {
                d.function = FurnitureFunction.Pass;
                d.useNameKey = TavernLocKeys.StationPass;
                d.reach = 1.5f;
                Sprite table = Tavern("props", "LongTableH");
                var at = new Vector2(1.5f, 0.5f);
                var facing = new FurnitureFacing
                {
                    size = new Vector2Int(4, 1),
                    art = { Art("Pass", table, at) },
                    grouped = true,
                    groupPoint = at,
                    bodies = { FullBody(table, at) },
                    interactPoint = at,
                    usePoints = { at + new Vector2(0f, 0.5f) },
                    hasStaffPost = true,
                    staffPost = at + new Vector2(0f, -0.9f),
                    highlight = Offset(new Rect(-1.75f, 0f, 3.5f, 1f), at),
                };
                for (int i = 0; i < 4; i++) facing.slots.Add(at + new Vector2(-1.2f + i * 0.8f, 0.55f));
                d.facings.Add(facing);
            });

            // Round tables: what seats face.
            foreach (var (id, sprite) in new[] { ("table_round_a", "TableRoundA"), ("table_round_b", "TableRoundB") })
                yield return Define(id, $"furniture.{id}", FurnitureCategory.Tables, d =>
                {
                    d.function = FurnitureFunction.Table;
                    d.price = 40;
                    d.sources = FurnitureSource.Starter | FurnitureSource.Bought;
                    Sprite table = Tavern("props", sprite);
                    var at = new Vector2(0.5f, 0f);
                    d.facings.Add(new FurnitureFacing { size = Vector2Int.one, art = { Art("Table", table, at) }, bodies = { FullBody(table, at) } });
                });

            // The tavern chair: Minifantasy drew all four facings, so it turns through them (D2: authored facings). A
            // customer steps on from below, except facing the camera, where the table is below: then from the side.
            yield return Define("tavern_chair", "furniture.tavern_chair", FurnitureCategory.Seating, d =>
            {
                d.function = FurnitureFunction.Seat;
                d.rotation = RotationMode.AuthoredFacings;
                d.price = 15;
                d.sources = FurnitureSource.Starter | FurnitureSource.Bought;
                var at = new Vector2(0.5f, 0f);
                foreach (var (turns, sprite, facing, approach) in new[]
                         {
                             (0, "ChairFacingS", new Vector2Int(0, -1), new Vector2(1.4f, 0f)),
                             (1, "ChairFacingE", new Vector2Int(1, 0), new Vector2(0.5f, -0.9f)),
                             (2, "ChairFacingN", new Vector2Int(0, 1), new Vector2(0.5f, -0.9f)),
                             (3, "ChairFacingW", new Vector2Int(-1, 0), new Vector2(0.5f, -0.9f)),
                         })
                {
                    Sprite chair = Tavern("props", sprite);
                    d.facings.Add(new FurnitureFacing
                    {
                        turns = turns,
                        size = Vector2Int.one,
                        art = { Art("Chair", chair, at) },
                        bodies = { FullBody(chair, at) },
                        seats = { new FurnitureSeat { position = at, approach = approach, facing = facing } },
                    });
                }
            });

            yield return Define("cellar_barrel", "furniture.cellar_barrel", FurnitureCategory.BarAndStorage, d =>
            {
                d.price = 20;
                d.sources = FurnitureSource.Starter | FurnitureSource.Bought;
                Sprite barrel = MinifantasyImporter.Sprite(MinifantasySheets.Dungeon, "Props", "Barrel");
                var at = new Vector2(0.5f, 0f);
                d.facings.Add(new FurnitureFacing { size = Vector2Int.one, art = { Art("Barrel", barrel, at) }, bodies = { FullBody(barrel, at) } });
            });

            // The back wall: shelves of bottles behind the bar, a sign, the fireplace (a small flame, with its glow), a
            // low shelf by the kitchen and a row of glasses on it. Wall pieces never block and don't turn.
            yield return Wall("bottle_shelves", FurnitureCategory.BarAndStorage, new Vector2Int(6, 2),
                Art("Shelves", Tavern("props", "Shelves"), new Vector2(0.75f, 0.25f), SortingLayers.Floor, 3),
                Art("Bottles", Tavern("props2", "ShelfGoods"), new Vector2(0.75f, 0.125f), SortingLayers.Floor, 4));
            yield return Wall("wall_sign", FurnitureCategory.WallDecor, new Vector2Int(2, 1),
                Art("Sign", Tavern("props", "Sign"), new Vector2(0f, 0.25f), SortingLayers.Floor, 3));
            yield return Wall("low_shelf", FurnitureCategory.BarAndStorage, new Vector2Int(2, 1),
                Art("Shelf", Tavern("props", "ShelfLow"), new Vector2(0.5f, 0.75f), SortingLayers.Floor, 3));
            yield return Wall("shelf_glasses", FurnitureCategory.BarAndStorage, new Vector2Int(2, 1),
                Art("Glasses", Tavern("props2", "Glasses"), new Vector2(0.75f, 0.5f), SortingLayers.Floor, 4));
            FurnitureArt flame = Art("Fire", null, new Vector2(1.5f, 0f), SortingLayers.Floor, 3);
            flame.frames = MinifantasyImporter.Row(MinifantasySheets.DwarvenKingdom, "WallFireplace", 0, 8);
            flame.frameSeconds = 0.12f;
            FurnitureDefinition fireplace = Wall("wall_fireplace", FurnitureCategory.Lighting, new Vector2Int(3, 3), flame);
            fireplace.facings[0].lights.Add(Light(new Vector2(1.5f, 0.5f), k_Warm, 0.9f, 7f));
            yield return fireplace;
        }

        static FurnitureDefinition Wall(string id, FurnitureCategory category, Vector2Int size, params FurnitureArt[] art) =>
            Define(id, $"furniture.{id}", category, d =>
            {
                d.layer = FurnitureLayer.Wall;
                var facing = new FurnitureFacing { size = size };
                facing.art.AddRange(art);
                d.facings.Add(facing);
            });

        // ------------------------------------------------------------------ the starting layout

        /// <summary>
        /// Where 4e had each piece (world tiles of the art's pivot) as a cell and a nudge. Seats are numbered in this
        /// order, as in 4e: each table's west chair, then its east chair.
        /// </summary>
        static FurnitureStartingLayout StartingLayout()
        {
            var pieces = new List<PlacedFurniture>();
            void Place(string id, int x, int y, int turns = 0, int nx = 0, int ny = 0) =>
                pieces.Add(new PlacedFurniture { uid = pieces.Count + 1, definition = id, cell = new Vector2Int(x, y), turns = turns, nudge = new Vector2Int(nx, ny) });

            Place("tavern_bar", 1, 11);
            Place("kitchen_range", 19, 12);
            Place("stew_pot", 24, 11);
            Place("pass_table", 20, 8);
            // Tables at (4.5, 7), (9.5, 7) and (18, 4), with a chair 1.25 tiles either side, a quarter tile up.
            Place("table_round_a", 4, 7);
            Place("tavern_chair", 3, 7, turns: 1, nx: -2, ny: 2);
            Place("tavern_chair", 5, 7, turns: 3, nx: 2, ny: 2);
            Place("table_round_b", 9, 7);
            Place("tavern_chair", 8, 7, turns: 1, nx: -2, ny: 2);
            Place("tavern_chair", 10, 7, turns: 3, nx: 2, ny: 2);
            Place("table_round_a", 18, 4, nx: -4);
            Place("tavern_chair", 16, 4, turns: 1, nx: 2, ny: 2);
            Place("tavern_chair", 19, 4, turns: 3, nx: -2, ny: 2);
            // Barrels by the east wall.
            Place("cellar_barrel", 26, 8, nx: -2);
            Place("cellar_barrel", 26, 9, nx: -2);
            Place("cellar_barrel", 25, 8, nx: -2);
            // The back wall.
            Place("bottle_shelves", 2, 14);
            Place("wall_sign", 11, 15);
            Place("wall_fireplace", 14, 14);
            Place("low_shelf", 24, 14);
            Place("shelf_glasses", 24, 15);

            return LookTestContent.CreateOrUpdate<FurnitureStartingLayout>(StartingLayoutPath, s =>
            {
                s.areas = new List<AreaLayoutData> { new() { area = PropertyArea.TavernId, pieces = pieces } };
                s.storage = new List<OwnedFurnitureData>();
            });
        }

        static FurniturePresentation Presentation() =>
            LookTestContent.CreateOrUpdate<FurniturePresentation>(PresentationPath, p =>
            {
                p.litMaterial = LookTestContent.LitSpriteMaterial;
                p.overlayMaterial = AssetDatabase.LoadAssetAtPath<Material>(k_UnlitSprite);
                Sprite Selector(string name) => MinifantasyImporter.Sprite(MinifantasySheets.UIOverhaul, "Selectors", name);
                p.cornerTopLeft = Selector("CornerTL");
                p.cornerTopRight = Selector("CornerTR");
                p.cornerBottomLeft = Selector("CornerBL");
                p.cornerBottomRight = Selector("CornerBR");
                p.marker = Selector("Marker");
                p.highlightColor = new Color(1f, 0.82f, 0.3f);
                p.seatHighlight = new Rect(-0.5f, -0.1f, 1f, 1.85f);
                p.pixel = DungeonUI.Pixel();
            });
    }
}
