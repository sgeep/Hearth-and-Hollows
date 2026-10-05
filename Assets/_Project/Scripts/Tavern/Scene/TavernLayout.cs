using System.Collections.Generic;
using Hearthdelve.Tavern.Staff;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// Points on the tavern floor (2D, on the walkable grid) that the service uses: the door customers come and go by,
    /// where they queue for a seat, where staff rest (all part of the room's fixed structure), and, from the placed
    /// furniture, the seats and where staff stand to work each job (4f: <see cref="AreaFurniture"/> sets them whenever
    /// the layout is built). The service decides who goes where.
    /// </summary>
    public sealed class TavernLayout : MonoBehaviour
    {
        [SerializeField, Tooltip("Just inside the front door: where customers appear and leave.")]
        Transform m_Door;
        [SerializeField, Tooltip("Queue spots, front first.")]
        Transform[] m_Queue = System.Array.Empty<Transform>();
        [SerializeField, Tooltip("Where staff wait when off duty.")]
        Transform m_RestPost;

        readonly List<TavernSeat> m_Seats = new();
        readonly Dictionary<StaffStation, Vector2> m_Posts = new();

        public Vector2 Door => m_Door != null ? (Vector2)m_Door.position : Vector2.zero;
        public int SeatCount => m_Seats.Count;
        public int QueueLength => m_Queue.Length;
        /// <summary>Usable seats, numbered in the order their pieces were placed.</summary>
        public IReadOnlyList<TavernSeat> Seats => m_Seats;

        public void Configure(Transform door, Transform[] queue, Transform restPost)
        {
            m_Door = door;
            m_Queue = queue;
            m_RestPost = restPost;
        }

        /// <summary>The seats and staff posts of the placed furniture (replacing any from an earlier build).</summary>
        public void SetFurniture(IEnumerable<TavernSeat> seats, IReadOnlyDictionary<StaffStation, Vector2> posts)
        {
            m_Seats.Clear();
            m_Seats.AddRange(seats);
            m_Posts.Clear();
            foreach (var pair in posts) m_Posts[pair.Key] = pair.Value;
        }

        /// <summary>The queue spots, front first, and where staff rest (the area's layout check uses them).</summary>
        public IEnumerable<Vector2> QueueSpots
        {
            get
            {
                foreach (Transform spot in m_Queue)
                    if (spot != null) yield return spot.position;
            }
        }

        public Vector2 RestPost => m_RestPost != null ? (Vector2)m_RestPost.position : Door;

        public TavernSeat Seat(int index) => index >= 0 && index < m_Seats.Count ? m_Seats[index] : null;

        /// <summary>Where the customer in queue place <paramref name="place"/> (0 = front) stands. Extra people stand at the last spot.</summary>
        public Vector2 QueueSpot(int place) =>
            m_Queue.Length == 0 ? Door : (Vector2)m_Queue[Mathf.Clamp(place, 0, m_Queue.Length - 1)].position;

        /// <summary>Where staff stand for a job: its station's post, or the rest post (also for a station that isn't out).</summary>
        public Vector2 PostFor(StaffStation station)
        {
            if (station != StaffStation.None && m_Posts.TryGetValue(station, out Vector2 post)) return post;
            return m_RestPost != null ? (Vector2)m_RestPost.position : Door;
        }
    }
}
