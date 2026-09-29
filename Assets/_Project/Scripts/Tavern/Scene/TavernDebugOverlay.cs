using Hearthdelve.Core.Random;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// Developer-only keys for the tavern greybox (not player-facing, so not localized).
    /// Removes itself outside development builds.
    /// </summary>
    public sealed class TavernDebugOverlay : MonoBehaviour
    {
        bool m_Visible = true;
        readonly IRandom m_Random = new SeededRandom();

        void Awake()
        {
            if (!Debug.isDebugBuild) Destroy(this);
        }

        void Update()
        {
            var kb = Keyboard.current;
            var director = TavernDirector.Instance;
            if (kb == null || director == null) return;

            if (kb.f1Key.wasPressedThisFrame) m_Visible = !m_Visible;
            if (kb.f4Key.wasPressedThisFrame) director.FillStoreroom();
            if (kb.f5Key.wasPressedThisFrame) director.EndServiceNow();
            if (kb.f6Key.wasPressedThisFrame && director.Session != null && director.Content.customers.Count > 0)
                director.SpawnCustomer(director.Content.customers[m_Random.Range(0, director.Content.customers.Count - 1)]);
            if (kb.f7Key.wasPressedThisFrame) director.Restart();
        }

        void OnGUI()
        {
            var director = TavernDirector.Instance;
            if (!m_Visible || director == null) return;
            GUILayout.BeginArea(new Rect(8, Screen.height - 110, 420, 105), GUI.skin.box);
            GUILayout.Label($"<b>DEBUG</b> (F1 hide)   phase: {director.Phase}   stock: {director.Storeroom.TotalCount} parts");
            if (director.Session != null)
                GUILayout.Label($"time left: {director.Session.Remaining:0}s   customers: {director.Agents.Count}   tickets: {director.Session.Tickets.Count}");
            GUILayout.Label("F4 fill storeroom   F5 end service   F6 spawn customer   F7 restart");
            GUILayout.EndArea();
        }
    }
}
