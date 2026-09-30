using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

namespace Hearthdelve.UI.Hud
{
    /// <summary>
    /// Minimal dungeon HUD (GDD §8.2): Essence bar, satchel slots (with freshness), harvest feed,
    /// the satchel-full and exit hints, and the day. Driven by events and the Shared GameFlow;
    /// knows nothing about Dungeon types.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class DungeonHud : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] float m_FeedEntrySeconds = 3f;
        [SerializeField, Min(1)] int m_MaxFeedEntries = 5;

        VisualElement m_EssenceFill;
        VisualElement m_EssenceBar;
        Label m_EssenceLabel;
        Label m_SatchelLabel;
        VisualElement m_Slots;
        VisualElement m_Feed;
        Label m_Hint;
        Label m_ExitHint;
        Label m_Day;
        Satchel m_Satchel;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_EssenceBar = root.Q("essence-bar");
            m_EssenceFill = root.Q("essence-fill");
            m_EssenceLabel = root.Q<Label>("essence-label");
            m_SatchelLabel = root.Q<Label>("satchel-label");
            m_Slots = root.Q("satchel-slots");
            m_Feed = root.Q("harvest-feed");
            m_Hint = root.Q<Label>("satchel-hint");
            m_Hint.style.display = DisplayStyle.None;
            m_ExitHint = root.Q<Label>("exit-hint");
            m_ExitHint.style.display = DisplayStyle.None;
            m_Day = root.Q<Label>("day");
            // Freshness drains continuously without a Changed event; redraw the slots now and then.
            m_Slots.schedule.Execute(RefreshSlots).Every(1000);

            EventBus<EssenceChanged>.Subscribe(OnEssence);
            EventBus<SatchelBound>.Subscribe(OnSatchelBound);
            EventBus<HarvestFeedback>.Subscribe(OnHarvest);
            EventBus<SatchelFullHint>.Subscribe(OnHint);
            EventBus<DelveExitHint>.Subscribe(OnExitHint);
            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
            RefreshStaticText();
        }

        void OnDisable()
        {
            EventBus<EssenceChanged>.Unsubscribe(OnEssence);
            EventBus<SatchelBound>.Unsubscribe(OnSatchelBound);
            EventBus<HarvestFeedback>.Unsubscribe(OnHarvest);
            EventBus<SatchelFullHint>.Unsubscribe(OnHint);
            EventBus<DelveExitHint>.Unsubscribe(OnExitHint);
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            if (m_Satchel != null) m_Satchel.Changed -= RefreshSlots;
        }

        void OnLocaleChanged(Locale _)
        {
            RefreshStaticText();
            RefreshSlots();
        }

        void RefreshStaticText()
        {
            m_EssenceLabel.text = Loc.UI(LocKeys.HudEssence);
            m_SatchelLabel.text = Loc.UI(LocKeys.HudSatchel);
            var flow = GameFlow.Instance;
            bool inLoop = flow != null && flow.InGame;
            m_Day.style.display = inLoop ? DisplayStyle.Flex : DisplayStyle.None;
            if (inLoop) m_Day.text = Loc.UI(LoopLocKeys.HudDay, flow.State.Day);
        }

        void OnExitHint(DelveExitHint evt)
        {
            m_ExitHint.style.display = evt.Visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!evt.Visible) return;
            var interact = InputMaps.Find(InputMaps.Dungeon, DungeonActions.Interact);
            m_ExitHint.text = Loc.UI(LoopLocKeys.HudExit, interact != null ? interact.GetBindingDisplayString() : "?");
        }

        void OnEssence(EssenceChanged evt)
        {
            m_EssenceFill.style.width = Length.Percent(Mathf.Clamp01(evt.Normalized) * 100f);
            m_EssenceBar.EnableInClassList("hd-essence--low", evt.IsLow);
            if (evt.FromDamage)
            {
                m_EssenceBar.AddToClassList("hd-essence--hit");
                m_EssenceBar.schedule.Execute(() => m_EssenceBar.RemoveFromClassList("hd-essence--hit")).StartingIn(120);
            }
        }

        void OnSatchelBound(SatchelBound evt)
        {
            if (m_Satchel != null) m_Satchel.Changed -= RefreshSlots;
            m_Satchel = evt.Satchel;
            m_Satchel.Changed += RefreshSlots;
            RefreshSlots();
        }

        void RefreshSlots()
        {
            if (m_Satchel == null) return;
            m_Slots.Clear();
            foreach (var slot in m_Satchel.Slots)
            {
                var element = new VisualElement();
                SlotView.Populate(element, slot, compact: true);
                m_Slots.Add(element);
            }
        }

        void OnHarvest(HarvestFeedback evt)
        {
            string itemName = Loc.ItemName(evt.Item);
            string text = (evt.Flags & HarvestFlags.Destroyed) != 0
                ? Loc.UI(LocKeys.HarvestDestroyed, itemName)
                : Loc.UI(LocKeys.HarvestGot, itemName, evt.Count);

            if ((evt.Flags & HarvestFlags.Finisher) != 0) text = $"{Loc.UI(LocKeys.HarvestFinisher)} {text}";
            else if ((evt.Flags & HarvestFlags.CleanKill) != 0 && (evt.Flags & HarvestFlags.Overkill) == 0) text = $"{Loc.UI(LocKeys.HarvestCleanKill)} {text}";
            else if ((evt.Flags & HarvestFlags.Overkill) != 0) text = $"{Loc.UI(LocKeys.HarvestOverkill)} {text}";

            var entry = new Label(text);
            entry.AddToClassList("hd-feed__entry");
            entry.AddToClassList($"hd-quality--{evt.Item.Quality.ToString().ToLowerInvariant()}");
            if ((evt.Flags & HarvestFlags.Destroyed) != 0) entry.AddToClassList("hd-feed__entry--destroyed");
            m_Feed.Insert(0, entry);
            while (m_Feed.childCount > m_MaxFeedEntries) m_Feed.RemoveAt(m_Feed.childCount - 1);
            entry.schedule.Execute(() => entry.RemoveFromHierarchy()).StartingIn((long)(m_FeedEntrySeconds * 1000));
        }

        void OnHint(SatchelFullHint evt)
        {
            m_Hint.style.display = evt.Visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!evt.Visible) return;
            var interact = InputMaps.Find(InputMaps.Dungeon, DungeonActions.Interact);
            string binding = interact != null ? interact.GetBindingDisplayString() : "?";
            m_Hint.text = Loc.UI(LocKeys.HudSatchelFull, binding);
        }
    }
}
