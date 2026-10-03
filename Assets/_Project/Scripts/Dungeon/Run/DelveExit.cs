using System.Collections;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Dungeon.Essence;
using Hearthdelve.Dungeon.Harvest;
using Hearthdelve.Shared.Run;
using MoreMountains.Feedbacks;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Run
{
    /// <summary>
    /// The way out (GDD §4.4): a rope back up to the tavern. Standing at it shows the hint; pressing
    /// Interact climbs out. The climb can't be hurt or drained: Essence stops, gameplay input stops,
    /// and the player rises up the rope and fades, then the delve ends with the whole satchel
    /// (<see cref="DelveRunController.Extract"/>).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class DelveExit : MonoBehaviour
    {
        [SerializeField, Tooltip("Played as the climb starts: sound and haptic together.")]
        MMF_Player m_ClimbFeedback;
        [SerializeField, Min(0.1f), Tooltip("How long the climb takes, in seconds.")]
        float m_ClimbTime = 0.8f;
        [SerializeField, Min(0f), Tooltip("How far up the rope the player rises as they fade, in tiles.")]
        float m_ClimbHeight = 1.6f;

        SatchelCarrier m_Inside;
        bool m_HintShown;

        public bool IsClimbing { get; private set; }
        public bool PlayerAtExit => m_Inside != null;

        public void Configure(MMF_Player climbFeedback) => m_ClimbFeedback = climbFeedback;

        void OnTriggerEnter2D(Collider2D other) => Enter(other);
        void OnTriggerStay2D(Collider2D other) => Enter(other);

        void Enter(Collider2D other)
        {
            if (IsClimbing || !other.TryGetComponent(out SatchelCarrier carrier)) return;
            m_Inside = carrier;
            SetHint(true);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!other.TryGetComponent(out SatchelCarrier carrier) || carrier != m_Inside) return;
            m_Inside = null;
            SetHint(false);
        }

        void OnDisable() => SetHint(false);

        void Update()
        {
            if (m_Inside == null || IsClimbing || m_Inside.IsPrompting) return;
            DelveRunController run = DelveRunController.Active;
            if (run == null || run.IsEnding) return;
            var health = m_Inside.GetComponent<Health>();
            if (health != null && health.CurrentHealth <= 0f) return;
            var interact = InputMaps.Find(InputMaps.Dungeon, DungeonActions.Interact);
            if (interact != null && interact.WasPressedThisFrame()) StartCoroutine(Climb(m_Inside));
        }

        IEnumerator Climb(SatchelCarrier player)
        {
            IsClimbing = true;
            SetHint(false);
            InputMaps.ActivateUIOnly();
            if (player.TryGetComponent(out EssenceHealth essence))
            {
                essence.DrainPaused = true;
                essence.DamageDisabled();
            }
            player.GetComponent<CharacterMovement>()?.SetMovement(Vector2.zero);
            m_ClimbFeedback?.PlayFeedbacks(transform.position);

            // Presentation only: the sprites rise up the rope and fade; the character stays put.
            var character = player.GetComponent<Character>();
            SpriteRenderer[] sprites = character != null && character.CharacterModel != null
                ? character.CharacterModel.GetComponentsInChildren<SpriteRenderer>()
                : player.GetComponentsInChildren<SpriteRenderer>();
            var rest = new Vector3[sprites.Length];
            for (int i = 0; i < sprites.Length; i++) rest[i] = sprites[i].transform.localPosition;
            float started = Time.time;
            while (Time.time - started < m_ClimbTime)
            {
                float t = (Time.time - started) / m_ClimbTime;
                for (int i = 0; i < sprites.Length; i++)
                {
                    sprites[i].transform.localPosition = rest[i] + Vector3.up * (t * m_ClimbHeight);
                    Color color = sprites[i].color;
                    color.a = 1f - t;
                    sprites[i].color = color;
                }
                yield return null;
            }
            DelveRunController.Active?.Extract();
        }

        void SetHint(bool visible)
        {
            if (m_HintShown == visible) return;
            m_HintShown = visible;
            EventBus<DelveExitHint>.Publish(new DelveExitHint(visible));
        }
    }
}
