using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// Service playtest keys (4c decision 4), in the editor and development builds only:
    /// F5 ends the service now, F6 lets a customer in. The rest (F4, F8, F9) come with the prep screen
    /// and the day loop; the old F1 panel stays deferred.
    /// </summary>
    public sealed class TavernDebugKeys : MonoBehaviour
    {
        void Awake()
        {
            if (!Debug.isDebugBuild) enabled = false;
        }

        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            TavernDirector director = TavernDirector.Instance;
            if (keyboard == null || director == null) return;
            if (keyboard.f5Key.wasPressedThisFrame) director.EndServiceNow();
            if (keyboard.f6Key.wasPressedThisFrame) director.SpawnCustomer();
        }
    }
}
