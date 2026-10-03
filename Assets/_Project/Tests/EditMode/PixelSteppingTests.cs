using System.Collections.Generic;
using Hearthdelve.Core.Movement;
using NUnit.Framework;
using UnityEngine;

namespace Hearthdelve.Tests
{
    public class PixelSteppingTests
    {
        /// <summary>Moves a target at a constant velocity (art pixels per frame) and records each display step.</summary>
        static List<Vector2Int> Walk(Vector2 start, Vector2 velocity, int frames, float tolerance = PixelStepping.FollowAlone)
        {
            var display = Vector2Int.RoundToInt(start);
            var state = new PixelStepping.State();
            var steps = new List<Vector2Int>();
            Vector2 target = start;
            for (int i = 0; i < frames; i++)
            {
                target += velocity;
                Vector2Int next = PixelStepping.Step(display, target, velocity, ref state);
                steps.Add(next - display);
                display = next;
                Assert.That(Mathf.Abs(target.x - display.x), Is.LessThanOrEqualTo(tolerance + 1e-4f), $"frame {i}: display drifted on X");
                Assert.That(Mathf.Abs(target.y - display.y), Is.LessThanOrEqualTo(tolerance + 1e-4f), $"frame {i}: display drifted on Y");
            }
            return steps;
        }

        [TestCase(0f, 0f, 0.566f, 0.566f)]
        [TestCase(0.37f, 0.81f, 0.566f, 0.566f)]
        [TestCase(0.6f, 0.1f, 0.566f, 0.566f)]
        [TestCase(0.49f, 0.51f, 0.566f, 0.566f)]
        [TestCase(0.13f, 0.85f, 0.566f, -0.566f)]
        [TestCase(0.72f, 0.31f, -0.3f, 0.3f)]
        public void Diagonal_StepsBothAxesTogether_WhateverTheStartingPhase(float phaseX, float phaseY, float vx, float vy)
        {
            // A 45° walk; 0.57 px per frame per axis is 6 tiles/s at 8 PPU and 60 fps.
            List<Vector2Int> steps = Walk(new Vector2(10f + phaseX, 20f + phaseY), new Vector2(vx, vy), 120);
            int single = 0, both = 0;
            foreach (Vector2Int s in steps)
            {
                if (s.x != 0 && s.y != 0) both++;
                else if (s.x != 0 || s.y != 0) single++;
            }
            Assert.That(both, Is.GreaterThanOrEqualTo((int)(Mathf.Abs(vx) * 120f) - 3), "nearly every step moves both axes");
            Assert.That(single, Is.LessThanOrEqualTo(1), "only the first step may need to align the phases");
            Assert.That(steps.TrueForAll(s => Mathf.Abs(s.x) <= 1 && Mathf.Abs(s.y) <= 1), "walking never jumps two pixels");
        }

        [Test]
        public void ExactDiagonal_StaysInLockstep_DespiteFloatNoise()
        {
            // Interpolated movement at 45 degrees wobbles by a few thousandths of a pixel per frame.
            var random = new System.Random(7);
            for (int walk = 0; walk < 200; walk++)
            {
                var state = new PixelStepping.State();
                var target = new Vector2(10f + (float)random.NextDouble(), 20f + (float)random.NextDouble());
                Vector2Int display = Vector2Int.RoundToInt(target);
                int single = 0;
                for (int frame = 0; frame < 150; frame++)
                {
                    var movement = new Vector2(0.566f + (float)(random.NextDouble() - 0.5) * 0.004f, 0.566f + (float)(random.NextDouble() - 0.5) * 0.004f);
                    target += movement;
                    Vector2Int next = PixelStepping.Step(display, target, movement, ref state);
                    Vector2Int step = next - display;
                    display = next;
                    Assert.That(step.x, Is.InRange(0, 1));
                    Assert.That(step.y, Is.InRange(0, 1));
                    if (frame > 5 && (step.x != 0) != (step.y != 0)) single++;
                }
                Assert.That(single, Is.LessThanOrEqualTo(2), $"walk {walk}: the lead flipped and broke the lockstep");
            }
        }

        [Test]
        public void Straight_NeverStepsTheOtherAxis()
        {
            foreach (Vector2Int s in Walk(new Vector2(3.3f, 7.4f), new Vector2(0.8f, 0f), 100, PixelStepping.ReverseStep))
                Assert.That(s.y, Is.Zero);
        }

        [Test]
        public void Steps_AreNeverBackwards_ForSteadyMovement()
        {
            foreach (Vector2Int s in Walk(new Vector2(0.2f, 0.9f), new Vector2(-0.3f, 0.45f), 200))
            {
                Assert.That(s.x, Is.LessThanOrEqualTo(0));
                Assert.That(s.y, Is.GreaterThanOrEqualTo(0));
            }
        }

        [TestCase(0.8f, 0.05f)]
        [TestCase(0.05f, -0.8f)]
        [TestCase(0.7f, 0.2f)]
        public void ShallowAngles_NeverStepBackAndForth(float vx, float vy)
        {
            List<Vector2Int> steps = Walk(new Vector2(2.6f, 9.3f), new Vector2(vx, vy), 300);
            foreach (Vector2Int s in steps)
            {
                Assert.That(s.x * System.Math.Sign(vx), Is.GreaterThanOrEqualTo(0), "X stepped against the movement");
                Assert.That(s.y * System.Math.Sign(vy), Is.GreaterThanOrEqualTo(0), "Y stepped against the movement");
            }
        }

        [Test]
        public void ReversingDirection_FollowsAfterAShortLag()
        {
            var display = new Vector2Int(0, 0);
            var state = new PixelStepping.State();
            Vector2 target = Vector2.zero;
            for (int i = 0; i < 20; i++) { target.x += 0.6f; display = PixelStepping.Step(display, target, new Vector2(0.6f, 0f), ref state); }
            for (int i = 0; i < 20; i++) { target.x -= 0.6f; display = PixelStepping.Step(display, target, new Vector2(-0.6f, 0f), ref state); }
            Assert.That(Mathf.Abs(target.x - display.x), Is.LessThanOrEqualTo(0.75f + 1e-4f));
            Assert.That(state.LastStep.x, Is.EqualTo(-1));
        }

        [Test]
        public void FastMovement_TakesWholePixels()
        {
            // A dodge roll: about 1.9 px per frame.
            foreach (Vector2Int s in Walk(new Vector2(0f, 0f), new Vector2(1.87f, 0f), 30))
                Assert.That(s.x, Is.InRange(1, 3));
        }

        [Test]
        public void StandingStill_NeverMoves()
        {
            var display = new Vector2Int(5, 5);
            var state = new PixelStepping.State();
            for (int i = 0; i < 10; i++)
                Assert.That(PixelStepping.Step(display, new Vector2(5.4f, 4.6f), Vector2.zero, ref state), Is.EqualTo(display));
        }

        [Test]
        public void Teleport_JumpsStraightToTheTarget()
        {
            var state = new PixelStepping.State { LastStep = new Vector2Int(1, 1), Lead = 1 };
            Assert.That(PixelStepping.Step(new Vector2Int(0, 0), new Vector2(40.4f, -12.6f), new Vector2(40f, -12f), ref state), Is.EqualTo(new Vector2Int(40, -13)));
        }
    }
}
