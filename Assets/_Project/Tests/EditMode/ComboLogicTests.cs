using System.Collections.Generic;
using Hearthdelve.Dungeon.Combat;
using NUnit.Framework;

namespace Hearthdelve.Tests
{
    public class ComboLogicTests
    {
        const float Dt = 1f / 60f;

        // startup 4, active 2, recovery 10, cancel at frame 10 → total 16 frames.
        static AttackData Attack(string name) => new()
        {
            debugName = name,
            startupFrames = 4,
            activeFrames = 2,
            recoveryFrames = 10,
            cancelFrame = 10,
        };

        static ComboLogic Make(out List<int> started)
        {
            var combo = new ComboLogic(new[] { Attack("A"), Attack("B"), Attack("C") }, inputBuffer: 0.15f, linkWindow: 0.2f);
            var list = new List<int>();
            combo.AttackStarted += list.Add;
            started = list;
            return combo;
        }

        static void Frames(ComboLogic combo, int frames)
        {
            for (int i = 0; i < frames; i++) combo.Tick(Dt);
        }

        [Test]
        public void Press_StartsFirstAttack_ThroughStartupActiveRecovery()
        {
            var combo = Make(out var started);
            var activeStarts = new List<int>();
            combo.ActiveStarted += activeStarts.Add;

            combo.PressAttack();
            combo.Tick(Dt);
            Assert.That(started, Is.EqualTo(new[] { 0 }));
            Assert.That(combo.Phase, Is.EqualTo(AttackPhase.Startup));

            Frames(combo, 5);            // elapsed 5/60: inside active (4..6)
            Assert.That(combo.Phase, Is.EqualTo(AttackPhase.Active));
            Assert.That(activeStarts, Is.EqualTo(new[] { 0 }));

            Frames(combo, 2);            // 7/60
            Assert.That(combo.Phase, Is.EqualTo(AttackPhase.Recovery));

            Frames(combo, 11);
            Assert.That(combo.Phase, Is.EqualTo(AttackPhase.Idle));
        }

        [Test]
        public void PressDuringCancelWindow_ChainsToNextHit()
        {
            var combo = Make(out var started);
            combo.PressAttack();
            combo.Tick(Dt);
            Frames(combo, 10);            // reach cancel frame
            combo.PressAttack();
            combo.Tick(Dt);
            Assert.That(started, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void EarlyPress_IsBuffered_UntilCancelFrame()
        {
            var combo = Make(out var started);
            combo.PressAttack();
            combo.Tick(Dt);
            Frames(combo, 3);             // frame ~4: in startup
            combo.PressAttack();          // early, but within 0.15 s of the cancel frame
            Frames(combo, 3);
            Assert.That(started, Is.EqualTo(new[] { 0 }), "must not cancel before the cancel frame");
            Frames(combo, 5);
            Assert.That(started, Is.EqualTo(new[] { 0, 1 }), "buffered press fires at the cancel frame");
        }

        [Test]
        public void ThreeHits_ThenWrapsToFirst()
        {
            var combo = Make(out var started);
            for (int hit = 0; hit < 3; hit++)
            {
                combo.PressAttack();
                Frames(combo, 11);
            }
            Frames(combo, 30);            // let the last hit fully recover and the link lapse
            combo.PressAttack();
            combo.Tick(Dt);
            Assert.That(started, Is.EqualTo(new[] { 0, 1, 2, 0 }));
        }

        [Test]
        public void PressWithinLinkWindow_AfterRecovery_ContinuesCombo()
        {
            var combo = Make(out var started);
            combo.PressAttack();
            Frames(combo, 18);            // hit 1 fully recovered
            Assert.That(combo.IsBusy, Is.False);
            Frames(combo, 5);             // ~0.08 s later, inside the 0.2 s link window
            combo.PressAttack();
            combo.Tick(Dt);
            Assert.That(started, Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void PressAfterLinkWindow_RestartsCombo()
        {
            var combo = Make(out var started);
            combo.PressAttack();
            Frames(combo, 18);
            Frames(combo, 30);            // 0.5 s > link window
            combo.PressAttack();
            combo.Tick(Dt);
            Assert.That(started, Is.EqualTo(new[] { 0, 0 }));
        }

        [Test]
        public void Cancel_BreaksChain_AndEndsActiveFrames()
        {
            var combo = Make(out var started);
            int ended = 0;
            combo.ActiveEnded += _ => ended++;
            combo.PressAttack();
            Frames(combo, 6);             // elapsed 5/60: active
            Assert.That(combo.IsActive);
            combo.Cancel();
            Assert.That(ended, Is.EqualTo(1));
            Assert.That(combo.IsBusy, Is.False);

            combo.PressAttack();
            combo.Tick(Dt);
            Assert.That(started, Is.EqualTo(new[] { 0, 0 }), "cancel resets to the first hit");
        }
    }
}
