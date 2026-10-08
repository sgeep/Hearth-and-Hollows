using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Core.Minigames;
using Hearthdelve.Core.Pathfinding;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
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

        bool m_Started;

        /// <summary>The stations, the pass and the seats of the placed furniture (4f: set whenever the layout is built).</summary>
        // ---------- The Butcher Block (4f Checkpoint C; D17: mise en place at Prep) ----------

        TavernInteractable m_ButcherBlock;
        IngredientItem m_ButcherPart;

        /// <summary>The placed Butcher Block (null when it's in storage).</summary>
        public TavernInteractable ButcherBlock => m_ButcherBlock;
        /// <summary>The part on the block now (invalid when nothing is being cut).</summary>
        public IngredientItem ButcheringPart => m_ButcherPart;
        /// <summary>Breaking down a part is mise en place: at Prep, with a block out, and nothing else on the go.</summary>
        public bool CanButcher => m_Director != null && m_Director.Phase == TavernPhase.Prep && m_ButcherBlock != null && ActiveCook == null && FindPlayer();
        /// <summary>The cuts a finished breakdown gave (the Prep screen shows them).</summary>
        public event System.Action<IngredientStack> Butchered;

        public void ConfigureButcherBlock(TavernInteractable block)
        {
            m_ButcherBlock = block;
            // Used from the Prep screen (mise en place), never by walking up to it during service: no hint, no use.
            block?.SetAvailable(false);
        }

        /// <summary>
        /// Puts one part from the storeroom on the block and opens the cut (the keeper steps up to it). Nothing is taken until
        /// the cut finishes; stepping away leaves the part as it was.
        /// </summary>
        public bool Butcher(IngredientItem part)
        {
            if (!CanButcher || !part.IsValid || !part.Definition.Butcherable) return false;
            if (m_Director.Storeroom.CountMatching(item => item == part) <= 0) return false;
            m_ButcherPart = part;
            // At the block, facing it, while the knife works.
            Vector2 at = m_ButcherBlock.UsePoint;
            m_Player.transform.position = at;
            if (m_Player.TryGetComponent(out Rigidbody2D body)) body.position = at;
            OpenPanel(m_Director.KeeperMinigames.CreateButcher(part.Definition.butchering.maxCuts, new Hearthdelve.Core.Random.SeededRandom(UnityEngine.Random.Range(1, int.MaxValue))));
            return true;
        }

        void TickButcher(float dt)
        {
            if (m_Cancel != null && m_Cancel.WasPressedThisFrame())
            {
                m_ButcherPart = default;
                EndCook();
                return;
            }
            ActiveCook.Tick(dt, ReadMinigameInput());
            if (ActiveCook.IsComplete) FinishCook(ActiveCook.Evaluate());
        }

        void FinishButcher(float score)
        {
            IngredientItem part = m_ButcherPart;
            m_ButcherPart = default;
            EndCook();
            IngredientStack cuts = ButcherRules.Butcher(m_Director.Storeroom, part, score);
            if (cuts.IsEmpty) return;
            EventBus<PartButchered>.Publish(new PartButchered(part.Definition.id, cuts.Item.Definition.id, cuts.Count, score, "keeper"));
            Butchered?.Invoke(cuts);
        }

        public void Configure(TavernInteractable grill, TavernInteractable tap, TavernInteractable stewPot, TavernInteractable pass, TavernInteractable[] seats)
        {
            m_Grill = grill;
            m_Tap = tap;
            m_StewPot = stewPot;
            m_Pass = pass;
            m_Seats = seats ?? System.Array.Empty<TavernInteractable>();
            if (m_Started) HookDescriptions();
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
            m_Started = true;
            HookDescriptions();
        }

        void HookDescriptions()
        {
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
            if (m_ButcherPart.IsValid && ActiveCook != null)
            {
                TickButcher(Time.deltaTime);
                return;
            }
            if (!m_Director.IsServing)
            {
                if (ActiveCook != null || Carrying != null) StopWork();
                SetAvailability(false);
                // 4h: in the free daytime the Grill and the Tap cook the delve meal (the panel opens on use).
                if (m_Director.Phase == TavernPhase.Daytime && ActiveCook == null)
                    foreach (TavernInteractable station in new[] { m_Grill, m_Tap })
                        if (station != null) station.SetAvailable(true);
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

        bool StaffWorks(StaffStation station) => m_Director != null && m_Director.StaffAt(station) != null;
        StaffDefinition StaffOn(StaffStation station) => m_Director != null ? m_Director.StaffAt(station)?.Member : null;

        // ---------- Hints ----------

        TavernHint DescribeCook(TavernInteractable station, CookStation cook, StaffStation staffed)
        {
            // 4h: in the daytime a station cooks the delve meal.
            if (m_Director != null && m_Director.Phase == TavernPhase.Daytime) return TavernHint.Use(DelveMealHintKey);
            if (StaffWorks(staffed)) return new TavernHint(TavernHintKind.Staffed, staff: StaffOn(staffed));
            Ticket next = Session?.NextToCook(cook);
            return next != null ? new TavernHint(TavernHintKind.Cook, dish: next.Recipe) : TavernHint.Use(station.NameKey);
        }

        TavernHint DescribeStewPot()
        {
            if (StaffWorks(StaffStation.StewPot)) return new TavernHint(TavernHintKind.Staffed, staff: StaffOn(StaffStation.StewPot));
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

        /// <summary>UI key of the daytime station hint ("cook a delve meal"), set by the builder.</summary>
        public static string DelveMealHintKey { get; set; } = "surface.delve_meal";

        void OnInteracted(TavernInteracted e)
        {
            // 4h: in the daytime the Grill and the Tap open the delve meal's choice for that station.
            if (m_Director != null && m_Director.Phase == TavernPhase.Daytime && ActiveCook == null && e.Kind is TavernInteractableKind.Grill or TavernInteractableKind.Tap)
            {
                EventBus<DaytimePlaceUsed>.Publish(new DaytimePlaceUsed(e.Kind, e.Kind == TavernInteractableKind.Tap ? CookStation.Tap : CookStation.Grill));
                return;
            }
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
                    OpenPanel(m_Director.KeeperMinigames.CreateChop(session.Pot.ChopItems.Count, m_Random));
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
            OpenPanel(m_Director.KeeperMinigames.CreateCook(station));
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
            OpenPanel(m_Director.KeeperMinigames.CreateCook(recipe.station));
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
            if (m_ButcherPart.IsValid)
            {
                FinishButcher(score);
                return;
            }
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
            TavernDirector.RestoreInput();
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
            float left, width;
            if (ActiveCook is ButcherMinigame)
            {
                left = m_Director.Minigames.Butcher.boardLeft;
                width = m_Director.Minigames.Butcher.boardWidth;
            }
            else
            {
                left = m_Director.Minigames.Chop.boardLeft;
                width = m_Director.Minigames.Chop.boardWidth;
            }
            return (screenX / Mathf.Max(1f, Screen.width) - left) / Mathf.Max(0.01f, width);
        }

        // ---------- Carrying ----------

        void PickUp(Ticket ticket)
        {
            if (ticket == null || !Session.StartDelivery(ticket, this)) return;
            CarryTicket = ticket;
            Carrying = m_Director.KeeperMinigames.CreateServing();
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
