using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Editor;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Save;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Hearthdelve.Tests
{
    /// <summary>
    /// 4f Checkpoint B: the furniture catalogue generated from its table, buying and selling, catalogue tiers by Renown,
    /// palette recolouring, area finishes, the property's areas sharing one storage, the guest room's checks, and the
    /// version 6 save.
    /// </summary>
    public class CatalogueTests
    {
        static GameDatabase Database => AssetDatabase.LoadAssetAtPath<GameDatabase>(EditorPaths.Data + "/GameDatabase.asset");

        // ------------------------------------------------------------------ the generated catalogue

        [Test]
        public void EveryPieceInTheTable_HasAGeneratedDefinition_InTheDatabase_WithItsStableId()
        {
            FurnitureCatalog.CatalogJson table = FurnitureCatalog.Load();
            Assert.That(table.pieces.Length, Is.GreaterThanOrEqualTo(70), "a substantial first catalogue, not a token one");
            foreach (FurnitureCatalog.PieceJson p in table.pieces)
            {
                FurnitureDefinition d = Database.Furniture(p.id);
                Assert.That(d, Is.Not.Null, p.id);
                Assert.That(d.id, Is.EqualTo(p.id));
                Assert.That(d.nameKey, Is.EqualTo(FurnitureCatalog.NameKey(p.id)));
                Assert.That(d.price, Is.EqualTo(p.price), p.id);
                Assert.That(d.catalogTier, Is.EqualTo(p.tier), p.id);
            }
        }

        [Test]
        public void Ids_AreUnique_AcrossTheCatalogueAndTheStartingPieces()
        {
            var ids = Database.furniture.Where(d => d != null).Select(d => d.id).ToList();
            Assert.That(ids.Count, Is.EqualTo(ids.Distinct().Count()), string.Join(", ", ids.GroupBy(i => i).Where(g => g.Count() > 1).Select(g => g.Key)));
            Assert.That(ids.All(i => System.Text.RegularExpressions.Regex.IsMatch(i, "^[a-z0-9_]+$")), "ids are lower-case snake case");
        }

        [Test]
        public void EveryDefinition_HasDrawingsForEachFacingAndColourway_AndConsistentTurns()
        {
            var problems = new List<string>();
            foreach (FurnitureDefinition d in Database.furniture.Where(d => d != null))
            {
                if (d.facings.Count == 0) problems.Add($"{d.id}: no facings");
                if (d.rotation == RotationMode.AuthoredFacings && d.facings.Select(f => f.turns).Distinct().Count() != d.facings.Count) problems.Add($"{d.id}: turns repeat");
                if (d.rotation != RotationMode.AuthoredFacings && d.facings.Count > 1) problems.Add($"{d.id}: several facings but not authored facings");
                for (int v = 0; v < Mathf.Max(1, d.variants.Count); v++)
                    foreach (FurnitureFacing f in d.facings)
                    foreach (FurnitureArt a in f.art)
                        if (a.SpriteFor(v) == null) problems.Add($"{d.id}: no drawing for variant {v}, turn {f.turns}");
                if (d.variants.Select(v => v.id).Distinct().Count() != d.variants.Count) problems.Add($"{d.id}: colourway ids repeat");
                if (d.layer == FurnitureLayer.Standing && f0(d).bodies.Count == 0) problems.Add($"{d.id}: standing with no body");
                if (d.function == FurnitureFunction.Seat && d.facings.Any(f => f.seats.Count == 0)) problems.Add($"{d.id}: a seat with no seat anchor");
                if (d.ForSale && d.price <= 0) problems.Add($"{d.id}: for sale for nothing");
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));

            static FurnitureFacing f0(FurnitureDefinition d) => d.facings[0];
        }

        [Test]
        public void TheCatalogueOffers_EveryCollection_AcrossTheFourTiers()
        {
            List<FurnitureDefinition> sold = Database.furniture.Where(d => d != null && d.ForSale).ToList();
            foreach (FurnitureTheme theme in new[] { FurnitureTheme.Tavern, FurnitureTheme.Village, FurnitureTheme.Dwarven, FurnitureTheme.Elven, FurnitureTheme.Castle, FurnitureTheme.Haunted })
                Assert.That(sold.Count(d => d.theme == theme), Is.GreaterThanOrEqualTo(4), $"{theme} has a real collection");
            foreach (FurnitureCategory category in new[] { FurnitureCategory.Seating, FurnitureCategory.Tables, FurnitureCategory.BarAndStorage, FurnitureCategory.Lighting,
                         FurnitureCategory.WallDecor, FurnitureCategory.FloorDecor, FurnitureCategory.Plants, FurnitureCategory.Curios, FurnitureCategory.Bedroom })
                Assert.That(sold.Count(d => d.category == category), Is.GreaterThanOrEqualTo(4), $"{category}");
            Assert.That(sold.Select(d => d.catalogTier).Distinct().OrderBy(t => t), Is.EqualTo(new[] { 0, 1, 2, 3 }));
            int entries = Database.furniture.Where(d => d != null).Sum(d => Mathf.Max(1, d.variants.Count) * d.facings.Count);
            Assert.That(entries, Is.GreaterThanOrEqualTo(170), "about 170 placeable entries counting facings and colourways");
        }

        [Test]
        public void TheStartingLayout_NamesOnlyRealPieces_InBothAreas()
        {
            FurnitureStartingLayout start = Database.startingFurniture;
            Assert.That(start.areas.Select(a => a.area), Is.EquivalentTo(new[] { "tavern", "guest_room" }));
            foreach (AreaLayoutData area in start.areas)
            foreach (PlacedFurniture p in area.pieces)
                Assert.That(Database.Furniture(p.definition), Is.Not.Null, $"{area.area}: {p.definition}");
            Assert.That(start.finishes, Does.Contain("floor_diamonds").And.Contain("wall_panelling").And.Contain("floor_teal").And.Contain("wall_cream"));
            Assert.That(start.areas.SelectMany(a => a.pieces).Select(p => p.uid).Distinct().Count(), Is.EqualTo(start.areas.Sum(a => a.pieces.Count)), "uids unique across areas");
        }

        // ------------------------------------------------------------------ buying, selling, tiers

        static readonly int[] k_Tiers = { 0, 25, 60, 100 };

        static FurnitureDefinition Piece(string id, int price = 30, int tier = 0, bool unique = false, FurnitureSource sources = FurnitureSource.Bought)
        {
            var d = ScriptableObject.CreateInstance<FurnitureDefinition>();
            d.id = id;
            d.price = price;
            d.catalogTier = tier;
            d.unique = unique;
            d.sources = sources;
            return d;
        }

        static GameState Game(int gold, int renown)
        {
            var state = new GameState();
            state.AddGold(gold);
            SaveData data = SaveSystem.Capture(state);
            data.renown = renown;
            data.furniture = new FurnitureSaveData { initialized = true };
            return SaveSystem.Restore(data, _ => null, _ => true);
        }

        [Test]
        public void Tiers_OpenWithRenown_AndRenownIsNeverSpent()
        {
            Assert.That(FurnitureShop.OpenTier(0, k_Tiers), Is.EqualTo(0));
            Assert.That(FurnitureShop.OpenTier(24, k_Tiers), Is.EqualTo(0));
            Assert.That(FurnitureShop.OpenTier(25, k_Tiers), Is.EqualTo(1));
            Assert.That(FurnitureShop.OpenTier(99, k_Tiers), Is.EqualTo(2));
            Assert.That(FurnitureShop.OpenTier(140, k_Tiers), Is.EqualTo(3));
            GameState game = Game(500, 30);
            Assert.That(FurnitureShop.Buy(game, Piece("keg", 20, tier: 1), k_Tiers));
            Assert.That(game.Renown, Is.EqualTo(30), "buying never spends Renown (GDD §7.1)");
            Assert.That(FurnitureShop.CanBuy(Piece("elven", 20, tier: 2), game, k_Tiers), Is.EqualTo(PurchaseProblem.Locked));
        }

        [Test]
        public void BuyingOne_GivesOneCopy_IntoStorage_ForItsPrice()
        {
            GameState game = Game(100, 0);
            FurnitureDefinition chair = Piece("chair", 30);
            Assert.That(FurnitureShop.Buy(game, chair, k_Tiers));
            Assert.That(FurnitureShop.Buy(game, chair, k_Tiers));
            Assert.That(FurnitureShop.Buy(game, chair, k_Tiers));
            Assert.That(game.Gold, Is.EqualTo(10));
            Assert.That(game.Furniture.OwnedCount("chair"), Is.EqualTo(3), "copies, not unlimited after one (D7)");
            Assert.That(game.Furniture.InStorage("chair"), Is.EqualTo(3));
            Assert.That(FurnitureShop.CanBuy(chair, game, k_Tiers), Is.EqualTo(PurchaseProblem.NotEnoughGold));
            Assert.That(FurnitureShop.Buy(game, chair, k_Tiers), Is.False);
            Assert.That(game.Gold, Is.EqualTo(10), "a refused purchase changes nothing");
        }

        [Test]
        public void Uniques_CapAtOne_AndDiscoveriesStartersAndUniques_CantBeSold()
        {
            GameState game = Game(1000, 200);
            FurnitureDefinition fireplace = Piece("fireplace", 200, unique: true);
            Assert.That(FurnitureShop.Buy(game, fireplace, k_Tiers));
            Assert.That(FurnitureShop.CanBuy(fireplace, game, k_Tiers), Is.EqualTo(PurchaseProblem.AlreadyOwned));
            Assert.That(FurnitureShop.CanSell(fireplace, 1), Is.False, "uniques can't be sold (D12)");
            Assert.That(FurnitureShop.CanSell(Piece("curio", 0, sources: FurnitureSource.Discovery), 1), Is.False, "discoveries can't be sold");
            Assert.That(FurnitureShop.CanBuy(Piece("bar", 0, sources: FurnitureSource.Starter), game, k_Tiers), Is.EqualTo(PurchaseProblem.NotForSale));
        }

        [Test]
        public void Selling_TakesOneFromStorage_ForHalfThePrice_AndNeedsOneStored()
        {
            GameState game = Game(100, 0);
            FurnitureDefinition table = Piece("table", 45);
            FurnitureShop.Buy(game, table, k_Tiers);
            Assert.That(table.SellPrice, Is.EqualTo(22), "half, rounded down (D12)");
            Assert.That(FurnitureShop.Sell(game, table, inStorage: 0), Is.False, "nothing stored: placed copies aren't sold from the room");
            Assert.That(FurnitureShop.Sell(game, table, inStorage: 1));
            Assert.That(game.Gold, Is.EqualTo(55 + 22));
            Assert.That(game.Furniture.OwnedCount("table"), Is.Zero);
        }

        [Test]
        public void Finishes_AreBoughtOnce_ForEveryArea()
        {
            GameState game = Game(100, 30);
            var finish = ScriptableObject.CreateInstance<FinishDefinition>();
            finish.id = "floor_parquet";
            finish.price = 70;
            finish.catalogTier = 1;
            Assert.That(FurnitureShop.Buy(game, finish, k_Tiers));
            Assert.That(game.Furniture.OwnsFinish("floor_parquet"));
            Assert.That(FurnitureShop.CanBuy(finish, game, k_Tiers), Is.EqualTo(PurchaseProblem.AlreadyOwned));
            game.Furniture.SetFinish("tavern", FinishKind.Floor, "floor_parquet");
            game.Furniture.SetFinish("guest_room", FinishKind.Floor, "floor_parquet");
            Assert.That(game.Gold, Is.EqualTo(30), "paid once, laid twice");
        }

        [Test]
        public void EachTierThatOpens_IsAnnouncedOnce()
        {
            var furniture = new FurnitureState();
            Assert.That(FurnitureShop.Announce(furniture, 10, k_Tiers), Is.Empty);
            Assert.That(FurnitureShop.Announce(furniture, 64, k_Tiers), Is.EqualTo(new[] { 1, 2 }), "a big night opens two");
            Assert.That(FurnitureShop.Announce(furniture, 64, k_Tiers), Is.Empty, "not again");
            Assert.That(FurnitureShop.Announce(furniture, 100, k_Tiers), Is.EqualTo(new[] { 3 }));
        }

        // ------------------------------------------------------------------ palettes

        static Color32 C(string hex) => FurnitureLooks.Hex(hex);

        [Test]
        public void PaletteChoices_ReadAndWriteInOneOrder()
        {
            Assert.That(FurniturePalette.Format(FurniturePalette.Parse("wood=walnut;cushion=teal")), Is.EqualTo("cushion=teal;wood=walnut"));
            Assert.That(FurniturePalette.With("cushion=teal", "wood", "ash"), Is.EqualTo("cushion=teal;wood=ash"));
            Assert.That(FurniturePalette.With("cushion=teal;wood=ash", "wood", null), Is.EqualTo("cushion=teal"), "back to as drawn");
            var wood = new List<PaletteChannel> { new() { kind = "wood" } };
            Assert.That(FurniturePalette.Restrict("cushion=teal;wood=ash", wood), Is.EqualTo("wood=ash"), "copying colours keeps the channels a piece has");
            Assert.That(FurniturePalette.Parse(""), Is.Empty);
            Assert.That(FurniturePalette.CacheKey("chair", "wood=ash"), Is.Not.EqualTo(FurniturePalette.CacheKey("chair", "wood=birch")));
            Assert.That(FurniturePalette.CacheKey("chair", ""), Is.EqualTo("chair"));
        }

        [Test]
        public void TheRemap_MatchesColoursByBrightnessRank_AndKeepsEverythingElse()
        {
            Color32[] drawn = { C("#5d2415"), C("#a94117"), C("#d56825") };
            Color32[] ramp = { C("#2b3007"), C("#443c09"), C("#593a09"), C("#723908"), C("#8a530e") };
            Dictionary<int, Color32> map = FurniturePalette.Mapping(new[] { (drawn, ramp) });
            Color32 outline = new(0, 0, 0, 255), clear = new(213, 104, 37, 0), half = new(213, 104, 37, 128);
            Color32[] result = FurniturePalette.Remap(new[] { C("#5d2415"), C("#a94117"), C("#d56825"), outline, clear, half }, map);
            Assert.That(result[0], Is.EqualTo(ramp[0]), "darkest to darkest");
            Assert.That(result[1], Is.EqualTo(ramp[2]), "middle to middle");
            Assert.That(result[2], Is.EqualTo(ramp[4]), "lightest to lightest");
            Assert.That(result[3], Is.EqualTo(outline), "the outline isn't a channel colour");
            Assert.That(result[4].a, Is.Zero, "transparent stays transparent");
            Assert.That(result[5], Is.EqualTo(new Color32(ramp[4].r, ramp[4].g, ramp[4].b, 128)), "alpha kept");
        }

        [Test]
        public void ARecolour_IsBakedOnce_AndSharedByEveryPieceThatAsksForIt()
        {
            var texture = new Texture2D(2, 1, TextureFormat.RGBA32, false);
            texture.SetPixels32(new[] { C("#5d2415"), C("#c01515") });
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 2, 1), new Vector2(0.5f, 0f), 8);
            sprite.name = "test_chair";
            var library = ScriptableObject.CreateInstance<PaletteLibrary>();
            library.ramps.Add(new PaletteRamp { id = "teal", kind = "cushion", colors = new[] { C("#1b5b7a"), C("#2c917b") } });
            var chair = Piece("test_chair");
            chair.paletteChannels.Add(new PaletteChannel { kind = "cushion", source = new[] { C("#c01515") } });

            Sprite baked = FurnitureRecolour.Apply(sprite, chair, "cushion=teal", library);
            Assert.That(baked, Is.Not.SameAs(sprite));
            Color32[] pixels = baked.texture.GetPixels32();
            Assert.That(pixels[0], Is.EqualTo(C("#5d2415")), "the wood isn't a cushion");
            Assert.That(pixels[1], Is.EqualTo(C("#1b5b7a")).Or.EqualTo(C("#2c917b")), "the cushion turns teal");
            Assert.That(baked.pivot, Is.EqualTo(sprite.pivot), "same pivot");
            Assert.That(baked.pixelsPerUnit, Is.EqualTo(8f));
            Assert.That(baked.texture.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(FurnitureRecolour.Apply(sprite, chair, "cushion=teal", library), Is.SameAs(baked), "cached");
            Assert.That(FurnitureRecolour.Apply(sprite, chair, "", library), Is.SameAs(sprite), "as drawn: the drawing itself");
            Assert.That(FurnitureRecolour.Apply(sprite, chair, "wood=ash", library), Is.SameAs(sprite), "no channel for it");
        }

        [Test]
        public void TheLibrary_OffersMinifantasyRampsForTheTavernsWoodAndCushion()
        {
            PaletteLibrary library = Database.palettes;
            Assert.That(library.For("wood").Count, Is.GreaterThanOrEqualTo(6));
            Assert.That(library.For("cushion").Count, Is.GreaterThanOrEqualTo(6));
            Assert.That(library.presets.Count, Is.GreaterThanOrEqualTo(4));
            foreach (PalettePreset p in library.presets)
            foreach (PalettePick pick in p.picks)
                Assert.That(library.Ramp(pick.ramp)?.kind, Is.EqualTo(pick.kind), $"{p.id}: {pick.ramp}");
            FurnitureDefinition chair = Database.Furniture("tavern_chair");
            Assert.That(chair.paletteChannels.Select(c => c.kind), Is.EquivalentTo(new[] { "wood", "cushion" }), "the prototype group");
        }

        // ------------------------------------------------------------------ finishes

        [Test]
        public void AFinish_RepeatsItsPattern_WithItsEdgeColumns()
        {
            var finish = ScriptableObject.CreateInstance<FinishDefinition>();
            Tile T(string n)
            {
                var t = ScriptableObject.CreateInstance<Tile>();
                t.name = n;
                return t;
            }
            Tile a = T("a"), b = T("b"), left = T("left"), right = T("right");
            finish.pattern = new Vector2Int(2, 1);
            finish.tiles = new List<TileBase> { a, b };
            finish.leftEdge = new List<TileBase> { left };
            finish.rightEdge = new List<TileBase> { right };
            Assert.That(finish.TileAt(0, 0, 6), Is.SameAs(left));
            Assert.That(finish.TileAt(1, 3, 6), Is.SameAs(b));
            Assert.That(finish.TileAt(2, 1, 6), Is.SameAs(a));
            Assert.That(finish.TileAt(5, 0, 6), Is.SameAs(right));
        }

        [Test]
        public void TheFinishes_IncludeEachAreasOwn_AndPanellingRecolouredThroughTheWoodRamps()
        {
            Assert.That(Database.finishes.Count(f => f.kind == FinishKind.Floor), Is.GreaterThanOrEqualTo(5));
            Assert.That(Database.finishes.Count(f => f.kind == FinishKind.Wall), Is.GreaterThanOrEqualTo(5));
            FinishDefinition walnut = Database.Finish("wall_panelling_walnut");
            Assert.That(walnut.channels.Single().kind, Is.EqualTo("wood"));
            Assert.That(Database.palettes.Ramp("walnut"), Is.Not.Null);
            foreach (FinishDefinition f in Database.finishes)
                Assert.That(f.tiles.All(t => t is Tile tile && tile.sprite != null), $"{f.id} has its tiles");
        }

        // ------------------------------------------------------------------ the property's areas

        static FurnitureStartingLayout TwoAreas()
        {
            var start = ScriptableObject.CreateInstance<FurnitureStartingLayout>();
            start.areas.Add(new AreaLayoutData { area = "tavern", pieces = { new PlacedFurniture { uid = 1, definition = "chair" }, new PlacedFurniture { uid = 2, definition = "table" } }, floor = "f1", wall = "w1" });
            start.areas.Add(new AreaLayoutData
            {
                area = "guest_room", floor = "f2", wall = "w2",
                pieces = { new PlacedFurniture { uid = 101, definition = "bed" }, new PlacedFurniture { uid = 102, definition = "candle", host = 101 }, new PlacedFurniture { uid = 103, definition = "chair" } },
            });
            start.finishes = new List<string> { "f1", "w1", "f2", "w2" };
            return start;
        }

        [Test]
        public void StorageIsPropertyWide_CountingWhatsPlacedInEveryArea()
        {
            var furniture = new FurnitureState();
            furniture.GrantStarter(TwoAreas());
            Assert.That(furniture.OwnedCount("chair"), Is.EqualTo(2));
            Assert.That(furniture.PlacedCount("chair"), Is.EqualTo(2), "one in each area");
            furniture.SetLayout("guest_room", furniture.Layout("guest_room").Where(p => p.definition != "chair").ToList());
            Assert.That(furniture.InStorage("chair"), Is.EqualTo(1), "taken from the guest room, into the one storage");
            Assert.That(furniture.Finish("guest_room", FinishKind.Floor), Is.EqualTo("f2"));
            Assert.That(furniture.OwnsFinish("w1") && furniture.OwnsFinish("w2"));
        }

        [Test]
        public void ASaveWithoutTheGuestRoom_GetsItsStartingFurniture_Once_WithFreshUids()
        {
            var furniture = new FurnitureState();
            FurnitureStartingLayout start = TwoAreas();
            furniture.Restore(new[] { ("chair", 3), ("table", 1) }, new[] { ("tavern", new List<PlacedFurniture> { new() { uid = 101, definition = "chair" }, new() { uid = 102, definition = "table" } }) }, 103);
            furniture.GrantMissing(start);
            IReadOnlyList<PlacedFurniture> guest = furniture.Layout("guest_room");
            Assert.That(guest.Count, Is.EqualTo(3));
            Assert.That(guest.Select(p => p.uid).Intersect(new[] { 101, 102 }), Is.Empty, "no uid clashes with the tavern's");
            PlacedFurniture bed = guest.Single(p => p.definition == "bed"), candle = guest.Single(p => p.definition == "candle");
            Assert.That(candle.host, Is.EqualTo(bed.uid), "the candle still stands on the bed's surface");
            Assert.That(furniture.OwnedCount("chair"), Is.EqualTo(4), "the guest room's chair is a new copy; storage keeps the tavern's spare");
            Assert.That(furniture.Finish("guest_room", FinishKind.Wall), Is.EqualTo("w2"));
            Assert.That(furniture.Finish("tavern", FinishKind.Floor), Is.EqualTo("f1"), "the tavern's finishes filled in too");
            furniture.GrantMissing(start);
            Assert.That(furniture.Layout("guest_room").Count, Is.EqualTo(3), "not twice");
            Assert.That(furniture.OwnedCount("chair"), Is.EqualTo(4));
        }

        [Test]
        public void TheGuestRoom_IsCheckedOnlyForItsDoorway()
        {
            var shape = new AreaShape { Id = "guest_room", Kind = AreaKind.GuestRoom, Bounds = new RectInt(0, 0, 8, 6), Floor = new RectInt(1, 1, 6, 4), Door = new Vector2(4.5f, 1.5f) };
            LayoutReport empty = LayoutCheck.For(shape, new List<ResolvedFurniture>());
            Assert.That(empty.Issues, Is.Empty, "no stations, pass or seats needed");
            Assert.That(empty.CanOpen);
            var blocker = Piece("crate");
            blocker.layer = FurnitureLayer.Standing;
            blocker.facings.Add(new FurnitureFacing { size = Vector2Int.one, bodies = { new Rect(0f, 0f, 1f, 1f) } });
            ResolvedFurniture onDoor = FurnitureGeometry.Resolve(blocker, new PlacedFurniture { definition = "crate", cell = new Vector2Int(4, 1) });
            LayoutReport blocked = LayoutCheck.For(shape, new List<ResolvedFurniture> { onDoor });
            Assert.That(blocked.Issues.Select(i => i.Kind), Is.EqualTo(new[] { LayoutIssueKind.EntranceBlocked }));
        }

        [Test]
        public void FixedFixtures_BlockTheWalkableGrid()
        {
            var shape = new AreaShape { Bounds = new RectInt(0, 0, 6, 6), Floor = new RectInt(0, 0, 6, 6) };
            shape.Fixtures.Add(new Rect(2f, 2f, 2f, 2f));
            Core.Pathfinding.GridMap map = LayoutCheck.Walkable(shape, new List<ResolvedFurniture>());
            Assert.That(map.IsWalkable(new Core.Pathfinding.GridCell(2, 2)), Is.False);
            Assert.That(map.IsWalkable(new Core.Pathfinding.GridCell(0, 0)));
        }

        // ------------------------------------------------------------------ seats and surfaces

        [Test]
        public void ABacklessStool_FacesWhicheverTableIsBesideIt()
        {
            var stool = new PlacedSeat(new Vector2(3.5f, 2f), Vector2.zero, Vector2Int.zero);
            var tableEast = new List<Rect> { new(4.1f, 2f, 1f, 1f) };
            Assert.That(FurnitureRules.TryFaceAny(stool, tableEast, out PlacedSeat faced));
            Assert.That(faced.Facing, Is.EqualTo(Vector2Int.right));
            Assert.That(faced.Approach, Is.EqualTo(new Vector2(3.5f, 1.1f)), "stepped onto from below");
            var tableBelow = new List<Rect> { new(3f, 0.5f, 1f, 1.2f) };
            Assert.That(FurnitureRules.TryFaceAny(stool, tableBelow, out faced));
            Assert.That(faced.Facing, Is.EqualTo(Vector2Int.down));
            Assert.That(FurnitureRules.TryFaceAny(stool, new List<Rect>(), out _), Is.False, "no table, no seat");
        }

        [Test]
        public void ASurfaceItemOnATable_SortsJustInFrontOfIt()
        {
            var table = Piece("table");
            table.function = FurnitureFunction.Table;
            table.facings.Add(new FurnitureFacing
            {
                size = Vector2Int.one, art = { new FurnitureArt { position = new Vector2(0.5f, 0f), sortingLayer = SortingLayers.YSorted } },
                bodies = { new Rect(0f, 0f, 1f, 1f) }, surfaces = { new Vector2(0.5f, 0.6f) },
            });
            var candle = Piece("candle");
            candle.layer = FurnitureLayer.Surface;
            candle.facings.Add(new FurnitureFacing { art = { new FurnitureArt { sortingLayer = SortingLayers.YSorted } } });
            var defs = new Dictionary<string, FurnitureDefinition> { ["table"] = table, ["candle"] = candle };
            var layout = new FurnitureLayout(new AreaShape { Bounds = new RectInt(0, 0, 4, 4), Floor = new RectInt(0, 0, 4, 4) }, id => defs.GetValueOrDefault(id),
                new[] { new PlacedFurniture { uid = 1, definition = "table", cell = new Vector2Int(1, 1) }, new PlacedFurniture { uid = 2, definition = "candle", host = 1 } });
            ResolvedFurniture r = layout.Resolve(layout.Find(2));
            Assert.That(r.SortingLayer, Is.EqualTo(SortingLayers.YSorted));
            Assert.That(r.Grouped, "sorted as one with its host");
            Assert.That(r.GroupPoint.y, Is.LessThan(1f), "just in front of the table's sort point, so never hidden behind it");
            Assert.That(r.Art[0].Position, Is.EqualTo(new Vector2(1.5f, 1.6f)), "drawn on the table top");
        }

        // ------------------------------------------------------------------ saves

        [Test]
        public void Version6_KeepsLooksFinishesAndBothAreas()
        {
            var state = new GameState();
            state.Furniture.GrantStarter(TwoAreas());
            var tavern = state.Furniture.Layout("tavern").Select(p => p.Clone()).ToList();
            tavern[0].variant = "slate";
            tavern[0].palette = "cushion=teal;wood=walnut";
            state.Furniture.SetLayout("tavern", tavern);
            state.Furniture.SetFinish("tavern", FinishKind.Wall, "w2");
            state.Furniture.OwnFinish("parquet");
            state.Furniture.AnnouncedTier = 2;

            string json = SaveSystem.ToJson(SaveSystem.Capture(state));
            Assert.That(json, Does.Contain("\"version\": 6"));
            GameState back = SaveSystem.Restore(SaveSystem.FromJson(json), _ => null, _ => true);
            PlacedFurniture chair = back.Furniture.Layout("tavern").Single(p => p.uid == tavern[0].uid);
            Assert.That(chair.variant, Is.EqualTo("slate"));
            Assert.That(chair.palette, Is.EqualTo("cushion=teal;wood=walnut"));
            Assert.That(back.Furniture.Finish("tavern", FinishKind.Wall), Is.EqualTo("w2"));
            Assert.That(back.Furniture.Finish("guest_room", FinishKind.Floor), Is.EqualTo("f2"));
            Assert.That(back.Furniture.OwnsFinish("parquet"));
            Assert.That(back.Furniture.AnnouncedTier, Is.EqualTo(2));
            Assert.That(back.Furniture.Layout("guest_room").Count, Is.EqualTo(3));
            Assert.That(back.Furniture.Layout("guest_room").Single(p => p.definition == "candle").host, Is.EqualTo(101));
        }

        [Test]
        public void AVersion5Save_LoadsWithItsTavernAsItWas_AndGainsTheGuestRoom()
        {
            const string v5 = "{\"version\":5,\"day\":3,\"phase\":\"Night\",\"gold\":80,\"renown\":30,\"furniture\":{\"initialized\":true,\"nextUid\":3," +
                              "\"owned\":[{\"id\":\"chair\",\"count\":1},{\"id\":\"table\",\"count\":1}]," +
                              "\"areas\":[{\"id\":\"tavern\",\"pieces\":[{\"uid\":1,\"def\":\"chair\",\"x\":4,\"y\":5,\"turns\":1,\"host\":-1},{\"uid\":2,\"def\":\"table\",\"x\":5,\"y\":5,\"host\":-1}]}]}}";
            SaveData data = SaveSystem.FromJson(v5);
            Assert.That(data.version, Is.EqualTo(6));
            Assert.That(data.furniture.tierAnnounced, Is.Zero, "tiers already reached are announced at the next Night");
            GameState state = SaveSystem.Restore(data, _ => null, _ => true, null, _ => true, TwoAreas());
            Assert.That(state.Furniture.Layout("tavern").Select(p => (p.definition, p.cell)), Is.EqualTo(new[] { ("chair", new Vector2Int(4, 5)), ("table", new Vector2Int(5, 5)) }));
            Assert.That(state.Furniture.Layout("tavern").All(p => p.variant == "" && p.palette == ""), "as drawn");
            Assert.That(state.Furniture.Layout("guest_room").Count, Is.EqualTo(3), "the guest room arrives furnished");
            Assert.That(state.Furniture.Finish("tavern", FinishKind.Floor), Is.EqualTo("f1"), "the tavern keeps its own floor");
            Assert.That(state.Gold, Is.EqualTo(80));
        }
    }
}
