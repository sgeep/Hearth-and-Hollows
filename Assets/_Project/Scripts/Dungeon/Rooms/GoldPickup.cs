using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Run;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>
    /// A room's Gold reward (4d step 3): a coin on the floor once the room is clear. Walking over it adds its Gold to the
    /// run (<see cref="DelveRunController.Loot"/>): unbanked until the player extracts, lost if they die. Left lying, it
    /// stays behind with the room.
    /// </summary>
    public sealed class GoldPickup : MonoBehaviour
    {
        [SerializeField, Min(0)] int m_Amount;
        [SerializeField, Tooltip("The coin, bobbing.")]
        Transform m_Sprite;
        [SerializeField, Tooltip("Picking it up: sound and haptic together.")]
        MMF_Player m_Feedback;
        [SerializeField, Min(0f), Tooltip("Seconds before it can be picked up, so it's seen appearing.")]
        float m_Delay = 0.35f;

        float m_Spawned;
        bool m_Taken;

        public int Amount => m_Amount;

        public void Configure(Transform sprite, MMF_Player feedback)
        {
            m_Sprite = sprite;
            m_Feedback = feedback;
        }

        public void SetAmount(int amount) => m_Amount = Mathf.Max(0, amount);

        void OnEnable() => m_Spawned = Time.time;

        void Update()
        {
            if (m_Sprite != null) m_Sprite.localPosition = new Vector3(0f, 0.25f + Mathf.Sin((Time.time - m_Spawned) * 4f) * 0.06f, 0f);
        }

        void OnTriggerEnter2D(Collider2D other) => Take(other);
        void OnTriggerStay2D(Collider2D other) => Take(other);

        void Take(Collider2D other)
        {
            DelveRunController run = DelveRunController.Active;
            if (m_Taken || Time.time - m_Spawned < m_Delay || run == null || run.IsEnding || !other.TryGetComponent(out SatchelCarrier _)) return;
            m_Taken = true;
            run.Loot.AddGold(m_Amount);
            if (m_Feedback != null)
            {
                // The sound outlives the coin.
                m_Feedback.transform.SetParent(null, true);
                m_Feedback.PlayFeedbacks(transform.position);
                Destroy(m_Feedback.gameObject, 1.5f);
            }
            Destroy(gameObject);
        }
    }
}
