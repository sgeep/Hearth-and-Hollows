using Hearthdelve.Core.Input;
using Hearthdelve.Core.Minigames;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.Tavern.Scene
{
    public enum PlayerHint
    {
        None,
        /// <summary>At a cooking station with an order waiting.</summary>
        Cook,
        /// <summary>At the pass with a dish ready.</summary>
        PickUp,
        /// <summary>The staff helper is working this station.</summary>
        Staffed,
        /// <summary>At a station with nothing to do.</summary>
        Idle,
        /// <summary>Carrying a plate next to someone waiting for that dish.</summary>
        Serve,
        /// <summary>Carrying a plate next to someone waiting for a different dish (<see cref="TavernPlayer.HintRecipe"/> is theirs).</summary>
        WrongDish,
        /// <summary>Carrying a plate at the pass.</summary>
        PutBack,
        /// <summary>At the empty stew pot with a stew to make (<see cref="TavernPlayer.HintRecipe"/>).</summary>
        StartStew,
        /// <summary>At the stew pot while it simmers.</summary>
        Simmering,
        /// <summary>At the stew pot while it has helpings left.</summary>
        StewReady,
    }

    /// <summary>
    /// The keeper during service: walks the floor (Tavern map), cooks at the Grill/Tap (Minigame
    /// map), and carries plates from the pass (the Serving minigame drives movement while
    /// carrying). A plate is served with Interact next to anyone waiting for that dish, or put
    /// back with Interact at the pass. At the empty stew pot, Interact puts a batch on and starts
    /// the chopping minigame (mouse or stick moves the knife).
    /// </summary>
    public sealed class TavernPlayer : MonoBehaviour
    {
        [SerializeField] SpriteRenderer m_Body;
        [SerializeField] SpriteRenderer m_Plate;

        TavernDirector m_Director;
        InputAction m_Move, m_Interact, m_Aim, m_Action, m_MinigameCancel, m_Point;
        Station m_Near;
        CustomerAgent m_ServeTarget;
        readonly IRandom m_Random = new SeededRandom();
        Vector2 m_LastPointer;
        bool m_ChoppingPot;
        RecipeDefinition m_BreakfastRecipe;
        CookedIngredients m_BreakfastIngredients;

        public IMinigame ActiveCook { get; private set; }
        public Ticket CookTicket { get; private set; }
        public ServingMinigame Carrying { get; private set; }
        public Ticket CarryTicket { get; private set; }
        public PlayerHint Hint { get; private set; }
        public RecipeDefinition HintRecipe { get; private set; }
        public float X => transform.position.x;

        public void Configure(SpriteRenderer body, SpriteRenderer plate)
        {
            m_Body = body;
            m_Plate = plate;
        }

        void Start()
        {
            m_Director = TavernDirector.Instance;
            m_Move = InputMaps.Find(InputMaps.Tavern, TavernActions.Move);
            m_Interact = InputMaps.Find(InputMaps.Tavern, TavernActions.Interact);
            m_Aim = InputMaps.Find(InputMaps.Minigame, MinigameActions.Aim);
            m_Action = InputMaps.Find(InputMaps.Minigame, MinigameActions.Action);
            m_MinigameCancel = InputMaps.Find(InputMaps.Minigame, MinigameActions.Cancel);
            m_Point = InputMaps.Find(InputMaps.Minigame, MinigameActions.Point);
            if (m_Plate != null) m_Plate.enabled = false;
        }

        void Update()
        {
            if (m_Director != null && m_Director.Phase == TavernPhase.Morning && m_BreakfastRecipe != null)
            {
                TickBreakfast(Time.deltaTime);
                return;
            }
            if (m_Director == null || m_Director.Phase != TavernPhase.Service || m_Director.Session == null) return;
            float dt = Time.deltaTime;

            if (ActiveCook != null)
            {
                TickCook(dt);
                return;
            }
            if (Carrying != null)
            {
                TickServing(dt);
                return;
            }

            float move = m_Move != null ? m_Move.ReadValue<Vector2>().x : 0f;
            MoveTo(X + move * m_Director.PlayerSettings.walkSpeed * dt);
            if (m_Body != null && Mathf.Abs(move) > 0.1f) m_Body.flipX = move < 0f;

            UpdateNearStation();
            if (m_Interact != null && m_Interact.WasPressedThisFrame()) Interact();
        }

        void UpdateNearStation()
        {
            var layout = m_Director.Layout;
            Station best = null;
            float bestDistance = m_Director.PlayerSettings.interactRange;
            foreach (var s in new[] { layout.Grill, layout.Tap, layout.Pass, layout.StewPot })
            {
                if (s == null) continue;
                float d = Mathf.Abs(s.X - X);
                if (d <= bestDistance)
                {
                    best = s;
                    bestDistance = d;
                }
            }
            SetNear(best);

            var session = m_Director.Session;
            HintRecipe = null;
            if (m_Near == null) Hint = PlayerHint.None;
            else if (m_Near.Kind == StationKind.Pass)
            {
                var ready = session.NextToServe();
                Hint = ready != null ? PlayerHint.PickUp : PlayerHint.Idle;
                HintRecipe = ready?.Recipe;
            }
            else if (m_Director.Staff != null && m_Director.Staff.Works(m_Near.Kind))
            {
                Hint = PlayerHint.Staffed;
            }
            else if (m_Near.Kind == StationKind.StewPot)
            {
                var pot = session.Pot;
                switch (pot.State)
                {
                    case PotState.Empty:
                        HintRecipe = session.NextBatch();
                        Hint = HintRecipe != null ? PlayerHint.StartStew : PlayerHint.Idle;
                        break;
                    case PotState.Simmering:
                        Hint = PlayerHint.Simmering;
                        HintRecipe = pot.Recipe;
                        break;
                    case PotState.Ready:
                        Hint = PlayerHint.StewReady;
                        HintRecipe = pot.Recipe;
                        break;
                    default:
                        Hint = PlayerHint.Idle;
                        break;
                }
            }
            else
            {
                var next = session.NextToCook(ToCookStation(m_Near.Kind));
                Hint = next != null ? PlayerHint.Cook : PlayerHint.Idle;
                HintRecipe = next?.Recipe;
            }
        }

        void SetNear(Station station)
        {
            if (m_Near == station) return;
            if (m_Near != null) m_Near.SetHighlighted(false);
            if (station != null) station.SetHighlighted(true);
            m_Near = station;
        }

        /// <summary>The customer Interact would serve right now; they get the floor highlight.</summary>
        public CustomerAgent ServeTarget => m_ServeTarget;

        void SetServeTarget(CustomerAgent agent)
        {
            if (m_ServeTarget == agent) return;
            if (m_ServeTarget != null) m_ServeTarget.SetHighlighted(false);
            if (agent != null) agent.SetHighlighted(true);
            m_ServeTarget = agent;
        }

        void Interact()
        {
            var session = m_Director.Session;
            if (Hint == PlayerHint.Cook)
            {
                var station = ToCookStation(m_Near.Kind);
                var ticket = session.NextToCook(station);
                if (!session.StartCooking(ticket, this)) return;
                CookTicket = ticket;
                ActiveCook = m_Director.Minigames.CreateCook(station);
                ActiveCook.Begin();
                InputMaps.Activate(InputMaps.Minigame);
            }
            else if (Hint == PlayerHint.StartStew)
            {
                if (!session.StartBatch(session.NextBatch(), this)) return;
                m_ChoppingPot = true;
                ActiveCook = m_Director.Minigames.CreateChop(session.Pot.ChopItems.Count, m_Random);
                ActiveCook.Begin();
                m_LastPointer = ReadPointer();
                InputMaps.Activate(InputMaps.Minigame);
            }
            else if (Hint == PlayerHint.PickUp)
            {
                var ticket = session.NextToServe();
                if (!session.StartDelivery(ticket, this)) return;
                CarryTicket = ticket;
                Carrying = m_Director.Minigames.CreateServing(X);
                Carrying.Begin();
                SetNear(null);
                if (m_Plate != null)
                {
                    m_Plate.enabled = true;
                    m_Plate.color = ticket.Recipe.placeholderColor;
                }
            }
        }

        void TickCook(float dt)
        {
            var session = m_Director.Session;
            bool stillMine = m_ChoppingPot
                ? session.Pot.State == PotState.Chopping && ReferenceEquals(session.Pot.ClaimedBy, this)
                : CookTicket.State == TicketState.Cooking;
            if (!stillMine)
            {
                EndCook();
                return;
            }
            if (m_MinigameCancel != null && m_MinigameCancel.WasPressedThisFrame())
            {
                if (m_ChoppingPot) session.AbandonBatch(this);
                else session.AbandonCooking(CookTicket);
                EndCook();
                return;
            }

            ActiveCook.Tick(dt, ReadMinigameInput());
            if (!ActiveCook.IsComplete) return;
            if (m_ChoppingPot) session.FinishChopping(this, ActiveCook.Evaluate());
            else session.FinishCooking(CookTicket, ActiveCook.Evaluate());
            EndCook();
        }

        MinigameInput ReadMinigameInput()
        {
            var pointer = ReadPointer();
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

        // ---------- Breakfast (Morning) ----------

        /// <summary>
        /// Takes one serving's ingredients and starts the dish's station minigame (the minigame
        /// panel draws it). Finishing eats it for the delve buff; Cancel puts the ingredients back.
        /// </summary>
        public bool CookBreakfast(RecipeDefinition recipe)
        {
            if (ActiveCook != null || recipe == null || recipe.station == CookStation.StewPot) return false;
            var used = RecipeMatcher.TryTake(recipe, m_Director.Storeroom);
            if (used == null) return false;
            m_BreakfastRecipe = recipe;
            m_BreakfastIngredients = used;
            ActiveCook = m_Director.Minigames.CreateCook(recipe.station);
            ActiveCook.Begin();
            m_LastPointer = ReadPointer();
            InputMaps.Activate(InputMaps.Minigame);
            return true;
        }

        void TickBreakfast(float dt)
        {
            if (m_MinigameCancel != null && m_MinigameCancel.WasPressedThisFrame())
            {
                m_Director.Storeroom.AddRange(m_BreakfastIngredients.Used);
                EndBreakfast();
                return;
            }
            ActiveCook.Tick(dt, ReadMinigameInput());
            if (!ActiveCook.IsComplete) return;
            var recipe = m_BreakfastRecipe;
            var used = m_BreakfastIngredients;
            float score = ActiveCook.Evaluate();
            EndBreakfast();
            m_Director.EatBreakfast(recipe, used, score);
        }

        void EndBreakfast()
        {
            m_BreakfastRecipe = null;
            m_BreakfastIngredients = null;
            ActiveCook = null;
            InputMaps.ActivateUIOnly();
        }

        Vector2 ReadPointer() => m_Point != null ? m_Point.ReadValue<Vector2>() : Vector2.zero;

        /// <summary>Screen x (pixels) to 0–1 across the chopping board, which the UI draws at the same screen span.</summary>
        float BoardFraction(float screenX)
        {
            var chop = m_Director.Minigames.Chop;
            float width = Mathf.Max(1f, Screen.width);
            return (screenX / width - chop.boardLeft) / Mathf.Max(0.01f, chop.boardWidth);
        }

        void TickServing(float dt)
        {
            var session = m_Director.Session;
            if (CarryTicket.State != TicketState.Delivering)
            {
                EndCarry();
                return;
            }

            float move = m_Move != null ? m_Move.ReadValue<Vector2>().x : 0f;
            Carrying.Tick(dt, new MinigameInput { Move = move });
            MoveTo(Carrying.Position);
            if (m_Body != null && Mathf.Abs(move) > 0.1f) m_Body.flipX = move < 0f;

            if (BumpedIntoSomeone(X, m_Director)) Carrying.RegisterBump();

            if (Carrying.Dropped)
            {
                session.Dropped(CarryTicket);
                EndCarry();
                return;
            }

            UpdateCarryHint();
            if (m_Interact == null || !m_Interact.WasPressedThisFrame()) return;
            if (Hint == PlayerHint.Serve)
            {
                Carrying.Deliver(m_ServeTarget.X);
                session.Deliver(CarryTicket, m_ServeTarget.Logic, Carrying.Evaluate());
                EndCarry();
            }
            else if (Hint == PlayerHint.PutBack)
            {
                session.PutBack(CarryTicket);
                EndCarry();
            }
        }

        /// <summary>
        /// While carrying: the nearest seated customer in reach who ordered this dish can be
        /// served; otherwise the pass takes the plate back; otherwise say what a nearby customer wants.
        /// </summary>
        void UpdateCarryHint()
        {
            var session = m_Director.Session;
            float range = m_Director.PlayerSettings.interactRange;
            CustomerAgent serve = null, other = null;
            float serveDistance = range, otherDistance = range;
            foreach (var agent in m_Director.Agents)
            {
                if (agent == null || agent.IsWalking || agent.Logic.State != CustomerState.WaitingForFood) continue;
                float d = Mathf.Abs(agent.X - X);
                if (session.CanDeliver(CarryTicket, agent.Logic))
                {
                    if (d <= serveDistance)
                    {
                        serve = agent;
                        serveDistance = d;
                    }
                }
                else if (d <= otherDistance)
                {
                    other = agent;
                    otherDistance = d;
                }
            }

            SetServeTarget(serve);
            var pass = m_Director.Layout.Pass;
            if (serve != null)
            {
                Hint = PlayerHint.Serve;
                HintRecipe = CarryTicket.Recipe;
            }
            else if (pass != null && Mathf.Abs(pass.X - X) <= range)
            {
                Hint = PlayerHint.PutBack;
                HintRecipe = CarryTicket.Recipe;
            }
            else if (other != null)
            {
                Hint = PlayerHint.WrongDish;
                HintRecipe = other.Logic.Order;
            }
            else
            {
                Hint = PlayerHint.None;
                HintRecipe = null;
            }
        }

        /// <summary>A walking customer overlaps this x position.</summary>
        public static bool BumpedIntoSomeone(float x, TavernDirector director)
        {
            float distance = director.PlayerSettings.bumpDistance;
            foreach (var agent in director.Agents)
                if (agent != null && agent.IsWalking && Mathf.Abs(agent.X - x) < distance) return true;
            return false;
        }

        void MoveTo(float x)
        {
            var layout = m_Director.Layout;
            var p = transform.position;
            p.x = Mathf.Clamp(x, layout.MinX, layout.MaxX);
            transform.position = p;
        }

        /// <summary>Service ended: put everything down.</summary>
        public void StopWork()
        {
            if (m_BreakfastRecipe != null)
            {
                m_Director.Storeroom.AddRange(m_BreakfastIngredients.Used);
                EndBreakfast();
            }
            if (ActiveCook != null) EndCook();
            if (Carrying != null) EndCarry();
            if (m_Near != null) m_Near.SetHighlighted(false);
            Hint = PlayerHint.None;
        }

        void EndCook()
        {
            ActiveCook = null;
            CookTicket = null;
            m_ChoppingPot = false;
            if (m_Director.Phase == TavernPhase.Service) InputMaps.Activate(InputMaps.Tavern);
        }

        void EndCarry()
        {
            Carrying = null;
            CarryTicket = null;
            SetServeTarget(null);
            Hint = PlayerHint.None;
            HintRecipe = null;
            if (m_Plate != null) m_Plate.enabled = false;
        }

        public static CookStation ToCookStation(StationKind kind) => kind switch
        {
            StationKind.Tap => CookStation.Tap,
            StationKind.StewPot => CookStation.StewPot,
            _ => CookStation.Grill,
        };
    }
}
