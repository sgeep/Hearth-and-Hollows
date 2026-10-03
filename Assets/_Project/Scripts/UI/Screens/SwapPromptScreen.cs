using System;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// The satchel-full flow's UI (GDD §4.4): the "satchel full — press Interact to swap" hint, and the
    /// swap prompt itself. The prompt shows the part found and the six satchel slots; choosing a slot
    /// drops that slot's whole stack for the new part, "Leave it" (or Cancel) leaves the new part on the
    /// ground. Keyboard, controller and mouse all work. Gameplay pauses it and switches input; this
    /// only shows the choice and answers through the request.
    /// </summary>
    public sealed class SwapPromptScreen : MonoBehaviour
    {
        [SerializeField] GameObject m_Panel;
        [SerializeField] LocalizedSuperText m_Subtitle;
        [SerializeField] Image m_IncomingIcon;
        [SerializeField] LocalizedSuperText m_Detail;
        [SerializeField] SatchelSlotView[] m_Slots = Array.Empty<SatchelSlotView>();
        [SerializeField] Button m_LeaveIt;
        [SerializeField] GameObject m_Hint;
        [SerializeField] LocalizedSuperText m_HintText;

        Action<int> m_OnChosen;
        Satchel m_Satchel;

        public bool IsOpen => m_Panel != null && m_Panel.activeSelf;
        public SatchelSlotView[] Slots => m_Slots;
        public Button LeaveIt => m_LeaveIt;

        public void Configure(GameObject panel, LocalizedSuperText subtitle, Image incomingIcon, LocalizedSuperText detail,
            SatchelSlotView[] slots, Button leaveIt, GameObject hint, LocalizedSuperText hintText)
        {
            m_Panel = panel;
            m_Subtitle = subtitle;
            m_IncomingIcon = incomingIcon;
            m_Detail = detail;
            m_Slots = slots;
            m_LeaveIt = leaveIt;
            m_Hint = hint;
            m_HintText = hintText;
        }

        void Awake()
        {
            for (int i = 0; i < m_Slots.Length; i++)
            {
                int index = i;
                m_Slots[i].GetComponent<Button>().onClick.AddListener(() => Choose(index));
                m_Slots[i].Selected += ShowDetail;
            }
            if (m_LeaveIt != null) m_LeaveIt.onClick.AddListener(() => Choose(-1));
            if (m_Panel != null) m_Panel.SetActive(false);
            if (m_Hint != null) m_Hint.SetActive(false);
        }

        void OnEnable()
        {
            EventBus<SwapPromptRequested>.Subscribe(Open);
            EventBus<SatchelFullHint>.Subscribe(OnHint);
        }

        void OnDisable()
        {
            EventBus<SwapPromptRequested>.Unsubscribe(Open);
            EventBus<SatchelFullHint>.Unsubscribe(OnHint);
        }

        void OnHint(SatchelFullHint hint)
        {
            if (m_Hint == null) return;
            m_Hint.SetActive(hint.Visible && !IsOpen);
            if (hint.Visible && m_HintText != null) m_HintText.Set(LocKeys.HudSatchelFull, InteractBinding());
        }

        static string InteractBinding()
        {
            InputAction interact = InputMaps.Find(InputMaps.Dungeon, DungeonActions.Interact);
            if (interact == null) return string.Empty;
            // The binding for whichever device the player is using.
            string group = Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame ? "Gamepad" : "Keyboard&Mouse";
            string shown = interact.GetBindingDisplayString(InputBinding.MaskByGroup(group));
            return string.IsNullOrEmpty(shown) ? interact.GetBindingDisplayString() : shown;
        }

        void Open(SwapPromptRequested request)
        {
            m_Satchel = request.Satchel;
            m_OnChosen = request.OnChosen;
            if (m_Hint != null) m_Hint.SetActive(false);
            IngredientStack incoming = request.Incoming;
            if (m_Subtitle != null) m_Subtitle.Set(LocKeys.SwapSubtitle, Loc.ItemName(incoming.Item), incoming.Count);
            if (m_IncomingIcon != null)
            {
                m_IncomingIcon.enabled = incoming.Item.IsValid && incoming.Item.Definition.icon != null;
                if (m_IncomingIcon.enabled) m_IncomingIcon.sprite = incoming.Item.Definition.icon;
            }
            for (int i = 0; i < m_Slots.Length; i++)
            {
                bool exists = i < m_Satchel.Capacity;
                m_Slots[i].gameObject.SetActive(exists);
                if (exists) m_Slots[i].Show(m_Satchel.Slots[i]);
            }
            m_Panel.SetActive(true);
            if (EventSystem.current != null && m_Slots.Length > 0)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(m_Slots[0].gameObject);
            }
        }

        void ShowDetail(SatchelSlotView slot)
        {
            if (m_Detail == null) return;
            IngredientStack stack = slot.Stack;
            if (stack.IsEmpty) m_Detail.Set(LocKeys.SlotEmpty);
            else m_Detail.Set(LocKeys.SwapSlotDetail, Loc.ItemName(stack.Item), stack.Count, Mathf.RoundToInt(stack.Freshness * 100f));
        }

        void Update()
        {
            if (!IsOpen) return;
            // Cancel leaves the new part where it is.
            InputAction cancel = InputMaps.Find(InputMaps.UI, "Cancel");
            if (cancel != null && cancel.WasPressedThisFrame()) Choose(-1);
        }

        /// <summary>Answers the prompt: a slot index to drop for the new part, or -1 to leave it.</summary>
        public void Choose(int slot)
        {
            if (!IsOpen) return;
            m_Panel.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            Action<int> answer = m_OnChosen;
            m_OnChosen = null;
            answer?.Invoke(slot);
        }
    }
}
