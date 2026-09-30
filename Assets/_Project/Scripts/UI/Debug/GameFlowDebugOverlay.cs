using Hearthdelve.Shared.Game;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.UI.DebugTools
{
    /// <summary>
    /// Developer-only day-loop keys, in the Boot scene (not player-facing, so not localized):
    /// F8 skip to the next phase, F9 +100 gold, F10 toggle the Night summary. Removes itself
    /// outside development builds. Avoids F1–F7, which the dungeon and tavern overlays use.
    /// </summary>
    public sealed class GameFlowDebugOverlay : MonoBehaviour
    {
        [SerializeField, Min(1)] int m_GoldStep = 100;

        void Awake()
        {
            if (!Debug.isDebugBuild) Destroy(this);
        }

        void Update()
        {
            var kb = Keyboard.current;
            var flow = GameFlow.Instance;
            if (kb == null || flow == null || !flow.InGame) return;
            if (kb.f8Key.wasPressedThisFrame) flow.DebugSkipPhase();
            if (kb.f9Key.wasPressedThisFrame) flow.DebugAddGold(m_GoldStep);
            if (kb.f10Key.wasPressedThisFrame) flow.ShowSummary = !flow.ShowSummary;
        }

        void OnGUI()
        {
            var flow = GameFlow.Instance;
            if (flow == null || !flow.InGame) return;
            var s = flow.State;
            GUILayout.BeginArea(new Rect(Screen.width * 0.5f - 230f, 4f, 460f, 26f), GUI.skin.box);
            GUILayout.Label($"Day {s.Day} · {s.Phase} · {s.Gold} gold   |   F8 skip phase  F9 +{m_GoldStep} gold  F10 summary");
            GUILayout.EndArea();
        }
    }
}
