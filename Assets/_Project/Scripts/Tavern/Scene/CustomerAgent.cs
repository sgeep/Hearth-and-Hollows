using Hearthdelve.Shared.Animation;
using Hearthdelve.Tavern.Customers;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// A customer in the tavern: a TDE character that walks (on the grid, through the thin pathfinding
    /// AI action) from the door to the queue or a seat, sits facing the table, and walks back out. Every
    /// decision is in <see cref="CustomerLogic"/>; this only moves the body and shows patience, the order,
    /// and a walkout. Customers don't collide with the player or each other (their own physics layer),
    /// so they never shove anyone. Their look is picked once, from their seed, and kept.
    /// </summary>
    public sealed class CustomerAgent : MonoBehaviour
    {
        const float k_Arrive = 0.2f;
        /// <summary>Standing still until the goal is this far away again (no flip-flopping at the edge of arriving).</summary>
        const float k_Resume = 0.35f;
        const float k_UpsetSeconds = 2f;

        [SerializeField] LayeredSpriteAnimator m_Look;
        [SerializeField] SpriteRenderer m_PatienceBack;
        [SerializeField] SpriteRenderer m_PatienceFill;
        [SerializeField] GameObject m_Bubble;
        [SerializeField] SpriteRenderer m_BubbleIcon;
        [SerializeField] Sprite m_Thinking;
        [SerializeField] Sprite m_Upset;
        [SerializeField] Color m_PatienceFull = new(0.45f, 0.9f, 0.35f);
        [SerializeField] Color m_PatienceEmpty = new(0.95f, 0.3f, 0.25f);

        TavernDirector m_Director;
        AIBrain m_Brain;
        CharacterMovement m_Movement;
        Rigidbody2D m_Body;
        TopDownController m_Controller;
        Transform m_Goal;
        TavernSeat m_Seat;
        float m_UpsetUntil;

        public CustomerLogic Logic { get; private set; }
        public bool IsSeated { get; private set; }
        /// <summary>The look picked for this customer (fixed for their whole visit).</summary>
        public AppearanceChoice Appearance { get; private set; }
        /// <summary>Walking across the floor (for serving bumps, step 3).</summary>
        public bool IsWalking => !IsSeated && m_Controller != null && m_Controller.CurrentMovement.sqrMagnitude > 0.01f;
        public Vector2 Goal => m_Goal != null ? (Vector2)m_Goal.position : (Vector2)transform.position;
        public LayeredSpriteAnimator Look => m_Look;
        public bool ShowsPatience => m_PatienceFill != null && m_PatienceFill.enabled;
        public Sprite BubbleIcon => m_Bubble != null && m_Bubble.activeSelf ? m_BubbleIcon.sprite : null;

        public void Configure(LayeredSpriteAnimator look, SpriteRenderer patienceBack, SpriteRenderer patienceFill, GameObject bubble,
            SpriteRenderer bubbleIcon, Sprite thinking, Sprite upset)
        {
            m_Look = look;
            m_PatienceBack = patienceBack;
            m_PatienceFill = patienceFill;
            m_Bubble = bubble;
            m_BubbleIcon = bubbleIcon;
            m_Thinking = thinking;
            m_Upset = upset;
        }

        public void Initialize(CustomerLogic logic, TavernDirector director, int seed)
        {
            Logic = logic;
            m_Director = director;
            name = $"Customer_{(logic.Profile != null ? logic.Profile.id : "guest")}_{logic.Id}";
            m_Brain = GetComponent<AIBrain>();
            m_Movement = GetComponent<Character>().FindAbility<CharacterMovement>();
            m_Body = GetComponent<Rigidbody2D>();
            m_Controller = GetComponent<TopDownController>();
            if (m_Movement != null) m_Movement.WalkSpeed = m_Movement.MovementSpeed = logic.Traits.walkSpeed;
            m_Goal = new GameObject($"{name}_Goal").transform;
            m_Goal.position = transform.position;
            if (m_Brain != null) m_Brain.Target = m_Goal;

            NpcAppearancePool pool = logic.Profile != null ? logic.Profile.appearance : null;
            if (pool != null && m_Look != null)
            {
                Appearance = pool.Pick(seed);
                m_Look.SetAppearance(pool.Layers(Appearance));
            }
            logic.Departed += OnDeparted;
            UpdateVisuals();
        }

        void OnDestroy()
        {
            if (Logic != null) Logic.Departed -= OnDeparted;
            if (m_Goal != null) Destroy(m_Goal.gameObject);
        }

        void OnDeparted(CustomerLogic logic)
        {
            if (logic.Departure is Departure.WalkedOut or Departure.SoldOut) m_UpsetUntil = Time.time + k_UpsetSeconds;
            if (IsSeated) StandUp();
        }

        void Update()
        {
            if (Logic == null) return;
            // The controller can carry a little speed into the step after sitting down: keep them on the chair.
            if (IsSeated && m_Seat != null && ((Vector2)transform.position - m_Seat.SitPoint).sqrMagnitude > 1e-6f)
            {
                m_Movement?.SetMovement(Vector2.zero);
                Place(m_Seat.SitPoint);
            }
            switch (Logic.State)
            {
                case CustomerState.Queueing:
                    m_Goal.position = m_Director.Layout.QueueSpot(Mathf.Max(0, m_Director.QueuePlace(this)));
                    break;
                case CustomerState.WalkingToSeat:
                    m_Seat = m_Director.Layout.Seat(Logic.Seat);
                    if (m_Seat == null) break;
                    m_Goal.position = m_Seat.ApproachPoint;
                    if (Arrived()) SitDown();
                    break;
                case CustomerState.Leaving:
                case CustomerState.Gone:
                    m_Goal.position = m_Director.Layout.Door;
                    if (Arrived())
                    {
                        m_Director.CustomerGone(this);
                        return;
                    }
                    break;
            }
            if (!IsSeated) HoldWhenThere();
            UpdateVisuals();
        }

        bool Arrived() => Vector2.Distance(transform.position, m_Goal.position) <= k_Arrive;

        /// <summary>
        /// Once there (a queue spot), stand still with the AI off: the walk action would otherwise overshoot
        /// the point, turn back, and overshoot again every frame. It walks again once the goal moves away.
        /// </summary>
        void HoldWhenThere()
        {
            if (m_Brain == null) return;
            float distance = Vector2.Distance(transform.position, m_Goal.position);
            if (m_Brain.BrainActive && distance <= k_Arrive)
            {
                m_Brain.BrainActive = false;
                m_Movement?.SetMovement(Vector2.zero);
            }
            else if (!m_Brain.BrainActive && distance > k_Resume) m_Brain.BrainActive = true;
        }

        void SitDown()
        {
            IsSeated = true;
            if (m_Brain != null) m_Brain.BrainActive = false;
            m_Movement?.SetMovement(Vector2.zero);
            // The seat is inside the chair's footprint: no collisions while sitting, or the chair pushes them off it.
            SetSolid(false);
            Place(m_Seat.SitPoint);
            m_Look?.LockFacing(m_Seat.Facing);
            Logic.ArrivedAtSeat();
        }

        void StandUp()
        {
            IsSeated = false;
            if (m_Seat != null) Place(m_Seat.ApproachPoint);
            SetSolid(true);
            m_Look?.ReleaseFacing();
            if (m_Brain != null) m_Brain.BrainActive = true;
        }

        void SetSolid(bool solid)
        {
            foreach (Collider2D c in GetComponents<Collider2D>()) c.enabled = solid;
            // TDE's controller moves the body every physics step; while sitting it's paused, so nothing shifts them.
            if (m_Controller != null) m_Controller.enabled = solid;
            if (m_Body != null)
            {
                m_Body.linearVelocity = Vector2.zero;
                m_Body.bodyType = solid ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            }
        }

        void Place(Vector2 position)
        {
            transform.position = position;
            if (m_Body != null) m_Body.position = position;
        }

        void UpdateVisuals()
        {
            bool waiting = Logic.IsWaiting;
            if (m_PatienceBack != null) m_PatienceBack.enabled = waiting;
            if (m_PatienceFill != null)
            {
                m_PatienceFill.enabled = waiting;
                // The fill hangs off an anchor at the bar's left end and shortens from the right, in whole art pixels.
                float pixels = Mathf.Ceil(Logic.Patience * 8f);
                m_PatienceFill.transform.parent.localScale = new Vector3(pixels, 1f, 1f);
                m_PatienceFill.color = Color.Lerp(m_PatienceEmpty, m_PatienceFull, Logic.Patience);
            }

            Sprite icon = Time.time < m_UpsetUntil ? m_Upset
                : Logic.State == CustomerState.Ordering ? m_Thinking
                : Logic.State == CustomerState.WaitingForFood && Logic.Order != null ? Logic.Order.icon
                : null;
            if (m_Bubble != null && m_Bubble.activeSelf != (icon != null)) m_Bubble.SetActive(icon != null);
            if (icon != null && m_BubbleIcon != null) m_BubbleIcon.sprite = icon;
        }
    }
}
