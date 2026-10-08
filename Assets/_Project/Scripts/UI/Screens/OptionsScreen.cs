using System;
using System.Collections.Generic;
using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Haptics;
using Hearthdelve.Shared.Settings;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// Options (4i-B): five tabs (audio, feel, display, accessibility, controls), each a few lines changed with left and right.
    /// Every change is written through <see cref="GameOptions"/> at once (saved to the options file, never the game's save) and
    /// felt at once: a volume plays a sample sound through Effects, vibration a sample rumble. The controls tab is 4i-A's
    /// controls reference. Opened from the main menu and the pause menu, which call <see cref="HandleInput"/> while it's open.
    /// </summary>
    public sealed class OptionsScreen : MonoBehaviour
    {
        public enum Tab
        {
            Audio,
            Feel,
            Display,
            Accessibility,
            Controls,
        }

        [SerializeField] GameObject m_Root;
        [SerializeField] GameObject m_Panel;
        [SerializeField] Button[] m_Tabs;
        [SerializeField] OptionRow[] m_Rows;
        [SerializeField] LocalizedSuperText m_Note;
        [SerializeField] ControlsPage m_Controls;
        [SerializeField] string m_SampleRumble = "Tap.Firm";

        Tab m_Tab;
        readonly List<OptionRow> m_Shown = new();

        public bool IsOpen => m_Root != null && m_Root.activeSelf;
        public Tab Current => m_Tab;
        public IReadOnlyList<OptionRow> Rows => m_Shown;
        public ControlsPage Controls => m_Controls;

        /// <summary>The web build: the display tab offers fullscreen only (the browser owns the window).</summary>
        public static bool IsWeb => Application.platform == RuntimePlatform.WebGLPlayer;

        public void Configure(GameObject root, GameObject panel, Button[] tabs, OptionRow[] rows, LocalizedSuperText note, ControlsPage controls)
        {
            m_Root = root;
            m_Panel = panel;
            m_Tabs = tabs;
            m_Rows = rows;
            m_Note = note;
            m_Controls = controls;
        }

        void Awake()
        {
            for (int i = 0; i < m_Tabs.Length; i++)
            {
                Tab tab = (Tab)i;
                m_Tabs[i].onClick.AddListener(() => Show(tab));
            }
        }

        public void Open(Tab tab = Tab.Audio)
        {
            if (m_Root == null) return;
            m_Root.SetActive(true);
            Show(tab);
        }

        public void Close()
        {
            m_Controls?.Close();
            if (m_Root != null) m_Root.SetActive(false);
        }

        public void Show(Tab tab)
        {
            m_Tab = tab;
            if (tab == Tab.Controls)
            {
                if (m_Panel != null) m_Panel.SetActive(false);
                Select(null);
                m_Controls?.Open();
                return;
            }
            m_Controls?.Close();
            if (m_Panel != null) m_Panel.SetActive(true);
            for (int i = 0; i < m_Tabs.Length; i++) Light(m_Tabs[i], i == (int)tab);
            m_Shown.Clear();
            foreach (OptionRow row in m_Rows) row.gameObject.SetActive(false);
            m_Note?.Set(tab switch
            {
                Tab.Display => IsWeb ? MenuLocKeys.OptionsNoteWeb : MenuLocKeys.OptionsNoteDisplay,
                Tab.Accessibility => MenuLocKeys.OptionsNoteAccess,
                // 4i-C: the device in use only (the 4i-A prompt rule), which also keeps the note on one line.
                _ => InputDevices.Current == InputDeviceKind.Gamepad ? MenuLocKeys.OptionsNoteChangePad : MenuLocKeys.OptionsNoteChange,
            });
            switch (tab)
            {
                case Tab.Audio:
                    Add(MenuLocKeys.OptMaster, () => Percent(GameOptions.Current.masterVolume), d => Volume(o => o.masterVolume, (o, v) => o.masterVolume = v, d));
                    Add(MenuLocKeys.OptMusic, () => Percent(GameOptions.Current.musicVolume), d => Volume(o => o.musicVolume, (o, v) => o.musicVolume = v, d));
                    Add(MenuLocKeys.OptEffects, () => Percent(GameOptions.Current.effectsVolume), d => Volume(o => o.effectsVolume, (o, v) => o.effectsVolume = v, d));
                    break;
                case Tab.Feel:
                    Add(MenuLocKeys.OptShake, () => Loc.UI(ShakeKey(GameOptions.Current.shake)), d => GameOptions.Change(o => o.shake = (ShakeLevel)Cycle((int)o.shake, d, 3)));
                    Add(MenuLocKeys.OptFlashes, () => OnOff(GameOptions.Current.flashes), _ => GameOptions.Change(o => o.flashes = !o.flashes));
                    Add(MenuLocKeys.OptHitStop, () => OnOff(GameOptions.Current.hitStop), _ => GameOptions.Change(o => o.hitStop = !o.hitStop));
                    Add(MenuLocKeys.OptVibration, () => OnOff(GameOptions.Current.vibration), _ =>
                    {
                        GameOptions.Change(o => o.vibration = !o.vibration);
                        Rumble();
                    });
                    Add(MenuLocKeys.OptIntensity, () => Percent(GameOptions.Current.vibrationIntensity), d =>
                    {
                        GameOptions.Change(o => o.vibrationIntensity = Mathf.Max(0.1f, OptionsRules.StepVolume(o.vibrationIntensity, d)));
                        Rumble();
                    }, () => GameOptions.Current.vibration);
                    Add(MenuLocKeys.OptReduced, () => OnOff(GameOptions.Current.reducedVibration), _ =>
                    {
                        GameOptions.Change(o => o.reducedVibration = !o.reducedVibration);
                        Rumble();
                    }, () => GameOptions.Current.vibration);
                    break;
                case Tab.Display:
                    if (IsWeb) Add(MenuLocKeys.OptFullscreen, () => OnOff(WebFullscreenShown), _ => ToggleWebFullscreen());
                    else
                    {
                        Add(MenuLocKeys.OptFullscreen, () => OnOff(GameOptions.Current.fullscreen), _ => GameOptions.Change(o => o.fullscreen = !o.fullscreen, display: true));
                        Add(MenuLocKeys.OptWindow, WindowText, WindowStep, () => !GameOptions.Current.fullscreen);
                    }
                    break;
                case Tab.Accessibility:
                    Add(MenuLocKeys.OptTextSpeed, () => Loc.UI(SpeedKey(GameOptions.Current.textSpeed)), d => GameOptions.Change(o => o.textSpeed = (TextSpeed)Cycle((int)o.textSpeed, d, 3)));
                    Add(MenuLocKeys.OptRelaxed, () => OnOff(GameOptions.Current.relaxedTiming), _ => GameOptions.Change(o => o.relaxedTiming = !o.relaxedTiming));
                    Add(MenuLocKeys.OptPatient, () => OnOff(GameOptions.Current.patientCustomers), _ => GameOptions.Change(o => o.patientCustomers = !o.patientCustomers));
                    break;
            }
            Navigate();
            Select(m_Shown.Count > 0 ? m_Shown[0] : null);
        }

        void Add(string label, Func<string> show, Action<int> change, Func<bool> usable = null)
        {
            if (m_Shown.Count >= m_Rows.Length) return;
            OptionRow row = m_Rows[m_Shown.Count];
            row.gameObject.SetActive(true);
            row.Bind(label, show, change, usable);
            m_Shown.Add(row);
        }

        /// <summary>Up and down through the lines, wrapping; every line refreshes after any change (one can enable another).</summary>
        void Navigate()
        {
            for (int i = 0; i < m_Shown.Count; i++)
            {
                Navigation n = m_Shown[i].navigation;
                n.mode = Navigation.Mode.Explicit;
                n.selectOnUp = m_Shown[(i - 1 + m_Shown.Count) % m_Shown.Count];
                n.selectOnDown = m_Shown[(i + 1) % m_Shown.Count];
                n.selectOnLeft = n.selectOnRight = null;
                m_Shown[i].navigation = n;
            }
        }

        // ---------- the web's fullscreen (the owner's 4i-B note) ----------
        // The browser enters or leaves fullscreen a moment after it's asked, so right after the change Screen.fullScreen still
        // says the old thing. The line shows what was asked for until the browser catches up (or a couple of seconds pass, if it
        // refused), and follows the real state whenever it changes (Esc in the browser leaves fullscreen too).

        const float WebFullscreenGrace = 2f;
        bool m_WebFullscreenAsked;
        float m_WebFullscreenAskedAt = -10f;
        bool m_WebFullscreenSeen;

        bool WebFullscreenShown => Time.unscaledTime - m_WebFullscreenAskedAt < WebFullscreenGrace ? m_WebFullscreenAsked : Screen.fullScreen;

        void ToggleWebFullscreen()
        {
            m_WebFullscreenAsked = !WebFullscreenShown;
            m_WebFullscreenAskedAt = Time.unscaledTime;
            Screen.fullScreen = m_WebFullscreenAsked;
        }

        void Update()
        {
            if (!IsOpen || m_Tab != Tab.Display) return;
            bool now = Screen.fullScreen;
            if (now == m_WebFullscreenSeen && Time.unscaledTime - m_WebFullscreenAskedAt > WebFullscreenGrace + 0.1f) return;
            if (now == m_WebFullscreenAsked) m_WebFullscreenAskedAt = -10f;
            m_WebFullscreenSeen = now;
            RefreshAll();
        }

        void OnEnable()
        {
            GameOptions.Changed += RefreshAll;
            InputDevices.Changed += OnDeviceChanged;
        }

        void OnDisable()
        {
            GameOptions.Changed -= RefreshAll;
            InputDevices.Changed -= OnDeviceChanged;
        }

        /// <summary>The note follows the device; the selected line stays selected.</summary>
        void OnDeviceChanged(InputDeviceKind _)
        {
            if (!IsOpen || m_Tab == Tab.Controls || m_Note == null) return;
            if (m_Tab is Tab.Audio or Tab.Feel) m_Note.Set(InputDevices.Current == InputDeviceKind.Gamepad ? MenuLocKeys.OptionsNoteChangePad : MenuLocKeys.OptionsNoteChange);
        }

        void RefreshAll()
        {
            foreach (OptionRow row in m_Shown) row.Refresh();
        }

        static void Light(Button tab, bool on)
        {
            var image = tab.targetGraphic as Image;
            if (image != null) image.color = on ? new Color(1f, 0.86f, 0.45f) : Color.white;
        }

        static void Select(Selectable s)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(s != null ? s.gameObject : null);
        }

        // ---------- values ----------

        static string Percent(float v) => Loc.UI(MenuLocKeys.OptPercent, Mathf.RoundToInt(v * 100f));
        static string OnOff(bool on) => Loc.UI(on ? MenuLocKeys.OptOn : MenuLocKeys.OptOff);
        static int Cycle(int value, int direction, int count) => ((value + direction) % count + count) % count;

        static string ShakeKey(ShakeLevel s) => s switch
        {
            ShakeLevel.Off => MenuLocKeys.OptOff,
            ShakeLevel.Low => MenuLocKeys.OptLow,
            _ => MenuLocKeys.OptFull,
        };

        static string SpeedKey(TextSpeed s) => s switch
        {
            TextSpeed.Slow => MenuLocKeys.OptSlow,
            TextSpeed.Instant => MenuLocKeys.OptInstant,
            _ => MenuLocKeys.OptNormal,
        };

        /// <summary>A volume one step up or down, heard at once (a sample through Effects; the music is its own sample).</summary>
        static void Volume(Func<PlayerOptions, float> get, Action<PlayerOptions, float> set, int direction)
        {
            GameOptions.Change(o => set(o, OptionsRules.StepVolume(get(o), direction)));
            UiFeedback.Play(UiMoment.Confirm);
        }

        void Rumble() => HapticService.Play(m_SampleRumble);

        static string WindowText()
        {
            Resolution d = Screen.currentResolution;
            int k = OptionsRules.WindowScale(GameOptions.Current.windowScale, d.width, d.height);
            return Loc.UI(MenuLocKeys.OptWindowSize, 320 * k, 180 * k);
        }

        static void WindowStep(int direction)
        {
            Resolution d = Screen.currentResolution;
            List<int> scales = OptionsRules.WindowScales(d.width, d.height);
            int now = scales.IndexOf(OptionsRules.WindowScale(GameOptions.Current.windowScale, d.width, d.height));
            int next = scales[Mathf.Clamp(now + direction, 0, scales.Count - 1)];
            GameOptions.Change(o => o.windowScale = next, display: true);
        }

        // ---------- input (the owner calls this while it's open) ----------

        /// <summary>
        /// Q / E or LB / RB change the tab; Esc or B goes back (from the controls page, to Options; from Options, out). Returns true
        /// when Options closed this frame.
        /// </summary>
        public bool HandleInput()
        {
            if (!IsOpen) return false;
            if (m_Tab == Tab.Controls && m_Controls != null)
            {
                if (m_Controls.HandleInput()) Show(Tab.Audio);
                return false;
            }
            Keyboard keys = Keyboard.current;
            Gamepad pad = Gamepad.current;
            bool next = (keys != null && keys.eKey.wasPressedThisFrame) || (pad != null && pad.rightShoulder.wasPressedThisFrame);
            bool previous = (keys != null && keys.qKey.wasPressedThisFrame) || (pad != null && pad.leftShoulder.wasPressedThisFrame);
            if (next || previous)
            {
                Show((Tab)Cycle((int)m_Tab, next ? 1 : -1, Enum.GetValues(typeof(Tab)).Length));
                return false;
            }
            InputAction cancel = InputMaps.Find(InputMaps.UI, UIActions.Cancel);
            if (cancel != null && cancel.WasPressedThisFrame())
            {
                Close();
                return true;
            }
            return false;
        }
    }
}
