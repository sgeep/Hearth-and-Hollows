using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Staff;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4f step 1: the tavern's furniture is data, built as the scene loads. The starting layout must build the recorded
    /// starting room exactly (every sprite, body, use point, highlight, seat, post, light, loop, view and the walkable
    /// grid): the 4e room (<c>Tavern4e.txt</c>) with the third table's group moved half a tile onto the grid after the
    /// Checkpoint A playtest (<c>TavernStarting.txt</c>). A rebuilt layout must replace what was built, with the grid,
    /// seats and stations following.
    /// </summary>
    public class FurnitureSceneTests : LookTestFixture
    {
        const string Scene = "Tavern";
        static string Expected => Path.Combine(TavernBaselineCaptures.BaselineFolder, "TavernStarting.txt");

        static AreaFurniture TavernFurniture => AreaFurniture.All.Single(a => a.Area.Id == PropertyArea.TavernId);

        [UnityTest]
        public IEnumerator TheStartingTavern_IsTheRecordedRoom()
        {
            yield return Load(Scene);
            List<string> now = TavernSnapshot.Take();
            List<string> expected = File.ReadAllLines(Expected).Where(l => l.Length > 0).ToList();
            var missing = expected.Except(now).ToList();
            var extra = now.Except(expected).ToList();
            Assert.That(missing.Count + extra.Count, Is.Zero,
                "The tavern differs from the recorded starting room.\nMissing:\n" + string.Join("\n", missing) + "\nUnexpected:\n" + string.Join("\n", extra));
        }

        [UnityTest]
        public IEnumerator EveryPiece_IsBuiltOnce_FromTheStartingLayout()
        {
            yield return Load(Scene);
            AreaFurniture area = TavernFurniture;
            int placed = area.CurrentLayout().Count;
            Assert.That(area.Pieces.Count, Is.EqualTo(placed), "every placement resolves");
            Assert.That(area.GetComponentsInChildren<FurnitureView>().Length, Is.EqualTo(placed), "and is built once");
            Assert.That(area.Pieces.Select(p => p.Placement.uid).Distinct().Count(), Is.EqualTo(placed), "uids are unique");
        }

        [UnityTest]
        public IEnumerator RebuildingALayout_ReplacesThePieces_AndTheGridAndSeatsFollow()
        {
            yield return Load(Scene);
            AreaFurniture area = TavernFurniture;
            var layout = area.CurrentLayout().Select(p => p.Clone()).ToList();
            int before = Object.FindObjectsByType<FurnitureView>().Length;
            NavGrid grid = NavGrid.Current;
            var oldBarrel = new Vector2(24.5f, 8.5f);
            Assert.That(grid.Map.IsWalkable(grid.Space.ToCell(oldBarrel)), Is.False, "a barrel stands there");

            // Move the lone barrel to the open floor, and take the third table's east chair away.
            PlacedFurniture barrel = layout.Single(p => p.definition == "cellar_barrel" && p.cell == new Vector2Int(25, 8));
            barrel.cell = new Vector2Int(14, 10);
            layout.RemoveAll(p => p.definition == "tavern_chair" && p.cell == new Vector2Int(19, 4));
            area.Build(layout);
            yield return null;

            Assert.That(Object.FindObjectsByType<FurnitureView>().Length, Is.EqualTo(before - 1), "the old pieces are gone, nothing doubled");
            Assert.That(grid.Map.IsWalkable(grid.Space.ToCell(oldBarrel)), "the old spot is floor again");
            Assert.That(grid.Map.IsWalkable(grid.Space.ToCell(new Vector2(14.5f, 10.5f))), Is.False, "the new spot is blocked");
            Assert.That(TavernDirector.Instance.Layout.Seats.Count, Is.EqualTo(5), "one chair fewer, one seat fewer");
            Assert.That(TavernDirector.Instance.Layout.PostFor(StaffStation.Grill), Is.EqualTo(new Vector2(20.8125f, 11.425f)).Using(Vector2EqualityComparer.Instance),
                "the stations still have their posts");
            Assert.That(TavernInteractable.All.Count(i => i.Kind == TavernInteractableKind.Grill), Is.EqualTo(1), "one Grill, not two");
        }

        sealed class Vector2EqualityComparer : IEqualityComparer<Vector2>
        {
            public static readonly Vector2EqualityComparer Instance = new();
            public bool Equals(Vector2 a, Vector2 b) => Vector2.Distance(a, b) < 1e-4f;
            public int GetHashCode(Vector2 v) => 0;
        }
    }
}
