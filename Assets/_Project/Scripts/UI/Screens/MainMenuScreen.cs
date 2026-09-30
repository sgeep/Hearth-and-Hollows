using Hearthdelve.Core.Input;
using Hearthdelve.Shared.Game;
using Hearthdelve.UI.Localization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Hearthdelve.UI.Screens
{
    /// <summary>New Game / Continue (one save slot) / Quit. Played on its own, it starts the Boot scene first.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuScreen : MonoBehaviour
    {
        Label m_Title, m_Note;
        Button m_Continue, m_NewGame, m_Quit;

        void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_Title = root.Q<Label>("title");
            m_Note = root.Q<Label>("note");
            m_Continue = root.Q<Button>("continue");
            m_NewGame = root.Q<Button>("new-game");
            m_Quit = root.Q<Button>("quit");
            m_Continue.clicked += OnContinue;
            m_NewGame.clicked += () => GameFlow.Instance?.NewGame();
            m_Quit.clicked += Application.Quit;
        }

        void Start()
        {
            // The game's services live in Boot; open it if this scene was played directly.
            if (GameFlow.Instance == null)
            {
                SceneManager.LoadScene(GameScenes.Boot);
                return;
            }
            InputMaps.ActivateUIOnly();
            Refresh();
            m_Continue.schedule.Execute(() => (m_Continue.enabledSelf ? m_Continue : m_NewGame).Focus());
        }

        void Refresh()
        {
            var flow = GameFlow.Instance;
            m_Title.text = Loc.UI(LoopLocKeys.MenuTitle);
            m_Continue.text = Loc.UI(LoopLocKeys.MenuContinue);
            m_NewGame.text = Loc.UI(LoopLocKeys.MenuNewGame);
            m_Quit.text = Loc.UI(LoopLocKeys.MenuQuit);
            m_Continue.SetEnabled(flow.HasSave);
            m_Note.text = flow.HasSave ? string.Empty : Loc.UI(LoopLocKeys.MenuNoSave);
        }

        void OnContinue()
        {
            var flow = GameFlow.Instance;
            if (flow != null && !flow.Continue()) m_Note.text = string.Join("\n", flow.LastWarnings);
        }
    }
}
