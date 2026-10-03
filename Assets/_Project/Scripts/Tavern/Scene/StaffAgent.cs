using Hearthdelve.Shared.Animation;
using Hearthdelve.Tavern.Staff;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// A member of staff (Pip) as a TDE character: walks on the grid, through the same thin pathfinding
    /// AI action as customers, to the post of the job they're given, and waits there facing the room.
    /// Doing the job (cooking at a station, carrying plates) comes with the stations and serving (4c step 3).
    /// Like customers, staff don't collide with the player.
    /// </summary>
    public sealed class StaffAgent : MonoBehaviour
    {
        const float k_Arrive = 0.2f;
        const float k_Resume = 0.35f;

        AIBrain m_Brain;
        CharacterMovement m_Movement;
        Transform m_Goal;
        TavernLayout m_Layout;

        public StaffDefinition Member { get; private set; }
        public StaffStation Assignment { get; private set; } = StaffStation.None;
        /// <summary>Standing at the post of their job.</summary>
        public bool AtPost { get; private set; }
        public Vector2 Post => m_Layout != null ? m_Layout.PostFor(Assignment) : (Vector2)transform.position;

        void Awake()
        {
            m_Brain = GetComponent<AIBrain>();
            m_Movement = GetComponent<Character>()?.FindAbility<CharacterMovement>();
            m_Goal = new GameObject($"{name}_Goal").transform;
            m_Goal.position = transform.position;
            if (m_Brain != null) m_Brain.Target = m_Goal;
        }

        void OnDestroy()
        {
            if (m_Goal != null) Destroy(m_Goal.gameObject);
        }

        /// <summary>Starts the evening at a job (None: they wait out of the way).</summary>
        public void Begin(StaffStation assignment, StaffDefinition member, TavernLayout layout)
        {
            Member = member;
            Assignment = member != null ? assignment : StaffStation.None;
            m_Layout = layout;
            AtPost = false;
            if (m_Brain != null) m_Brain.BrainActive = true;
        }

        void Update()
        {
            if (m_Layout == null) return;
            m_Goal.position = Post;
            float distance = Vector2.Distance(transform.position, m_Goal.position);
            // At the post: stand still with the AI off. The walk action alone overshoots a point, turns back and
            // overshoots again each frame (Pip "vibrating" at the pass, step 2 playtest). Walk again once the post moves away.
            if (!AtPost && distance <= k_Arrive)
            {
                AtPost = true;
                if (m_Brain != null) m_Brain.BrainActive = false;
                m_Movement?.SetMovement(Vector2.zero);
            }
            else if (AtPost && distance > k_Resume)
            {
                AtPost = false;
                if (m_Brain != null) m_Brain.BrainActive = true;
            }
        }
    }
}
