using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Recipes;
using UnityEngine;

namespace Hearthdelve.Tavern.Service
{
    [Serializable]
    public struct StewPotSettings
    {
        [Min(0), Tooltip("Seconds a batch simmers on its own after chopping.")]
        public float simmerSeconds;
        [Min(1), Tooltip("Helpings from a batch chopped badly (score 0).")]
        public int minHelpings;
        [Min(1), Tooltip("Helpings from a batch chopped perfectly (score 1).")]
        public int maxHelpings;

        public static StewPotSettings Default => new() { simmerSeconds = 25f, minHelpings = 3, maxHelpings = 5 };
    }

    public enum PotState
    {
        Empty,
        /// <summary>Ingredients are in; someone is chopping them.</summary>
        Chopping,
        /// <summary>Cooking on its own; the helpings are known.</summary>
        Simmering,
        /// <summary>Helpings are ladled out as stew orders come in.</summary>
        Ready,
    }

    /// <summary>
    /// The stew pot (GDD §6.2): one batch of a StewPot recipe's ingredients is chopped, simmers on
    /// its own, then serves several helpings. Chopping accuracy sets how many. Driven by
    /// <see cref="ServiceSession"/>, which decides when helpings are ladled. Pure logic.
    /// </summary>
    public sealed class StewPot
    {
        readonly List<IngredientItem> m_ChopItems = new();

        internal StewPot(StewPotSettings settings) => Settings = settings;

        public StewPotSettings Settings { get; }
        public PotState State { get; private set; } = PotState.Empty;
        /// <summary>The stew in the pot (null when empty).</summary>
        public RecipeDefinition Recipe { get; private set; }
        /// <summary>Everything that went into this batch (shared by each helping for dish scoring).</summary>
        public CookedIngredients Batch { get; private set; }
        /// <summary>Helpings left (Ready) or coming (Simmering).</summary>
        public int Helpings { get; private set; }
        public float SimmerRemaining { get; private set; }
        public float SimmerProgress => Settings.simmerSeconds > 0f ? Mathf.Clamp01(1f - SimmerRemaining / Settings.simmerSeconds) : 1f;
        /// <summary>Who is chopping (player, staff). Null otherwise.</summary>
        public object ClaimedBy { get; private set; }
        /// <summary>One entry per ingredient to chop, in recipe order.</summary>
        public IReadOnlyList<IngredientItem> ChopItems => m_ChopItems;

        public static int HelpingsFor(float chopScore, in StewPotSettings s) =>
            Mathf.RoundToInt(Mathf.Lerp(s.minHelpings, Mathf.Max(s.minHelpings, s.maxHelpings), Mathf.Clamp01(chopScore)));

        /// <summary>Helpings this pot can still promise to <paramref name="recipe"/> orders (while chopping, the fewest it could make).</summary>
        public int Capacity(RecipeDefinition recipe)
        {
            if (recipe == null || recipe != Recipe) return 0;
            return State == PotState.Chopping ? Settings.minHelpings : Helpings;
        }

        internal void Fill(RecipeDefinition recipe, CookedIngredients batch, object cook)
        {
            Recipe = recipe;
            Batch = batch;
            ClaimedBy = cook;
            State = PotState.Chopping;
            Helpings = 0;
            m_ChopItems.Clear();
            foreach (var stack in batch.Used)
            {
                bool seen = false;
                foreach (var item in m_ChopItems) seen |= item.Definition == stack.Item.Definition;
                if (!seen) m_ChopItems.Add(stack.Item);
            }
        }

        internal void StartSimmering(float chopScore)
        {
            Helpings = HelpingsFor(chopScore, Settings);
            SimmerRemaining = Settings.simmerSeconds;
            ClaimedBy = null;
            State = SimmerRemaining > 0f ? PotState.Simmering : PotState.Ready;
        }

        /// <summary>Returns true when the stew has just finished simmering.</summary>
        internal bool Tick(float deltaTime)
        {
            if (State != PotState.Simmering) return false;
            SimmerRemaining -= deltaTime;
            if (SimmerRemaining > 0f) return false;
            SimmerRemaining = 0f;
            State = PotState.Ready;
            return true;
        }

        internal void TakeHelping()
        {
            if (State != PotState.Ready || Helpings <= 0) return;
            Helpings--;
            if (Helpings == 0) Empty();
        }

        internal void Empty()
        {
            State = PotState.Empty;
            Recipe = null;
            Batch = null;
            ClaimedBy = null;
            Helpings = 0;
            SimmerRemaining = 0f;
            m_ChopItems.Clear();
        }
    }
}
