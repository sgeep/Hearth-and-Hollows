using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Run;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>
    /// A furnishing discovery on the floor of the Hollows (4f Checkpoint C; plan §14): a small chest with a cool glint, unlike
    /// a coin, a part or a power's spark. Walking over it adds the piece to the run's curios (<see cref="DelveRunController.Loot"/>):
    /// it never takes a satchel slot, comes home on extraction and is lost on death (D9). Left lying, it stays with the room.
    /// </summary>
    public sealed class CurioPickup : MonoBehaviour
    {
        [SerializeField] string m_FurnitureId;
        [SerializeField, Tooltip("The chest, bobbing.")]
        Transform m_Sprite;
        [SerializeField, Tooltip("The glint: a star that twinkles over the chest.")]
        SpriteRenderer m_Glint;
        [SerializeField, Tooltip("Picking it up: chime, flash and the discovery haptic together.")]
        MMF_Player m_Feedback;
        [SerializeField, Min(0f), Tooltip("Seconds before it can be picked up, so it's seen appearing.")]
        float m_Delay = 0.45f;

        float m_Spawned;
        bool m_Taken;

        public string FurnitureId => m_FurnitureId;

        public void Configure(Transform sprite, SpriteRenderer glint, MMF_Player feedback)
        {
            m_Sprite = sprite;
            m_Glint = glint;
            m_Feedback = feedback;
        }

        public void SetPiece(string furnitureId) => m_FurnitureId = furnitureId;

        void OnEnable() => m_Spawned = Time.time;

        void Update()
        {
            float t = Time.time - m_Spawned;
            if (m_Sprite != null) m_Sprite.localPosition = new Vector3(0f, 0.3f + Mathf.Sin(t * 3f) * 0.07f, 0f);
            if (m_Glint == null) return;
            // A twinkle every second or so, off the chest's corner: a different rhythm from the coin's bob.
            float phase = Mathf.Repeat(t * 0.9f, 1f);
            float twinkle = phase < 0.35f ? Mathf.Sin(phase / 0.35f * Mathf.PI) : 0f;
            m_Glint.color = new Color(1f, 1f, 1f, twinkle);
            m_Glint.transform.localScale = Vector3.one * (0.6f + 0.6f * twinkle);
            m_Glint.transform.localPosition = new Vector3(0.22f, 0.55f + Mathf.Sin(t * 3f) * 0.07f, 0f);
        }

        void OnTriggerEnter2D(Collider2D other) => Take(other);
        void OnTriggerStay2D(Collider2D other) => Take(other);

        void Take(Collider2D other)
        {
            DelveRunController run = DelveRunController.Active;
            if (m_Taken || string.IsNullOrEmpty(m_FurnitureId) || Time.time - m_Spawned < m_Delay || run == null || run.IsEnding
                || !other.TryGetComponent(out SatchelCarrier _)) return;
            m_Taken = true;
            run.Loot.AddCurio(m_FurnitureId);
            if (m_Feedback != null)
            {
                // The chime outlives the chest.
                m_Feedback.transform.SetParent(null, true);
                m_Feedback.PlayFeedbacks(transform.position);
                Destroy(m_Feedback.gameObject, 2f);
            }
            Destroy(gameObject);
        }
    }
}
