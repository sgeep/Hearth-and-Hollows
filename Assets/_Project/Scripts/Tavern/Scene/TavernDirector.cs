using System;
using System.Collections.Generic;
using Hearthdelve.Core.Input;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>The parts of an evening in the tavern.</summary>
    public enum TavernPhase
    {
        Prep,
        Service,
        Results,
    }

    /// <summary>
    /// Runs the evening in the tavern room: owns the <see cref="ServiceSession"/> (the ported, tested
    /// service rules: seats, the queue, orders, patience, walkouts), spawns customers by the arrival
    /// schedule, and starts Pip at their job. UI reads it directly (CLAUDE.md).
    /// </summary>
    /// <remarks>
    /// The evening runs Prep → Service → Results (<see cref="Phase"/>). Played on its own the scene starts
    /// at Prep with a debug-filled storeroom, and Results offers another evening; the day loop (4c step 5)
    /// brings the real storeroom and goes on to Night.
    /// </remarks>
    [DefaultExecutionOrder(-50)]
    public sealed class TavernDirector : MonoBehaviour
    {
        [SerializeField] TavernContent m_Content;
        [SerializeField] TavernLayout m_Layout;
        [SerializeField] CustomerAgent m_CustomerPrefab;
        [SerializeField] StaffAgent m_Staff;
        [SerializeField, Tooltip("0 = random each evening.")] int m_Seed;

        readonly List<RecipeDefinition> m_Menu = new();
        readonly List<CustomerAgent> m_Agents = new();
        IRandom m_Random;
        ArrivalSchedule m_Arrivals;
        int m_EveningSeed;

        public static TavernDirector Instance { get; private set; }

        public TavernContent Content => m_Content;
        public TavernLayout Layout => m_Layout;
        public StaffAgent Staff => m_Staff;
        public Storeroom Storeroom { get; private set; }
        public ServiceSession Session { get; private set; }
        public IReadOnlyList<CustomerAgent> Agents => m_Agents;
        public IReadOnlyList<RecipeDefinition> Menu => m_Menu;
        public int ActiveSeats { get; private set; }
        public StaffStation StaffAssignment { get; private set; } = StaffStation.None;
        public StaffDefinition StaffMember => m_Content != null && m_Content.staff.Count > 0 ? m_Content.staff[0] : null;
        public bool IsServing => Session != null && !Session.IsOver;
        /// <summary>Makes the station minigames (and serving) from the tavern's tuning.</summary>
        public MinigameFactory Minigames { get; private set; }
        /// <summary>Stops scheduled arrivals (tests, debugging); customers can still be let in with <see cref="SpawnCustomer"/>.</summary>
        public bool ArrivalsPaused { get; set; }

        public TavernPhase Phase { get; private set; } = TavernPhase.Prep;
        /// <summary>The evening's outcome, once it's over (Results).</summary>
        public EveningReport Report { get; private set; }
        public int MaxMenuSize => m_Content.service.service.maxMenuSize;
        /// <summary>A menu is set and the storeroom can make at least one dish on it.</summary>
        public bool CanOpen => Phase == TavernPhase.Prep && PrepRules.CanOpen(m_Menu, Storeroom);

        public event Action ServiceOpened;
        public event Action ServiceEnded;
        public event Action PhaseChanged;
        /// <summary>The storeroom, menu or staff job changed during Prep.</summary>
        public event Action PrepChanged;

        public void Configure(TavernContent content, TavernLayout layout, CustomerAgent customerPrefab, StaffAgent staff)
        {
            m_Content = content;
            m_Layout = layout;
            m_CustomerPrefab = customerPrefab;
            m_Staff = staff;
        }

        void Awake()
        {
            Instance = this;
            Storeroom = new Storeroom();
            ActiveSeats = Mathf.Clamp(m_Content.baseSeats, 1, Mathf.Max(1, m_Layout.SeatCount));
            m_Layout.SetActiveSeats(ActiveSeats);
            m_EveningSeed = m_Seed != 0 ? m_Seed : Environment.TickCount;
            m_Random = new SeededRandom(m_EveningSeed);
            Minigames = new MinigameFactory(m_Content.grill.grill, m_Content.tap.tap, m_Content.serving.serving,
                m_Content.stew != null ? m_Content.stew.chop : ChopSettings.Default);
            // Pip starts the evening carrying plates (the prototype's playtest: most useful there).
            StaffAssignment = StaffMember != null ? StaffStation.Serving : StaffStation.None;
        }

        void Start()
        {
            if (m_Staff != null) m_Staff.Begin(StaffAssignment, StaffMember, this, m_Random);
            // Played on its own: one evening from a debug-filled storeroom (F4 fills it again).
            FillStoreroom();
            SetPhase(TavernPhase.Prep);
        }

        // ---------- Prep ----------

        /// <summary>Debug (F4) and standalone: stocks the storeroom with test ingredients at mixed quality and freshness.</summary>
        public void FillStoreroom()
        {
            DebugStockFiller.Fill(Storeroom, m_Content.debugStockIngredients, m_Random);
            PrepChanged?.Invoke();
        }

        /// <summary>Adds a dish to tonight's menu or takes it off. Returns whether it's on the menu afterwards.</summary>
        public bool ToggleMenu(RecipeDefinition recipe)
        {
            if (Phase != TavernPhase.Prep) return m_Menu.Contains(recipe);
            bool on = PrepRules.Toggle(m_Menu, recipe, MaxMenuSize);
            PrepChanged?.Invoke();
            return on;
        }

        /// <summary>Close without opening the doors (nothing to cook, or by choice).</summary>
        public void CloseForTheNight()
        {
            if (Phase != TavernPhase.Prep) return;
            Report = new EveningReport(null, false, stayedShut: true);
            SetPhase(TavernPhase.Results);
        }

        /// <summary>Results: done with the evening. Played on its own, another evening begins (the day loop goes to Night).</summary>
        public void FinishEvening()
        {
            if (Phase != TavernPhase.Results) return;
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.name);
        }

        void SetPhase(TavernPhase phase)
        {
            Phase = phase;
            if (phase == TavernPhase.Service) InputMaps.Activate(InputMaps.Tavern);
            else InputMaps.ActivateUIOnly();
            PhaseChanged?.Invoke();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Tests: puts the first dishes the storeroom can make on the menu and opens at once.</summary>
        public void OpenDebugEvening()
        {
            m_Menu.Clear();
            foreach (RecipeDefinition recipe in m_Content.recipes)
                if (recipe != null && m_Menu.Count < m_Content.service.service.maxMenuSize && RecipeMatcher.CanCook(recipe, Storeroom))
                    m_Menu.Add(recipe);
            OpenService();
        }

        /// <summary>Sets tonight's menu (up to the menu size) while the doors are closed (the prep screen, step 4; tests).</summary>
        public void SetMenu(IEnumerable<RecipeDefinition> recipes)
        {
            if (IsServing) return;
            if (Phase == TavernPhase.Results) SetPhase(TavernPhase.Prep);
            m_Menu.Clear();
            foreach (RecipeDefinition recipe in recipes)
                if (recipe != null && !m_Menu.Contains(recipe) && m_Menu.Count < m_Content.service.service.maxMenuSize) m_Menu.Add(recipe);
            PrepChanged?.Invoke();
        }

        /// <summary>Puts Pip on a job (the prep screen, step 4; tests). They walk to its post and start at once.</summary>
        public void AssignStaff(StaffStation station)
        {
            StaffAssignment = StaffMember != null ? station : StaffStation.None;
            if (m_Staff != null) m_Staff.Begin(StaffAssignment, StaffMember, this, m_Random);
            PrepChanged?.Invoke();
        }

        public void OpenService()
        {
            if (IsServing || !PrepRules.CanOpen(m_Menu, Storeroom)) return;
            var economy = m_Content.economy;
            Session = new ServiceSession(m_Content.service.service, economy.dishScoring, economy.service, Storeroom, m_Menu, ActiveSeats, m_Random,
                m_Content.stew != null ? m_Content.stew.pot : StewPotSettings.Default);
            Session.Ended += OnServiceEnded;
            m_Arrivals = new ArrivalSchedule(m_Content.service.service, m_Content.customers, m_Random);
            Report = null;
            SetPhase(TavernPhase.Service);
            ServiceOpened?.Invoke();
        }

        /// <summary>Debug (F5) and tests: close up now. Everyone still inside goes home.</summary>
        public void EndServiceNow() => Session?.End();

        void Update()
        {
            if (!IsServing) return;
            float dt = Time.deltaTime;
            Session.Tick(dt);
            if (Session.IsOver) return;
            CustomerProfile profile = m_Arrivals.Tick(dt, Session.CanAdmitCustomer && !ArrivalsPaused);
            if (profile != null) SpawnCustomer(profile);
        }

        /// <summary>A customer walks in through the door (also debug F6). Null if the doors are closed.</summary>
        public CustomerAgent SpawnCustomer(CustomerProfile profile = null)
        {
            if (!IsServing) return null;
            profile ??= m_Arrivals.PickProfile();
            if (profile == null) return null;
            CustomerAgent agent = Instantiate(m_CustomerPrefab, m_Layout.Door, Quaternion.identity);
            var logic = new CustomerLogic(profile);
            m_Agents.Add(agent);
            // The look comes from the evening's seed and the customer's id: fixed for their visit, varied between them.
            agent.Initialize(logic, this, unchecked(m_EveningSeed * 31 + logic.Id));
            Session.AdmitCustomer(logic);
            return agent;
        }

        /// <summary>A customer walked out the door.</summary>
        public void CustomerGone(CustomerAgent agent)
        {
            m_Agents.Remove(agent);
            Session?.CustomerGone(agent.Logic);
            Destroy(agent.gameObject);
        }

        /// <summary>Position in the seat queue (0 = front), or -1 if not queueing.</summary>
        public int QueuePlace(CustomerAgent agent)
        {
            int place = 0;
            foreach (CustomerAgent a in m_Agents)
            {
                if (a == agent) return a.Logic.State == CustomerState.Queueing ? place : -1;
                if (a.Logic.State == CustomerState.Queueing) place++;
            }
            return -1;
        }

        void OnServiceEnded()
        {
            Report = new EveningReport(Session.Ledger, Session.ClosedEarly, stayedShut: false);
            SetPhase(TavernPhase.Results);
            ServiceEnded?.Invoke();
        }
    }
}
