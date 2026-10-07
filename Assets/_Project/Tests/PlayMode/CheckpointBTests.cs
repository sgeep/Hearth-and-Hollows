using System.Collections;
using System.Linq;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Tavern;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4f Checkpoint B in the tavern scene: buying from the catalogue onto the cursor, storing and selling; tiers opening
    /// with Renown; recolouring a piece (and undoing it), the colour panel and matching every copy; laying a floor finish
    /// and putting it back; decorating the guest room by switching areas; the corner stairs and the guest room's door; and a
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

        /// <summary>The colour panel from the keyboard: down to the cushion, right through its ramps, and the catalogue's pages.</summary>
        [UnityTest]
        public IEnumerator ThePanels_WorkFromTheKeyboard()
        {
            yield return LoadTavern();
            Mode.Enter();
            yield return null;
            PlacedFurniture chair = Mode.Layout.Pieces.First(p => p.definition == "tavern_chair");
            Mode.SetCursor(chair.cell);
            yield return Press(UnityEngine.InputSystem.Key.V);
            Assert.That(Screen.Style.IsOpen, "V opens the colours");
            yield return Press(UnityEngine.InputSystem.Key.D);
            Assert.That(Mode.StyleTarget.palette, Does.StartWith("wood="), "right: the next wood");
            yield return Press(UnityEngine.InputSystem.Key.S);
            yield return Press(UnityEngine.InputSystem.Key.D);
            Assert.That(Mode.StyleTarget.palette, Does.Contain("cushion="), "down, then right: a cushion");
            yield return Press(UnityEngine.InputSystem.Key.Escape);
            Assert.That(Screen.Style.IsOpen, Is.False);
            Assert.That(Mode.IsActive, "closing the panel isn't leaving");

            // Pointing at another chair with the mouse, then the keys.
            PlacedFurniture other = Mode.Layout.Pieces.Last(p => p.definition == "tavern_chair");
            Vector2 at = FurnitureGeometry.ArtBounds(Mode.Layout.Resolve(other)).center;
            InputSystem.QueueStateEvent(Pointer, new UnityEngine.InputSystem.LowLevel.MouseState { position = Camera.main.WorldToScreenPoint(at), delta = new Vector2(30f, 0f) });
            yield return null;
            yield return null;
            Assert.That(Mode.Pointing && Mode.StyleTarget?.uid == other.uid, "the mouse is on the other chair");
            yield return Press(UnityEngine.InputSystem.Key.V);
            Assert.That(Screen.Style.IsOpen);
            yield return Press(UnityEngine.InputSystem.Key.D);
            yield return Press(UnityEngine.InputSystem.Key.D);
            Assert.That(Mode.Layout.Find(other.uid).palette, Does.StartWith("wood="), "the pointed-at chair changes");
            yield return Press(UnityEngine.InputSystem.Key.Escape);

            yield return Press(UnityEngine.InputSystem.Key.Tab);
            Assert.That(Screen.Catalogue.IsOpen);
            string first = Screen.Catalogue.TabText;
            yield return Press(UnityEngine.InputSystem.Key.Q);
            Assert.That(Screen.Catalogue.TabText, Is.Not.EqualTo(first), "Q: the next page");
            yield return Press(UnityEngine.InputSystem.Key.S);
            Assert.That(Screen.Catalogue.Selected, Is.EqualTo(1), "down the list");
            yield return Press(UnityEngine.InputSystem.Key.Escape);
            Assert.That(Screen.Catalogue.IsOpen, Is.False);
            Mode.Leave();
        }

        /// <summary>Web build: a key that goes down and up within one frame (as the browser can deliver them) still steps.</summary>
        [UnityTest]
        public IEnumerator ATapWithinOneFrame_StillSteps()
        {
            yield return LoadTavern();
            Mode.Enter();
            yield return null;
            Vector2Int before = Mode.Cursor;
            Hold(UnityEngine.InputSystem.Key.RightArrow);
            ReleaseKeys();
            yield return null;
            yield return null;
            Assert.That(Mode.Cursor, Is.EqualTo(before + Vector2Int.right), "the cursor");
            PlacedFurniture chair = Mode.Layout.Pieces.First(p => p.definition == "tavern_chair");
            Mode.SetCursor(chair.cell);
            Screen.OpenStyle();
            yield return null;
            Hold(UnityEngine.InputSystem.Key.D);
            ReleaseKeys();
            yield return null;
            yield return null;
            Assert.That(Mode.StyleTarget.palette, Does.StartWith("wood="), "the colour panel");
            Screen.Style.Close();
            Mode.Leave();
        }

        IEnumerator Press(UnityEngine.InputSystem.Key key)
        {
            Hold(key);
            yield return null;
            yield return null;
            ReleaseKeys();
            yield return null;
            yield return null;
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

        /// <summary>Every row and detail line of every page fits its box: nothing wraps onto the line below.</summary>
        [UnityTest]
        public IEnumerator EveryCatalogueText_FitsItsLines()
        {
            yield return LoadTavern();
            Mode.Enter();
            yield return null;
            DecorateCatalogue catalogue = Screen.Catalogue;
            Screen.OpenStorage();
            yield return null;
            // Every piece stored, so the storage page lists them all; then the shop pages (locked rows too).
            foreach (FurnitureDefinition d in Tavern.Content.furniture.Where(d => d != null && Tavern.State.OwnedCount(d.id) == 0))
                Tavern.State.AddOwnedCopies(d.id, 1);
            var problems = new System.Collections.Generic.List<string>();
            for (int tab = 0; tab < catalogue.TabCount; tab++, catalogue.Tab(1))
            for (int i = 0; i < catalogue.Count; i++)
            {
                catalogue.Select(i);
                foreach (CatalogueRow row in catalogue.Rows)
                {
                    Check(row.name, 1, problems);
                    Check(row.info, 1, problems);
                    if (!row.info.isActiveAndEnabled || !row.name.isActiveAndEnabled) continue;
                    var name = row.name.GetComponent<SuperTextMesh>();
                    var info = row.info.GetComponent<SuperTextMesh>();
                    if (string.IsNullOrEmpty(name.text) || string.IsNullOrEmpty(info.text)) continue;
                    // A pixel of daylight between the name and the price.
                    float gap = (info.finalTopLeftTextBounds.x - name.finalBottomRightTextBounds.x) / row.name.transform.lossyScale.x;
                    if (gap < 2f) problems.Add($"\"{name.text}\" runs into \"{info.text}\" ({gap:0.#} px apart)");
                }
                foreach ((LocalizedSuperText text, int lines) in catalogue.DetailTexts) Check(text, lines, problems);
            }
            Assert.That(problems.Distinct(), Is.Empty);
        }

        /// <summary>
        /// The color panel on every piece with looks, every option of every row: names stay on one line and clear of the
        /// swatches.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryColorPanelText_FitsItsRow()
        {
            yield return LoadTavern();
            DecorateMode.Sandbox = true;
            Mode.Enter();
            yield return null;
            DecorateStyle style = Screen.Style;
            var problems = new System.Collections.Generic.List<string>();
            int checkedPieces = 0;
            foreach (FurnitureDefinition d in Tavern.Content.furniture.Where(d => d != null && d.HasLooks))
            {
                Tavern.State.AddOwnedCopies(d.id, 1);
                if (!Mode.TakeFromStorage(d.id)) continue;
                Screen.OpenStyle();
                Assert.That(style.IsOpen, d.id);
                checkedPieces++;
                for (int line = 0; line < style.LineCount; line++)
                for (int option = 0; option < 16; option++)
                {
                    style.Choose(line, option);
                    foreach (StyleRow row in style.Rows.Where(r => r.root.activeInHierarchy))
                    {
                        Check(row.label, 1, problems);
                        Check(row.value, 1, problems);
                        var value = row.value.GetComponent<SuperTextMesh>();
                        if (!row.value.isActiveAndEnabled || string.IsNullOrEmpty(value.text)) continue;
                        float right = row.buttons.Where(b => b.gameObject.activeSelf)
                            .Select(b => ((RectTransform)b.transform).TransformPoint(new Vector3(((RectTransform)b.transform).rect.xMax, 0f)).x)
                            .DefaultIfEmpty(float.MinValue).Max();
                        float gap = (value.finalTopLeftTextBounds.x - right) / row.value.transform.lossyScale.x;
                        if (gap < 1f) problems.Add($"{d.id}: \"{value.text}\" runs into the swatches ({gap:0.#} px)");
                        float edge = ((RectTransform)row.root.transform).TransformPoint(new Vector3(((RectTransform)row.root.transform).rect.xMax, 0f)).x;
                        if (value.finalBottomRightTextBounds.x > edge + 0.01f) problems.Add($"{d.id}: \"{value.text}\" runs past the row");
                    }
                }
                style.Close();
                Mode.PutBack();
                yield return null;
            }
            Assert.That(checkedPieces, Is.GreaterThan(50), "the pieces with looks were checked");
            Assert.That(problems.Distinct(), Is.Empty);
        }

        static void Check(LocalizedSuperText label, int lines, System.Collections.Generic.List<string> problems)
        {
            if (label == null || !label.isActiveAndEnabled) return;
            var text = label.GetComponent<SuperTextMesh>();
            if (string.IsNullOrEmpty(text.text)) return;
            text.Rebuild();
            // STM lists one height per line plus one for the last character (its own bookkeeping).
            int drawn = text.lineHeights.Count - 1;
            if (drawn > lines) problems.Add($"{label.name}: \"{text.text}\" takes {drawn} lines (room for {lines})");
        }

        /// <summary>Walks the keeper into a doorway the way through, a step a physics frame (from just outside it).</summary>
        static IEnumerator StepInto(Rigidbody2D keeper, AreaPassage passage, Vector2 way)
        {
            Vector2 centre = passage.transform.position;
            for (int i = 0; i < 12 && !passage.Busy; i++)
            {
                keeper.position = centre + way * (-0.6f + i * 0.1f);
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator TheCornerStairs_LeadUpToTheGuestRoom_AndItsDoorLeadsBackDown()
        {
            yield return LoadTavern();
            Rigidbody2D keeper = GameObject.FindGameObjectWithTag("Player").GetComponent<Rigidbody2D>();
            AreaPassage up = Tavern.GetComponentsInChildren<AreaPassage>().Single();
            AreaPassage down = Guest.GetComponentsInChildren<AreaPassage>().Single();
            Assert.That(up.To, Is.SameAs(Guest.Area));
            Assert.That(down.To, Is.SameAs(Tavern.Area));
            Assert.That(up.transform.position.x, Is.GreaterThan(26f), "in the back-right corner");
            // The stairs' flight is a fixture (4h adds the menu board's and the storeroom shelves', SurfaceBuilder.TavernFixtures).
            Assert.That(Tavern.Area.Fixtures, Has.Member(new Rect(26f, 12f, 1f, 2f)));

            // Walked onto the stairs' foot: the screen fades, and the keeper is upstairs.
            // (Since the B playtest a doorway needs the keeper to go the way through, not only to stand in it: this scene alone
            // has no input manager, so the keeper is stepped up into it.)
            yield return StepInto(keeper, up, Vector2.up);
            yield return WaitUntil(() => PropertyArea.Current == Guest.Area, 3f, "the guest room");
            yield return WaitUntil(() => !up.Busy, 3f, "the fade back in");
            Assert.That(Vector2.Distance(keeper.position, Guest.Area.Arrival), Is.LessThan(0.6f));
            Assert.That((Vector2)TavernView.Camera.position, Is.EqualTo(Guest.Area.CameraPoint));

            // Decorating from upstairs decorates the guest room.
            Mode.Enter();
            yield return null;
            Assert.That(Mode.Area, Is.SameAs(Guest));
            Mode.Leave();

            // Down through the door: the keeper stands just below the stairs' foot, clear of it.
            yield return StepInto(keeper, down, Vector2.down);
            yield return WaitUntil(() => PropertyArea.Current == Tavern.Area, 3f, "down through the door");
            yield return WaitUntil(() => !down.Busy, 3f, "the fade back in");
            Assert.That(Vector2.Distance(keeper.position, Tavern.Area.Arrival), Is.LessThan(0.6f));
            Assert.That((Vector2)TavernView.Camera.position, Is.EqualTo(Tavern.Area.CameraPoint));
            yield return new WaitForSeconds(0.5f);
            Assert.That(PropertyArea.Current, Is.SameAs(Tavern.Area), "arriving doesn't send the keeper straight back up");

            // Mid-service the stairs work, and the end of the evening brings the keeper back down.
            Director.OpenDebugEvening();
            yield return null;
            yield return StepInto(keeper, up, Vector2.up);
            yield return WaitUntil(() => PropertyArea.Current == Guest.Area, 3f, "upstairs mid-service");
            yield return WaitUntil(() => !up.Busy, 3f, "the fade");
            Director.EndServiceNow();
            yield return WaitUntil(() => PropertyArea.Current == Tavern.Area, 5f, "back down when the evening ends");
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
