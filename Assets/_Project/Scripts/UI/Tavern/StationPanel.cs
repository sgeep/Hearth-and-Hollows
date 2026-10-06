using Hearthdelve.Core.Input;
using Hearthdelve.Core.Services;
using Hearthdelve.Core.Minigames;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using UnityEngine;
using UnityEngine.UI;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>
    /// The station panel over the room (GDD §6.1): draws the Grill, Tap or Chop minigame the keeper is
    /// playing, from its live state (<see cref="KeeperWork.ActiveCook"/>). Reads only; the minigames decide.
    /// </summary>
    public sealed class StationPanel : MonoBehaviour
    {
        [Header("Grill")]
        [SerializeField] GameObject m_Grill;
        [SerializeField] LocalizedSuperText m_GrillSide;
        [SerializeField] LocalizedSuperText m_GrillPrompt;
        [SerializeField] RectTransform m_GrillBand;
        [SerializeField] RectTransform m_GrillNeedle;

        [Header("Tap")]
        [SerializeField] GameObject m_Tap;
        [SerializeField] LocalizedSuperText m_TapPrompt;
        [SerializeField] RectTransform m_Glass;
        [SerializeField] RectTransform m_Liquid;
        [SerializeField] RectTransform m_Foam;
        [SerializeField] RectTransform m_Line;
        [SerializeField] RectTransform m_FoamZone;

        [Header("Chop")]
        [SerializeField] GameObject m_Chop;
        [SerializeField] RectTransform m_Board;
        [SerializeField] Image m_Food;
        [SerializeField] RectTransform[] m_Lines = System.Array.Empty<RectTransform>();
        [SerializeField] Image[] m_Cuts = System.Array.Empty<Image>();
        [SerializeField] RectTransform m_Knife;
        [SerializeField] RectTransform m_Timer;
        [SerializeField] LocalizedSuperText m_ChopTitle;
        [SerializeField] LocalizedSuperText m_ChopProgress;
        [SerializeField] LocalizedSuperText m_ChopPrompt;

        [Header("Butcher Block (4f Checkpoint C)")]
        [SerializeField] GameObject m_Butcher;
        [SerializeField] RectTransform m_ButcherBoard;
        [SerializeField] Image m_ButcherPart;
        [SerializeField, Tooltip("The cut lines' dots, line after line.")] Image[] m_ButcherDots = System.Array.Empty<Image>();
        [SerializeField, Min(1)] int m_DotsPerLine = 7;
        [SerializeField] RectTransform m_ButcherKnife;
        [SerializeField] RectTransform m_ButcherTimer;
        [SerializeField] LocalizedSuperText m_ButcherTitle;
        [SerializeField] LocalizedSuperText m_ButcherPrompt;
        string m_LastButcherTitle;

        static readonly Color k_LineUncut = new(0.98f, 0.92f, 0.75f, 0.9f);

        public void ConfigureButcher(GameObject root, RectTransform board, Image part, Image[] dots, int dotsPerLine, RectTransform knife, RectTransform timer,
            LocalizedSuperText title, LocalizedSuperText prompt)
        {
            m_Butcher = root;
            m_ButcherBoard = board;
            m_ButcherPart = part;
            m_ButcherDots = dots;
            m_DotsPerLine = dotsPerLine;
            m_ButcherKnife = knife;
            m_ButcherTimer = timer;
            m_ButcherTitle = title;
            m_ButcherPrompt = prompt;
        }

        void DrawButcher(ButcherMinigame b)
        {
            ButcherSettings s = b.Settings;
            // The board spans the same share of the screen the pointer is mapped onto (KeeperWork).
            SpanX(m_ButcherBoard, s.boardLeft, s.boardLeft + s.boardWidth);
            IngredientItem part = KeeperWork.Instance != null ? KeeperWork.Instance.ButcheringPart : default;
            string title = part.IsValid ? Loc.Get(part.Definition.displayName) : string.Empty;
            if (title != m_LastButcherTitle)
            {
                m_LastButcherTitle = title;
                m_ButcherTitle?.Set(TavernLocKeys.ButcherPanelTitle, title);
                if (m_ButcherPart != null)
                {
                    m_ButcherPart.sprite = part.IsValid ? part.Definition.icon : null;
                    m_ButcherPart.enabled = m_ButcherPart.sprite != null;
                }
            }
            for (int i = 0; i < m_ButcherDots.Length; i++)
            {
                int line = i / m_DotsPerLine, dot = i % m_DotsPerLine;
                Image image = m_ButcherDots[i];
                bool shown = line < b.LineCount;
                image.gameObject.SetActive(shown);
                if (!shown) continue;
                float t = (dot + 0.5f) / m_DotsPerLine;
                var rect = (RectTransform)image.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(b.LineAt(line, t), 1f - t);
                rect.anchoredPosition = Vector2.zero;
                if (b.IsCut(line)) image.color = Color.Lerp(k_CutBad, k_CutGood, b.LineScore(line));
                else if (b.Stroke == line && t <= b.StrokeProgress) image.color = b.OnTrack ? k_CutGood : new Color(0.95f, 0.65f, 0.3f);
                else image.color = k_LineUncut;
            }
            m_ButcherKnife.anchorMin = m_ButcherKnife.anchorMax = new Vector2(b.Knife, b.Stroke >= 0 ? 1f - b.StrokeProgress : 1f);
            m_ButcherKnife.anchoredPosition = Vector2.zero;
            SpanX(m_ButcherTimer, 0f, Mathf.Clamp01(b.TimeLeft / Mathf.Max(0.01f, s.timeLimit)));
        }

        [SerializeField] LocalizedSuperText m_StepAway;
        [SerializeField, Tooltip("A wash over the box for a moment: gold for a perfect or clean result, red for a burn or overflow.")]
        Image m_Flash;
        [SerializeField, Min(0.01f)] float m_FlashSeconds = 0.3f;
        [SerializeField] Color m_GoodFlash = new(1f, 0.85f, 0.35f, 0.45f);
        [SerializeField] Color m_BadFlash = new(0.9f, 0.2f, 0.15f, 0.45f);
        [SerializeField, Tooltip("The grill needle as the meat nears burning (it follows the warning rumble).")]
        Color m_HotNeedle = new(1f, 0.3f, 0.2f);

        TavernFeedback m_Feedback;
        float m_FlashLeft;
        Color m_FlashColour;

        static readonly Color k_CutBad = new(0.85f, 0.25f, 0.2f);
        static readonly Color k_CutGood = new(0.3f, 0.85f, 0.35f);

        string m_LastGrillSide, m_LastChopTitle, m_LastChopProgress;
        bool m_PromptsSet;

        /// <summary>Which minigame is showing (null: the panel is closed).</summary>
        public IMinigame Showing { get; private set; }

        public void ConfigureGrill(GameObject root, LocalizedSuperText side, LocalizedSuperText prompt, RectTransform band, RectTransform needle)
        {
            m_Grill = root;
            m_GrillSide = side;
            m_GrillPrompt = prompt;
            m_GrillBand = band;
            m_GrillNeedle = needle;
        }

        public void ConfigureTap(GameObject root, LocalizedSuperText prompt, RectTransform glass, RectTransform liquid, RectTransform foam, RectTransform line, RectTransform foamZone)
        {
            m_Tap = root;
            m_TapPrompt = prompt;
            m_Glass = glass;
            m_Liquid = liquid;
            m_Foam = foam;
            m_Line = line;
            m_FoamZone = foamZone;
        }

        public void ConfigureChop(GameObject root, RectTransform board, Image food, RectTransform[] lines, Image[] cuts, RectTransform knife, RectTransform timer,
            LocalizedSuperText title, LocalizedSuperText progress, LocalizedSuperText prompt)
        {
            m_Chop = root;
            m_Board = board;
            m_Food = food;
            m_Lines = lines;
            m_Cuts = cuts;
            m_Knife = knife;
            m_Timer = timer;
            m_ChopTitle = title;
            m_ChopProgress = progress;
            m_ChopPrompt = prompt;
        }

        public void ConfigureStepAway(LocalizedSuperText stepAway) => m_StepAway = stepAway;
        public void ConfigureFlash(Image flash) => m_Flash = flash;

        /// <summary>The flash showing now (tests).</summary>
        public bool IsFlashing => m_FlashLeft > 0f;

        void Awake() => Show(null);

        void Start()
        {
            m_Feedback = FindAnyObjectByType<TavernFeedback>();
            if (m_Feedback != null) m_Feedback.MomentPlayed += OnMoment;
            if (m_Flash != null) m_Flash.enabled = false;
        }

        void OnDestroy()
        {
            if (m_Feedback != null) m_Feedback.MomentPlayed -= OnMoment;
        }

        // What the result looked like, as well as felt and heard; skipped when the player turns flashes off.
        void OnMoment(string moment)
        {
            if (m_Flash == null || !GameSettings.FlashEnabled || Showing == null) return;
            switch (moment)
            {
                case nameof(TavernMoments.perfectFlip):
                case nameof(TavernMoments.cleanPour):
                case nameof(TavernMoments.lineReached):
                case nameof(TavernMoments.chopDone):
                case nameof(TavernMoments.butcherClean):
                case nameof(TavernMoments.butcherDone):
                    m_FlashColour = m_GoodFlash;
                    break;
                case nameof(TavernMoments.burned):
                case nameof(TavernMoments.overflow):
                    m_FlashColour = m_BadFlash;
                    break;
                default:
                    return;
            }
            m_FlashLeft = m_FlashSeconds;
        }

        void UpdateFlash()
        {
            if (m_Flash == null) return;
            m_FlashLeft = Mathf.Max(0f, m_FlashLeft - Time.unscaledDeltaTime);
            m_Flash.enabled = m_FlashLeft > 0f;
            if (m_Flash.enabled) m_Flash.color = new Color(m_FlashColour.r, m_FlashColour.g, m_FlashColour.b, m_FlashColour.a * m_FlashLeft / m_FlashSeconds);
        }

        void LateUpdate()
        {
            IMinigame game = KeeperWork.Instance != null ? KeeperWork.Instance.ActiveCook : null;
            if (game != Showing) Show(game);
            UpdateFlash();
            switch (game)
            {
                case GrillMinigame grill: DrawGrill(grill); break;
                case TapMinigame tap: DrawTap(tap); break;
                case ChopMinigame chop: DrawChop(chop); break;
                case ButcherMinigame butcher: DrawButcher(butcher); break;
            }
        }

        void Show(IMinigame game)
        {
            Showing = game;
            if (m_Grill != null) m_Grill.SetActive(game is GrillMinigame);
            if (m_Tap != null) m_Tap.SetActive(game is TapMinigame);
            if (m_Chop != null) m_Chop.SetActive(game is ChopMinigame);
            if (m_Butcher != null) m_Butcher.SetActive(game is ButcherMinigame);
            m_LastButcherTitle = null;
            if (m_StepAway != null) m_StepAway.gameObject.SetActive(game != null);
            m_LastGrillSide = m_LastChopTitle = m_LastChopProgress = null;
            if (game != null && !m_PromptsSet) SetPrompts();
        }

        void SetPrompts()
        {
            m_PromptsSet = true;
            string action = InputHints.Binding(InputMaps.Minigame, MinigameActions.Action);
            string aim = InputHints.Binding(InputMaps.Minigame, MinigameActions.Aim);
            m_GrillPrompt?.Set(TavernLocKeys.GrillPrompt, action);
            m_TapPrompt?.Set(TavernLocKeys.TapPrompt, action, aim);
            m_ChopPrompt?.Set(TavernLocKeys.ChopPrompt, aim, action);
            m_ButcherPrompt?.Set(TavernLocKeys.ButcherPrompt, action);
            m_StepAway?.Set(TavernLocKeys.HintStepAway, InputHints.Binding(InputMaps.Minigame, MinigameActions.Cancel));
        }

        static void SpanX(RectTransform rect, float from, float to)
        {
            rect.anchorMin = new Vector2(Mathf.Clamp01(from), rect.anchorMin.y);
            rect.anchorMax = new Vector2(Mathf.Clamp01(to), rect.anchorMax.y);
            rect.offsetMin = new Vector2(0f, rect.offsetMin.y);
            rect.offsetMax = new Vector2(0f, rect.offsetMax.y);
        }

        static void SpanY(RectTransform rect, float from, float to)
        {
            rect.anchorMin = new Vector2(rect.anchorMin.x, Mathf.Clamp01(from));
            rect.anchorMax = new Vector2(rect.anchorMax.x, Mathf.Clamp01(to));
            rect.offsetMin = new Vector2(rect.offsetMin.x, 0f);
            rect.offsetMax = new Vector2(rect.offsetMax.x, 0f);
        }

        static void AtX(RectTransform rect, float x)
        {
            rect.anchorMin = new Vector2(Mathf.Clamp01(x), rect.anchorMin.y);
            rect.anchorMax = new Vector2(Mathf.Clamp01(x), rect.anchorMax.y);
            rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
        }

        void DrawGrill(GrillMinigame g)
        {
            GrillSettings s = g.Settings;
            string side = $"{Mathf.Min(g.Side + 1, g.SideCount)}/{g.SideCount}";
            if (side != m_LastGrillSide)
            {
                m_LastGrillSide = side;
                m_GrillSide?.Set(TavernLocKeys.GrillSide, Mathf.Min(g.Side + 1, g.SideCount), g.SideCount);
            }
            SpanX(m_GrillBand, s.bandMin, s.bandMax);
            AtX(m_GrillNeedle, g.IsPausing ? 0f : g.Meter);
            // Past the band the needle reddens with the same warning the controller rumbles.
            if (m_GrillNeedle.TryGetComponent(out Image needle))
                needle.color = Color.Lerp(Color.white, m_HotNeedle, m_Feedback != null ? m_Feedback.GrillWarning : 0f);
        }

        void DrawTap(TapMinigame t)
        {
            TapSettings s = t.Settings;
            SpanY(m_Liquid, 0f, t.Liquid);
            SpanY(m_Foam, t.Liquid, t.Liquid + t.Foam);
            SpanY(m_Line, s.fillLine, s.fillLine);
            // Where the beer/foam boundary should land for a head inside the foam band.
            SpanY(m_FoamZone, s.fillLine * (1f - s.foamBandMax), s.fillLine * (1f - s.foamBandMin));
            m_Glass.localRotation = Quaternion.Euler(0f, 0f, (0.5f - t.Tilt) * 50f);
        }

        void DrawChop(ChopMinigame c)
        {
            ChopSettings s = c.Settings;
            // The board spans the same share of the screen the pointer is mapped onto (KeeperWork).
            SpanX(m_Board, s.boardLeft, s.boardLeft + s.boardWidth);

            var pot = TavernDirector.Instance != null && TavernDirector.Instance.Session != null ? TavernDirector.Instance.Session.Pot : null;
            IngredientItem item = pot != null && c.Item < pot.ChopItems.Count ? pot.ChopItems[c.Item] : default;
            string title = item.IsValid ? Loc.Get(item.Definition.displayName) : string.Empty;
            if (title != m_LastChopTitle)
            {
                m_LastChopTitle = title;
                m_ChopTitle?.Set(TavernLocKeys.ChopTitle, title);
                if (m_Food != null)
                {
                    m_Food.sprite = item.IsValid ? item.Definition.icon : null;
                    m_Food.enabled = m_Food.sprite != null;
                }
            }
            string progress = $"{c.Item + 1}/{c.ItemCount}";
            if (progress != m_LastChopProgress)
            {
                m_LastChopProgress = progress;
                m_ChopProgress?.Set(TavernLocKeys.ChopProgress, c.Item + 1, c.ItemCount);
            }

            float[] lines = c.Item < c.ItemCount ? c.LinesOf(c.Item) : System.Array.Empty<float>();
            for (int i = 0; i < m_Lines.Length; i++)
            {
                bool shown = i < lines.Length;
                m_Lines[i].gameObject.SetActive(shown);
                if (shown) AtX(m_Lines[i], lines[i]);
                bool cut = shown && c.IsCut(c.Item, i);
                m_Cuts[i].gameObject.SetActive(cut);
                if (!cut) continue;
                AtX((RectTransform)m_Cuts[i].transform, c.CutPosition(c.Item, i));
                m_Cuts[i].color = Color.Lerp(k_CutBad, k_CutGood, c.CutScore(c.Item, i));
            }
            AtX(m_Knife, c.Knife);
            SpanX(m_Timer, 0f, Mathf.Clamp01(c.ItemTimeLeft / Mathf.Max(0.01f, s.itemTimeLimit)));
        }
    }
}
