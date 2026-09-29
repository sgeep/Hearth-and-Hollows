using System;

namespace Hearthdelve.Shared.Ingredients
{
    /// <summary>
    /// One harvested part: definition + quality + prep state. Two items stack only when all
    /// three match. (Freshness, when added, will need its own stacking rule.)
    /// </summary>
    public readonly struct IngredientItem : IEquatable<IngredientItem>
    {
        public readonly IngredientDefinition Definition;
        public readonly Quality Quality;
        public readonly PrepState Prep;

        public IngredientItem(IngredientDefinition definition, Quality quality, PrepState prep = PrepState.Raw)
        {
            Definition = definition;
            Quality = quality;
            Prep = prep;
        }

        public bool IsValid => Definition != null;

        public bool Equals(IngredientItem other) =>
            ReferenceEquals(Definition, other.Definition) && Quality == other.Quality && Prep == other.Prep;

        public override bool Equals(object obj) => obj is IngredientItem other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(Definition != null ? Definition.GetInstanceID() : 0, (int)Quality, (int)Prep);

        public static bool operator ==(IngredientItem a, IngredientItem b) => a.Equals(b);
        public static bool operator !=(IngredientItem a, IngredientItem b) => !a.Equals(b);

        public override string ToString() =>
            $"{(Definition != null ? Definition.id : "<none>")} [{Quality}, {Prep}]";
    }
}
