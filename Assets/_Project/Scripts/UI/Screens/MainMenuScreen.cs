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
    /// The main menu (4c decision 6): Continue, when there's a save that can be read (saying which day it
    /// resumes), and New Game. Starting a new game over an existing save asks once. Played on its own the
    /// scene loads Boot, which brings it back with the day loop running.
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

        GameFlow m_Flow;
        bool m_HasSave;

        public Button ContinueButton => m_Continue;
        public Button NewGameButton => m_NewGame;
        public bool IsConfirming => m_Confirm != null && m_Confirm.activeSelf;
        public Button ConfirmYes => m_ConfirmYes;
        public Button ConfirmNo => m_ConfirmNo;

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

        void Start()
        {
            m_Flow = GameFlow.Instance;
            if (m_Flow == null)
            {
                SceneManager.LoadScene(GameScenes.Boot);
                return;
            }
            SaveData save = m_Flow.PeekSave();
            m_HasSave = save != null;
            m_Continue.gameObject.SetActive(m_HasSave);
            m_ContinueDetail.gameObject.SetActive(m_HasSave);
            // The gap between Continue and New Game goes with them.
            Transform gap = m_Choices.transform.Find("Gap");
            if (gap != null) gap.gameObject.SetActive(m_HasSave);
            if (m_HasSave) m_ContinueDetail.Set(LoopLocKeys.MenuContinueFrom, save.day, Loc.UI(PhaseKey(save.phase)));
            m_Continue.onClick.AddListener(() => m_Flow.Continue());
            m_NewGame.onClick.AddListener(NewGame);
            m_ConfirmYes.onClick.AddListener(() => m_Flow.NewGame());
            m_ConfirmNo.onClick.AddListener(() => ShowChoices(m_NewGame));
            ShowChoices(m_HasSave ? m_Continue : m_NewGame);
        }

        void NewGame()
        {
            if (!m_HasSave)
            {
                m_Flow.NewGame();
                return;
            }
            m_Choices.SetActive(false);
            m_Confirm.SetActive(true);
            // Back is the safe default: starting over replaces the save.
            Select(m_ConfirmNo);
        }

        void ShowChoices(Button selected)
        {
            m_Confirm.SetActive(false);
            m_Choices.SetActive(true);
            Select(selected);
        }

        static void Select(Button button)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        static string PhaseKey(string phase) => phase switch
        {
            nameof(DayPhase.Evening) => LoopLocKeys.PhaseEvening,
            nameof(DayPhase.Night) => LoopLocKeys.PhaseNight,
            _ => LoopLocKeys.PhaseMorning,
        };
    }
}
