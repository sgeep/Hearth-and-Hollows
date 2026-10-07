using System.Collections;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>
    /// The surface HUD (4h): the clock's face ("2:40 pm") in a corner, moving in the clock's display steps, and beside it today's
    /// Vigor as a row of small pips (Checkpoint B). Quiet on purpose: the clock tells the world's time and isn't a countdown;
    /// the pips say what strenuous work is left, nothing more. Only in the free daytime, never in the Hollows (where the
    /// Essence bar alone speaks). Spending Vigor gives its pips a small pop, a soft tick and a light tap; a harvest gets a
    /// one-line note. Indoors, the Decorate key's reminder sits in the bottom corner, clear of the room.
    /// </summary>
    public sealed class SurfaceClockView : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] LocalizedSuperText m_Text;
        [SerializeField, Tooltip("The Decorate key's reminder, indoors only.")] GameObject m_DecorateRoot;
        [SerializeField] LocalizedSuperText m_Decorate;
        [SerializeField, Tooltip("Vigor's pips (as many as the most Vigor could be; the day's maximum are shown).")] Image[] m_Pips;
        [SerializeField] Color m_PipFull = new(0.96f, 0.78f, 0.32f, 1f);
        [SerializeField] Color m_PipEmpty = new(0.22f, 0.18f, 0.16f, 1f);
        [SerializeField] GameObject m_NoteRoot;
        [SerializeField] LocalizedSuperText m_Note;
        [SerializeField, Min(0.5f)] float m_NoteSeconds = 3f;

        bool m_DecorateShown;
        int m_Shown = -1;
        int m_ShownVigor = -1, m_ShownMax = -1;
        Coroutine m_NoteHide;

        public bool IsShown => m_Root != null && m_Root.activeSelf;
        /// <summary>The minute on the face now.</summary>
        public int ShownMinute => m_Shown;
        /// <summary>Full pips on show (tests).</summary>
        public int ShownVigor => m_ShownVigor;
        /// <summary>Pips on show, full or empty (tests).</summary>
        public int ShownPips => m_ShownMax;
        public bool NoteShown => m_NoteRoot != null && m_NoteRoot.activeSelf;

        public void Configure(GameObject root, LocalizedSuperText text, GameObject decorateRoot = null, LocalizedSuperText decorate = null,
            Image[] pips = null, GameObject noteRoot = null, LocalizedSuperText note = null)
        {
            m_Root = root;
            m_Text = text;
            m_DecorateRoot = decorateRoot;
            m_Decorate = decorate;
            m_Pips = pips;
            m_NoteRoot = noteRoot;
            m_Note = note;
        }

        void OnEnable()
        {
            EventBus<SurfaceTimeChanged>.Subscribe(OnTime);
            EventBus<VigorSpent>.Subscribe(OnSpent);
            EventBus<CropHarvested>.Subscribe(OnHarvested);
            if (m_NoteRoot != null) m_NoteRoot.SetActive(false);
        }

        void OnDisable()
        {
            EventBus<SurfaceTimeChanged>.Unsubscribe(OnTime);
            EventBus<VigorSpent>.Unsubscribe(OnSpent);
            EventBus<CropHarvested>.Unsubscribe(OnHarvested);
        }

        void OnTime(SurfaceTimeChanged e) => Show(e.Minute);

        void LateUpdate()
        {
            TavernDirector director = TavernDirector.Instance;
            GameFlow flow = GameFlow.Instance;
            bool shown = director != null && director.Phase == TavernPhase.Daytime && flow != null && flow.InGame;
            if (m_Root != null && m_Root.activeSelf != shown) m_Root.SetActive(shown);
            if (shown && m_Shown != SurfaceTime.ShownMinute) Show(SurfaceTime.ShownMinute);
            if (shown) ShowVigor(flow.State.Vigor.Current, flow.State.Vigor.Max);
            // Decorating is a key away indoors (4h): say which, quietly.
            bool decorate = shown && SurfaceArea.Current != null && SurfaceArea.Current.Indoors && (DecorateMode.Instance == null || !DecorateMode.Instance.IsActive);
            if (m_DecorateRoot != null && m_DecorateRoot.activeSelf != decorate) m_DecorateRoot.SetActive(decorate);
            if (decorate && !m_DecorateShown && m_Decorate != null)
                m_Decorate.Set(SurfaceLocKeys.DecorateHint, InputHints.Binding(Hearthdelve.Core.Input.InputMaps.Tavern, Hearthdelve.Core.Input.TavernActions.Decorate));
            m_DecorateShown = decorate;
            if (!shown && m_NoteRoot != null && m_NoteRoot.activeSelf) m_NoteRoot.SetActive(false);
        }

        void Show(int minute)
        {
            if (m_Text == null || minute == m_Shown) return;
            m_Shown = minute;
            (int hour, int minutes, bool pm) = SurfaceClock.Face(minute);
            m_Text.Set(SurfaceLocKeys.Clock, hour, minutes.ToString("00"), Loc.UI(pm ? SurfaceLocKeys.Pm : SurfaceLocKeys.Am));
        }

        void ShowVigor(int current, int max)
        {
            if (m_Pips == null || (current == m_ShownVigor && max == m_ShownMax)) return;
            m_ShownVigor = current;
            m_ShownMax = Mathf.Min(max, m_Pips.Length);
            for (int i = 0; i < m_Pips.Length; i++)
            {
                if (m_Pips[i] == null) continue;
                GameObject pip = m_Pips[i].transform.parent != null ? m_Pips[i].transform.parent.gameObject : m_Pips[i].gameObject;
                pip.SetActive(i < m_ShownMax);
                m_Pips[i].color = i < current ? m_PipFull : m_PipEmpty;
            }
        }

        void OnSpent(VigorSpent e)
        {
            UiFeedback.Play(UiMoment.Vigor);
            if (m_Pips == null || !isActiveAndEnabled) return;
            // The pips just emptied: the ones from what's left up to what was there before.
            for (int i = e.Remaining; i < Mathf.Min(e.Remaining + e.Amount, m_Pips.Length); i++)
                if (m_Pips[i] != null) StartCoroutine(Pop(m_Pips[i].rectTransform));
        }

        static IEnumerator Pop(RectTransform pip)
        {
            const float seconds = 0.22f;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                pip.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, t / seconds);
                yield return null;
            }
            pip.localScale = Vector3.one;
        }

        void OnHarvested(CropHarvested e)
        {
            UiFeedback.Play(UiMoment.Harvest);
            if (m_Note == null || m_NoteRoot == null || !isActiveAndEnabled) return;
            IngredientDefinition produce = GameFlow.Instance != null && GameFlow.Instance.Database != null ? GameFlow.Instance.Database.Ingredient(e.IngredientId) : null;
            string name = produce != null ? Loc.Get(produce.displayName) : e.IngredientId;
            m_Note.Set(e.Quality >= Quality.Fine ? GardenLocKeys.HarvestedFine : GardenLocKeys.Harvested, e.Count, name);
            m_NoteRoot.SetActive(true);
            if (m_NoteHide != null) StopCoroutine(m_NoteHide);
            m_NoteHide = StartCoroutine(HideNote());
        }

        IEnumerator HideNote()
        {
            yield return new WaitForSecondsRealtime(m_NoteSeconds);
            if (m_NoteRoot != null) m_NoteRoot.SetActive(false);
            m_NoteHide = null;
        }
    }
}
