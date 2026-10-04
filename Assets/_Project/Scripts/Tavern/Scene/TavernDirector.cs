using System;
using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Economy;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Progression;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>What's happening in the tavern: the morning before a delve, the evening's three parts, and the night.</summary>
    public enum TavernPhase
    {
        /// <summary>Day loop: the storeroom, breakfast, setting off.</summary>
        Morning,
        Prep,
        Service,
        Results,
        /// <summary>Day loop: the day's summary, upgrades, sleep.</summary>
        Night,
    }

    /// <summary>
    /// Runs the evening in the tavern room: owns the <see cref="ServiceSession"/> (the ported, tested
    /// service rules: seats, the queue, orders, patience, walkouts), spawns customers by the arrival
    /// schedule, and starts Pip at their job. UI reads it directly (CLAUDE.md).
    /// </summary>
    /// <remarks>
    /// The evening runs Prep → Service → Results (<see cref="Phase"/>). In the day loop (a <see cref="GameFlow"/>
    /// is running) the storeroom is the game's, the scene opens at the day's phase (Morning, the evening's Prep, or
    /// Night), Results banks the takings and moves on to Night, and Sleep starts the next day. Played on its own the
    /// scene starts at Prep with a debug-filled storeroom, and Results offers another evening.
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
        GameFlow m_Flow;

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
        /// <summary>The day loop this tavern belongs to, or null when the scene is played on its own.</summary>
        public GameFlow Flow => m_Flow;
        public bool InDayLoop => m_Flow != null;
        /// <summary>F4 may fill the storeroom: always on its own, in the day loop only if the database allows it.</summary>
        public bool CanDebugFill => Debug.isDebugBuild && (m_Flow == null || m_Flow.AllowDebugFill);
        /// <summary>This morning's breakfast has been eaten.</summary>
        public bool HasEatenBreakfast => m_Flow != null && m_Flow.State.Meal.IsActive;
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
            m_Flow = GameFlow.Instance != null && GameFlow.Instance.InGame ? GameFlow.Instance : null;
            Storeroom = m_Flow != null ? m_Flow.State.Storeroom : new Storeroom();
            Storeroom.Changed += OnStoreroomChanged;
            // Seat upgrades bring out more of the room's tables.
            int extraSeats = m_Flow != null && m_Flow.Database != null ? m_Flow.Database.Effects(m_Flow.State).Seats : 0;
            ActiveSeats = Mathf.Clamp(m_Content.baseSeats + extraSeats, 1, Mathf.Max(1, m_Layout.SeatCount));
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
            if (m_Flow == null)
            {
                // Played on its own: one evening from a debug-filled storeroom (F4 fills it again).
                FillStoreroom();
                SetPhase(TavernPhase.Prep);
                return;
            }
            SetPhase(m_Flow.Phase switch
            {
                DayPhase.Morning => TavernPhase.Morning,
                DayPhase.Night => TavernPhase.Night,
                _ => TavernPhase.Prep,
            });
        }

        void OnEnable() => EventBus<DebugSkipPhaseRequested>.Subscribe(OnDebugSkip);
        void OnDisable() => EventBus<DebugSkipPhaseRequested>.Unsubscribe(OnDebugSkip);

        void OnStoreroomChanged() => PrepChanged?.Invoke();

        // ---------- Morning (day loop) ----------

        /// <summary>Dishes that make a breakfast: Grill and Tap dishes with a buff (a stew is too slow for the morning).</summary>
        public IEnumerable<RecipeDefinition> BreakfastOptions()
        {
            foreach (RecipeDefinition recipe in m_Content.recipes)
                if (recipe != null && recipe.station != CookStation.StewPot && recipe.mealBuff.kind != MealBuffKind.None) yield return recipe;
        }

        public bool CanCookBreakfast(RecipeDefinition recipe) =>
            Phase == TavernPhase.Morning && !HasEatenBreakfast && KeeperWork.Instance != null && KeeperWork.Instance.ActiveCook == null &&
            recipe != null && recipe.station != CookStation.StewPot && recipe.mealBuff.kind != MealBuffKind.None && RecipeMatcher.CanCook(recipe, Storeroom);

        /// <summary>Cooks one serving for breakfast at its station (the station panel opens, as in service).</summary>
        public bool CookBreakfast(RecipeDefinition recipe) => CanCookBreakfast(recipe) && KeeperWork.Instance.CookBreakfast(recipe);

        /// <summary>Breakfast is cooked and eaten: its buff, scaled by how well it came out, waits for today's delve.</summary>
        public void EatBreakfast(RecipeDefinition recipe, CookedIngredients used, float cookScore)
        {
            DishScoringSettings scoring = m_Content.economy.dishScoring;
            float quality = DishScoring.DishQuality(used.Used, DishScoring.MinigameScore(cookScore, 1f, scoring), scoring);
            m_Flow?.EatMeal(MealBuff.FromDish(recipe, quality));
            PrepChanged?.Invoke();
        }

        /// <summary>Set off for the dungeon.</summary>
        public void Descend()
        {
            if (Phase != TavernPhase.Morning || m_Flow == null || m_Flow.IsLoading) return;
            KeeperWork.Instance?.StopWork();
            m_Flow.StartDelve();
        }

        // ---------- Night (day loop) ----------

        public bool BuyUpgrade(TavernUpgradeDefinition upgrade) => Phase == TavernPhase.Night && m_Flow != null && m_Flow.BuyUpgrade(upgrade);

        /// <summary>Sleep: overnight, the save, and the next morning.</summary>
        public void Sleep()
        {
            if (Phase != TavernPhase.Night || m_Flow == null || m_Flow.IsLoading) return;
            m_Flow.Sleep();
        }

        /// <summary>F8 in the day loop: finish whatever part of the day this is, the way the player would.</summary>
        void OnDebugSkip(DebugSkipPhaseRequested _)
        {
            switch (Phase)
            {
                case TavernPhase.Morning: Descend(); break;
                case TavernPhase.Prep: CloseForTheNight(); break;
                case TavernPhase.Service: EndServiceNow(); break;
                case TavernPhase.Results: FinishEvening(); break;
                case TavernPhase.Night: Sleep(); break;
            }
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

        /// <summary>
        /// Close without opening the doors (nothing to cook, or by choice). In the day loop it's straight on to
        /// Night (there's nothing to report; the night's summary says the doors stayed shut); on its own, Results says so.
        /// </summary>
        public void CloseForTheNight()
        {
            if (Phase != TavernPhase.Prep) return;
            Report = new EveningReport(null, false, stayedShut: true);
            if (m_Flow == null)
            {
                SetPhase(TavernPhase.Results);
                return;
            }
            m_Flow.SkipService();
            SetPhase(TavernPhase.Night);
        }

        /// <summary>
        /// Results: done with the evening. In the day loop the takings are banked (and saved) and it's Night;
        /// played on its own, another evening begins.
        /// </summary>
        public void FinishEvening()
        {
            if (Phase != TavernPhase.Results) return;
            if (m_Flow == null)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.name);
                return;
            }
            ServiceLedger ledger = Session != null ? Session.Ledger : new ServiceLedger();
            m_Flow.CompleteService(new ServiceReport(ledger.DishesServed, ledger.Gold, ledger.Tips, ledger.Renown, ledger.Walkouts));
            SetPhase(TavernPhase.Night);
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
            // The day loop's storeroom outlives this scene.
            if (Storeroom != null) Storeroom.Changed -= OnStoreroomChanged;
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
