using System;
using System.Collections.Generic;
using Hearthdelve.Core.Input;
using Hearthdelve.Core.Movement;
using Hearthdelve.Shared.Animation;
using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Customization;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Story;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// The new keeper (4g Checkpoint B): a name, one of the bodies the art fully supports, and Minifantasy colourways for its skin,
    /// hair and clothes, with a live preview. Controller first: up and down choose a row, left and right change it, A on the name
    /// opens the letters (or just type on a keyboard); the arrows and keys answer the mouse too. Begin starts the new game.
    /// </summary>
    public sealed class CharacterCreatorScreen : MonoBehaviour
    {
        /// <summary>A row of the creator: what it changes, its button (for focus), its value text and its two arrows.</summary>
        [Serializable]
        public sealed class Row
        {
            public string kind;
            public Button button;
            public LocalizedSuperText value;
            public Button left;
            public Button right;
        }

        public const string NameRow = "name";
        public const string BodyRow = "body";
        public const string SkinRow = "skin";
        public const string HairRow = "keeper_hair";
        public const string OutfitRow = "keeper_outfit";

        [SerializeField] GameObject m_Root;
        [SerializeField] Image m_Preview;
        [SerializeField] Row[] m_Rows = Array.Empty<Row>();
        [SerializeField] Button m_Begin;
        [SerializeField] Button m_Back;
        [Header("Name")]
        [SerializeField] GameObject m_NamePanel;
        [SerializeField] LocalizedSuperText m_NameField;
        [SerializeField] Button[] m_Keys = Array.Empty<Button>();
        [SerializeField] string[] m_KeyValues = Array.Empty<string>();
        [SerializeField] LocalizedSuperText[] m_KeyLabels = Array.Empty<LocalizedSuperText>();
        [SerializeField, Min(0.1f), Tooltip("The preview turns to its next facing this often (seconds).")] float m_TurnSeconds = 1.6f;

        public const string KeyShift = "<shift>", KeySpace = "<space>", KeyDelete = "<delete>", KeyDone = "<done>";

        GameFlow m_Flow;
        KeeperLooks m_Looks;
        PlayerProfile m_Profile = new();
        /// <summary>The parts the player has chosen a colour for: kept across a change of body (the rest show the new body as drawn).</summary>
        readonly System.Collections.Generic.HashSet<string> m_Chosen = new();
        bool m_Caps = true;
        string m_Typed = string.Empty;
        Action m_OnBack;
        float m_Opened;

        public bool IsOpen => m_Root != null && m_Root.activeSelf;
        public bool IsNaming => m_NamePanel != null && m_NamePanel.activeSelf;
        public PlayerProfile Profile => m_Profile;
        public Button BeginButton => m_Begin;
        public Button BackButton => m_Back;
        public Row RowFor(string kind) => Array.Find(m_Rows, r => r != null && r.kind == kind);
        public string ValueText(string kind) => RowFor(kind)?.value != null ? RowFor(kind).value.GetComponent<SuperTextMesh>().text : null;
        public Sprite PreviewSprite => m_Preview != null ? m_Preview.sprite : null;
        public string TypedName => m_Typed;

        public void Configure(GameObject root, Image preview, Row[] rows, Button begin, Button back, GameObject namePanel, LocalizedSuperText nameField,
            Button[] keys, string[] keyValues, LocalizedSuperText[] keyLabels)
        {
            m_KeyLabels = keyLabels;
            m_Root = root;
            m_Preview = preview;
            m_Rows = rows;
            m_Begin = begin;
            m_Back = back;
            m_NamePanel = namePanel;
            m_NameField = nameField;
            m_Keys = keys;
            m_KeyValues = keyValues;
        }

        void Awake()
        {
            foreach (Row row in m_Rows)
            {
                if (row == null) continue;
                string kind = row.kind;
                if (kind == NameRow) row.button.onClick.AddListener(OpenName);
                if (row.left != null) row.left.onClick.AddListener(() => Change(kind, -1));
                if (row.right != null) row.right.onClick.AddListener(() => Change(kind, +1));
            }
            m_Begin.onClick.AddListener(Begin);
            m_Back.onClick.AddListener(Back);
            for (int i = 0; i < m_Keys.Length; i++)
            {
                string value = m_KeyValues[i];
                m_Keys[i].onClick.AddListener(() => Press(value));
            }
            if (m_Root != null) m_Root.SetActive(false);
            if (m_NamePanel != null) m_NamePanel.SetActive(false);
        }

        Keyboard m_Keyboard;
        float m_LastX;

        /// <summary>Typing reaches the name only while the creator is open (from the keyboard in use when it opened).</summary>
        void Listen(bool on)
        {
            if (m_Keyboard != null) m_Keyboard.onTextInput -= OnText;
            m_Keyboard = on ? Keyboard.current : null;
            if (m_Keyboard != null) m_Keyboard.onTextInput += OnText;
        }

        void OnDisable() => Listen(false);

        /// <summary>Opens the creator on a fresh keeper (Bram, the townsfolk, as drawn).</summary>
        public void Open(GameFlow flow, Action onBack)
        {
            m_Flow = flow;
            m_Looks = flow != null && flow.Database != null ? flow.Database.keeperLooks : null;
            m_OnBack = onBack;
            m_Profile = new PlayerProfile();
            m_Chosen.Clear();
            KeeperBody body = m_Looks != null ? m_Looks.Body(m_Profile.body) : null;
            if (body != null) m_Profile.body = body.id;
            m_Profile.palette = Defaults(body);
            m_Root.SetActive(true);
            m_NamePanel.SetActive(false);
            m_Opened = Time.unscaledTime;
            m_LastX = 0f;
            Listen(true);
            Refresh();
            Select(m_Rows.Length > 0 ? m_Rows[0].button : m_Begin);
        }

        /// <summary>The ramps that are the body's drawing as it is, by kind (shown as its choices, so the rows name a colour).</summary>
        static string Defaults(KeeperBody body)
        {
            if (body == null) return string.Empty;
            var picks = new Dictionary<string, string>();
            foreach (PalettePick pick in body.drawnAs)
                if (pick != null && !string.IsNullOrEmpty(pick.kind)) picks[pick.kind] = pick.ramp;
            return FurniturePalette.Format(picks);
        }

        public void Close()
        {
            Listen(false);
            if (m_NamePanel != null) m_NamePanel.SetActive(false);
            if (m_Root != null) m_Root.SetActive(false);
        }

        void Begin()
        {
            if (m_Flow == null || m_Flow.IsLoading) return;
            m_Profile.name = KeeperRules.CleanName(m_Profile.name);
            Close();
            m_Flow.NewGame(m_Profile);
        }

        void Back()
        {
            Close();
            m_OnBack?.Invoke();
        }

        // ---------- Rows ----------

        string SkinKind(KeeperBody body)
        {
            if (body == null) return null;
            foreach (PaletteChannel c in body.channels)
                if (c != null && c.kind.EndsWith("skin", StringComparison.Ordinal)) return c.kind;
            return null;
        }

        /// <summary>Steps a row's choice (left -1, right +1).</summary>
        public void Change(string kind, int step)
        {
            if (m_Looks == null || kind == NameRow) return;
            KeeperBody body = m_Looks.Body(m_Profile.body);
            if (kind == BodyRow)
            {
                int index = Math.Max(0, m_Looks.bodies.IndexOf(body));
                KeeperBody next = m_Looks.bodies[KeeperRules.Step(index, step, m_Looks.bodies.Count)];
                Dictionary<string, string> old = FurniturePalette.Parse(m_Profile.palette);
                m_Profile.body = next.id;
                // A colour the player chose carries over where the new body has that part; everything else is the body as drawn.
                var picks = FurniturePalette.Parse(Defaults(next));
                foreach (string chosen in m_Chosen)
                    if (picks.ContainsKey(chosen) && old.TryGetValue(chosen, out string ramp)) picks[chosen] = ramp;
                m_Profile.palette = FurniturePalette.Format(picks);
            }
            else
            {
                string channel = kind == SkinRow ? SkinKind(body) : kind;
                if (channel == null || m_Looks.palettes == null) return;
                List<PaletteRamp> ramps = m_Looks.palettes.For(channel);
                if (ramps.Count == 0) return;
                int current = Math.Max(0, ramps.FindIndex(r => r.id == Pick(channel)));
                m_Profile.palette = FurniturePalette.With(m_Profile.palette, channel, ramps[KeeperRules.Step(current, step, ramps.Count)].id);
                m_Chosen.Add(channel);
            }
            UiFeedback.Play(UiMoment.Confirm);
            Refresh();
        }

        string Pick(string kind) =>
            kind != null && FurniturePalette.Parse(m_Profile.palette).TryGetValue(kind, out string ramp) ? ramp : null;

        void Refresh()
        {
            KeeperBody body = m_Looks != null ? m_Looks.Body(m_Profile.body) : null;
            foreach (Row row in m_Rows)
            {
                if (row?.value == null) continue;
                switch (row.kind)
                {
                    case NameRow:
                        row.value.Set(TavernLocKeys.Plain, m_Profile.name);
                        break;
                    case BodyRow:
                        row.value.Set(body != null ? body.nameKey : TavernLocKeys.Plain, string.Empty);
                        break;
                    default:
                        string channel = row.kind == SkinRow ? SkinKind(body) : row.kind;
                        PaletteRamp ramp = m_Looks != null && m_Looks.palettes != null ? m_Looks.palettes.Ramp(Pick(channel)) : null;
                        row.value.Set(ramp != null ? ramp.nameKey : TavernLocKeys.Plain, string.Empty);
                        break;
                }
            }
            m_Set = KeeperLook.Build(m_Looks, m_Profile, out _, CharacterAnim.Walk);
        }

        // ---------- The preview ----------

        SpriteAnimationSet m_Set;
        static readonly Facing4[] k_Turn = { Facing4.FrontRight, Facing4.FrontLeft, Facing4.BackLeft, Facing4.BackRight };

        void Update()
        {
            if (!IsOpen) return;
            AnimatePreview();
            if (IsNaming) UpdateNaming();
            else UpdateRows();
        }

        void AnimatePreview()
        {
            if (m_Preview == null || m_Set == null) return;
            float t = Time.unscaledTime - m_Opened;
            SpriteAnim walk = m_Set.Find(CharacterAnim.Walk) ?? m_Set.Find(CharacterAnim.Idle);
            if (walk == null) return;
            Facing4 facing = k_Turn[Mathf.FloorToInt(t / m_TurnSeconds) % k_Turn.Length];
            Sprite[] frames = walk.For(facing, out bool mirrored);
            if (frames == null || frames.Length == 0) return;
            m_Preview.sprite = frames[Mathf.FloorToInt(t / walk.frameDuration) % frames.Length];
            Vector3 scale = m_Preview.rectTransform.localScale;
            scale.x = Mathf.Abs(scale.x) * (mirrored ? -1f : 1f);
            m_Preview.rectTransform.localScale = scale;
        }

        /// <summary>Left and right on a focused row change it (the stick, the d-pad, arrows or A/D).</summary>
        void UpdateRows()
        {
            InputAction navigate = InputMaps.Find(InputMaps.UI, "Navigate");
            if (navigate == null) return;
            // Navigate passes the stick and keys straight through: a change is a push past half way, once per push.
            float x = navigate.ReadValue<Vector2>().x, last = m_LastX;
            m_LastX = x;
            if (Mathf.Abs(x) < 0.5f || Mathf.Abs(last) >= 0.5f) return;
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            foreach (Row row in m_Rows)
                if (row != null && row.button != null && selected == row.button.gameObject && row.kind != NameRow)
                    Change(row.kind, x > 0f ? +1 : -1);
        }

        // ---------- The name ----------

        int m_NamingFrame = -1;

        void OpenName()
        {
            // The Enter (or A) that just closed the letters mustn't open them again.
            if (Time.frameCount <= m_NamingFrame + 1) return;
            m_NamingFrame = Time.frameCount;
            m_Typed = m_Profile.name;
            m_NamePanel.SetActive(true);
            ShowTyped();
            Select(m_Keys.Length > 0 ? m_Keys[0] : null);
        }

        void CloseName()
        {
            m_NamingFrame = Time.frameCount;
            m_Profile.name = KeeperRules.CleanName(m_Typed);
            m_NamePanel.SetActive(false);
            Refresh();
            Select(RowFor(NameRow)?.button);
        }

        void ShowTyped()
        {
            m_NameField.Set(TavernLocKeys.Plain, m_Typed + "_");
            // The letters show their case (shift, and lower case after a capital).
            for (int i = 0; i < m_KeyLabels.Length && i < m_KeyValues.Length; i++)
            {
                string value = m_KeyValues[i];
                if (m_KeyLabels[i] != null && value.Length > 0 && value[0] != '<') m_KeyLabels[i].Set(TavernLocKeys.Plain, Letter(value));
            }
        }

        /// <summary>One key of the letters (a letter, or shift, space, delete, done).</summary>
        public void Press(string key)
        {
            // On a keyboard, Enter closes the letters and Space is typed: neither also presses the focused letter (uGUI's Submit).
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                return;
            switch (key)
            {
                case KeyShift: m_Caps = !m_Caps; break;
                case KeySpace: Type(' '); break;
                case KeyDelete: if (m_Typed.Length > 0) m_Typed = m_Typed.Substring(0, m_Typed.Length - 1); break;
                case KeyDone: CloseName(); return;
                default:
                    string letter = Letter(key);
                    if (!string.IsNullOrEmpty(letter)) Type(letter[0]);
                    break;
            }
            ShowTyped();
        }

        /// <summary>A letter key's value is its two cases, "a|A" (written out, never converted): the one shift chooses.</summary>
        string Letter(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            int bar = key.IndexOf('|');
            if (bar < 0) return key;
            return m_Caps ? key.Substring(bar + 1) : key.Substring(0, bar);
        }

        void Type(char c)
        {
            if (m_Typed.Length >= KeeperRules.MaxNameLength || !KeeperRules.IsNameCharacter(c)) return;
            m_Typed += c;
            // After a capital, lower case (as a keyboard's shift).
            if (char.IsLetter(c)) m_Caps = false;
        }

        /// <summary>Typing on a keyboard: letters go in directly, and the letters' focus is dropped so the same keys can't press them too.</summary>
        void OnText(char c)
        {
            if (!IsNaming || char.IsControl(c)) return;
            if (!KeeperRules.IsNameCharacter(c)) return;
            if (m_Typed.Length < KeeperRules.MaxNameLength) m_Typed += c;
            ShowTyped();
            Select(null);
        }

        void UpdateNaming()
        {
            Keyboard keyboard = Keyboard.current;
            // The press that opened the letters doesn't also close them.
            if (keyboard != null && Time.frameCount > m_NamingFrame + 1)
            {
                if (keyboard.backspaceKey.wasPressedThisFrame) Press(KeyDelete);
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
                {
                    CloseName();
                    return;
                }
            }
            // A gamepad (or the arrows) brings the letters' focus back.
            bool pad = Gamepad.current != null && (Gamepad.current.dpad.ReadValue().sqrMagnitude > 0.25f || Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.25f);
            bool arrows = keyboard != null && (keyboard.upArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame ||
                                               keyboard.leftArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame);
            if ((pad || arrows) && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && m_Keys.Length > 0)
                Select(m_Keys[0]);
            InputAction cancel = InputMaps.Find(InputMaps.UI, UIActions.Cancel);
            if (Gamepad.current != null && cancel != null && cancel.WasPressedThisFrame() && Gamepad.current.buttonEast.wasPressedThisFrame) Press(KeyDelete);
        }

        static void Select(Selectable target)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target != null ? target.gameObject : null);
        }
    }
}
