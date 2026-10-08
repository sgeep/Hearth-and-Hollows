using System.Collections.Generic;
using Hearthdelve.Core.Movement;
using Hearthdelve.Tavern.Scene;
using UnityEngine;

namespace Hearthdelve.Village
{
    /// <summary>
    /// A named place a schedule can send someone (4h Checkpoint C): <c>square.memorial</c>, <c>kaloren.tower</c>,
    /// <c>tavern.table</c>…, owned by the scene it's in, never coordinates in a character's code. The area says where the keeper
    /// has to be to see it. A window anchor hosts someone unseen (Ogrin in bed): they're talked to there, through the window.
    /// A tavern-table anchor is wherever the nearest seat of today's furniture is (the layout is the player's).
    /// </summary>
    public sealed class ScheduleAnchor : MonoBehaviour
    {
        static readonly List<ScheduleAnchor> s_All = new();

        [SerializeField] string m_Id;
        [SerializeField, Tooltip("The surface area it's in (kariaston, tavern).")] string m_Area = "kariaston";
        [SerializeField] Facing4 m_Facing = Facing4.FrontRight;
        [SerializeField, Tooltip("Someone here is indoors, unseen, and talked to through a window (the talk point is this anchor).")]
        bool m_Window;
        [SerializeField, Tooltip("Shown while someone is here (a window's glow).")] GameObject m_Occupied;
        [SerializeField, Tooltip("Tally Ho!: the spot is the nearest seat of the current furniture (Maximo's lunch).")]
        bool m_TavernSeat;

        public static IReadOnlyList<ScheduleAnchor> All => s_All;
        public string Id => m_Id;
        public string Area => m_Area;
        public bool Window => m_Window;

        public static ScheduleAnchor Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (ScheduleAnchor a in s_All)
                if (a.m_Id == id) return a;
            return null;
        }

        public void Configure(string id, string area, Facing4 facing, bool window = false, GameObject occupied = null, bool tavernSeat = false)
        {
            m_Id = id;
            m_Area = area;
            m_Facing = facing;
            m_Window = window;
            m_Occupied = occupied;
            m_TavernSeat = tavernSeat;
        }

        void OnEnable()
        {
            s_All.Add(this);
            SetOccupied(false);
        }

        void OnDisable() => s_All.Remove(this);

        TavernSeat Seat()
        {
            TavernDirector director = TavernDirector.Instance;
            if (!m_TavernSeat || director == null || director.Layout == null) return null;
            TavernSeat best = null;
            float bestDistance = float.MaxValue;
            foreach (TavernSeat seat in director.Layout.Seats)
            {
                if (seat == null || !seat.isActiveAndEnabled) continue;
                float d = ((Vector2)seat.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = seat;
                }
            }
            return best;
        }

        /// <summary>Where they stand (a seat's own spot when it's a table).</summary>
        public Vector2 Spot
        {
            get
            {
                TavernSeat seat = Seat();
                return seat != null ? seat.SitPoint : (Vector2)transform.position;
            }
        }

        /// <summary>Where they walk to before taking the spot (a seat's approach; otherwise the spot).</summary>
        public Vector2 Approach
        {
            get
            {
                TavernSeat seat = Seat();
                return seat != null ? seat.ApproachPoint : (Vector2)transform.position;
            }
        }

        public Facing4 Facing
        {
            get
            {
                TavernSeat seat = Seat();
                return seat != null ? seat.Facing : m_Facing;
            }
        }

        public void SetOccupied(bool occupied)
        {
            if (m_Occupied != null && m_Occupied.activeSelf != occupied) m_Occupied.SetActive(occupied);
            // 2026-10-08: the window itself, lit while someone's behind it.
            if (m_Window) LitWindow.Set(m_Id, occupied);
        }
    }
}
