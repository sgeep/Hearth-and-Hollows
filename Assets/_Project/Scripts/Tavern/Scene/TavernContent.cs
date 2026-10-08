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
    /// <summary>
    /// A named villager who may come to dinner (4h Checkpoint D): their stable id, how often (each evening, seeded), the customer
    /// profile they order as, and the look they already have in Kariaston. Service treats them as any other customer.
    /// </summary>
    [System.Serializable]
    public sealed class NamedPatron
    {
        public string character;
        [Range(0f, 1f)] public float chance = 0.2f;
        public CustomerProfile profile;
        public Hearthdelve.Shared.Animation.SpriteAnimationSet[] layers;
        public Hearthdelve.Shared.Animation.SpriteAnimationSet shadow;
    }

    /// <summary>Everything the tavern scene needs, in one asset (recipes, customers, staff, tuning).</summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Tavern Content", fileName = "TavernContent")]
    public sealed class TavernContent : ScriptableObject
    {
        public List<RecipeDefinition> recipes = new();
        public List<CustomerProfile> customers = new();
        public List<StaffDefinition> staff = new();
        [Tooltip("4h Checkpoint D: familiar faces at dinner, 0–2 an evening.")]
        public List<NamedPatron> namedPatrons = new();
        [Tooltip("What the debug 'fill storeroom' action stocks (Phase 1 ingredients).")]
        public List<IngredientDefinition> debugStockIngredients = new();

        [Header("Tuning")]
        public ServiceConfig service;
        public EconomyConfig economy;
        public GrillConfig grill;
        public TapConfig tap;
        public ServingConfig serving;
        public StewConfig stew;
        [Tooltip("The Butcher Block (4f Checkpoint C).")]
        public ButcherConfig butcher;
    }
}
