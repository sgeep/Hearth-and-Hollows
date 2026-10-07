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
        [SerializeField, Tooltip("Its tile is reserved in its room: it lies exactly there, always shown (2026-10-07: the keeper's room).")]
        bool m_FixedSpot;
        [SerializeField, Tooltip("Outside arrival day: the prompt to look at it (UI key) and the line it plays.")]
        string m_LookKey;
        [SerializeField] string m_LookConversation;

        TavernDirector m_Director;

        public TavernInteractable Interactable => m_Interactable;
        public bool IsOpen => m_Visual != null && m_Visual.activeSelf;

        public void Configure(TavernInteractable interactable, GameObject visual, Vector2 preferred, bool fixedSpot = false, string lookKey = null,
            string lookConversation = null)
        {
            m_Interactable = interactable;
            m_Visual = visual;
            m_Preferred = preferred;
            m_FixedSpot = fixedSpot;
            m_LookKey = lookKey;
            m_LookConversation = lookConversation;
        }

        bool Arriving => m_Director != null && m_Director.Phase == TavernPhase.Arrival;

        void Start()
        {
            m_Director = TavernDirector.Instance;
            if (m_Interactable != null)
            {
                m_Interactable.Used += _ => Use();
                // Arrival day: down into the Hollows; any other free daytime: a look at it.
                if (m_FixedSpot) m_Interactable.Describe = () => TavernHint.Use(Arriving || string.IsNullOrEmpty(m_LookKey) ? m_Interactable.NameKey : m_LookKey);
            }
            if (m_Director != null) m_Director.PhaseChanged += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (m_Director != null) m_Director.PhaseChanged -= Refresh;
        }

        void Use()
        {
            if (Arriving) m_Director?.GoDownHatch();
            else if (!string.IsNullOrEmpty(m_LookConversation)) Hearthdelve.Shared.Story.StoryServices.Conversations?.Play(m_LookConversation);
        }

        void Refresh()
        {
            bool arriving = Arriving;
            bool daytime = m_Director != null && m_Director.Phase == TavernPhase.Daytime;
            // A fixed hatch is always there (a hole in the keeper's floor); the old one appeared only on arrival day.
            bool shown = m_FixedSpot || arriving;
            if (shown) Place();
            if (m_Visual != null) m_Visual.SetActive(shown);
            bool usable = arriving || (m_FixedSpot && daytime && !string.IsNullOrEmpty(m_LookConversation));
            if (m_Interactable != null)
            {
                m_Interactable.gameObject.SetActive(shown);
                m_Interactable.SetAvailable(usable);
            }
        }

        void Update()
        {
            // Not while someone's talking (a look is a conversation too).
            if (!m_FixedSpot || m_Interactable == null) return;
            bool talking = Hearthdelve.Shared.Story.StoryServices.Conversations != null && Hearthdelve.Shared.Story.StoryServices.Conversations.IsTalking;
            bool usable = !talking && (Arriving || (m_Director != null && m_Director.Phase == TavernPhase.Daytime && !string.IsNullOrEmpty(m_LookConversation)));
            if (usable != m_Interactable.IsAvailable) m_Interactable.SetAvailable(usable);
        }

        /// <summary>The open tile nearest the preferred spot, with an open tile under it for the keeper to stand on.</summary>
        void Place()
        {
            if (m_FixedSpot)
            {
                transform.position = m_Preferred;
                return;
            }
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
