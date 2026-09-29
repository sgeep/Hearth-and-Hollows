using Hearthdelve.Core.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.Dungeon.Player
{
    /// <summary>One physics step's worth of player input. Presses are latched from Update.</summary>
    public struct PlayerFrameInput
    {
        public Vector2 Move;
        public bool JumpPressed;
        public bool JumpHeld;
        public bool AttackPressed;
        public bool DodgePressed;
        public bool InteractPressed;
    }

    /// <summary>
    /// Reads the Dungeon action map of the project-wide actions asset. Button presses are
    /// latched every rendered frame and consumed once per FixedUpdate so none are dropped.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        InputAction m_Move, m_Jump, m_Attack, m_Dodge, m_Interact;
        PlayerFrameInput m_Pending;

        public bool Enabled { get; set; } = true;

        void OnEnable()
        {
            m_Move = InputMaps.Find(InputMaps.Dungeon, DungeonActions.Move);
            m_Jump = InputMaps.Find(InputMaps.Dungeon, DungeonActions.Jump);
            m_Attack = InputMaps.Find(InputMaps.Dungeon, DungeonActions.Attack);
            m_Dodge = InputMaps.Find(InputMaps.Dungeon, DungeonActions.Dodge);
            m_Interact = InputMaps.Find(InputMaps.Dungeon, DungeonActions.Interact);
            if (m_Move == null) Debug.LogError("Dungeon input actions not found. Run Hearthdelve/Setup/Configure Project.", this);
        }

        void Update()
        {
            if (!Enabled || m_Move == null) return;
            m_Pending.JumpPressed |= m_Jump.WasPressedThisFrame();
            m_Pending.AttackPressed |= m_Attack.WasPressedThisFrame();
            m_Pending.DodgePressed |= m_Dodge.WasPressedThisFrame();
            m_Pending.InteractPressed |= m_Interact.WasPressedThisFrame();
        }

        /// <summary>Returns this step's input and clears latched presses.</summary>
        public PlayerFrameInput Consume()
        {
            if (Override.HasValue) return Override.Value;
            if (!Enabled || m_Move == null)
            {
                m_Pending = default;
                return default;
            }
            var frame = m_Pending;
            frame.Move = m_Move.ReadValue<Vector2>();
            frame.JumpHeld = m_Jump.IsPressed();
            m_Pending = default;
            return frame;
        }

        /// <summary>When set, returned by every <see cref="Consume"/> instead of device input (tests, scripted sequences).</summary>
        public PlayerFrameInput? Override { get; set; }
    }
}
