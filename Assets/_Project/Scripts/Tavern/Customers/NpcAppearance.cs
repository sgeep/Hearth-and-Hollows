using System;
using System.Collections.Generic;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Animation;
using UnityEngine;

namespace Hearthdelve.Tavern.Customers
{
    /// <summary>
    /// What a kind of customer can look like (A Myriad of NPCs layers): bodies, tops, trousers, heads
    /// (a hairstyle or a hat) and beards. Curated for readability at 320×180: tops in colours that
    /// stand out against the tavern floor, few layers. Presentation data only.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/NPC Appearance Pool", fileName = "Appearance")]
    public sealed class NpcAppearancePool : ScriptableObject
    {
        public List<SpriteAnimationSet> bodies = new();
        public List<SpriteAnimationSet> tops = new();
        public List<SpriteAnimationSet> trousers = new();
        [Tooltip("Hairstyles and hats: one is worn.")]
        public List<SpriteAnimationSet> heads = new();
        public List<SpriteAnimationSet> beards = new();
        [Range(0, 1)] public float beardChance;

        /// <summary>The layers to draw, back to front (body, trousers, top, beard, head); null for none.</summary>
        public SpriteAnimationSet[] Layers(AppearanceChoice choice) => new[]
        {
            At(bodies, choice.Body), At(trousers, choice.Trousers), At(tops, choice.Top), At(beards, choice.Beard), At(heads, choice.Head),
        };

        public AppearanceChoice Pick(int seed) =>
            AppearanceRules.Pick(seed, bodies.Count, tops.Count, trousers.Count, heads.Count, beards.Count, beardChance);

        static SpriteAnimationSet At(List<SpriteAnimationSet> list, int index) => index >= 0 && index < list.Count ? list[index] : null;
    }

    /// <summary>Which option of each layer a customer wears (-1 for none).</summary>
    [Serializable]
    public struct AppearanceChoice : IEquatable<AppearanceChoice>
    {
        public int Body, Top, Trousers, Head, Beard;

        public bool Equals(AppearanceChoice other) =>
            Body == other.Body && Top == other.Top && Trousers == other.Trousers && Head == other.Head && Beard == other.Beard;

        public override bool Equals(object obj) => obj is AppearanceChoice other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Body, Top, Trousers, Head, Beard);
        public override string ToString() => $"body {Body}, top {Top}, trousers {Trousers}, head {Head}, beard {Beard}";
    }

    /// <summary>
    /// Picks a customer's look from a seed (4c decision 3): the same seed always gives the same look, so a
    /// customer keeps theirs for as long as they exist. Pure logic.
    /// </summary>
    public static class AppearanceRules
    {
        public static AppearanceChoice Pick(int seed, int bodies, int tops, int trousers, int heads, int beards, float beardChance)
        {
            var random = new SeededRandom(seed);
            return new AppearanceChoice
            {
                Body = Index(random, bodies),
                Top = Index(random, tops),
                Trousers = Index(random, trousers),
                Head = Index(random, heads),
                Beard = beards > 0 && random.Value() < beardChance ? Index(random, beards) : -1,
            };
        }

        static int Index(IRandom random, int count) => count > 0 ? random.Range(0, count - 1) : -1;
    }
}
