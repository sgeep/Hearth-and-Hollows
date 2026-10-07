using System;
using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Core.Input;
using Hearthdelve.Core.Minigames;
using Hearthdelve.Core.Random;
using Hearthdelve.Shared.Economy;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Inventory;
using Hearthdelve.Shared.Progression;
using Hearthdelve.Shared.Recipes;
using Hearthdelve.Tavern.Customers;
using Hearthdelve.Tavern.Minigames;
using Hearthdelve.Tavern.Service;
using Hearthdelve.Tavern.Staff;
using Hearthdelve.Shared.Story;
using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>What's happening in the tavern: the daytime, the evening's three parts, and the night after the delve.</summary>
    public enum TavernPhase
    {
        /// <summary>
        /// Day loop: the free daytime (4h): the keeper wakes upstairs and walks Tally Ho! and Kariaston; the storeroom, the delve
        /// meal, decorating and the market are places, and the menu board begins the evening when the player chooses.
        /// </summary>
        Daytime,
        Prep,
        Service,
        Results,
        /// <summary>Day loop: the day's summary, upgrades, sleep.</summary>
        Night,
        /// <summary>
        /// The Act I opening's first day (4g Checkpoint B): the keeper has just arrived, walks the room, meets Orik and Boog, and goes
        /// down the cellar hatch to the first delve. In place of the daytime placeholder.
        /// </summary>
        Arrival,
    }

    /// <summary>
    /// Runs the evening in the tavern room: owns the <see cref="ServiceSession"/> (the ported, tested
    /// service rules: seats, the queue, orders, patience, walkouts), spawns customers by the arrival
    /// schedule, and starts Orik at their job. UI reads it directly (CLAUDE.md).
    /// </summary>
    /// <remarks>
    /// The evening runs Prep → Service → Results (<see cref="Phase"/>). In the day loop (a <see cref="GameFlow"/>
    /// is running) the storeroom is the game's, the scene opens at the day's phase (daytime, the evening's Prep, or
    /// Night after the delve), Results banks the takings and closes up for the night's delve, and Sleep starts the next day. Played on its own the
    /// scene starts at Prep with a debug-filled storeroom, and Results offers another evening.
    /// </remarks>
    [DefaultExecutionOrder(-50)]
    public sealed class TavernDirector : MonoBehaviour
    {
        [SerializeField] TavernContent m_Content;
        [SerializeField] TavernLayout m_Layout;
        [SerializeField] CustomerAgent m_CustomerPrefab;
        [SerializeField] StaffAgent m_Staff;
        [SerializeField, Tooltip("Gunta, the cook (4f Checkpoint C): a second member of staff with her own job.")]
        StaffAgent m_Cook;
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
        /// <summary>Gunta's agent (4f Checkpoint C), or null in a scene without her.</summary>
        public StaffAgent Cook => m_Cook;
        /// <summary>Gunta's job tonight: a cooking station, or none.</summary>
        public StaffStation CookAssignment { get; private set; } = StaffStation.None;
        /// <summary>Gunta Ashbelly, the cook (stable id <c>gunta</c>).</summary>
        public StaffDefinition CookMember => m_Content != null ? m_Content.staff.Find(s => s != null && s.id == StaffIds.Boog) : null;

        /// <summary>Everyone on staff tonight (Orik and Gunta).</summary>
        public IEnumerable<StaffAgent> StaffAgents
        {
            get
            {
                if (m_Staff != null) yield return m_Staff;
                if (m_Cook != null) yield return m_Cook;
            }
        }

        /// <summary>The member of staff working <paramref name="station"/> tonight, or null.</summary>
        public StaffAgent StaffAt(StaffStation station)
        {
            if (station == StaffStation.None) return null;
            foreach (StaffAgent agent in StaffAgents)
                if (agent.Member != null && agent.Assignment == station) return agent;
            return null;
        }

        public void ConfigureCook(StaffAgent cook) => m_Cook = cook;

        [SerializeField, Min(0f), Tooltip("Seconds Gunta works a part at the Butcher Block once she's there.")]
        float m_CookButcherSeconds = 2.5f;

        /// <summary>Is Gunta free to take a part to the Butcher Block (Prep, a block out, her not already at it)?</summary>
        public bool CookCanButcher => Phase == TavernPhase.Prep && m_Cook != null && CookMember != null && !m_Cook.HasTask
                                      && KeeperWork.Instance != null && KeeperWork.Instance.ButcherBlock != null;

        /// <summary>Gunta at the Butcher Block now (it works while she's there).</summary>
        public bool CookButchering => m_Cook != null && m_Cook.WorkingTask;

        /// <summary>
        /// Gunta breaks down a part (4f Checkpoint C): she walks to the block, works it, and the cuts come from her steady
        /// hand (the minigame auto-played at her skill, capped like all staff work). <paramref name="done"/> gets the cuts.
        /// </summary>
        public bool LetCookButcher(IngredientItem part, Action<IngredientStack> done = null)
        {
            if (!CookCanButcher || !part.IsValid || !part.Definition.Butcherable || Storeroom.CountMatching(i => i == part) <= 0) return false;
            StaffDefinition gunta = CookMember;
            TavernInteractable block = KeeperWork.Instance.ButcherBlock;
            return m_Cook.DoTask(block.UsePoint, m_CookButcherSeconds, () =>
            {
                ButcherMinigame game = Minigames.CreateButcher(part.Definition.butchering.maxCuts, m_Random);
                float score = Mathf.Min(MinigameRunner.RunToCompletion(game, MinigameFactory.CreateAutoPlayer(game, gunta.skill, m_Random)), gunta.qualityCap);
                IngredientStack cuts = ButcherRules.Butcher(Storeroom, part, score);
                if (!cuts.IsEmpty)
                {
                    EventBus<PartButchered>.Publish(new PartButchered(part.Definition.id, cuts.Item.Definition.id, cuts.Count, score, gunta.id));
                    m_Cook.Show(cuts.Count >= part.Definition.butchering.maxCuts ? m_Cook.Faces.content : m_Cook.Faces.happy);
                }
                done?.Invoke(cuts);
                PrepChanged?.Invoke();
            });
        }
        public Storeroom Storeroom { get; private set; }
        public ServiceSession Session { get; private set; }
        public IReadOnlyList<CustomerAgent> Agents => m_Agents;
        public IReadOnlyList<RecipeDefinition> Menu => m_Menu;
        /// <summary>Seats open tonight: the placed furniture's usable seats (4f, D16).</summary>
        public int ActiveSeats { get; private set; }
        public StaffStation StaffAssignment { get; private set; } = StaffStation.None;
        public StaffDefinition StaffMember => m_Content == null ? null
            : m_Content.staff.Find(s => s != null && s.id == StaffIds.Orik) ?? m_Content.staff.Find(s => s != null && s.id != StaffIds.Boog);
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
        /// <summary>Today's delve meal has been eaten (it waits for tonight's delve).</summary>
        public bool HasEatenDelveMeal => m_Flow != null && m_Flow.State.Meal.IsActive;
        /// <summary>The evening's outcome, once it's over (Results).</summary>
        public EveningReport Report { get; private set; }
        public int MaxMenuSize => m_Content.service.service.maxMenuSize;
        /// <summary>What the furniture's layout check says about service (D13: problems that make it impossible keep the doors shut).</summary>
        public Hearthdelve.Shared.Customization.LayoutReport Furnishing =>
            AreaFurniture.Tavern != null ? AreaFurniture.Tavern.Report : Hearthdelve.Shared.Customization.LayoutReport.Clear;

        /// <summary>A menu is set, the storeroom can make at least one dish on it, and the layout lets service run.</summary>
        public bool CanOpen => Phase == TavernPhase.Prep && PrepRules.CanOpen(m_Menu, Storeroom) && Furnishing.CanOpen;

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
            // 4f (D16): the seats are the placed furniture's usable seats (AreaFurniture builds them before this wakes).
            ActiveSeats = m_Layout.SeatCount;
            m_EveningSeed = m_Seed != 0 ? m_Seed : Environment.TickCount;
            m_Random = new SeededRandom(m_EveningSeed);
            Minigames = new MinigameFactory(m_Content.grill.grill, m_Content.tap.tap, m_Content.serving.serving,
                m_Content.stew != null ? m_Content.stew.chop : ChopSettings.Default,
                m_Content.butcher != null ? m_Content.butcher.butcher : ButcherSettings.Default);
            // Orik starts the evening carrying plates (the prototype's playtest: most useful there).
            StaffAssignment = StaffMember != null ? StaffStation.Serving : StaffStation.None;
            // Gunta starts off duty (4f Checkpoint C): the keeper cooks every station until choosing, at Prep, which one
            // she takes. (Tuning for the playtest: a default station would take the keeper's minigame from the first night.)
            CookAssignment = StaffStation.None;
        }

        void Start()
        {
            if (m_Staff != null) m_Staff.Begin(StaffAssignment, StaffMember, this, m_Random);
            if (m_Cook != null) m_Cook.Begin(CookAssignment, CookMember, this, m_Random);
            if (m_Flow == null)
            {
                // Played on its own: one evening from a debug-filled storeroom (F4 fills it again).
                FillStoreroom();
                SetPhase(TavernPhase.Prep);
                return;
            }
            SetPhase(m_Flow.Phase switch
            {
                DayPhase.Daytime when m_Flow.State.Story.Opening == OpeningStage.Arrival => TavernPhase.Arrival,
                DayPhase.Daytime => TavernPhase.Daytime,
                DayPhase.Night => TavernPhase.Night,
                _ => TavernPhase.Prep,
            });
            if (Phase == TavernPhase.Daytime) StartCoroutine(WakeUpstairs());
        }

        void OnEnable() => EventBus<DebugSkipPhaseRequested>.Subscribe(OnDebugSkip);
        void OnDisable() => EventBus<DebugSkipPhaseRequested>.Unsubscribe(OnDebugSkip);

        void OnStoreroomChanged() => PrepChanged?.Invoke();

        // ---------- Daytime (day loop) ----------

        /// <summary>Dishes that make a delve meal: Grill and Tap dishes with a buff (a stew is too slow for the morning).</summary>
        public IEnumerable<RecipeDefinition> DelveMealOptions()
        {
            foreach (RecipeDefinition recipe in m_Content.recipes)
                if (recipe != null && recipe.station != CookStation.StewPot && recipe.mealBuff.kind != MealBuffKind.None) yield return recipe;
        }

        public bool CanCookDelveMeal(RecipeDefinition recipe) =>
            Phase == TavernPhase.Daytime && !HasEatenDelveMeal && KeeperWork.Instance != null && KeeperWork.Instance.ActiveCook == null &&
            recipe != null && recipe.station != CookStation.StewPot && recipe.mealBuff.kind != MealBuffKind.None && RecipeMatcher.CanCook(recipe, Storeroom);

        /// <summary>Cooks one serving for delve meal at its station (the station panel opens, as in service).</summary>
        public bool CookDelveMeal(RecipeDefinition recipe) => CanCookDelveMeal(recipe) && KeeperWork.Instance.CookDelveMeal(recipe);

        /// <summary>The delve meal is cooked and eaten: its buff, scaled by how well it came out, waits for today's delve.</summary>
        public void EatDelveMeal(RecipeDefinition recipe, CookedIngredients used, float cookScore)
        {
            DishScoringSettings scoring = m_Content.economy.dishScoring;
            float quality = DishScoring.DishQuality(used.Used, DishScoring.MinigameScore(cookScore, 1f, scoring), scoring);
            m_Flow?.EatMeal(MealBuff.FromDish(recipe, quality));
            PrepChanged?.Invoke();
        }

        /// <summary>Arrival day (the opening): down the cellar hatch to the first delve.</summary>
        public void GoDownHatch()
        {
            if (Phase != TavernPhase.Arrival || m_Flow == null || m_Flow.IsLoading) return;
            if (StoryServices.Conversations != null && StoryServices.Conversations.IsTalking) return;
            m_Flow.BeginFirstDelve();
        }

        /// <summary>The player chose to begin the evening (the menu board, after confirming): Prep, and the village unloads.</summary>
        public void OpenForEvening()
        {
            if (Phase != TavernPhase.Daytime || m_Flow == null || m_Flow.IsLoading) return;
            KeeperWork.Instance?.StopWork();
            EventBus<EveningPrepChosen>.Publish(new EveningPrepChosen(m_Flow.State.Day, m_Flow.State.Surface.WholeMinute));
            m_Flow.StartEvening();
        }

        // ---------- On foot ----------

        /// <summary>The parts of the day the keeper walks Tally Ho! (4h: the free daytime too).</summary>
        public bool OnFoot => Phase is TavernPhase.Service or TavernPhase.Arrival or TavernPhase.Daytime;

        /// <summary>
        /// Gives the keeper back their feet after a panel, a station or Decorate Mode closes: the Tavern map while on foot,
        /// otherwise the UI alone (the evening's screens, the night).
        /// </summary>
        public static void RestoreInput()
        {
            TavernDirector director = Instance;
            bool busy = (KeeperWork.Instance != null && KeeperWork.Instance.ActiveCook != null) || (DecorateMode.Instance != null && DecorateMode.Instance.IsActive);
            if (director != null && director.OnFoot && !busy) InputMaps.Activate(InputMaps.Tavern);
            else if (!busy) InputMaps.ActivateUIOnly();
        }

        /// <summary>
        /// A new day begins upstairs (4h, H3): the keeper wakes beside the bed in the upstairs room (today's guest room) and the
        /// camera holds there. Waits a frame for the level to place the keeper first.
        /// </summary>
        System.Collections.IEnumerator WakeUpstairs()
        {
            yield return null;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            AreaFurniture upstairs = AreaFurniture.Find(PropertyArea.GuestRoomId);
            if (player == null || upstairs == null || !player.TryGetComponent(out Rigidbody2D body)) yield break;
            Vector2 at = WakePoint(upstairs);
            body.position = at;
            player.transform.position = new Vector3(at.x, at.y, player.transform.position.z);
            PropertyArea.Current = upstairs.Area;
            TavernView.Show(upstairs.Area);
            Woke?.Invoke();
        }

        /// <summary>Where the keeper wakes: just in front of the bed upstairs, wherever it has been moved; the room's door if there's no bed.</summary>
        public static Vector2 WakePoint(AreaFurniture upstairs)
        {
            PropertyArea area = upstairs.Area;
            foreach (Shared.Customization.ResolvedFurniture piece in upstairs.Pieces)
                if (piece?.Definition != null && piece.Definition.id.Contains("bed"))
                    return area.Origin + new Vector2(piece.Footprint.center.x, piece.Footprint.yMin - 0.6f);
            return area.Arrival;
        }

        /// <summary>The keeper woke upstairs (tests).</summary>
        public event Action Woke;

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
                case TavernPhase.Daytime: OpenForEvening(); break;
                case TavernPhase.Prep: CloseForTheNight(); break;
                case TavernPhase.Service: EndServiceNow(); break;
                case TavernPhase.Results: FinishEvening(); break;
                case TavernPhase.Night: Sleep(); break;
                case TavernPhase.Arrival: GoDownHatch(); break;
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
            // Kept shut: on to the night's delve (GameFlow loads the dungeon).
            if (m_Flow.IsLoading) return;
            m_Flow.SkipService();
        }

        /// <summary>
        /// Results: done with the evening. In the day loop the takings are banked (and saved), the tavern closes and the
        /// night's delve begins; played on its own, another evening begins.
        /// </summary>
        public void FinishEvening()
        {
            if (Phase != TavernPhase.Results) return;
            if (m_Flow == null)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(gameObject.scene.name);
                return;
            }
            if (m_Flow.IsLoading) return;
            ServiceLedger ledger = Session != null ? Session.Ledger : new ServiceLedger();
            m_Flow.CompleteService(new ServiceReport(ledger.DishesServed, ledger.Gold, ledger.Tips, ledger.Renown, ledger.Walkouts));
        }

        void SetPhase(TavernPhase phase)
        {
            Phase = phase;
            // On foot while serving, on arrival day (walking the room is the point) and in the free daytime (4h).
            if (OnFoot) InputMaps.Activate(InputMaps.Tavern);
            else InputMaps.ActivateUIOnly();
            PhaseChanged?.Invoke();
            // The fact (4g Checkpoint B): the story layer plays the opening's beats when they begin.
            EventBus<TavernPhaseStarted>.Publish(new TavernPhaseStarted(phase.ToString()));
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

        /// <summary>Puts Orik on a job (the prep screen, step 4; tests). They walk to its post and start at once.</summary>
        public void AssignStaff(StaffStation station)
        {
            StaffAssignment = StaffMember != null ? station : StaffStation.None;
            if (m_Staff != null) m_Staff.Begin(StaffAssignment, StaffMember, this, m_Random);
            // Two people can't work one station: Gunta steps away from it.
            if (StaffAssignment != StaffStation.None && CookAssignment == StaffAssignment)
            {
                CookAssignment = StaffStation.None;
                if (m_Cook != null) m_Cook.Begin(CookAssignment, CookMember, this, m_Random);
            }
            PrepChanged?.Invoke();
        }

        /// <summary>
        /// Puts Gunta on a job (the prep screen; 4f Checkpoint C): the Grill, the Tap, the Stew Pot, or none. She cooks, she
        /// doesn't carry plates. If Orik is at that station, Orik goes back to serving.
        /// </summary>
        public void AssignCook(StaffStation station)
        {
            if (station == StaffStation.Serving) station = StaffStation.None;
            CookAssignment = CookMember != null ? station : StaffStation.None;
            if (m_Cook != null) m_Cook.Begin(CookAssignment, CookMember, this, m_Random);
            if (CookAssignment != StaffStation.None && StaffAssignment == CookAssignment)
            {
                StaffAssignment = StaffStation.Serving;
                if (m_Staff != null) m_Staff.Begin(StaffAssignment, StaffMember, this, m_Random);
            }
            PrepChanged?.Invoke();
        }

        public void OpenService()
        {
            if (IsServing || !PrepRules.CanOpen(m_Menu, Storeroom) || !Furnishing.CanOpen) return;
            var economy = m_Content.economy;
            Session = new ServiceSession(m_Content.service.service, economy.dishScoring, economy.service, Storeroom, m_Menu, ActiveSeats, m_Random,
                m_Content.stew != null ? m_Content.stew.pot : StewPotSettings.Default);
            Session.Ended += OnServiceEnded;
            // Special requests (4f Checkpoint D), and the evening's facts for 4g.
            Session.ConfigureRequests(m_Content.service.requests);
            Session.Served += (c, t, gold, tip) => EventBus<DishServed>.Publish(new DishServed(t.Recipe.id, PatronId(c), t.DishQuality, gold, tip, c.IsRequest));
            Session.RequestIssued += (c, dish) => EventBus<CustomerRequestIssued>.Publish(new CustomerRequestIssued(PatronId(c), c.Id, dish.id));
            Session.RequestCompleted += (c, dish, quality, gold, renown) =>
                EventBus<CustomerRequestCompleted>.Publish(new CustomerRequestCompleted(PatronId(c), c.Id, dish.id, quality, gold, renown));
            Session.RequestFailed += (c, dish, outcome) =>
                EventBus<CustomerRequestFailed>.Publish(new CustomerRequestFailed(PatronId(c), c.Id, dish != null ? dish.id : null, RequestReason(outcome)));
            m_Arrivals = new ArrivalSchedule(m_Content.service.service, m_Content.customers, m_Random);
            Report = null;
            SetPhase(TavernPhase.Service);
            ServiceOpened?.Invoke();
        }

        static string PatronId(CustomerLogic c) => c.Profile != null ? c.Profile.id : "guest";

        static string RequestReason(RequestOutcome outcome) => outcome switch
        {
            RequestOutcome.WalkedOut => "walked_out",
            RequestOutcome.SoldOut => "sold_out",
            _ => "closing_time",
        };

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
            ServiceLedger l = Session.Ledger;
            EventBus<ServiceCompleted>.Publish(new ServiceCompleted(m_Flow != null && m_Flow.State != null ? m_Flow.State.Day : 0, l.DishesServed, l.Gold, l.Tips,
                l.Renown, l.Walkouts, l.RequestsCompleted, l.RequestsFailed));
            SetPhase(TavernPhase.Results);
            ServiceEnded?.Invoke();
        }
    }
}
