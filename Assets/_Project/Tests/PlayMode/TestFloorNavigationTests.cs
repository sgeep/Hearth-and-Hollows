using System.Collections;
using System.Collections.Generic;
using Hearthdelve.Core;
using Hearthdelve.Core.Pathfinding;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Navigation;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// Enemy navigation on the 4b test floor, with the real TDE character, collider and physics:
    /// a slime chases a player it cannot see in a straight line, around a U-shaped wall, a column
    /// of props and through a doorway. Its collision box must never touch (let alone overlap) a
    /// wall or prop on the way, and it must get there.
    /// </summary>
    public class TestFloorNavigationTests : LookTestFixture
    {
        const string TestFloorScene = "Dungeon_TestFloor";

        [UnityTest]
        public IEnumerator NavGrid_BlocksWallsPillarsAndProps_AndOpensTheFloor()
        {
            yield return Load(TestFloorScene);
            NavGrid grid = NavGrid.Current;
            Assert.That(grid, Is.Not.Null, "the test floor has a NavGrid");
            GridMap map = grid.Map;
            var dump = new System.Text.StringBuilder();
            for (int y = map.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < map.Width; x++) dump.Append(map.IsWalkable(new GridCell(x, y)) ? '.' : '#');
                dump.Append('\n');
            }
            Debug.Log("[Hearthdelve] Baked test floor grid:\n" + dump);

            foreach (char id in "123456")
                Assert.That(map.IsWalkable(grid.Space.ToCell(Point(id))), $"point {id} stands on open floor");
            Assert.That(map.IsWalkable(grid.Space.ToCell(Player.transform.position)), "the spawn is open");

            int obstacles = LayerMask.NameToLayer(Layers.Obstacles);
            int props = 0;
            foreach (BoxCollider2D box in Object.FindObjectsByType<BoxCollider2D>())
            {
                if (box.gameObject.layer != obstacles || box.isTrigger) continue;
                props++;
                Assert.That(map.IsWalkable(grid.Space.ToCell(box.bounds.center)), Is.False, $"{box.name} blocks its tile");
            }
            Assert.That(props, Is.GreaterThan(10), "the floor has props to path around");
            Assert.That(map.IsWalkable(grid.Space.ToCell(new Vector2(0.5f, 10.5f))), Is.False, "the outer wall blocks");
        }

        [UnityTest]
        public IEnumerator Slime_PathsAroundAUShapedWall_WithoutTouchingIt()
        {
            yield return Chase('1', '2', speed: 0f, seconds: 20f);
        }

        [UnityTest]
        public IEnumerator Slime_PathsAroundAColumnOfProps_WithoutTouchingThem()
        {
            yield return Chase('3', '4', speed: 0f, seconds: 20f);
        }

        [UnityTest]
        public IEnumerator Slime_PathsThroughADoorwayIntoTheNextRoom_WithoutTouchingTheWalls()
        {
            yield return Chase('5', '6', speed: 4f, seconds: 20f);
        }

        static Vector2 Point(char id)
        {
            GameObject point = GameObject.Find($"NavTestPoints/Point{id}");
            Assert.That(point, Is.Not.Null, $"test point {id}");
            return point.transform.position;
        }

        /// <param name="speed">Walk speed override in tiles per second; 0 keeps the slime's own.</param>
        IEnumerator Chase(char from, char to, float speed, float seconds)
        {
            yield return Load(TestFloorScene);
            Player.CharacterHealth.DamageDisabled();

            var slimes = new List<EnemyIdentity>(Object.FindObjectsByType<EnemyIdentity>());
            Assert.That(slimes, Is.Not.Empty);
            EnemyIdentity slime = slimes[0];
            for (int i = 1; i < slimes.Count; i++) Object.Destroy(slimes[i].gameObject);

            Teleport(Player, Point(to));
            Teleport(slime, Point(from));
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            var character = slime.GetComponent<Character>();
            if (speed > 0f) character.FindAbility<CharacterMovement>().WalkSpeed = speed;
            character.FindAbility<CharacterMovement>().MovementSpeed = character.FindAbility<CharacterMovement>().WalkSpeed;
            AIBrain brain = character.CharacterBrain;
            brain.Target = Player.transform;
            brain.TransitionToState("Chase");
            var action = slime.GetComponent<AIActionPathfindToTarget2D>();
            Assert.That(action, Is.Not.Null, "the slime chases with the pathfinding action");

            NavGrid grid = NavGrid.Current;
            Collider2D body = null;
            foreach (Collider2D candidate in slime.GetComponents<Collider2D>())
                if (!candidate.isTrigger) body = candidate;
            Assert.That(grid.Map.IsWalkable(grid.Space.ToCell(body.bounds.center)), "starts on open floor");
            Assert.That(GridSweep.IsClear(grid.Map, grid.Space, body.bounds.center, (Vector2)Player.transform.position + (Vector2)(body.bounds.center - slime.transform.position), body.bounds.extents),
                Is.False, "the player is out of straight reach, so this exercises the path");

            var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = LayerMask.GetMask(Layers.Obstacles) };
            var hits = new List<Collider2D>();
            int frames = 0, touching = 0, overlapping = 0, pathing = 0;
            string firstTouch = null;
            float deadline = Time.time + seconds;
            bool reached = false;
            while (Time.time < deadline)
            {
                yield return new WaitForFixedUpdate();
                frames++;
                if (action.IsFollowingPath) pathing++;
                if (body.IsTouching(filter))
                {
                    touching++;
                    firstTouch ??= $"touched an obstacle at {(Vector2)slime.transform.position}";
                }
                Bounds bounds = body.bounds;
                if (Physics2D.OverlapBox(bounds.center, (Vector2)bounds.size - Vector2.one * 0.04f, 0f, filter, hits) > 0)
                {
                    overlapping++;
                    firstTouch ??= $"overlapped {hits[0].name} at {(Vector2)slime.transform.position}";
                }
                if (Vector2.Distance(slime.transform.position, Player.transform.position) < 1f)
                {
                    reached = true;
                    break;
                }
            }

            Debug.Log($"[Hearthdelve] Chase {from}→{to}: {frames} physics steps, {pathing} following a path, {touching} touching, {overlapping} overlapping.");
            Assert.That(reached, $"the slime never reached the player; it stopped at {(Vector2)slime.transform.position}");
            Assert.That(pathing, Is.GreaterThan(0), "it followed a path");
            Assert.That(overlapping, Is.Zero, firstTouch);
            Assert.That(touching, Is.Zero, firstTouch);
        }
    }
}
