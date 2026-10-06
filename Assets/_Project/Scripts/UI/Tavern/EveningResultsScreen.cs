using System;
using System.Collections;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.Tavern.Service;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
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
        [SerializeField] LocalizedSuperText[] m_Labels = Array.Empty<LocalizedSuperText>();
        [SerializeField, Tooltip("Each line's amount, beside its label.")] LocalizedSuperText[] m_Lines = Array.Empty<LocalizedSuperText>();
        [SerializeField] GameObject m_TakingsRow;
        [SerializeField] LocalizedSuperText m_Takings;
        [SerializeField] Button m_Done;
        [SerializeField] LocalizedSuperText m_DoneLabel;
        [SerializeField, Min(0.05f), Tooltip("Seconds between lines.")] float m_LineGap = 0.3f;
        [SerializeField, Min(0.05f), Tooltip("Seconds a number takes to count up.")] float m_CountTime = 0.35f;

        TavernDirector m_Director;
        Coroutine m_Reveal;

        public bool IsShown => m_Root != null && m_Root.activeSelf;
        public bool IsRevealing => m_Reveal != null;
        public Button DoneButton => m_Done;

        public void Configure(GameObject root, LocalizedSuperText note, LocalizedSuperText[] labels, LocalizedSuperText[] lines, GameObject takingsRow, LocalizedSuperText takings,
            Button done, LocalizedSuperText doneLabel)
        {
            m_Labels = labels;
            m_TakingsRow = takingsRow;
            m_DoneLabel = doneLabel;
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
            for (int i = 0; i < m_Lines.Length; i++) ShowLine(i, false);
            m_TakingsRow.SetActive(false);
            // In the day loop the evening ends in Night (the takings are banked there); on its own, another evening.
            if (m_DoneLabel != null) m_DoneLabel.Set(m_Director.InDayLoop ? LoopLocKeys.ResultsToNight : TavernLocKeys.ResultsAgain);
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
                ShowLine(i, true);
                m_Labels[i].Set(KeyFor(kind));
                UiFeedback.Play(UiMoment.Tick);
                for (float t = 0f; t < m_CountTime; t += Time.unscaledDeltaTime)
                {
                    text.Set(TavernLocKeys.Plain, Format(kind, Mathf.RoundToInt(value * (t / m_CountTime)), report));
                    yield return null;
                }
                text.Set(TavernLocKeys.Plain, Format(kind, value, report));
            }
            if (!report.StayedShut)
            {
                yield return new WaitForSecondsRealtime(m_LineGap);
                m_TakingsRow.SetActive(true);
                m_Takings.Set(TavernLocKeys.PrepValue, report.Takings);
                UiFeedback.Play(UiMoment.Takings);
            }
            m_Reveal = null;
        }

        void ShowAll(EveningReport report)
        {
            for (int i = 0; i < m_Lines.Length; i++)
            {
                bool has = i < report.Lines.Count;
                ShowLine(i, has);
                if (!has) continue;
                m_Labels[i].Set(KeyFor(report.Lines[i].line));
                m_Lines[i].Set(TavernLocKeys.Plain, Format(report.Lines[i].line, report.Lines[i].value, report));
            }
            m_TakingsRow.SetActive(!report.StayedShut);
            if (!report.StayedShut) m_Takings.Set(TavernLocKeys.PrepValue, report.Takings);
        }

        void ShowLine(int i, bool shown)
        {
            m_Lines[i].gameObject.SetActive(shown);
            if (i < m_Labels.Length) m_Labels[i].gameObject.SetActive(shown);
        }

        public static string KeyFor(EveningLine line) => line switch
        {
            EveningLine.Served => TavernLocKeys.ResultsServed,
            EveningLine.Gold => TavernLocKeys.ResultsGold,
            EveningLine.Tips => TavernLocKeys.ResultsTips,
            EveningLine.Renown => TavernLocKeys.ResultsRenown,
            EveningLine.Walkouts => TavernLocKeys.ResultsWalkouts,
            EveningLine.SoldOut => TavernLocKeys.ResultsSoldOut,
            EveningLine.Requests => TavernLocKeys.ResultsRequests,
            _ => TavernLocKeys.ResultsDropped,
        };

        static object Format(EveningLine line, int value, EveningReport report) => line switch
        {
            EveningLine.Renown when value > 0 => $"+{value}",
            // Special requests: those met of those made.
            EveningLine.Requests => Loc.UI(TavernLocKeys.ResultsRequestsOf, value, report != null ? report.RequestsIssued : value),
            _ => value,
        };
    }
}
