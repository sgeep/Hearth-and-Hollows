using UnityEngine;

namespace Hearthdelve.Tavern.Scene
{
    /// <summary>
    /// Where the tavern scene's camera holds (decision 1, 4c: each room fits one screen, so the camera holds still on it).
    /// With the guest room (4f step 6) there's more than one room: the camera holds on whichever area is shown.
    /// </summary>
    public static class TavernView
    {
        public const string CameraName = "Tavern Camera";

        static Transform s_Camera;

        public static Transform Camera
        {
            get
            {
                if (s_Camera == null)
                {
                    GameObject found = GameObject.Find(CameraName);
                    s_Camera = found != null ? found.transform : null;
                }
                return s_Camera;
            }
        }

        /// <summary>Moves the camera to hold on <paramref name="area"/> (nothing happens without the tavern camera or an area).</summary>
        public static void Show(PropertyArea area)
        {
            if (area == null || Camera == null) return;
            Vector3 at = Camera.position;
            Vector2 point = area.CameraPoint;
            if (point == Vector2.zero) return;
            Camera.position = new Vector3(point.x, point.y, at.z);
            UnityEngine.Camera main = UnityEngine.Camera.main;
            if (main != null) main.transform.position = new Vector3(point.x, point.y, main.transform.position.z);
        }
    }
}
