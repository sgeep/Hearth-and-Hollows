using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.Tavern.Staff;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// Points on the tavern floor (2D, on the walkable grid): the door customers come and go by, where
    /// they queue for a seat, the seats, and where staff stand to work each job. Scene data; the
    /// service decides who goes where.
    /// </summary>
    public sealed class TavernLayout : MonoBehaviour
    {
        [SerializeField, Tooltip("Just inside the front door: where customers appear and leave.")]
        Transform m_Door;
        [SerializeField, Tooltip("Queue spots, front first.")]
        Transform[] m_Queue = System.Array.Empty<Transform>();
        [SerializeField, Tooltip("Seats in the order they're bought (the first ones are open from the start).")]
        TavernSeat[] m_Seats = System.Array.Empty<TavernSeat>();
        [SerializeField] Transform m_ServingPost;
        [SerializeField] Transform m_GrillPost;
        [SerializeField] Transform m_TapPost;
        [SerializeField] Transform m_StewPotPost;
        [SerializeField, Tooltip("Where staff wait when off duty.")]
        Transform m_RestPost;

        public Vector2 Door => m_Door != null ? (Vector2)m_Door.position : Vector2.zero;
        public int SeatCount => m_Seats.Length;
        public int QueueLength => m_Queue.Length;
        public int ActiveSeats { get; private set; }
        public IReadOnlyList<TavernSeat> Seats => m_Seats;

        public void Configure(Transform door, Transform[] queue, TavernSeat[] seats, Transform servingPost, Transform grillPost, Transform tapPost,
            Transform stewPotPost, Transform restPost)
        {
            m_Door = door;
            m_Queue = queue;
            m_Seats = seats;
            m_ServingPost = servingPost;
            m_GrillPost = grillPost;
            m_TapPost = tapPost;
            m_StewPotPost = stewPotPost;
            m_RestPost = restPost;
        }

        void Awake() => ActiveSeats = m_Seats.Length;

        public TavernSeat Seat(int index) => index >= 0 && index < m_Seats.Length ? m_Seats[index] : null;

        /// <summary>Where the customer in queue place <paramref name="place"/> (0 = front) stands. Extra people stand at the last spot.</summary>
        public Vector2 QueueSpot(int place) =>
            m_Queue.Length == 0 ? Door : (Vector2)m_Queue[Mathf.Clamp(place, 0, m_Queue.Length - 1)].position;

        public Vector2 PostFor(StaffStation station)
        {
            Transform post = station switch
            {
                StaffStation.Serving => m_ServingPost,
                StaffStation.Grill => m_GrillPost,
                StaffStation.Tap => m_TapPost,
                StaffStation.StewPot => m_StewPotPost,
                _ => m_RestPost,
            };
            return post != null ? (Vector2)post.position : Door;
        }

        /// <summary>
        /// Opens the first <paramref name="count"/> seats. A table whose seats are all closed is put away
        /// (its furniture hidden and no longer solid), and the walkable grid is told the layout changed.
        /// </summary>
        public void SetActiveSeats(int count)
        {
            ActiveSeats = Mathf.Clamp(count, 0, m_Seats.Length);
            var used = new HashSet<GameObject>();
            for (int i = 0; i < m_Seats.Length; i++)
            {
                m_Seats[i].SetActive(i < ActiveSeats);
                if (i < ActiveSeats) used.UnionWith(m_Seats[i].Furniture);
            }
            bool changed = false;
            foreach (TavernSeat seat in m_Seats)
            foreach (GameObject piece in seat.Furniture)
            {
                if (piece == null) continue;
                bool show = used.Contains(piece);
                if (piece.activeSelf == show) continue;
                piece.SetActive(show);
                changed = true;
            }
            if (changed) EventBus<NavigationLayoutChanged>.Publish(new NavigationLayoutChanged());
        }
    }
}
