using Hearthdelve.Core.Pathfinding;
using Hearthdelve.Shared.Navigation;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// The cellar hatch, on arrival day (4g Checkpoint B, the Act I opening): the way down to the first delve, used like any station
    /// ("go down into the Hollows"). Shown only while the keeper is arriving; afterwards the day's delve begins as it always has.
    /// It lies on an open floor tile near its authored spot, so the starting layout (or a moved table) never covers it.
    /// </summary>
    public sealed class OpeningHatch : MonoBehaviour
    {
        [SerializeField] TavernInteractable m_Interactable;
        [SerializeField] GameObject m_Visual;
        [SerializeField, Tooltip("Where it would like to be (world); the nearest open tile is used.")]
        Vector2 m_Preferred = new(5.5f, 4.5f);
        [SerializeField, Min(1)] int m_SearchRadius = 6;

        TavernDirector m_Director;

        public TavernInteractable Interactable => m_Interactable;
        public bool IsOpen => m_Visual != null && m_Visual.activeSelf;

        public void Configure(TavernInteractable interactable, GameObject visual, Vector2 preferred)
        {
            m_Interactable = interactable;
            m_Visual = visual;
            m_Preferred = preferred;
        }

        void Start()
        {
            m_Director = TavernDirector.Instance;
            if (m_Interactable != null) m_Interactable.Used += _ => m_Director?.GoDownHatch();
            if (m_Director != null) m_Director.PhaseChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (m_Director != null) m_Director.PhaseChanged -= Refresh;
        }

        void Refresh()
        {
            bool open = m_Director != null && m_Director.Phase == TavernPhase.Arrival;
            if (open) Place();
            if (m_Visual != null) m_Visual.SetActive(open);
            if (m_Interactable != null)
            {
                m_Interactable.gameObject.SetActive(open);
                m_Interactable.SetAvailable(open);
            }
        }

        /// <summary>The open tile nearest the preferred spot, with an open tile under it for the keeper to stand on.</summary>
        void Place()
        {
            NavGrid grid = NavGrid.Current;
            if (grid == null || !grid.IsBaked)
            {
                transform.position = m_Preferred;
                return;
            }
            GridSpace space = grid.Space;
            GridCell start = space.ToCell(m_Preferred);
            for (int r = 0; r <= m_SearchRadius; r++)
            for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != r) continue;
                var cell = new GridCell(start.X + dx, start.Y + dy);
                if (!grid.Map.IsWalkable(cell) || !grid.Map.IsWalkable(new GridCell(cell.X, cell.Y - 1))) continue;
                transform.position = space.CellCentre(cell);
                return;
            }
            transform.position = m_Preferred;
        }
    }
}
