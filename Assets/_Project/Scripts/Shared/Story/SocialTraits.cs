using System;
using UnityEngine;

namespace Hearthdelve.Shared.Story
{
    /// <summary>
    /// The few things characters value, and the same things a deed shows (4g, plan §7): Love/Hate's personality traits, in this
    /// order. Craft is good cooking and good work, Nerve is daring in the Hollows, Warmth is looking after people.
    /// </summary>
    [Serializable]
    public struct SocialTraits
    {
        public const int Count = 3;
        public static readonly string[] Names = { "Craft", "Nerve", "Warmth" };

        [Range(-100, 100)] public float craft;
        [Range(-100, 100)] public float nerve;
        [Range(-100, 100)] public float warmth;

        public SocialTraits(float craft, float nerve, float warmth)
        {
            this.craft = craft;
            this.nerve = nerve;
            this.warmth = warmth;
        }

        public float this[int i] => i switch { 0 => craft, 1 => nerve, 2 => warmth, _ => throw new ArgumentOutOfRangeException(nameof(i)) };

        /// <summary>In Love/Hate's order (<see cref="Names"/>).</summary>
        public float[] ToArray() => new[] { craft, nerve, warmth };
    }
}
