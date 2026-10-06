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
    /// value, how many there are and how it's prepared), choosing up to three for tonight, Orik's job, and
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
        [SerializeField, Tooltip("Behind the nothing-cookable message.")] GameObject m_NothingBanner;
        [SerializeField, Tooltip("In the banner when the layout keeps the doors shut (4f): Decorate Mode, to fix it.")] Button m_Decorate;
        [SerializeField] Color m_CardColour = new(0.82f, 0.66f, 0.46f);
        [SerializeField, Tooltip("A chosen dish's tile, with gold corners as well.")] Color m_ChosenColour = new(0.98f, 0.86f, 0.5f);

        static readonly StaffStation[] k_Jobs = { StaffStation.Serving, StaffStation.Grill, StaffStation.Tap, StaffStation.StewPot, StaffStation.None };
        /// <summary>Gunta cooks (4f Checkpoint C): a station or off duty, never the plates.</summary>
        static readonly StaffStation[] k_CookJobs = { StaffStation.Grill, StaffStation.StewPot, StaffStation.Tap, StaffStation.None };

        [SerializeField, Tooltip("Gunta's job (4f Checkpoint C).")] Button m_Cook;
        [SerializeField] LocalizedSuperText m_CookLabel;
        [SerializeField, Tooltip("The Butcher Block (mise en place).")] Button m_Butcher;
        [SerializeField] ButcherPanel m_ButcherPanel;
        [SerializeField, Tooltip("More dishes than cards: the next page.")] Button m_Page;
        [SerializeField] LocalizedSuperText m_PageLabel;

        int m_PageIndex;
        bool m_WasCooking;
        readonly List<RecipeDefinition> m_All = new();

        public Button CookButton => m_Cook;
        public Button ButcherButton => m_Butcher;
        public ButcherPanel Butcher => m_ButcherPanel;
        public Button PageButton => m_Page;
        public int Page => m_PageIndex;
        public int PageCount => Mathf.Max(1, (m_All.Count + m_Cards.Length - 1) / Mathf.Max(1, m_Cards.Length));

        public void ConfigureKitchen(Button cook, LocalizedSuperText cookLabel, Button butcher, ButcherPanel butcherPanel, Button page, LocalizedSuperText pageLabel)
        {
            m_Cook = cook;
            m_CookLabel = cookLabel;
            m_Butcher = butcher;
            m_ButcherPanel = butcherPanel;
            m_Page = page;
            m_PageLabel = pageLabel;
        }

        TavernDirector m_Director;
        readonly List<RecipeDefinition> m_Recipes = new();

        public bool IsShown => m_Root != null && m_Root.activeSelf;
        public IReadOnlyList<DishCard> Cards => m_Cards;
        public Button OpenButton => m_Open;
        public Button CloseButton => m_Close;
        public Button StaffButton => m_Staff;
        public Button DecorateButton => m_Decorate;
        /// <summary>The recipe each card shows, in order.</summary>
        public IReadOnlyList<RecipeDefinition> Recipes => m_Recipes;

        public void Configure(GameObject root, SatchelSlotView[] stock, LocalizedSuperText stockEmpty, LocalizedSuperText tonight, DishCard[] cards,
            Button staff, LocalizedSuperText staffLabel, Button close, Button open, LocalizedSuperText nothing, GameObject fillHint, GameObject nothingBanner = null,
            Button decorate = null)
        {
            m_Decorate = decorate;
            m_NothingBanner = nothingBanner;
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
            for (int i = 0; i < m_Cards.Length; i++)
            {
                int index = i;
                m_Cards[i].button.onClick.AddListener(() => Toggle(index));
            }
            m_Staff.onClick.AddListener(NextJob);
            if (m_Cook != null) m_Cook.onClick.AddListener(NextCookJob);
            if (m_Page != null) m_Page.onClick.AddListener(NextPage);
            if (m_Butcher != null && m_ButcherPanel != null)
            {
                m_Butcher.onClick.AddListener(m_ButcherPanel.Open);
                m_ButcherPanel.Closed += () =>
                {
                    Refresh();
                    if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(m_Butcher.gameObject);
                };
            }
            m_Close.onClick.AddListener(() => m_Director.CloseForTheNight());
            m_Open.onClick.AddListener(() => m_Director.OpenService());
            if (m_FillHint != null) m_FillHint.SetActive(m_Director.CanDebugFill);
            if (m_Decorate != null) m_Decorate.onClick.AddListener(() => DecorateMode.Instance?.Enter());
            m_Director.PhaseChanged += Refresh;
            m_Director.PrepChanged += Refresh;
            if (DecorateMode.Instance != null) DecorateMode.Instance.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (DecorateMode.Instance != null) DecorateMode.Instance.Changed -= Refresh;
            if (m_Director == null) return;
            m_Director.PhaseChanged -= Refresh;
            m_Director.PrepChanged -= Refresh;
        }

        public void Toggle(int card)
        {
            if (card < m_Recipes.Count) m_Director.ToggleMenu(m_Recipes[card]);
        }

        /// <summary>Gunta's next job: the Grill, the Stew Pot, the Tap, or off duty.</summary>
        public void NextCookJob()
        {
            int at = Array.IndexOf(k_CookJobs, m_Director.CookAssignment);
            m_Director.AssignCook(k_CookJobs[(at + 1) % k_CookJobs.Length]);
        }

        /// <summary>The menu's next page of dishes (more dishes than cards since 4f Checkpoint C).</summary>
        public void NextPage()
        {
            m_PageIndex = (m_PageIndex + 1) % PageCount;
            Refresh();
        }

        // Breaking down a part at the block (or the list of parts) has the screen; Prep comes back after.
        void Update()
        {
            if (m_Director == null) return;
            bool busy = KeeperWork.Instance != null && KeeperWork.Instance.ActiveCook != null || m_ButcherPanel != null && m_ButcherPanel.IsOpen;
            if (busy == m_WasCooking) return;
            m_WasCooking = busy;
            Refresh();
        }

        public void NextJob()
        {
            int at = Array.IndexOf(k_Jobs, m_Director.StaffAssignment);
            m_Director.AssignStaff(k_Jobs[(at + 1) % k_Jobs.Length]);
        }

        void Refresh()
        {
            bool busy = KeeperWork.Instance != null && KeeperWork.Instance.ActiveCook != null || m_ButcherPanel != null && m_ButcherPanel.IsOpen;
            bool shown = m_Director.Phase == TavernPhase.Prep && !DecorateScreen.IsDecorating && !busy;
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

            // The dishes the storeroom can make first (and those already chosen), then the rest, a page at a time.
            m_All.Clear();
            m_All.AddRange(m_Director.Content.recipes.Where(r => r != null)
                .OrderByDescending(r => m_Director.Menu.Contains(r) || PrepRules.Makeable(r, storeroom) > 0));
            m_PageIndex = Mathf.Clamp(m_PageIndex, 0, PageCount - 1);
            m_Recipes.Clear();
            m_Recipes.AddRange(m_All.Skip(m_PageIndex * m_Cards.Length).Take(m_Cards.Length));
            if (m_Page != null) m_Page.gameObject.SetActive(PageCount > 1);
            m_PageLabel?.Set(TavernLocKeys.PrepPage, m_PageIndex + 1, PageCount);

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

            bool nothing = !PrepRules.AnythingCookable(m_All, storeroom);
            m_Tonight.gameObject.SetActive(!nothing);
            m_Tonight.Set(TavernLocKeys.PrepTonight, m_Director.Menu.Count, m_Director.MaxMenuSize);
            StaffDefinition pip = m_Director.StaffMember;
            m_Staff.gameObject.SetActive(pip != null);
            if (pip != null) m_StaffLabel.Set(TavernLocKeys.PrepStaffJob, Loc.Get(pip.displayName), Loc.UI(JobKey(m_Director.StaffAssignment)));
            StaffDefinition gunta = m_Director.CookMember;
            if (m_Cook != null) m_Cook.gameObject.SetActive(gunta != null);
            if (gunta != null) m_CookLabel?.Set(TavernLocKeys.PrepStaffJob, Loc.Get(gunta.displayName), Loc.UI(JobKey(m_Director.CookAssignment)));
            if (m_Butcher != null) m_Butcher.interactable = KeeperWork.Instance != null;
            m_Open.interactable = m_Director.CanOpen;
            // The banner says why the doors can't open: nothing to cook, or (4f, D13) the layout makes service impossible.
            Hearthdelve.Shared.Customization.LayoutReport layout = m_Director.Furnishing;
            bool layoutShut = !nothing && !layout.CanOpen;
            if (layoutShut) m_Nothing.Set(DecorateLocKeys.PrepCantOpen, DecorateScreen.IssueText(layout.Issues.First(i => i.Blocking)));
            else m_Nothing.Set(TavernLocKeys.PrepNothingCookable);
            m_Nothing.gameObject.SetActive(nothing || layoutShut);
            if (m_NothingBanner != null) m_NothingBanner.SetActive(nothing || layoutShut);
            if (m_Decorate != null) m_Decorate.gameObject.SetActive(layoutShut);

            if (!wasShown && EventSystem.current != null)
            {
                // Never start on a disabled control (A / Enter would do nothing): with nothing to cook, closing for
                // the night is the choice; otherwise the first dish that can be cooked.
                Button first = layoutShut && m_Decorate != null ? m_Decorate
                    : nothing ? m_Close : m_Cards.Select(c => c.button).FirstOrDefault(b => b.interactable && b.gameObject.activeInHierarchy) ?? m_Open;
                EventSystem.current.SetSelectedGameObject(first.gameObject);
            }
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
