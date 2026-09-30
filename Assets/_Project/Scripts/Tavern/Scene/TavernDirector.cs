using System;
using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Economy;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hearthdelve.Tavern.Scene
{
    public enum TavernPhase
    {
        /// <summary>Day loop only: check stock, eat breakfast, set off for the dungeon.</summary>
        Morning,
        Prep,
        Service,
        Results,
        /// <summary>Day loop only: upgrades and the day summary, then sleep.</summary>
        Night,
    }

    /// <summary>
    /// Runs the tavern's parts of the day. In the day loop (a <see cref="GameFlow"/> is running)
    /// that's Morning (breakfast) or Evening Prep → Service → Results → Night (upgrades), using
    /// the persistent storeroom. Played on its own, the scene runs one evening with its own
    /// storeroom, as in Phase 2. Owns the <see cref="ServiceSession"/>; spawns customers. UI
    /// reads this directly (CLAUDE.md).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class TavernDirector : MonoBehaviour
    {
        [SerializeField] TavernContent m_Content;
        [SerializeField] TavernLayout m_Layout;
        [SerializeField] CustomerAgent m_CustomerPrefab;
        [SerializeField] TavernPlayer m_Player;
        [SerializeField] StaffAgent m_Staff;
        [SerializeField, Tooltip("0 = random each evening.")] int m_Seed;

        readonly List<RecipeDefinition> m_Menu = new();
        readonly List<CustomerAgent> m_Agents = new();
        IRandom m_Random;
        ArrivalSchedule m_Arrivals;
        GameFlow m_Flow;

        public static TavernDirector Instance { get; private set; }

        public TavernContent Content => m_Content;
        public TavernLayout Layout => m_Layout;
        public TavernPlayer Player => m_Player;
        public StaffAgent Staff => m_Staff;
        public TavernPhase Phase { get; private set; } = TavernPhase.Prep;
        public Storeroom Storeroom { get; private set; }
        /// <summary>The running day loop, or null when the scene is played on its own.</summary>
        public GameFlow Flow => m_Flow;
        public int Day => m_Flow != null ? m_Flow.State.Day : 1;
        /// <summary>Seats in use tonight: the base seats plus seat upgrades, up to what the room holds.</summary>
        public int ActiveSeats { get; private set; }
        /// <summary>The debug fill works standalone, and in the day loop only when enabled in the GameDatabase.</summary>
        public bool CanDebugFill => m_Flow == null || m_Flow.AllowDebugFill;
        public ServiceSession Session { get; private set; }
        public MinigameFactory Minigames { get; private set; }
        public IReadOnlyList<RecipeDefinition> SelectedMenu => m_Menu;
        public IReadOnlyList<CustomerAgent> Agents => m_Agents;
        public StaffStation StaffAssignment { get; private set; } = StaffStation.None;
        public StaffDefinition StaffMember => m_Content != null && m_Content.staff.Count > 0 ? m_Content.staff[0] : null;
        public int MaxMenuSize => m_Content.service.service.maxMenuSize;
        /// <summary>A menu is set and the stock can make at least one dish on it (otherwise service would close at once, sold out).</summary>
        public bool CanOpen => Phase == TavernPhase.Prep && m_Menu.Exists(r => RecipeMatcher.CanCook(r, Storeroom));
        public bool HasEatenBreakfast => m_Flow != null && m_Flow.State.Meal.IsActive;
        /// <summary>Quality of this morning's breakfast (for display), or -1.</summary>
        public float BreakfastQuality { get; private set; } = -1f;
        public TavernPlayerSettings PlayerSettings => m_Content.service.player;

        public event Action PhaseChanged;
        public event Action PrepChanged;

        public void Configure(TavernContent content, TavernLayout layout, CustomerAgent customerPrefab, TavernPlayer player, StaffAgent staff)
        {
            m_Content = content;
            m_Layout = layout;
            m_CustomerPrefab = customerPrefab;
            m_Player = player;
            m_Staff = staff;
        }

        void Awake()
        {
            Instance = this;
            m_Flow = GameFlow.Instance != null && GameFlow.Instance.InGame ? GameFlow.Instance : null;
            Storeroom = m_Flow != null ? m_Flow.State.Storeroom : new Storeroom();
            int extraSeats = m_Flow != null && m_Flow.Database != null ? m_Flow.Database.Effects(m_Flow.State).Seats : 0;
            ActiveSeats = Mathf.Clamp(m_Content.baseSeats + extraSeats, 1, Mathf.Max(1, m_Layout.SeatCount));
            m_Layout.SetActiveSeats(ActiveSeats);
            if (m_Flow != null)
                Phase = m_Flow.Phase switch
                {
                    DayPhase.Morning => TavernPhase.Morning,
                    DayPhase.Night => TavernPhase.Night,
                    _ => TavernPhase.Prep,
                };
            m_Random = m_Seed != 0 ? new SeededRandom(m_Seed) : new SeededRandom();
            Minigames = new MinigameFactory(m_Content.grill.grill, m_Content.tap.tap, m_Content.serving.serving,
                m_Content.stew != null ? m_Content.stew.chop : ChopSettings.Default);
            // Pip starts the evening carrying plates (playtest: most useful there); changeable at prep.
            StaffAssignment = StaffMember != null ? StaffStation.Serving : StaffStation.None;
            Storeroom.Changed += OnStoreroomChanged;
        }

        void Start() => InputMaps.ActivateUIOnly();

        void OnEnable() => EventBus<DebugSkipPhaseRequested>.Subscribe(OnDebugSkip);
        void OnDisable() => EventBus<DebugSkipPhaseRequested>.Unsubscribe(OnDebugSkip);

        void OnDestroy()
        {
            // The day loop's storeroom outlives this scene.
            if (Storeroom != null) Storeroom.Changed -= OnStoreroomChanged;
            if (Instance == this) Instance = null;
        }

        void OnStoreroomChanged() => PrepChanged?.Invoke();

        // ---------- Morning (day loop) ----------

        /// <summary>Dishes that give a breakfast buff (Grill and Tap dishes; stews aren't offered).</summary>
        public IEnumerable<RecipeDefinition> BreakfastOptions()
        {
            foreach (var r in m_Content.recipes)
                if (r != null && r.station != CookStation.StewPot && r.mealBuff.kind != MealBuffKind.None) yield return r;
        }

        public bool CanCookBreakfast(RecipeDefinition recipe) =>
            Phase == TavernPhase.Morning && !HasEatenBreakfast && m_Player != null && m_Player.ActiveCook == null &&
            recipe != null && recipe.station != CookStation.StewPot && recipe.mealBuff.kind != MealBuffKind.None && RecipeMatcher.CanCook(recipe, Storeroom);

        /// <summary>Cooks one dish for breakfast through its station's minigame (drawn by the minigame panel).</summary>
        public bool CookBreakfast(RecipeDefinition recipe) => CanCookBreakfast(recipe) && m_Player.CookBreakfast(recipe);

        /// <summary>The player finished cooking breakfast: eating it sets the buff for today's delve, scaled by the dish's quality.</summary>
        public void EatBreakfast(RecipeDefinition recipe, CookedIngredients used, float cookScore)
        {
            var scoring = m_Content.economy.dishScoring;
            float quality = DishScoring.DishQuality(used.Used, DishScoring.MinigameScore(cookScore, 1f, scoring), scoring);
            BreakfastQuality = quality;
            m_Flow?.EatMeal(MealBuff.FromDish(recipe, quality));
            PrepChanged?.Invoke();
        }

        /// <summary>Set off for the dungeon.</summary>
        public void Descend()
        {
            if (Phase != TavernPhase.Morning || m_Flow == null || m_Flow.IsLoading) return;
            if (m_Player != null) m_Player.StopWork();
            m_Flow.StartDelve();
        }

        // ---------- Night (day loop) ----------

        /// <summary>Close without opening the doors (nothing to cook, or by choice).</summary>
        public void CloseForTheNight()
        {
            if (Phase != TavernPhase.Prep || m_Flow == null) return;
            m_Flow.SkipService();
            SetPhase(TavernPhase.Night);
        }

        /// <summary>Results → Night: bank the takings. Standalone: prepare another evening.</summary>
        public void FinishEvening()
        {
            if (Phase != TavernPhase.Results) return;
            if (m_Flow == null)
            {
                Restart();
                return;
            }
            var l = Session.Ledger;
            m_Flow.CompleteService(new ServiceReport(l.DishesServed, l.Gold, l.Tips, l.Renown, l.Walkouts));
            SetPhase(TavernPhase.Night);
        }

        public void Sleep()
        {
            if (Phase != TavernPhase.Night || m_Flow == null || m_Flow.IsLoading) return;
            m_Flow.Sleep();
        }

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

        /// <summary>Debug: stock the storeroom with Phase 1 ingredients at mixed quality and freshness (see <see cref="CanDebugFill"/> for the UI/F4 gate).</summary>
        public void FillStoreroom() => DebugStockFiller.Fill(Storeroom, m_Content.debugStockIngredients, m_Random);

        public int ServingsAvailable(RecipeDefinition recipe) => RecipeMatcher.ServingsAvailable(recipe, Storeroom);

        /// <summary>Adds or removes a dish from tonight's menu. Returns whether it's on the menu afterwards.</summary>
        public bool ToggleMenu(RecipeDefinition recipe)
        {
            if (Phase != TavernPhase.Prep || recipe == null) return m_Menu.Contains(recipe);
            if (!m_Menu.Remove(recipe) && m_Menu.Count < MaxMenuSize) m_Menu.Add(recipe);
            PrepChanged?.Invoke();
            return m_Menu.Contains(recipe);
        }

        public void AssignStaff(StaffStation station)
        {
            if (Phase != TavernPhase.Prep) return;
            StaffAssignment = StaffMember != null ? station : StaffStation.None;
            PrepChanged?.Invoke();
        }

        // ---------- Service ----------

        public void OpenService()
        {
            if (!CanOpen) return;
            var economy = m_Content.economy;
            Session = new ServiceSession(m_Content.service.service, economy.dishScoring, economy.service,
                Storeroom, m_Menu, ActiveSeats, m_Random, m_Content.stew != null ? m_Content.stew.pot : StewPotSettings.Default);
            Session.Ended += OnServiceEnded;
            m_Arrivals = new ArrivalSchedule(m_Content.service.service, m_Content.customers, m_Random);
            if (m_Staff != null) m_Staff.Begin(StaffAssignment, StaffMember, this, m_Random);
            SetPhase(TavernPhase.Service);
            InputMaps.Activate(InputMaps.Tavern);
        }

        /// <summary>Debug / tests: close up now.</summary>
        public void EndServiceNow() => Session?.End();

        void Update()
        {
            if (Phase != TavernPhase.Service || Session == null) return;
            float dt = Time.deltaTime;
            Session.Tick(dt);
            if (Session.IsOver) return;
            var profile = m_Arrivals.Tick(dt, Session.CanAdmitCustomer);
            if (profile != null) SpawnCustomer(profile);
        }

        public CustomerAgent SpawnCustomer(CustomerProfile profile)
        {
            if (Session == null || profile == null) return null;
            var position = new Vector3(m_Layout.ExitX, m_Layout.FloorY, 0f);
            var agent = Instantiate(m_CustomerPrefab, position, Quaternion.identity);
            var logic = new CustomerLogic(profile);
            m_Agents.Add(agent);
            agent.Initialize(logic, this);
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

        /// <summary>Position in the seat queue (0 = front), or -1.</summary>
        public int QueuePlace(CustomerAgent agent)
        {
            int place = 0;
            foreach (var a in m_Agents)
            {
                if (a == agent) return place;
                if (a.Logic.State == CustomerState.Queueing) place++;
            }
            return -1;
        }

        void OnServiceEnded()
        {
            if (m_Player != null) m_Player.StopWork();
            if (m_Staff != null) m_Staff.StopWork();
            SetPhase(TavernPhase.Results);
            InputMaps.ActivateUIOnly();
        }

        /// <summary>Standalone only: reload the scene for another evening.</summary>
        public void Restart()
        {
            if (m_Flow == null) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void SetPhase(TavernPhase phase)
        {
            Phase = phase;
            PhaseChanged?.Invoke();
        }
    }
}
