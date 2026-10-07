using Hearthdelve.Core.Events;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Quests;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>
    /// A quest object on the floor of the Hollows (4g Checkpoint B): drawn from its own frames (Boog's bomb, its fuse sputtering),
    /// with a flickering warm glow that's like no coin, part, spark or chest. Walking over it adds it to the run's quest objects
    /// (<see cref="RunLoot.QuestObjects"/>): never a satchel slot, never the Lockbox; home with an extraction, lost with a death by
    /// its policy (it turns up again on a later delve while the quest wants it).
    /// </summary>
    public sealed class QuestObjectPickup : MonoBehaviour
    {
        [SerializeField] SpriteRenderer m_Sprite;
        [SerializeField, Tooltip("The fuse's glow: flickers with the frames.")]
        Light2D m_Glow;
        [SerializeField, Tooltip("Picking it up: a find's chime, flash and haptic together.")]
        MMF_Player m_Feedback;
        [SerializeField, Tooltip("The arrow bobbing over it (4g Checkpoint C: it's small, and it's what the delve is for).")]
        Transform m_Marker;
        [SerializeField, Min(0f), Tooltip("Seconds before it can be picked up, so it's seen appearing.")]
        float m_Delay = 0.45f;

        QuestObjectDefinition m_Definition;
        float m_Spawned;
        float m_GlowIntensity = 1f;
        bool m_Taken;

        public string ObjectId => m_Definition != null ? m_Definition.id : null;

        public void Configure(SpriteRenderer sprite, Light2D glow, MMF_Player feedback, Transform marker = null)
        {
            m_Sprite = sprite;
            m_Glow = glow;
            m_Feedback = feedback;
            m_Marker = marker;
        }

        public void Set(QuestObjectDefinition definition)
        {
            m_Definition = definition;
            if (m_Sprite != null && definition != null && definition.frames.Length > 0) m_Sprite.sprite = definition.frames[0];
        }

        void Awake()
        {
            if (m_Glow != null) m_GlowIntensity = m_Glow.intensity;
        }

        void OnEnable() => m_Spawned = Time.time;

        void Update()
        {
            float t = Time.time - m_Spawned;
            if (m_Definition != null && m_Sprite != null && m_Definition.frames.Length > 0)
                m_Sprite.sprite = m_Definition.frames[Mathf.FloorToInt(t / m_Definition.frameSeconds) % m_Definition.frames.Length];
            // It rocks a little, as if it might go off.
            if (m_Sprite != null) m_Sprite.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 5f) * 6f);
            // The fuse flickers; under it, a slow swell that catches the eye across a dark room.
            if (m_Glow != null) m_Glow.intensity = m_GlowIntensity * (0.55f + 0.25f * Mathf.PerlinNoise(t * 9f, 0.3f) + 0.35f * (0.5f + 0.5f * Mathf.Sin(t * 3f)));
            if (m_Marker != null) m_Marker.localPosition = new Vector3(0f, 0.95f + Mathf.Round(Mathf.Sin(t * 4f) * 1.5f) / 8f, 0f);
        }

        void OnTriggerEnter2D(Collider2D other) => Take(other);
        void OnTriggerStay2D(Collider2D other) => Take(other);

        void Take(Collider2D other)
        {
            DelveRunController run = DelveRunController.Active;
            if (m_Taken || m_Definition == null || Time.time - m_Spawned < m_Delay || run == null || run.IsEnding
                || !other.TryGetComponent(out SatchelCarrier _)) return;
            m_Taken = true;
            run.Loot.AddQuestObject(m_Definition.id);
            EventBus<QuestObjectFound>.Publish(new QuestObjectFound(m_Definition.id));
            if (m_Feedback != null)
            {
                m_Feedback.transform.SetParent(null, true);
                m_Feedback.PlayFeedbacks(transform.position);
                Destroy(m_Feedback.gameObject, 2f);
            }
            Destroy(gameObject);
        }
    }
}
