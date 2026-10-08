using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Engine;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Story;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Tavern.Scene;
using Hearthdelve.UI.Localization;
using Hearthdelve.UI.Tavern;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// The pause menu (4i-A, A2), in Boot over every scene: resume, the controls reference, quit to the menu and (on desktop) quit
    /// the game. Start or Esc opens it when <see cref="PauseRules.CanOpen"/> allows; Esc, B or Start closes it. While it's open the
    /// game is paused (<see cref="MenuPause"/>), the surface clock holds, and the keeper's controls are off; closing gives back
    /// exactly the controls that were on.
    /// <para>
    /// Quitting follows <see cref="QuitRules"/>: the day and the night save and come back to the same moment; the evening's results
    /// and the delve's result are banked as their own buttons would bank them; quitting mid-evening, mid-delve or on arrival day
    /// asks first, says what is lost, and leaves the last save as it was.
    /// </para>
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] GameObject m_Root;
        [SerializeField] GameObject m_Main;
        [SerializeField] Button m_Resume;
        [SerializeField] Button m_Controls;
        [SerializeField] Button m_QuitMenu;
        [SerializeField] Button m_QuitGame;
        [SerializeField] GameObject m_Confirm;
        [SerializeField] LocalizedSuperText m_ConfirmText;
        [SerializeField] Button m_ConfirmYes;
        [SerializeField] Button m_ConfirmNo;
        [SerializeField] ControlsPage m_ControlsPage;

        InputAction m_Menu;
        bool m_PausableLastFrame;
        int m_ClosedFrame = -1;
        string[] m_Maps;
        bool m_QuitApp;

        public static PauseMenu Instance { get; private set; }

        public bool IsOpen => m_Root != null && m_Root.activeSelf;
        public bool IsConfirming => m_Confirm != null && m_Confirm.activeSelf;
        public ControlsPage ControlsPage => m_ControlsPage;
        public Button ResumeButton => m_Resume;
        public Button ControlsButton => m_Controls;
        public Button QuitMenuButton => m_QuitMenu;
        public Button QuitGameButton => m_QuitGame;
        public Button ConfirmYes => m_ConfirmYes;
        public Button ConfirmNo => m_ConfirmNo;
        /// <summary>What the confirmation says (tests).</summary>
        public string ConfirmKey => m_ConfirmText != null ? m_ConfirmText.Key : null;

        /// <summary>Quitting the application is offered (desktop builds; the web page is closed by the browser).</summary>
        public static bool CanQuitApplication => Application.platform != RuntimePlatform.WebGLPlayer;

        public void Configure(GameObject root, GameObject main, Button resume, Button controls, Button quitMenu, Button quitGame,
            GameObject confirm, LocalizedSuperText confirmText, Button yes, Button no, ControlsPage controlsPage)
        {
            m_Root = root;
            m_Main = main;
            m_Resume = resume;
            m_Controls = controls;
            m_QuitMenu = quitMenu;
            m_QuitGame = quitGame;
            m_Confirm = confirm;
            m_ConfirmText = confirmText;
            m_ConfirmYes = yes;
            m_ConfirmNo = no;
            m_ControlsPage = controlsPage;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            if (m_Root != null) m_Root.SetActive(false);
            // Esc and Start, always listening; whether they may open the menu is decided each frame (PauseRules).
            m_Menu = new InputAction("Pause Menu", InputActionType.Button);
            m_Menu.AddBinding("<Keyboard>/escape");
            m_Menu.AddBinding("<Gamepad>/start");
            m_Menu.Enable();
            m_Resume?.onClick.AddListener(Close);
            m_Controls?.onClick.AddListener(OpenControls);
            m_QuitMenu?.onClick.AddListener(() => AskQuit(app: false));
            m_QuitGame?.onClick.AddListener(() => AskQuit(app: true));
            m_ConfirmYes?.onClick.AddListener(Quit);
            m_ConfirmNo?.onClick.AddListener(ShowMain);
            if (m_QuitGame != null && !CanQuitApplication)
            {
                // The web: no quit game, and the panel closes up round the three buttons left (whole pixels).
                m_QuitGame.gameObject.SetActive(false);
                if (m_Main != null && m_Main.transform is RectTransform main)
                {
                    main.sizeDelta -= new Vector2(0f, 20f);
                    foreach (RectTransform child in main) child.anchoredPosition -= new Vector2(0f, 10f);
                }
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            m_Menu?.Dispose();
            if (IsOpen) ReleaseHolds();
        }

        // ---------- when ----------

        /// <summary>Whether Start or Esc would open the menu now.</summary>
        public static bool Pausable
        {
            get
            {
                GameFlow flow = GameFlow.Instance;
                bool inGame = flow != null && flow.InGame;
                bool loading = flow != null && (flow.IsLoading || (flow.Transition != null && flow.Transition.IsCovering));
                bool talking = StoryServices.Conversations != null && StoryServices.Conversations.IsTalking;
                bool onFoot = MapOn(InputMaps.Tavern) || MapOn(InputMaps.Dungeon);
                return PauseRules.CanOpen(inGame, loading, talking, PauseRules.IsBlocked, onFoot, MenuPause.IsPaused, OnPhaseScreen());
            }
        }

        static bool MapOn(string map) => InputSystem.actions != null && InputSystem.actions.FindActionMap(map)?.enabled == true;

        static PrepScreen s_Prep;
        static EveningResultsScreen s_Results;
        static NightScreen s_Night;
        static DelveResultScreen s_DelveResult;

        /// <summary>The phase screens of the scenes loaded now (found again whenever a scene loads).</summary>
        static void FindScreens()
        {
            s_Prep = FindAnyObjectByType<PrepScreen>(FindObjectsInactive.Include);
            s_Results = FindAnyObjectByType<EveningResultsScreen>(FindObjectsInactive.Include);
            s_Night = FindAnyObjectByType<NightScreen>(FindObjectsInactive.Include);
            s_DelveResult = FindAnyObjectByType<DelveResultScreen>(FindObjectsInactive.Include);
        }

        /// <summary>Prep, the evening's results, the night, the delve's result: screens that are the moment itself, with nothing else over them.</summary>
        static bool OnPhaseScreen()
        {
            // Over them, a station (the Butcher Block at Prep), Decorate Mode or a panel owns Esc.
            if (MapOn(InputMaps.Minigame) || MapOn(InputMaps.Decorate)) return false;
            return (s_Prep != null && s_Prep.IsShown) || (s_Results != null && s_Results.IsShown) || (s_Night != null && s_Night.IsShown) ||
                   (s_DelveResult != null && s_DelveResult.IsOpen);
        }

        void OnEnable()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            FindScreens();
        }

        void OnDisable() => UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;

        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) => FindScreens();

        void Update()
        {
            if (IsOpen)
            {
                HandleOpen();
                return;
            }
            // Only if it was pausable last frame too: Esc that just closed a panel doesn't also open the menu.
            if (m_Menu.WasPressedThisFrame() && m_PausableLastFrame && Time.frameCount != m_ClosedFrame && Pausable) Open();
        }

        void LateUpdate() => m_PausableLastFrame = !IsOpen && Pausable;

        void HandleOpen()
        {
            if (m_ControlsPage != null && m_ControlsPage.IsOpen)
            {
                if (m_ControlsPage.HandleInput()) ShowMain(m_Controls);
                return;
            }
            InputAction cancel = InputMaps.Find(InputMaps.UI, UIActions.Cancel);
            bool back = (cancel != null && cancel.WasPressedThisFrame()) || m_Menu.WasPressedThisFrame();
            if (!back) return;
            if (IsConfirming) ShowMain(m_QuitMenu);
            else Close();
        }

        // ---------- open and close ----------

        public void Open()
        {
            if (IsOpen || m_Root == null) return;
            m_Maps = InputMaps.Snapshot();
            InputMaps.ActivateUIOnly();
            MenuPause.Push();
            SurfacePause.Hold(this);
            m_Root.SetActive(true);
            ShowMain(m_Resume);
        }

        public void Close()
        {
            if (!IsOpen) return;
            m_ControlsPage?.Close();
            m_Root.SetActive(false);
            ReleaseHolds();
            InputMaps.Restore(m_Maps);
            m_ClosedFrame = Time.frameCount;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        void ReleaseHolds()
        {
            MenuPause.Pop();
            SurfacePause.Release(this);
        }

        void ShowMain() => ShowMain(m_Resume);

        void ShowMain(Button selected)
        {
            m_ControlsPage?.Close();
            if (m_Confirm != null) m_Confirm.SetActive(false);
            if (m_Main != null) m_Main.SetActive(true);
            Select(selected != null && selected.gameObject.activeInHierarchy ? selected : m_Resume);
        }

        void OpenControls()
        {
            if (m_ControlsPage == null) return;
            if (m_Main != null) m_Main.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            m_ControlsPage.Open(StartPage());
        }

        /// <summary>The controls page for where the keeper is: the Hollows, decorating isn't pausable, the evening, or the day.</summary>
        static int StartPage()
        {
            GameFlow flow = GameFlow.Instance;
            if (flow == null || !flow.InGame) return 0;
            return flow.State.Phase switch
            {
                DayPhase.Delve => 2,
                DayPhase.Evening => 1,
                _ => 0,
            };
        }

        static void Select(Button button)
        {
            if (button != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        // ---------- quitting ----------

        /// <summary>The plan for quitting now (<see cref="QuitRules"/>).</summary>
        public static QuitPlan CurrentPlan()
        {
            GameFlow flow = GameFlow.Instance;
            if (flow == null || !flow.InGame) return new QuitPlan(QuitKind.SaveAndQuit);
            TavernDirector director = TavernDirector.Instance;
            // The delve is over when its result shows (the screen holds the report its button would bring home).
            bool delveOver = s_DelveResult != null && s_DelveResult.IsOpen;
            return QuitRules.Plan(flow.State.Phase, flow.State.Story.Opening, director != null ? director.Phase.ToString() : null, delveOver);
        }

        void AskQuit(bool app)
        {
            m_QuitApp = app;
            QuitPlan plan = CurrentPlan();
            if (!plan.Warns)
            {
                Quit();
                return;
            }
            if (m_ConfirmText != null)
                m_ConfirmText.Set(plan.Kind switch
                {
                    QuitKind.RestartArrival => MenuLocKeys.QuitArrival,
                    QuitKind.AbandonDelve => MenuLocKeys.QuitDelve,
                    _ => MenuLocKeys.QuitEvening,
                });
            if (m_Main != null) m_Main.SetActive(false);
            if (m_Confirm != null) m_Confirm.SetActive(true);
            // Back is the safe default.
            Select(m_ConfirmNo);
        }

        void Quit()
        {
            QuitPlan plan = CurrentPlan();
            bool app = m_QuitApp;
            GameFlow flow = GameFlow.Instance;
            m_ControlsPage?.Close();
            if (m_Confirm != null) m_Confirm.SetActive(false);
            if (IsOpen)
            {
                m_Root.SetActive(false);
                ReleaseHolds();
            }
            if (flow != null && flow.InGame)
            {
                switch (plan.Kind)
                {
                    case QuitKind.BankEvening:
                        TavernDirector.Instance?.BankForQuit();
                        break;
                    case QuitKind.BankDelve:
                        flow.CompleteDelve(s_DelveResult.Report, goHome: false);
                        break;
                }
                flow.QuitToMenu(save: plan.Kind == QuitKind.SaveAndQuit);
            }
            if (app && CanQuitApplication) Application.Quit();
        }
    }
}
