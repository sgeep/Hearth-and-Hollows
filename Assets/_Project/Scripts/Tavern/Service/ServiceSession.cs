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

    /// <summary>
    /// One order. Ingredients are reserved from the storeroom when it's placed. If its customer
    /// leaves once cooking has started, the dish becomes a spare (<see cref="Customer"/> null)
    /// that can go to anyone else who ordered the same dish.
    /// </summary>
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
        /// <summary>Who the dish is for; null for a spare.</summary>
        public CustomerLogic Customer { get; internal set; }
        public bool IsSpare => Customer == null;
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
            Storeroom storeroom, IReadOnlyList<RecipeDefinition> menu, int seatCount, IRandom random, StewPotSettings? pot = null)
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
            Pot = new StewPot(pot ?? StewPotSettings.Default);
            RefreshSoldOut();
        }

        public Storeroom Storeroom { get; }
        /// <summary>The stew pot (batch cooking for StewPot recipes).</summary>
        public StewPot Pot { get; }
        public ServiceLedger Ledger { get; } = new();
        public IReadOnlyList<RecipeDefinition> Menu => m_Menu;
        public IReadOnlyList<Ticket> Tickets => m_Tickets;
        public IReadOnlyList<CustomerLogic> Customers => m_Customers;
        public int SeatCount => m_Seats.Length;
        public float Elapsed { get; private set; }
        public float Remaining => Math.Max(0f, m_Settings.lengthSeconds - Elapsed);
        public bool IsLastOrders => Remaining <= m_Settings.lastOrdersSeconds;
        public bool IsOver { get; private set; }
        /// <summary>Service ended before the clock ran out because everything sold out.</summary>
        public bool ClosedEarly { get; private set; }
        /// <summary>Every dish on the menu is sold out: the door closes to new customers.</summary>
        public bool AllSoldOut => m_SoldOut.Count == m_Menu.Count;
        public bool CanAdmitCustomer => !IsOver && !IsLastOrders && !AllSoldOut && m_Customers.Count < m_Settings.maxCustomers;

        public event Action SoldOutChanged;
        public event Action<Ticket> TicketChanged;
        public event Action PotChanged;
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
            if (Pot.Tick(deltaTime))
            {
                PotChanged?.Invoke();
                LadleStew();
            }
            foreach (var c in m_Customers.ToArray()) c.Tick(deltaTime);
            if (Elapsed >= m_Settings.lengthSeconds) End();
            else if (AllSoldOut && !HasOpenOrders && !AnyoneEating)
            {
                // Nothing left to sell and nothing left to serve: close early.
                ClosedEarly = true;
                End();
            }
        }

        /// <summary>A customer is still waiting on an order (queued, cooking, on the pass, or being carried).</summary>
        public bool HasOpenOrders
        {
            get
            {
                foreach (var t in m_Tickets)
                    if (!t.IsSpare && t.State is TicketState.Queued or TicketState.Cooking or TicketState.Ready or TicketState.Delivering)
                        return true;
                return false;
            }
        }

        bool AnyoneEating
        {
            get
            {
                foreach (var c in m_Customers) if (c.State == CustomerState.Eating) return true;
                return false;
            }
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
            var choice = Preferences.ChooseOrder(AvailableDishes(), customer.Traits, m_Economy, m_Random);
            var spare = choice != null ? SpareOf(choice) : null;
            if (spare != null)
            {
                // A dish someone else left behind: it's theirs, and no more stock is used.
                spare.Customer = customer;
                customer.PlaceOrder(choice);
                TicketChanged?.Invoke(spare);
                return;
            }
            if (choice != null && choice.station == CookStation.StewPot)
            {
                // Stew comes out of the pot: the order waits for a helping (ingredients are taken per batch).
                var order = new Ticket(customer, choice, null);
                m_Tickets.Add(order);
                customer.PlaceOrder(choice);
                TicketChanged?.Invoke(order);
                LadleStew();
                RefreshSoldOut();
                return;
            }
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
                    Release(ticket);
                    break;
                case Departure.SoldOut:
                    Ledger.SoldOutLeaves++;
                    Ledger.Renown += m_Economy.soldOutRenown;
                    Release(ticket);
                    break;
                case Departure.ClosingTime:
                    Release(ticket);
                    break;
            }
            CustomerLeft?.Invoke(customer);
        }

        void Settle(CustomerLogic customer, Ticket ticket)
        {
            if (ticket == null) return;
            float match = Preferences.FlavorMatch(ticket.Reserved.Flavors, ticket.Recipe.station, customer.Traits, m_Economy);
            float satisfaction = ServiceEconomy.Satisfaction(ticket.DishQuality, match, customer.WaitFraction, m_Economy);
            Ledger.DishesServed++;
            Ledger.Gold += ServiceEconomy.Payment(ticket.DishValue);
            Ledger.Tips += ServiceEconomy.Tip(ticket.DishValue, satisfaction, customer.Traits.generosity, m_Economy);
            Ledger.Renown += ServiceEconomy.Renown(satisfaction, m_Economy);
            m_Tickets.Remove(ticket);
            TicketChanged?.Invoke(ticket);
        }

        /// <summary>
        /// The ticket's customer is gone. An order nobody has started cooking is cancelled and its
        /// reserved ingredients go back to the storeroom; a dish already cooking or cooked stays
        /// as a spare for someone else who orders it.
        /// </summary>
        void Release(Ticket ticket)
        {
            if (ticket == null || ticket.State is TicketState.Served or TicketState.Cancelled) return;
            ticket.Customer = null;
            if (ticket.State == TicketState.Queued)
            {
                if (ticket.Reserved != null) Storeroom.AddRange(ticket.Reserved.Used);
                Cancel(ticket);
                return;
            }
            TicketChanged?.Invoke(ticket);
        }

        void Cancel(Ticket ticket)
        {
            ticket.State = TicketState.Cancelled;
            ticket.ClaimedBy = null;
            m_Tickets.Remove(ticket);
            TicketChanged?.Invoke(ticket);
        }

        Ticket TicketFor(CustomerLogic customer, Ticket except = null)
        {
            foreach (var t in m_Tickets) if (t.Customer == customer && t != except) return t;
            return null;
        }

        /// <summary>The oldest spare of a dish not already on its way to someone, or null.</summary>
        Ticket SpareOf(RecipeDefinition recipe)
        {
            foreach (var t in m_Tickets)
                if (t.IsSpare && t.Recipe == recipe && t.State is TicketState.Cooking or TicketState.Ready or TicketState.Delivering)
                    return t;
            return null;
        }

        /// <summary>
        /// Marks dishes the stock can no longer make as sold out. Sold out is final for the night:
        /// ingredients that come back later (a walkout's reservation) stay in the storeroom.
        /// </summary>
        void RefreshSoldOut()
        {
            bool changed = false;
            foreach (var r in m_Menu)
                if (!m_SoldOut.Contains(r) && !CanStillMake(r) && m_SoldOut.Add(r)) changed = true;
            if (changed) SoldOutChanged?.Invoke();
        }

        bool CanStillMake(RecipeDefinition recipe)
        {
            if (recipe.station != CookStation.StewPot) return RecipeMatcher.CanCook(recipe, Storeroom);
            // Stew: helpings the pot can still promise, plus the fewest helpings each batch the
            // storeroom can still make would give, must cover the orders already waiting.
            int batches = RecipeMatcher.ServingsAvailable(recipe, Storeroom);
            return Pot.Capacity(recipe) + batches * Pot.Settings.minHelpings - WaitingForStew(recipe) > 0;
        }

        /// <summary>Stew orders still waiting for a helping.</summary>
        public int WaitingForStew(RecipeDefinition recipe)
        {
            int n = 0;
            foreach (var t in m_Tickets)
                if (t.Recipe == recipe && t.State == TicketState.Queued && t.Recipe.station == CookStation.StewPot) n++;
            return n;
        }

        // ---------- Stew pot ----------

        /// <summary>
        /// The stew to put on next, or null if the pot is busy or nothing can be made: the one
        /// with the most orders waiting, else the first stew still on the menu.
        /// </summary>
        public RecipeDefinition NextBatch()
        {
            if (IsOver || Pot.State != PotState.Empty) return null;
            RecipeDefinition best = null;
            int bestWaiting = 0;
            foreach (var r in m_Menu)
            {
                if (r.station != CookStation.StewPot || !RecipeMatcher.CanCook(r, Storeroom)) continue;
                int waiting = WaitingForStew(r);
                if (waiting == 0 && m_SoldOut.Contains(r)) continue;
                if (best == null || waiting > bestWaiting)
                {
                    best = r;
                    bestWaiting = waiting;
                }
            }
            return best;
        }

        /// <summary>Takes a batch of ingredients into the empty pot; <paramref name="cook"/> then chops them.</summary>
        public bool StartBatch(RecipeDefinition recipe, object cook)
        {
            if (IsOver || Pot.State != PotState.Empty || recipe == null || recipe.station != CookStation.StewPot || !m_Menu.Contains(recipe)) return false;
            var batch = RecipeMatcher.TryTake(recipe, Storeroom);
            if (batch == null) return false;
            Pot.Fill(recipe, batch, cook);
            PotChanged?.Invoke();
            RefreshSoldOut();
            return true;
        }

        /// <summary>The cook walked away mid-chop: the ingredients go back to the storeroom.</summary>
        public void AbandonBatch(object cook)
        {
            if (Pot.State != PotState.Chopping || Pot.ClaimedBy != cook) return;
            Storeroom.AddRange(Pot.Batch.Used);
            Pot.Empty();
            PotChanged?.Invoke();
        }

        /// <summary>Chopping finished: accuracy sets the helpings, then the pot simmers on its own.</summary>
        public void FinishChopping(object cook, float chopScore)
        {
            if (Pot.State != PotState.Chopping || Pot.ClaimedBy != cook) return;
            Pot.StartSimmering(chopScore);
            PotChanged?.Invoke();
            LadleStew();
            RefreshSoldOut();
        }

        /// <summary>Ladles a helping onto the pass for each waiting stew order, oldest first, while the pot has any.</summary>
        void LadleStew()
        {
            bool ladled = false;
            while (Pot.State == PotState.Ready)
            {
                Ticket next = null;
                foreach (var t in m_Tickets)
                {
                    if (t.Recipe != Pot.Recipe || t.State != TicketState.Queued || t.IsSpare) continue;
                    next = t;
                    break;
                }
                if (next == null) break;
                next.Reserved = Pot.Batch;
                next.CookScore = 1f; // chopping sets the helpings, not the quality
                next.State = TicketState.Ready;
                Pot.TakeHelping();
                ladled = true;
                TicketChanged?.Invoke(next);
            }
            if (ladled) PotChanged?.Invoke();
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

        /// <summary>The cook walked away mid-minigame: the order goes back in the queue untouched.</summary>
        public void AbandonCooking(Ticket ticket)
        {
            if (ticket == null || ticket.State != TicketState.Cooking) return;
            ticket.State = TicketState.Queued;
            ticket.ClaimedBy = null;
            TicketChanged?.Invoke(ticket);
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

        /// <summary>
        /// Oldest unclaimed dish on the pass that someone is waiting for; failing that, the oldest
        /// spare (unless <paramref name="includeSpares"/> is false). Null if the pass is empty.
        /// </summary>
        public Ticket NextToServe(bool includeSpares = true)
        {
            Ticket spare = null;
            foreach (var t in m_Tickets)
            {
                if (t.State != TicketState.Ready || t.ClaimedBy != null) continue;
                if (!t.IsSpare) return t;
                spare ??= t;
            }
            return includeSpares ? spare : null;
        }

        public bool StartDelivery(Ticket ticket, object carrier)
        {
            if (ticket == null || ticket.State != TicketState.Ready || ticket.ClaimedBy != null) return false;
            ticket.State = TicketState.Delivering;
            ticket.ClaimedBy = carrier;
            TicketChanged?.Invoke(ticket);
            return true;
        }

        /// <summary>Whether this carried plate can be served to this customer: they're waiting and ordered this dish.</summary>
        public bool CanDeliver(Ticket ticket, CustomerLogic customer) =>
            ticket != null && ticket.State == TicketState.Delivering && customer != null &&
            customer.State == CustomerState.WaitingForFood && customer.Order == ticket.Recipe;

        /// <summary>
        /// Serves a carried plate to <paramref name="customer"/>, who may not be the one it was
        /// cooked for. Their own order then passes to the plate's original customer (or, for a
        /// spare, is released: cancelled if not started, otherwise a spare itself). Scores the
        /// dish and starts them eating. Returns false if they can't take it.
        /// </summary>
        public bool Deliver(Ticket ticket, CustomerLogic customer, float servingScore)
        {
            if (!CanDeliver(ticket, customer)) return false;
            if (ticket.Customer != customer)
            {
                var theirs = TicketFor(customer, except: ticket);
                var original = ticket.Customer;
                ticket.Customer = customer;
                if (theirs != null)
                {
                    if (original != null)
                    {
                        theirs.Customer = original;
                        TicketChanged?.Invoke(theirs);
                    }
                    else Release(theirs);
                }
            }

            float minigame = DishScoring.MinigameScore(ticket.CookScore, servingScore, m_Scoring);
            ticket.DishQuality = DishScoring.DishQuality(ticket.Reserved.Used, minigame, m_Scoring);
            ticket.DishValue = DishScoring.DishValue(ticket.Recipe.baseValue, ticket.DishQuality);
            ticket.State = TicketState.Served;
            ticket.ClaimedBy = null;
            customer.Serve(ticket.DishQuality);
            TicketChanged?.Invoke(ticket);
            return true;
        }

        /// <summary>The carrier put the plate back on the pass.</summary>
        public void PutBack(Ticket ticket)
        {
            if (ticket == null || ticket.State != TicketState.Delivering) return;
            ticket.State = TicketState.Ready;
            ticket.ClaimedBy = null;
            TicketChanged?.Invoke(ticket);
        }

        /// <summary>
        /// The plate was dropped: those ingredients are lost. The order goes back to the kitchen
        /// if the stock allows; otherwise the customer leaves as sold out. A dropped spare is just gone.
        /// </summary>
        public void Dropped(Ticket ticket)
        {
            if (ticket == null || ticket.State != TicketState.Delivering) return;
            Ledger.DroppedDishes++;
            ticket.ClaimedBy = null;
            if (ticket.IsSpare)
            {
                Cancel(ticket);
                return;
            }
            if (ticket.Recipe.station == CookStation.StewPot)
            {
                // Back in line for the next helping.
                ticket.Reserved = null;
                ticket.CookScore = 0f;
                ticket.State = TicketState.Queued;
                TicketChanged?.Invoke(ticket);
                LadleStew();
                RefreshSoldOut();
                return;
            }
            var again = RecipeMatcher.TryTake(ticket.Recipe, Storeroom);
            if (again != null)
            {
                ticket.Reserved = again;
                ticket.State = TicketState.Queued;
                RefreshSoldOut();
                TicketChanged?.Invoke(ticket);
                return;
            }
            var customer = ticket.Customer;
            Cancel(ticket);
            customer.NothingToOrder();
        }
    }
}
