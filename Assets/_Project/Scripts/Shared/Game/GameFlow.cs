using System;
using System.Collections;
using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Progression;
using Hearthdelve.Shared.Save;
using Hearthdelve.Shared.Story;
using Hearthdelve.Shared.Surface;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hearthdelve.Shared.Game
{
    /// <summary>Scene names the day loop moves between (all in the build list).</summary>
    public static class GameScenes
    {
        public const string Boot = "Boot";
        public const string MainMenu = "MainMenu";
        public const string Tavern = "Tavern";
        /// <summary>The night's delve: the generated Cellars run (4d). <c>Dungeon_TestFloor</c> stays a standalone test bed.</summary>
        public const string Dungeon = "Dungeon";
        /// <summary>The village (4h): loaded beside the tavern in the free daytime, so stepping outside never reloads anything.</summary>
        public const string Kariaston = "Kariaston";

        static readonly string[] k_Tavern = { Tavern };
        static readonly string[] k_Surface = { Tavern, Kariaston };
        static readonly string[] k_Dungeon = { Dungeon };

        /// <summary>
        /// The content scenes a part of the day needs (4h, H1): the free daytime is the tavern and the village together (the
        /// tavern first: it's the active scene); arrival day, the evening and the night are the tavern alone; the delve is
        /// the dungeon. Pure.
        /// </summary>
        public static string[] ContentFor(DayPhase phase, OpeningStage opening) => phase switch
        {
            DayPhase.Delve => k_Dungeon,
            DayPhase.Daytime when opening != OpeningStage.Arrival => k_Surface,
            _ => k_Tavern,
        };
    }

    /// <summary>
    /// Covers the screen while the content scene changes and reveals it again (a fade with the day and
    /// phase, in the UI). Optional: without one, scenes simply swap.
    /// </summary>
    public interface ISceneTransition
    {
        IEnumerator Cover();
        IEnumerator Reveal();
        /// <summary>Anything of the cover is still up (4g Checkpoint B: the opening's conversations wait for the scene to show).</summary>
        bool IsCovering { get; }
    }

    /// <summary>
    /// A boss was defeated for the first time (4e), published once its delve is recorded. The hook for first-clear rewards,
    /// story reactions and 4f's boss trophy (keyed by the boss's stable id).
    /// </summary>
    public readonly struct BossFirstCleared : IEvent
    {
        public readonly string BossId;
        public BossFirstCleared(string bossId) => BossId = bossId;
    }

    /// <summary>Debug: skip to the next phase. The loaded scene finishes its phase properly if it can.</summary>
    public readonly struct DebugSkipPhaseRequested : IEvent { }

    /// <summary>
    /// The persistent game service in the Boot scene (GDD §10.2–10.3 GameFlowManager). Holds the
    /// <see cref="GameState"/>, applies <see cref="DayRules"/>, saves, and swaps the content scene
    /// (main menu, tavern, dungeon), loaded additively so Boot stays alive. The tavern and the
    /// dungeon talk only to this, never to each other. When a content scene is played on its own
    /// there is no GameFlow, and scenes fall back to their standalone behaviour.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameFlow : MonoBehaviour
    {
        [SerializeField] GameDatabase m_Database;

        readonly List<string> m_Loaded = new();

        public static GameFlow Instance { get; private set; }
        /// <summary>Tests point saves at a temp folder.</summary>
        public static string SaveDirectoryOverride { get; set; }

        public GameDatabase Database => m_Database;
        public GameState State { get; private set; }
        public bool InGame => State != null;
        public DayPhase Phase => State?.Phase ?? DayPhase.Daytime;
        public DelveLoadout Loadout => State != null && m_Database != null ? m_Database.Loadout(State) : DelveLoadout.None;
        public SaveStore Store { get; private set; }
        public bool HasSave => Store != null && Store.Exists;
        /// <summary>The screen transition (the Boot scene's fade), if any.</summary>
        public ISceneTransition Transition { get; set; }
        public bool IsLoading { get; private set; }
        /// <summary>The main content scene (the active one): the tavern, the dungeon or the menu.</summary>
        public string LoadedScene => m_Loaded.Count > 0 ? m_Loaded[0] : null;
        /// <summary>Every content scene loaded now (4h: the tavern and the village in the daytime).</summary>
        public IReadOnlyList<string> LoadedScenes => m_Loaded;
        public bool IsLoaded(string scene) => m_Loaded.Contains(scene);
        public bool AllowDebugFill => m_Database != null && m_Database.allowDebugFill;
        /// <summary>Messages from the last load (dropped content) or save failure.</summary>
        public List<string> LastWarnings { get; } = new();

        /// <summary>Phase or day changed.</summary>
        public event Action PhaseChanged;
        /// <summary>Gold, upgrades, meal or storeroom changed.</summary>
        public event Action StateChanged;

        public void Configure(GameDatabase database) => m_Database = database;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Store = new SaveStore(SaveDirectoryOverride ?? Application.persistentDataPath);
        }

        void Start() => Load(GameScenes.MainMenu);

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------- Menu ----------

        /// <summary>
        /// The saved game, if there is one and it can be read: what Continue offers (and whether it's offered
        /// at all). Null for no save, or one that's unreadable or from a newer version.
        /// </summary>
        public SaveData PeekSave()
        {
            SaveCheck check = CheckSave();
            if (check.Problem is SaveProblem.Unreadable or SaveProblem.Newer) Debug.LogWarning($"[Hearthdelve] The save can't be read: {check.Detail}");
            return check.Usable ? check.Data : null;
        }

        /// <summary>The save, looked at (4i-A): usable, missing, unreadable or from a newer version.</summary>
        public SaveCheck CheckSave() => Store.CheckMain();

        /// <summary>The backup of the last good save before the current one (4i-A), looked at.</summary>
        public SaveCheck CheckBackup() => Store.CheckBackup();

        /// <summary>A save file is there, whether or not it reads (so New Game asks before replacing it).</summary>
        public bool AnySaveFile => Store.Exists;

        /// <summary>
        /// A new game (4g Checkpoint B), after character creation: the keeper made in the creator arrives at Tally Ho! on day 1
        /// (the Act I opening: Orik and Boog, then the hatch down to the first delve). Saved at once, so Continue never brings
        /// back a game the player started over from.
        /// </summary>
        public void NewGame(PlayerProfile keeper)
        {
            State = new GameState(1, DayPhase.Daytime) { Gold = m_Database != null ? m_Database.newGameGold : 0 };
            State.Furniture.GrantStarter(m_Database != null ? m_Database.startingFurniture : null);
            State.Story.Player = (keeper ?? new PlayerProfile()).Clone();
            State.Story.Player.name = Characters.KeeperRules.CleanName(State.Story.Player.name);
            State.Story.CreationComplete = true;
            State.Story.Opening = OpeningStage.Arrival;
            StartWorld();
            StartSurfaceDay();
            StoryServices.State?.Clear();
            Save();
            PhaseChanged?.Invoke();
            Load(ContentFor(State));
        }

        /// <summary>
        /// A new game without character creation or the opening (tests and debugging; the game before 4g Checkpoint B): Bram,
        /// day 1 with a delve into the Hollows (the storeroom starts empty), every onboarding prompt already seen.
        /// </summary>
        public void QuickNewGame()
        {
            State = new GameState(1, DayPhase.Delve) { Gold = m_Database != null ? m_Database.newGameGold : 0 };
            State.Furniture.GrantStarter(m_Database != null ? m_Database.startingFurniture : null);
            State.Story.Opening = OpeningStage.Complete;
            State.Story.CreationComplete = true;
            foreach (string hint in OnboardingHints.All) State.Story.SeenHints.Add(hint);
            StartWorld();
            StartSurfaceDay();
            StoryServices.State?.Clear();
            Save();
            PhaseChanged?.Invoke();
            Load(ContentFor(State));
        }

        /// <summary>The opening's arrival (4g Checkpoint B): down the hatch to the first delve. Saved.</summary>
        public bool BeginFirstDelve()
        {
            if (!InGame || IsLoading || State.Phase != DayPhase.Daytime || State.Story.Opening != OpeningStage.Arrival) return false;
            DayRules.StartOpeningDelve(State);
            PhaseChanged?.Invoke();
            Save();
            Load(GameScenes.Dungeon);
            return true;
        }

        /// <summary>The opening moves on (the story layer, as its beats finish). Never backwards; saved at the next safe point.</summary>
        public void AdvanceOpening(OpeningStage stage)
        {
            if (!InGame || stage <= State.Story.Opening) return;
            State.Story.Opening = stage;
            StateChanged?.Invoke();
        }

        /// <summary>An onboarding prompt was shown (saved with the day's next save).</summary>
        public void MarkHintSeen(string hint)
        {
            if (InGame && !string.IsNullOrEmpty(hint)) State.Story.SeenHints.Add(hint);
        }

        /// <summary>A quest now wants an object (giving its quest): it turns up in the Hollows until brought home.</summary>
        public bool WantQuestObject(string id)
        {
            if (!InGame || !State.QuestObjects.Want(id)) return false;
            StateChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// A quest object handed over (the conversation that ends its quest): its reward is given (Hearth &amp; Hollows code,
        /// never a quest action, so a reload can't give it twice), the fact is published, and the game saves.
        /// </summary>
        public bool DeliverQuestObject(string id)
        {
            if (!InGame || !State.QuestObjects.Deliver(id)) return false;
            Quests.QuestObjectDefinition definition = m_Database != null ? m_Database.QuestObject(id) : null;
            if (definition != null) State.AddGold(definition.rewardGold);
            EventBus<QuestObjectDelivered>.Publish(new QuestObjectDelivered(id));
            StateChanged?.Invoke();
            Save();
            return true;
        }

        /// <summary>
        /// Loads the save and resumes at its phase. Returns false if there's no readable save. <paramref name="fromBackup"/> (4i-A):
        /// the backup instead, when the save itself can't be read; the next save sets the unreadable one aside.
        /// </summary>
        public bool Continue(bool fromBackup = false)
        {
            LastWarnings.Clear();
            SaveData data = fromBackup ? (CheckBackup() is { Usable: true } backup ? backup.Data : null) : PeekSave();
            if (data == null) return false;
            try
            {
                State = SaveSystem.Restore(data, m_Database, LastWarnings);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                LastWarnings.Add(e.Message);
                return false;
            }
            foreach (var w in LastWarnings) Debug.LogWarning($"[Hearthdelve] Save: {w}");
            // The story's middleware gets the loaded game's state back before its scene loads (4g).
            StoryServices.State?.Clear();
            StoryServices.State?.Restore(State);
            // Version 10: a daytime resumes at the minute it was saved (Vigor and the garden came back with the state); anything
            // else starts its next surface day in the morning as before.
            if (State.Phase == DayPhase.Daytime && State.Surface.WholeMinute > 0) SurfaceTime.Instance?.Restart();
            else StartSurfaceDay();
            PhaseChanged?.Invoke();
            Load(ContentFor(State));
            return true;
        }

        /// <summary>
        /// Back to the main menu (4i-A, D2). <paramref name="save"/>: save first (the free day, the night, after banking); otherwise
        /// the last save stands, and Continue comes back to it. Every pause and hold of the session is let go.
        /// </summary>
        public void QuitToMenu(bool save = false)
        {
            if (save) Save();
            State = null;
            StoryServices.State?.Clear();
            Engine.MenuPause.Clear();
            Surface.SurfacePause.Clear();
            Load(GameScenes.MainMenu);
        }

        /// <summary>
        /// Quitting while the evening's results show (4i-A, <see cref="QuitKind.BankEvening"/>): the takings banked exactly as closing
        /// up banks them, and saved (phase Delve, so Continue is the night's delve); no scene loads.
        /// </summary>
        public void BankEvening(ServiceReport report)
        {
            if (!InGame || State.Phase != DayPhase.Evening) return;
            DayRules.CompleteService(State, report);
            PhaseChanged?.Invoke();
            Save();
        }

        // ---------- The day ----------

        /// <summary>
        /// The day is done and the tavern opens for the evening (a fresh tavern scene). The daytime placeholder calls this
        /// today; the free-roaming village day will call it when its day ends. Not saved: daytime has nothing to lose.
        /// </summary>
        public void StartEvening()
        {
            // 4h Checkpoint B: the day as the keeper leaves it (its minute, Vigor, the garden) is saved before the evening begins;
            // the evening itself isn't a resume point (quitting during it comes back to this moment of the day).
            if (State.Phase == DayPhase.Daytime) Save();
            DayRules.StartEvening(State);
            PhaseChanged?.Invoke();
            Load(ContentFor(State));
        }

        /// <summary>
        /// Service is over: bank the takings, close up, and head below for the night's delve. Saved here (phase Delve,
        /// before the run): quitting mid-run and continuing starts the night's delve again from the top, with nothing
        /// from the abandoned run (the run itself is never saved).
        /// </summary>
        public void CompleteService(ServiceReport report)
        {
            DayRules.CompleteService(State, report);
            PhaseChanged?.Invoke();
            Save();
            Load(GameScenes.Dungeon);
        }

        /// <summary>Kept shut tonight: straight on to the delve, saved as above.</summary>
        public void SkipService()
        {
            DayRules.SkipService(State);
            PhaseChanged?.Invoke();
            Save();
            Load(GameScenes.Dungeon);
        }

        /// <summary>
        /// Back from the delve: the haul and the run's Gold are applied once and saved at once (phase Night), so a reload
        /// can't apply them again or lose them; then home to the tavern for the night.
        /// </summary>
        public void CompleteDelve(DelveReport report) => CompleteDelve(report, goHome: true);

        /// <summary>As above; <paramref name="goHome"/> false when quitting from the delve's result (4i-A): applied and saved, no scene load.</summary>
        public void CompleteDelve(DelveReport report, bool goHome)
        {
            DayRules.CompleteDelve(State, report, m_Database != null ? m_Database.Furniture : null, m_Database != null ? m_Database.bossTrophies : null,
                m_Database != null ? m_Database.QuestObject : null);
            // Facts for later reactions (4g's dialogue adapters): what came home from the Hollows.
            foreach (string curio in report.CuriosKept) EventBus<CurioBroughtHome>.Publish(new CurioBroughtHome(curio));
            foreach (string id in report.QuestObjectsCarried)
            {
                if (DayRules.QuestObjectsHome.Contains(id)) EventBus<QuestObjectBroughtHome>.Publish(new QuestObjectBroughtHome(id));
                else EventBus<QuestObjectLost>.Publish(new QuestObjectLost(id));
            }
            // A boss's first defeat (4e): the hook for first-clear rewards, story reactions and 4f's trophy.
            foreach (string boss in report.BossesDefeated)
            {
                if (State.TimesDefeated(boss) == 1) EventBus<BossFirstCleared>.Publish(new BossFirstCleared(boss));
                EventBus<BossDefeated>.Publish(new BossDefeated(boss, State.TimesDefeated(boss), report.Outcome != DelveOutcome.Died));
            }
            PhaseChanged?.Invoke();
            Save();
            if (goHome) Load(GameScenes.Tavern);
        }

        public bool EatMeal(MealBuff meal)
        {
            if (!DayRules.EatMeal(State, meal)) return false;
            StateChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Buys one offer at the Kariaston market (daytime; 4f Checkpoint C, D19): gold out, the goods into the storeroom,
        /// and the game saves. False when it can't be afforded.
        /// </summary>
        public bool BuyFromMarket(Inventory.SupplyOffer offer)
        {
            if (!InGame || m_Database == null || m_Database.market == null) return false;
            if (!DayRules.Buy(State, m_Database.market, offer, SurfaceTime.CurrentSettings)) return false;
            EventBus<MarketPurchase>.Publish(new MarketPurchase(offer.ingredient.id, offer.bundle, offer.price));
            StateChanged?.Invoke();
            Save();
            return true;
        }

        // ---------- The garden (4h Checkpoint B) ----------

        public Surface.VigorSettings VigorSettings => m_Database != null ? m_Database.Vigor : Surface.VigorSettings.Default;

        /// <summary>Prepares and plants a bed (Vigor 2 for now): all or nothing; saved.</summary>
        public Garden.GardenResult PlantBed(string bedId, Garden.CropDefinition crop)
        {
            if (!InGame) return Garden.GardenResult.UnknownBed;
            Garden.GardenResult result = DayRules.PlantBed(State, bedId, crop, VigorSettings);
            if (result != Garden.GardenResult.Done) return result;
            Spent(Surface.VigorActivity.PlantBed);
            EventBus<CropPlanted>.Publish(new CropPlanted(bedId, crop.id));
            StateChanged?.Invoke();
            Save();
            return result;
        }

        /// <summary>Tends a growing bed (Vigor 1 for now; once per bed per day); saved.</summary>
        public Garden.GardenResult TendBed(string bedId)
        {
            if (!InGame) return Garden.GardenResult.UnknownBed;
            Garden.BedState bed = State.Garden.Bed(bedId);
            Garden.CropDefinition crop = bed != null && m_Database != null ? m_Database.Crop(bed.Crop) : null;
            Garden.GardenResult result = DayRules.TendBed(State, bedId, crop, VigorSettings);
            if (result != Garden.GardenResult.Done) return result;
            Spent(Surface.VigorActivity.TendBed);
            EventBus<CropTended>.Publish(new CropTended(bedId, crop.id, bed.TendedDays));
            StateChanged?.Invoke();
            Save();
            return result;
        }

        /// <summary>Harvests a ready bed into the storeroom (free for now); saved.</summary>
        public Garden.GardenResult HarvestBed(string bedId)
        {
            if (!InGame) return Garden.GardenResult.UnknownBed;
            Garden.BedState bed = State.Garden.Bed(bedId);
            Garden.CropDefinition crop = bed != null && m_Database != null ? m_Database.Crop(bed.Crop) : null;
            Garden.GardenResult result = DayRules.HarvestBed(State, bedId, crop, VigorSettings, out Inventory.IngredientStack produce);
            if (result != Garden.GardenResult.Done) return result;
            Spent(Surface.VigorActivity.HarvestBed);
            EventBus<CropHarvested>.Publish(new CropHarvested(bedId, crop.id, produce.Item.Definition.id, produce.Count, produce.Item.Quality));
            StateChanged?.Invoke();
            Save();
            return result;
        }

        void Spent(Surface.VigorActivity activity)
        {
            int cost = VigorSettings.Cost(activity);
            if (cost > 0) EventBus<VigorSpent>.Publish(new VigorSpent(activity, cost, State.Vigor.Current));
        }

        public bool BuyUpgrade(TavernUpgradeDefinition upgrade)
        {
            if (!DayRules.BuyUpgrade(State, upgrade)) return false;
            StateChanged?.Invoke();
            Save();
            return true;
        }

        /// <summary>Furniture bought, sold or restyled (4f): the screens refresh and the game saves (autosave after purchases).</summary>
        public void FurnitureChanged()
        {
            if (!InGame) return;
            StateChanged?.Invoke();
            Save();
        }

        /// <summary>Sleep: overnight freshness loss, the next day's daytime, autosave, fresh tavern scene.</summary>
        public void Sleep()
        {
            // In order: overnight freshness, the new day, the garden's growth (once), Vigor full; then the surface clock's morning.
            if (m_Database != null) DayRules.Sleep(State, m_Database.Freshness, m_Database.Crop, m_Database.GardenSettings);
            else DayRules.Sleep(State, default);
            StartSurfaceDay();
            PhaseChanged?.Invoke();
            Save();
            Load(ContentFor(State));
        }

        /// <summary>Debug: lets the loaded scene finish its phase; if nothing handles it, skips with nothing gained.</summary>
        public void DebugSkipPhase()
        {
            if (!InGame || IsLoading) return;
            if (EventBus<DebugSkipPhaseRequested>.HandlerCount > 0)
            {
                EventBus<DebugSkipPhaseRequested>.Publish(default);
                return;
            }
            switch (State.Phase)
            {
                case DayPhase.Daytime: StartEvening(); break;
                case DayPhase.Evening: SkipService(); break;
                case DayPhase.Delve: CompleteDelve(DelveReport.Empty); break;
                case DayPhase.Night: Sleep(); break;
            }
        }

        /// <summary>
        /// Debug (4g Checkpoint A, Shift+F1): the first boss trophy comes home again: owned, taken down into storage if it's up,
        /// and waiting for its homecoming in Decorate Mode, so hanging it publishes <see cref="TrophyDisplayed"/> again. Saved.
        /// Returns its id, or null when there's no trophy in the game.
        /// </summary>
        public string DebugTrophyHomecoming()
        {
            if (!InGame || m_Database == null || m_Database.bossTrophies == null || m_Database.bossTrophies.Count == 0) return null;
            Customization.FurnitureDefinition trophy = m_Database.bossTrophies[0]?.trophy;
            if (trophy == null) return null;
            Customization.FurnitureState furniture = State.Furniture;
            if (furniture.OwnedCount(trophy.id) == 0) furniture.Receive(trophy);
            foreach (string area in new List<string>(furniture.AreaIds))
            {
                var kept = new List<Customization.PlacedFurniture>();
                foreach (Customization.PlacedFurniture p in furniture.Layout(area))
                    if (p.definition != trophy.id) kept.Add(p);
                if (kept.Count != furniture.Layout(area).Count) furniture.SetLayout(area, kept);
            }
            furniture.PendingHomecoming = trophy.id;
            StateChanged?.Invoke();
            Save();
            return trophy.id;
        }

        /// <summary>Debug: add gold.</summary>
        public void DebugAddGold(int amount)
        {
            if (!InGame) return;
            State.AddGold(amount);
            StateChanged?.Invoke();
        }

        public void Save()
        {
            if (!InGame) return;
            try
            {
                // The story's middleware records its state into the game's first (4g): one save, one file.
                StoryServices.State?.Capture(State);
                Store.Write(SaveSystem.ToJson(SaveSystem.Capture(State)));
                EventBus<GameSaved>.Publish(new GameSaved(State.Day));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                LastWarnings.Add(e.Message);
            }
        }

        // ---------- Scenes ----------

        static string[] ContentFor(GameState state) => GameScenes.ContentFor(state.Phase, state.Story.Opening);

        /// <summary>A new game's world (4h Checkpoint B): its seed, the starter garden's empty beds, today's Vigor.</summary>
        void StartWorld()
        {
            State.WorldSeed = WorldSeed.New();
            State.Garden.Ensure(m_Database != null ? m_Database.GardenBeds : null);
            State.Vigor.Configure(VigorSettings);
        }

        /// <summary>A new surface day (or a loaded one): the clock back to the morning; the next frame announces it.</summary>
        void StartSurfaceDay()
        {
            State.Surface.Reset(SurfaceTime.CurrentSettings);
            SurfaceTime.Instance?.Restart();
        }

        void Load(params string[] scenes) => StartCoroutine(LoadContent(scenes));

        /// <summary>
        /// Swaps the content scenes for <paramref name="scenes"/> (4h: a content set), behind the cover. Every scene of the
        /// set is loaded fresh; the first becomes the active one. Scenes outside the set are unloaded.
        /// </summary>
        IEnumerator LoadContent(string[] scenes)
        {
            while (IsLoading) yield return null;
            IsLoading = true;
            ISceneTransition transition = Transition;
            if (transition != null) yield return transition.Cover();
            foreach (string loaded in m_Loaded)
                if (SceneManager.GetSceneByName(loaded).isLoaded)
                    yield return SceneManager.UnloadSceneAsync(loaded);
            m_Loaded.Clear();
            foreach (string scene in scenes)
            {
                yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
                m_Loaded.Add(scene);
                if (m_Loaded.Count == 1) SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene));
            }
            IsLoading = false;
            if (transition != null) yield return transition.Reveal();
        }
    }
}
