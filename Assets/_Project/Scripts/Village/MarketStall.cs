using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Tavern.Scene;
using UnityEngine;

namespace Hearthdelve.Village
{
    /// <summary>
    /// Kariaston's market stall (4h Checkpoint A): a place in the square that opens the existing market (the same offers
    /// and prices, <c>MarketPanel</c> over <c>SupplySource</c>) while it trades, from morning until five, and is packed up
    /// after. Until Grim keeps it (Checkpoint C) nobody stands behind it. Its look follows the market hours.
    /// </summary>
    public sealed class MarketStall : MonoBehaviour
    {
        [SerializeField] TavernInteractable m_Interactable;
        [SerializeField, Tooltip("Shown while the market trades.")] GameObject m_Open;
        [SerializeField, Tooltip("Shown once it has packed up for the day.")] GameObject m_Closed;
        [SerializeField, Tooltip("UI key of the line shown when it's closed.")] string m_ClosedKey;

        public bool IsOpen => SurfaceTime.MarketOpen;
        public TavernInteractable Interactable => m_Interactable;

        public void Configure(TavernInteractable interactable, GameObject open, GameObject closed, string closedKey)
        {
            m_Interactable = interactable;
            m_Open = open;
            m_Closed = closed;
            m_ClosedKey = closedKey;
        }

        void OnEnable()
        {
            EventBus<SurfaceTimeChanged>.Subscribe(OnTime);
            if (m_Interactable == null) return;
            m_Interactable.Used += OnUsed;
            m_Interactable.Describe = () => IsOpen ? TavernHint.Use(m_Interactable.NameKey) : new TavernHint(TavernHintKind.Note, m_ClosedKey);
            Show();
        }

        void OnDisable()
        {
            EventBus<SurfaceTimeChanged>.Unsubscribe(OnTime);
            if (m_Interactable != null) m_Interactable.Used -= OnUsed;
        }

        void OnTime(SurfaceTimeChanged _) => Show();

        void Show()
        {
            bool open = IsOpen;
            if (m_Open != null && m_Open.activeSelf != open) m_Open.SetActive(open);
            if (m_Closed != null && m_Closed.activeSelf == open) m_Closed.SetActive(!open);
        }

        void OnUsed(TavernInteractable _)
        {
            if (IsOpen) EventBus<MarketStallUsed>.Publish(default);
        }
    }
}
