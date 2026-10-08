using System.Collections;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Story;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Story.Dialogue;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.World;
using Hearthdelve.UI.Tavern;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Hearthdelve.Story.Presentation
{
    /// <summary>
    /// Overheard exchanges (4h Checkpoint D): plays a short authored conversation (<c>Ambient/…</c>, seeded once, owned by the node
    /// editor) line by line in one small bubble over each speaker's head. The Dialogue System's graph decides what's said: from
    /// START, the first entry whose condition holds, then each next one the same way; scripts run (a once-only line marks its
    /// variable). Never interactive, never holds the clock, one bubble at a time; a conversation, a menu or a held moment
    /// (Decorate Mode, a panel) stops it at once. When it plays is the village's business (<c>AmbientMoments</c>).
    /// </summary>
    [DefaultExecutionOrder(1000)] // placed after the camera has moved
    public sealed class AmbientBarks : MonoBehaviour, IBarkService
    {
        [SerializeField] GameObject m_Bubble;
        [SerializeField] RectTransform m_Panel;
        [SerializeField] LocalizedSuperText m_Text;
        [SerializeField, Tooltip("From the speaker's feet to the bubble's bottom, in tiles.")] float m_Above = 2.3f;
        [SerializeField, Min(0.5f)] float m_MinSeconds = 2.4f;
        [SerializeField, Min(0f), Tooltip("Extra seconds a line stays up per character.")] float m_PerCharacter = 0.05f;
        [SerializeField, Min(0f)] float m_Gap = 0.35f;

        Coroutine m_Running;
        Transform m_Speaker;
        RectTransform m_Canvas;
        Camera m_Camera;
        PixelPerfectCamera m_PixelPerfect;
        SuperTextMesh m_Stm;

        public bool IsBarking => m_Running != null;
        /// <summary>The line showing now (tests), or null.</summary>
        public string Showing { get; private set; }
        /// <summary>Who's saying it (tests).</summary>
        public string ShowingSpeaker { get; private set; }
        /// <summary>How many bubbles are up (tests: never more than one).</summary>
        public int Bubbles => m_Bubble != null && m_Bubble.activeSelf ? 1 : 0;
        /// <summary>Where the bubble sits on its canvas (tests), and the canvas's size.</summary>
        public Vector2 BubbleAt => m_Panel != null ? m_Panel.anchoredPosition : Vector2.zero;
        public Vector2 CanvasSize => m_Canvas != null ? m_Canvas.rect.size : Vector2.zero;

        public void Configure(GameObject bubble, RectTransform panel, LocalizedSuperText text)
        {
            m_Bubble = bubble;
            m_Panel = panel;
            m_Text = text;
        }

        void Awake()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) m_Canvas = (RectTransform)canvas.rootCanvas.transform;
            if (m_Text != null) m_Stm = m_Text.GetComponent<SuperTextMesh>();
            if (m_Bubble != null) m_Bubble.SetActive(false);
        }

        void OnEnable() => StoryServices.RegisterBarks(this);
        void OnDisable() => StoryServices.UnregisterBarks(this);

        /// <summary>Something more important is on screen: nothing is overheard now.</summary>
        public static bool Blocked =>
            (StoryServices.Conversations != null && StoryServices.Conversations.IsTalking) || MenuPause.IsPaused || SurfacePause.IsHeld;

        public bool Play(string title)
        {
            if (IsBarking || Blocked || !DialogueManager.hasInstance || DialogueManager.masterDatabase == null) return false;
            Conversation conversation = DialogueManager.masterDatabase.GetConversation(title);
            if (conversation == null) return false;
            DialogueEntry first = Next(conversation, conversation.GetFirstDialogueEntry());
            if (first == null) return false;
            m_Running = StartCoroutine(Run(conversation, first));
            return true;
        }

        public void Stop()
        {
            if (m_Running != null) StopCoroutine(m_Running);
            m_Running = null;
            Hide();
        }

        /// <summary>The first linked entry (in this conversation) whose condition holds now, or null.</summary>
        static DialogueEntry Next(Conversation conversation, DialogueEntry from)
        {
            if (from == null) return null;
            foreach (Link link in from.outgoingLinks)
            {
                if (link.destinationConversationID != conversation.id) continue;
                DialogueEntry e = conversation.GetDialogueEntry(link.destinationDialogueID);
                if (e != null && (string.IsNullOrEmpty(e.conditionsString) || Lua.IsTrue(e.conditionsString))) return e;
            }
            return null;
        }

        IEnumerator Run(Conversation conversation, DialogueEntry entry)
        {
            while (entry != null)
            {
                if (!string.IsNullOrEmpty(entry.userScript)) Lua.Run(entry.userScript);
                Actor actor = DialogueManager.masterDatabase.GetActor(entry.ActorID);
                string speaker = actor != null ? Field.LookupValue(actor.fields, DialogueAdapter.CharacterIdField) : null;
                m_Speaker = Speakers.Find(speaker);
                string line = DialogueText.Entry(entry);
                if (m_Speaker == null || string.IsNullOrEmpty(line)) break;
                Show(line, speaker);
                float seconds = Mathf.Max(m_MinSeconds, m_MinSeconds + line.Length * m_PerCharacter - 1f);
                bool cut = false;
                for (float t = 0f; t < seconds && !cut; t += Time.unscaledDeltaTime)
                {
                    cut = Blocked || m_Speaker == null;
                    if (!cut) yield return null;
                }
                Hide();
                if (cut) break;
                yield return new WaitForSecondsRealtime(m_Gap);
                if (Blocked) break;
                entry = Next(conversation, entry);
            }
            Hide();
            m_Running = null;
        }

        void Show(string line, string speaker)
        {
            Showing = line;
            ShowingSpeaker = speaker;
            if (m_Text != null) m_Text.Set(TavernLocKeys.Plain, line);
            if (m_Bubble != null) m_Bubble.SetActive(true);
            Place();
        }

        void Hide()
        {
            Showing = null;
            ShowingSpeaker = null;
            if (m_Bubble != null) m_Bubble.SetActive(false);
        }

        void LateUpdate()
        {
            if (m_Bubble != null && m_Bubble.activeSelf) Place();
        }

        void Place()
        {
            if (m_Speaker == null || m_Panel == null || m_Canvas == null) return;
            if (m_Camera == null)
            {
                m_Camera = Camera.main;
                if (m_Camera != null) m_PixelPerfect = m_Camera.GetComponent<PixelPerfectCamera>();
            }
            if (m_Camera == null) return;
            // Fit the panel to the lines the text took (one or two), then keep it on screen.
            if (m_Stm != null && m_Stm.lineHeights != null && m_Stm.lineHeights.Count > 1)
            {
                int lines = Mathf.Clamp(m_Stm.lineHeights.Count - 1, 1, 3);
                m_Panel.sizeDelta = new Vector2(m_Panel.sizeDelta.x, lines * SilverMetricsLine + 8f);
            }
            Vector2 at = SpeechBubble.ToCanvas(m_Camera, m_PixelPerfect, m_Canvas, m_Speaker.position + new Vector3(0f, m_Above, 0f));
            Vector2 size = m_Canvas.rect.size, half = m_Panel.sizeDelta * 0.5f;
            at.x = Mathf.Clamp(at.x, half.x + 2f, size.x - half.x - 2f);
            at.y = Mathf.Clamp(at.y, 2f, size.y - m_Panel.sizeDelta.y - 2f);
            m_Panel.anchorMin = m_Panel.anchorMax = Vector2.zero;
            m_Panel.pivot = new Vector2(0.5f, 0f);
            m_Panel.anchoredPosition = new Vector2(Mathf.Round(at.x), Mathf.Round(at.y));
        }

        const float SilverMetricsLine = 12f;
    }
}
