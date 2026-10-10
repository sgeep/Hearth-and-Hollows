using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core.Pathfinding;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.Shared.Village;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Village;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// Kariaston, the Crossroads (2026-10-10, with the owner's hand adjustments), walked on its own baked grid: every place a
    /// schedule sends someone is open ground reachable from Tally Ho!'s door, every walk a day asks for has a path, and the garden
    /// is closed except at its gate.
    /// </summary>
    public class KariastonWalkTests : BootFixture
    {
        NavGrid m_Grid;

        IEnumerator Village()
        {
            yield return StartDaytime(hold: true);
            m_Grid = Object.FindObjectsByType<NavGrid>(FindObjectsSortMode.None).Single(g => g.gameObject.scene.name == "Kariaston");
            Physics2D.SyncTransforms();
            m_Grid.Bake();
        }

        GridCell Cell(Vector2 world) => m_Grid.Space.ToCell(world);
        static Vector2 Door => SurfaceDoor.Find(SurfaceDoor.FrontOutside).Arrival;

        /// <summary>
        /// The places someone walks to (Musashi's cart, where his own feet stand all day, and windows, where nobody stands, aside).
        /// </summary>
        static IEnumerable<ScheduleAnchor> WalkedTo => ScheduleAnchor.All.Where(a => a.Area == SurfaceArea.KariastonId && !a.Window && a.Id != "market.cart");

        bool Connected(Vector2 from, Vector2 to) => GridPathfinder.TryFindPath(m_Grid.Map, Cell(from), Cell(to), new List<GridCell>());

        [UnityTest]
        public IEnumerator EveryPlaceInKariaston_IsOpenGround_ReachableFromTallyHosDoor()
        {
            yield return Village();
            Assert.That(m_Grid.Map.IsWalkable(Cell(Door)), "the door's arrival is open");
            foreach (ScheduleAnchor anchor in WalkedTo)
            {
                Assert.That(m_Grid.Map.IsWalkable(Cell(anchor.Spot)), $"{anchor.Id} stands on open ground");
                Assert.That(Connected(Door, anchor.Spot), $"{anchor.Id} is reachable from Tally Ho!'s door");
            }
            Assert.That(WalkedTo.Count(), Is.GreaterThanOrEqualTo(15), "the village's places are all there");
        }

        [UnityTest]
        public IEnumerator EveryWalkASchedule_AsksFor_HasAPath()
        {
            yield return Village();
            var walked = new HashSet<string>(WalkedTo.Select(a => a.Id));
            foreach (ScheduleDefinition schedule in Flow.Database.schedules)
            {
                // The day's places in Kariaston, in order; a place in Tally Ho! is reached through its door.
                var stops = schedule.blocks.Select(b => walked.Contains(b.anchor) ? ScheduleAnchor.Find(b.anchor).Spot : ScheduleAnchor.Find(b.anchor) != null ? Door : (Vector2?)null)
                    .Where(p => p.HasValue).Select(p => p.Value).ToList();
                for (int i = 0; i < stops.Count; i++)
                for (int j = i + 1; j < stops.Count; j++)
                    Assert.That(Connected(stops[i], stops[j]), $"{schedule.character}: from {stops[i]} to {stops[j]}");
            }
            // Kaloren's herbs, from his tower to the cottage door.
            Assert.That(Connected(ScheduleAnchor.Find("kaloren.tower").Spot, ScheduleAnchor.Find("cottage.door").Spot), "the herb walk");
        }

        [UnityTest]
        public IEnumerator TheGarden_IsClosed_ExceptAtItsGate()
        {
            yield return Village();
            Vector2 origin = VillageSpots.Origin;
            GridCell inside = Cell(origin + new Vector2(17.5f, 36.5f));
            var gate = new HashSet<GridCell> { Cell(origin + new Vector2(11.5f, 30.5f)), Cell(origin + new Vector2(12.5f, 30.5f)) };
            Assert.That(m_Grid.Map.IsWalkable(inside), "the garden is open inside");
            Assert.That(gate.All(c => m_Grid.Map.IsWalkable(c)), "its gate is open");
            // Flood from inside without stepping through the gate: Tally Ho!'s door is never reached.
            var seen = new HashSet<GridCell> { inside };
            var queue = new Queue<GridCell>(seen);
            while (queue.Count > 0)
            {
                GridCell c = queue.Dequeue();
                foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    var n = new GridCell(c.X + dx, c.Y + dy);
                    if (gate.Contains(n) || seen.Contains(n) || !m_Grid.Map.IsWalkable(n)) continue;
                    seen.Add(n);
                    queue.Enqueue(n);
                }
            }
            Assert.That(seen.Contains(Cell(Door)), Is.False, "the fence closes the garden but for its gate");
            Assert.That(Connected(Door, origin + new Vector2(17.5f, 36.5f)), "and through the gate it's reached");
        }
    }
}
