using System;

namespace Hearthdelve.Shared.Ingredients
{
    /// <summary>Ingredient category (GDD §5.1). Flags so weapons can list several clean-kill categories.</summary>
    [Flags]
    public enum IngredientCategory
    {
        None = 0,
        Meat = 1 << 0,
        Offal = 1 << 1,
        Fish = 1 << 2,
        Fungus = 1 << 3,
        Plant = 1 << 4,
        Egg = 1 << 5,
        Spice = 1 << 6,
        Liquid = 1 << 7,
        Magical = 1 << 8,
        /// <summary>Bread, malt and other grain (4f Checkpoint C, D19).</summary>
        Grain = 1 << 9,
    }

    /// <summary>
    /// Where an ingredient usually comes from (GDD §5.1, §5.5). Recipes never ask for a source, only ingredients or
    /// categories, so later sources (the village shop, farming, ranching, fishing, villagers) plug in without touching them.
    /// Only the Market and the Hollows have gameplay in 4f.
    /// </summary>
    public enum IngredientSource
    {
        Hollows,
        Market,
        Farm,
        Ranch,
        Fishing,
        Villager,
    }

    [Flags]
    public enum FlavorTags
    {
        None = 0,
        Savory = 1 << 0,
        Sweet = 1 << 1,
        Spicy = 1 << 2,
        Sour = 1 << 3,
        Bitter = 1 << 4,
        Umami = 1 << 5,
        Earthy = 1 << 6,
        Arcane = 1 << 7,
    }

    /// <summary>Harvest quality tier. Ordered so tiers can be shifted arithmetically.</summary>
    public enum Quality
    {
        Poor = 0,
        Standard = 1,
        Fine = 2,
        Premium = 3,
    }

    /// <summary>How the part arrives, set by the killing element (GDD §4.3).</summary>
    public enum PrepState
    {
        Raw = 0,
        Seared = 1,
        Chilled = 2,
        Inedible = 3,
    }

    public enum Rarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
    }

    /// <summary>Damage element. Shared because the tavern cares about Seared/Chilled parts too.</summary>
    public enum Element
    {
        None = 0,
        Fire = 1,
        Ice = 2,
        Poison = 3,
    }

    public static class QualityExtensions
    {
        public const Quality Min = Quality.Poor;
        public const Quality Max = Quality.Premium;

        /// <summary>Shift by whole tiers, clamped to [Poor, Premium].</summary>
        public static Quality Shift(this Quality quality, int tiers)
        {
            int value = Math.Clamp((int)quality + tiers, (int)Min, (int)Max);
            return (Quality)value;
        }
    }
}
