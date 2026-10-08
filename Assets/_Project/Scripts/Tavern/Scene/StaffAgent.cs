using Hearthdelve.Core.Events;
using Hearthdelve.Core.Pathfinding;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Shared.Story;
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
    /// A member of staff (Orik) as a TDE character, walking the grid through the same thin pathfinding AI
    /// action as customers. At the Grill, Tap or Stew Pot they stand at the station and cook (the ported
    /// <see cref="StaffCook"/> / <see cref="StaffPotCook"/> auto-resolve). On Serving they take plates that
    /// someone is waiting for from the pass and walk them over: speed comes from their skill, customers
    /// walking across the floor can bump them, and if the customer leaves (or the keeper serves them first)
    /// they take the plate back to the pass. Like customers, staff don't collide with the player.
    /// </summary>
    /// <summary>The faces a member of staff shows (4f Checkpoint C, D18: wordless).</summary>
    [System.Serializable]
    public sealed class StaffFaces
    {
        public Sprite happy;
        public Sprite content;
        public Sprite frown;
        public Sprite surprised;
        public Sprite sweat;
        public Sprite heart;
    }

    public sealed class StaffAgent : MonoBehaviour
    {
        [SerializeField, Tooltip("Their face over the head (4f Checkpoint C).")] NpcEmote m_Emote;
        [SerializeField] StaffFaces m_Faces = new();
        [SerializeField, Tooltip("Where they wait off duty, beside the rest post (so two staff don't stand on one spot).")]
        Vector2 m_RestOffset;
        [SerializeField, Min(0f), Tooltip("Orik: seconds spent tidying a table after a guest leaves.")] float m_TidySeconds = 1.2f;

        // Personality beats (presentation only): a face to show later, a table to tidy.
        Sprite m_PendingFace;
        float m_PendingAt;
        Vector2? m_Tidy;
        float m_TidyLeft;

        // A job outside service (Gunta at the Butcher Block during Prep): walk there, work a moment, report.
        Vector2 m_TaskAt;
        float m_TaskLeft;
        float m_TaskWalked;
        System.Action m_TaskDone;

        [SerializeField, Min(1f), Tooltip("Seconds of walking to a job outside service before she steps straight to it (the 4f web check: " +
                                          "Gunta pressed against the far side of the Butcher Block and never reached it).")]
        float m_TaskWalkLimit = 8f;

        /// <summary>The last job ended with a step straight to it (tests).</summary>
        public bool SteppedToTask { get; private set; }

        /// <summary>Busy with a job outside service (walking to it or working it).</summary>
        public bool HasTask => m_TaskDone != null;
        /// <summary>At the job and working it (the Butcher Block's knife moves).</summary>
        public bool WorkingTask => m_TaskDone != null && AtGoal && Vector2.Distance(transform.position, m_TaskAt) <= 0.3f;

        /// <summary>
        /// Walks to <paramref name="at"/>, works there for <paramref name="seconds"/>, then calls <paramref name="done"/>
        /// (outside service only). False if already busy.
        /// </summary>
        public bool DoTask(Vector2 at, float seconds, System.Action done)
        {
            if (HasTask || Member == null || done == null) return false;
            m_TaskAt = at;
            m_TaskLeft = seconds;
            m_TaskWalked = 0f;
            SteppedToTask = false;
            m_TaskDone = done;
            return true;
        }

        public NpcEmote Emote => m_Emote;
        public StaffFaces Faces => m_Faces;
        /// <summary>Tidying a table a guest just left (Orik, between plates).</summary>
        public bool IsTidying => m_Tidy.HasValue;
        /// <summary>Tables tidied tonight (tests).</summary>
        public int Tidied { get; private set; }

        public void ConfigureLook(NpcEmote emote, Vector2 restOffset, StaffFaces faces)
        {
            m_Emote = emote;
            m_RestOffset = restOffset;
            m_Faces = faces ?? new StaffFaces();
        }

        void OnEnable()
        {
            EventBus<ServingBumped>.Subscribe(OnBumped);
            EventBus<KeeperPlate>.Subscribe(OnKeeperPlate);
        }

        void OnDisable()
        {
            EventBus<ServingBumped>.Unsubscribe(OnBumped);
            EventBus<KeeperPlate>.Unsubscribe(OnKeeperPlate);
            if (m_Session != null) m_Session.CustomerLeft -= OnCustomerLeft;
        }

        /// <summary>A face now (presentation only).</summary>
        public void Show(Sprite face, float seconds = 0f) => m_Emote?.Show(face, seconds);

        /// <summary>A face a little later (Gunta tasting the pot she's just filled).</summary>
        void ShowLater(Sprite face, float delay)
        {
            m_PendingFace = face;
            m_PendingAt = Time.time + delay;
        }

        // Anyone's plate on the floor: a start, and a bead of sweat.
        void OnBumped(ServingBumped e)
        {
            if (e.Dropped && Member != null) Show(m_Faces.sweat);
        }

        void OnKeeperPlate(KeeperPlate e)
        {
            if (e.Moment == PlateMoment.Dropped && Member != null) Show(m_Faces.surprised);
        }

        /// <summary>A guest who paid and left: Orik, if serving and free, tidies the table.</summary>
        void OnCustomerLeft(CustomerLogic customer)
        {
            if (Assignment != StaffStation.Serving || customer == null || customer.Departure != Departure.Paid || m_Director == null) return;
            TavernSeat seat = m_Director.Layout.Seat(customer.Seat);
            if (seat != null && !m_Tidy.HasValue) m_Tidy = seat.ApproachPoint;
        }

        void OnCooked(RecipeDefinition dish, float quality)
        {
            if (Member == null || dish == null) return;
            EventBus<StaffWorkDone>.Publish(new StaffWorkDone(Member.id, dish.id, quality));
            if (quality >= 0.8f && m_Random.Value() < 0.35f) Show(m_Faces.content);
        }

        /// <summary>Gunta fills the pot, then tastes it: a nod for a good batch, a frown for a ragged one.</summary>
        void OnChopped(float score)
        {
            if (Member == null) return;
            RecipeDefinition stew = m_Session?.Pot?.Recipe;
            EventBus<StaffWorkDone>.Publish(new StaffWorkDone(Member.id, stew != null ? stew.id : string.Empty, score));
            ShowLater(score >= 0.7f ? m_Faces.content : score < 0.45f ? m_Faces.frown : null, 1.5f);
        }
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
        /// <summary>Talking to them (4g): through the story's conversation service, while they stand still with their hands free.</summary>
        public TavernInteractable Talk => m_Talk;
        /// <summary>Their stable character id (the same as their staff id).</summary>
        public string CharacterId => Member == null ? null : Member.character != null ? Member.character.id : Member.id;
        TavernInteractable m_Talk;
        public StaffStation Assignment { get; private set; } = StaffStation.None;
        /// <summary>Standing where they're going (their post, or the table they're carrying to).</summary>
        public bool AtGoal { get; private set; }
        /// <summary>Standing at the post of their job.</summary>
        public bool AtPost => AtGoal && m_Goal != null && Vector2.Distance(m_Goal.position, Post) < 0.01f;
        public Vector2 Post => m_Director != null
            ? m_Director.Layout.PostFor(Assignment) + (Assignment == StaffStation.None ? m_RestOffset : Vector2.zero)
            : (Vector2)transform.position;
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
            // Talking to them (4g): the keeper's Interact, like any station; the story decides what's said.
            var talk = new GameObject("Talk");
            talk.transform.SetParent(transform, false);
            m_Talk = talk.AddComponent<TavernInteractable>();
            m_Talk.Configure(TavernInteractableKind.Person, null, Vector2.zero, 0.9f, null);
            m_Talk.Describe = () => new TavernHint(TavernHintKind.Talk, staff: Member);
            m_Talk.Used += _ => StoryServices.Conversations?.Talk(CharacterId);
            m_Talk.SetAvailable(false);
        }

        void OnDestroy()
        {
            Speakers.Remove(CharacterId, transform);
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
            // Only while standing still with their hands free: a member of staff walking plates never takes the keeper's Interact.
            if (m_Talk != null)
            {
                IConversationService talk = StoryServices.Conversations;
                m_Talk.SetAvailable(Member != null && m_Carrying == null && AtGoal && talk != null && !talk.IsTalking && talk.CanTalk(CharacterId));
            }
            // 4h Checkpoint D: findable for an overheard line (Boog and Gimp at the bar).
            if (Member != null) Speakers.Set(CharacterId, transform);
            if (m_Director == null) return;
            ServiceSession session = m_Director.Session;
            if (session != m_Session)
            {
                // A new evening: new jobs.
                if (m_Session != null) m_Session.CustomerLeft -= OnCustomerLeft;
                m_Session = session;
                if (m_Session != null) m_Session.CustomerLeft += OnCustomerLeft;
                m_Tidy = null;
                m_Cook = null;
                m_PotCook = null;
                DropCarry();
            }
            if (!m_Director.IsServing && m_Carrying != null) DropCarry();
            Walk(m_Director.IsServing ? Work(Time.deltaTime) : HasTask ? Task(Time.deltaTime) : Post);
            if (m_PendingFace != null && Time.time >= m_PendingAt)
            {
                Show(m_PendingFace);
                m_PendingFace = null;
            }
        }

        Vector2 Task(float dt)
        {
            if (!WorkingTask)
            {
                // A job outside service never hangs: if the way there is blocked (a body in the way, a layout the path
                // can't get round), after a while she steps straight to it, as the keeper does at the Butcher Block.
                m_TaskWalked += dt;
                if (m_TaskWalked < m_TaskWalkLimit) return m_TaskAt;
                StepTo(m_TaskAt);
                SteppedToTask = true;
                if (Debug.isDebugBuild) Debug.Log($"[Hearthdelve] {Member?.id}: couldn't walk to the job; stepped to it");
                return m_TaskAt;
            }
            m_TaskLeft -= dt;
            if (m_TaskLeft > 0f) return m_TaskAt;
            System.Action done = m_TaskDone;
            m_TaskDone = null;
            done();
            return Post;
        }

        /// <summary>Does the job for this frame; returns where to be.</summary>
        Vector2 Work(float dt)
        {
            switch (Assignment)
            {
                case StaffStation.Grill:
                case StaffStation.Tap:
                    if (!AtPost) return Post;
                    if (m_Cook == null)
                    {
                        m_Cook = new StaffCook(m_Session, m_Director.Minigames, Assignment == StaffStation.Grill ? CookStation.Grill : CookStation.Tap,
                            Member.skill, Member.qualityCap, Member.restBetweenJobs, m_Random);
                        m_Cook.Cooked += OnCooked;
                    }
                    m_Cook.Tick(dt);
                    return Post;
                case StaffStation.StewPot:
                    if (!AtPost) return Post;
                    if (m_PotCook == null)
                    {
                        m_PotCook = new StaffPotCook(m_Session, m_Director.Minigames, Member.skill, Member.qualityCap, Member.restBetweenJobs, m_Random);
                        m_PotCook.Chopped += OnChopped;
                    }
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
                // Between plates: a table a guest just left gets a wipe first (a plate waiting comes first, though).
                if (m_Tidy.HasValue && m_Session.NextToServe(includeSpares: false) == null)
                {
                    Vector2 table = m_Tidy.Value;
                    if (!AtGoal || Vector2.Distance(transform.position, table) > 0.3f) return table;
                    if (m_TidyLeft <= 0f) m_TidyLeft = m_TidySeconds;
                    m_TidyLeft -= dt;
                    if (m_TidyLeft > 0f) return table;
                    m_Tidy = null;
                    Tidied++;
                    Show(m_Faces.content, 1f);
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
                float served = Mathf.Min(m_Serving.Evaluate(), Member.qualityCap);
                RecipeDefinition dish = m_Carrying.Recipe;
                if (!m_Session.Deliver(m_Carrying, m_For, served)) m_Session.PutBack(m_Carrying);
                else EventBus<StaffWorkDone>.Publish(new StaffWorkDone(Member.id, dish != null ? dish.id : string.Empty, served));
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
        /// a point, turns back and overshoots again (Orik "vibrating" at the pass, step 2 playtest).
        /// </summary>
        void Walk(Vector2 goal)
        {
            if (Vector2.Distance(m_Goal.position, goal) > 0.01f)
            {
                m_Goal.position = goal;
                if (AtGoal && Vector2.Distance(transform.position, goal) > k_Arrive) Resume();
            }
            float distance = Vector2.Distance(transform.position, goal);
            // Within one frame's travel counts as there (a slow frame would carry them past it); they stand on the spot.
            float reach = Arrival.Reach(k_Arrive, m_Movement != null ? m_Movement.MovementSpeed : m_WalkSpeed, Time.deltaTime);
            if (!AtGoal && distance <= k_Arrive)
            {
                AtGoal = true;
                if (m_Brain != null) m_Brain.BrainActive = false;
                m_Movement?.SetMovement(Vector2.zero);
            }
            else if (!AtGoal && distance <= reach) StepTo(goal);
            else if (AtGoal && distance > Arrival.Resume(k_Resume, reach)) Resume();
        }

        void StepTo(Vector2 at)
        {
            transform.position = at;
            if (TryGetComponent(out Rigidbody2D body))
            {
                body.position = at;
                body.linearVelocity = Vector2.zero;
            }
            AtGoal = true;
            if (m_Brain != null) m_Brain.BrainActive = false;
            m_Movement?.SetMovement(Vector2.zero);
        }

        void Resume()
        {
            AtGoal = false;
            if (m_Brain != null) m_Brain.BrainActive = true;
        }
    }
}
