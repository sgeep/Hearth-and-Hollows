using System.Linq;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>Before service: see the storeroom, pick up to 3 dishes, assign the staff helper, open.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class TavernPrepScreen : MonoBehaviour
    {
        VisualElement m_Screen, m_Stock, m_Recipes, m_StaffButtons;
        Label m_Title, m_StockHeader, m_MenuHeader, m_StaffLabel;
        Button m_Fill, m_Open, m_Close;
        TavernDirector m_Director;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_Screen = root.Q("screen");
            m_Title = root.Q<Label>("title");
            m_StockHeader = root.Q<Label>("stock-header");
            m_Stock = root.Q("stock-list");
            m_MenuHeader = root.Q<Label>("menu-header");
            m_Recipes = root.Q("recipe-list");
            m_StaffLabel = root.Q<Label>("staff-label");
            m_StaffButtons = root.Q("staff-buttons");
            m_Fill = root.Q<Button>("fill");
            m_Open = root.Q<Button>("open");
            m_Close = root.Q<Button>("close");
            m_Fill.clicked += () => m_Director?.FillStoreroom();
            m_Open.clicked += () => m_Director?.OpenService();
            m_Close.clicked += () => m_Director?.CloseForTheNight();
        }

        void Start()
        {
            m_Director = TavernDirector.Instance;
            if (m_Director == null) return;
            m_Director.PrepChanged += Refresh;
            m_Director.PhaseChanged += Refresh;
            Refresh();
            m_Screen.schedule.Execute(() => (m_Recipes.childCount > 0 ? m_Recipes[0] : m_Open).Focus());
        }

        void OnDestroy()
        {
            if (m_Director == null) return;
            m_Director.PrepChanged -= Refresh;
            m_Director.PhaseChanged -= Refresh;
        }

        void Refresh()
        {
            bool visible = m_Director.Phase == TavernPhase.Prep;
            m_Screen.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;

            m_Title.text = Loc.UI(TavernLocKeys.PrepTitle);
            m_StockHeader.text = Loc.UI(TavernLocKeys.PrepStoreroom);
            m_Fill.text = Loc.UI(TavernLocKeys.PrepFill);
            m_Open.text = Loc.UI(TavernLocKeys.PrepOpen);
            m_Open.SetEnabled(m_Director.CanOpen);
            // The debug fill: development builds only, and in the day loop only when enabled.
            m_Fill.style.display = Debug.isDebugBuild && m_Director.CanDebugFill ? DisplayStyle.Flex : DisplayStyle.None;
            m_Close.text = Loc.UI(LoopLocKeys.PrepClose);
            m_Close.style.display = m_Director.Flow != null ? DisplayStyle.Flex : DisplayStyle.None;

            TavernUI.StockRows(m_Stock, m_Director.Storeroom);

            m_MenuHeader.text = Loc.UI(TavernLocKeys.PrepMenu, m_Director.MaxMenuSize);
            m_Recipes.Clear();
            foreach (var recipe in m_Director.Content.recipes)
            {
                var r = recipe;
                int servings = m_Director.ServingsAvailable(r);
                var button = new Button(() => m_Director.ToggleMenu(r));
                button.AddToClassList("hd-recipe");
                button.EnableInClassList(Screens.SlotView.SelectedClass, m_Director.SelectedMenu.Contains(r));
                var swatch = new VisualElement();
                swatch.AddToClassList("hd-recipe__swatch");
                swatch.style.backgroundColor = r.placeholderColor;
                button.Add(swatch);
                button.Add(Row(TavernUI.RecipeName(r), "hd-recipe__name"));
                var pot = m_Director.Content.stew != null ? m_Director.Content.stew.pot : StewPotSettings.Default;
                string details = r.station == CookStation.StewPot
                    ? Loc.UI(TavernLocKeys.PrepRecipeDetailsStew, TavernUI.Station(r.station), r.baseValue, servings, pot.minHelpings, pot.maxHelpings)
                    : Loc.UI(TavernLocKeys.PrepRecipeDetails, TavernUI.Station(r.station), r.baseValue, servings);
                button.Add(Row(details, "hd-recipe__details"));
                button.SetEnabled(servings > 0 || m_Director.SelectedMenu.Contains(r));
                m_Recipes.Add(button);
            }

            var staff = m_Director.StaffMember;
            m_StaffLabel.text = Loc.UI(TavernLocKeys.PrepStaff, staff != null ? Loc.Get(staff.displayName) : "-");
            m_StaffButtons.Clear();
            foreach (StaffStation station in System.Enum.GetValues(typeof(StaffStation)))
            {
                var st = station;
                var b = new Button(() => m_Director.AssignStaff(st)) { text = TavernUI.Station(st) };
                b.AddToClassList("hd-button");
                b.AddToClassList("hd-button--small");
                b.EnableInClassList(Screens.SlotView.SelectedClass, m_Director.StaffAssignment == st);
                b.SetEnabled(staff != null);
                m_StaffButtons.Add(b);
            }
        }

        static Label Row(string text, string cls) => TavernUI.Row(text, cls);
    }
}
