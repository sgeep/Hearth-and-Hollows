using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.UI.Localization;
using UnityEngine.InputSystem;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>Small localized-text helpers shared by the tavern screens.</summary>
    public static class TavernUI
    {
        public static string Station(CookStation station) => Loc.UI(station switch
        {
            CookStation.Tap => TavernLocKeys.StationTap,
            CookStation.StewPot => TavernLocKeys.StationStewPot,
            _ => TavernLocKeys.StationGrill,
        });

        public static string Station(StaffStation station) => station switch
        {
            StaffStation.Grill => Loc.UI(TavernLocKeys.StationGrill),
            StaffStation.Tap => Loc.UI(TavernLocKeys.StationTap),
            StaffStation.Serving => Loc.UI(TavernLocKeys.StationServing),
            StaffStation.StewPot => Loc.UI(TavernLocKeys.StationStewPot),
            _ => Loc.UI(TavernLocKeys.PrepStaffOff),
        };

        public static string RecipeName(RecipeDefinition recipe) => recipe != null ? Loc.Get(recipe.displayName) : string.Empty;

        /// <summary>The key/button currently bound to an action, e.g. "E" or "Space".</summary>
        public static string Binding(string map, string action)
        {
            var a = InputMaps.Find(map, action);
            return a != null ? a.GetBindingDisplayString() : "?";
        }

        public static string Clock(float seconds)
        {
            int s = UnityEngine.Mathf.CeilToInt(seconds);
            return $"{s / 60}:{s % 60:00}";
        }
    }
}
