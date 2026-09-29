using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// Positions on the tavern floor. The tavern is a side view, so everything lives on the
    /// x axis: door on the left, tables in the middle, the bar and kitchen on the right.
    /// </summary>
    public sealed class TavernLayout : MonoBehaviour
    {
        [SerializeField] Transform m_Door;
        [SerializeField] Transform[] m_Seats = new Transform[0];
        [SerializeField, Tooltip("Where the first queueing customer stands; later ones line up toward the door.")]
        Transform m_QueueFront;
        [SerializeField, Min(0.1f)] float m_QueueSpacing = 0.7f;
        [SerializeField] Station m_Grill;
        [SerializeField] Station m_Tap;
        [SerializeField] Station m_Pass;
        [SerializeField] float m_MinX = 0.5f;
        [SerializeField] float m_MaxX = 19.5f;

        public int SeatCount => m_Seats.Length;
        public float DoorX => m_Door != null ? m_Door.position.x : 0f;
        public float ExitX => DoorX - 1.5f;
        public float FloorY => m_Door != null ? m_Door.position.y : 0f;
        public Station Grill => m_Grill;
        public Station Tap => m_Tap;
        public Station Pass => m_Pass;
        public float MinX => m_MinX;
        public float MaxX => m_MaxX;

        public void Configure(Transform door, Transform[] seats, Transform queueFront, Station grill, Station tap, Station pass, float minX, float maxX)
        {
            m_Door = door;
            m_Seats = seats;
            m_QueueFront = queueFront;
            m_Grill = grill;
            m_Tap = tap;
            m_Pass = pass;
            m_MinX = minX;
            m_MaxX = maxX;
        }

        public float SeatX(int seat) => seat >= 0 && seat < m_Seats.Length ? m_Seats[seat].position.x : DoorX;

        public float QueueX(int place) => (m_QueueFront != null ? m_QueueFront.position.x : DoorX + 1f) - place * m_QueueSpacing;

        public Station StationFor(StationKind kind) => kind switch
        {
            StationKind.Grill => m_Grill,
            StationKind.Tap => m_Tap,
            _ => m_Pass,
        };
    }
}
