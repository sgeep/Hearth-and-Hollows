using System;
using System.Collections.Generic;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Economy;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;

namespace Hearthdelve.Tavern.Service
{
    public enum TicketState
    {
        /// <summary>Ordered; ingredients reserved; waiting for a cook.</summary>
        Queued,
        Cooking,
        /// <summary>On the pass, waiting to be carried out.</summary>
        Ready,
        Delivering,
        Served,
        Cancelled,
    }

    /// <summary>One order. Ingredients are reserved from the storeroom when it's placed.</summary>
    public sealed class Ticket
    {
        static int s_NextId;

        internal Ticket(CustomerLogic customer, RecipeDefinition recipe, CookedIngredients reserved)
        {
            Id = ++s_NextId;
            Customer = customer;
            Recipe = recipe;
            Reserved = reserved;
        }

        public int Id { get; }
        public CustomerLogic Customer { get; }
        public RecipeDefinition Recipe { get; }
        public CookedIngredients Reserved { get; internal set; }
        public TicketState State { get; internal set; } = TicketState.Queued;
        public float CookScore { get; internal set; }
        public float DishQuality { get; internal set; }
        public float DishValue { get; internal set; }
        /// <summary>Who is working on it (player, staff). Null when unclaimed.</summary>
        public object ClaimedBy { get; internal set; }
    }

    public sealed class ServiceLedger
    {
        public int DishesServed;
        public int Gold;
        public int Tips;
        public int Renown;
        public int Walkouts;
        public int SoldOutLeaves;
        public int DroppedDishes;
        public int CustomersArrived;
    }

    /// <summary>
    /// One evening of service (GDD §6.1): the clock, seating, orders, tickets, sold-out
    /// handling, and the money/renown ledger. The scene moves people and runs minigames;
    /// everything that decides outcomes lives here. Pure logic.
    /// </summary>
    public sealed class ServiceSession
    {
        readonly ServiceSettings m_Settings;
        readonly DishScoringSettings m_Scoring;
        readonly ServiceEconomySettings m_Economy;
        readonly IRandom m_Random;
        readonly List<RecipeDefinition> m_Menu;
        readonly List<Ticket> m_Tickets = new();
        readonly List<CustomerLogic> m_Customers = new();
        readonly List<CustomerLogic> m_SeatQueue = new();
        readonly CustomerLogic[] m_Seats;
        readonly HashSet<RecipeDefinition> m_SoldOut = new();

        public ServiceSession(ServiceSettings settings, DishScoringSettings scoring, ServiceEconomySettings economy,
            Storeroom storeroom, IReadOnlyList<RecipeDefinition> menu, int seatCount, IRandom random)
        {
            if (menu == null || menu.Count == 0) throw new ArgumentException("The menu needs at least one dish.", nameof(menu));
            if (menu.Count > settings.maxMenuSize) throw new ArgumentException($"The menu holds at most {settings.maxMenuSize} dishes.", nameof(menu));
            m_Settings = settings;
            m_Scoring = scoring;
            m_Economy = economy;
            Storeroom = storeroom ?? throw new ArgumentNullException(nameof(storeroom));
            m_Menu = new List<RecipeDefinition>(menu);
            m_Seats = new CustomerLogic[Math.Max(1, seatCount)];
            m_Random = random ?? new SeededRandom();
            RefreshSoldOut();
        }

        public Storeroom Storeroom { get; }
        public ServiceLedger Ledger { get; } = new();
        public IReadOnlyList<RecipeDefinition> Menu => m_Menu;
        public IReadOnlyList<Ticket> Tickets => m_Tickets;
        public IReadOnlyList<CustomerLogic> Customers => m_Customers;
        public int SeatCount => m_Seats.Length;
        public float Elapsed { get; private set; }
        public float Remaining => Math.Max(0f, m_Settings.lengthSeconds - Elapsed);
        public bool IsLastOrders => Remaining <= m_Settings.lastOrdersSeconds;
        public bool IsOver { get; private set; }
        public bool CanAdmitCustomer => !IsOver && !IsLastOrders && m_Customers.Count < m_Settings.maxCustomers;

        public event Action SoldOutChanged;
        public event Action<Ticket> TicketChanged;
        public event Action<CustomerLogic> CustomerLeft;
        public event Action Ended;

        public bool IsSoldOut(RecipeDefinition recipe) => m_SoldOut.Contains(recipe);

