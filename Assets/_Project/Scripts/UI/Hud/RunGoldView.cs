using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Localization;
using UnityEngine;

namespace Hearthdelve.UI.Hud
{
    /// <summary>
    /// The run's Gold under the Essence bar (4d step 3): a coin and the amount, shown once the run has found some.
    /// It's unbanked: the delve result says whether it came home.
    /// </summary>
    public sealed class RunGoldView : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] LocalizedSuperText m_Amount;

        public int Shown { get; private set; }

        public void Configure(GameObject root, LocalizedSuperText amount)
        {
            m_Root = root;
            m_Amount = amount;
        }

        void Awake() => Show(0);
        void OnEnable() => EventBus<RunGoldChanged>.Subscribe(OnChanged);
        void OnDisable() => EventBus<RunGoldChanged>.Unsubscribe(OnChanged);

        void OnChanged(RunGoldChanged e) => Show(e.Gold);

        void Show(int gold)
        {
            Shown = gold;
            if (m_Root != null) m_Root.SetActive(gold > 0);
            if (gold > 0) m_Amount?.Set(LocKeys.HudRunGold, gold);
        }
    }
}
