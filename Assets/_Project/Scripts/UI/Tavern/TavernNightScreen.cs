using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Progression;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>
    /// Night (day loop): gold and renown, the upgrade shop, the debug end-of-day summary (F10),
    /// and Sleep. The game autosaves when Night begins and after each purchase.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class TavernNightScreen : MonoBehaviour
    {
        VisualElement m_Screen, m_Upgrades, m_Summary, m_SummaryRows;
        Label m_Title, m_Purse, m_UpgradesHeader, m_SummaryHeader, m_Saved;
        Button m_Sleep;
        TavernDirector m_Director;
        bool m_ShowingSummary;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_Screen = root.Q("screen");
            m_Upgrades = root.Q("upgrade-list");
            m_Summary = root.Q("summary");
            m_SummaryRows = root.Q("summary-rows");
            m_Title = root.Q<Label>("title");
            m_Purse = root.Q<Label>("purse");
            m_UpgradesHeader = root.Q<Label>("upgrades-header");
            m_SummaryHeader = root.Q<Label>("summary-header");
            m_Saved = root.Q<Label>("saved");
            m_Sleep = root.Q<Button>("sleep");
            m_Sleep.clicked += () => m_Director?.Sleep();
            m_Screen.style.display = DisplayStyle.None;
        }

        void Start()
        {
            m_Director = TavernDirector.Instance;
            if (m_Director == null) return;
            m_Director.PhaseChanged += Refresh;
            if (m_Director.Flow != null) m_Director.Flow.StateChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (m_Director == null) return;
            m_Director.PhaseChanged -= Refresh;
            if (m_Director.Flow != null) m_Director.Flow.StateChanged -= Refresh;
        }

        void Update()
        {
            var flow = m_Director != null ? m_Director.Flow : null;
            if (flow == null || flow.ShowSummary == m_ShowingSummary) return;
            Refresh();
        }

        void Refresh()
        {
            var flow = m_Director.Flow;
            bool visible = m_Director.Phase == TavernPhase.Night && flow != null && flow.InGame;
            m_Screen.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;
            var state = flow.State;

            m_Title.text = Loc.UI(LoopLocKeys.NightTitle, state.Day);
            m_Purse.text = Loc.UI(LoopLocKeys.NightPurse, state.Gold, state.Renown);
            m_UpgradesHeader.text = Loc.UI(LoopLocKeys.NightUpgrades);
            m_Sleep.text = Loc.UI(LoopLocKeys.NightSleep);
            m_Saved.text = flow.LastWarnings.Count == 0 ? Loc.UI(LoopLocKeys.NightSaved) : string.Join("\n", flow.LastWarnings);

            m_Upgrades.Clear();
            int cheapest = -1;
            foreach (var upgrade in flow.Database.upgrades)
            {
                if (upgrade == null) continue;
                var u = upgrade;
                int level = state.UpgradeLevel(u.id);
                int cost = Upgrades.NextCost(u, level);
                if (cost >= 0 && (cheapest < 0 || cost < cheapest)) cheapest = cost;

                var row = new VisualElement();
                row.AddToClassList("hd-recipe");
                row.Add(TavernUI.Row(Loc.UI(LoopLocKeys.NightUpgradeLevel, Loc.Get(u.displayName), level, u.MaxLevel), "hd-recipe__name"));
                if (cost < 0)
                {
                    row.Add(TavernUI.Row(Loc.UI(LoopLocKeys.NightMaxed), "hd-recipe__details"));
                }
                else
                {
                    var buy = new Button(() => flow.BuyUpgrade(u))
                    {
                        text = Loc.UI(LoopLocKeys.NightBuy, TavernUI.UpgradeEffect(u.kind, u.levels[level].amount), cost),
                    };
                    buy.AddToClassList("hd-button");
                    buy.AddToClassList("hd-button--small");
                    buy.SetEnabled(state.Gold >= cost);
                    row.Add(buy);
                }
                m_Upgrades.Add(row);
            }

            m_ShowingSummary = flow.ShowSummary;
            m_Summary.style.display = m_ShowingSummary ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_ShowingSummary) FillSummary(state.Today, cheapest);
            m_Screen.schedule.Execute(() => m_Sleep.Focus());
        }

        void FillSummary(DaySummary today, int cheapestUpgrade)
        {
            m_SummaryHeader.text = Loc.UI(LoopLocKeys.SummaryTitle);
            m_SummaryRows.Clear();
            string delve = Loc.UI(today.Delve switch
            {
                DelveOutcome.Extracted => LoopLocKeys.SummaryExtracted,
                DelveOutcome.Died => LoopLocKeys.SummaryDied,
                _ => LoopLocKeys.SummarySkipped,
            });
            m_SummaryRows.Add(new Label(Loc.UI(LoopLocKeys.SummaryDelve, delve)));
            m_SummaryRows.Add(new Label(Loc.UI(LoopLocKeys.SummaryParts, today.PartsBroughtBack, today.PartsLost)));
            m_SummaryRows.Add(new Label(Loc.UI(LoopLocKeys.SummaryDishes, today.DishesServed, today.Walkouts)));
            m_SummaryRows.Add(new Label(Loc.UI(LoopLocKeys.SummaryEarned, today.Gold, today.Tips, today.Earned)));
            m_SummaryRows.Add(new Label(cheapestUpgrade >= 0 ? Loc.UI(LoopLocKeys.SummaryNextUpgrade, cheapestUpgrade) : Loc.UI(LoopLocKeys.SummaryAllBought)));
        }
    }
}
