using UnityEngine;
using UnityEngine.InputSystem;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// Service playtest keys (4c decision 4), in the editor and development builds only:
    /// F4 fills the storeroom (at Prep; in the day loop only if the database allows it), F5 ends the service now,
    /// F6 lets a customer in, F7 opens Decorate Mode (4f). F8 and F9 are the day loop's (<c>GameFlowDebugKeys</c>, in Boot); the old F1 panel
    /// stays deferred.
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
            if (keyboard.f4Key.wasPressedThisFrame && director.Phase == TavernPhase.Prep && director.CanDebugFill) director.FillStoreroom();
            if (keyboard.f5Key.wasPressedThisFrame) director.EndServiceNow();
            if (keyboard.f6Key.wasPressedThisFrame) director.SpawnCustomer();
            // F7: Decorate Mode, whatever the quiet phase (the tavern played on its own has only Prep).
            if (keyboard.f7Key.wasPressedThisFrame && DecorateMode.Instance != null && DecorateMode.Instance.CanEnter) DecorateMode.Instance.Enter();
        }
    }
}
