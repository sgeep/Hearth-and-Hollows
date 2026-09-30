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
        public const string Tavern = "TavernGreybox";
        public const string Dungeon = "CombatGreybox";
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
        public DayPhase Phase => State?.Phase ?? DayPhase.Morning;
        public DelveLoadout Loadout => State != null && m_Database != null ? m_Database.Loadout(State) : DelveLoadout.None;
        public SaveStore Store { get; private set; }
        public bool HasSave => Store != null && Store.Exists;
        public bool IsLoading { get; private set; }
        public string LoadedScene => m_LoadedScene;
        public bool AllowDebugFill => m_Database != null && m_Database.allowDebugFill;
        /// <summary>Debug end-of-day summary at Night (F10).</summary>
        public bool ShowSummary { get; set; }
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
            ShowSummary = Debug.isDebugBuild;
        }

        void Start() => Load(GameScenes.MainMenu);

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---------- Menu ----------

        public void NewGame()
        {
            State = new GameState { Gold = m_Database != null ? m_Database.newGameGold : 0 };
            PhaseChanged?.Invoke();
            Load(SceneFor(State.Phase));
        }

        /// <summary>Loads the save and resumes at its phase. Returns false if there's no readable save.</summary>
        public bool Continue()
        {
            LastWarnings.Clear();
            if (!HasSave) return false;
            try
            {
                State = SaveSystem.Restore(SaveSystem.FromJson(Store.Read()), m_Database, LastWarnings);
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

        public void StartDelve()
        {
            DayRules.StartDelve(State);
            PhaseChanged?.Invoke();
            Load(GameScenes.Dungeon);
        }

        public void CompleteDelve(DelveReport report)
        {
            DayRules.CompleteDelve(State, report);
            PhaseChanged?.Invoke();
            Load(GameScenes.Tavern);
        }

        /// <summary>Evening service is over: bank the takings, move to Night (the tavern stays loaded), autosave.</summary>
        public void CompleteService(ServiceReport report)
        {
            DayRules.CompleteService(State, report);
            PhaseChanged?.Invoke();
            Save();
        }

        public void SkipService()
        {
            DayRules.SkipService(State);
            PhaseChanged?.Invoke();
            Save();
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

        /// <summary>Overnight freshness loss, next morning, autosave, fresh tavern scene.</summary>
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
                case DayPhase.Morning: StartDelve(); break;
                case DayPhase.Delve: CompleteDelve(DelveReport.Empty); break;
                case DayPhase.Evening: SkipService(); break;
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
            if (!string.IsNullOrEmpty(m_LoadedScene) && SceneManager.GetSceneByName(m_LoadedScene).isLoaded)
                yield return SceneManager.UnloadSceneAsync(m_LoadedScene);
            yield return SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName(scene));
            m_LoadedScene = scene;
            IsLoading = false;
        }
    }
}
