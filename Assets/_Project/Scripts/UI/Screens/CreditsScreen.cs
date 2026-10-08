using System;
using Hearthdelve.Core.Input;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hearthdelve.UI.Screens
{
    /// <summary>Opens web links (the credits' HeatleyBros link). Tests replace <see cref="Opener"/> to see what would open.</summary>
    public static class UrlService
    {
        public static Action<string> Opener = Application.OpenURL;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Opener = Application.OpenURL;

        public static void Open(string url)
        {
            if (!string.IsNullOrEmpty(url)) Opener?.Invoke(url);
        }
    }

    /// <summary>
    /// The credits (4i-C), from the main menu and the pause menu: the Credits table's lines on a parchment panel, drifting up slowly
    /// on their own and scrolled by hand (up / down, the stick, the mouse wheel), with HeatleyBros' link as the one selectable line
    /// (its licence requires a working link in-game). Esc / B goes back; the owner calls <see cref="HandleInput"/> while it's open.
    /// </summary>
    public sealed class CreditsScreen : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] RectTransform m_Viewport;
        [SerializeField] RectTransform m_Content;
        [SerializeField] Button m_Link;
        [SerializeField] LocalizedSuperText m_Footer;
        [SerializeField, Tooltip("Pixels a second the credits drift up on their own.")]
        float m_Drift = 8f;
        [SerializeField, Tooltip("Pixels a second they move while scrolled by hand.")]
        float m_ScrollSpeed = 90f;
        [SerializeField, Tooltip("Seconds before the drift starts, and after a hand scroll before it resumes.")]
        float m_DriftDelay = 1.5f;

        float m_Idle;

        public bool IsOpen => m_Root != null && m_Root.activeSelf;
        public Button LinkButton => m_Link;
        public float Scroll => m_Content != null ? m_Content.anchoredPosition.y : 0f;
        public float MaxScroll => m_Content == null || m_Viewport == null ? 0f : Mathf.Max(0f, m_Content.rect.height - m_Viewport.rect.height);

        public void Configure(GameObject root, RectTransform viewport, RectTransform content, Button link, LocalizedSuperText footer)
        {
            m_Footer = footer;
            m_Root = root;
            m_Viewport = viewport;
            m_Content = content;
            m_Link = link;
        }

        void Awake()
        {
            if (m_Link != null) m_Link.onClick.AddListener(OpenLink);
        }

        void OnEnable()
        {
            InputDevices.Changed += ShowFooter;
            ShowFooter(InputDevices.Current);
        }

        void OnDisable() => InputDevices.Changed -= ShowFooter;

        /// <summary>The controls for the device in use (the 4i-A prompt rule).</summary>
        void ShowFooter(InputDeviceKind kind)
        {
            if (m_Footer != null) m_Footer.Set(kind == InputDeviceKind.Gamepad ? CreditsLocKeys.BackPad : CreditsLocKeys.Back);
        }

        public void OpenLink()
        {
            UrlService.Open(CreditsLocKeys.HeatleyBrosUrl);
            UiFeedback.Play(UiMoment.Confirm);
        }

        public void Open()
        {
            if (m_Root == null) return;
            m_Root.SetActive(true);
            SetScroll(0f);
            m_Idle = 0f;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(m_Link != null ? m_Link.gameObject : null);
        }

        public void Close()
        {
            if (m_Root != null) m_Root.SetActive(false);
        }

        /// <summary>Moves the list by <paramref name="pixels"/> (positive: further down the credits), kept in range.</summary>
        public void ScrollBy(float pixels)
        {
            SetScroll(Scroll + pixels);
            m_Idle = 0f;
        }

        void SetScroll(float y)
        {
            if (m_Content == null) return;
            // Whole pixels, so Silver stays crisp while it moves.
            m_Content.anchoredPosition = new Vector2(m_Content.anchoredPosition.x, Mathf.Round(Mathf.Clamp(y, 0f, MaxScroll)));
        }

        float m_Exact;

        /// <summary>Esc / B closes (returns true); up / down, the stick and the wheel scroll; otherwise it drifts.</summary>
        public bool HandleInput()
        {
            if (!IsOpen) return false;
            InputAction cancel = InputMaps.Find(InputMaps.UI, UIActions.Cancel);
            if (cancel != null && cancel.WasPressedThisFrame())
            {
                Close();
                return true;
            }
            float dt = Time.unscaledDeltaTime, hand = 0f;
            Keyboard keys = Keyboard.current;
            Gamepad pad = Gamepad.current;
            if (keys != null)
            {
                if (keys.downArrowKey.isPressed || keys.sKey.isPressed) hand += 1f;
                if (keys.upArrowKey.isPressed || keys.wKey.isPressed) hand -= 1f;
            }
            if (pad != null)
            {
                float stick = -pad.leftStick.ReadValue().y - pad.rightStick.ReadValue().y;
                if (Mathf.Abs(stick) > 0.3f) hand += Mathf.Clamp(stick, -1f, 1f);
                if (pad.dpad.down.isPressed) hand += 1f;
                if (pad.dpad.up.isPressed) hand -= 1f;
            }
            float wheel = Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0f;
            if (Mathf.Abs(wheel) > 0.01f) ScrollBy(-Mathf.Sign(wheel) * 12f);
            if (Mathf.Abs(hand) > 0.01f)
            {
                if (Mathf.Abs(m_Exact - Scroll) >= 1f) m_Exact = Scroll;
                m_Exact = Mathf.Clamp(m_Exact + hand * m_ScrollSpeed * dt, 0f, MaxScroll);
                SetScroll(m_Exact);
                m_Idle = 0f;
                return false;
            }
            m_Idle += dt;
            if (m_Idle >= m_DriftDelay && Scroll < MaxScroll)
            {
                if (Mathf.Abs(m_Exact - Scroll) >= 1f) m_Exact = Scroll;
                m_Exact += m_Drift * dt;
                SetScroll(m_Exact);
            }
            // Keep the link selected, so A / Enter opens it (the stick scrolls rather than moving the selection).
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null && m_Link != null)
                EventSystem.current.SetSelectedGameObject(m_Link.gameObject);
            return false;
        }
    }
}
