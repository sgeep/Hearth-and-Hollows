using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Hearthdelve.Dungeon.Harvest
{
    /// <summary>
    /// A harvested part lying on the floor. The player touching it offers it to their satchel
    /// (<see cref="SatchelCarrier"/>); what doesn't fit stays, and the carrier offers the swap prompt.
    /// It loses freshness on the floor too. A stack the player just dropped ignores them until they
    /// step off it.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class IngredientPickup : MonoBehaviour
    {
        [SerializeField] SpriteRenderer m_Icon;
        [SerializeField, Tooltip("Played when the part is picked up (sound, haptic, visual together).")]
        MMF_Player m_PickedFeedback;

        FreshnessSettings m_Freshness = FreshnessSettings.Default;
        SatchelCarrier m_IgnoreUntilLeft;

        public IngredientItem Item { get; private set; }
        public int Count { get; private set; }
        public float Freshness { get; private set; } = Shared.Inventory.Freshness.Max;
        public IngredientStack Stack => new(Item, Count, Freshness);

        public void Configure(SpriteRenderer icon, MMF_Player pickedFeedback)
        {
            m_Icon = icon;
            m_PickedFeedback = pickedFeedback;
        }

        public void Initialize(IngredientItem item, int count, float freshness = Shared.Inventory.Freshness.Max)
        {
            Item = item;
            Count = count;
            Freshness = Shared.Inventory.Freshness.Clamp(freshness);
            if (m_Icon != null && item.IsValid && item.Definition.icon != null) m_Icon.sprite = item.Definition.icon;
        }

        /// <summary>How fast it spoils while it lies here (the delve's freshness settings).</summary>
        public void SetFreshnessRules(FreshnessSettings settings) => m_Freshness = settings;

        /// <summary>Don't go back into <paramref name="carrier"/>'s satchel until they've stepped off this pickup.</summary>
        public void IgnoreUntilLeft(SatchelCarrier carrier) => m_IgnoreUntilLeft = carrier;

        void Update()
        {
            if (Count > 0 && Item.IsValid)
                Freshness = Shared.Inventory.Freshness.Clamp(Freshness - m_Freshness.LossFor(Item, m_Freshness.dungeonLossPerMinute * Time.deltaTime / 60f));
        }

        void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
        void OnTriggerStay2D(Collider2D other) => TryCollect(other);

        void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent(out SatchelCarrier carrier)) return;
            if (m_IgnoreUntilLeft == carrier) m_IgnoreUntilLeft = null;
            carrier.Withdraw(this);
        }

        void TryCollect(Collider2D other)
        {
            if (Count <= 0 || !Item.IsValid) return;
            // Only the carrier's own body collects: a weapon's damage area is a child collider and must not.
            if (!other.TryGetComponent(out SatchelCarrier carrier) || carrier.Satchel == null || carrier == m_IgnoreUntilLeft) return;
            Take(carrier.Offer(this));
        }

        /// <summary>Removes <paramref name="taken"/> parts (they went into the satchel); the pickup goes when empty.</summary>
        public void Take(int taken)
        {
            if (taken <= 0) return;
            Count -= taken;
            if (m_PickedFeedback != null) m_PickedFeedback.PlayFeedbacks(transform.position);
            if (Count > 0) return;
            // Let the feedback finish (it lives on this object) before the pickup goes away.
            if (m_Icon != null) m_Icon.enabled = false;
            GetComponent<Collider2D>().enabled = false;
            Destroy(gameObject, m_PickedFeedback != null ? Mathf.Max(0.1f, m_PickedFeedback.TotalDuration) : 0f);
        }
    }
}
