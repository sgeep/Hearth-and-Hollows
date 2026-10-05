using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.Core.Input
{
    /// <summary>
    /// Reads a direction action (a stick, d-pad, arrows or WASD) for menus and cursors that step a cell or a row at a
    /// time: the held direction, or, for a tap so short it went down and up within one frame (a quick d-pad flick, or a
    /// browser delivering both key events together on the web build), that tap's direction for one read. Reading the
    /// value alone loses such taps (Checkpoint B web test).
    /// </summary>
    public sealed class DirectionReader
    {
        readonly InputAction m_Action;
        Vector2 m_Tap;

        public DirectionReader(InputAction action)
        {
            m_Action = action;
            if (m_Action != null) m_Action.performed += OnPerformed;
        }

        void OnPerformed(InputAction.CallbackContext context)
        {
            Vector2 value = context.ReadValue<Vector2>();
            if (value.sqrMagnitude > 0.25f) m_Tap = value;
        }

        /// <summary>The direction now: what's held, or a tap since the last read.</summary>
        public Vector2 Read()
        {
            if (m_Action == null) return Vector2.zero;
            Vector2 held = m_Action.ReadValue<Vector2>();
            Vector2 tap = m_Tap;
            m_Tap = Vector2.zero;
            return held.sqrMagnitude > 0.25f ? held : tap;
        }

        /// <summary>Forgets a tap (on opening a panel, so the key that led there isn't also a move inside it).</summary>
        public void Clear() => m_Tap = Vector2.zero;

        public void Dispose()
        {
            if (m_Action != null) m_Action.performed -= OnPerformed;
        }
    }
}
