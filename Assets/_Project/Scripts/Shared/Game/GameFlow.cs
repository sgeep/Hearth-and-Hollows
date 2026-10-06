using System;
using System.Collections;
using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Progression;
using Hearthdelve.Shared.Save;
using Hearthdelve.Shared.Story;
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
    }

    /// <summary>
    /// Covers the screen while the content scene changes and reveals it again (a fade with the day and
    /// phase, in the UI). Optional: without one, scenes simply swap.
    /// </summary>
    public interface ISceneTransition
    {
        IEnumerator Cover();
        IEnumerator Reveal();
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

        string m_LoadedScene;

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
        public string LoadedScene => m_LoadedScene;
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
            if (!HasSave) return null;
            try
            {
                return SaveSystem.FromJson(Store.Read());
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Hearthdelve] The save can't be read: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Starts day 1 with a delve into the Hollows: the storeroom starts empty, so the first thing to do is go down for
        /// something to cook (after the 4d playtest: an empty first daytime and a shut first evening were dead time). The
        /// first night, sleep, and the second day begins in the daytime. Saved at once, so Continue never brings back a
        /// game the player started over from.
        /// </summary>
        public void NewGame()
        {
            State = new GameState(1, DayPhase.Delve) { Gold = m_Database != null ? m_Database.newGameGold : 0 };
            State.Furniture.GrantStarter(m_Database != null ? m_Database.startingFurniture : null);
            // A fresh story (4g): no history, the opening still to play (Step 5). Explicit, never inferred from the day.
            State.Story.OpeningComplete = false;
            StoryServices.State?.Clear();
            Save();
            PhaseChanged?.Invoke();
            Load(SceneFor(State.Phase));
        }

        /// <summary>Loads the save and resumes at its phase. Returns false if there's no readable save.</summary>
        public bool Continue()
        {
            LastWarnings.Clear();
            SaveData data = PeekSave();
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
            PhaseChanged?.Invoke();
            Load(SceneFor(State.Phase));
            return true;
        }

        public void QuitToMenu()
        {
            State = null;
            StoryServices.State?.Clear();
            Load(GameScenes.MainMenu);
        }

        // ---------- The day ----------

        /// <summary>
        /// The day is done and the tavern opens for the evening (a fresh tavern scene). The daytime placeholder calls this
        /// today; the free-roaming village day will call it when its day ends. Not saved: daytime has nothing to lose.
        /// </summary>
        public void StartEvening()
        {
            DayRules.StartEvening(State);
            PhaseChanged?.Invoke();
            Load(GameScenes.Tavern);
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
        public void CompleteDelve(DelveReport report)
        {
            DayRules.CompleteDelve(State, report, m_Database != null ? m_Database.Furniture : null, m_Database != null ? m_Database.bossTrophies : null);
            // Facts for later reactions (4g's dialogue adapters): what came home from the Hollows.
            foreach (string curio in report.CuriosKept) EventBus<CurioBroughtHome>.Publish(new CurioBroughtHome(curio));
            // A boss's first defeat (4e): the hook for first-clear rewards, story reactions and 4f's trophy.
            foreach (string boss in report.BossesDefeated)
            {
                if (State.TimesDefeated(boss) == 1) EventBus<BossFirstCleared>.Publish(new BossFirstCleared(boss));
                EventBus<BossDefeated>.Publish(new BossDefeated(boss, State.TimesDefeated(boss), report.Outcome != DelveOutcome.Died));
            }
            PhaseChanged?.Invoke();
            Save();
            Load(GameScenes.Tavern);
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
            if (!DayRules.Buy(State, m_Database.market, offer)) return false;
            EventBus<MarketPurchase>.Publish(new MarketPurchase(offer.ingredient.id, offer.bundle, offer.price));
            StateChanged?.Invoke();
            Save();
            return true;
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
            DayRules.Sleep(State, m_Database != null ? m_Database.Freshness : default);
            PhaseChanged?.Invoke();
            Save();
            Load(GameScenes.Tavern);
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
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                LastWarnings.Add(e.Message);
            }
        }

        // ---------- Scenes ----------

        static string SceneFor(DayPhase phase) => phase == DayPhase.Delve ? GameScenes.Dungeon : GameScenes.Tavern;

        void Load(string scene) => StartCoroutine(LoadContent(scene));

        IEnumerator LoadContent(string scene)
        {
            while (IsLoading) yield return null;
            IsLoading = true;
            ISceneTransition transition = Transition;
            if (transition != null) yield return transition.Cover();
            if (!string.IsNullOrEmpty(m_LoadedScene) && SceneManager.GetSceneByName(m_LoadedScene).isLoaded)
                yield return SceneManager.UnloadSceneAsync(m_LoadedScene);
            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene));
            m_LoadedScene = scene;
            IsLoading = false;
            if (transition != null) yield return transition.Reveal();
        }
    }
}
