using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Core.Random;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Run;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace Hearthdelve.Dungeon.Powers
{
    /// <summary>
    /// A power room's reward (4d step 4): a spark on the floor once the room is clear. Stepping onto it pauses the run
    /// and offers three powers it hasn't taken; choosing one keeps it until the delve ends. Backing out leaves the spark
    /// where it is, to come back to (it stays behind with the room if the player leaves).
    /// </summary>
    public sealed class PowerPickup : MonoBehaviour
    {
        [SerializeField, Tooltip("The spark, bobbing.")]
        Transform m_Sprite;
        [SerializeField, Tooltip("Taking a power: sound, haptic and a flash, together.")]
        MMF_Player m_Feedback;
        [SerializeField, Min(0f), Tooltip("Seconds before it can be touched, so it's seen appearing.")]
        float m_Delay = 0.35f;
        [SerializeField, Min(1)] int m_Choices = 3;

        RunPowerDefinition[] m_Pool = System.Array.Empty<RunPowerDefinition>();
        int m_Seed;
        float m_Spawned;
        bool m_Armed = true;
        bool m_Offering;
        bool m_Taken;

        public bool IsOffering => m_Offering;

        public void Configure(Transform sprite, MMF_Player feedback)
        {
            m_Sprite = sprite;
            m_Feedback = feedback;
        }

        /// <summary>The powers it can offer, and the seed its offer is drawn with.</summary>
        public void Setup(RunPowerDefinition[] pool, int seed)
        {
            m_Pool = pool ?? System.Array.Empty<RunPowerDefinition>();
            m_Seed = seed;
        }

        /// <summary>What it would offer this run now.</summary>
        public List<RunPowerDefinition> Options(RunPowers powers) => powers.Offer(m_Pool, m_Choices, new SeededRandom(m_Seed));

        void OnEnable() => m_Spawned = Time.time;

        void Update()
        {
            if (m_Sprite != null) m_Sprite.localPosition = new Vector3(0f, 0.3f + Mathf.Sin((Time.time - m_Spawned) * 3f) * 0.08f, 0f);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            DelveRunController run = DelveRunController.Active;
            if (!m_Armed || m_Offering || m_Taken || Time.time - m_Spawned < m_Delay || run == null || run.IsEnding || !other.TryGetComponent(out SatchelCarrier _)) return;
            m_Armed = false;
            Offer(run);
        }

        // Backing out of the offer and stepping off lets the spark offer again.
        void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent(out SatchelCarrier _)) m_Armed = true;
        }

        void Offer(DelveRunController run)
        {
            List<RunPowerDefinition> options = Options(run.Powers);
            if (options.Count == 0)
            {
                Destroy(gameObject);
                return;
            }
            // No UI to ask with (tests of the run alone): the first is taken.
            if (EventBus<RunPowerOfferRequested>.HandlerCount == 0)
            {
                Choose(run, options[0]);
                return;
            }
            m_Offering = true;
            MenuPause.Push();
            InputMaps.ActivateUIOnly();
            EventBus<RunPowerOfferRequested>.Publish(new RunPowerOfferRequested(options, chosen =>
            {
                m_Offering = false;
                MenuPause.Pop();
                InputMaps.Activate(InputMaps.Dungeon);
                if (chosen != null) Choose(run, chosen);
            }));
        }

        void Choose(DelveRunController run, RunPowerDefinition power)
        {
            if (m_Taken || !run.TakePower(power)) return;
            m_Taken = true;
            if (m_Feedback != null)
            {
                // The sound outlives the spark.
                m_Feedback.transform.SetParent(null, true);
                m_Feedback.PlayFeedbacks(transform.position);
                Destroy(m_Feedback.gameObject, 2f);
            }
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (!m_Offering) return;
            m_Offering = false;
            MenuPause.Pop();
        }
    }
}
