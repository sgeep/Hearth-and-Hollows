using System;
using Hearthdelve.Shared.Recipes;

namespace Hearthdelve.Tavern.Customers
{
    public enum CustomerState
    {
        /// <summary>Walking in from the door.</summary>
        Arriving,
        /// <summary>Waiting by the door for a seat.</summary>
        Queueing,
        WalkingToSeat,
        /// <summary>Seated, reading the menu.</summary>
        Ordering,
        /// <summary>Order placed; patience ticking.</summary>
        WaitingForFood,
        Eating,
        Leaving,
        Gone,
    }

    public enum Departure
    {
        None,
        Paid,
        /// <summary>Ran out of patience (seat queue or waiting for food).</summary>
        WalkedOut,
        /// <summary>Nothing they could order was left.</summary>
        SoldOut,
        /// <summary>Service ended before they were served.</summary>
        ClosingTime,
    }

    /// <summary>
    /// One customer's lifecycle (GDD §10.3): Enter → Queue → Seat → Order → Wait → Eat → Pay → Leave.
    /// Movement is the scene's job; it reports arrivals. Pure logic.
    /// </summary>
    public sealed class CustomerLogic
    {
        static int s_NextId;

        readonly CustomerTraits m_Traits;
        float m_Timer;

        public CustomerLogic(CustomerProfile profile)
            : this(profile != null ? profile.traits : CustomerTraits.Default) => Profile = profile;

        public CustomerLogic(CustomerTraits traits)
        {
            m_Traits = traits;
            Id = ++s_NextId;
        }

        public int Id { get; }
        public CustomerProfile Profile { get; }
        public CustomerTraits Traits => m_Traits;
        public CustomerState State { get; private set; } = CustomerState.Arriving;
        public Departure Departure { get; private set; }
        public int Seat { get; private set; } = -1;
        public RecipeDefinition Order { get; private set; }
        /// <summary>0–1 patience left in the current waiting state (for the patience bar).</summary>
        public float Patience { get; private set; } = 1f;
        /// <summary>Share of their food patience used before the dish arrived.</summary>
        public float WaitFraction { get; private set; }
        public float DishQuality { get; private set; }
        public bool IsWaiting => State is CustomerState.Queueing or CustomerState.WaitingForFood;
        /// <summary>Their order is a special request (4f Checkpoint D): they particularly want it tonight.</summary>
        public bool IsRequest { get; private set; }
        /// <summary>How their special request ended (Open while it's still on).</summary>
        public Hearthdelve.Tavern.Service.RequestOutcome RequestOutcome { get; private set; }

        internal void MarkRequest() => IsRequest = true;
        internal void EndRequest(Hearthdelve.Tavern.Service.RequestOutcome outcome)
        {
            if (IsRequest && RequestOutcome == Hearthdelve.Tavern.Service.RequestOutcome.Open) RequestOutcome = outcome;
        }

        /// <summary>Raised once, when they've finished reading the menu. The service answers with PlaceOrder or NothingToOrder.</summary>
        public event Action<CustomerLogic> OrderRequested;
        /// <summary>Raised once, when they start leaving (paid, walked out, sold out, or closing).</summary>
        public event Action<CustomerLogic> Departed;

        /// <summary>Came through the door. With no free seat they queue by the door.</summary>
        public void ArrivedInside(int seat)
        {
            if (State != CustomerState.Arriving) return;
            if (seat >= 0) AssignSeat(seat);
            else
            {
                State = CustomerState.Queueing;
                m_Timer = m_Traits.seatPatience;
                Patience = 1f;
            }
        }

        /// <summary>A seat freed up while they were queueing.</summary>
        public void AssignSeat(int seat)
        {
            if (State is not (CustomerState.Arriving or CustomerState.Queueing)) return;
            Seat = seat;
            State = CustomerState.WalkingToSeat;
            Patience = 1f;
        }

        public void ArrivedAtSeat()
        {
            if (State != CustomerState.WalkingToSeat) return;
            State = CustomerState.Ordering;
            m_Timer = m_Traits.orderDelay;
        }

        public void PlaceOrder(RecipeDefinition recipe)
        {
            if (State != CustomerState.Ordering || recipe == null) return;
            Order = recipe;
            State = CustomerState.WaitingForFood;
            m_Timer = m_Traits.orderPatience;
            Patience = 1f;
        }

        public void NothingToOrder() => Leave(Departure.SoldOut);

        /// <summary>The dish arrived. <paramref name="dishQuality"/> feeds satisfaction when they pay.</summary>
        public void Serve(float dishQuality)
        {
            if (State != CustomerState.WaitingForFood) return;
            WaitFraction = m_Traits.orderPatience > 0f ? 1f - m_Timer / m_Traits.orderPatience : 0f;
            DishQuality = dishQuality;
            State = CustomerState.Eating;
            m_Timer = m_Traits.eatSeconds;
        }

        /// <summary>Service is over; anyone not already leaving goes home (no penalty). Diners pay for what they're eating.</summary>
        public void CloseService()
        {
            if (State is CustomerState.Leaving or CustomerState.Gone) return;
            Leave(State == CustomerState.Eating ? Departure.Paid : Departure.ClosingTime);
        }

        public void ArrivedAtExit()
        {
            if (State == CustomerState.Leaving) State = CustomerState.Gone;
        }

        public void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            switch (State)
            {
                case CustomerState.Queueing:
                    m_Timer -= deltaTime;
                    Patience = m_Traits.seatPatience > 0f ? Math.Max(0f, m_Timer / m_Traits.seatPatience) : 0f;
                    if (m_Timer <= 0f) Leave(Departure.WalkedOut);
                    break;
                case CustomerState.Ordering:
                    m_Timer -= deltaTime;
                    if (m_Timer <= 0f && Order == null)
                    {
                        m_Timer = float.MaxValue; // ask once
                        OrderRequested?.Invoke(this);
                    }
                    break;
                case CustomerState.WaitingForFood:
                    m_Timer -= deltaTime;
                    Patience = m_Traits.orderPatience > 0f ? Math.Max(0f, m_Timer / m_Traits.orderPatience) : 0f;
                    if (m_Timer <= 0f) Leave(Departure.WalkedOut);
                    break;
                case CustomerState.Eating:
                    m_Timer -= deltaTime;
                    if (m_Timer <= 0f) Leave(Departure.Paid);
                    break;
            }
        }

        void Leave(Departure departure)
        {
            if (State is CustomerState.Leaving or CustomerState.Gone) return;
            Departure = departure;
            State = CustomerState.Leaving;
            Departed?.Invoke(this);
        }
    }
}
