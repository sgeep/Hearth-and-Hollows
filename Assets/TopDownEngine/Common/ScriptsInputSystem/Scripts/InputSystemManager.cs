using System;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace MoreMountains.TopDownEngine
{
    /// <summary>
    /// This is a replacement InputManager if you prefer using Unity's InputSystem over the legacy one.
    /// Note that it's not the default solution in the engine at the moment, because older versions of Unity don't support it, 
    /// and most people still prefer not using it
    /// You can see an example of how to set it up in the MinimalScene3D_InputSystem demo scene
    /// </summary>
    public class InputSystemManager : InputManager
    {
        /// a set of input actions to use to read input on
        [NonSerialized]
        public TopDownEngineInputActions InputActions;
        /// the position of the mouse
        public override Vector2 MousePosition => Mouse.current.position.ReadValue();

        [Header("Optional Input Asset Override")]
        [Tooltip("If assigned, this asset is used instead of the built-in TopDownEngineInputActions generated asset.")]
        public InputActionAsset CustomInputActionsAsset;
        [Tooltip("Action map name to read controls from when using a custom asset.")]
        public string PlayerControlsActionMapName = "PlayerControls";

        protected Vector2 _primaryMovementInput;
        protected Vector2 _secondaryMovementInput;
        protected InputActionAsset _activeInputActionsAsset;
        protected InputActionMap _playerControlsMap;
        protected InputAction _primaryMovementAction;
        protected InputAction _secondaryMovementAction;
        protected InputAction _cameraRotationAction;

        protected override void Awake()
        {
            base.Awake();
            if (CustomInputActionsAsset != null)
            {
                _activeInputActionsAsset = CustomInputActionsAsset;
                InputActions = null;
            }
            else
            {
                InputActions = new TopDownEngineInputActions();
                _activeInputActionsAsset = InputActions.asset;
            }
        }
        
        /// <summary>
        /// On init we register to all our actions
        /// </summary>
        protected override void Initialization()
        {
            base.Initialization();

            if (CustomInputActionsAsset != null)
            {
                InitializeFromCustomAsset();
            }
            else
            {
                InitializeFromGeneratedActions();
            }
        }

        /// <summary>
        /// Binds all controls using the strongly-typed generated TopDownEngineInputActions class.
        /// </summary>
        protected virtual void InitializeFromGeneratedActions()
        {
            InputActions.PlayerControls.PrimaryMovement.performed += context =>
            {
                _primaryMovementInput = context.ReadValue<Vector2>();
                TestForceDesktop();
            };
            InputActions.PlayerControls.PrimaryMovement.canceled += context =>
            {
                _primaryMovementInput = context.ReadValue<Vector2>();
                TestForceDesktop();
            };
            InputActions.PlayerControls.SecondaryMovement.performed += context => _secondaryMovementInput = context.ReadValue<Vector2>();
            InputActions.PlayerControls.SecondaryMovement.canceled += context => _secondaryMovementInput = context.ReadValue<Vector2>();
            InputActions.PlayerControls.CameraRotation.performed += context => _cameraRotationInput = context.ReadValue<float>();
            InputActions.PlayerControls.CameraRotation.canceled += context => _cameraRotationInput = context.ReadValue<float>();

            InputActions.PlayerControls.Jump.performed += context => { BindButton(context, JumpButton); };
            InputActions.PlayerControls.Run.performed += context => { BindButton(context, RunButton); };
            InputActions.PlayerControls.Dash.performed += context => { BindButton(context, DashButton); };
            InputActions.PlayerControls.Crouch.performed += context => { BindButton(context, CrouchButton); };
            InputActions.PlayerControls.Shoot.performed += context => { BindButton(context, ShootButton); };
            InputActions.PlayerControls.SecondaryShoot.performed += context => { BindButton(context, SecondaryShootButton); };
            InputActions.PlayerControls.Interact.performed += context => { BindButton(context, InteractButton); };
            InputActions.PlayerControls.Reload.performed += context => { BindButton(context, ReloadButton); };
            InputActions.PlayerControls.Pause.performed += context => { BindButton(context, PauseButton); };
            InputActions.PlayerControls.SwitchWeapon.performed += context => { BindButton(context, SwitchWeaponButton); };
            InputActions.PlayerControls.SwitchCharacter.performed += context => { BindButton(context, SwitchCharacterButton); };
            InputActions.PlayerControls.TimeControl.performed += context => { BindButton(context, TimeControlButton); };
        }

        /// <summary>
        /// Binds all controls using string-based action lookup on the custom InputActionAsset.
        /// </summary>
        protected virtual void InitializeFromCustomAsset()
        {
            _playerControlsMap = _activeInputActionsAsset.FindActionMap(PlayerControlsActionMapName, false);
            if (_playerControlsMap == null)
            {
                Debug.LogWarning($"{nameof(InputSystemManager)} could not find action map '{PlayerControlsActionMapName}' on asset '{_activeInputActionsAsset.name}'.");
                return;
            }

            _primaryMovementAction = FindPlayerAction("PrimaryMovement");
            if (_primaryMovementAction != null)
            {
                _primaryMovementAction.performed += context => { _primaryMovementInput = context.ReadValue<Vector2>(); TestForceDesktop(); };
                _primaryMovementAction.canceled += context => { _primaryMovementInput = context.ReadValue<Vector2>(); TestForceDesktop(); };
            }

            _secondaryMovementAction = FindPlayerAction("SecondaryMovement");
            if (_secondaryMovementAction != null)
            {
                _secondaryMovementAction.performed += context => _secondaryMovementInput = context.ReadValue<Vector2>();
                _secondaryMovementAction.canceled += context => _secondaryMovementInput = context.ReadValue<Vector2>();
            }

            _cameraRotationAction = FindPlayerAction("CameraRotation");
            if (_cameraRotationAction != null)
            {
                _cameraRotationAction.performed += context => _cameraRotationInput = context.ReadValue<float>();
                _cameraRotationAction.canceled += context => _cameraRotationInput = context.ReadValue<float>();
            }

            BindButtonAction("Jump", JumpButton);
            BindButtonAction("Run", RunButton);
            BindButtonAction("Dash", DashButton);
            BindButtonAction("Crouch", CrouchButton);
            BindButtonAction("Shoot", ShootButton);
            BindButtonAction("SecondaryShoot", SecondaryShootButton);
            BindButtonAction("Interact", InteractButton);
            BindButtonAction("Reload", ReloadButton);
            BindButtonAction("Pause", PauseButton);
            BindButtonAction("SwitchWeapon", SwitchWeaponButton);
            BindButtonAction("SwitchCharacter", SwitchCharacterButton);
            BindButtonAction("TimeControl", TimeControlButton);
        }

        /// <summary>
        /// Finds an action by name in the player controls map, logging a warning if not found.
        /// </summary>
        protected virtual InputAction FindPlayerAction(string actionName)
        {
            var action = _playerControlsMap.FindAction(actionName, false);
            if (action == null)
            {
                Debug.LogWarning($"{nameof(InputSystemManager)} could not find action '{actionName}' in map '{PlayerControlsActionMapName}'.");
            }
            return action;
        }

        /// <summary>
        /// Binds performed/canceled callbacks for a named action to an IMButton.
        /// </summary>
        protected virtual void BindButtonAction(string actionName, MMInput.IMButton imButton)
        {
            var action = FindPlayerAction(actionName);
            if (action == null) { return; }
            action.performed += context => { BindButton(context, imButton); };
            action.canceled += context => { BindButton(context, imButton); };
        }

        protected virtual void TestForceDesktop()
        {
            if ((Mathf.Abs(_primaryMovement.x) > Threshold.x) ||
             (Mathf.Abs(_primaryMovement.y) > Threshold.y))
            {
                _primaryAxisActiveTimestamp = Time.unscaledTime;
                
                if (IsMobile && ForceDesktopIfPrimaryAxisActive)
                {
                    IsMobile = false;
                    IsPrimaryAxisActive = true;
                    if (GUIManager.HasInstance) { GUIManager.Instance.SetMobileControlsActive(false); }
                }
            }
            
        }

        protected override void Update()
        {
            TestAutoRevert();
            _primaryMovement = ApplyCameraRotation(_primaryMovementInput);
            _secondaryMovement = ApplyCameraRotation(_secondaryMovementInput);
        }

        protected virtual void TestAutoRevert()
        {
            if (!IsMobile && ForceDesktopIfPrimaryAxisActive && AutoRevertToMobileIfPrimaryAxisInactive)
            {
                if (Time.unscaledTime - _primaryAxisActiveTimestamp > AutoRevertToMobileIfPrimaryAxisInactiveDuration)
                {
                    if (GUIManager.HasInstance) { GUIManager.Instance.SetMobileControlsActive(true, MovementControl); }
                    IsMobile = true;
                    IsPrimaryAxisActive = false;
                }
            }
        }

        /// <summary>
        /// Changes the state of our button based on the input value
        /// </summary>
        /// <param name="context"></param>
        /// <param name="imButton"></param>
        protected virtual void BindButton(InputAction.CallbackContext context, MMInput.IMButton imButton)
        {
            if (!InputDetectionActive)
            {
                return;
            }
        
            var control = context.control;

            if (control is ButtonControl button)
            {
                if (button.wasPressedThisFrame)
                {
                    imButton.State.ChangeState(MMInput.ButtonStates.ButtonDown);
                }
                if ( button.wasReleasedThisFrame 
                    || (imButton.State.CurrentState == MMInput.ButtonStates.ButtonPressed && !button.isPressed) )
                {
                    imButton.State.ChangeState(MMInput.ButtonStates.ButtonUp);
                }
            }
        }

        protected override void GetInputButtons()
        {
            // useless now
        }

        public override void SetMovement()
        {
            //do nothing
        }

        public override void SetSecondaryMovement()
        {
            //do nothing
        }

        protected override void SetShootAxis()
        {
            //do nothing
        }
        
        protected override void SetCameraRotationAxis()
        {
            // do nothing
        }
        
        protected override void TestPrimaryAxis()
        {
            // do nothing
        }

        /// <summary>
        /// On enable we enable our input actions
        /// </summary>
        protected virtual void OnEnable()
        {
            _activeInputActionsAsset?.Enable();
        }

        /// <summary>
        /// On disable we disable our input actions
        /// </summary>
        protected virtual void OnDisable()
        {
            _activeInputActionsAsset?.Disable();
        }
    }
}