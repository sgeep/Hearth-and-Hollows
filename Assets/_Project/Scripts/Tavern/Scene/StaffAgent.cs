using Hearthdelve.Core.Events;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// A member of staff (Pip) as a TDE character, walking the grid through the same thin pathfinding AI
    /// action as customers. At the Grill, Tap or Stew Pot they stand at the station and cook (the ported
    /// <see cref="StaffCook"/> / <see cref="StaffPotCook"/> auto-resolve). On Serving they take plates that
    /// someone is waiting for from the pass and walk them over: speed comes from their skill, customers
    /// walking across the floor can bump them, and if the customer leaves (or the keeper serves them first)
    /// they take the plate back to the pass. Like customers, staff don't collide with the player.
    /// </summary>
    public sealed class StaffAgent : MonoBehaviour
    {
        const float k_Arrive = 0.2f;
        const float k_Resume = 0.35f;

        AIBrain m_Brain;
        CharacterMovement m_Movement;
        TopDownController m_Controller;
        Collider2D m_Body;
        CarryView m_CarryView;
        Transform m_Goal;
        TavernDirector m_Director;
        IRandom m_Random;
        float m_WalkSpeed;

        ServiceSession m_Session;
        StaffCook m_Cook;
        StaffPotCook m_PotCook;
        ServingMinigame m_Serving;
        Ticket m_Carrying;
        CustomerLogic m_For;
        Vector2 m_PickedUpAt;
        float m_Rest;

        public StaffDefinition Member { get; private set; }
        public StaffStation Assignment { get; private set; } = StaffStation.None;
        /// <summary>Standing where they're going (their post, or the table they're carrying to).</summary>
        public bool AtGoal { get; private set; }
        /// <summary>Standing at the post of their job.</summary>
        public bool AtPost => AtGoal && m_Goal != null && Vector2.Distance(m_Goal.position, Post) < 0.01f;
        public Vector2 Post => m_Director != null ? m_Director.Layout.PostFor(Assignment) : (Vector2)transform.position;
        public Ticket Carrying => m_Carrying;
        public bool IsCooking => (m_Cook != null && m_Cook.IsBusy) || (m_PotCook != null && m_PotCook.IsBusy);

        void Awake()
        {
            m_Brain = GetComponent<AIBrain>();
            m_Movement = GetComponent<Character>()?.FindAbility<CharacterMovement>();
            m_Controller = GetComponent<TopDownController>();
            m_Body = GetComponent<Collider2D>();
            m_CarryView = GetComponentInChildren<CarryView>(true);
            m_WalkSpeed = m_Movement != null ? m_Movement.WalkSpeed : 3f;
            m_Goal = new GameObject($"{name}_Goal").transform;
            m_Goal.position = transform.position;
            if (m_Brain != null) m_Brain.Target = m_Goal;
        }

        void OnDestroy()
        {
            if (m_Goal != null) Destroy(m_Goal.gameObject);
        }

        /// <summary>Starts the evening at a job (None: they wait out of the way).</summary>
        public void Begin(StaffStation assignment, StaffDefinition member, TavernDirector director, IRandom random)
        {
            Member = member;
            Assignment = member != null ? assignment : StaffStation.None;
            m_Director = director;
            m_Random = random ?? new SeededRandom();
            m_Session = null;
            AtGoal = false;
            if (m_Brain != null) m_Brain.BrainActive = true;
        }

        void Update()
        {
            if (m_Director == null) return;
            ServiceSession session = m_Director.Session;
            if (session != m_Session)
            {
                // A new evening: new jobs.
                m_Session = session;
                m_Cook = null;
                m_PotCook = null;
                DropCarry();
            }
            if (!m_Director.IsServing && m_Carrying != null) DropCarry();
            Walk(m_Director.IsServing ? Work(Time.deltaTime) : Post);
        }

        /// <summary>Does the job for this frame; returns where to be.</summary>
        Vector2 Work(float dt)
        {
            switch (Assignment)
            {
                case StaffStation.Grill:
                case StaffStation.Tap:
                    if (!AtPost) return Post;
                    m_Cook ??= new StaffCook(m_Session, m_Director.Minigames, Assignment == StaffStation.Grill ? CookStation.Grill : CookStation.Tap,
                        Member.skill, Member.qualityCap, Member.restBetweenJobs, m_Random);
                    m_Cook.Tick(dt);
                    return Post;
                case StaffStation.StewPot:
                    if (!AtPost) return Post;
                    m_PotCook ??= new StaffPotCook(m_Session, m_Director.Minigames, Member.skill, Member.qualityCap, Member.restBetweenJobs, m_Random);
                    m_PotCook.Tick(dt);
                    return Post;
                case StaffStation.Serving:
                    return Serve(dt);
                default:
                    return Post;
            }
        }

        Vector2 Serve(float dt)
        {
            if (m_Carrying == null)
            {
                if (m_Rest > 0f)
                {
                    m_Rest -= dt;
                    return Post;
                }
                if (!AtPost) return Post;
                // Only plates someone is waiting for: spares stay on the pass for the keeper.
                Ticket next = m_Session.NextToServe(includeSpares: false);
                if (next == null || next.Customer == null || !m_Session.StartDelivery(next, this)) return Post;
                m_Carrying = next;
                m_For = next.Customer;
                m_Serving = m_Director.Minigames.CreateServing();
                m_Serving.Begin();
                var pace = (ServingAutoPlayer)MinigameFactory.CreateAutoPlayer(m_Serving, Member.skill, m_Random);
                if (m_Movement != null) m_Movement.MovementSpeed = m_Serving.Settings.carrySpeed * pace.SpeedFactor;
                m_PickedUpAt = transform.position;
                m_CarryView?.Show(next.Recipe.icon);
            }

            if (m_Carrying.State != TicketState.Delivering)
            {
                FinishDelivery();
                return Post;
            }
            m_Serving.Tick(dt, default);
            float strength = KeeperWork.BumpStrength(m_Body, m_Controller, m_Serving.Settings.carrySpeed, m_Director.Agents);
            if (strength > 0f && m_Serving.RegisterBump(strength))
                EventBus<ServingBumped>.Publish(new ServingBumped(strength, m_Serving.Spill, m_Serving.Dropped, false));
            m_CarryView?.SetSpill(m_Serving.Spill);
            if (m_Serving.Dropped)
            {
                m_Session.Dropped(m_Carrying);
                FinishDelivery();
                return Post;
            }

            TavernSeat seat = m_Director.Layout.Seat(m_For.Seat);
            bool stillWaiting = m_Carrying.Customer == m_For && m_For.State == CustomerState.WaitingForFood && seat != null;
            if (!stillWaiting)
            {
                // They left, or the keeper served them: the plate goes back to the pass.
                if (!AtPost) return Post;
                m_Session.PutBack(m_Carrying);
                FinishDelivery();
                return Post;
            }
            if (AtGoal && Vector2.Distance(transform.position, seat.SitPoint) <= m_Serving.Settings.arriveDistance + 0.5f)
            {
                m_Serving.Deliver(KeeperWork.ShortestWalk(m_PickedUpAt, transform.position));
                if (!m_Session.Deliver(m_Carrying, m_For, Mathf.Min(m_Serving.Evaluate(), Member.qualityCap))) m_Session.PutBack(m_Carrying);
                FinishDelivery();
                return Post;
            }
            return seat.ApproachPoint;
        }

        void FinishDelivery()
        {
            DropCarry();
            m_Rest = Member != null ? Member.restBetweenJobs : 0f;
        }

        void DropCarry()
        {
            m_Carrying = null;
            m_For = null;
            m_Serving = null;
            if (m_Movement != null) m_Movement.MovementSpeed = m_WalkSpeed;
            m_CarryView?.Hide();
        }

        /// <summary>
        /// Walks to <paramref name="goal"/>, then stands still with the AI off: the walk action alone overshoots
        /// a point, turns back and overshoots again (Pip "vibrating" at the pass, step 2 playtest).
        /// </summary>
        void Walk(Vector2 goal)
        {
            if (Vector2.Distance(m_Goal.position, goal) > 0.01f)
            {
                m_Goal.position = goal;
                if (AtGoal && Vector2.Distance(transform.position, goal) > k_Arrive) Resume();
            }
            float distance = Vector2.Distance(transform.position, goal);
            if (!AtGoal && distance <= k_Arrive)
            {
                AtGoal = true;
                if (m_Brain != null) m_Brain.BrainActive = false;
                m_Movement?.SetMovement(Vector2.zero);
            }
            else if (AtGoal && distance > k_Resume) Resume();
        }

        void Resume()
        {
            AtGoal = false;
            if (m_Brain != null) m_Brain.BrainActive = true;
        }
    }
}
