using System.Linq;
using Hearthdelve.Core.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Hearthdelve.UI.Screens
{
    /// <summary>
    /// Keeps a menu usable on a controller and the keys (the owner's 4i-C note: at night, with only "sleep" and "decorate" left,
    /// the stick and d-pad did nothing). uGUI moves the selection only from something already selected; a mouse click on empty
    /// space, a screen opening while something else held the focus, or a selected button hidden or greyed out leaves nothing
    /// to move from. So when the menus' controls are the only ones live, and the stick, the d-pad or the arrows (or Submit) are
    /// used with nothing usable selected, the topmost menu's first usable control is selected; that first push only lands the
    /// focus, the next moves it. It never acts on the mouse, and never during play (any gameplay controls live). Made at start-up;
    /// no scene holds it.
    /// </summary>
    public sealed class MenuFocus : MonoBehaviour
    {
        static MenuFocus s_Instance;

        /// <summary>How many times the focus was given back (tests).</summary>
        public static int Restored { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Instance = null;
            Restored = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (s_Instance != null) return;
            var go = new GameObject("MenuFocus");
            DontDestroyOnLoad(go);
            s_Instance = go.AddComponent<MenuFocus>();
        }

        static readonly string[] k_Gameplay = { InputMaps.Tavern, InputMaps.Dungeon, InputMaps.Decorate, InputMaps.Minigame };

        static bool MapOn(string map) => InputSystem.actions != null && InputSystem.actions.FindActionMap(map)?.enabled == true;

        /// <summary>Only the menus' controls are live (a phase screen, a panel, a pause), never on foot or at a station.</summary>
        static bool InMenus => MapOn(InputMaps.UI) && !k_Gameplay.Any(MapOn);

        /// <summary>The stick, the d-pad, the arrows or Submit, this frame (the mouse never counts).</summary>
        static bool Asked()
        {
            InputAction navigate = InputMaps.Find(InputMaps.UI, UIActions.Navigate);
            InputAction submit = InputMaps.Find(InputMaps.UI, UIActions.Submit);
            return (navigate != null && navigate.WasPerformedThisFrame()) || (submit != null && submit.WasPressedThisFrame());
        }

        void Update()
        {
            EventSystem events = EventSystem.current;
            if (events == null || !InMenus || !Asked()) return;
            if (Usable(events.currentSelectedGameObject)) return;
            Selectable first = TopmostFirst();
            if (first == null) return;
            events.SetSelectedGameObject(first.gameObject);
            Restored++;
        }

        /// <summary>A selection that can still be used: shown and interactable.</summary>
        public static bool Usable(GameObject selected) =>
            selected != null && selected.activeInHierarchy && selected.TryGetComponent(out Selectable s) && s.enabled && s.IsInteractable();

        /// <summary>
        /// The menu on top (the highest-sorted canvas with anything usable on it) and its first control: the highest on screen,
        /// then the leftmost.
        /// </summary>
        public static Selectable TopmostFirst()
        {
            Selectable best = null;
            int bestOrder = int.MinValue;
            Vector3 bestAt = default;
            foreach (Selectable s in Selectable.allSelectablesArray)
            {
                if (s == null || !Usable(s.gameObject)) continue;
                Canvas canvas = s.GetComponentInParent<Canvas>();
                if (canvas == null || !canvas.isActiveAndEnabled) continue;
                int order = canvas.rootCanvas.sortingOrder;
                Vector3 at = s.transform.position;
                bool better = best == null || order > bestOrder
                              || (order == bestOrder && (at.y > bestAt.y + 0.01f || (Mathf.Abs(at.y - bestAt.y) <= 0.01f && at.x < bestAt.x)));
                if (!better) continue;
                best = s;
                bestOrder = order;
                bestAt = at;
            }
            return best;
        }
    }
}
