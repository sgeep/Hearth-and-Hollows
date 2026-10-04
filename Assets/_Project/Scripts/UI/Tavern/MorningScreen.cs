using System;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>
    /// Morning (GDD §3.1, day loop): what's in the storeroom, an optional breakfast (one Grill or Tap dish, cooked
    /// at its station's panel; how well it comes out scales its buff), what today's delve starts with from upgrades
    /// and breakfast, and setting off. Out of the way while breakfast cooks.
    /// </summary>
    public sealed class MorningScreen : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] LocalizedSuperText m_Title;
        [SerializeField] SatchelSlotView[] m_Stock = Array.Empty<SatchelSlotView>();
        [SerializeField] LocalizedSuperText m_StockEmpty;
        [SerializeField] DishCard[] m_Cards = Array.Empty<DishCard>();
        [SerializeField] LocalizedSuperText m_Breakfast;
        [SerializeField] LocalizedSuperText m_Bonuses;
        [SerializeField] Button m_Descend;
        [SerializeField] Color m_CardColour = new(0.82f, 0.66f, 0.46f);
        [SerializeField, Tooltip("The dish eaten this morning.")] Color m_EatenColour = new(0.98f, 0.86f, 0.5f);

        TavernDirector m_Director;
        readonly List<RecipeDefinition> m_Options = new();
        bool m_Dirty = true;
        bool m_WasShown;

        public bool IsShown => m_Root != null && m_Root.activeSelf;
        public IReadOnlyList<DishCard> Cards => m_Cards;
        public IReadOnlyList<RecipeDefinition> Options => m_Options;
        public Button DescendButton => m_Descend;

        public void Configure(GameObject root, LocalizedSuperText title, SatchelSlotView[] stock, LocalizedSuperText stockEmpty, DishCard[] cards,
            LocalizedSuperText breakfast, LocalizedSuperText bonuses, Button descend)
        {
            m_Root = root;
            m_Title = title;
            m_Stock = stock;
            m_StockEmpty = stockEmpty;
            m_Cards = cards;
            m_Breakfast = breakfast;
            m_Bonuses = bonuses;
            m_Descend = descend;
        }

        void Start()
        {
            m_Director = TavernDirector.Instance;
            m_Root.SetActive(false);
            if (m_Director == null) return;
            m_Options.AddRange(m_Director.BreakfastOptions().Take(m_Cards.Length));
            for (int i = 0; i < m_Cards.Length; i++)
            {
                int index = i;
                m_Cards[i].button.onClick.AddListener(() => Cook(index));
            }
            m_Descend.onClick.AddListener(() => m_Director.Descend());
            m_Director.PhaseChanged += MarkDirty;
            m_Director.PrepChanged += MarkDirty;
        }

        void OnDestroy()
        {
            if (m_Director == null) return;
            m_Director.PhaseChanged -= MarkDirty;
            m_Director.PrepChanged -= MarkDirty;
        }

        void MarkDirty() => m_Dirty = true;

        public void Cook(int card)
        {
            if (card >= m_Options.Count) return;
            RecipeDefinition recipe = m_Options[card];
            m_Director.CookBreakfast(recipe);
        }

        void LateUpdate()
        {
            if (m_Director == null) return;
            // Hidden while breakfast cooks: the station panel has the screen.
            bool shown = m_Director.Phase == TavernPhase.Morning && (KeeperWork.Instance == null || KeeperWork.Instance.ActiveCook == null);
            if (shown != m_Root.activeSelf) m_Root.SetActive(shown);
            if (!shown)
            {
                m_WasShown = false;
                return;
            }
            if (m_Dirty || !m_WasShown) Refresh();
            if (!m_WasShown && EventSystem.current != null)
            {
                // First thing in the morning: the first breakfast you can cook; afterwards (or with none), setting off.
                DishCard first = m_Cards.FirstOrDefault(c => c.button.gameObject.activeSelf && c.button.interactable);
                EventSystem.current.SetSelectedGameObject(first != null && !m_Director.HasEatenBreakfast ? first.button.gameObject : m_Descend.gameObject);
            }
            m_WasShown = true;
        }

        void Refresh()
        {
            m_Dirty = false;
            GameFlow flow = m_Director.Flow;
            m_Title.Set(LoopLocKeys.MorningTitle, flow != null ? flow.State.Day : 1);

            Storeroom storeroom = m_Director.Storeroom;
            List<IngredientStack> stacks = storeroom.Stacks.Where(s => !s.IsEmpty).OrderBy(s => s.Item.Definition.id).ThenByDescending(s => s.Item.Quality).ToList();
            for (int i = 0; i < m_Stock.Length; i++)
            {
                bool has = i < stacks.Count;
                m_Stock[i].gameObject.SetActive(has);
                if (has) m_Stock[i].Show(stacks[i]);
            }
            m_StockEmpty.gameObject.SetActive(stacks.Count == 0);

            MealBuff meal = flow != null ? flow.State.Meal : MealBuff.None;
            for (int i = 0; i < m_Cards.Length; i++)
            {
                DishCard card = m_Cards[i];
                bool exists = i < m_Options.Count;
                card.button.gameObject.SetActive(exists);
                if (!exists) continue;
                RecipeDefinition recipe = m_Options[i];
                bool eaten = meal.IsActive && meal.RecipeId == recipe.id;
                bool can = m_Director.CanCookBreakfast(recipe);
                card.icon.sprite = recipe.icon;
                card.icon.enabled = recipe.icon != null;
                card.name.Set(TavernLocKeys.Plain, Loc.Get(recipe.displayName));
                card.detail.Set(TavernLocKeys.Plain, Buff(recipe.mealBuff.kind, recipe.mealBuff.amount));
                card.steps.Set(LoopLocKeys.MorningCookAt, Loc.UI(recipe.station == CookStation.Tap ? TavernLocKeys.StationTap : TavernLocKeys.StationGrill));
                card.selected.SetActive(eaten);
                card.back.color = eaten ? m_EatenColour : m_CardColour;
                card.button.interactable = can;
                card.group.alpha = can || eaten ? 1f : 0.45f;
            }

            if (meal.IsActive)
            {
                RecipeDefinition dish = m_Director.Content.recipes.FirstOrDefault(r => r != null && r.id == meal.RecipeId);
                m_Breakfast.Set(LoopLocKeys.MorningAte, dish != null ? Loc.Get(dish.displayName) : meal.RecipeId, Buff(meal.Kind, meal.Amount));
            }
            else if (!m_Options.Any(m_Director.CanCookBreakfast)) m_Breakfast.Set(LoopLocKeys.MorningNoBreakfast);
            else m_Breakfast.Set(LoopLocKeys.MorningBreakfast);

            m_Bonuses.Set(TavernLocKeys.Plain, Bonuses(flow != null ? flow.Loadout : DelveLoadout.None));
        }

        /// <summary>"+15 max Essence", "Essence drains 20% slower".</summary>
        public static string Buff(MealBuffKind kind, float amount) => kind switch
        {
            MealBuffKind.MaxEssence => Loc.UI(LoopLocKeys.BuffMaxEssence, Mathf.RoundToInt(amount)),
            MealBuffKind.SlowerDrain => Loc.UI(LoopLocKeys.BuffSlowerDrain, Mathf.RoundToInt(amount * 100f)),
            _ => string.Empty,
        };

        /// <summary>"Today's delve: satchel +1 · Essence +32 · drain -20%", only what applies.</summary>
        public static string Bonuses(DelveLoadout loadout)
        {
            var parts = new List<string>();
            if (loadout.ExtraSatchelSlots > 0) parts.Add(Loc.UI(LoopLocKeys.BonusSatchel, loadout.ExtraSatchelSlots));
            if (loadout.MaxEssenceBonus >= 0.5f) parts.Add(Loc.UI(LoopLocKeys.BonusEssence, Mathf.RoundToInt(loadout.MaxEssenceBonus)));
            int slower = Mathf.RoundToInt((1f - loadout.DrainMultiplier) * 100f);
            if (slower > 0) parts.Add(Loc.UI(LoopLocKeys.BonusDrain, slower));
            if (parts.Count == 0) return Loc.UI(LoopLocKeys.MorningNoBonuses);
            string joined = parts[0];
            for (int i = 1; i < parts.Count; i++) joined = Loc.UI(LoopLocKeys.BonusJoin, joined, parts[i]);
            return Loc.UI(LoopLocKeys.MorningBonuses, joined);
        }
    }
}
