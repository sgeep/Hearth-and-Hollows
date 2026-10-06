using Hearthdelve.Shared.Animation;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Service;
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
        /// <summary>Walking velocity (tiles per second); zero when seated or standing.</summary>
        public Vector2 Velocity => IsWalking ? (Vector2)m_Controller.CurrentMovement : Vector2.zero;
        public Vector2 Goal => m_Goal != null ? (Vector2)m_Goal.position : (Vector2)transform.position;
        public LayeredSpriteAnimator Look => m_Look;
        public bool ShowsPatience => m_PatienceFill != null && m_PatienceFill.enabled;
        public Sprite BubbleIcon => m_Bubble != null && m_Bubble.activeSelf ? m_BubbleIcon.sprite : null;

        [SerializeField, Tooltip("A boss trophy on the wall, noticed on sitting down (4f Checkpoint C, D18): a wordless face.")]
        Sprite m_NoticeTrophy;
        [SerializeField, Range(0f, 1f), Tooltip("Chance a patron notices a trophy on the wall when they sit down.")]
        float m_NoticeChance = 0.5f;
        Sprite m_Emote;
        float m_EmoteFrom, m_EmoteUntil;
        int m_Seed;

        /// <summary>Did this patron notice a trophy (tests)?</summary>
        public bool NoticedTrophy { get; private set; }

        public void ConfigureEmotes(Sprite noticeTrophy) => m_NoticeTrophy = noticeTrophy;

        [Header("Special requests (4f Checkpoint D)")]
        [SerializeField, Tooltip("The sparkle on the bubble while their request is on.")] SpriteRenderer m_RequestMark;
        [SerializeField] Sprite m_RequestMet;
        [SerializeField] Sprite m_RequestMissed;
        RequestOutcome m_ShownOutcome;

        /// <summary>Is the request sparkle showing (tests)?</summary>
        public bool ShowsRequest => m_RequestMark != null && m_RequestMark.gameObject.activeInHierarchy && m_RequestMark.enabled;

        public void ConfigureRequests(SpriteRenderer mark, Sprite met, Sprite missed)
        {
            m_RequestMark = mark;
            m_RequestMet = met;
            m_RequestMissed = missed;
        }

        /// <summary>Shows a face in the bubble from <paramref name="delay"/> seconds for <paramref name="seconds"/> (presentation only).</summary>
        public void Emote(Sprite face, float seconds, float delay = 0f)
        {
            if (face == null) return;
            m_Emote = face;
            m_EmoteFrom = Time.time + delay;
            m_EmoteUntil = m_EmoteFrom + seconds;
        }

        /// <summary>A trophy among the tavern's furniture (a boss's: never bought, never lost)?</summary>
        static bool TrophyOnDisplay()
        {
            AreaFurniture tavern = AreaFurniture.Tavern;
            if (tavern == null) return false;
            foreach (Hearthdelve.Shared.Customization.PlacedFurniture p in tavern.CurrentLayout())
            {
                var d = tavern.Definition(p.definition);
                if (d != null && (d.sources & Hearthdelve.Shared.Customization.FurnitureSource.Boss) != 0) return true;
            }
            return false;
        }

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
            m_Seed = seed;
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
            // Some patrons look up at the troll's tusks as they sit (wordless, D18; what they'd say comes with 4g).
            if (m_NoticeTrophy != null && TrophyOnDisplay() && new System.Random(unchecked(m_Seed * 7919 + 17)).NextDouble() < m_NoticeChance)
            {
                NoticedTrophy = true;
                Emote(m_NoticeTrophy, 1.6f, 0.6f);
            }
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

            bool emoting = m_Emote != null && Time.time >= m_EmoteFrom && Time.time < m_EmoteUntil;
            Sprite icon = Time.time < m_UpsetUntil ? m_Upset
                : emoting ? m_Emote
                : Logic.State == CustomerState.Ordering ? m_Thinking
                : Logic.State == CustomerState.WaitingForFood && Logic.Order != null ? Logic.Order.icon
                : null;
            if (m_Bubble != null && m_Bubble.activeSelf != (icon != null)) m_Bubble.SetActive(icon != null);
            if (icon != null && m_BubbleIcon != null) m_BubbleIcon.sprite = icon;

            // A special request: the sparkle beside the dish they want; then a heart, or a frown as they go.
            if (Logic.RequestOutcome != m_ShownOutcome)
            {
                m_ShownOutcome = Logic.RequestOutcome;
                if (m_ShownOutcome == RequestOutcome.Completed) Emote(m_RequestMet, 1.6f);
                else if (m_ShownOutcome != RequestOutcome.Open) Emote(m_RequestMissed, 1.6f);
            }
            if (m_RequestMark != null)
                m_RequestMark.enabled = Logic.IsRequest && Logic.RequestOutcome == RequestOutcome.Open && icon != null && icon == Logic.Order?.icon;
        }
    }
}
