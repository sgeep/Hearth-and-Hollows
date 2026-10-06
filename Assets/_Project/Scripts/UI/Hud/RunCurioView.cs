using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Localization;
using UnityEngine;

namespace Hearthdelve.UI.Hud
{
    /// <summary>
    /// The furnishings the delve carries (4f Checkpoint C), beside the run's gold: a chest and how many, shown once there's
    /// one. Like the gold, they're the run's until it ends well; the delve result says whether they came home.
    /// </summary>
    public sealed class RunCurioView : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] LocalizedSuperText m_Count;

        public int Shown { get; private set; }

        public void Configure(GameObject root, LocalizedSuperText count)
        {
            m_Root = root;
            m_Count = count;
        }

        void Awake() => Show(0);
        void OnEnable() => EventBus<CurioFound>.Subscribe(OnFound);
        void OnDisable() => EventBus<CurioFound>.Unsubscribe(OnFound);

        void OnFound(CurioFound e) => Show(e.Total);

        void Show(int count)
        {
            Shown = count;
            if (m_Root != null) m_Root.SetActive(count > 0);
            if (count > 0) m_Count?.Set(LocKeys.HudCurios, count);
        }
    }
}
