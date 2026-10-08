using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Save;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// The main menu (4c decision 6; 4i-A): Continue, when there's a save that can be read (saying which day it resumes), New
    /// Game, the controls reference and, on desktop, Quit, over a still of Tally Ho! and Kariaston, with the build's version in the
    /// corner. Starting a new game over any save file, readable or not, asks once.
    /// <para>
    /// 4i-A, saves the player can trust: a save that can't be read says so, and when the last good save before it was kept as the
    /// backup, Continue offers that instead; a save from a newer version of the game says so and is never loaded here.
    /// </para>
    /// Played on its own the scene loads Boot, which brings it back with the day loop running.
    /// </summary>
    public sealed class MainMenuScreen : MonoBehaviour
    {
        [SerializeField] GameObject m_Choices;
        [SerializeField] Button m_Continue;
        [SerializeField] LocalizedSuperText m_ContinueDetail;
        [SerializeField] Button m_NewGame;
        [SerializeField] GameObject m_Confirm;
        [SerializeField] Button m_ConfirmYes;
        [SerializeField] Button m_ConfirmNo;
        [SerializeField] CharacterCreatorScreen m_Creator;
        [SerializeField] Button m_Controls;
        [SerializeField] Button m_Quit;
        [SerializeField] ControlsPage m_ControlsPage;
        [SerializeField] LocalizedSuperText m_Message;
        [SerializeField] LocalizedSuperText m_Version;
        [SerializeField] GameObject m_Title;
        [SerializeField] GameObject m_Panel;

        GameFlow m_Flow;
        bool m_HasSave;
        bool m_FromBackup;

        public Button ContinueButton => m_Continue;
        public Button NewGameButton => m_NewGame;
        public Button ControlsButton => m_Controls;
        public Button QuitButton => m_Quit;
        public bool IsConfirming => m_Confirm != null && m_Confirm.activeSelf;
        public Button ConfirmYes => m_ConfirmYes;
        public Button ConfirmNo => m_ConfirmNo;
        public CharacterCreatorScreen Creator => m_Creator;
        public ControlsPage ControlsPage => m_ControlsPage;
        /// <summary>The save problem the menu is showing (its key), or null.</summary>
        public string MessageKey => MessageGroup != null && MessageGroup.alpha > 0f ? m_Message.Key : null;
        /// <summary>Continue would load the backup.</summary>
        public bool ContinuesFromBackup => m_FromBackup;

        /// <summary>The character creator New Game opens (4g Checkpoint B).</summary>
        public void ConfigureCreator(CharacterCreatorScreen creator) => m_Creator = creator;

        public void Configure(GameObject choices, Button continueButton, LocalizedSuperText continueDetail, Button newGame, GameObject confirm, Button yes, Button no)
        {
            m_Choices = choices;
            m_Continue = continueButton;
            m_ContinueDetail = continueDetail;
            m_NewGame = newGame;
            m_Confirm = confirm;
            m_ConfirmYes = yes;
            m_ConfirmNo = no;
        }

        /// <summary>4i-A: the controls page and quit, the save message, the version label and the title (hidden under the controls).</summary>
        public void ConfigureFirstImpressions(Button controls, Button quit, ControlsPage controlsPage, LocalizedSuperText message, LocalizedSuperText version, GameObject title,
            GameObject panel)
        {
            m_Panel = panel;
            m_Controls = controls;
            m_Quit = quit;
            m_ControlsPage = controlsPage;
            m_Message = message;
            m_Version = version;
            m_Title = title;
        }

        void Start()
        {
            m_Flow = GameFlow.Instance;
            if (m_Flow == null)
            {
                SceneManager.LoadScene(GameScenes.Boot);
                return;
            }
            SaveCheck save = m_Flow.CheckSave();
            SaveCheck backup = save.Problem == SaveProblem.Unreadable ? m_Flow.CheckBackup() : SaveCheck.Missing;
            m_FromBackup = !save.Usable && backup.Usable;
            SaveData shown = save.Usable ? save.Data : backup.Data;
            m_HasSave = shown != null;
            m_Continue.gameObject.SetActive(m_HasSave);
            m_ContinueDetail.gameObject.SetActive(m_HasSave);
            // The line waits for the string tables (no raw "day {0}, {1}" while they load).
            Hide(m_ContinueDetail);
            // The gap between Continue and New Game goes with them.
            Transform gap = m_Choices.transform.Find("Gap");
            if (gap != null) gap.gameObject.SetActive(m_HasSave);
            if (m_HasSave) StartCoroutine(ShowSave(shown, m_FromBackup));
            StartCoroutine(ShowProblem(save.Problem, backup.Usable));
            if (m_Version != null) StartCoroutine(ShowVersion());
            m_Continue.onClick.AddListener(() => m_Flow.Continue(m_FromBackup));
            m_NewGame.onClick.AddListener(NewGame);
            m_ConfirmYes.onClick.AddListener(OpenCreator);
            m_ConfirmNo.onClick.AddListener(() => ShowChoices(m_NewGame));
            if (m_Controls != null) m_Controls.onClick.AddListener(OpenControls);
            if (m_Quit != null)
            {
                m_Quit.gameObject.SetActive(PauseMenu.CanQuitApplication);
                m_Quit.onClick.AddListener(Application.Quit);
            }
            ShowChoices(m_HasSave ? m_Continue : m_NewGame);
        }

        void Update()
        {
            if (m_ControlsPage != null && m_ControlsPage.IsOpen && m_ControlsPage.HandleInput()) ShowChoices(m_Controls);
        }

        void NewGame()
        {
            // Any save file at all, readable or not, is asked about before it's replaced.
            if (!m_Flow.AnySaveFile)
            {
                OpenCreator();
                return;
            }
            m_Choices.SetActive(false);
            SetPanel(false);
            m_Confirm.SetActive(true);
            // Back is the safe default: starting over replaces the save.
            Select(m_ConfirmNo);
        }

        /// <summary>
        /// A new game begins with the keeper (4g Checkpoint B): the creator, then Tally Ho!. Without a creator (an old menu
        /// scene) the game starts straight away, as Bram.
        /// </summary>
        void OpenCreator()
        {
            m_Confirm.SetActive(false);
            if (m_Creator == null)
            {
                m_Flow.NewGame(new Hearthdelve.Shared.Story.PlayerProfile());
                return;
            }
            m_Choices.SetActive(false);
            SetPanel(false);
            ShowMessage(false);
            m_Creator.Open(m_Flow, () => ShowChoices(m_NewGame));
        }

        void OpenControls()
        {
            if (m_ControlsPage == null) return;
            m_Choices.SetActive(false);
            SetPanel(false);
            if (m_Title != null) m_Title.SetActive(false);
            ShowMessage(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            m_ControlsPage.Open();
        }

        void ShowChoices(Button selected)
        {
            m_Confirm.SetActive(false);
            m_Choices.SetActive(true);
            SetPanel(true);
            if (m_Title != null) m_Title.SetActive(true);
            ShowMessage(m_ProblemShown);
            Select(selected != null && selected.gameObject.activeInHierarchy ? selected : m_NewGame);
        }

        void SetPanel(bool shown)
        {
            if (m_Panel != null) m_Panel.SetActive(shown);
        }

        static void Select(Button button)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        static void Hide(LocalizedSuperText text)
        {
            CanvasGroup group = text.GetComponent<CanvasGroup>();
            if (group == null) group = text.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
        }

        static void Reveal(LocalizedSuperText text)
        {
            CanvasGroup group = text.GetComponent<CanvasGroup>();
            if (group != null) group.alpha = 1f;
        }

        bool m_ProblemShown;

        /// <summary>The message's strip (its dark backing) fades with it; never deactivated, so its text always draws.</summary>
        CanvasGroup MessageGroup
        {
            get
            {
                if (m_Message == null) return null;
                Transform strip = m_Message.transform.parent != null && m_Message.transform.parent.name == "Message" ? m_Message.transform.parent : m_Message.transform;
                CanvasGroup group = strip.GetComponent<CanvasGroup>();
                if (group == null) group = strip.gameObject.AddComponent<CanvasGroup>();
                return group;
            }
        }

        void ShowMessage(bool shown)
        {
            CanvasGroup group = MessageGroup;
            if (group == null) return;
            group.alpha = shown ? 1f : 0f;
            group.blocksRaycasts = false;
        }

        System.Collections.IEnumerator ShowProblem(SaveProblem problem, bool backupUsable)
        {
            if (m_Message == null) yield break;
            ShowMessage(false);
            string key = problem switch
            {
                SaveProblem.Unreadable => backupUsable ? MenuLocKeys.SaveUnreadableBackup : MenuLocKeys.SaveUnreadable,
                SaveProblem.Newer => MenuLocKeys.SaveNewer,
                _ => null,
            };
            if (key == null) yield break;
            while (!Loc.IsReady) yield return null;
            m_Message.Set(key);
            m_ProblemShown = true;
            ShowMessage(m_Choices.activeSelf);
        }

        System.Collections.IEnumerator ShowVersion()
        {
            while (!Loc.IsReady) yield return null;
            m_Version.Set(MenuLocKeys.Version, Application.version);
        }

        /// <summary>
        /// "day 3, daytime". The phase word is looked up as an argument, so it waits for the string tables: on the web they
        /// load asynchronously, and a lookup before then freezes the raw key into the line (found in the 4d web smoke test).
        /// </summary>
        System.Collections.IEnumerator ShowSave(SaveData save, bool fromBackup)
        {
            while (!Loc.IsReady) yield return null;
            m_ContinueDetail.Set(fromBackup ? MenuLocKeys.BackupFrom : LoopLocKeys.MenuContinueFrom, save.day, Loc.UI(PhaseKey(save.phase)));
            // One frame for the text to take, then show it.
            yield return null;
            Reveal(m_ContinueDetail);
        }

        static string PhaseKey(string phase) => phase switch
        {
            nameof(DayPhase.Evening) => LoopLocKeys.PhaseEvening,
            nameof(DayPhase.Delve) => LoopLocKeys.PhaseDelve,
            nameof(DayPhase.Night) => LoopLocKeys.PhaseNight,
            _ => LoopLocKeys.PhaseMorning,
        };
    }
}
