using System;
using Hearthdelve.Core.Haptics;
using UnityEngine;

namespace Hearthdelve.Tavern.Minigames
{
    /// <summary>The tavern's feedback tuning (4c step 6): when a moment counts as good or bad, and how continuous rumble grows.</summary>
    [Serializable]
    public struct TavernFeedbackSettings
    {
        [Header("Grill")]
        [Range(0, 1), Tooltip("A flip scoring this or more is perfect (success pulse instead of a tap).")]
        public float perfectFlip;
        [Tooltip("Rumble as doneness runs from the band's end (0) to burning (1). Nothing before the band ends, so normal cooking is quiet.")]
        public HapticRamp burnWarning;

        [Header("Tap")]
        [Range(0, 1), Tooltip("Low motor while pouring: the steady feel of the stream.")]
        public float pourBase;
        [Tooltip("High motor as the glass fills, by fill relative to the line (1 = at the line).")]
        public HapticRamp pourRise;
        [Range(0, 1), Tooltip("A pour scoring this or more gets the clean completion cue.")]
        public float cleanPour;

        [Header("Chop")]
        [Range(0, 1), Tooltip("A cut scoring this or more is clean (crisp); below, ragged.")]
        public float cleanCut;
        [Range(0, 1), Tooltip("A board averaging this or more finishes with the success cue.")]
        public float goodChop;

        [Header("Serving")]
        [Min(0), Tooltip("A bump this strong or stronger is hard (the strength is closing speed over carry speed, 0.5–1.5).")]
        public float hardBump;
        [Range(0, 1), Tooltip("Spill past this warns once that the plate is close to falling.")]
        public float spillWarning;

        public static TavernFeedbackSettings Default => new()
        {
            perfectFlip = 0.9f,
            burnWarning = new HapticRamp(0f, 1f, 0.12f, 0.8f, 1.6f),
            pourBase = 0.12f,
            pourRise = new HapticRamp(0.6f, 1.15f, 0.04f, 0.85f, 1.5f),
            cleanPour = 0.85f,
            cleanCut = 0.75f,
            goodChop = 0.75f,
            hardBump = 1.0f,
            spillWarning = 0.66f,
        };
    }

    public enum FlipFeel { Flip, Perfect, Burned }
    public enum PourResult { Plain, Clean, Overflow }
    public enum CutFeel { Clean, Ragged }
    public enum BumpFeel { Soft, Hard }

    /// <summary>
    /// What the cooking and serving moments feel like, from the minigames' own state (feedback observes; it never
    /// times anything itself). Gameplay values in, named moments and 0–1 intensities out. Pure logic.
    /// </summary>
    public static class TavernFeedbackRules
    {
        /// <summary>The grill's warning rumble at doneness <paramref name="meter"/>: zero until the band ends, rising to the burn.</summary>
        public static float GrillWarning(float meter, in GrillSettings grill, in TavernFeedbackSettings s)
        {
            if (meter <= grill.bandMax) return 0f;
            float toBurn = Mathf.InverseLerp(grill.bandMax, GrillMinigame.BurnAt, meter);
            return HapticMath.Clamp01(s.burnWarning.Evaluate(toBurn));
        }

        /// <summary>A side just finished with <paramref name="score"/>: a plain flip, a perfect one, or burned.</summary>
        public static FlipFeel Flip(float score, bool burned, in TavernFeedbackSettings s) =>
            burned ? FlipFeel.Burned : score >= s.perfectFlip ? FlipFeel.Perfect : FlipFeel.Flip;

        /// <summary>Motors while pouring: a steady low stream, and a high buzz that rises as the glass nears the line and beyond.</summary>
        public static (float low, float high) Pour(bool pouring, float total, in TapSettings tap, in TavernFeedbackSettings s)
        {
            if (!pouring) return (0f, 0f);
            float toLine = tap.fillLine > 0f ? total / tap.fillLine : 1f;
            return (s.pourBase, HapticMath.Clamp01(s.pourRise.Evaluate(toLine)));
        }

        /// <summary>The fill crossed into the line's tolerance this step (the crisp "you're there" cue).</summary>
        public static bool ReachedLine(float before, float after, in TapSettings tap)
        {
            float line = tap.fillLine - tap.fillTolerance;
            return before < line && after >= line;
        }

        public static PourResult Pour(bool overflowed, float score, in TavernFeedbackSettings s) =>
            overflowed ? PourResult.Overflow : score >= s.cleanPour ? PourResult.Clean : PourResult.Plain;

        public static CutFeel Cut(float score, in TavernFeedbackSettings s) => score >= s.cleanCut ? CutFeel.Clean : CutFeel.Ragged;

        public static bool GoodChop(float average, in TavernFeedbackSettings s) => average >= s.goodChop;

        /// <summary>A bump on the carried plate: soft or hard, and how strongly (strength 0.5–1.5 maps to 0.4–1).</summary>
        public static (BumpFeel feel, float intensity) Bump(float strength, in TavernFeedbackSettings s) =>
            (strength >= s.hardBump ? BumpFeel.Hard : BumpFeel.Soft, Mathf.Lerp(0.4f, 1f, Mathf.InverseLerp(0.5f, 1.5f, strength)));

        /// <summary>The spill meter just went past the warning line (once per plate: it only rises).</summary>
        public static bool SpillWarning(float before, float after, in TavernFeedbackSettings s) => before < s.spillWarning && after >= s.spillWarning;
    }

}
