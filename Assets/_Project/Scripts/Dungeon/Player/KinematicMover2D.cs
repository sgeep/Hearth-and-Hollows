using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Dungeon.Player
{
    /// <summary>
    /// Custom kinematic collision for a box-shaped character. Moves by box-casting against
    /// solid geometry, axis by axis, and handles one-way platforms itself (only blocking
    /// downward movement that starts above the platform, unless drop-through is active).
    /// Reports contacts for the motor. Shared by the player and ground enemies.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class KinematicMover2D : MonoBehaviour
    {
        [SerializeField] LayerMask m_SolidMask;
        [SerializeField] LayerMask m_OneWayMask;
        [SerializeField, Tooltip("Gap kept between the box and geometry to avoid snagging.")]
        float m_Skin = 0.02f;
        [SerializeField, Tooltip("How far contact probes reach beyond the skin.")]
        float m_ProbeDistance = 0.04f;

        const float k_GroundNormal = 0.7f;
        const float k_WallNormal = 0.7f;

        Rigidbody2D m_Body;
        BoxCollider2D m_Box;
        Vector2 m_Position;
        readonly List<RaycastHit2D> m_Hits = new(8);

        public MotorCollisions Contacts { get; private set; }

        /// <summary>When true, one-way platforms don't block or count as ground.</summary>
        public bool IgnoreOneWay { get; set; }

        public Vector2 Position => m_Position;
        public Bounds Bounds => new(m_Position + m_Box.offset, m_Box.size);
        public LayerMask SolidMask => m_SolidMask;

        public void Configure(LayerMask solid, LayerMask oneWay)
        {
            m_SolidMask = solid;
            m_OneWayMask = oneWay;
        }

        void Awake()
        {
            m_Body = GetComponent<Rigidbody2D>();
            m_Box = GetComponent<BoxCollider2D>();
            m_Body.bodyType = RigidbodyType2D.Kinematic;
            m_Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            m_Position = m_Body.position;
        }

        void OnEnable()
        {
            if (m_Body != null) m_Position = m_Body.position;
        }

        /// <summary>Place the body without collision (spawns, respawns).</summary>
        public void Teleport(Vector2 position)
        {
            m_Position = position;
            m_Body.position = position;
            transform.position = position;
            RefreshContacts();
        }

        /// <summary>Move by <paramref name="delta"/>, stopping at geometry. Call from FixedUpdate.</summary>
        public Vector2 Move(Vector2 delta)
        {
            Vector2 start = m_Position;

            if (!Mathf.Approximately(delta.x, 0f))
                m_Position.x += CastDistance(new Vector2(Mathf.Sign(delta.x), 0f), Mathf.Abs(delta.x), m_SolidMask, false);

            if (!Mathf.Approximately(delta.y, 0f))
            {
                bool down = delta.y < 0f;
                int mask = down && !IgnoreOneWay ? (m_SolidMask | m_OneWayMask) : m_SolidMask.value;
                m_Position.y += CastDistance(new Vector2(0f, Mathf.Sign(delta.y)), Mathf.Abs(delta.y), mask, down);
            }

            m_Body.MovePosition(m_Position);
            RefreshContacts();
            return m_Position - start;
        }

        /// <summary>Signed distance we can travel along <paramref name="dir"/> (≤ distance).</summary>
        float CastDistance(Vector2 dir, float distance, LayerMask mask, bool oneWayAllowed)
        {
            float allowed = distance;
            var hitCount = Cast(dir, distance + m_Skin, mask);
            for (int i = 0; i < hitCount; i++)
            {
                var hit = m_Hits[i];
                if (!IsBlocking(hit, dir, oneWayAllowed)) continue;
                allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - m_Skin));
            }
            return allowed * (dir.x + dir.y);
        }

        bool IsBlocking(in RaycastHit2D hit, Vector2 dir, bool oneWayAllowed)
        {
            bool isOneWay = ((1 << hit.collider.gameObject.layer) & m_OneWayMask) != 0;
            if (!isOneWay) return true;
            // One-way: only block when falling onto its top from above (not while overlapping it).
            return oneWayAllowed && dir.y < 0f && hit.distance > 0f && hit.normal.y > k_GroundNormal;
        }

        int Cast(Vector2 dir, float distance, LayerMask mask)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(mask);
            // Inset the box on the axis we're not moving along so we don't catch perpendicular walls.
            Vector2 size = m_Box.size - (dir.x != 0f ? new Vector2(m_Skin * 2f, m_Skin * 4f) : new Vector2(m_Skin * 4f, m_Skin * 2f));
            return Physics2D.BoxCast(m_Position + m_Box.offset, size, 0f, dir, filter, m_Hits, distance);
        }

        void RefreshContacts()
        {
            float reach = m_Skin + m_ProbeDistance;
            var contacts = new MotorCollisions();

            int groundMask = IgnoreOneWay ? m_SolidMask.value : (m_SolidMask | m_OneWayMask);
            int n = Cast(Vector2.down, reach, groundMask);
            bool solidGround = false, oneWayGround = false;
            for (int i = 0; i < n; i++)
            {
                var hit = m_Hits[i];
                if (hit.normal.y < k_GroundNormal) continue;
                bool isOneWay = ((1 << hit.collider.gameObject.layer) & m_OneWayMask) != 0;
                if (isOneWay) { if (hit.distance > 0f) oneWayGround = true; }
                else solidGround = true;
            }
            contacts.Grounded = solidGround || oneWayGround;
            contacts.OnOneWay = oneWayGround && !solidGround;

            contacts.WallLeft = HasWall(Vector2.left, reach);
            contacts.WallRight = HasWall(Vector2.right, reach);

            n = Cast(Vector2.up, reach, m_SolidMask);
            for (int i = 0; i < n; i++)
                if (m_Hits[i].normal.y < -k_GroundNormal) contacts.Ceiling = true;

            Contacts = contacts;
        }

        bool HasWall(Vector2 dir, float reach)
        {
            int n = Cast(dir, reach, m_SolidMask);
            for (int i = 0; i < n; i++)
                if (Mathf.Abs(m_Hits[i].normal.x) > k_WallNormal) return true;
            return false;
        }

        /// <summary>Is there solid or one-way ground just ahead of the box (for ledge-aware AI)?</summary>
        public bool HasGroundAhead(int direction, float lookAhead = 0.3f)
        {
            var bounds = Bounds;
            var origin = new Vector2(direction > 0 ? bounds.max.x + lookAhead : bounds.min.x - lookAhead, bounds.min.y + 0.1f);
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(m_SolidMask | m_OneWayMask);
            return Physics2D.Raycast(origin, Vector2.down, filter, m_Hits, 0.6f) > 0;
        }
    }
}
