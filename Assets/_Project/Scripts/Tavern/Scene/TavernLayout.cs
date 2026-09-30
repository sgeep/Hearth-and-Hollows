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
        [SerializeField, Tooltip("Stool sprite per seat, hidden while the seat isn't bought.")]
        SpriteRenderer[] m_Stools = new SpriteRenderer[0];
        [SerializeField, Tooltip("Table sprite per pair of seats, hidden while neither seat is bought.")]
        SpriteRenderer[] m_Tables = new SpriteRenderer[0];
        [SerializeField, Tooltip("Where the first queueing customer stands; later ones line up toward the door.")]
        Transform m_QueueFront;
        [SerializeField, Min(0.1f)] float m_QueueSpacing = 0.7f;
        [SerializeField] Station m_Grill;
        [SerializeField] Station m_Tap;
        [SerializeField] Station m_Pass;
        [SerializeField] Station m_StewPot;
        [SerializeField] float m_MinX = 0.5f;
        [SerializeField] float m_MaxX = 19.5f;

        /// <summary>Seats the room holds (bought or not).</summary>
        public int SeatCount => m_Seats.Length;
        public int ActiveSeats { get; private set; } = int.MaxValue;
        public float DoorX => m_Door != null ? m_Door.position.x : 0f;
        public float ExitX => DoorX - 1.5f;
        public float FloorY => m_Door != null ? m_Door.position.y : 0f;
        public Station Grill => m_Grill;
        public Station Tap => m_Tap;
        public Station Pass => m_Pass;
        public Station StewPot => m_StewPot;
        public float MinX => m_MinX;
        public float MaxX => m_MaxX;

        public void Configure(Transform door, Transform[] seats, Transform queueFront, Station grill, Station tap, Station pass, Station stewPot,
            float minX, float maxX)
        {
            m_Door = door;
            m_Seats = seats;
            m_QueueFront = queueFront;
            m_Grill = grill;
            m_Tap = tap;
            m_Pass = pass;
            m_StewPot = stewPot;
            m_MinX = minX;
            m_MaxX = maxX;
        }

        public void ConfigureProps(SpriteRenderer[] stools, SpriteRenderer[] tables)
        {
            m_Stools = stools;
            m_Tables = tables;
        }

        /// <summary>Shows the first <paramref name="count"/> seats' stools, and each table with at least one of them.</summary>
        public void SetActiveSeats(int count)
        {
            ActiveSeats = Mathf.Clamp(count, 0, m_Seats.Length);
            for (int i = 0; i < m_Stools.Length; i++)
                if (m_Stools[i] != null) m_Stools[i].enabled = i < ActiveSeats;
            for (int t = 0; t < m_Tables.Length; t++)
                if (m_Tables[t] != null) m_Tables[t].enabled = t * 2 < ActiveSeats;
        }

        public float SeatX(int seat) => seat >= 0 && seat < m_Seats.Length ? m_Seats[seat].position.x : DoorX;

        public float QueueX(int place) => (m_QueueFront != null ? m_QueueFront.position.x : DoorX + 1f) - place * m_QueueSpacing;

        public Station StationFor(StationKind kind) => kind switch
        {
            StationKind.Grill => m_Grill,
            StationKind.Tap => m_Tap,
            StationKind.StewPot => m_StewPot,
            _ => m_Pass,
        };
    }
}
