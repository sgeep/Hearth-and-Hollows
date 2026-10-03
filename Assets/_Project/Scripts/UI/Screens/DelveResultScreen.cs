using System;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// The end of a delve: how it ended, the parts brought home (shown like satchel slots) and how many
    /// were lost, then on to the tavern (with the day loop) or into another delve (the floor on its own).
    /// </summary>
    public sealed class DelveResultScreen : MonoBehaviour
    {
        [SerializeField] GameObject m_Panel;
        [SerializeField] LocalizedSuperText m_Title;
        [SerializeField] LocalizedSuperText m_Summary;
        [SerializeField] SatchelSlotView[] m_Slots = Array.Empty<SatchelSlotView>();
        [SerializeField] Button m_Continue;
        [SerializeField] LocalizedSuperText m_ContinueLabel;

        Action m_OnContinue;

        public bool IsOpen => m_Panel != null && m_Panel.activeSelf;
        public DelveReport Report { get; private set; }
        public SatchelSlotView[] Slots => m_Slots;
        public Button Continue => m_Continue;

        public void Configure(GameObject panel, LocalizedSuperText title, LocalizedSuperText summary, SatchelSlotView[] slots, Button proceed, LocalizedSuperText proceedLabel)
        {
            m_Panel = panel;
            m_Title = title;
            m_Summary = summary;
            m_Slots = slots;
            m_Continue = proceed;
            m_ContinueLabel = proceedLabel;
        }

        void Awake()
        {
            if (m_Continue != null) m_Continue.onClick.AddListener(Proceed);
            if (m_Panel != null) m_Panel.SetActive(false);
        }

        void OnEnable() => EventBus<DelveResultRequested>.Subscribe(Open);
        void OnDisable() => EventBus<DelveResultRequested>.Unsubscribe(Open);

        void Open(DelveResultRequested request)
        {
            Report = request.Report;
            m_OnContinue = request.OnContinue;
            bool extracted = Report.Outcome == DelveOutcome.Extracted;
            m_Title?.Set(extracted ? LocKeys.ResultTitleExtracted : LocKeys.ResultTitleDied);
            if (m_Summary != null)
            {
                if (Report.Haul.Count == 0) m_Summary.Set(Report.PartsLost > 0 ? LocKeys.ResultNothingLost : LocKeys.ResultNothing, Report.PartsLost);
                else m_Summary.Set(LocKeys.ResultSummary, Report.PartsBroughtBack, Report.PartsLost);
            }
            for (int i = 0; i < m_Slots.Length; i++)
            {
                bool shown = i < Report.Haul.Count;
                m_Slots[i].gameObject.SetActive(shown);
                if (shown) m_Slots[i].Show(Report.Haul[i]);
            }
            m_ContinueLabel?.Set(request.BackToTavern ? LocKeys.ResultBackToTavern : LocKeys.ResultDelveAgain);
            m_Panel.SetActive(true);
            if (EventSystem.current != null && m_Continue != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(m_Continue.gameObject);
            }
        }

        public void Proceed()
        {
            if (!IsOpen) return;
            m_Panel.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            Action proceed = m_OnContinue;
            m_OnContinue = null;
            proceed?.Invoke();
        }
    }
}
