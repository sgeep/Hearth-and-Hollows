using Hearthdelve.Core.Movement;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// A chair a customer can sit on: where they sit (this object, at the chair's sort point), where
    /// they step from to sit down (a walkable spot beside it), and which way they face (the table).
    /// The chair itself is solid; a customer steps onto it from the approach point.
    /// </summary>
    public sealed class TavernSeat : MonoBehaviour
    {
        [SerializeField, Tooltip("Where a customer walks to before sitting, relative to the seat.")]
        Vector2 m_Approach = new(0f, -0.9f);
        [SerializeField] Facing4 m_Facing = Facing4.FrontRight;
        [SerializeField, Tooltip("The table and chairs this seat belongs to, hidden while the seat isn't bought.")]
        GameObject[] m_Furniture = System.Array.Empty<GameObject>();

        /// <summary>Where the customer's feet go when seated: just in front of the chair, so they're drawn over it.</summary>
        public Vector2 SitPoint => (Vector2)transform.position + new Vector2(0f, -0.02f);
        public Vector2 ApproachPoint => (Vector2)transform.position + m_Approach;
        public Facing4 Facing => m_Facing;
        public bool IsActive { get; private set; } = true;

        public void Configure(Vector2 approach, Facing4 facing, GameObject[] furniture)
        {
            m_Approach = approach;
            m_Facing = facing;
            m_Furniture = furniture;
        }

        /// <summary>Shows or hides this seat (and its table, once no seat at it is active).</summary>
        public void SetActive(bool active) => IsActive = active;

        public GameObject[] Furniture => m_Furniture;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, ApproachPoint);
            Gizmos.DrawWireSphere(ApproachPoint, 0.15f);
        }
    }
}
