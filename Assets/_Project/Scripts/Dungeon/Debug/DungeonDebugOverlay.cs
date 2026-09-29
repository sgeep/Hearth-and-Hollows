using Hearthdelve.Core.Services;
using Hearthdelve.Dungeon.Player;
using Hearthdelve.Dungeon.Run;
using Hearthdelve.Shared.Ingredients;
using Hearthdelve.Shared.Run;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Hearthdelve.Dungeon.DebugTools
{
    /// <summary>
    /// Developer-only overlay for the greybox (not player-facing, so not localized).
    /// F1 toggles the panel. Removes itself outside development builds.
    /// </summary>
    public sealed class DungeonDebugOverlay : MonoBehaviour
    {
        [SerializeField] PlayerController m_Player;

        bool m_Visible = true;
        PlayerVitals m_Vitals;

        public void Configure(PlayerController player) => m_Player = player;

        void Awake()
        {
            if (!Debug.isDebugBuild) Destroy(this);
        }

        void Start()
        {
            if (m_Player == null) m_Player = FindFirstObjectByType<PlayerController>();
            if (m_Player != null) m_Vitals = m_Player.GetComponent<PlayerVitals>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || m_Player == null) return;

            if (kb.f1Key.wasPressedThisFrame) m_Visible = !m_Visible;
            if (kb.digit1Key.wasPressedThisFrame) m_Player.ElementOverride = null;
            if (kb.digit2Key.wasPressedThisFrame) m_Player.ElementOverride = Element.Fire;
            if (kb.digit3Key.wasPressedThisFrame) m_Player.ElementOverride = Element.Ice;
            if (kb.digit4Key.wasPressedThisFrame) m_Player.ElementOverride = Element.Poison;
            if (kb.f2Key.wasPressedThisFrame) GameSettings.ScreenShakeScale = GameSettings.ScreenShakeScale > 0f ? 0f : 1f;
            if (kb.f3Key.wasPressedThisFrame) GameSettings.HitStopEnabled = !GameSettings.HitStopEnabled;
            if (kb.f4Key.wasPressedThisFrame && m_Vitals != null) m_Vitals.Restore(1000f);
            if (kb.f5Key.wasPressedThisFrame && m_Vitals != null) m_Vitals.Essence.TakeDamage(Mathf.Max(0f, m_Vitals.Essence.Current - 3f));
            if (kb.f6Key.wasPressedThisFrame && m_Vitals != null) m_Vitals.GodMode = !m_Vitals.GodMode;
            if (kb.f7Key.wasPressedThisFrame && !GamePause.IsMenuPaused) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void OnGUI()
        {
            if (!m_Visible || m_Player == null || m_Player.Motor == null) return;
            var motor = m_Player.Motor;
            var combo = m_Player.Combo;
            GUILayout.BeginArea(new Rect(8, Screen.height - 250, 380, 245), GUI.skin.box);
            GUILayout.Label($"<b>DEBUG</b> (F1 hide)   state: {motor.State}  facing: {motor.Facing}  iframes: {motor.IsInvulnerable}");
            GUILayout.Label($"vel: {motor.Velocity.x:0.0}, {motor.Velocity.y:0.0}   combo: {(combo.IsBusy ? $"hit {combo.Index + 1} {combo.Phase}" : "idle")}");
            if (m_Vitals != null)
                GUILayout.Label($"essence: {m_Vitals.Essence.Current:0.0}/{m_Vitals.Essence.Max:0}   god: {m_Vitals.GodMode}");
            GUILayout.Label($"element: {m_Player.CurrentElement}  [1 none, 2 fire, 3 ice, 4 poison]");
            GUILayout.Label($"shake: {(GameSettings.ScreenShakeScale > 0 ? "on" : "off")} [F2]  hit-stop: {(GameSettings.HitStopEnabled ? "on" : "off")} [F3]");
            GUILayout.Label("F4 refill essence   F5 essence to 3   F6 god mode   F7 restart");
            var run = DelveRunController.Active;
            GUILayout.Label($"satchel: {(run != null ? run.Satchel.TotalCount : 0)} parts   stash (kept): {PersistentStash.Items.Count}");
            GUILayout.EndArea();
        }
    }
}
