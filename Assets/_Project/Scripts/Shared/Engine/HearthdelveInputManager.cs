using System;
using System.Collections.Generic;
using Hearthdelve.Core.Input;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.Shared.Engine
{
    /// <summary>
    /// Feeds TopDown Engine from our own action maps (CLAUDE.md: our maps stay the source of
    /// truth). The Dungeon or Tavern map is mapped onto TDE's movement and buttons; the
    /// Minigame and UI maps never go through TDE.
    /// </summary>
    public class HearthdelveInputManager : InputSystemManager
    {
        public enum GameplayMap { Dungeon, Tavern }

        [Header("Hearthdelve")]
        [Tooltip("Which of our action maps drives the TDE character in this scene.")]
        public GameplayMap Map = GameplayMap.Dungeon;

        [Tooltip("Ignores a released stick springing back past centre, so the character doesn't turn round as it stops.")]
        public StickReleaseSettings StickRelease = StickReleaseSettings.Default;

        readonly List<Action> m_Unbind = new();
        InputAction m_AimPoint;
        StickReleaseFilter m_StickRelease;
        Vector2 m_RawMovement;

        public string MapName => Map == GameplayMap.Dungeon ? InputMaps.Dungeon : InputMaps.Tavern;

        /// <summary>True while the mouse was used more recently than a gamepad (aim follows the pointer).</summary>
        public bool PointerAim { get; private set; } = true;

        public override Vector2 MousePosition
        {
            get
            {
                if (m_AimPoint != null && m_AimPoint.enabled) return m_AimPoint.ReadValue<Vector2>();
                return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            }
        }

        protected override void Awake()
        {
            if (CustomInputActionsAsset == null) CustomInputActionsAsset = InputSystem.actions;
            PlayerControlsActionMapName = MapName;
            base.Awake();
        }

        protected override void InitializeFromCustomAsset()
        {
            _playerControlsMap = _activeInputActionsAsset != null ? _activeInputActionsAsset.FindActionMap(MapName, false) : null;
            if (_playerControlsMap == null)
            {
                Debug.LogWarning($"{nameof(HearthdelveInputManager)} could not find the '{MapName}' action map.", this);
                return;
            }

            // Both maps name their movement action "Move".
            _primaryMovementAction = _playerControlsMap.FindAction(DungeonActions.Move, false);
            BindValue(_primaryMovementAction, context =>
            {
                m_RawMovement = context.ReadValue<Vector2>();
                _primaryMovementInput = FilteredMovement();
                NoteDevice(context);
            });

            if (Map == GameplayMap.Dungeon)
            {
                m_AimPoint = _playerControlsMap.FindAction(DungeonActions.AimPoint, false);
                BindButton(DungeonActions.Attack, ShootButton);
                BindButton(DungeonActions.Heavy, SecondaryShootButton);
                BindButton(DungeonActions.Dodge, DashButton);
                BindButton(DungeonActions.Interact, InteractButton);
                BindButton(DungeonActions.Pause, PauseButton);
            }
            else
            {
                BindButton(TavernActions.Interact, InteractButton);
                BindButton(TavernActions.Pause, PauseButton);
            }
        }

        void BindValue(InputAction action, Action<InputAction.CallbackContext> handler)
        {
            if (action == null) return;
            action.performed += handler;
            action.canceled += handler;
            m_Unbind.Add(() =>
            {
                action.performed -= handler;
                action.canceled -= handler;
            });
        }

        void BindButton(string actionName, MMInput.IMButton button)
        {
            InputAction action = _playerControlsMap.FindAction(actionName, false);
            if (action == null)
            {
                Debug.LogWarning($"{nameof(HearthdelveInputManager)} could not find '{MapName}/{actionName}'.", this);
                return;
            }

            void Down(InputAction.CallbackContext context)
            {
                NoteDevice(context);
                if (InputDetectionActive) button.State.ChangeState(MMInput.ButtonStates.ButtonDown);
            }

            void Up(InputAction.CallbackContext context)
            {
                if (InputDetectionActive) button.State.ChangeState(MMInput.ButtonStates.ButtonUp);
            }

            action.performed += Down;
            action.canceled += Up;
            m_Unbind.Add(() =>
            {
                action.performed -= Down;
                action.canceled -= Up;
            });
        }

        void NoteDevice(InputAction.CallbackContext context)
        {
            InputDevice device = context.control?.device;
            if (device is Gamepad) PointerAim = false;
            else if (device is Mouse || device is Keyboard) PointerAim = true;
        }

        Vector2 FilteredMovement()
        {
            m_StickRelease ??= new StickReleaseFilter(StickRelease);
            return m_StickRelease.Filter(m_RawMovement, Time.unscaledTime);
        }

        protected override void Update()
        {
            // Every frame, not only when the stick moves: a weak push held still passes once the window is over.
            _primaryMovementInput = FilteredMovement();
            base.Update();
            if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 4f) PointerAim = true;
        }

        // Our maps are switched as a group (exactly one gameplay map plus UI), so the manager
        // must not enable or disable the whole asset the way the base class does.
        protected override void OnEnable() => InputMaps.Activate(MapName);

        protected override void OnDisable()
        {
            m_RawMovement = Vector2.zero;
            _primaryMovementInput = Vector2.zero;
        }

        protected virtual void OnDestroy()
        {
            foreach (Action unbind in m_Unbind) unbind();
            m_Unbind.Clear();
        }
    }
}
