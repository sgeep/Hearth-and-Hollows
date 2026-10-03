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
            if (hint.Visible) m_Text?.Set(TavernLocKeys.HintUse, InputHints.TavernInteract(), Loc.UI(hint.NameKey));
        }
    }
}
