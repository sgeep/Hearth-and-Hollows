using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.Core.Input
{
    /// <summary>The kind of device the player is using, for button prompts and the controls reference (4i-A).</summary>
    public enum InputDeviceKind
    {
        KeyboardMouse,
        Gamepad,
    }

    /// <summary>
    /// Which device the prompts should name (4i-A, A4; pure, EditMode-tested). Only a **meaningful** input moves them: a key or
    /// button pressed, or a stick or trigger pushed past a clear threshold. The mouse merely moving or a stick resting off-centre
    /// never does, so the prompts don't flicker between "E" and "A" while the other device sits idle.
    /// </summary>
    public static class InputDeviceRules
    {
        /// <summary>How far a stick or trigger must be pushed to count (well past drift).</summary>
        public const float AnalogThreshold = 0.5f;

        /// <summary>The device to show after an input from <paramref name="source"/>.</summary>
        /// <param name="current">The device shown now.</param>
        /// <param name="source">Where the input came from.</param>
        /// <param name="isButton">A key or button (any press counts).</param>
        /// <param name="magnitude">For a stick, trigger or scroll: how far.</param>
        /// <param name="isPointerMotion">The mouse or a touch merely moving.</param>
        public static InputDeviceKind After(InputDeviceKind current, InputDeviceKind source, bool isButton, float magnitude, bool isPointerMotion)
        {
            if (source == current || isPointerMotion) return current;
            return isButton || magnitude >= AnalogThreshold ? source : current;
        }

        /// <summary>The Input System control scheme (binding group) for a device kind.</summary>
        public static string Group(InputDeviceKind kind) => kind == InputDeviceKind.Gamepad ? "Gamepad" : "Keyboard&Mouse";
    }

    /// <summary>
    /// The device last used meaningfully (4i-A): one source for every prompt in the game. Listens to the Input System's actions as
    /// they're performed, applies <see cref="InputDeviceRules"/>, and announces a change.
    /// </summary>
    public static class InputDevices
    {
        static InputDeviceKind s_Current = InputDeviceKind.KeyboardMouse;
        static bool s_Listening;

        /// <summary>The device the prompts name.</summary>
        public static InputDeviceKind Current => s_Current;

        /// <summary>The binding group for <see cref="Current"/>.</summary>
        public static string Group => InputDeviceRules.Group(s_Current);

        /// <summary>The prompts' device changed.</summary>
        public static event Action<InputDeviceKind> Changed;

        /// <summary>Sets the device (tests, and a scene that knows better).</summary>
        public static void Set(InputDeviceKind kind)
        {
            if (kind == s_Current) return;
            s_Current = kind;
            Changed?.Invoke(kind);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Current = InputDeviceKind.KeyboardMouse;
            Changed = null;
            if (s_Listening) InputSystem.onActionChange -= OnActionChange;
            s_Listening = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Listen()
        {
            if (s_Listening) return;
            InputSystem.onActionChange += OnActionChange;
            s_Listening = true;
            // A pad that's the only device at launch is the one in use.
            if (Gamepad.current != null && Keyboard.current == null) s_Current = InputDeviceKind.Gamepad;
        }

        static void OnActionChange(object action, InputActionChange change)
        {
            if (change != InputActionChange.ActionPerformed || action is not InputAction input) return;
            InputControl control = input.activeControl;
            if (control == null) return;
            InputDeviceKind source = control.device is Gamepad ? InputDeviceKind.Gamepad : InputDeviceKind.KeyboardMouse;
            bool pointer = control.device is Pointer && control is not UnityEngine.InputSystem.Controls.ButtonControl &&
                           !control.name.Contains("scroll");
            bool button = control is UnityEngine.InputSystem.Controls.ButtonControl;
            float magnitude = 0f;
            if (!button)
            {
                object value = control.ReadValueAsObject();
                magnitude = value switch
                {
                    float f => Mathf.Abs(f),
                    Vector2 v => v.magnitude,
                    _ => 0f,
                };
                if (control.device is Mouse && control.name.Contains("scroll")) magnitude = magnitude > 0f ? 1f : 0f;
            }
            Set(InputDeviceRules.After(s_Current, source, button, magnitude, pointer));
        }
    }
}
