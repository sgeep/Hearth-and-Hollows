using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Inventory;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// Shared base for full-screen modal pickers over the satchel (death screen, swap prompt).
    /// Controller/keyboard navigable; the game is paused while it's open.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public abstract class SlotPickerScreen : MonoBehaviour
    {
        protected VisualElement Root { get; private set; }
        protected VisualElement SlotRow { get; private set; }
        protected readonly List<Button> SlotButtons = new();
        Action<int> m_OnChosen;

        protected virtual void OnEnable()
        {
            Root = GetComponent<UIDocument>().rootVisualElement.Q("screen");
            SlotRow = Root.Q("slot-row");
            Root.style.display = DisplayStyle.None;
            Root.RegisterCallback<NavigationCancelEvent>(_ => OnCancel());
        }

        protected bool IsOpen => Root != null && Root.style.display == DisplayStyle.Flex;

        protected void Open(Satchel satchel, Action<int> onChosen, bool emptySlotsSelectable)
        {
            m_OnChosen = onChosen;
            SlotButtons.Clear();
            SlotRow.Clear();
            for (int i = 0; i < satchel.Capacity; i++)
            {
                int index = i;
                var slot = satchel.Slots[i];
                var button = new Button(() => OnSlotClicked(index));
                SlotView.Populate(button, slot, compact: false);
                button.SetEnabled(emptySlotsSelectable || !slot.IsEmpty);
                button.RegisterCallback<FocusInEvent>(_ => OnSlotFocused(index));
                SlotButtons.Add(button);
                SlotRow.Add(button);
            }
            Root.style.display = DisplayStyle.Flex;
        }

        protected void Close(int choice)
        {
            Root.style.display = DisplayStyle.None;
            var callback = m_OnChosen;
            m_OnChosen = null;
            callback?.Invoke(choice);
        }

        /// <summary>Focus next frame, once layout exists, so gamepad navigation starts somewhere sensible.</summary>
        protected void FocusLater(Focusable target) => Root.schedule.Execute(() => target?.Focus());

        protected Button FirstEnabledSlot()
        {
            foreach (var b in SlotButtons) if (b.enabledSelf) return b;
            return null;
        }

        protected abstract void OnSlotClicked(int index);
        protected virtual void OnSlotFocused(int index) { }
        protected virtual void OnCancel() { }
    }
}
