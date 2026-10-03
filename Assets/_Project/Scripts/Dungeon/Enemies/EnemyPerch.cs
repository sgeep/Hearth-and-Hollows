using Hearthdelve.Core;
using Hearthdelve.Shared.Animation;
using UnityEngine;

namespace Hearthdelve.Dungeon.Enemies
{
    /// <summary>
    /// A sleeping bat hangs from a wall: its sleep pose is drawn clinging to the brick face above its
    /// feet. Bats are placed at the top of a floor tile directly under a wall (the test floor's
    /// <c>V</c> markers). While hanging, its ground shadow is hidden. It lets go (<see cref="Detach"/>)
    /// when it wakes or is hit, playing its unfold animation, and then flies like any bat. A bat with
    /// no wall above it never shows the hanging pose: it hovers in its flying idle and logs a warning,
    /// so a misplaced bat is visible in the console rather than floating asleep in open space.
    /// </summary>
    [RequireComponent(typeof(EnemyIdentity))]
    public sealed class EnemyPerch : MonoBehaviour
    {
        [SerializeField, Min(0.1f), Tooltip("How far above the feet a wall must be for the bat to hang from it, in tiles.")]
        float m_ProbeHeight = 0.7f;
        [SerializeField, Tooltip("The ground shadow, hidden while hanging.")]
        GameObject m_Shadow;

        CharacterSpriteAnimator m_Animator;
        bool m_Checked;
        bool m_HasPerch;

        /// <summary>True while hanging asleep from its wall.</summary>
        public bool IsPerched { get; private set; }

        /// <summary>Whether there is a wall above the bat to hang from (checked when it first goes to sleep).</summary>
        public bool HasPerch
        {
            get
            {
                if (!m_Checked) Check();
                return m_HasPerch;
            }
        }

        public void Configure(GameObject shadow) => m_Shadow = shadow;

        void Awake() => m_Animator = GetComponentInChildren<CharacterSpriteAnimator>();

        /// <summary>Whether a wall or prop blocks the point <paramref name="height"/> tiles above <paramref name="feet"/>.</summary>
        public static bool WallAbove(Vector2 feet, float height)
        {
            var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = LayerMask.GetMask(Layers.Obstacles) };
            var hits = new Collider2D[1];
            return Physics2D.OverlapPoint(feet + Vector2.up * height, filter, hits) > 0;
        }

        void Check()
        {
            m_Checked = true;
            Physics2D.SyncTransforms();
            m_HasPerch = WallAbove(transform.position, m_ProbeHeight);
            if (!m_HasPerch)
                Debug.LogWarning($"[Hearthdelve] {name} at {(Vector2)transform.position} has no wall above it to hang from; it hovers instead. Place sleeping bats directly under a wall.", this);
        }

        /// <summary>Goes to sleep: hangs from the wall if there is one, otherwise hovers awake-looking in place.</summary>
        public void Hang()
        {
            IsPerched = HasPerch;
            if (!IsPerched)
            {
                // Hovering: the flying idle, with its shadow.
                m_Animator?.Release();
                if (m_Shadow != null) m_Shadow.SetActive(true);
                return;
            }
            m_Animator?.Hold(CharacterAnim.Sleep);
            if (m_Shadow != null) m_Shadow.SetActive(false);
        }

        /// <summary>Lets go of the wall: the unfold animation, then flight. Does nothing if not hanging.</summary>
        public void Detach()
        {
            if (!IsPerched) return;
            IsPerched = false;
            m_Animator?.Release();
            m_Animator?.PlayOneShot(CharacterAnim.Wake);
            if (m_Shadow != null) m_Shadow.SetActive(true);
        }
    }
}
