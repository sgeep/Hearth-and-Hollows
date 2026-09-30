using Hearthdelve.Tavern.Customers;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// A customer in the tavern: walks between the door, the queue and their seat, and shows
    /// patience, their order, and how they left. All decisions are in <see cref="CustomerLogic"/>.
    /// </summary>
    public sealed class CustomerAgent : MonoBehaviour
    {
        [SerializeField] SpriteRenderer m_Body;
        [SerializeField] SpriteRenderer m_PatienceBack;
        [SerializeField] SpriteRenderer m_PatienceFill;
        [SerializeField] SpriteRenderer m_OrderIcon;
        [SerializeField] SpriteRenderer m_UpsetIcon;
        [SerializeField, Tooltip("Floor ring shown when Interact would serve this customer the carried plate.")]
        SpriteRenderer m_Highlight;
        [SerializeField] Color m_PatienceFull = new(0.4f, 0.9f, 0.4f);
        [SerializeField] Color m_PatienceEmpty = new(0.95f, 0.3f, 0.25f);

        TavernDirector m_Director;
        Vector3 m_FillBaseScale;

        public CustomerLogic Logic { get; private set; }
        /// <summary>Walking across the floor: plate carriers bump into walking customers.</summary>
        public bool IsWalking { get; private set; }
        public float X => transform.position.x;

        public void Configure(SpriteRenderer body, SpriteRenderer patienceBack, SpriteRenderer patienceFill, SpriteRenderer orderIcon,
            SpriteRenderer upsetIcon, SpriteRenderer highlight)
        {
            m_Body = body;
            m_PatienceBack = patienceBack;
            m_PatienceFill = patienceFill;
            m_OrderIcon = orderIcon;
            m_UpsetIcon = upsetIcon;
            m_Highlight = highlight;
        }

        public bool IsHighlighted => m_Highlight != null && m_Highlight.enabled;

        public void SetHighlighted(bool on)
        {
            if (m_Highlight != null) m_Highlight.enabled = on;
        }

        public void Initialize(CustomerLogic logic, TavernDirector director)
        {
            Logic = logic;
            m_Director = director;
            m_FillBaseScale = m_PatienceFill != null ? m_PatienceFill.transform.localScale : Vector3.one;
            if (m_Body != null && logic.Profile != null)
            {
                if (logic.Profile.sprite != null) m_Body.sprite = logic.Profile.sprite;
                m_Body.color = logic.Profile.placeholderColor;
            }
            name = $"Customer_{(logic.Profile != null ? logic.Profile.id : "guest")}_{logic.Id}";
        }

        void Update()
        {
            if (Logic == null) return;

            float target = TargetX();
            float step = Logic.Traits.walkSpeed * Time.deltaTime;
            var p = transform.position;
            IsWalking = Mathf.Abs(p.x - target) > 0.05f;
            if (IsWalking)
            {
                p.x = Mathf.MoveTowards(p.x, target, step);
                transform.position = p;
                if (m_Body != null) m_Body.flipX = target < p.x;
            }
            else
            {
                if (Logic.State == CustomerState.WalkingToSeat) Logic.ArrivedAtSeat();
                else if (Logic.State == CustomerState.Leaving)
                {
                    m_Director.CustomerGone(this);
                    return;
                }
            }

            UpdateVisuals();
        }

        float TargetX()
        {
            var layout = m_Director.Layout;
            switch (Logic.State)
            {
                case CustomerState.Queueing:
                    return layout.QueueX(Mathf.Max(0, m_Director.QueuePlace(this)));
                case CustomerState.WalkingToSeat:
                case CustomerState.Ordering:
                case CustomerState.WaitingForFood:
                case CustomerState.Eating:
                    return layout.SeatX(Logic.Seat);
                case CustomerState.Leaving:
                case CustomerState.Gone:
                    return layout.ExitX;
                default:
                    return layout.DoorX;
            }
        }

        void UpdateVisuals()
        {
            bool showPatience = Logic.IsWaiting;
            if (m_PatienceBack != null) m_PatienceBack.enabled = showPatience;
            if (m_PatienceFill != null)
            {
                m_PatienceFill.enabled = showPatience;
                m_PatienceFill.transform.localScale = new Vector3(m_FillBaseScale.x * Logic.Patience, m_FillBaseScale.y, 1f);
                m_PatienceFill.color = Color.Lerp(m_PatienceEmpty, m_PatienceFull, Logic.Patience);
            }

            bool showOrder = Logic.Order != null && Logic.State is CustomerState.WaitingForFood or CustomerState.Eating;
            if (m_OrderIcon != null)
            {
                m_OrderIcon.enabled = showOrder;
                if (showOrder) m_OrderIcon.color = Logic.Order.placeholderColor;
            }

            if (m_UpsetIcon != null)
                m_UpsetIcon.enabled = Logic.State == CustomerState.Leaving &&
                                      Logic.Departure is Departure.WalkedOut or Departure.SoldOut;
        }
    }
}
