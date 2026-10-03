using System;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Run;
using Hearthdelve.UI.Screens;
using UnityEngine;

namespace Hearthdelve.UI.Hud
{
    /// <summary>
    /// The satchel on the HUD: one slot per satchel slot (icon, count, quality dots, freshness bar),
    /// following the delve's satchel (<see cref="SatchelBound"/>). Contents refresh on every change;
    /// freshness, which falls continuously, a few times a second.
    /// </summary>
    public sealed class SatchelHud : MonoBehaviour
    {
        const float k_FreshnessRefresh = 0.25f;

        [SerializeField] SatchelSlotView[] m_Slots = Array.Empty<SatchelSlotView>();

        Satchel m_Satchel;
        float m_NextRefresh;

        public SatchelSlotView[] Slots => m_Slots;
        public Satchel Satchel => m_Satchel;

        public void Configure(SatchelSlotView[] slots) => m_Slots = slots;

        void OnEnable() => EventBus<SatchelBound>.Subscribe(Bind);

        void OnDisable()
        {
            EventBus<SatchelBound>.Unsubscribe(Bind);
            if (m_Satchel != null) m_Satchel.Changed -= Refresh;
        }

        void Bind(SatchelBound bound)
        {
            if (m_Satchel != null) m_Satchel.Changed -= Refresh;
            m_Satchel = bound.Satchel;
            if (m_Satchel != null) m_Satchel.Changed += Refresh;
            Refresh();
        }

        void Update()
        {
            if (m_Satchel == null || Time.unscaledTime < m_NextRefresh) return;
            Refresh();
        }

        public void Refresh()
        {
            m_NextRefresh = Time.unscaledTime + k_FreshnessRefresh;
            for (int i = 0; i < m_Slots.Length; i++)
            {
                bool exists = m_Satchel != null && i < m_Satchel.Capacity;
                if (m_Slots[i].gameObject.activeSelf != exists) m_Slots[i].gameObject.SetActive(exists);
                if (exists) m_Slots[i].Show(m_Satchel.Slots[i]);
            }
        }
    }
}
