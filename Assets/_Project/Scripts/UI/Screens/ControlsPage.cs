using Hearthdelve.Core.Input;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// The controls reference (4i-A, A3): one page per part of the game (the day, the evening, the Hollows, decorating, menus), each a
    /// short table of what, keyboard and mouse, and controller. Both columns are always shown; the one for the device in use is
    /// lit. Left and right turn the page; Esc or B goes back. Opened from the main menu and the pause menu, which own it and
    /// call <see cref="HandleInput"/> while it's open.
    /// </summary>
    public sealed class ControlsPage : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] LocalizedSuperText m_Title;
        [SerializeField] LocalizedSuperText m_KeyboardHeader;
        [SerializeField] LocalizedSuperText m_GamepadHeader;
        [SerializeField] LocalizedSuperText[] m_Actions;
        [SerializeField] LocalizedSuperText[] m_Keyboard;
        [SerializeField] LocalizedSuperText[] m_Gamepad;

        // On the parchment: the panel titles' red for the device in use, the labels' brown, lighter, for the other.
        static readonly Color k_Lit = Hearthdelve.UI.Typography.UiPalette.Title, k_Dim = new(0.62f, 0.5f, 0.4f);

        int m_Page;
        float m_LastX;

        public bool IsOpen => m_Root != null && m_Root.activeSelf;
        public int Page => m_Page;
        public int PageCount => MenuLocKeys.Pages.Length;

        public void Configure(GameObject root, LocalizedSuperText title, LocalizedSuperText keyboardHeader, LocalizedSuperText gamepadHeader,
            LocalizedSuperText[] actions, LocalizedSuperText[] keyboard, LocalizedSuperText[] gamepad)
        {
            m_Root = root;
            m_Title = title;
            m_KeyboardHeader = keyboardHeader;
            m_GamepadHeader = gamepadHeader;
            m_Actions = actions;
            m_Keyboard = keyboard;
            m_Gamepad = gamepad;
        }

        // Built hidden; no Awake that hides it (on the page's own object, Awake runs when it's first shown).

        void OnEnable() => InputDevices.Changed += OnDevice;
        void OnDisable() => InputDevices.Changed -= OnDevice;

        void OnDevice(InputDeviceKind _) => Light();

        public void Open(int page = 0)
        {
            if (m_Root == null) return;
            m_Root.SetActive(true);
            m_LastX = 0f;
            Show(page);
        }

        public void Close()
        {
            if (m_Root != null) m_Root.SetActive(false);
        }

        public void Show(int page)
        {
            m_Page = (page % PageCount + PageCount) % PageCount;
            MenuLocKeys.Page p = MenuLocKeys.Pages[m_Page];
            if (m_Title != null) m_Title.Set(MenuLocKeys.ControlsPage, Loc.UI(p.Title), m_Page + 1, PageCount);
            for (int i = 0; i < m_Actions.Length; i++)
            {
                bool used = i < p.Rows.Length;
                m_Actions[i].gameObject.SetActive(used);
                m_Keyboard[i].gameObject.SetActive(used);
                m_Gamepad[i].gameObject.SetActive(used);
                if (!used) continue;
                m_Actions[i].Set(p.Rows[i].Action);
                m_Keyboard[i].Set(p.Rows[i].Keyboard);
                m_Gamepad[i].Set(p.Rows[i].Gamepad);
            }
            Light();
        }

        /// <summary>The column for the device in use is lit; the other is dimmed (both stay readable).</summary>
        void Light()
        {
            bool pad = InputDevices.Current == InputDeviceKind.Gamepad;
            Tint(m_KeyboardHeader, pad ? k_Dim : k_Lit);
            Tint(m_GamepadHeader, pad ? k_Lit : k_Dim);
        }

        static void Tint(LocalizedSuperText text, Color color)
        {
            if (text == null) return;
            var stm = text.GetComponent<SuperTextMesh>();
            if (stm == null || stm.color == color) return;
            stm.color = color;
            stm.Rebuild();
        }

        /// <summary>
        /// While open: left and right turn the page, Esc or B (the UI map's Cancel) goes back. Returns true when it closed this
        /// frame (the owner shows what was under it, and ignores Cancel itself this frame).
        /// </summary>
        public bool HandleInput()
        {
            if (!IsOpen) return false;
            InputAction cancel = InputMaps.Find(InputMaps.UI, UIActions.Cancel);
            if (cancel != null && cancel.WasPressedThisFrame())
            {
                Close();
                return true;
            }
            // A press turns the page at once (a quick tap counts); a stick pushed over turns it once per push.
            Keyboard keys = Keyboard.current;
            Gamepad pad = Gamepad.current;
            bool right = (keys != null && (keys.rightArrowKey.wasPressedThisFrame || keys.dKey.wasPressedThisFrame)) ||
                         (pad != null && (pad.dpad.right.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame));
            bool left = (keys != null && (keys.leftArrowKey.wasPressedThisFrame || keys.aKey.wasPressedThisFrame)) ||
                        (pad != null && (pad.dpad.left.wasPressedThisFrame || pad.leftShoulder.wasPressedThisFrame));
            float x = pad != null ? pad.leftStick.ReadValue().x : 0f;
            if (right) Show(m_Page + 1);
            else if (left) Show(m_Page - 1);
            else if (Mathf.Abs(x) > 0.5f && Mathf.Abs(m_LastX) <= 0.5f) Show(m_Page + (x > 0f ? 1 : -1));
            m_LastX = x;
            return false;
        }
    }
}
