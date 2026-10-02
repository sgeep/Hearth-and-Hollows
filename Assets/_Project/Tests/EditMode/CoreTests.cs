using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon.Enemies;
using Hearthdelve.Shared.Ingredients;
using NUnit.Framework;

namespace Hearthdelve.Tests
{
    public class EventBusTests
    {
        struct Ping : IEvent
        {
            public int Value;
        }

        [SetUp, TearDown]
        public void Reset() => EventBusRegistry.ClearAll();

        [Test]
        public void Publish_ReachesSubscribers_UntilUnsubscribed()
        {
            int received = 0;
            void Handler(Ping p) => received += p.Value;

            EventBus<Ping>.Subscribe(Handler);
            EventBus<Ping>.Publish(new Ping { Value = 2 });
            EventBus<Ping>.Unsubscribe(Handler);
            EventBus<Ping>.Publish(new Ping { Value = 5 });

            Assert.That(received, Is.EqualTo(2));
        }

        [Test]
        public void DuplicateSubscribe_IsIgnored()
        {
            int received = 0;
            void Handler(Ping p) => received++;
            EventBus<Ping>.Subscribe(Handler);
            EventBus<Ping>.Subscribe(Handler);
            EventBus<Ping>.Publish(default);
            Assert.That(received, Is.EqualTo(1));
        }

        [Test]
        public void HandlerMayUnsubscribeDuringPublish()
        {
            int received = 0;
            void Handler(Ping p)
            {
                received++;
                EventBus<Ping>.Unsubscribe(Handler);
            }
            EventBus<Ping>.Subscribe(Handler);
            EventBus<Ping>.Publish(default);
            EventBus<Ping>.Publish(default);
            Assert.That(received, Is.EqualTo(1));
        }

        [Test]
        public void ClearAll_RemovesHandlers()
        {
            EventBus<Ping>.Subscribe(_ => { });
            EventBusRegistry.ClearAll();
            Assert.That(EventBus<Ping>.HandlerCount, Is.EqualTo(0));
        }
    }

    public class QualityTests
    {
        [TestCase(Quality.Standard, 1, Quality.Fine)]
        [TestCase(Quality.Standard, -1, Quality.Poor)]
        [TestCase(Quality.Premium, 2, Quality.Premium)]
        [TestCase(Quality.Poor, -3, Quality.Poor)]
        public void Shift_ClampsToRange(Quality from, int tiers, Quality expected) =>
            Assert.That(from.Shift(tiers), Is.EqualTo(expected));
    }

    public class AttackCycleTests
    {
        [Test]
        public void RunsThroughAllPhases_BackToReady()
        {
            var cycle = new AttackCycle(0.5f, 0.2f, 0.3f, 1f);
            var phases = new System.Collections.Generic.List<EnemyAttackPhase>();
            cycle.PhaseChanged += phases.Add;

            Assert.That(cycle.TryStart());
            for (int i = 0; i < 200; i++) cycle.Tick(1f / 60f);

            Assert.That(phases, Is.EqualTo(new[]
            {
                EnemyAttackPhase.Telegraph, EnemyAttackPhase.Active, EnemyAttackPhase.Recovery,
                EnemyAttackPhase.Cooldown, EnemyAttackPhase.Ready,
            }));
        }

        [Test]
        public void TelegraphLastsConfiguredTime()
        {
            var cycle = new AttackCycle(0.5f, 0.2f, 0.3f, 1f);
            cycle.TryStart();
            cycle.Tick(0.49f);
            Assert.That(cycle.Phase, Is.EqualTo(EnemyAttackPhase.Telegraph));
            cycle.Tick(0.02f);
            Assert.That(cycle.Phase, Is.EqualTo(EnemyAttackPhase.Active));
        }

        [Test]
        public void CannotStartWhileBusy()
        {
            var cycle = new AttackCycle(0.5f, 0.2f, 0.3f, 1f);
            cycle.TryStart();
            Assert.That(cycle.TryStart(), Is.False);
        }

        [Test]
        public void Interrupt_SkipsToCooldown()
        {
            var cycle = new AttackCycle(0.5f, 0.2f, 0.3f, 1f);
            cycle.TryStart();
            cycle.Tick(0.1f);
            cycle.Interrupt();
            Assert.That(cycle.Phase, Is.EqualTo(EnemyAttackPhase.Cooldown));
            Assert.That(cycle.IsAttacking, Is.False);
        }

        [Test]
        public void LargeTick_PassesThroughShortPhases()
        {
            var cycle = new AttackCycle(0.1f, 0.1f, 0.1f, 0.1f);
            cycle.TryStart();
            cycle.Tick(0.25f);
            Assert.That(cycle.Phase, Is.EqualTo(EnemyAttackPhase.Recovery));
        }
    }
}
