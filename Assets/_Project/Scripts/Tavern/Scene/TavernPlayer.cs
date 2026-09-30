using Hearthdelve.Core.Input;
using Hearthdelve.Core.Minigames;
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
    }

    /// <summary>
    /// The keeper during service: walks the floor (Tavern map), cooks at the Grill/Tap (Minigame
    /// map), and carries plates from the pass (the Serving minigame drives movement while
    /// carrying). A plate is served with Interact next to anyone waiting for that dish, or put
    /// back with Interact at the pass.
    /// </summary>
    public sealed class TavernPlayer : MonoBehaviour
    {
        [SerializeField] SpriteRenderer m_Body;
        [SerializeField] SpriteRenderer m_Plate;

        TavernDirector m_Director;
        InputAction m_Move, m_Interact, m_Aim, m_Action, m_MinigameCancel;
        Station m_Near;
        CustomerAgent m_ServeTarget;

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
            if (m_Plate != null) m_Plate.enabled = false;
        }

        void Update()
        {
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
            foreach (var s in new[] { layout.Grill, layout.Tap, layout.Pass })
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
            if (CookTicket.State != TicketState.Cooking)
            {
                EndCook(); // the customer left mid-cook
                return;
            }
            if (m_MinigameCancel != null && m_MinigameCancel.WasPressedThisFrame())
            {
                session.AbandonCooking(CookTicket);
                EndCook();
                return;
            }

            var input = new MinigameInput
            {
                ActionPressed = m_Action != null && m_Action.WasPressedThisFrame(),
                ActionHeld = m_Action != null && m_Action.IsPressed(),
                ActionReleased = m_Action != null && m_Action.WasReleasedThisFrame(),
                Aim = m_Aim != null ? m_Aim.ReadValue<Vector2>() : Vector2.zero,
            };
            ActiveCook.Tick(dt, input);
            if (!ActiveCook.IsComplete) return;
            session.FinishCooking(CookTicket, ActiveCook.Evaluate());
            EndCook();
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

            m_ServeTarget = serve;
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
            if (ActiveCook != null) EndCook();
            if (Carrying != null) EndCarry();
            if (m_Near != null) m_Near.SetHighlighted(false);
            Hint = PlayerHint.None;
        }

        void EndCook()
        {
            ActiveCook = null;
            CookTicket = null;
            if (m_Director.Phase == TavernPhase.Service) InputMaps.Activate(InputMaps.Tavern);
        }

        void EndCarry()
        {
            Carrying = null;
            CarryTicket = null;
            m_ServeTarget = null;
            Hint = PlayerHint.None;
            HintRecipe = null;
            if (m_Plate != null) m_Plate.enabled = false;
        }

        public static CookStation ToCookStation(StationKind kind) => kind == StationKind.Tap ? CookStation.Tap : CookStation.Grill;
    }
}
