using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Core;
using Hearthdelve.Core.Movement;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Staff;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Hearthdelve.Tests.PlayMode
{
    /// <summary>
    /// 4c step 2 in the <c>Tavern</c> scene: customers walk in, take seats or queue, wait with visible
    /// patience, walk out when it runs out, and never clip furniture or shove the player; their layered
    /// look stays in step and fixed; Pip walks to their post.
    /// </summary>
    public class TavernCustomerTests : LookTestFixture
    {
        const string Scene = "Tavern";
        TavernDirector Director => TavernDirector.Instance;

        IEnumerator Open()
        {
            yield return Load(Scene);
            Director.OpenDebugEvening();
            Assert.That(Director.IsServing, "the evening opens");
            Director.ArrivalsPaused = true;
        }

        /// <summary>A copy of a real profile with short patience, to see walkouts quickly.</summary>
        CustomerProfile Impatient(float orderPatience = 1.5f, float seatPatience = 30f)
        {
            CustomerProfile profile = Object.Instantiate(Director.Content.customers[0]);
            profile.traits.orderPatience = orderPatience;
            profile.traits.seatPatience = seatPatience;
            profile.traits.orderDelay = 0.3f;
            return profile;
        }

        static bool Clips(CustomerAgent agent)
        {
            var body = agent.GetComponent<BoxCollider2D>();
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = LayerMask.GetMask(Layers.Obstacles), useTriggers = false };
            var hits = new List<Collider2D>();
            // Shrink by the contact skin: touching a wall isn't clipping it.
            return Physics2D.OverlapBox(body.bounds.center, (Vector2)body.bounds.size - Vector2.one * 0.05f, 0f, filter, hits) > 0;
        }

        [UnityTest]
        public IEnumerator ACustomer_WalksFromTheDoorToASeat_WithoutClipping_AndSitsFacingTheTable()
        {
            yield return Open();
            CustomerAgent agent = Director.SpawnCustomer();
            Assert.That(Vector2.Distance(agent.transform.position, Director.Layout.Door), Is.LessThan(0.01f), "comes in at the door");
            float until = Time.time + 12f;
            while (!agent.IsSeated && Time.time < until)
            {
                Assert.That(Clips(agent), Is.False, $"clipped furniture at {(Vector2)agent.transform.position}");
                yield return new WaitForFixedUpdate();
            }
            Assert.That(agent.IsSeated, "reached their seat");
            TavernSeat seat = Director.Layout.Seat(agent.Logic.Seat);
            Assert.That(Vector2.Distance(agent.transform.position, seat.SitPoint), Is.LessThan(0.01f), "sitting on the chair");
            Assert.That(agent.Look.Facing, Is.EqualTo(seat.Facing), "facing the table");
            Assert.That(agent.Logic.State == CustomerState.Ordering || agent.Logic.State == CustomerState.WaitingForFood, $"reading the menu or waiting (was {agent.Logic.State})");
            Vector2 at = agent.transform.position;
            yield return new WaitForSeconds(1f);
            Assert.That(Vector2.Distance(agent.transform.position, at), Is.LessThan(0.01f), "stays put while seated");
        }

        [UnityTest]
        public IEnumerator WhenEverySeatIsTaken_CustomersQueueByTheDoor_ThenTakeFreedSeats()
        {
            yield return Open();
            int seats = Director.ActiveSeats;
            Assert.That(seats, Is.EqualTo(6), "6 seats open before upgrades");
            var diners = new List<CustomerAgent>();
            for (int i = 0; i < seats; i++)
            {
                diners.Add(Director.SpawnCustomer(Impatient(orderPatience: 4f)));
                yield return new WaitForSeconds(0.4f);
            }
            CustomerAgent first = Director.SpawnCustomer();
            yield return new WaitForSeconds(0.4f);
            CustomerAgent second = Director.SpawnCustomer();
            Assert.That(first.Logic.State, Is.EqualTo(CustomerState.Queueing));
            Assert.That(second.Logic.State, Is.EqualTo(CustomerState.Queueing));
            yield return new WaitForSeconds(2f);
            Assert.That(Vector2.Distance(first.transform.position, Director.Layout.QueueSpot(0)), Is.LessThan(0.3f), "first in line at the front spot");
            Assert.That(Vector2.Distance(second.transform.position, Director.Layout.QueueSpot(1)), Is.LessThan(0.3f), "second behind them");
            Assert.That(first.ShowsPatience, "queueing customers show their patience");
            // Queueing customers stand still at their spot (no walking on the spot).
            Vector2 queued = first.transform.position;
            for (int i = 0; i < 30; i++)
            {
                yield return null;
                if (first.Logic.State != CustomerState.Queueing) break;
                Assert.That(Vector2.Distance(first.transform.position, queued), Is.LessThan(0.01f), "standing still in the queue");
                Assert.That(first.Look.Current, Is.EqualTo(Hearthdelve.Shared.Animation.CharacterAnim.Idle));
            }

            yield return WaitUntil(() => first.IsSeated && second.IsSeated, 20f, "the queue to take the seats the impatient diners left");
            Assert.That(diners.Count(d => d == null) + diners.Count(d => d != null && d.Logic.State == CustomerState.Leaving), Is.GreaterThanOrEqualTo(2));
        }

        [UnityTest]
        public IEnumerator WhenPatienceRunsOut_TheyWalkOutUpset_AndLeaveByTheDoor()
        {
            yield return Open();
            CustomerAgent agent = Director.SpawnCustomer(Impatient(orderPatience: 1f));
            yield return WaitUntil(() => agent.Logic.State == CustomerState.WaitingForFood, 15f, "them to sit and order");
            Assert.That(agent.ShowsPatience, "waiting for food shows patience");
            if (agent.Logic.Order.icon != null) Assert.That(agent.BubbleIcon, Is.SameAs(agent.Logic.Order.icon), "the bubble shows their order");
            yield return WaitUntil(() => agent.Logic.State == CustomerState.Leaving, 3f, "patience to run out");
            Assert.That(agent.Logic.Departure, Is.EqualTo(Departure.WalkedOut));
            Assert.That(agent.IsSeated, Is.False, "they get up");
            Assert.That(agent.BubbleIcon.name, Does.Contain("Angry"), "upset as they go");
            Assert.That(Director.Session.Ledger.Walkouts, Is.EqualTo(1));
            yield return WaitUntil(() => agent == null, 15f, "them to walk out the door");
            Assert.That(Director.Agents, Is.Empty);
        }

        [UnityTest]
        public IEnumerator Customers_WalkThroughThePlayer_WithoutShovingThem()
        {
            yield return Open();
            Assert.That(Physics2D.GetIgnoreLayerCollision(LayerMask.NameToLayer(Layers.Npcs), LayerMask.NameToLayer(Layers.Player)), "customers and the player don't collide");
            Assert.That(Physics2D.GetIgnoreLayerCollision(LayerMask.NameToLayer(Layers.Npcs), LayerMask.NameToLayer(Layers.Npcs)), "nor customers with each other");
            Teleport(Player, Director.Layout.Door + new Vector2(0f, 1.2f));
            yield return new WaitForFixedUpdate();
            Vector2 standing = Player.transform.position;
            CustomerAgent agent = Director.SpawnCustomer();
            yield return new WaitForSeconds(2f);
            Assert.That(Vector2.Distance(Player.transform.position, standing), Is.LessThan(0.05f), "the player wasn't moved");
            Assert.That(Vector2.Distance(agent.transform.position, Director.Layout.Door), Is.GreaterThan(1f), "the customer got past");
        }

        /// <summary>Readability and determinism: every layer shows the same animation and frame; the look never changes.</summary>
        [UnityTest]
        public IEnumerator TheLayeredLook_StaysInStep_AndNeverChanges()
        {
            yield return Open();
            CustomerAgent agent = Director.SpawnCustomer();
            AppearanceChoice look = agent.Appearance;
            var seenFacings = new HashSet<Facing4>();
            float until = Time.time + 8f;
            while (Time.time < until && !agent.IsSeated)
            {
                yield return null;
                seenFacings.Add(agent.Look.Facing);
                var shown = agent.Look.Layers.Where(l => l.enabled && l.sprite != null).Select(l => l.sprite.name).ToList();
                Assert.That(shown.Count, Is.GreaterThanOrEqualTo(4), "body, trousers, top and head at least");
                // Sprite names end in _frame_row and start with the animation: all layers must match.
                var suffixes = shown.Select(n => n.Substring(n.LastIndexOf('_', n.LastIndexOf('_') - 1))).Distinct().ToList();
                Assert.That(suffixes.Count, Is.EqualTo(1), $"layers out of step: {string.Join(", ", shown)}");
                Assert.That(shown.Select(n => n.StartsWith("NpcWalk")).Distinct().Count(), Is.EqualTo(1), "all walking or all idle");
                Assert.That(agent.Appearance, Is.EqualTo(look), "the look never changes");
            }
            Assert.That(agent.IsSeated);
            Assert.That(agent.Appearance, Is.EqualTo(look));
            Assert.That(seenFacings.Count, Is.GreaterThanOrEqualTo(2), "facing follows the walk");
        }

        [UnityTest]
        public IEnumerator Pip_WalksToTheirPost()
        {
            yield return Open();
            StaffAgent pip = Director.Staff;
            Assert.That(pip, Is.Not.Null);
            Assert.That(pip.Assignment, Is.EqualTo(StaffStation.Serving), "Pip starts on serving");
            yield return WaitUntil(() => pip.AtPost, 10f, "Pip to reach the serving post");
            Assert.That(Vector2.Distance(pip.transform.position, Director.Layout.PostFor(StaffStation.Serving)), Is.LessThan(0.25f));
            // Step 2 playtest: Pip "vibrated" at the pass, overshooting the post and turning back every frame.
            var look = pip.GetComponentInChildren<Hearthdelve.Shared.Animation.LayeredSpriteAnimator>();
            // The interpolated sprite catches up with the stopped body within a physics step or two.
            yield return new WaitForSeconds(0.2f);
            Vector2 standing = pip.transform.position;
            Facing4 facing = look.Facing;
            for (int i = 0; i < 90; i++)
            {
                yield return null;
                Assert.That(Vector2.Distance(pip.transform.position, standing), Is.LessThan(0.01f), "Pip stands still at the post");
                Assert.That(look.Facing, Is.EqualTo(facing), "without turning back and forth");
                Assert.That(look.Current, Is.EqualTo(Hearthdelve.Shared.Animation.CharacterAnim.Idle), "idle, not walking on the spot");
            }
            Assert.That(pip.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer(Layers.Npcs)));
        }

        /// <summary>4f (D16): seating is the placed chairs that face a table; the starting layout has three tables and six seats.</summary>
        [UnityTest]
        public IEnumerator Seating_IsThePlacedChairsFacingTables()
        {
            yield return Open();
            Assert.That(Director.Layout.Seats.Count, Is.EqualTo(6));
            Assert.That(Director.ActiveSeats, Is.EqualTo(6));
            NavGrid grid = NavGrid.Current;
            Assert.That(grid.Map.IsWalkable(grid.Space.ToCell(new Vector2(23f, 4.5f))), "where 4e kept a fourth table for the seat upgrade, the floor is open");
        }

        [UnityTest]
        public IEnumerator EndingService_SendsEveryoneHome()
        {
            yield return Open();
            CustomerAgent a = Director.SpawnCustomer();
            yield return new WaitForSeconds(1f);
            CustomerAgent b = Director.SpawnCustomer();
            yield return new WaitForSeconds(0.5f);
            Director.EndServiceNow();
            Assert.That(a.Logic.State, Is.EqualTo(CustomerState.Leaving));
            Assert.That(b.Logic.State, Is.EqualTo(CustomerState.Leaving));
            Assert.That(Director.SpawnCustomer(), Is.Null, "no one comes in after closing");
            yield return WaitUntil(() => a == null && b == null, 15f, "both to leave");
        }
    }
}
