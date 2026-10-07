using Hearthdelve.Core.Events;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Screens;
using UnityEngine;

namespace Hearthdelve.UI.Tavern
{
    /// <summary>"E: Grill" while the player is in reach of something to use (<see cref="TavernInteractHint"/>).</summary>
    public sealed class TavernHintView : MonoBehaviour
    {
        [SerializeField] GameObject m_Hint;
        [SerializeField] LocalizedSuperText m_Text;

        public bool IsShown => HintVisibility.IsShown(m_Hint);

        public void Configure(GameObject hint, LocalizedSuperText text)
        {
            m_Hint = hint;
            m_Text = text;
        }

        void Awake() => HintVisibility.Init(m_Hint);

        void OnEnable() => EventBus<TavernInteractHint>.Subscribe(OnHint);
        void OnDisable() => EventBus<TavernInteractHint>.Unsubscribe(OnHint);

        void OnHint(TavernInteractHint hint)
        {
            if (m_Hint == null) return;
            HintVisibility.Set(m_Hint, hint.Visible);
            if (!hint.Visible || m_Text == null) return;
            TavernHint h = hint.Hint;
            string key = InputHints.TavernInteract();
            string dish = h.Dish != null ? Loc.Get(h.Dish.displayName) : string.Empty;
            switch (h.Kind)
            {
                case TavernHintKind.Cook: m_Text.Set(TavernLocKeys.HintCook, key, dish); break;
                case TavernHintKind.PickUp: m_Text.Set(TavernLocKeys.HintPickUp, key, dish); break;
                case TavernHintKind.Serve: m_Text.Set(TavernLocKeys.HintServe, key, dish); break;
                case TavernHintKind.PutBack: m_Text.Set(TavernLocKeys.HintPutBack, key, dish); break;
                case TavernHintKind.WrongDish: m_Text.Set(TavernLocKeys.HintWrongDish, dish); break;
                case TavernHintKind.StartStew: m_Text.Set(TavernLocKeys.HintStartStew, key, dish); break;
                case TavernHintKind.Simmering: m_Text.Set(TavernLocKeys.HintSimmering, dish); break;
                case TavernHintKind.StewReady: m_Text.Set(TavernLocKeys.HintStewReady, dish, h.Count); break;
                case TavernHintKind.Staffed: m_Text.Set(TavernLocKeys.HintStaffed, h.Staff != null ? Loc.Get(h.Staff.displayName) : string.Empty); break;
                // Staff by their definition; a villager (2026-10-07) by the Content table key in the hint.
                case TavernHintKind.Talk:
                    m_Text.Set(TavernLocKeys.HintTalk, key, h.Staff != null ? Loc.Get(h.Staff.displayName)
                        : !string.IsNullOrEmpty(h.NameKey) ? Loc.Get(Loc.ContentTable, h.NameKey) : string.Empty);
                    break;
                case TavernHintKind.Note: m_Text.Set(h.NameKey); break;
                case TavernHintKind.Growing:
                    m_Text.Set(h.Count <= 1 ? GardenLocKeys.ReadyTomorrow : GardenLocKeys.ReadyIn, Loc.UI(h.NameKey), h.Count);
                    break;
                default: m_Text.Set(TavernLocKeys.HintUse, key, Loc.UI(h.NameKey)); break;
            }
        }
    }
}
