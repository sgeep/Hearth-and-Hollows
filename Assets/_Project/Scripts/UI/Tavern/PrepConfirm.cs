using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Tavern.Scene;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>
    /// The menu board's question (4h): begin evening prep now? The free day ends only when the player says yes; "not yet" (or
    /// Escape / B) goes back to the room. Holds the surface clock while it's asked.
    /// </summary>
    public sealed class PrepConfirm : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] Button m_Yes;
        [SerializeField] Button m_No;

        public bool IsOpen => m_Root != null && m_Root.activeSelf;
        public Button Yes => m_Yes;
        public Button No => m_No;

        public void Configure(GameObject root, Button yes, Button no)
        {
            m_Root = root;
            m_Yes = yes;
            m_No = no;
        }

        void Awake()
        {
            if (m_Yes != null) m_Yes.onClick.AddListener(Confirm);
            if (m_No != null) m_No.onClick.AddListener(Cancel);
            if (m_Root != null) m_Root.SetActive(false);
        }

        void OnEnable() => EventBus<DaytimePlaceUsed>.Subscribe(OnPlaceUsed);

        void OnDisable()
        {
            EventBus<DaytimePlaceUsed>.Unsubscribe(OnPlaceUsed);
            SurfacePause.Release(this);
        }

        void OnPlaceUsed(DaytimePlaceUsed e)
        {
            if (e.Kind == TavernInteractableKind.MenuBoard) Ask();
        }

        /// <summary>Asks (the menu board).</summary>
        public void Ask()
        {
            TavernDirector director = TavernDirector.Instance;
            if (IsOpen || m_Root == null || director == null || director.Phase != TavernPhase.Daytime) return;
            m_Root.SetActive(true);
            SurfacePause.Hold(this);
            InputMaps.ActivateUIOnly();
            // "Not yet" is the safe default under the cursor.
            if (EventSystem.current != null && m_No != null) EventSystem.current.SetSelectedGameObject(m_No.gameObject);
        }

        /// <summary>Yes: the evening begins (Prep; the village unloads).</summary>
        public void Confirm()
        {
            if (!IsOpen) return;
            m_Root.SetActive(false);
            SurfacePause.Release(this);
            TavernDirector.Instance?.OpenForEvening();
        }

        /// <summary>Not yet: back to the room.</summary>
        public void Cancel()
        {
            if (!IsOpen) return;
            m_Root.SetActive(false);
            SurfacePause.Release(this);
            TavernDirector.RestoreInput();
        }

        void Update()
        {
            if (!IsOpen) return;
            bool back = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame
                        || UnityEngine.InputSystem.Gamepad.current != null && UnityEngine.InputSystem.Gamepad.current.buttonEast.wasPressedThisFrame;
            if (back) Cancel();
        }
    }
}
