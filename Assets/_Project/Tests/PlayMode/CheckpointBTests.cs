using System.Collections;
using System.Linq;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Tavern;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4f Checkpoint B in the tavern scene: buying from the catalogue onto the cursor, storing and selling; tiers opening
    /// with Renown; recolouring a piece (and undoing it), the colour panel and matching every copy; laying a floor finish
    /// and putting it back; decorating the guest room by switching areas; the stairs and the guest room's door; and a
    /// service in a tavern furnished quite differently from the start.
    /// </summary>
    public class CheckpointBTests : LookTestFixture
    {
        static DecorateMode Mode => DecorateMode.Instance;
        static AreaFurniture Tavern => AreaFurniture.Tavern;
        static AreaFurniture Guest => AreaFurniture.Find(PropertyArea.GuestRoomId);
        static TavernDirector Director => TavernDirector.Instance;
        static DecorateScreen Screen => Object.FindAnyObjectByType<DecorateScreen>();

        IEnumerator LoadTavern()
        {
            yield return Load("Tavern");
            yield return WaitUntil(() => Loc.IsReady, 10f, "the string tables");
            yield return null;
        }

        [TearDown]
        public void NoSandbox() => DecorateMode.Sandbox = false;

        static void SelectPiece(DecorateCatalogue catalogue, string id)
        {
            for (int i = 0; i < catalogue.Count; i++)
            {
                catalogue.Select(i);
                if (catalogue.SelectedPiece != null && catalogue.SelectedPiece.id == id) return;
            }
            Assert.Fail($"{id} isn't on this page");
        }

        [UnityTest]
        public IEnumerator BuyingFromTheCatalogue_PutsItOnTheCursor_ThenStorage_AndSellingGivesHalfBack()
        {
            yield return LoadTavern();
            Tavern.Game.AddGold(100);
            Mode.Enter();
            yield return null;
            DecorateCatalogue catalogue = Screen.Catalogue;
            catalogue.OpenCategory(FurnitureCategory.Tables);
            Assert.That(catalogue.IsOpen && Mode.PanelOpen);
            SelectPiece(catalogue, "tavern_table_small");
            catalogue.Primary();
            Assert.That(catalogue.IsOpen, Is.False, "bought and onto the cursor");
            Assert.That(Mode.Carried?.definition, Is.EqualTo("tavern_table_small"));
            Assert.That(Tavern.Game.Gold, Is.EqualTo(80));
            Mode.SetCursor(new Vector2Int(14, 6));
            Mode.Place();
            Assert.That(Mode.Carried, Is.Null);
            Assert.That(Mode.Placed("tavern_table_small"), Is.EqualTo(1));
            Assert.That(Mode.Stored("tavern_table_small"), Is.Zero);

            catalogue.OpenCategory(FurnitureCategory.Tables);
            SelectPiece(catalogue, "tavern_table_small");
            catalogue.Secondary();
            Assert.That(Mode.Stored("tavern_table_small"), Is.EqualTo(1), "X buys one into storage");
            Assert.That(Tavern.Game.Gold, Is.EqualTo(60));
            catalogue.Tertiary();
            Assert.That(Mode.Stored("tavern_table_small"), Is.Zero, "Y sells one from storage");
            Assert.That(Tavern.Game.Gold, Is.EqualTo(70), "for half its price");
            catalogue.Tertiary();
            Assert.That(Tavern.Game.Gold, Is.EqualTo(70), "the placed one isn't sold from the room");
            catalogue.Close();
            Mode.Leave();
            Assert.That(Tavern.State.OwnedCount("tavern_table_small"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ACollection_StaysShut_UntilRenownOpensItsTier()
        {
            yield return LoadTavern();
            Tavern.Game.AddGold(500);
            Mode.Enter();
            yield return null;
            FurnitureDefinition chair = Tavern.Definition("dwarven_chair");
            Assert.That(Mode.CanBuy(chair), Is.EqualTo(PurchaseProblem.Locked));
            DecorateCatalogue catalogue = Screen.Catalogue;
            catalogue.OpenCategory(FurnitureCategory.Seating);
            SelectPiece(catalogue, "dwarven_chair");
            catalogue.Primary();
            Assert.That(catalogue.MessageText, Does.Contain("needs Renown 25"));
            Assert.That(Tavern.State.OwnedCount("dwarven_chair"), Is.Zero);
            Tavern.Game.AddRenown(25);
            catalogue.Secondary();
            Assert.That(Tavern.State.OwnedCount("dwarven_chair"), Is.EqualTo(1), "the dwarven masons take orders at 25");
            Assert.That(Tavern.Game.Renown, Is.EqualTo(25), "Renown isn't spent");
            Assert.That(Mode.CanBuy(Tavern.Definition("elven_chair")), Is.EqualTo(PurchaseProblem.Locked), "the elves want more");
            catalogue.Close();
            Mode.Leave();
        }

        static SpriteRenderer ArtOf(int uid) =>
            Tavern.GetComponentsInChildren<FurnitureView>().First(v => v.name.EndsWith("#" + uid)).GetComponentInChildren<SpriteRenderer>();

        [UnityTest]
        public IEnumerator Recolouring_ChangesThePiecesDrawing_AndUndoBringsItBack()
        {
            yield return LoadTavern();
            Mode.Enter();
            yield return null;
            PlacedFurniture chair = Mode.Layout.Pieces.First(p => p.definition == "tavern_chair");
            Sprite drawn = ArtOf(chair.uid).sprite;
            Mode.SetCursor(chair.cell);
            Assert.That(Mode.StyleTarget?.uid, Is.EqualTo(chair.uid));
            Assert.That(Mode.SetLook("", "cushion=teal;wood=walnut"));
            Sprite teal = ArtOf(chair.uid).sprite;
            Assert.That(teal, Is.Not.SameAs(drawn));
            Color32 tealRamp = Tavern.Palettes.Ramp("teal").colors[1];
            Assert.That(teal.texture.GetPixels32().Any(p => p.r == tealRamp.r && p.g == tealRamp.g && p.b == tealRamp.b), "the cushion is teal");
            Assert.That(ArtOf(chair.uid).sharedMaterial.name, Does.StartWith("Sprite-Lit-Default"), "still the lit sprite material (D11)");

            // The colour panel: wood, cushion and the schemes, then "use last colours" and "match every copy".
            Screen.OpenStyle();
            DecorateStyle style = Screen.Style;
            Assert.That(style.IsOpen);
            Assert.That(style.LineCount, Is.EqualTo(5), "wood, cushion, schemes, last colours, every copy");
            style.Choose(1, 0);
            Assert.That(Mode.StyleTarget.palette, Is.EqualTo("wood=walnut"), "cushion back to as drawn");
            style.ApplyAll();
            Assert.That(Mode.Layout.Pieces.Where(p => p.definition == "tavern_chair").All(p => p.palette == "wood=walnut"), "every chair here matches");
            style.Close();

            Mode.Undo();
            Mode.Undo();
            Mode.Undo();
            Assert.That(Mode.Layout.Pieces.First(p => p.uid == chair.uid).palette, Is.Empty);
            Assert.That(ArtOf(chair.uid).sprite, Is.SameAs(drawn), "undone: as drawn");
            Mode.Leave();
        }

        [UnityTest]
        public IEnumerator AFloorFinish_CoversTheWholeFloor_AndPuttingItAllBackRestoresIt()
        {
            yield return LoadTavern();
            DecorateMode.Sandbox = true;
            Tilemap floor = Tavern.Finishes.Floor;
            TileBase before = floor.GetTile(new Vector3Int(5, 5, 0));
            Mode.Enter();
            yield return null;
            DecorateCatalogue catalogue = Screen.Catalogue;
            catalogue.OpenRoom();
            for (int i = 0; i < catalogue.Count && catalogue.SelectedFinish?.id != "floor_plum"; i++) catalogue.Select(i);
            catalogue.Primary();
            Assert.That(Mode.Finish(FinishKind.Floor), Is.EqualTo("floor_plum"));
            TileBase plum = Tavern.Content.Finish("floor_plum").tiles[0];
            Assert.That(floor.GetTile(new Vector3Int(5, 5, 0)), Is.SameAs(plum));
            Assert.That(floor.GetTile(new Vector3Int(20, 10, 0)), Is.SameAs(plum), "the whole floor (D5)");
            catalogue.Close();
            Mode.PutAllBack();
            Assert.That(floor.GetTile(new Vector3Int(5, 5, 0)), Is.SameAs(before));
            Mode.Leave();
            Assert.That(Tavern.State.Finish("tavern", FinishKind.Floor), Is.EqualTo("floor_diamonds"));
        }

        [UnityTest]
        public IEnumerator DecoratingSwitchesToTheGuestRoom_WithTheSameStorage_AndKeepsItsLayout()
        {
            yield return LoadTavern();
            DecorateMode.Sandbox = true;
            Assert.That(Guest, Is.Not.Null, "the guest room is part of the property");
            Assert.That(Guest.Pieces.Select(p => p.Definition.id), Is.SupersetOf(new[] { "bed_double", "nightstand", "chest", "rug_village" }));
            Mode.Enter();
            yield return null;
            // A tavern chair to storage, then on to the guest room.
            PlacedFurniture chair = Mode.Layout.Pieces.First(p => p.definition == "tavern_chair");
            Mode.SetCursor(chair.cell);
            Mode.Store();
            Mode.SwitchArea();
            Assert.That(Mode.Area, Is.SameAs(Guest));
            Assert.That(Screen.TitleText, Does.Contain("the guest room"));
            Assert.That((Vector2)TavernView.Camera.position, Is.EqualTo(Guest.Area.CameraPoint), "the camera holds on the guest room");
            Assert.That(Mode.Stored("tavern_chair"), Is.EqualTo(1), "one storage for the whole property");
            Assert.That(Mode.TakeFromStorage("tavern_chair"));
            Mode.SetCursor(new Vector2Int(9, 6));
            Mode.Place();
            Assert.That(Mode.Carried, Is.Null, "the tavern's chair stands in the guest room");
            Assert.That(Mode.Report.Issues, Is.Empty, "only the doorway matters here");
            Mode.Leave();
            Assert.That(Guest.State.Layout("guest_room").Count(p => p.definition == "tavern_chair"), Is.EqualTo(1));
            Assert.That(Tavern.State.Layout("tavern").Count(p => p.definition == "tavern_chair"), Is.EqualTo(5));
            Assert.That((Vector2)TavernView.Camera.position, Is.EqualTo(Tavern.Area.CameraPoint), "back to where the keeper is");
        }

        [UnityTest]
        public IEnumerator TheStairs_LeadUpToTheGuestRoom_AndItsDoorLeadsBackDown()
        {
            yield return LoadTavern();
            Rigidbody2D keeper = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();
            AreaPassage up = Tavern.GetComponentsInChildren<AreaPassage>().Single();
            AreaPassage down = Guest.GetComponentsInChildren<AreaPassage>().Single();
            Assert.That(up.To, Is.SameAs(Guest.Area));
            Assert.That(down.To, Is.SameAs(Tavern.Area));

            // Walked onto the stairs' foot: the screen fades, and the keeper is upstairs.
            keeper.position = up.transform.position;
            yield return WaitUntil(() => PropertyArea.Current == Guest.Area, 3f, "the guest room");
            yield return WaitUntil(() => !up.Busy, 3f, "the fade back in");
            Assert.That(Vector2.Distance(keeper.position, Guest.Area.Arrival), Is.LessThan(0.6f));
            Assert.That((Vector2)TavernView.Camera.position, Is.EqualTo(Guest.Area.CameraPoint));

            // Decorating from upstairs decorates the guest room.
            Mode.Enter();
            yield return null;
            Assert.That(Mode.Area, Is.SameAs(Guest));
            Mode.Leave();

            down.Pass(keeper);
            Assert.That(PropertyArea.Current, Is.SameAs(Tavern.Area));
            Assert.That(Vector2.Distance(keeper.position, Tavern.Area.Arrival), Is.LessThan(0.01f));
            Assert.That((Vector2)TavernView.Camera.position, Is.EqualTo(Tavern.Area.CameraPoint));
        }

        [UnityTest]
        public IEnumerator AServiceRuns_InATavernFurnishedFromTheCatalogue()
        {
            yield return LoadTavern();
            var skipped = CheckpointBCaptures.Furnish(Tavern, "floor_chequer", "wall_panelling_walnut", true, CheckpointBCaptures.DwarvenHall);
            yield return null;
            Assert.That(skipped.Count, Is.LessThanOrEqualTo(1), string.Join("; ", skipped));
            Assert.That(Tavern.Report.CanOpen, string.Join(", ", Tavern.Report.Issues.Select(i => i.Kind)));
            Assert.That(Tavern.Seats.Count, Is.GreaterThanOrEqualTo(12), "a hall's worth of dwarven seating");
            Assert.That(Tavern.Pieces.Any(p => p.Definition.id == "tavern_chair"), Is.False, "none of the starting tables and chairs");

            Director.OpenDebugEvening();
            Director.ArrivalsPaused = true;
            for (int i = 0; i < 6; i++) Director.SpawnCustomer();
            Time.timeScale = 4f;
            float until = Time.time + 60f;
            while (Time.time < until && Director.Agents.Count(a => a.Logic.State is CustomerState.Ordering or CustomerState.WaitingForFood) < 6) yield return null;
            Time.timeScale = 1f;
            var grid = Hearthdelve.Shared.Navigation.NavGrid.Current;
            string seats = string.Join(" ", Tavern.Seats.Select(t => $"{t.ApproachPoint}:{grid.Map.IsWalkable(grid.Space.ToCell(t.ApproachPoint))}"));
            Assert.That(Director.Agents.Count(a => a.Logic.State is CustomerState.Ordering or CustomerState.WaitingForFood), Is.EqualTo(6),
                "six customers seated at the dwarven tables: " + string.Join(", ", Director.Agents.Select(a => a.Logic.State)) + " | seats " + seats);
        }
    }
}
