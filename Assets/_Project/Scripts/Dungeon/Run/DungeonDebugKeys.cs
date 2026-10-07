using Hearthdelve.Dungeon.Essence;
using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.Dungeon.Run
{
    /// <summary>
    /// Delve playtest keys, in the editor and development builds only (4g Checkpoint C, for the browser playthrough; the look test's
    /// old table, restored for the day loop): F4 refills the keeper's Essence, F6 toggles god mode (no Essence lost to drain or
    /// hits). Never in a release build.
    /// </summary>
    public sealed class DungeonDebugKeys : MonoBehaviour
    {
        void Awake()
        {
            if (!Debug.isDebugBuild) enabled = false;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !LevelManager.HasInstance || LevelManager.Instance.Players == null || LevelManager.Instance.Players.Count == 0) return;
            var essence = LevelManager.Instance.Players[0].GetComponent<EssenceHealth>();
            if (essence == null) return;
            if (keyboard.f4Key.wasPressedThisFrame)
            {
                essence.Restore(essence.MaximumHealth);
                Debug.Log("[Hearthdelve] Essence refilled (debug).");
            }
            if (keyboard.f6Key.wasPressedThisFrame)
            {
                essence.GodMode = !essence.GodMode;
                Debug.Log($"[Hearthdelve] God mode {(essence.GodMode ? "on" : "off")} (debug).");
            }
        }
    }
}
