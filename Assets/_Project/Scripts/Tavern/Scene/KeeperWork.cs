using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Core.Minigames;
using Hearthdelve.Core.Pathfinding;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Navigation;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// The keeper's work during service (the prototype's TavernPlayer, top-down). Using the Grill or Tap
    /// cooks the next order there in its minigame panel (Minigame input map); using the empty Stew Pot
    /// puts a batch on and chops it; using the pass picks up the next plate, or puts the carried one back.
    /// Carrying a plate is the Serving minigame: the keeper walks at the carry speed, customers walking
    /// across the floor bump and spill it, and using a seated customer who ordered that dish serves it.
    /// Every rule is in <see cref="ServiceSession"/> and the minigames; this connects them to the room.
    /// </summary>
    public sealed class KeeperWork : MonoBehaviour
    {
        [SerializeField] TavernInteractable m_Grill;
        [SerializeField] TavernInteractable m_Tap;
        [SerializeField] TavernInteractable m_StewPot;
        [SerializeField] TavernInteractable m_Pass;
        [SerializeField] TavernInteractable[] m_Seats = System.Array.Empty<TavernInteractable>();

        static readonly List<GridCell> s_Path = new();
        readonly IRandom m_Random = new SeededRandom();
        TavernDirector m_Director;
        Character m_Player;
        CharacterMovement m_PlayerMovement;
        TopDownController m_PlayerController;
        Collider2D m_PlayerBody;
        CarryView m_CarryView;
        InputAction m_Aim, m_Action, m_Cancel, m_Point;
        Vector2 m_LastPointer;
        Vector2 m_CarryFrom;
        float m_WalkSpeed;

        public static KeeperWork Instance { get; private set; }

        /// <summary>The minigame open in the station panel (Grill, Tap or Chop), or null.</summary>
        public IMinigame ActiveCook { get; private set; }
        public Ticket CookTicket { get; private set; }
        public bool ChoppingPot { get; private set; }
        public ServingMinigame Carrying { get; private set; }
        /// <summary>The dish being cooked as the delve meal (daytime), if any.</summary>
        public RecipeDefinition DelveMeal { get; private set; }
        CookedIngredients m_DelveMealUsed;
        public Ticket CarryTicket { get; private set; }

        public void Configure(TavernInteractable grill, TavernInteractable tap, TavernInteractable stewPot, TavernInteractable pass, TavernInteractable[] seats)
        {
            m_Grill = grill;
            m_Tap = tap;
            m_StewPot = stewPot;
            m_Pass = pass;
            m_Seats = seats;
        }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable() => EventBus<TavernInteracted>.Subscribe(OnInteracted);
        void OnDisable() => EventBus<TavernInteracted>.Unsubscribe(OnInteracted);

        void Start()
        {
            m_Director = TavernDirector.Instance;
            m_Aim = InputMaps.Find(InputMaps.Minigame, MinigameActions.Aim);
            m_Action = InputMaps.Find(InputMaps.Minigame, MinigameActions.Action);
            m_Cancel = InputMaps.Find(InputMaps.Minigame, MinigameActions.Cancel);
            m_Point = InputMaps.Find(InputMaps.Minigame, MinigameActions.Point);
            if (m_Grill != null) m_Grill.Describe = () => DescribeCook(m_Grill, CookStation.Grill, StaffStation.Grill);
            if (m_Tap != null) m_Tap.Describe = () => DescribeCook(m_Tap, CookStation.Tap, StaffStation.Tap);
            if (m_StewPot != null) m_StewPot.Describe = DescribeStewPot;
            if (m_Pass != null) m_Pass.Describe = DescribePass;
            for (int i = 0; i < m_Seats.Length; i++)
            {
                int seat = i;
                m_Seats[i].Describe = () => DescribeSeat(seat);
            }
        }

        bool FindPlayer()
        {
            if (m_Player != null) return true;
            GameObject found = GameObject.FindWithTag("Player");
            if (found == null || !found.TryGetComponent(out m_Player)) return false;
            m_PlayerMovement = m_Player.FindAbility<CharacterMovement>();
            m_PlayerController = found.GetComponent<TopDownController>();
            m_PlayerBody = found.GetComponent<Collider2D>();
            m_CarryView = found.GetComponentInChildren<CarryView>(true);
            m_WalkSpeed = m_PlayerMovement != null ? m_PlayerMovement.WalkSpeed : 6f;
            return true;
        }

        ServiceSession Session => m_Director != null ? m_Director.Session : null;
        StaffAgent Staff => m_Director != null ? m_Director.Staff : null;

        void Update()
        {
            if (m_Director == null || !FindPlayer()) return;
            if (DelveMeal != null)
            {
                TickDelveMeal(Time.deltaTime);
                return;
            }
            if (!m_Director.IsServing)
            {
                if (ActiveCook != null || Carrying != null) StopWork();
                SetAvailability(false);
                return;
            }
            SetAvailability(true);
            float dt = Time.deltaTime;
            if (ActiveCook != null) TickCook(dt);
            else if (Carrying != null) TickCarry(dt);
        }

        /// <summary>Stations while empty-handed; the pass always; seats with someone waiting while carrying.</summary>
        void SetAvailability(bool serving)
        {
            bool carrying = Carrying != null;
            foreach (TavernInteractable station in new[] { m_Grill, m_Tap, m_StewPot })
                if (station != null) station.SetAvailable(serving && !carrying);
            if (m_Pass != null) m_Pass.SetAvailable(serving);
            for (int i = 0; i < m_Seats.Length; i++)
                m_Seats[i].SetAvailable(serving && carrying && CustomerAt(i) is { Logic: { State: CustomerState.WaitingForFood } });
        }

        CustomerAgent CustomerAt(int seat)
        {
            foreach (CustomerAgent agent in m_Director.Agents)
                if (agent != null && agent.IsSeated && agent.Logic.Seat == seat) return agent;
            return null;
        }

        bool StaffWorks(StaffStation station) => Staff != null && Staff.Assignment == station;

        // ---------- Hints ----------

        TavernHint DescribeCook(TavernInteractable station, CookStation cook, StaffStation staffed)
        {
            if (StaffWorks(staffed)) return new TavernHint(TavernHintKind.Staffed, staff: Staff.Member);
            Ticket next = Session?.NextToCook(cook);
            return next != null ? new TavernHint(TavernHintKind.Cook, dish: next.Recipe) : TavernHint.Use(station.NameKey);
        }

        TavernHint DescribeStewPot()
        {
            if (StaffWorks(StaffStation.StewPot)) return new TavernHint(TavernHintKind.Staffed, staff: Staff.Member);
            StewPot pot = Session?.Pot;
            if (pot == null) return TavernHint.Use(m_StewPot.NameKey);
            switch (pot.State)
            {
                case PotState.Empty:
                    RecipeDefinition next = Session.NextBatch();
                    return next != null ? new TavernHint(TavernHintKind.StartStew, dish: next) : TavernHint.Use(m_StewPot.NameKey);
                case PotState.Simmering:
                    return new TavernHint(TavernHintKind.Simmering, dish: pot.Recipe);
                case PotState.Ready:
                    return new TavernHint(TavernHintKind.StewReady, dish: pot.Recipe, count: pot.Helpings);
                default:
                    return TavernHint.Use(m_StewPot.NameKey);
            }
        }

        TavernHint DescribePass()
        {
            if (CarryTicket != null) return new TavernHint(TavernHintKind.PutBack, dish: CarryTicket.Recipe);
            Ticket ready = Session?.NextToServe();
            return ready != null ? new TavernHint(TavernHintKind.PickUp, dish: ready.Recipe) : TavernHint.Use(m_Pass.NameKey);
        }

        TavernHint DescribeSeat(int seat)
        {
            CustomerAgent customer = CustomerAt(seat);
            if (CarryTicket == null || customer == null) return default;
            return Session.CanDeliver(CarryTicket, customer.Logic)
                ? new TavernHint(TavernHintKind.Serve, dish: CarryTicket.Recipe)
                : new TavernHint(TavernHintKind.WrongDish, dish: customer.Logic.Order);
        }

        // ---------- Using things ----------

        void OnInteracted(TavernInteracted e)
        {
            if (m_Director == null || !m_Director.IsServing || ActiveCook != null || !FindPlayer()) return;
            ServiceSession session = Session;
            switch (e.Kind)
            {
                case TavernInteractableKind.Grill:
                    if (!StaffWorks(StaffStation.Grill)) StartCooking(CookStation.Grill);
                    break;
                case TavernInteractableKind.Tap:
                    if (!StaffWorks(StaffStation.Tap)) StartCooking(CookStation.Tap);
                    break;
                case TavernInteractableKind.StewPot:
                    if (StaffWorks(StaffStation.StewPot) || session.Pot.State != PotState.Empty) break;
                    RecipeDefinition batch = session.NextBatch();
                    if (batch == null || !session.StartBatch(batch, this)) break;
                    ChoppingPot = true;
                    OpenPanel(m_Director.Minigames.CreateChop(session.Pot.ChopItems.Count, m_Random));
                    break;
                case TavernInteractableKind.Pass:
                    if (Carrying != null)
                    {
                        RecipeDefinition back = CarryTicket.Recipe;
                        session.PutBack(CarryTicket);
                        EndCarry();
                        EventBus<KeeperPlate>.Publish(new KeeperPlate(PlateMoment.PutBack, back));
                    }
                    else PickUp(session.NextToServe());
                    break;
                case TavernInteractableKind.Seat:
                    int seat = System.Array.IndexOf(m_Seats, e.Target);
                    CustomerAgent customer = seat >= 0 ? CustomerAt(seat) : null;
                    if (Carrying == null || customer == null || !session.CanDeliver(CarryTicket, customer.Logic)) break;
                    Carrying.Deliver(ShortestWalk(m_CarryFrom, m_Player.transform.position));
                    float served = Carrying.Evaluate();
                    RecipeDefinition dish = CarryTicket.Recipe;
                    session.Deliver(CarryTicket, customer.Logic, served);
                    EndCarry();
                    EventBus<KeeperPlate>.Publish(new KeeperPlate(PlateMoment.Served, dish, served));
                    break;
            }
        }

        void StartCooking(CookStation station)
        {
            ServiceSession session = Session;
            Ticket ticket = session.NextToCook(station);
            if (ticket == null || !session.StartCooking(ticket, this)) return;
            CookTicket = ticket;
            OpenPanel(m_Director.Minigames.CreateCook(station));
        }

        // ---------- The delve meal (daytime) ----------

        /// <summary>
        /// Takes one serving from the storeroom and cooks it at its station's panel, as in service. Finishing eats
        /// it (the buff for today's delve); Cancel puts the ingredients back.
        /// </summary>
        public bool CookDelveMeal(RecipeDefinition recipe)
        {
            if (ActiveCook != null || recipe == null || recipe.station == CookStation.StewPot || m_Director == null || !FindPlayer()) return false;
            CookedIngredients used = RecipeMatcher.TryTake(recipe, m_Director.Storeroom);
            if (used == null) return false;
            DelveMeal = recipe;
            m_DelveMealUsed = used;
            OpenPanel(m_Director.Minigames.CreateCook(recipe.station));
            return true;
        }

        void TickDelveMeal(float dt)
        {
            if (m_Cancel != null && m_Cancel.WasPressedThisFrame())
            {
                CancelDelveMeal();
                return;
            }
            ActiveCook.Tick(dt, ReadMinigameInput());
            if (ActiveCook.IsComplete) FinishCook(ActiveCook.Evaluate());
        }

        void CancelDelveMeal()
        {
            if (DelveMeal == null) return;
            m_Director.Storeroom.AddRange(m_DelveMealUsed.Used);
            DelveMeal = null;
            m_DelveMealUsed = null;
            EndCook();
        }

        void OpenPanel(IMinigame game)
        {
            ActiveCook = game;
            ActiveCook.Begin();
            m_LastPointer = ReadPointer();
            m_PlayerMovement?.SetMovement(Vector2.zero);
            InputMaps.Activate(InputMaps.Minigame);
        }

        void TickCook(float dt)
        {
            ServiceSession session = Session;
            bool stillMine = ChoppingPot
                ? session.Pot.State == PotState.Chopping && ReferenceEquals(session.Pot.ClaimedBy, this)
                : CookTicket != null && CookTicket.State == TicketState.Cooking;
            if (!stillMine)
            {
                EndCook();
                return;
            }
            if (m_Cancel != null && m_Cancel.WasPressedThisFrame())
            {
                if (ChoppingPot) session.AbandonBatch(this);
                else session.AbandonCooking(CookTicket);
                EndCook();
                return;
            }
            ActiveCook.Tick(dt, ReadMinigameInput());
            if (ActiveCook.IsComplete) FinishCook(ActiveCook.Evaluate());
        }

        /// <summary>Ends the open minigame with a score (tests and debugging; normally the minigame finishes itself).</summary>
        public void FinishCook(float score)
        {
            if (ActiveCook == null) return;
            if (DelveMeal != null)
            {
                RecipeDefinition recipe = DelveMeal;
                CookedIngredients used = m_DelveMealUsed;
                DelveMeal = null;
                m_DelveMealUsed = null;
                EndCook();
                m_Director.EatDelveMeal(recipe, used, score);
                return;
            }
            if (ChoppingPot) Session.FinishChopping(this, score);
            else Session.FinishCooking(CookTicket, score);
            EndCook();
        }

        void EndCook()
        {
            ActiveCook = null;
            CookTicket = null;
            ChoppingPot = false;
            if (m_Director != null && m_Director.IsServing) InputMaps.Activate(InputMaps.Tavern);
            else InputMaps.ActivateUIOnly();
        }

        MinigameInput ReadMinigameInput()
        {
            Vector2 pointer = ReadPointer();
            var input = new MinigameInput
            {
                ActionPressed = m_Action != null && m_Action.WasPressedThisFrame(),
                ActionHeld = m_Action != null && m_Action.IsPressed(),
                ActionReleased = m_Action != null && m_Action.WasReleasedThisFrame(),
                Aim = m_Aim != null ? m_Aim.ReadValue<Vector2>() : Vector2.zero,
                PointerActive = pointer != m_LastPointer,
                Pointer = BoardFraction(pointer.x),
            };
            m_LastPointer = pointer;
            return input;
        }

        Vector2 ReadPointer() => m_Point != null ? m_Point.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>Screen x (pixels) to 0–1 across the chopping board, which the panel draws at the same share of the screen.</summary>
        float BoardFraction(float screenX)
        {
            ChopSettings chop = m_Director.Minigames.Chop;
            return (screenX / Mathf.Max(1f, Screen.width) - chop.boardLeft) / Mathf.Max(0.01f, chop.boardWidth);
        }

        // ---------- Carrying ----------

        void PickUp(Ticket ticket)
        {
            if (ticket == null || !Session.StartDelivery(ticket, this)) return;
            CarryTicket = ticket;
            Carrying = m_Director.Minigames.CreateServing();
            Carrying.Begin();
            m_CarryFrom = m_Player.transform.position;
            if (m_PlayerMovement != null) m_PlayerMovement.MovementSpeed = Carrying.Settings.carrySpeed;
            m_CarryView?.Show(ticket.Recipe.icon);
            EventBus<KeeperPlate>.Publish(new KeeperPlate(PlateMoment.PickedUp, ticket.Recipe));
        }

        void TickCarry(float dt)
        {
            if (CarryTicket.State != TicketState.Delivering)
            {
                EndCarry();
                return;
            }
            Carrying.Tick(dt, default);
            float strength = BumpStrength(m_PlayerBody, m_PlayerController, Carrying.Settings.carrySpeed, m_Director.Agents);
            if (strength > 0f && Carrying.RegisterBump(strength))
                EventBus<ServingBumped>.Publish(new ServingBumped(strength, Carrying.Spill, Carrying.Dropped, true));
            m_CarryView?.SetSpill(Carrying.Spill);
            if (!Carrying.Dropped) return;
            RecipeDefinition dropped = CarryTicket.Recipe;
            Session.Dropped(CarryTicket);
            EndCarry();
            EventBus<KeeperPlate>.Publish(new KeeperPlate(PlateMoment.Dropped, dropped));
        }

        void EndCarry()
        {
            Carrying = null;
            CarryTicket = null;
            if (m_PlayerMovement != null) m_PlayerMovement.MovementSpeed = m_WalkSpeed;
            m_CarryView?.Hide();
        }

        /// <summary>
        /// How hard a customer walking across the floor bumped into this carrier (0: no one). Bodies
        /// overlapping count; the speed they close at sets the strength (<see cref="ServingMinigame.BumpStrength"/>).
        /// </summary>
        public static float BumpStrength(Collider2D carrier, TopDownController carrierController, float carrySpeed, IReadOnlyList<CustomerAgent> customers)
        {
            if (carrier == null) return 0f;
            Bounds mine = carrier.bounds;
            Vector2 myVelocity = carrierController != null ? (Vector2)carrierController.CurrentMovement : Vector2.zero;
            float strongest = 0f;
            foreach (CustomerAgent customer in customers)
            {
                if (customer == null || !customer.IsWalking || !customer.TryGetComponent(out Collider2D body) || !body.enabled) continue;
                if (!mine.Intersects(body.bounds)) continue;
                Vector2 theirs = customer.Velocity;
                strongest = Mathf.Max(strongest, ServingMinigame.BumpStrength((myVelocity - theirs).magnitude, carrySpeed));
            }
            return strongest;
        }

        /// <summary>The shortest walkable distance between two points (straight-line if there's no grid or path).</summary>
        public static float ShortestWalk(Vector2 from, Vector2 to)
        {
            NavGrid grid = NavGrid.Current;
            if (grid != null && GridPathfinder.TryFindPath(grid.Map, grid.Space.ToCell(from), grid.Space.ToCell(to), s_Path))
                return GridPathfinder.Length(s_Path);
            return Vector2.Distance(from, to);
        }

        /// <summary>Service is over (or the keeper sets off): put everything down. An unfinished delve meal goes back in the storeroom.</summary>
        public void StopWork()
        {
            if (DelveMeal != null) CancelDelveMeal();
            if (ActiveCook != null) EndCook();
            if (Carrying != null) EndCarry();
        }
    }
}
