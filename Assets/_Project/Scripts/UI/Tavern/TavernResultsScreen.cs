using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>End of service: dishes served, gold, tips, renown, walkouts.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class TavernResultsScreen : MonoBehaviour
    {
        VisualElement m_Screen, m_Rows;
        Label m_Title;
        Button m_Again;
        TavernDirector m_Director;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_Screen = root.Q("screen");
            m_Rows = root.Q("rows");
            m_Title = root.Q<Label>("title");
            m_Again = root.Q<Button>("again");
            m_Again.clicked += () => m_Director?.FinishEvening();
            m_Screen.style.display = DisplayStyle.None;
        }

        void Start()
        {
            m_Director = TavernDirector.Instance;
            if (m_Director != null) m_Director.PhaseChanged += OnPhaseChanged;
        }

        void OnDestroy()
        {
            if (m_Director != null) m_Director.PhaseChanged -= OnPhaseChanged;
        }

        void OnPhaseChanged()
        {
            bool visible = m_Director.Phase == TavernPhase.Results && m_Director.Session != null;
            m_Screen.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;

            var l = m_Director.Session.Ledger;
            m_Title.text = Loc.UI(TavernLocKeys.ResultsTitle);
            m_Again.text = Loc.UI(m_Director.Flow != null ? LoopLocKeys.ResultsToNight : TavernLocKeys.ResultsAgain);
            m_Rows.Clear();
            if (m_Director.Session.ClosedEarly) AddRow(Loc.UI(TavernLocKeys.ResultsClosedEarly));
            Add(TavernLocKeys.ResultsServed, l.DishesServed);
            Add(TavernLocKeys.ResultsGold, l.Gold);
            Add(TavernLocKeys.ResultsTips, l.Tips);
            Add(TavernLocKeys.ResultsRenown, l.Renown);
            Add(TavernLocKeys.ResultsWalkouts, l.Walkouts);
            Add(TavernLocKeys.ResultsSoldOut, l.SoldOutLeaves);
            Add(TavernLocKeys.ResultsDropped, l.DroppedDishes);
            m_Screen.schedule.Execute(() => m_Again.Focus());
        }

        void Add(string key, int value) => AddRow(Loc.UI(key, value));

        void AddRow(string text)
        {
            var row = new Label(text);
            row.AddToClassList("hd-results__row");
            m_Rows.Add(row);
        }
    }
}
