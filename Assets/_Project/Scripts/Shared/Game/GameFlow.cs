using System;
using System.Collections;
using System.Collections.Generic;
using Hearthdelve.Core.Events;
using Hearthdelve.Shared.Progression;
using Hearthdelve.Shared.Save;
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
            PhaseChanged?.Invoke();
            Load(SceneFor(State.Phase));
            return true;
        }

        public void QuitToMenu()
        {
            State = null;
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
            DayRules.CompleteDelve(State, report);
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

        public bool BuyUpgrade(TavernUpgradeDefinition upgrade)
        {
            if (!DayRules.BuyUpgrade(State, upgrade)) return false;
            StateChanged?.Invoke();
            Save();
            return true;
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
