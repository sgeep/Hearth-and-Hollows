using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.Shared.Game
{
    /// <summary>
    /// Day-loop playtest keys (4c decision 4), in the editor and development builds only: F8 finishes the
    /// current phase (the loaded scene ends it properly where it can), F9 adds 100 gold. Lives in Boot.
    /// </summary>
    public sealed class GameFlowDebugKeys : MonoBehaviour
    {
        void Awake()
        {
            if (!Debug.isDebugBuild) enabled = false;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            GameFlow flow = GameFlow.Instance;
            if (keyboard == null || flow == null || !flow.InGame) return;
            if (keyboard.f8Key.wasPressedThisFrame) flow.DebugSkipPhase();
            if (keyboard.f9Key.wasPressedThisFrame) flow.DebugAddGold(100);
        }
    }
}
