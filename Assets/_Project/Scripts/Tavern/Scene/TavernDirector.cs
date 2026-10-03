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
    /// <summary>
    /// Runs the evening in the tavern room: owns the <see cref="ServiceSession"/> (the ported, tested
    /// service rules: seats, the queue, orders, patience, walkouts), spawns customers by the arrival
    /// schedule, and starts Pip at their job. UI reads it directly (CLAUDE.md).
    /// </summary>
    /// <remarks>
    /// 4c step 2: played on its own the scene opens straight into a debug evening (a filled storeroom and
    /// the first dishes it can make). The prep screen (step 4) and the day loop (step 5) replace that.
    /// </remarks>
    [DefaultExecutionOrder(-50)]
    public sealed class TavernDirector : MonoBehaviour
    {
        [SerializeField] TavernContent m_Content;
        [SerializeField] TavernLayout m_Layout;
        [SerializeField] CustomerAgent m_CustomerPrefab;
        [SerializeField] StaffAgent m_Staff;
        [SerializeField, Tooltip("0 = random each evening.")] int m_Seed;
        [SerializeField, Tooltip("Open the doors at once with a debug-filled storeroom (until the prep screen exists).")]
        bool m_AutoOpen = true;

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

        public event Action ServiceOpened;
        public event Action ServiceEnded;

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
            if (m_AutoOpen) OpenDebugEvening();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Fills the storeroom with debug stock, puts the first dishes it can make on the menu, and opens.</summary>
        public void OpenDebugEvening()
        {
            DebugStockFiller.Fill(Storeroom, m_Content.debugStockIngredients, m_Random);
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
            m_Menu.Clear();
            foreach (RecipeDefinition recipe in recipes)
                if (recipe != null && !m_Menu.Contains(recipe) && m_Menu.Count < m_Content.service.service.maxMenuSize) m_Menu.Add(recipe);
        }

        /// <summary>Puts Pip on a job (the prep screen, step 4; tests). They walk to its post and start at once.</summary>
        public void AssignStaff(StaffStation station)
        {
            StaffAssignment = StaffMember != null ? station : StaffStation.None;
            if (m_Staff != null) m_Staff.Begin(StaffAssignment, StaffMember, this, m_Random);
        }

        public void OpenService()
        {
            if (m_Menu.Count == 0 || IsServing) return;
            var economy = m_Content.economy;
            Session = new ServiceSession(m_Content.service.service, economy.dishScoring, economy.service, Storeroom, m_Menu, ActiveSeats, m_Random,
                m_Content.stew != null ? m_Content.stew.pot : StewPotSettings.Default);
            Session.Ended += OnServiceEnded;
            m_Arrivals = new ArrivalSchedule(m_Content.service.service, m_Content.customers, m_Random);
            InputMaps.Activate(InputMaps.Tavern);
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

        void OnServiceEnded() => ServiceEnded?.Invoke();
    }
}