        public List<RecipeDefinition> AvailableDishes()
        {
            var list = new List<RecipeDefinition>();
            foreach (var r in m_Menu) if (!m_SoldOut.Contains(r)) list.Add(r);
            return list;
        }

        // ---------- Clock ----------

        public void Tick(float deltaTime)
        {
            if (IsOver || deltaTime <= 0f) return;
            Elapsed += deltaTime;
            foreach (var c in m_Customers.ToArray()) c.Tick(deltaTime);
            if (Elapsed >= m_Settings.lengthSeconds) End();
        }

        public void End()
        {
            if (IsOver) return;
            IsOver = true;
            foreach (var c in m_Customers.ToArray()) c.CloseService();
            Ended?.Invoke();
        }

        // ---------- Customers ----------

        /// <summary>A customer walked in. Returns their seat, or -1 if they queue by the door.</summary>
        public int AdmitCustomer(CustomerLogic customer)
        {
            m_Customers.Add(customer);
            Ledger.CustomersArrived++;
            customer.OrderRequested += OnOrderRequested;
            customer.Departed += OnDeparted;

            int seat = FreeSeat();
            if (seat >= 0) m_Seats[seat] = customer;
            else m_SeatQueue.Add(customer);
            customer.ArrivedInside(seat);
            return seat;
        }

        /// <summary>The scene reports a customer has walked out the door.</summary>
        public void CustomerGone(CustomerLogic customer)
        {
            customer.ArrivedAtExit();
            m_Customers.Remove(customer);
        }

        int FreeSeat()
        {
            for (int i = 0; i < m_Seats.Length; i++) if (m_Seats[i] == null) return i;
            return -1;
        }

        void OnOrderRequested(CustomerLogic customer)
        {
            var choice = Preferences.ChooseOrder(AvailableDishes(), customer.Traits, m_Random);
            var reserved = choice != null ? RecipeMatcher.TryTake(choice, Storeroom) : null;
            if (reserved == null)
            {
                // Everything they could order is gone: they leave, with a smaller penalty than a walkout.
                customer.NothingToOrder();
                return;
            }
            var ticket = new Ticket(customer, choice, reserved);
            m_Tickets.Add(ticket);
            customer.PlaceOrder(choice);
            RefreshSoldOut();
            TicketChanged?.Invoke(ticket);
        }

        void OnDeparted(CustomerLogic customer)
        {
            // Free the seat and seat the next person in the queue.
            for (int i = 0; i < m_Seats.Length; i++)
            {
                if (m_Seats[i] != customer) continue;
                m_Seats[i] = null;
                if (!IsOver)
                {
                    while (m_SeatQueue.Count > 0)
                    {
                        var next = m_SeatQueue[0];
                        m_SeatQueue.RemoveAt(0);
                        if (next.State != CustomerState.Queueing) continue;
                        m_Seats[i] = next;
                        next.AssignSeat(i);
                        break;
                    }
                }
            }
            m_SeatQueue.Remove(customer);

            var ticket = TicketFor(customer);
            switch (customer.Departure)
            {
                case Departure.Paid:
                    Settle(customer, ticket);
                    break;
                case Departure.WalkedOut:
                    Ledger.Walkouts++;
                    Ledger.Renown += m_Economy.walkoutRenown;
                    CancelTicket(ticket);
                    break;
                case Departure.SoldOut:
                    Ledger.SoldOutLeaves++;
                    Ledger.Renown += m_Economy.soldOutRenown;
                    CancelTicket(ticket);
                    break;
                case Departure.ClosingTime:
                    CancelTicket(ticket);
                    break;
            }
            CustomerLeft?.Invoke(customer);
        }

        void Settle(CustomerLogic customer, Ticket ticket)
        {
            if (ticket == null) return;
            float match = Preferences.FlavorMatch(ticket.Reserved.Flavors, ticket.Recipe.station, customer.Traits);
            float satisfaction = ServiceEconomy.Satisfaction(ticket.DishQuality, match, customer.WaitFraction, m_Economy);
            Ledger.DishesServed++;
            Ledger.Gold += ServiceEconomy.Payment(ticket.DishValue);
            Ledger.Tips += ServiceEconomy.Tip(ticket.DishValue, satisfaction, customer.Traits.generosity, m_Economy);
            Ledger.Renown += ServiceEconomy.Renown(satisfaction, m_Economy);
            m_Tickets.Remove(ticket);
            TicketChanged?.Invoke(ticket);
        }

