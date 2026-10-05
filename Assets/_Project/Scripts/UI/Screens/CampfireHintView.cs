using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Localization;
using UnityEngine;

namespace Hearthdelve.UI.Screens
{
    /// <summary>The campfire's prompt (4e sign-off): what it's for as the player comes near, and that it's working while they rest.</summary>
    public sealed class CampfireHintView : MonoBehaviour
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

        void OnEnable() => EventBus<CampfireHint>.Subscribe(OnHint);
        void OnDisable() => EventBus<CampfireHint>.Unsubscribe(OnHint);

        public bool IsWarming { get; private set; }

        void OnHint(CampfireHint hint)
        {
            if (m_Hint == null) return;
            IsWarming = hint.Visible && hint.Warming;
            HintVisibility.Set(m_Hint, hint.Visible);
            if (hint.Visible) m_Text?.Set(hint.Warming ? LoopLocKeys.HudCampfireWarming : LoopLocKeys.HudCampfire);
        }
    }
}
