using System;
using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Story.Dialogue;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hearthdelve.Story.Presentation
{
    /// <summary>
    /// Hearth &amp; Hollows' dialogue box (4g Step 2): the Dialogue System's conversations drawn in uGUI with Super Text Mesh, in
    /// Silver at 1×, on the 320×180 art grid. A box along the bottom with the speaker's portrait (2×) and name, up to four lines,
    /// the text revealed as it's spoken and a ▼ when it waits; the player's choices in a panel above it, with the speaker's line
    /// still showing. The world stays visible above and is paused (<see cref="MenuPause"/>) while the box is open.
    /// <para>
    /// Controls: Enter, Space, E or the gamepad's A (or a click on the box) finishes the line's reveal, then moves on. Choices are
    /// ordinary uGUI buttons: the stick, d-pad, arrows or W/S move between them, the mouse picks by hovering, and Submit, E or a
    /// click chooses. While the box is open only the UI input map is on; the maps that were on come back when it closes.
    /// </para>
    /// <para>The text is the Dialogue table's (Unity Localization), keyed by each entry's Guid field; the database's own text is the fallback.</para>
    /// </summary>
    public sealed class HearthDialogueUI : MonoBehaviour, IDialogueUI
    {
        [SerializeField] CanvasGroup m_Root;
        [SerializeField] Image m_Portrait;
        [SerializeField] GameObject m_PortraitFrame;
        [SerializeField] LocalizedSuperText m_SpeakerLabel;
        [SerializeField] LocalizedSuperText m_Body;
        [SerializeField] RectTransform m_Continue;
        [SerializeField] DialogueBoxClick m_Box;
        [SerializeField] RectTransform m_Choices;
        [SerializeField] Button[] m_ChoiceButtons = Array.Empty<Button>();
        [SerializeField] LocalizedSuperText[] m_ChoiceLabels = Array.Empty<LocalizedSuperText>();
        [SerializeField] GameObject[] m_ChoicePointers = Array.Empty<GameObject>();
        [SerializeField, Min(1f), Tooltip("How fast a line is revealed.")] float m_CharactersPerSecond = 45f;
        [SerializeField, Min(0.05f), Tooltip("The ▼ bobs a pixel this often (seconds).")] float m_ContinueBob = 0.35f;
        [SerializeField, Tooltip("Pixels from one choice to the next, and the panel's margin above and below them.")]
        float m_ChoicePitch = 15f, m_ChoicePad = 4f;

        enum Mode { Closed, Speaking, Choosing }

        Mode m_Mode;
        SuperTextMesh m_BodyText;
        Response[] m_Responses = Array.Empty<Response>();
        int m_ResponseCount;
        string[] m_Maps;
        int m_ArmedAfterFrame;
        bool m_Paused;
        PortraitDefinition m_Speaker;
        float m_TalkTime, m_NextBlink, m_BlinkUntil;
        Vector2 m_ContinueAt;

        public event EventHandler<SelectedResponseEventArgs> SelectedResponseHandler;

        public bool IsOpen => m_Mode != Mode.Closed;
        public bool IsChoosing => m_Mode == Mode.Choosing;
        /// <summary>The line is still being revealed.</summary>
        public bool IsRevealing => m_BodyText != null && m_BodyText.reading;
        /// <summary>The speaker shown now (their character id; null for the player or nobody).</summary>
        public string SpeakerId { get; private set; }
        public string SpeakerName { get; private set; }
        /// <summary>The line as shown (tests and captures).</summary>
        public string Line { get; private set; } = string.Empty;
        public int ResponseCount => m_ResponseCount;
        public Button ChoiceButton(int i) => i >= 0 && i < m_ChoiceButtons.Length ? m_ChoiceButtons[i] : null;
        public string ChoiceText(int i) => i >= 0 && i < m_ResponseCount && m_ChoiceLabels[i] != null ? m_ChoiceLabels[i].GetComponent<SuperTextMesh>().text : null;
        public bool ContinueShown => m_Continue != null && m_Continue.gameObject.activeSelf;

        public void Configure(CanvasGroup root, Image portrait, GameObject portraitFrame, LocalizedSuperText speakerName, LocalizedSuperText body,
            RectTransform continueMark, DialogueBoxClick box, RectTransform choices, Button[] choiceButtons, LocalizedSuperText[] choiceLabels,
            GameObject[] choicePointers)
        {
            m_Root = root;
            m_Portrait = portrait;
            m_PortraitFrame = portraitFrame;
            m_SpeakerLabel = speakerName;
            m_Body = body;
            m_Continue = continueMark;
            m_Box = box;
            m_Choices = choices;
            m_ChoiceButtons = choiceButtons;
            m_ChoiceLabels = choiceLabels;
            m_ChoicePointers = choicePointers;
        }

        void Awake()
        {
            m_BodyText = m_Body != null ? m_Body.GetComponent<SuperTextMesh>() : null;
            if (m_Continue != null) m_ContinueAt = m_Continue.anchoredPosition;
            for (int i = 0; i < m_ChoiceButtons.Length; i++)
            {
                int index = i;
                m_ChoiceButtons[i].onClick.AddListener(() => Choose(index));
            }
            if (m_Box != null) m_Box.Clicked += OnBoxClicked;
            Hide();
        }

        void OnDestroy()
        {
            if (m_Box != null) m_Box.Clicked -= OnBoxClicked;
            if (m_Paused) MenuPause.Pop();
        }

        // ---------- IDialogueUI ----------

        public void Open()
        {
            if (m_Mode != Mode.Closed) return;
            m_Maps = InputMaps.Snapshot();
            InputMaps.ActivateUIOnly();
            if (!m_Paused)
            {
                MenuPause.Push();
                m_Paused = true;
            }
            m_Mode = Mode.Speaking;
            m_Root.alpha = 1f;
            m_Root.blocksRaycasts = true;
            m_Root.gameObject.SetActive(true);
            SetChoicesVisible(false);
            Select(null);
            Arm();
        }

        public void Close()
        {
            if (m_Mode == Mode.Closed) return;
            Hide();
            if (m_Paused)
            {
                MenuPause.Pop();
                m_Paused = false;
            }
            InputMaps.Restore(m_Maps);
            m_Maps = null;
            Select(null);
        }

        public void ShowSubtitle(Subtitle subtitle)
        {
            if (subtitle == null) return;
            if (m_Mode == Mode.Closed) Open();
            m_Mode = Mode.Speaking;
            SetChoicesVisible(false);
            ShowSpeaker(subtitle);
            Line = DialogueText.Line(subtitle);
            if (m_BodyText != null) m_BodyText.readDelay = 1f / Mathf.Max(1f, m_CharactersPerSecond);
            m_Body.Set(TavernLocKeys.Plain, Line);
            m_TalkTime = 0f;
            if (m_Continue != null) m_Continue.gameObject.SetActive(false);
            Select(null);
            Arm();
        }

        public void HideSubtitle(Subtitle subtitle)
        {
            // The line stays on screen until the next one, or under the choices that answer it.
        }

        public void ShowResponses(Subtitle subtitle, Response[] responses, float timeout)
        {
            if (m_Mode == Mode.Closed) Open();
            m_Responses = responses ?? Array.Empty<Response>();
            m_ResponseCount = Mathf.Min(m_Responses.Length, m_ChoiceButtons.Length);
            if (m_Responses.Length > m_ChoiceButtons.Length)
                Debug.LogWarning($"[Hearthdelve] Dialogue: {m_Responses.Length} choices, the box shows {m_ChoiceButtons.Length}.");
            // The line being answered stays in the box, fully shown.
            if (string.IsNullOrEmpty(Line) && subtitle != null)
            {
                ShowSpeaker(subtitle);
                Line = DialogueText.Line(subtitle);
                m_Body.Set(TavernLocKeys.Plain, Line);
            }
            if (m_BodyText != null && m_BodyText.reading) m_BodyText.SkipToEnd();
            if (m_Continue != null) m_Continue.gameObject.SetActive(false);
            Button first = null;
            for (int i = 0; i < m_ChoiceButtons.Length; i++)
            {
                bool shown = i < m_ResponseCount;
                m_ChoiceButtons[i].gameObject.SetActive(shown);
                if (!shown) continue;
                Response r = m_Responses[i];
                m_ChoiceLabels[i].Set(TavernLocKeys.Plain, DialogueText.Response(r));
                m_ChoiceButtons[i].interactable = r.enabled;
                if (first == null && r.enabled) first = m_ChoiceButtons[i];
            }
            Wire(m_ResponseCount);
            // The panel is as tall as its choices.
            if (m_Choices != null) m_Choices.sizeDelta = new Vector2(m_Choices.sizeDelta.x, m_ChoicePad * 2f + m_ResponseCount * m_ChoicePitch - 1f);
            m_Mode = Mode.Choosing;
            SetChoicesVisible(true);
            Select(first != null ? first.gameObject : null);
            Arm();
        }

        public void HideResponses()
        {
            SetChoicesVisible(false);
            if (m_Mode == Mode.Choosing) m_Mode = Mode.Speaking;
        }

        public void ShowQTEIndicator(int index) { }
        public void HideQTEIndicator(int index) { }

        /// <summary>Not used by Hearth &amp; Hollows (alerts are the game's own HUD); logged in development builds.</summary>
        public void ShowAlert(string message, float duration)
        {
            if (Debug.isDebugBuild) Debug.Log($"[Hearthdelve] Dialogue alert: {message}");
        }

        public void HideAlert() { }

        // ---------- Input ----------

        /// <summary>A press that opened the box or chose a line this frame doesn't also move it on.</summary>
        void Arm() => m_ArmedAfterFrame = Time.frameCount;

        static bool Pressed(string action)
        {
            InputAction a = InputMaps.Find(InputMaps.UI, action);
            return a != null && a.WasPressedThisFrame();
        }

        void Update()
        {
            if (m_Mode == Mode.Closed) return;
            AnimatePortrait();
            bool waiting = m_Mode == Mode.Speaking && !IsRevealing;
            if (m_Continue != null)
            {
                if (m_Continue.gameObject.activeSelf != waiting) m_Continue.gameObject.SetActive(waiting);
                if (waiting) m_Continue.anchoredPosition = m_ContinueAt + new Vector2(0f, Mathf.FloorToInt(Time.unscaledTime / m_ContinueBob) % 2 == 0 ? 0f : -1f);
            }
            if (Time.frameCount <= m_ArmedAfterFrame) return;
            if (m_Mode == Mode.Speaking)
            {
                if (Pressed(UIActions.Submit) || Pressed(UIActions.Advance)) Advance();
            }
            else if (m_Mode == Mode.Choosing)
            {
                // E chooses like Submit (uGUI's Submit is handled by the input module).
                if (Pressed(UIActions.Advance))
                {
                    GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
                    int i = selected != null ? Array.IndexOf(m_ChoiceButtons, selected.GetComponent<Button>()) : -1;
                    if (i >= 0) m_ChoiceButtons[i].onClick.Invoke();
                }
                // Something always holds the focus while choosing (a click elsewhere drops it).
                else if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null) Select(FirstEnabled());
                ShowPointers();
            }
        }

        void OnBoxClicked()
        {
            if (m_Mode == Mode.Speaking && Time.frameCount > m_ArmedAfterFrame) Advance();
        }

        /// <summary>Finishes the line's reveal; once it's all there, moves the conversation on.</summary>
        public void Advance()
        {
            if (m_Mode != Mode.Speaking) return;
            if (IsRevealing)
            {
                m_BodyText.SkipToEnd();
                return;
            }
            UiFeedback.Play(UiMoment.Tick);
            Arm();
            if (DialogueManager.hasInstance)
                DialogueManager.instance.SendMessage(DialogueSystemMessages.OnConversationContinue, (IDialogueUI)this, SendMessageOptions.DontRequireReceiver);
        }

        /// <summary>Chooses response <paramref name="index"/> (a click, Submit or E on its button).</summary>
        public void Choose(int index)
        {
            if (m_Mode != Mode.Choosing || index < 0 || index >= m_ResponseCount || !m_Responses[index].enabled) return;
            Response chosen = m_Responses[index];
            HideResponses();
            Arm();
            SelectedResponseHandler?.Invoke(this, new SelectedResponseEventArgs(chosen));
        }

        // ---------- Presentation ----------

        void ShowSpeaker(Subtitle subtitle)
        {
            PixelCrushers.DialogueSystem.CharacterInfo info = subtitle.speakerInfo;
            Actor actor = DialogueManager.masterDatabase != null && info != null ? DialogueManager.masterDatabase.GetActor(info.id) : null;
            SpeakerId = DialogueAdapter.CharacterId(actor);
            CharacterDefinition character = StoryHost.Instance != null ? StoryHost.Instance.Characters.Definition(SpeakerId) : null;
            bool player = info != null && info.isPlayer;
            SpeakerName = player ? DialogueText.PlayerName() : character != null ? Loc.Get(character.displayName) : info?.Name ?? string.Empty;
            m_SpeakerLabel.Set(TavernLocKeys.Plain, SpeakerName);
            m_Speaker = player || character == null ? null : character.portrait;
            if (m_PortraitFrame != null) m_PortraitFrame.SetActive(m_Speaker != null);
            m_NextBlink = Time.unscaledTime + NextBlinkIn();
            m_BlinkUntil = 0f;
            AnimatePortrait();
        }

        float NextBlinkIn() => m_Speaker != null ? UnityEngine.Random.Range(m_Speaker.blinkEvery.x, m_Speaker.blinkEvery.y) : 3f;

        void AnimatePortrait()
        {
            if (m_Speaker == null || m_Portrait == null) return;
            float now = Time.unscaledTime;
            bool talking = m_Mode == Mode.Speaking && IsRevealing;
            if (talking) m_TalkTime += Time.unscaledDeltaTime;
            if (now >= m_NextBlink)
            {
                m_BlinkUntil = now + m_Speaker.blinkSeconds;
                m_NextBlink = now + NextBlinkIn();
            }
            Sprite frame = m_Speaker.Frame(talking, m_TalkTime, now < m_BlinkUntil);
            if (m_Portrait.sprite != frame) m_Portrait.sprite = frame;
        }

        void SetChoicesVisible(bool visible)
        {
            if (m_Choices != null && m_Choices.gameObject.activeSelf != visible) m_Choices.gameObject.SetActive(visible);
            if (!visible) m_ResponseCount = 0;
            ShowPointers();
        }

        /// <summary>
        /// The ▶ beside the focused choice. The marks stay active and are culled when hidden: Super Text Mesh doesn't build a
        /// text first switched on later (the Checkpoint A captures showed none).
        /// </summary>
        void ShowPointers()
        {
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            for (int i = 0; i < m_ChoicePointers.Length; i++)
            {
                bool on = m_Mode == Mode.Choosing && i < m_ResponseCount && selected == m_ChoiceButtons[i].gameObject;
                CanvasRenderer mark = m_ChoicePointers[i] != null ? m_ChoicePointers[i].GetComponent<CanvasRenderer>() : null;
                if (mark != null && mark.cull == on) mark.cull = !on;
            }
        }

        /// <summary>Whether the ▶ shows beside choice <paramref name="i"/> (tests).</summary>
        public bool PointerShown(int i) => i >= 0 && i < m_ChoicePointers.Length && m_ChoicePointers[i] != null &&
                                           m_ChoicePointers[i].GetComponent<CanvasRenderer>() is { cull: false };

        /// <summary>Up and down move between the shown choices, wrapping round.</summary>
        void Wire(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Navigation nav = m_ChoiceButtons[i].navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = m_ChoiceButtons[(i - 1 + count) % count];
                nav.selectOnDown = m_ChoiceButtons[(i + 1) % count];
                nav.selectOnLeft = nav.selectOnRight = null;
                m_ChoiceButtons[i].navigation = nav;
            }
        }

        GameObject FirstEnabled()
        {
            for (int i = 0; i < m_ResponseCount; i++)
                if (m_ChoiceButtons[i].interactable) return m_ChoiceButtons[i].gameObject;
            return null;
        }

        static void Select(GameObject target)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target);
        }

        void Hide()
        {
            m_Mode = Mode.Closed;
            Line = string.Empty;
            SpeakerId = SpeakerName = null;
            m_Speaker = null;
            m_ResponseCount = 0;
            if (m_Root != null)
            {
                m_Root.alpha = 0f;
                m_Root.blocksRaycasts = false;
            }
            SetChoicesVisible(false);
            if (m_Continue != null) m_Continue.gameObject.SetActive(false);
        }
    }
}
