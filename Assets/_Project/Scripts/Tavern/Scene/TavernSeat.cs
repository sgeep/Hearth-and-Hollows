using Hearthdelve.Core.Movement;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// A chair a customer can sit on: where they sit (this object, at the chair's sort point), where
    /// they step from to sit down (a walkable spot beside it), and which way they face (the table).
    /// The chair itself is solid; a customer steps onto it from the approach point. Made by
    /// <see cref="AreaFurniture"/> for each usable seat of the placed furniture.
    /// </summary>
    public sealed class TavernSeat : MonoBehaviour
    {
        [SerializeField, Tooltip("Where a customer walks to before sitting, relative to the seat.")]
        Vector2 m_Approach = new(0f, -0.9f);
        [SerializeField] Facing4 m_Facing = Facing4.FrontRight;

        /// <summary>Where the customer's feet go when seated: just in front of the chair, so they're drawn over it.</summary>
        public Vector2 SitPoint => (Vector2)transform.position + new Vector2(0f, -0.02f);
        public Vector2 ApproachPoint => (Vector2)transform.position + m_Approach;
        public Facing4 Facing => m_Facing;

        public void Configure(Vector2 approach, Facing4 facing)
        {
            m_Approach = approach;
            m_Facing = facing;
        }

        /// <summary>The drawn facing for a sitter looking along <paramref name="direction"/> (east and south face front-right, west front-left, north back-right).</summary>
        public static Facing4 FacingFor(Vector2Int direction) =>
            direction.x < 0 ? Facing4.FrontLeft : direction.y > 0 && direction.x == 0 ? Facing4.BackRight : Facing4.FrontRight;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, ApproachPoint);
            Gizmos.DrawWireSphere(ApproachPoint, 0.15f);
        }
    }
}
