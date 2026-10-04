using System;
using System.Collections.Generic;
using System.Linq;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>One dish on the prep screen: tap it to put it on tonight's menu or take it off.</summary>
    [Serializable]
    public sealed class DishCard
    {
        public Button button;
        public CanvasGroup group;
        public Image icon;
        public LocalizedSuperText name;
        public LocalizedSuperText detail;
        public LocalizedSuperText amount;
        public LocalizedSuperText steps;
        public GameObject selected;
        public Image back;
    }

    /// <summary>
    /// Evening Prep (GDD §3.1): what's in the storeroom, the dishes it can make (each as a card with its
    /// value, how many there are and how it's prepared), choosing up to three for tonight, Pip's job, and
    /// opening the doors or closing for the night. Shown over the room; the room stays in view around it.
    /// </summary>
    public sealed class PrepScreen : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] SatchelSlotView[] m_Stock = Array.Empty<SatchelSlotView>();
        [SerializeField] LocalizedSuperText m_StockEmpty;
        [SerializeField] LocalizedSuperText m_Tonight;
        [SerializeField] DishCard[] m_Cards = Array.Empty<DishCard>();
        [SerializeField] Button m_Staff;
        [SerializeField] LocalizedSuperText m_StaffLabel;
        [SerializeField] Button m_Close;
        [SerializeField] Button m_Open;
        [SerializeField] LocalizedSuperText m_Nothing;
        [SerializeField] GameObject m_FillHint;
        [SerializeField] Color m_CardColour = new(0.82f, 0.66f, 0.46f);
        [SerializeField, Tooltip("A chosen dish's tile, with gold corners as well.")] Color m_ChosenColour = new(0.98f, 0.86f, 0.5f);

        static readonly StaffStation[] k_Jobs = { StaffStation.Serving, StaffStation.Grill, StaffStation.Tap, StaffStation.StewPot, StaffStation.None };

        TavernDirector m_Director;
        readonly List<RecipeDefinition> m_Recipes = new();

        public bool IsShown => m_Root != null && m_Root.activeSelf;
        public IReadOnlyList<DishCard> Cards => m_Cards;
        public Button OpenButton => m_Open;
        public Button CloseButton => m_Close;
        public Button StaffButton => m_Staff;
        /// <summary>The recipe each card shows, in order.</summary>
        public IReadOnlyList<RecipeDefinition> Recipes => m_Recipes;

        public void Configure(GameObject root, SatchelSlotView[] stock, LocalizedSuperText stockEmpty, LocalizedSuperText tonight, DishCard[] cards,
            Button staff, LocalizedSuperText staffLabel, Button close, Button open, LocalizedSuperText nothing, GameObject fillHint)
        {
            m_Root = root;
            m_Stock = stock;
            m_StockEmpty = stockEmpty;
            m_Tonight = tonight;
            m_Cards = cards;
            m_Staff = staff;
            m_StaffLabel = staffLabel;
            m_Close = close;
            m_Open = open;
            m_Nothing = nothing;
            m_FillHint = fillHint;
        }

        void Start()
        {
            m_Director = TavernDirector.Instance;
            if (m_Director == null) return;
            m_Recipes.AddRange(m_Director.Content.recipes.Where(r => r != null).Take(m_Cards.Length));
            for (int i = 0; i < m_Cards.Length; i++)
            {
                int index = i;
                m_Cards[i].button.onClick.AddListener(() => Toggle(index));
            }
            m_Staff.onClick.AddListener(NextJob);
            m_Close.onClick.AddListener(() => m_Director.CloseForTheNight());
            m_Open.onClick.AddListener(() => m_Director.OpenService());
            if (m_FillHint != null) m_FillHint.SetActive(m_Director.CanDebugFill);
            m_Director.PhaseChanged += Refresh;
            m_Director.PrepChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (m_Director == null) return;
            m_Director.PhaseChanged -= Refresh;
            m_Director.PrepChanged -= Refresh;
        }

        public void Toggle(int card)
        {
            if (card < m_Recipes.Count) m_Director.ToggleMenu(m_Recipes[card]);
        }

        public void NextJob()
        {
            int at = Array.IndexOf(k_Jobs, m_Director.StaffAssignment);
            m_Director.AssignStaff(k_Jobs[(at + 1) % k_Jobs.Length]);
        }

        void Refresh()
        {
            bool shown = m_Director.Phase == TavernPhase.Prep;
            bool wasShown = m_Root.activeSelf;
            m_Root.SetActive(shown);
            if (!shown) return;

            Storeroom storeroom = m_Director.Storeroom;
            List<IngredientStack> stacks = storeroom.Stacks.Where(s => !s.IsEmpty).OrderBy(s => s.Item.Definition.id).ThenByDescending(s => s.Item.Quality).ToList();
            for (int i = 0; i < m_Stock.Length; i++)
            {
                bool has = i < stacks.Count;
                m_Stock[i].gameObject.SetActive(has);
                if (has) m_Stock[i].Show(stacks[i]);
            }
            m_StockEmpty.gameObject.SetActive(stacks.Count == 0);

            for (int i = 0; i < m_Cards.Length; i++)
            {
                DishCard card = m_Cards[i];
                bool exists = i < m_Recipes.Count;
                card.button.gameObject.SetActive(exists);
                if (!exists) continue;
                RecipeDefinition recipe = m_Recipes[i];
                bool on = m_Director.Menu.Contains(recipe);
                int makeable = PrepRules.Makeable(recipe, storeroom);
                card.icon.sprite = recipe.icon;
                card.icon.enabled = recipe.icon != null;
                card.name.Set(TavernLocKeys.Plain, Loc.Get(recipe.displayName));
                card.detail.Set(recipe.station == CookStation.StewPot ? TavernLocKeys.PrepValueStew : TavernLocKeys.PrepValue, recipe.baseValue);
                card.amount.Set(TavernLocKeys.Plain, Amount(recipe, makeable));
                card.steps.Set(TavernLocKeys.Plain, Steps(recipe));
                card.selected.SetActive(on);
                card.back.color = on ? m_ChosenColour : m_CardColour;
                // A dish the storeroom can't make can't be chosen (one already on the menu can still come off).
                card.button.interactable = on || makeable > 0;
                card.group.alpha = on || makeable > 0 ? 1f : 0.45f;
            }

            bool nothing = !PrepRules.AnythingCookable(m_Recipes, storeroom);
            m_Tonight.gameObject.SetActive(!nothing);
            m_Tonight.Set(TavernLocKeys.PrepTonight, m_Director.Menu.Count, m_Director.MaxMenuSize);
            StaffDefinition pip = m_Director.StaffMember;
            m_Staff.gameObject.SetActive(pip != null);
            if (pip != null) m_StaffLabel.Set(TavernLocKeys.PrepStaffJob, Loc.Get(pip.displayName), Loc.UI(JobKey(m_Director.StaffAssignment)));
            m_Open.interactable = m_Director.CanOpen;
            m_Nothing.gameObject.SetActive(nothing);

            if (!wasShown && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(m_Cards.Length > 0 ? m_Cards[0].button.gameObject : m_Open.gameObject);
        }

        /// <summary>"12 to serve", "2 pots", "0 to serve": the count even when it's none (the card dims as well).</summary>
        static string Amount(RecipeDefinition recipe, int makeable)
        {
            if (recipe.station != CookStation.StewPot) return Loc.UI(TavernLocKeys.PrepServings, makeable);
            return makeable == 1 ? Loc.UI(TavernLocKeys.PrepPot) : Loc.UI(TavernLocKeys.PrepPots, makeable);
        }

        /// <summary>"grill", or "chop, simmer": however many steps the dish has, in order.</summary>
        public static string Steps(RecipeDefinition recipe)
        {
            IReadOnlyList<PrepStep> steps = PrepRules.Steps(recipe);
            string text = steps.Count > 0 ? StepName(steps[0]) : string.Empty;
            for (int i = 1; i < steps.Count; i++) text = Loc.UI(TavernLocKeys.PrepThen, text, StepName(steps[i]));
            return text;
        }

        static string StepName(PrepStep step) => Loc.UI(step switch
        {
            PrepStep.Tap => TavernLocKeys.StationTap,
            PrepStep.Chop => TavernLocKeys.StepChop,
            PrepStep.Simmer => TavernLocKeys.StepSimmer,
            _ => TavernLocKeys.StationGrill,
        });

        static string JobKey(StaffStation job) => job switch
        {
            StaffStation.Grill => TavernLocKeys.StationGrill,
            StaffStation.Tap => TavernLocKeys.StationTap,
            StaffStation.StewPot => TavernLocKeys.StationStewPot,
            StaffStation.Serving => TavernLocKeys.StationServing,
            _ => TavernLocKeys.PrepStaffOff,
        };
    }
}
