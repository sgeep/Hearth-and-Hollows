using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>
    /// Morning Prep (day loop): the storeroom, one breakfast (cooked through its station's
    /// minigame, for a buff on today's delve), today's delve bonuses, and Descend.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class TavernMorningScreen : MonoBehaviour
    {
        VisualElement m_Screen, m_Stock, m_Breakfast;
        Label m_Title, m_StockHeader, m_BreakfastHeader, m_Status, m_Loadout;
        Button m_Descend;
        TavernDirector m_Director;
        bool m_WasCooking;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_Screen = root.Q("screen");
            m_Stock = root.Q("stock-list");
            m_Breakfast = root.Q("breakfast-list");
            m_Title = root.Q<Label>("title");
            m_StockHeader = root.Q<Label>("stock-header");
            m_BreakfastHeader = root.Q<Label>("breakfast-header");
            m_Status = root.Q<Label>("status");
            m_Loadout = root.Q<Label>("loadout");
            m_Descend = root.Q<Button>("descend");
            m_Descend.clicked += () => m_Director?.Descend();
            m_Screen.style.display = DisplayStyle.None;
        }

        void Start()
        {
            m_Director = TavernDirector.Instance;
            if (m_Director == null) return;
            m_Director.PhaseChanged += Refresh;
            m_Director.PrepChanged += Refresh;
            if (m_Director.Flow != null) m_Director.Flow.StateChanged += Refresh;
            Refresh();
            m_Screen.schedule.Execute(() => m_Descend.Focus());
        }

        void OnDestroy()
        {
            if (m_Director == null) return;
            m_Director.PhaseChanged -= Refresh;
            m_Director.PrepChanged -= Refresh;
            if (m_Director.Flow != null) m_Director.Flow.StateChanged -= Refresh;
        }

        void Update()
        {
            // Step aside while breakfast is on the stove so the minigame panel shows.
            bool cooking = m_Director != null && m_Director.Phase == TavernPhase.Morning && m_Director.Player.ActiveCook != null;
            if (cooking == m_WasCooking) return;
            m_WasCooking = cooking;
            Refresh();
        }

        void Refresh()
        {
            bool visible = m_Director.Phase == TavernPhase.Morning && m_Director.Flow != null && !m_WasCooking;
            m_Screen.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;
            var flow = m_Director.Flow;

            m_Title.text = Loc.UI(LoopLocKeys.MorningTitle, m_Director.Day);
            m_StockHeader.text = Loc.UI(LoopLocKeys.MorningStoreroom);
            m_BreakfastHeader.text = Loc.UI(LoopLocKeys.MorningBreakfast);
            m_Descend.text = Loc.UI(LoopLocKeys.MorningDescend);
            TavernUI.StockRows(m_Stock, m_Director.Storeroom);

            m_Breakfast.Clear();
            int offered = 0;
            foreach (var recipe in m_Director.BreakfastOptions())
            {
                var r = recipe;
                int servings = m_Director.ServingsAvailable(r);
                if (servings == 0) continue;
                offered++;
                var button = new Button(() => m_Director.CookBreakfast(r));
                button.AddToClassList("hd-recipe");
                var swatch = new VisualElement();
                swatch.AddToClassList("hd-recipe__swatch");
                swatch.style.backgroundColor = r.placeholderColor;
                button.Add(swatch);
                button.Add(TavernUI.Row(TavernUI.RecipeName(r), "hd-recipe__name"));
                button.Add(TavernUI.Row(Loc.UI(LoopLocKeys.MorningBreakfastRow, TavernUI.Station(r.station), TavernUI.Buff(r.mealBuff.kind, r.mealBuff.amount)), "hd-recipe__details"));
                button.SetEnabled(m_Director.CanCookBreakfast(r));
                m_Breakfast.Add(button);
            }
            if (offered == 0) m_Breakfast.Add(TavernUI.Row(Loc.UI(LoopLocKeys.MorningNoBreakfast), "hd-prep__empty"));

            var meal = flow.State.Meal;
            m_Status.text = meal.IsActive
                ? Loc.UI(LoopLocKeys.MorningAte, RecipeName(meal.RecipeId), TavernUI.Buff(meal.Kind, meal.Amount))
                : string.Empty;

            var loadout = flow.Loadout;
            m_Loadout.text = Loc.UI(LoopLocKeys.MorningLoadout, loadout.ExtraSatchelSlots, Mathf.RoundToInt(loadout.MaxEssenceBonus),
                Mathf.RoundToInt(loadout.DrainMultiplier * 100f));
        }

        string RecipeName(string id)
        {
            foreach (var r in m_Director.Content.recipes)
                if (r != null && r.id == id) return TavernUI.RecipeName(r);
            return id;
        }
    }
}
