using System.Collections;
using System.Linq;
using Hearthdelve.Core.Input;
using Hearthdelve.Core.Pathfinding;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Tavern;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4f step 2: Decorate Mode in the tavern. Keyboard, gamepad and mouse; picking up, turning, putting down, refusing a
    /// bad spot with a reason, putting away and taking out of storage; the doors staying shut without a Grill; a service
    /// running in a rearranged room, with the walkable grid and the check agreeing.
    /// </summary>
    public class DecorateModeTests : LookTestFixture
    {
        Gamepad m_Pad;

        static DecorateMode Mode => DecorateMode.Instance;
        static AreaFurniture Area => AreaFurniture.Tavern;
        static TavernDirector Director => TavernDirector.Instance;

        [TearDown]
        public void RemovePad()
        {
            if (m_Pad != null) InputSystem.RemoveDevice(m_Pad);
            m_Pad = null;
        }

        IEnumerator LoadTavern()
        {
            yield return Load("Tavern");
            yield return WaitUntil(() => Loc.IsReady, 10f, "the string tables");
            yield return null;
        }

        IEnumerator Tap(params Key[] keys)
        {
            Hold(keys);
            yield return null;
            ReleaseKeys();
            yield return null;
        }

        IEnumerator Arrows(Key key, int times)
        {
            for (int i = 0; i < times; i++) yield return Tap(key);
        }

        IEnumerator Pad(GamepadButton button)
        {
            InputSystem.QueueStateEvent(m_Pad, new GamepadState().WithButton(button));
            yield return null;
            InputSystem.QueueStateEvent(m_Pad, new GamepadState());
            yield return null;
        }

        static PlacedFurniture At(string id, int x, int y) => Mode.Layout.Pieces.FirstOrDefault(p => p.definition == id && p.cell == new Vector2Int(x, y));
        static PlacedFurniture Placed(string id, int x, int y) => Area.CurrentLayout().FirstOrDefault(p => p.definition == id && p.cell == new Vector2Int(x, y));

        [UnityTest]
        public IEnumerator Entering_TakesTheControls_AndLeavingGivesThemBack()
        {
            yield return LoadTavern();
            var prep = Object.FindAnyObjectByType<PrepScreen>();
            var screen = Object.FindAnyObjectByType<DecorateScreen>();
            Assert.That(prep.IsShown);
            Assert.That(Mode.CanEnter, "Prep is a quiet time");
            Mode.Enter();
            yield return null;
            yield return null;
            Assert.That(Mode.IsActive);
            Assert.That(InputSystem.actions.FindActionMap(InputMaps.Decorate).enabled);
            Assert.That(InputSystem.actions.FindActionMap(InputMaps.Tavern).enabled, Is.False, "the keeper stays put");
            Assert.That(prep.IsShown, Is.False, "the room is what you look at");
            Assert.That(screen.IsShown);
            Assert.That(screen.StatusText, Is.EqualTo("ready for service"));
            yield return Tap(Key.Escape);
            yield return null;
            Assert.That(Mode.IsActive, Is.False, "Esc with nothing in hand is done");
            Assert.That(screen.IsShown, Is.False);
            Assert.That(prep.IsShown);
        }

        [UnityTest]
        public IEnumerator TheKeyboard_PicksUpTurnsAndPutsDownAChair()
        {
            yield return LoadTavern();
            Mode.Enter();
            yield return null;
            Mode.SetCursor(new Vector2Int(14, 8));
            // To the third table's west chair, at (17, 4): three right, four down.
            yield return Arrows(Key.RightArrow, 3);
            yield return Arrows(Key.DownArrow, 4);
            Assert.That(Mode.Cursor, Is.EqualTo(new Vector2Int(17, 4)));
            Assert.That(Mode.HoveredPiece?.definition, Is.EqualTo("tavern_chair"));
            int uid = Mode.HoveredPiece.uid;

            yield return Tap(Key.E);
            Assert.That(Mode.Carried?.uid, Is.EqualTo(uid), "picked up");
            Assert.That(Object.FindObjectsByType<FurnitureView>().Any(v => v.Uid == uid), Is.False, "out of the room while carried");
            yield return Tap(Key.R);
            Assert.That(Mode.Carried.turns, Is.EqualTo(2), "east-facing turns to its next drawn facing, north");
            yield return Tap(Key.R);
            Assert.That(Mode.Carried.turns, Is.EqualTo(3), "then west");
            yield return Arrows(Key.LeftArrow, 3);
            Assert.That(Mode.Carried.cell, Is.EqualTo(new Vector2Int(14, 4)), "it travels with the cursor");
            yield return Tap(Key.E);
            Assert.That(Mode.Carried, Is.Null, "put down");
            Assert.That(Mode.Report.Seats, Is.EqualTo(5), "away from any table, it's no longer a seat");

            yield return Tap(Key.Escape);
            yield return null;
            Assert.That(Mode.IsActive, Is.False);
            PlacedFurniture chair = Area.CurrentLayout().Single(p => p.uid == uid);
            Assert.That((chair.cell, chair.turns, chair.nudge), Is.EqualTo((new Vector2Int(14, 4), 3, Vector2Int.zero)), "kept, on whole tiles (D1)");
            Assert.That(Director.Layout.Seats.Count, Is.EqualTo(5), "the service has one seat fewer");
        }

        [UnityTest]
        public IEnumerator ABadSpot_SaysWhy_AndThePieceStaysInHand_ThenGoesBack()
        {
            yield return LoadTavern();
            var screen = Object.FindAnyObjectByType<DecorateScreen>();
            var feedback = Object.FindAnyObjectByType<DecorateFeedback>();
            Mode.Enter();
            yield return null;
            Mode.SetCursor(new Vector2Int(25, 8));
            PlacedFurniture original = Mode.HoveredPiece.Clone();
            Mode.PickUp();
            Assert.That(Mode.Carried?.definition, Is.EqualTo("cellar_barrel"));
            Mode.SetCursor(new Vector2Int(4, 7));
            yield return null;
            Assert.That(Mode.CarriedCheck.Problem, Is.EqualTo(PlacementProblem.Overlaps));
            Assert.That(screen.PieceText, Does.Contain("can't go there").And.Contain("something's already there"));
            Mode.Place();
            yield return null;
            Assert.That(Mode.Carried, Is.Not.Null, "still in hand");
            Assert.That(feedback.LastMoment, Is.EqualTo(DecorateMoment.Invalid), "and it said no");

            yield return Tap(Key.Escape);
            Assert.That(Mode.Carried, Is.Null);
            Assert.That(Mode.IsActive, "Esc while carrying puts the piece back, it doesn't leave");
            PlacedFurniture back = At("cellar_barrel", 25, 8);
            Assert.That(back, Is.Not.Null);
            Assert.That((back.nudge, back.turns), Is.EqualTo((original.nudge, original.turns)), "exactly where it was");
            Mode.Leave();
        }

        [UnityTest]
        public IEnumerator TheGamepad_DrivesDecorateMode()
        {
            m_Pad = InputSystem.AddDevice<Gamepad>();
            yield return LoadTavern();
            Mode.Enter();
            yield return null;
            Mode.SetCursor(new Vector2Int(23, 9));
            // To the barrel at (26, 9): three right with the d-pad, then A, one up, A (the stairs stand above that).
            for (int i = 0; i < 3; i++) yield return Pad(GamepadButton.DpadRight);
            Assert.That(Mode.HoveredPiece?.definition, Is.EqualTo("cellar_barrel"));
            yield return Pad(GamepadButton.South);
            Assert.That(Mode.Carried, Is.Not.Null);
            yield return Pad(GamepadButton.DpadUp);
            yield return Pad(GamepadButton.South);
            Assert.That(Mode.Carried, Is.Null);
            Assert.That(At("cellar_barrel", 26, 10), Is.Not.Null);
            InputSystem.QueueStateEvent(m_Pad, new GamepadState { leftTrigger = 1f });
            yield return null;
            InputSystem.QueueStateEvent(m_Pad, new GamepadState());
            yield return null;
            Assert.That(At("cellar_barrel", 26, 9), Is.Not.Null, "LT undoes it");
            yield return Pad(GamepadButton.East);
            yield return null;
            Assert.That(Mode.IsActive, Is.False, "B is done");
        }

        [UnityTest]
        public IEnumerator TheMouse_PointsAtTiles_AndClicks()
        {
            yield return LoadTavern();
            Mode.Enter();
            yield return null;
            Camera camera = Camera.main;
            Vector2 Screen(Vector2 world) => camera.WorldToScreenPoint(world);
            InputSystem.QueueStateEvent(Pointer, new MouseState { position = Screen(new Vector2(26.5f, 9.5f)) });
            yield return null;
            yield return null;
            Assert.That(Mode.Cursor, Is.EqualTo(new Vector2Int(26, 9)), "the cursor follows the pointer");
            InputSystem.QueueStateEvent(Pointer, new MouseState { position = Screen(new Vector2(26.5f, 9.5f)) }.WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(Pointer, new MouseState { position = Screen(new Vector2(26.5f, 9.5f)) });
            yield return null;
            Assert.That(Mode.Carried?.definition, Is.EqualTo("cellar_barrel"), "a click picks it up");
            InputSystem.QueueStateEvent(Pointer, new MouseState { position = Screen(new Vector2(12.5f, 10.5f)) });
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(Pointer, new MouseState { position = Screen(new Vector2(12.5f, 10.5f)) }.WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(Pointer, new MouseState { position = Screen(new Vector2(12.5f, 10.5f)) });
            yield return null;
            Assert.That(At("cellar_barrel", 12, 10), Is.Not.Null, "and another puts it down");
            Mode.Leave();
        }

        /// <summary>The Checkpoint A playtest: the mouse picks a chair by what's drawn, carries it from where it was grabbed.</summary>
        [UnityTest]
        public IEnumerator TheMouse_PicksByWhatsDrawn_AndFreeModePlacesToThePixel()
        {
            yield return LoadTavern();
            Mode.Enter();
            yield return null;
            // The first table's west chair is drawn over the west edge of its tile: point there.
            Mode.Point(new Vector2(2.95f, 7.6f));
            Assert.That(Mode.HoveredPiece?.definition, Is.EqualTo("tavern_chair"));
            Mode.PickUp();
            Mode.Point(new Vector2(12.95f, 5.6f));
            Assert.That((Mode.Carried.cell, Mode.Carried.nudge), Is.EqualTo((new Vector2Int(13, 5), Vector2Int.zero)), "snapped to the tile it's carried over");
            Mode.ForceFree = true;
            Mode.Point(new Vector2(12.95f + 0.25f, 5.6f - 0.125f));
            Assert.That((Mode.Carried.cell, Mode.Carried.nudge), Is.EqualTo((new Vector2Int(13, 5), new Vector2Int(2, -1))), "free: to the pixel");
            Mode.Place();
            Mode.ForceFree = false;
            Assert.That(Mode.Carried, Is.Null);
            Mode.Leave();
            yield return null;
            ResolvedFurniture chair = Area.Pieces.First(p => p.Placement.cell == new Vector2Int(13, 5));
            Assert.That(Vector2.Distance(chair.Art[0].Position, new Vector2(13.25f + 0.25f, 5.25f - 0.125f)), Is.LessThan(1e-4f), "where it was put");
        }

        /// <summary>Checkpoint A playtest: corners round the drawing while snapping; in free mode no outline, the piece turns red where it can't go.</summary>
        [UnityTest]
        public IEnumerator TheGhost_IsFramedWhenSnapping_AndJustTintedInFreeMode()
        {
            yield return LoadTavern();
            Mode.Enter();
            yield return null;
            Transform Ghost() => Mode.transform.Find("Decorate Ghost");
            Mode.SetCursor(new Vector2Int(26, 9));
            Mode.PickUp();
            Mode.SetCursor(new Vector2Int(12, 10));
            Assert.That(Ghost().GetComponentsInChildren<SpriteRenderer>().Count(r => r.name == "Corner"), Is.EqualTo(4), "snapping: corners round it");
            Mode.ForceFree = true;
            yield return null;
            Assert.That(Ghost().GetComponentsInChildren<SpriteRenderer>().Count(r => r.name == "Corner"), Is.Zero, "free: no outline");
            SpriteRenderer barrel = Ghost().GetComponentsInChildren<SpriteRenderer>().Single();
            Assert.That(barrel.color.g, Is.GreaterThan(0.9f), "where it fits, drawn as it is");
            Mode.SetCursor(new Vector2Int(4, 7));
            barrel = Ghost().GetComponentsInChildren<SpriteRenderer>().Single();
            Assert.That(barrel.color.g, Is.LessThan(0.6f), "where it can't go, red");
            Mode.ForceFree = false;
            Mode.PutBack();
            Mode.Leave();
        }

        [UnityTest]
        public IEnumerator TheGlasses_RideOnTheirShelf()
        {
            yield return LoadTavern();
            Mode.Enter();
            yield return null;
            Mode.SetCursor(new Vector2Int(24, 14));
            Mode.CycleHover();
            while (Mode.HoveredPiece.definition != "low_shelf") Mode.CycleHover();
            Mode.PickUp();
            Assert.That(Mode.Layout.Pieces.Any(p => p.definition == "shelf_glasses"), Is.False, "the glasses come up with it");
            Mode.SetCursor(new Vector2Int(8, 15));
            Mode.Place();
            Assert.That(Mode.Carried, Is.Null);
            PlacedFurniture glasses = Mode.Layout.Pieces.Single(p => p.definition == "shelf_glasses");
            Assert.That(Vector2.Distance(Mode.Layout.Resolve(glasses).Art[0].Position, new Vector2(8.75f, 16.5f)), Is.LessThan(1e-4f), "and stand on it where it went");
            Mode.Leave();
        }

        [UnityTest]
        public IEnumerator WithoutAGrill_TheDoorsStayShut_AndPrepOffersDecorateMode()
        {
            yield return LoadTavern();
            var prep = Object.FindAnyObjectByType<PrepScreen>();
            Mode.Enter();
            yield return null;
            Mode.SetCursor(new Vector2Int(20, 12));
            Assert.That(Mode.HoveredPiece?.definition, Is.EqualTo("kitchen_range"));
            Mode.Store();
            Assert.That(Mode.Storage().Any(s => s.definition.id == "kitchen_range" && s.count == 1), "in storage, never destroyed (D12)");
            Assert.That(Mode.Report.CanOpen, Is.False);
            Mode.Leave();
            yield return null;
            yield return null;

            Director.OpenDebugEvening();
            yield return null;
            Assert.That(Director.IsServing, Is.False, "the doors stay shut (D13)");
            Assert.That(prep.OpenButton.interactable, Is.False);
            Assert.That(prep.DecorateButton.gameObject.activeInHierarchy, "the banner offers Decorate Mode");
            Assert.That(prep.GetComponentsInChildren<SuperTextMesh>().Any(t => t.text.Contains("the doors can't open") && t.text.Contains("no grill")));

            prep.DecorateButton.onClick.Invoke();
            yield return null;
            Assert.That(Mode.IsActive);
            Mode.SetCursor(new Vector2Int(19, 12));
            Assert.That(Mode.TakeFromStorage("kitchen_range"), "out of storage, onto the cursor");
            Mode.Place();
            Assert.That(Mode.Carried, Is.Null);
            Assert.That(Mode.Report.CanOpen);
            Mode.Leave();
            yield return null;
            Director.OpenDebugEvening();
            yield return null;
            Assert.That(Director.IsServing, "the Grill is back: service opens");
        }

        /// <summary>A table and its chairs moved across the room: customers find the seats, and the check's grid is the baked grid.</summary>
        [UnityTest]
        public IEnumerator AServiceRuns_InARearrangedTavern()
        {
            yield return LoadTavern();
            Mode.Enter();
            yield return null;
            foreach (var (from, to) in new[] { (new Vector2Int(9, 7), new Vector2Int(10, 4)), (new Vector2Int(8, 7), new Vector2Int(9, 4)), (new Vector2Int(10, 7), new Vector2Int(11, 4)) })
            {
                Mode.SetCursor(from);
                Mode.PickUp();
                Mode.SetCursor(to);
                Mode.Place();
                Assert.That(Mode.Carried, Is.Null, $"placed at {to}");
            }
            Assert.That(Mode.Report.Seats, Is.EqualTo(6));
            Mode.Leave();
            yield return null;
            yield return null;

            NavGrid grid = NavGrid.Current;
            GridMap pure = LayoutCheck.Walkable(Area.Shape(), Area.Pieces);
            for (int y = 0; y < grid.Map.Height; y++)
            for (int x = 0; x < grid.Map.Width; x++)
                Assert.That(pure.IsWalkable(new GridCell(x, y)), Is.EqualTo(grid.Map.IsWalkable(new GridCell(x, y))), $"cell ({x}, {y})");

            Director.OpenDebugEvening();
            Director.ArrivalsPaused = true;
            for (int i = 0; i < 6; i++) Director.SpawnCustomer();
            Time.timeScale = 4f;
            yield return WaitUntil(() => Director.Agents.Count(a => a.Logic.State is CustomerState.Ordering or CustomerState.WaitingForFood) == 6, 40f,
                "all six customers seated");
            Time.timeScale = 1f;
        }
    }
}
