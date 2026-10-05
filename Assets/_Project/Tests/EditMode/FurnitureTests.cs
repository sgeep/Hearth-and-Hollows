using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Save;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests
{
    /// <summary>4f step 1: furniture as data. The geometry transform, rotation modes (D2), flipping (D3), seats, ownership and the v4 save.</summary>
    public class FurnitureTests
    {
        readonly List<Object> m_Made = new();

        [TearDown]
        public void Clean()
        {
            foreach (Object o in m_Made) Object.DestroyImmediate(o);
            m_Made.Clear();
        }

        FurnitureDefinition Def(string id, RotationMode rotation, params FurnitureFacing[] facings)
        {
            var d = ScriptableObject.CreateInstance<FurnitureDefinition>();
            d.id = id;
            d.rotation = rotation;
            d.facings = facings.ToList();
            m_Made.Add(d);
            return d;
        }

        static void AreClose(Vector2 actual, Vector2 expected, string what = null) =>
            Assert.That(Vector2.Distance(actual, expected), Is.LessThan(1e-4f), $"{what}: {actual} vs {expected}");

        static void AreClose(Rect actual, Rect expected, string what = null)
        {
            AreClose(actual.min, expected.min, what + " min");
            AreClose(actual.max, expected.max, what + " max");
        }

        // ---------- The transform ----------

        [Test]
        public void QuarterTurns_RotateCounterClockwise_InsideTheTurnedBox()
        {
            var t = new FurnitureTransform(new Vector2Int(3, 1), 1, false);
            Assert.That(t.TurnedSize, Is.EqualTo(new Vector2Int(1, 3)));
            AreClose(t.Point(new Vector2(0f, 0f)), new Vector2(1f, 0f), "bottom-left goes to bottom-right");
            AreClose(t.Point(new Vector2(3f, 0f)), new Vector2(1f, 3f), "bottom-right goes to top-right");
            AreClose(t.Point(new Vector2(0.5f, 0.25f)), new Vector2(0.75f, 0.5f));
            Assert.That(t.Direction(new Vector2Int(0, -1)), Is.EqualTo(new Vector2Int(1, 0)), "facing south turns to east");
            Assert.That(t.Degrees, Is.EqualTo(90f));
        }

        [Test]
        public void FourQuarterTurns_AreTheIdentity_AndTwoAreAHalfTurn()
        {
            var size = new Vector2Int(3, 2);
            var p = new Vector2(0.75f, 1.25f);
            Vector2 q = p;
            for (int i = 0; i < 4; i++) q = new FurnitureTransform(i % 2 == 0 ? size : new Vector2Int(2, 3), 1, false).Point(q);
            AreClose(q, p, "four turns");
            AreClose(new FurnitureTransform(size, 2, false).Point(p), new Vector2(3f - 0.75f, 2f - 1.25f), "half turn");
            Assert.That(new FurnitureTransform(size, 6, false).Turns, Is.EqualTo(2), "turns wrap");
            Assert.That(new FurnitureTransform(size, -1, false).Turns, Is.EqualTo(3));
        }

        [Test]
        public void Flipping_MirrorsBeforeTurning_AndRectsStayOrdered()
        {
            var flip = new FurnitureTransform(new Vector2Int(2, 1), 0, true);
            AreClose(flip.Point(new Vector2(0.5f, 0.5f)), new Vector2(1.5f, 0.5f));
            Assert.That(flip.Direction(new Vector2Int(1, 0)), Is.EqualTo(new Vector2Int(-1, 0)), "east becomes west");
            AreClose(flip.Rect(new Rect(0f, 0f, 0.5f, 1f)), new Rect(1.5f, 0f, 0.5f, 1f), "a mirrored rect keeps min below max");
            var both = new FurnitureTransform(new Vector2Int(2, 1), 1, true);
            // Mirror (0.5,0.5) → (1.5,0.5), then turn in a 2×1 box: (1 − 0.5, 1.5).
            AreClose(both.Point(new Vector2(0.5f, 0.5f)), new Vector2(0.5f, 1.5f));
        }

        // ---------- Rotation modes (D2) ----------

        static FurnitureFacing Facing(int turns = 0, Vector2Int? size = null) => new() { turns = turns, size = size ?? Vector2Int.one };

        [Test]
        public void RotationModes_OfferTheRightTurns()
        {
            Assert.That(Def("fixed", RotationMode.None, Facing()).AllowedTurns(), Is.EqualTo(new[] { 0 }));
            Assert.That(Def("turned", RotationMode.QuarterTurnSprite, Facing()).AllowedTurns(), Is.EqualTo(new[] { 0, 1, 2, 3 }));
            FurnitureDefinition chair = Def("chair", RotationMode.AuthoredFacings, Facing(1), Facing(3));
            Assert.That(chair.AllowedTurns(), Is.EqualTo(new[] { 1, 3 }), "only the drawn facings");
            Assert.That(chair.NextTurns(1), Is.EqualTo(3));
            Assert.That(chair.NextTurns(3), Is.EqualTo(1));
            Assert.That(chair.TryGetFacing(2, out _, out _), Is.False, "an undrawn turn can't be stood at");
            Assert.That(chair.TryGetFacing(3, out FurnitureFacing west, out int rotate) && west.turns == 3 && rotate == 0, "an authored facing is already drawn turned");
            Assert.That(Def("q", RotationMode.QuarterTurnSprite, Facing()).TryGetFacing(3, out _, out int r) && r == 3, "a quarter-turn piece turns its one drawing");
            Assert.That(Def("n", RotationMode.None, Facing()).TryGetFacing(1, out _, out _), Is.False);
        }

        /// <summary>A real quarter turn moves everything together: footprint, bodies, seats, use points, staff post, surfaces, lights, art.</summary>
        [Test]
        public void AQuarterTurnedPiece_TurnsItsWholeGeometry()
        {
            var bench = new FurnitureFacing
            {
                size = new Vector2Int(3, 1),
                art = { new FurnitureArt { name = "Bench", position = new Vector2(1.5f, 0f) } },
                bodies = { new Rect(0f, 0f, 3f, 0.75f) },
                seats = { new FurnitureSeat { position = new Vector2(0.5f, 0.25f), approach = new Vector2(0.5f, -0.9f), facing = new Vector2Int(0, 1) } },
                interactPoint = new Vector2(1.5f, 0f),
                usePoints = { new Vector2(1.5f, -0.5f) },
                hasStaffPost = true,
                staffPost = new Vector2(2.5f, -0.5f),
                surfaces = { new Vector2(2.5f, 0.5f) },
                lights = { new FurnitureLight { position = new Vector2(0f, 1f) } },
                highlight = new Rect(0f, 0f, 3f, 1f),
            };
            FurnitureDefinition d = Def("bench", RotationMode.QuarterTurnSprite, bench);
            var placed = new PlacedFurniture { definition = "bench", cell = new Vector2Int(10, 5), turns = 1 };
            ResolvedFurniture r = FurnitureGeometry.Resolve(d, placed, new Vector2(100f, 0f));

            Assert.That(r.Footprint, Is.EqualTo(new RectInt(10, 5, 1, 3)), "a 3×1 footprint stands 1×3");
            var origin = new Vector2(110f, 5f);
            AreClose(r.Bodies.Single(), new Rect(origin + new Vector2(0.25f, 0f), new Vector2(0.75f, 3f)), "body");
            AreClose(r.Seats.Single().Position, origin + new Vector2(0.75f, 0.5f), "seat");
            AreClose(r.Seats.Single().Approach, origin + new Vector2(1.9f, 0.5f), "approach");
            Assert.That(r.Seats.Single().Facing, Is.EqualTo(new Vector2Int(-1, 0)), "facing north turns to west");
            AreClose(r.UsePoints.Single(), origin + new Vector2(1.5f, 1.5f), "use point");
            AreClose(r.StaffPost, origin + new Vector2(1.5f, 2.5f), "staff post");
            AreClose(r.Surfaces.Single(), origin + new Vector2(0.5f, 2.5f), "surface");
            AreClose(r.Lights.Single().Position, origin + new Vector2(0f, 0f), "light");
            AreClose(r.Art.Single().Position, origin + new Vector2(1f, 1.5f), "art pivot");
            Assert.That(r.Art.Single().Degrees, Is.EqualTo(90f));
            AreClose(r.Highlight, new Rect(origin, new Vector2(1f, 3f)), "highlight");
            Assert.That(r.Grouped, "a turned drawing sorts as one");
            AreClose(r.GroupPoint, origin + new Vector2(0.5f, 0f), "at the bottom-centre of what it covers");
        }

        [Test]
        public void Flipping_IsOptInPerPiece_AndMirrorsTheGeometry()
        {
            var facing = new FurnitureFacing
            {
                size = new Vector2Int(2, 1),
                art = { new FurnitureArt { position = new Vector2(0.5f, 0f) } },
                seats = { new FurnitureSeat { position = new Vector2(0.5f, 0f), facing = new Vector2Int(1, 0) } },
            };
            FurnitureDefinition d = Def("settle", RotationMode.None, facing);
            var placed = new PlacedFurniture { definition = "settle", cell = Vector2Int.zero, flipped = true };
            Assert.That(FurnitureGeometry.Resolve(d, placed), Is.Null, "D3: off by default");
            d.flippable = true;
            ResolvedFurniture r = FurnitureGeometry.Resolve(d, placed);
            AreClose(r.Art.Single().Position, new Vector2(1.5f, 0f));
            Assert.That(r.Art.Single().FlipX);
            Assert.That(r.Seats.Single().Facing, Is.EqualTo(new Vector2Int(-1, 0)));
        }

        [Test]
        public void TheNudge_MovesEverythingByArtPixels_WithinHalfATile()
        {
            FurnitureDefinition d = Def("chair", RotationMode.None, new FurnitureFacing
            {
                art = { new FurnitureArt { position = new Vector2(0.5f, 0f) } },
                bodies = { new Rect(0.125f, 0f, 0.75f, 1f) },
            });
            ResolvedFurniture r = FurnitureGeometry.Resolve(d, new PlacedFurniture { definition = "chair", cell = new Vector2Int(3, 7), nudge = new Vector2Int(-2, 2) });
            AreClose(r.Art.Single().Position, new Vector2(3.25f, 7.25f), "the 4e chair west of the first table");
            AreClose(r.Bodies.Single(), new Rect(2.875f, 7.25f, 0.75f, 1f), "its body");
            Assert.That(r.Footprint, Is.EqualTo(new RectInt(3, 7, 1, 1)), "the footprint stays on whole tiles (D1)");
            Assert.That(FurnitureGeometry.IsValidNudge(new Vector2Int(-4, 3)));
            Assert.That(FurnitureGeometry.IsValidNudge(new Vector2Int(4, 0)), Is.False, "half a tile is the next cell");
        }

        // ---------- Seats (D16) ----------

        [Test]
        public void ASeatCounts_OnlyWhenItFacesATable()
        {
            var tables = new List<Rect> { new(3.75f, 7f, 1.5f, 1.5f) };
            Assert.That(FurnitureRules.FacesTable(new PlacedSeat(new Vector2(3.25f, 7.25f), default, new Vector2Int(1, 0)), tables), "facing it, beside it");
            Assert.That(FurnitureRules.FacesTable(new PlacedSeat(new Vector2(3.25f, 7.25f), default, new Vector2Int(-1, 0)), tables), Is.False, "facing away");
            Assert.That(FurnitureRules.FacesTable(new PlacedSeat(new Vector2(1.5f, 7.25f), default, new Vector2Int(1, 0)), tables), Is.False, "too far");
            Assert.That(FurnitureRules.FacesTable(new PlacedSeat(new Vector2(4.5f, 6.25f), default, new Vector2Int(0, 1)), tables), "below it, facing north");
        }

        // ---------- Ownership ----------

        FurnitureStartingLayout Start()
        {
            var start = ScriptableObject.CreateInstance<FurnitureStartingLayout>();
            m_Made.Add(start);
            start.areas.Add(new AreaLayoutData
            {
                area = "tavern",
                pieces =
                {
                    new PlacedFurniture { uid = 1, definition = "table", cell = new Vector2Int(4, 7) },
                    new PlacedFurniture { uid = 2, definition = "chair", cell = new Vector2Int(3, 7), turns = 1, nudge = new Vector2Int(-2, 2) },
                    new PlacedFurniture { uid = 3, definition = "chair", cell = new Vector2Int(5, 7), turns = 3 },
                },
            });
            start.storage.Add(new OwnedFurnitureData { definition = "chair", count = 1 });
            return start;
        }

        [Test]
        public void TheStartingFurniture_IsOwned_AsPlacedPlusStorage()
        {
            var state = new FurnitureState();
            Assert.That(state.Initialized, Is.False);
            state.GrantStarter(Start());
            Assert.That(state.Initialized);
            Assert.That(state.OwnedCount("chair"), Is.EqualTo(3), "two placed, one in storage (D7: copies)");
            Assert.That(state.PlacedCount("chair"), Is.EqualTo(2));
            Assert.That(state.InStorage("chair"), Is.EqualTo(1));
            Assert.That(state.InStorage("table"), Is.Zero);
            Assert.That(state.Layout("tavern").Count, Is.EqualTo(3));
            Assert.That(state.Layout("guest_room_1"), Is.Empty);
            Assert.That(state.NextUid, Is.EqualTo(4));
            state.SetLayout("tavern", state.Layout("tavern").Where(p => p.uid != 3).ToList());
            Assert.That(state.InStorage("chair"), Is.EqualTo(2), "removing a piece puts it in storage, never destroys it (D12)");
        }

        [Test]
        public void TheStartingLayout_IsCopied_NotShared()
        {
            FurnitureStartingLayout start = Start();
            var state = new FurnitureState();
            state.GrantStarter(start);
            state.Layout("tavern")[0].cell = new Vector2Int(20, 3);
            Assert.That(start.areas[0].pieces[0].cell, Is.EqualTo(new Vector2Int(4, 7)), "moving the game's table never moves the content's");
        }

        // ---------- The save (v4) ----------

        static GameState Restore(string json, FurnitureStartingLayout start = null, List<string> warnings = null) =>
            SaveSystem.Restore(SaveSystem.FromJson(json), _ => null, id => id is "satchel_slots" or "max_essence", warnings, id => id is "table" or "chair", start);

        [Test]
        public void Furniture_SurvivesTheSave()
        {
            var state = new GameState();
            state.Furniture.GrantStarter(Start());
            string json = SaveSystem.ToJson(SaveSystem.Capture(state));
            Assert.That(json, Does.Contain("\"version\": 6"));
            GameState loaded = Restore(json);
            Assert.That(loaded.Furniture.Initialized);
            Assert.That(loaded.Furniture.OwnedCount("chair"), Is.EqualTo(3));
            PlacedFurniture chair = loaded.Furniture.Layout("tavern").Single(p => p.uid == 2);
            Assert.That((chair.definition, chair.cell, chair.turns, chair.nudge), Is.EqualTo(("chair", new Vector2Int(3, 7), 1, new Vector2Int(-2, 2))));
            Assert.That(loaded.Furniture.NextUid, Is.EqualTo(4));
        }

        [Test]
        public void UnknownFurniture_IsDropped_WithWarnings()
        {
            var state = new GameState();
            state.Furniture.GrantStarter(Start());
            state.Furniture.SetLayout("tavern", state.Furniture.Layout("tavern").Append(new PlacedFurniture { uid = 9, definition = "vanished_lamp" }).ToList());
            var warnings = new List<string>();
            GameState loaded = Restore(SaveSystem.ToJson(SaveSystem.Capture(state)), warnings: warnings);
            Assert.That(loaded.Furniture.Layout("tavern").Count, Is.EqualTo(3));
            Assert.That(warnings.Any(w => w.Contains("vanished_lamp")));
        }

        const string k_Version3 = "{\"version\":3,\"day\":9,\"phase\":\"Night\",\"gold\":50,\"renown\":12,\"storeroom\":[]," +
                                  "\"upgrades\":[{\"id\":\"max_essence\",\"level\":1},{\"id\":\"tavern_seats\",\"level\":LEVEL}]," +
                                  "\"meal\":{\"kind\":\"None\",\"amount\":0,\"recipe\":\"\"},\"bosses\":[{\"id\":\"larder_troll\",\"clears\":1}]}";

        [Test]
        public void AVersion4Save_KeepsItsBarrelsWhereTheyStood_AndPutsItsGlassesOnTheirShelf()
        {
            const string v4 = "{\"version\":4,\"day\":2,\"phase\":\"Night\",\"gold\":5,\"renown\":0,\"storeroom\":[],\"upgrades\":[],\"meal\":{\"kind\":\"None\"},\"bosses\":[]," +
                              "\"furniture\":{\"initialized\":true,\"nextUid\":5,\"owned\":[{\"id\":\"cellar_barrel\",\"count\":2},{\"id\":\"low_shelf\",\"count\":1},{\"id\":\"shelf_glasses\",\"count\":2}]," +
                              "\"areas\":[{\"id\":\"tavern\",\"pieces\":[" +
                              "{\"uid\":1,\"def\":\"cellar_barrel\",\"x\":25,\"y\":8,\"nx\":-2,\"host\":-1}," +
                              "{\"uid\":2,\"def\":\"cellar_barrel\",\"x\":12,\"y\":10,\"nx\":0,\"host\":-1}," +
                              "{\"uid\":3,\"def\":\"low_shelf\",\"x\":6,\"y\":14,\"host\":-1}," +
                              "{\"uid\":4,\"def\":\"shelf_glasses\",\"x\":6,\"y\":15,\"host\":-1}," +
                              "{\"uid\":5,\"def\":\"shelf_glasses\",\"x\":20,\"y\":16,\"host\":-1}]}]}}";
            SaveData data = SaveSystem.FromJson(v4);
            Assert.That(data.version, Is.EqualTo(SaveSystem.CurrentVersion));
            List<PieceData> pieces = data.furniture.areas.Single().pieces;
            Assert.That(pieces.Single(p => p.uid == 1).nx, Is.Zero, "the 4e barrel: still at 25.25");
            Assert.That(pieces.Single(p => p.uid == 2).nx, Is.EqualTo(2), "a moved barrel: still at 12.5");
            Assert.That((pieces.Single(p => p.uid == 4).host, pieces.Single(p => p.uid == 4).anchor), Is.EqualTo((3, 0)), "glasses on their shelf");
            Assert.That(pieces.Any(p => p.uid == 5), Is.False, "glasses on no shelf go to storage");
        }

        [TestCase(0, 0)]
        [TestCase(1, 120)]
        [TestCase(2, 340)]
        public void AVersion3Save_GetsTheStartingFurniture_AndItsSeatUpgradeRefunded(int levels, int refund)
        {
            string json = k_Version3.Replace("LEVEL", levels.ToString());
            SaveData data = SaveSystem.FromJson(json);
            Assert.That(data.version, Is.EqualTo(SaveSystem.CurrentVersion));
            Assert.That(data.upgrades.Select(u => u.id), Is.EqualTo(new[] { "max_essence" }), "the retired seat upgrade is gone (D16)");
            Assert.That(data.gold, Is.EqualTo(50 + refund), "its Gold is back in the purse");
            GameState state = SaveSystem.Restore(data, _ => null, id => id == "max_essence", null, id => id is "table" or "chair", Start());
            Assert.That(state.Furniture.Initialized, "the starting furniture is granted as it loads");
            Assert.That(state.Furniture.Layout("tavern").Count, Is.EqualTo(3));
            Assert.That((state.Day, state.Renown, state.TimesDefeated("larder_troll"), state.UpgradeLevel("max_essence")), Is.EqualTo((9, 12, 1, 1)), "everything else as it was");
            Assert.That(SaveSystem.SeatUpgradeRefund(levels), Is.EqualTo(refund));
        }

        [Test]
        public void OlderSaves_MigrateStepByStep_ToTheCurrentVersion()
        {
            SaveData fromV1 = SaveSystem.FromJson("{\"version\":1,\"day\":2,\"gold\":10,\"storeroom\":[]}");
            Assert.That(fromV1.version, Is.EqualTo(SaveSystem.CurrentVersion));
            Assert.That(fromV1.bosses, Is.Empty);
            Assert.That(fromV1.furniture.initialized, Is.False);
            SaveData fromV2 = SaveSystem.FromJson("{\"version\":2,\"day\":5,\"phase\":\"Night\",\"gold\":140,\"renown\":3,\"storeroom\":[],\"upgrades\":[{\"id\":\"tavern_seats\",\"level\":1}],\"meal\":{\"kind\":\"None\"}}");
            Assert.That(fromV2.version, Is.EqualTo(SaveSystem.CurrentVersion));
            Assert.That(fromV2.gold, Is.EqualTo(260), "a version 2 save with a seat level is refunded too");
        }

        /// <summary>
        /// Real content: a version 2 save as the 4d build wrote it, and a version 3 save from a 4e game that bought a seat
        /// level, load through the game database: the starting furniture arrives, every piece resolves, the seat level's
        /// Gold comes back, and nothing about the day is lost.
        /// </summary>
        [TestCase("{\"version\":2,\"day\":2,\"phase\":\"Morning\",\"gold\":39,\"renown\":2,\"storeroom\":[{\"ingredient\":\"slime_gel\",\"quality\":\"Poor\",\"prep\":\"Raw\",\"count\":5,\"freshness\":0.899}],\"upgrades\":[],\"meal\":{\"kind\":\"None\",\"amount\":0.0,\"recipe\":\"\"}}", 39, 5)]
        [TestCase("{\"version\":3,\"day\":6,\"phase\":\"Night\",\"gold\":12,\"renown\":30,\"storeroom\":[{\"ingredient\":\"spider_leg\",\"quality\":\"Fine\",\"prep\":\"Raw\",\"count\":2,\"freshness\":1.0}],\"upgrades\":[{\"id\":\"satchel_slots\",\"level\":1},{\"id\":\"tavern_seats\",\"level\":1}],\"meal\":{\"kind\":\"None\",\"amount\":0.0,\"recipe\":\"\"},\"bosses\":[{\"id\":\"larder_troll\",\"clears\":2}]}", 132, 2)]
        public void OldSaves_LoadWithTheGameDatabase_AndGetTheStartingTavern(string json, int gold, int parts)
        {
            var database = UnityEditor.AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/_Project/Data/GameDatabase.asset");
            Assert.That(database.startingFurniture, Is.Not.Null);
            var warnings = new List<string>();
            GameState state = SaveSystem.Restore(SaveSystem.FromJson(json), database, warnings);
            Assert.That(warnings, Is.Empty, string.Join("\n", warnings));
            Assert.That(state.Gold, Is.EqualTo(gold));
            Assert.That(state.Storeroom.TotalCount, Is.EqualTo(parts));
            IReadOnlyList<PlacedFurniture> tavern = state.Furniture.Layout("tavern");
            Assert.That(tavern.Count, Is.EqualTo(database.startingFurniture.Layout("tavern").Count));
            var layout = new FurnitureLayout(new AreaShape(), database.Furniture, tavern);
            foreach (PlacedFurniture p in tavern)
                Assert.That(layout.Resolve(p), Is.Not.Null, $"{p.definition}#{p.uid} resolves");
            Assert.That(tavern.Count(p => p.definition == "tavern_chair"), Is.EqualTo(6), "six seats, as a new 4e game had");
            Assert.That(database.Upgrade(SaveSystem.RetiredSeatUpgrade), Is.Null, "the seat upgrade is gone from the game");

            // And it saves as the current version, with the furniture, and loads back the same.
            GameState again = SaveSystem.Restore(SaveSystem.FromJson(SaveSystem.ToJson(SaveSystem.Capture(state))), database);
            Assert.That(again.Furniture.Layout("tavern").Select(p => (p.definition, p.cell, p.turns, p.nudge)),
                Is.EqualTo(tavern.Select(p => (p.definition, p.cell, p.turns, p.nudge))));
        }

        [Test]
        public void TheStartingLayout_OnlyNamesKnownPieces_WithUniqueUids_InsideTheTavernsFloorOrWall()
        {
            var database = UnityEditor.AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/_Project/Data/GameDatabase.asset");
            IReadOnlyList<PlacedFurniture> tavern = database.startingFurniture.Layout("tavern");
            Assert.That(tavern.Select(p => p.uid).Distinct().Count(), Is.EqualTo(tavern.Count), "unique uids");
            var floor = new RectInt(1, 2, 26, 12);
            var wall = new RectInt(1, 14, 26, 3);
            var standing = new List<RectInt>();
            var layout = new FurnitureLayout(new AreaShape(), database.Furniture, tavern);
            foreach (PlacedFurniture p in tavern)
            {
                FurnitureDefinition d = database.Furniture(p.definition);
                Assert.That(d, Is.Not.Null, p.definition);
                ResolvedFurniture r = layout.Resolve(p);
                Assert.That(r, Is.Not.Null, $"{p.definition}#{p.uid} resolves");
                if (d.layer == FurnitureLayer.Surface) continue;
                Assert.That(FurnitureGeometry.IsValidNudge(p.nudge), $"{p.definition}#{p.uid}: nudge within half a tile");
                RectInt area = d.layer == FurnitureLayer.Wall ? wall : floor;
                Assert.That(area.Contains(r.Footprint.min) && area.Contains(r.Footprint.max - Vector2Int.one), $"{p.definition}#{p.uid} stands inside its area");
                if (d.BlocksMovement)
                {
                    Assert.That(standing.Any(o => o.Overlaps(r.Footprint)), Is.False, $"{p.definition}#{p.uid}: footprints never overlap (D1)");
                    standing.Add(r.Footprint);
                }
            }
        }

        [Test]
        public void ASaveFromANewerGame_IsRefused()
        {
            Assert.Throws<System.NotSupportedException>(() => SaveSystem.FromJson("{\"version\":7}"));
        }
    }
}
