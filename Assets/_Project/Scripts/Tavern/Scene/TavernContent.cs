using System.Collections.Generic;
using Hearthdelve.Shared.Economy;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>Everything the tavern scene needs, in one asset (recipes, customers, staff, tuning).</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Tavern Content", fileName = "TavernContent")]
    public sealed class TavernContent : ScriptableObject
    {
        public List<RecipeDefinition> recipes = new();
        public List<CustomerProfile> customers = new();
        public List<StaffDefinition> staff = new();
        [Min(1), Tooltip("Seats before any seat upgrades (the room holds more; extra stools appear as seats are bought).")]
        public int baseSeats = 6;
        [Tooltip("What the debug 'fill storeroom' action stocks (Phase 1 ingredients).")]
        public List<IngredientDefinition> debugStockIngredients = new();

        [Header("Tuning")]
        public ServiceConfig service;
        public EconomyConfig economy;
        public GrillConfig grill;
        public TapConfig tap;
        public ServingConfig serving;
        public StewConfig stew;
    }
}
