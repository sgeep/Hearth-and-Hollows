using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Localization;
using UnityEngine.UIElements;

namespace Hearthdelve.UI.Screens
{
    /// <summary>Satchel-full prompt: choose a slot to drop for the new part, or leave it.</summary>
    public sealed class SwapPrompt : SlotPickerScreen
    {
        Label m_Title, m_Subtitle;
        Button m_Cancel;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Title = Root.Q<Label>("title");
            m_Subtitle = Root.Q<Label>("subtitle");
            m_Cancel = Root.Q<Button>("cancel");
            m_Cancel.clicked += OnCancel;
            EventBus<SwapPromptRequested>.Subscribe(OnRequested);
        }

        void OnDisable() => EventBus<SwapPromptRequested>.Unsubscribe(OnRequested);

        void OnRequested(SwapPromptRequested evt)
        {
            m_Title.text = Loc.UI(LocKeys.SwapTitle);
            m_Subtitle.text = Loc.UI(LocKeys.SwapSubtitle, Loc.ItemName(evt.Incoming), evt.IncomingCount);
            m_Cancel.text = Loc.UI(LocKeys.SwapCancel);
            Open(evt.Satchel, evt.OnChosen, emptySlotsSelectable: true);
            FocusLater(m_Cancel);
        }

        protected override void OnSlotClicked(int index) => Close(index);

        protected override void OnCancel()
        {
            if (IsOpen) Close(-1);
        }
    }
}
