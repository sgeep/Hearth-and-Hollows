using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Localization;
using UnityEngine.UIElements;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// Death / Essence-depleted screen: pick one satchel slot (its whole stack) for the Lockbox, then return.
    /// Selecting a slot highlights it; the confirm button commits.
    /// </summary>
    public sealed class DeathScreen : SlotPickerScreen
    {
        Label m_Title, m_Subtitle, m_Selection;
        Button m_Confirm, m_KeepNothing;
        Satchel m_Satchel;
        int m_Selected = DeathPenalty.KeepNothing;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Title = Root.Q<Label>("title");
            m_Subtitle = Root.Q<Label>("subtitle");
            m_Selection = Root.Q<Label>("selection");
            m_Confirm = Root.Q<Button>("confirm");
            m_KeepNothing = Root.Q<Button>("keep-nothing");
            m_Confirm.clicked += () => Close(m_Selected);
            m_KeepNothing.clicked += () => Select(DeathPenalty.KeepNothing);
            EventBus<DeathScreenRequested>.Subscribe(OnRequested);
        }

        void OnDisable() => EventBus<DeathScreenRequested>.Unsubscribe(OnRequested);

        void OnRequested(DeathScreenRequested evt)
        {
            m_Satchel = evt.Satchel;
            m_Title.text = Loc.UI(LocKeys.DeathTitle);
            m_Subtitle.text = Loc.UI(m_Satchel.IsEmpty ? LocKeys.DeathSubtitleEmpty : LocKeys.DeathSubtitle);
            m_Confirm.text = Loc.UI(LocKeys.DeathConfirm);
            m_KeepNothing.text = Loc.UI(LocKeys.DeathKeepNothing);
            m_KeepNothing.style.display = m_Satchel.IsEmpty ? DisplayStyle.None : DisplayStyle.Flex;

            Open(m_Satchel, evt.OnChosen, emptySlotsSelectable: false);

            var first = FirstEnabledSlot();
            // Default to the first part so a panicked confirm still keeps something.
            Select(first != null ? SlotButtons.IndexOf(first) : DeathPenalty.KeepNothing);
            FocusLater(first != null ? first : m_Confirm);
        }

        protected override void OnSlotClicked(int index)
        {
            Select(index);
            FocusLater(m_Confirm);
        }

        void Select(int index)
        {
            m_Selected = index;
            for (int i = 0; i < SlotButtons.Count; i++)
                SlotButtons[i].EnableInClassList(SlotView.SelectedClass, i == index);

            m_Selection.text = index == DeathPenalty.KeepNothing
                ? Loc.UI(LocKeys.DeathSelectedNone)
                : Loc.UI(LocKeys.DeathSelected, Loc.ItemName(m_Satchel.Slots[index].Item), m_Satchel.Slots[index].Count);
        }
    }
}
