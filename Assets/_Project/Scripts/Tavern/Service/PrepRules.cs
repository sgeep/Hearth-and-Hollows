using System.Collections.Generic;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;

namespace Hearthdelve.Tavern.Service
{
    /// <summary>One step of preparing a dish, as the prep screen shows it.</summary>
    public enum PrepStep
    {
        Grill,
        Tap,
        Chop,
        Simmer,
    }

    /// <summary>
    /// The evening's prep (GDD §3.1): what a dish takes, how many the storeroom can make, the menu, and
    /// whether the doors can open. Pure logic.
    /// </summary>
    public static class PrepRules
    {
        static readonly PrepStep[] k_Grill = { PrepStep.Grill };
        static readonly PrepStep[] k_Tap = { PrepStep.Tap };
        static readonly PrepStep[] k_Stew = { PrepStep.Chop, PrepStep.Simmer };

        /// <summary>
        /// How a dish is prepared, step by step. Today a dish is one station, and the Stew Pot is two steps
        /// (chop, then simmer); dishes with more stages (GDD §5.4) will list theirs here, and the screens draw
        /// whatever list they get.
        /// </summary>
        public static IReadOnlyList<PrepStep> Steps(RecipeDefinition recipe) => recipe == null ? System.Array.Empty<PrepStep>() : recipe.station switch
        {
            CookStation.Tap => k_Tap,
            CookStation.StewPot => k_Stew,
            _ => k_Grill,
        };

        /// <summary>How many the storeroom can make: servings, or for a stew, whole pots (each serves several helpings).</summary>
        public static int Makeable(RecipeDefinition recipe, Storeroom storeroom) =>
            recipe != null && storeroom != null ? RecipeMatcher.ServingsAvailable(recipe, storeroom) : 0;

        /// <summary>The doors can open when at least one dish on the menu can be made.</summary>
        public static bool CanOpen(IReadOnlyList<RecipeDefinition> menu, Storeroom storeroom)
        {
            if (menu == null) return false;
            foreach (RecipeDefinition recipe in menu)
                if (Makeable(recipe, storeroom) > 0) return true;
            return false;
        }

        /// <summary>Whether anything on the full recipe list can be made (if not, the honest choice is closing for the night).</summary>
        public static bool AnythingCookable(IEnumerable<RecipeDefinition> recipes, Storeroom storeroom)
        {
            foreach (RecipeDefinition recipe in recipes)
                if (Makeable(recipe, storeroom) > 0) return true;
            return false;
        }

        /// <summary>
        /// Adds a dish to the menu, or takes it off. A full menu doesn't take more (the player chooses what to drop).
        /// Returns whether it's on the menu afterwards.
        /// </summary>
        public static bool Toggle(List<RecipeDefinition> menu, RecipeDefinition recipe, int maxSize)
        {
            if (recipe == null) return false;
            if (menu.Remove(recipe)) return false;
            if (menu.Count >= maxSize) return false;
            menu.Add(recipe);
            return true;
        }
    }

    /// <summary>A line of the evening's results.</summary>
    public enum EveningLine
    {
        Served,
        Gold,
        Tips,
        Renown,
        Walkouts,
        SoldOut,
        Dropped,
    }

    /// <summary>
    /// What the evening came to, as the results screen tells it: what was earned first, then what went
    /// wrong, if anything (lines for walkouts, sell-out leaves and dropped plates only appear when they
    /// happened). Takings are payments plus tips. Pure logic.
    /// </summary>
    public sealed class EveningReport
    {
        public readonly List<(EveningLine line, int value)> Lines = new();
        public int Takings { get; }
        public bool ClosedEarly { get; }
        /// <summary>The doors never opened (closed for the night at prep).</summary>
        public bool StayedShut { get; }

        public EveningReport(ServiceLedger ledger, bool closedEarly, bool stayedShut)
        {
            ClosedEarly = closedEarly;
            StayedShut = stayedShut;
            if (stayedShut || ledger == null) return;
            Lines.Add((EveningLine.Served, ledger.DishesServed));
            Lines.Add((EveningLine.Gold, ledger.Gold));
            Lines.Add((EveningLine.Tips, ledger.Tips));
            Lines.Add((EveningLine.Renown, ledger.Renown));
            if (ledger.Walkouts > 0) Lines.Add((EveningLine.Walkouts, ledger.Walkouts));
            if (ledger.SoldOutLeaves > 0) Lines.Add((EveningLine.SoldOut, ledger.SoldOutLeaves));
            if (ledger.DroppedDishes > 0) Lines.Add((EveningLine.Dropped, ledger.DroppedDishes));
            Takings = ledger.Gold + ledger.Tips;
        }
    }
}
