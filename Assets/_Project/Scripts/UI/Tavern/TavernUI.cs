using System.Linq;
using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Progression;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

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

        /// <summary>"+20 max Essence" / "Essence drains 20% slower".</summary>
        public static string Buff(MealBuffKind kind, float amount) => kind switch
        {
            MealBuffKind.MaxEssence => Loc.UI(LoopLocKeys.BuffMaxEssence, Mathf.RoundToInt(amount)),
            MealBuffKind.SlowerDrain => Loc.UI(LoopLocKeys.BuffSlowerDrain, Mathf.RoundToInt(amount * 100f)),
            _ => string.Empty,
        };

        /// <summary>"+1 satchel slot" / "+20 max Essence" / "+1 seat".</summary>
        public static string UpgradeEffect(UpgradeKind kind, float amount) => Loc.UI(kind switch
        {
            UpgradeKind.SatchelSlots => LoopLocKeys.UpgradeSatchel,
            UpgradeKind.MaxEssence => LoopLocKeys.UpgradeEssence,
            _ => LoopLocKeys.UpgradeSeats,
        }, Mathf.RoundToInt(amount));

        /// <summary>Fills <paramref name="list"/> with one row per storeroom stack (name, count, freshness), quality-coloured.</summary>
        public static void StockRows(VisualElement list, Storeroom storeroom)
        {
            list.Clear();
            var stacks = storeroom.Stacks.OrderBy(s => s.Item.Definition.id).ThenByDescending(s => s.Item.Quality).ToList();
            if (stacks.Count == 0) list.Add(Row(Loc.UI(TavernLocKeys.PrepStoreroomEmpty), "hd-prep__empty"));
            foreach (var s in stacks)
                list.Add(Row(Loc.UI(TavernLocKeys.PrepStockRow, Loc.ItemName(s.Item), s.Count, Mathf.RoundToInt(s.Freshness * 100f)),
                    $"hd-quality--{s.Item.Quality.ToString().ToLowerInvariant()}"));
        }

        public static Label Row(string text, string cls)
        {
            var l = new Label(text);
            l.AddToClassList(cls);
            return l;
        }

        public static string Clock(float seconds)
        {
            int s = UnityEngine.Mathf.CeilToInt(seconds);
            return $"{s / 60}:{s % 60:00}";
        }
    }
}
