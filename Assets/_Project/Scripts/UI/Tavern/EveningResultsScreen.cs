using System;
using System.Collections;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Service;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>
    /// Results (the end of the evening): what was served, earned, tipped and how Renown moved, then anything
    /// that went wrong, revealed line by line with the numbers counting up, and finally the evening's takings.
    /// The room stays in view behind it while the last customers leave. The button (pressed mid-reveal it
    /// finishes the reveal) ends the evening.
    /// </summary>
    public sealed class EveningResultsScreen : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] LocalizedSuperText m_Note;
        [SerializeField] LocalizedSuperText[] m_Lines = Array.Empty<LocalizedSuperText>();
        [SerializeField] LocalizedSuperText m_Takings;
        [SerializeField] Button m_Done;
        [SerializeField, Min(0.05f), Tooltip("Seconds between lines.")] float m_LineGap = 0.3f;
        [SerializeField, Min(0.05f), Tooltip("Seconds a number takes to count up.")] float m_CountTime = 0.35f;

        TavernDirector m_Director;
        Coroutine m_Reveal;

        public bool IsShown => m_Root != null && m_Root.activeSelf;
        public bool IsRevealing => m_Reveal != null;
        public Button DoneButton => m_Done;

        public void Configure(GameObject root, LocalizedSuperText note, LocalizedSuperText[] lines, LocalizedSuperText takings, Button done)
        {
            m_Root = root;
            m_Note = note;
            m_Lines = lines;
            m_Takings = takings;
            m_Done = done;
        }

        void Start()
        {
            m_Director = TavernDirector.Instance;
            m_Root.SetActive(false);
            if (m_Director == null) return;
            m_Done.onClick.AddListener(OnDone);
            m_Director.PhaseChanged += OnPhase;
            OnPhase();
        }

        void OnDestroy()
        {
            if (m_Director != null) m_Director.PhaseChanged -= OnPhase;
        }

        void OnPhase()
        {
            bool shown = m_Director.Phase == TavernPhase.Results && m_Director.Report != null;
            m_Root.SetActive(shown);
            if (!shown) return;
            EveningReport report = m_Director.Report;
            m_Note.gameObject.SetActive(report.StayedShut || report.ClosedEarly);
            if (report.StayedShut) m_Note.Set(TavernLocKeys.ResultsStayedShut);
            else if (report.ClosedEarly) m_Note.Set(TavernLocKeys.ResultsClosedEarly);
            foreach (LocalizedSuperText line in m_Lines) line.gameObject.SetActive(false);
            m_Takings.gameObject.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(m_Done.gameObject);
            m_Reveal = StartCoroutine(Reveal(report));
        }

        void OnDone()
        {
            if (m_Reveal != null)
            {
                StopCoroutine(m_Reveal);
                m_Reveal = null;
                ShowAll(m_Director.Report);
                return;
            }
            m_Director.FinishEvening();
        }

        // Real time: the evening is over, but nothing else is paused.
        IEnumerator Reveal(EveningReport report)
        {
            for (int i = 0; i < report.Lines.Count && i < m_Lines.Length; i++)
            {
                yield return new WaitForSecondsRealtime(m_LineGap);
                var (kind, value) = report.Lines[i];
                LocalizedSuperText text = m_Lines[i];
                text.gameObject.SetActive(true);
                for (float t = 0f; t < m_CountTime; t += Time.unscaledDeltaTime)
                {
                    text.Set(KeyFor(kind), Format(kind, Mathf.RoundToInt(value * (t / m_CountTime))));
                    yield return null;
                }
                text.Set(KeyFor(kind), Format(kind, value));
            }
            if (!report.StayedShut)
            {
                yield return new WaitForSecondsRealtime(m_LineGap);
                m_Takings.gameObject.SetActive(true);
                m_Takings.Set(TavernLocKeys.ResultsTakings, report.Takings);
            }
            m_Reveal = null;
        }

        void ShowAll(EveningReport report)
        {
            for (int i = 0; i < m_Lines.Length; i++)
            {
                bool has = i < report.Lines.Count;
                m_Lines[i].gameObject.SetActive(has);
                if (has) m_Lines[i].Set(KeyFor(report.Lines[i].line), Format(report.Lines[i].line, report.Lines[i].value));
            }
            m_Takings.gameObject.SetActive(!report.StayedShut);
            if (!report.StayedShut) m_Takings.Set(TavernLocKeys.ResultsTakings, report.Takings);
        }

        public static string KeyFor(EveningLine line) => line switch
        {
            EveningLine.Served => TavernLocKeys.ResultsServed,
            EveningLine.Gold => TavernLocKeys.ResultsGold,
            EveningLine.Tips => TavernLocKeys.ResultsTips,
            EveningLine.Renown => TavernLocKeys.ResultsRenown,
            EveningLine.Walkouts => TavernLocKeys.ResultsWalkouts,
            EveningLine.SoldOut => TavernLocKeys.ResultsSoldOut,
            _ => TavernLocKeys.ResultsDropped,
        };

        static object Format(EveningLine line, int value) => line == EveningLine.Renown && value > 0 ? $"+{value}" : value;
    }
}