        /// <summary>
        /// Drops an unserved ticket. Ingredients that were only reserved (not cooked yet) go back
        /// to the storeroom; a dish already cooked is wasted.
        /// </summary>
        void CancelTicket(Ticket ticket)
        {
            if (ticket == null || ticket.State is TicketState.Served or TicketState.Cancelled) return;
            if (ticket.State == TicketState.Queued && ticket.Reserved != null) Storeroom.AddRange(ticket.Reserved.Used);
            ticket.State = TicketState.Cancelled;
            ticket.ClaimedBy = null;
            m_Tickets.Remove(ticket);
            RefreshSoldOut();
            TicketChanged?.Invoke(ticket);
        }

        Ticket TicketFor(CustomerLogic customer)
        {
            foreach (var t in m_Tickets) if (t.Customer == customer) return t;
            return null;
        }

        void RefreshSoldOut()
        {
            bool changed = false;
            foreach (var r in m_Menu)
            {
                bool soldOut = !RecipeMatcher.CanCook(r, Storeroom);
                if (soldOut ? m_SoldOut.Add(r) : m_SoldOut.Remove(r)) changed = true;
            }
            if (changed) SoldOutChanged?.Invoke();
        }

        // ---------- Kitchen ----------

        /// <summary>Oldest unclaimed ticket for a station, or null.</summary>
        public Ticket NextToCook(CookStation station)
        {
            foreach (var t in m_Tickets)
                if (t.State == TicketState.Queued && t.ClaimedBy == null && t.Recipe.station == station) return t;
            return null;
        }

        public bool StartCooking(Ticket ticket, object cook)
        {
            if (ticket == null || ticket.State != TicketState.Queued || ticket.ClaimedBy != null) return false;
            ticket.State = TicketState.Cooking;
            ticket.ClaimedBy = cook;
            TicketChanged?.Invoke(ticket);
            return true;
        }

        /// <summary>The cooking minigame finished; the dish goes on the pass.</summary>
        public void FinishCooking(Ticket ticket, float cookScore)
        {
            if (ticket == null || ticket.State != TicketState.Cooking) return;
            ticket.CookScore = Math.Clamp(cookScore, 0f, 1f);
            ticket.State = TicketState.Ready;
            ticket.ClaimedBy = null;
            TicketChanged?.Invoke(ticket);
        }

        /// <summary>Oldest unclaimed dish on the pass, or null.</summary>
        public Ticket NextToServe()
        {
            foreach (var t in m_Tickets)
                if (t.State == TicketState.Ready && t.ClaimedBy == null) return t;
            return null;
        }

        public bool StartDelivery(Ticket ticket, object carrier)
        {
            if (ticket == null || ticket.State != TicketState.Ready || ticket.ClaimedBy != null) return false;
            ticket.State = TicketState.Delivering;
            ticket.ClaimedBy = carrier;
            TicketChanged?.Invoke(ticket);
            return true;
        }

        /// <summary>The plate reached the table. Scores the dish and starts the customer eating.</summary>
        public void Delivered(Ticket ticket, float servingScore)
        {
            if (ticket == null || ticket.State != TicketState.Delivering) return;
            float minigame = DishScoring.MinigameScore(ticket.CookScore, servingScore, m_Scoring);
            ticket.DishQuality = DishScoring.DishQuality(ticket.Reserved.Used, minigame, m_Scoring);
            ticket.DishValue = DishScoring.DishValue(ticket.Recipe.baseValue, ticket.DishQuality);
            ticket.State = TicketState.Served;
            ticket.ClaimedBy = null;
            ticket.Customer.Serve(ticket.DishQuality);
            TicketChanged?.Invoke(ticket);
        }

        /// <summary>
        /// The plate was dropped: those ingredients are lost. The order goes back to the kitchen
        /// if the stock allows; otherwise the customer leaves as sold out.
        /// </summary>
        public void Dropped(Ticket ticket)
        {
            if (ticket == null || ticket.State != TicketState.Delivering) return;
            Ledger.DroppedDishes++;
            ticket.ClaimedBy = null;
            var again = RecipeMatcher.TryTake(ticket.Recipe, Storeroom);
            if (again != null)
            {
                ticket.Reserved = again;
                ticket.State = TicketState.Queued;
                RefreshSoldOut();
                TicketChanged?.Invoke(ticket);
                return;
            }
            ticket.State = TicketState.Cancelled;
            m_Tickets.Remove(ticket);
            RefreshSoldOut();
            TicketChanged?.Invoke(ticket);
            ticket.Customer.NothingToOrder();
        }
    }
}
