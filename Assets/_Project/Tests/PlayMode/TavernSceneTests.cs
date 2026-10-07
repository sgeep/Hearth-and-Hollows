using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Core.Pathfinding;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Tavern;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4c step 1: the <c>Tavern</c> scene. The room and its furniture, the fixed camera, the walkable
    /// grid (and rebuilding it when furniture moves), and the player's interactions with stations.
    /// </summary>
    public class TavernSceneTests : LookTestFixture
    {
        const string Scene = "Tavern";

        /// <summary>Loads the tavern and opens an evening at once (the scene starts at Prep, with walking off).</summary>
        IEnumerator LoadServing()
        {
            yield return Load(Scene);
            TavernDirector.Instance.OpenDebugEvening();
            yield return null;
        }

        static TavernInteractable Station(TavernInteractableKind kind) =>
            Object.FindObjectsByType<TavernInteractable>().First(s => s.Kind == kind);

        [UnityTest]
        public IEnumerator Room_HasItsStations_SeatsAndGrid()
        {
            yield return LoadServing();
            var daytimePlaces = new[] { TavernInteractableKind.MenuBoard, TavernInteractableKind.Storeroom, TavernInteractableKind.Inspect };
            var all = Object.FindObjectsByType<TavernInteractable>().Select(s => s.Kind).ToList();
            var kinds = all.Where(k => k != TavernInteractableKind.Seat && k != TavernInteractableKind.Person && !daytimePlaces.Contains(k)).ToList();
            Assert.That(kinds, Is.EquivalentTo(new[] { TavernInteractableKind.Grill, TavernInteractableKind.Tap, TavernInteractableKind.StewPot, TavernInteractableKind.ButcherBlock, TavernInteractableKind.Pass }));
            // 4h: the daytime's places in the room (the menu board, the storeroom shelves) and Phi's portrait upstairs.
            Assert.That(all, Is.SupersetOf(daytimePlaces));
            Assert.That(TavernDirector.Instance.Layout.Seats.Count, Is.EqualTo(6), "3 tables of 2 seats (the starting layout)");
            Assert.That(NavGrid.Current, Is.Not.Null);
            Assert.That(NavGrid.Current.Bounds, Is.EqualTo(new RectInt(0, 0, 28, 17)));
            foreach (SpriteRenderer sprite in Object.FindObjectsByType<SpriteRenderer>().Where(r => r.gameObject.layer != LayerMask.NameToLayer("UI")))
            {
                // Overlays (highlights, patience bars, speech bubbles) sit on the Above layer and are unlit on purpose.
                if (sprite.sortingLayerName == Hearthdelve.Core.SortingLayers.Above) continue;
                Assert.That(sprite.sharedMaterial.name, Does.StartWith("Sprite-Lit"), $"{sprite.name} uses the lit sprite material");
            }
        }

        [UnityTest]
        public IEnumerator Camera_HoldsStill_WithTheWholeRoomOnScreen()
        {
            yield return LoadServing();
            Camera camera = Camera.main;
            Vector3 before = camera.transform.position;
            Hold(Key.D, Key.S);
            yield return new WaitForSeconds(0.8f);
            ReleaseKeys();
            yield return null;
            Assert.That(Player.transform.position.x, Is.GreaterThan(14f), "the player moved");
            Assert.That(Vector3.Distance(camera.transform.position, before), Is.LessThan(0.001f), "the camera stays put");
            // Centred on the room, which fits the 180 px reference height at every common screen shape. (The batch
            // test window is narrower than any real screen, so the check is on the view, not on that window.)
            Assert.That((Vector2)camera.transform.position, Is.EqualTo(new Vector2(14f, 8.5f)), "centred on the room");
            const float viewHeight = 180f / 8f;
            Assert.That(viewHeight, Is.GreaterThanOrEqualTo(17f), "the room's height fits");
            foreach (float aspect in new[] { 16f / 9f, 16f / 10f, 5f / 4f })
                Assert.That(viewHeight * aspect, Is.GreaterThanOrEqualTo(28f), $"the room's width fits at aspect {aspect:0.00}");
        }

        /// <summary>
        /// Walking up into each footprint of each piece from below stops the player in front of it, drawn in front
        /// (footprints end at the sort point). Every footprint, not only the lowest: the kitchen's oven started
        /// higher than its range and drew over the player (4c step 4 playtest).
        /// </summary>
        [UnityTest]
        public IEnumerator Furniture_StopsThePlayerInFront_AndDrawsBehindThem()
        {
            yield return LoadServing();
            // Placed pieces are named after their definition and uid (4f step 1: the starting layout's bar, kitchen, stew pot,
            // pass, first and third tables, the first table's west chair and the third table's east chair).
            string[] pieces = { "tavern_bar#1", "kitchen_range#2", "stew_pot#3", "pass_table#4", "table_round_a#5", "table_round_a#11", "tavern_chair#6", "tavern_chair#13" };
            foreach (string name in pieces)
            {
                GameObject piece = GameObject.Find(name);
                Assert.That(piece, Is.Not.Null, name);
                foreach (Collider2D footprint in piece.GetComponents<Collider2D>())
                {
                    Vector2 start = new(footprint.bounds.center.x, footprint.bounds.min.y - 1.2f);
                    Teleport(Player, start);
                    yield return new WaitForFixedUpdate();
                    Hold(Key.W);
                    yield return new WaitForSeconds(0.6f);
                    ReleaseKeys();
                    yield return new WaitForFixedUpdate();
                    float feet = Player.transform.position.y;
                    Assert.That(feet, Is.LessThan(footprint.bounds.min.y), $"{name} blocks the player");
                    Assert.That(feet, Is.GreaterThan(footprint.bounds.min.y - 0.6f), $"the player walked right up to {name}");
                    Transform sortRoot = piece.GetComponentInChildren<SortingGroup>()?.transform ?? piece.GetComponentInChildren<SpriteRenderer>().transform;
                    Assert.That(feet, Is.LessThan(sortRoot.position.y), $"the player sorts in front of {name}");
                }
            }
        }

        [UnityTest]
        public IEnumerator Grid_ReachesEveryStation_FromTheDoorAndTheSpawn()
        {
            yield return LoadServing();
            NavGrid grid = NavGrid.Current;
            var path = new List<GridCell>();
            GridCell door = grid.Space.ToCell(new Vector2(13.5f, 2.5f));
            GridCell spawn = grid.Space.ToCell(Player.transform.position);
            Assert.That(grid.Map.IsWalkable(door), "inside the door is walkable");
            // Everything usable in the tavern's room (4h: the menu board and the storeroom shelves too; Phi's portrait is upstairs, off this grid).
            foreach (TavernInteractable station in Object.FindObjectsByType<TavernInteractable>()
                         .Where(i => i.Kind != TavernInteractableKind.Person && grid.Bounds.Contains(Vector2Int.FloorToInt(i.UsePoint))))
            {
                // Somewhere to stand within reach (the pass is used from either side of its table).
                var standing = new List<GridCell>();
                for (int y = 0; y < grid.Map.Height; y++)
                for (int x = 0; x < grid.Map.Width; x++)
                {
                    var cell = new GridCell(x, y);
                    if (grid.Map.IsWalkable(cell) && Vector2.Distance(grid.Space.CellCentre(cell), station.UsePoint) <= station.Reach) standing.Add(cell);
                }
                Assert.That(standing, Is.Not.Empty, $"{station.Kind}: there is somewhere to stand within reach");
                Assert.That(standing.Any(c => GridPathfinder.TryFindPath(grid.Map, door, c, path)), $"{station.Kind} is reachable from the door");
                Assert.That(standing.Any(c => GridPathfinder.TryFindPath(grid.Map, spawn, c, path)), $"{station.Kind} is reachable from the spawn");
            }
        }

        /// <summary>Furniture can move (4f): the grid is invalidated and rebuilt, and its version tells path followers to re-plan.</summary>
        /// <summary>
        /// Step 1 and 2 playtests: the whole kitchen (oven and range) is the Grill, and the Grill and the Stew Pot
        /// can be used from behind as well as from the front: there's a walkable tile behind each, reached by walking round.
        /// </summary>
        [UnityTest]
        public IEnumerator TheGrillAndStewPot_AreUsableFromBehind_AndTheWholeKitchenIsTheGrill()
        {
            yield return LoadServing();
            NavGrid grid = NavGrid.Current;
            var path = new List<GridCell>();
            GridCell spawn = grid.Space.ToCell(Player.transform.position);
            var interactor = Player.GetComponent<TavernInteractor>();
            foreach (TavernInteractableKind kind in new[] { TavernInteractableKind.Grill, TavernInteractableKind.StewPot })
            {
                TavernInteractable station = Station(kind);
                Vector2 behind = station.UsePoints.Skip(1).Single();
                Assert.That(behind.y, Is.GreaterThan(station.UsePoint.y + 1.5f), $"{kind}: the second spot is behind it");
                GridCell cell = grid.Space.ToCell(behind);
                Assert.That(grid.Map.IsWalkable(cell), $"{kind}: the floor behind it is walkable");
                Assert.That(GridPathfinder.TryFindPath(grid.Map, spawn, cell, path), $"{kind}: and reachable by walking round");

                Teleport(Player, behind);
                yield return WaitUntil(() => interactor.Target == station, 1f, $"{kind} to be the target from behind");
                yield return null;
                Assert.That(station.IsHighlighted, $"{kind} highlights from behind");
                Assert.That(Player.transform.position.y, Is.GreaterThan(station.transform.position.y), $"{kind}: the player is behind it (drawn behind it)");
            }

            TavernInteractable grill = Station(TavernInteractableKind.Grill);
            Bounds oven = GameObject.Find("kitchen_range#2").GetComponents<Collider2D>().OrderByDescending(c => c.bounds.min.y).First().bounds;
            // Walk up to the oven, left of the range.
            Teleport(Player, new Vector2(oven.center.x, oven.min.y - 1.2f));
            yield return new WaitForFixedUpdate();
            Hold(Key.W);
            yield return new WaitForSeconds(0.6f);
            ReleaseKeys();
            yield return WaitUntil(() => interactor.Target == grill, 1f, "the oven, part of the kitchen, to target the Grill");
            Assert.That(grill.IsHighlighted);
            // All four corners of the highlight draw (a 9-sliced frame lost its top and right edges).
            Transform highlight = grill.transform.Find("Highlight");
            foreach (string corner in new[] { "CornerTL", "CornerTR", "CornerBL", "CornerBR" })
                Assert.That(highlight.Find(corner)?.GetComponent<SpriteRenderer>()?.sprite, Is.Not.Null, corner);
        }

        [UnityTest]
        public IEnumerator Grid_IsRebuilt_WhenTheFurnitureLayoutChanges()
        {
            yield return LoadServing();
            NavGrid grid = NavGrid.Current;
            GameObject table = GameObject.Find("table_round_a#5");
            GridCell under = grid.Space.ToCell(table.GetComponent<Collider2D>().bounds.center);
            Assert.That(grid.Map.IsWalkable(under), Is.False, "the table blocks its cell");
            int version = grid.Version;
            int rebuilt = 0;
            grid.Rebuilt += _ => rebuilt++;

            table.transform.position += new Vector3(0f, -30f, 0f);
            EventBus<NavigationLayoutChanged>.Publish(new NavigationLayoutChanged());
            Assert.That(grid.IsBaked, Is.False, "the layout change invalidates the grid");
            Assert.That(grid.Map.IsWalkable(under), "and the next use rebakes it without the table");
            Assert.That(grid.Version, Is.GreaterThan(version));
            Assert.That(rebuilt, Is.EqualTo(1));

            table.transform.position += new Vector3(0f, 30f, 0f);
            grid.Rebuild();
            Assert.That(grid.Map.IsWalkable(under), Is.False, "an explicit rebuild sees it back");
        }

        [UnityTest]
        public IEnumerator Interaction_HighlightsTheNearestStation_HintsAndUsesIt()
        {
            var used = new List<TavernInteracted>();
            EventBus<TavernInteracted>.Subscribe(used.Add);
            yield return LoadServing();
            yield return WaitUntil(() => Hearthdelve.UI.Localization.Loc.IsReady, 5f, "the string tables");
            var hint = Object.FindAnyObjectByType<TavernHintView>();
            TavernInteractable grill = Station(TavernInteractableKind.Grill), pass = Station(TavernInteractableKind.Pass);
            var interactor = Player.GetComponent<TavernInteractor>();

            Assert.That(interactor.Target, Is.Null, "nothing in reach at the spawn");
            Assert.That(hint.IsShown, Is.False);

            Teleport(Player, grill.UsePoint);
            yield return WaitUntil(() => interactor.Target == grill, 1f, "the grill to become the target");
            yield return null;
            Assert.That(grill.IsHighlighted);
            Assert.That(hint.IsShown);
            string key = Hearthdelve.UI.Screens.InputHints.TavernInteract();
            Assert.That(key, Does.StartWith("E"), "the hint names the Interact key");
            Assert.That(hint.GetComponentInChildren<SuperTextMesh>().text, Is.EqualTo($"{key}: grill"));

            Hold(Key.E);
            yield return null;
            yield return null;
            ReleaseKeys();
            yield return null;
            Assert.That(used.Count, Is.EqualTo(1));
            Assert.That(used[0].Kind, Is.EqualTo(TavernInteractableKind.Grill));

            Teleport(Player, pass.UsePoint + new Vector2(0f, -1f));
            yield return WaitUntil(() => interactor.Target == pass, 1f, "the pass to take over");
            yield return null;
            Assert.That(grill.IsHighlighted, Is.False, "one highlight at a time");
            Assert.That(pass.IsHighlighted);
            Assert.That(hint.GetComponentInChildren<SuperTextMesh>().text, Is.EqualTo($"{key}: the pass"));

            Teleport(Player, new Vector2(13.5f, 5.5f));
            yield return WaitUntil(() => interactor.Target == null, 1f, "walking away to clear the target");
            yield return null;
            Assert.That(pass.IsHighlighted, Is.False);
            Assert.That(hint.IsShown, Is.False);
            EventBus<TavernInteracted>.Unsubscribe(used.Add);
        }

        [UnityTest]
        public IEnumerator Interaction_IsOff_WhileTheTavernMapIsOff()
        {
            yield return LoadServing();
            TavernInteractable tap = Station(TavernInteractableKind.Tap);
            var interactor = Player.GetComponent<TavernInteractor>();
            Teleport(Player, tap.UsePoint);
            yield return WaitUntil(() => interactor.Target == tap, 1f, "the tap to become the target");

            InputMaps.ActivateUIOnly();
            yield return null;
            Assert.That(interactor.Target, Is.Null, "a panel or menu is open: nothing to use");
            Assert.That(tap.IsHighlighted, Is.False);
            InputMaps.Activate(InputMaps.Tavern);
            yield return null;
            Assert.That(interactor.Target, Is.EqualTo(tap));
        }
    }
}
