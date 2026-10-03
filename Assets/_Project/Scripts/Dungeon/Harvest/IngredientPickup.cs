using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Hearthdelve.Dungeon.Harvest
{
    /// <summary>
    /// A harvested part lying on the floor. Walking over it puts as many as fit into the
    /// satchel; what doesn't fit stays on the ground (the swap prompt arrives in 4b).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class IngredientPickup : MonoBehaviour
    {
        [SerializeField] SpriteRenderer m_Icon;
        [SerializeField, Tooltip("Played when the part is picked up (sound, haptic, visual together).")]
        MMF_Player m_PickedFeedback;

        public IngredientItem Item { get; private set; }
        public int Count { get; private set; }
        public float Freshness { get; private set; } = Shared.Inventory.Freshness.Max;

        public void Configure(SpriteRenderer icon, MMF_Player pickedFeedback)
        {
            m_Icon = icon;
            m_PickedFeedback = pickedFeedback;
        }

        public void Initialize(IngredientItem item, int count)
        {
            Item = item;
            Count = count;
            if (m_Icon != null && item.IsValid && item.Definition.icon != null) m_Icon.sprite = item.Definition.icon;
        }

        void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
        void OnTriggerStay2D(Collider2D other) => TryCollect(other);

        void TryCollect(Collider2D other)
        {
            if (Count <= 0 || !Item.IsValid) return;
            // Only the carrier's own body collects: a weapon's damage area is a child collider and must not.
            if (!other.TryGetComponent(out SatchelCarrier carrier) || carrier.Satchel == null) return;

            int left = carrier.Satchel.Add(Item, Count, Freshness);
            if (left == Count) return;

            Count = left;
            if (m_PickedFeedback != null) m_PickedFeedback.PlayFeedbacks(transform.position);
            if (Count > 0) return;

            // Let the feedback finish (it lives on this object) before the pickup goes away.
            if (m_Icon != null) m_Icon.enabled = false;
            GetComponent<Collider2D>().enabled = false;
            Destroy(gameObject, m_PickedFeedback != null ? Mathf.Max(0.1f, m_PickedFeedback.TotalDuration) : 0f);
        }
    }
}
