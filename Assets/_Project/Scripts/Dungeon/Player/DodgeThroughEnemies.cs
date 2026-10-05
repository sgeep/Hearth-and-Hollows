using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Dungeon.Player
{
    /// <summary>
    /// The dodge roll passes through enemies (4e playtest): while dashing, the player's body ignores enemy bodies, so the roll
    /// neither shoves them nor stops against them. It stays ghosted until it no longer overlaps one, so a roll that ends
    /// inside an enemy doesn't pop either of them apart. Damage is unaffected: hitboxes are triggers, and the roll's i-frames
    /// are the dash's own.
    /// </summary>
    [RequireComponent(typeof(Character))]
    public sealed class DodgeThroughEnemies : MonoBehaviour
    {
        [SerializeField, Tooltip("Bodies the roll passes through.")]
        LayerMask m_PassThrough;

        Character m_Character;
        Collider2D m_Body;
        LayerMask m_Base;
        bool m_Ghosted;
        readonly Collider2D[] m_Overlaps = new Collider2D[4];

        public bool IsGhosted => m_Ghosted;

        public void Configure(LayerMask passThrough) => m_PassThrough = passThrough;

        void Awake()
        {
            m_Character = GetComponent<Character>();
            foreach (Collider2D candidate in GetComponents<Collider2D>())
                if (!candidate.isTrigger) { m_Body = candidate; break; }
            if (m_Body != null) m_Base = m_Body.excludeLayers;
        }

        void FixedUpdate()
        {
            if (m_Body == null) return;
            bool dashing = m_Character.MovementState != null && m_Character.MovementState.CurrentState == CharacterStates.MovementStates.Dashing;
            bool ghost = dashing || (m_Ghosted && OverlapsEnemy());
            if (ghost == m_Ghosted) return;
            m_Ghosted = ghost;
            m_Body.excludeLayers = ghost ? m_Base | m_PassThrough : m_Base;
        }

        void OnDisable()
        {
            if (m_Body != null) m_Body.excludeLayers = m_Base;
            m_Ghosted = false;
        }

        bool OverlapsEnemy()
        {
            var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = m_PassThrough };
            Bounds b = m_Body.bounds;
            return Physics2D.OverlapBox(b.center, b.size, 0f, filter, m_Overlaps) > 0;
        }
    }
}
