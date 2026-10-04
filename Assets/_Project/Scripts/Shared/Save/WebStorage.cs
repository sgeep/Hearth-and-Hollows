#if UNITY_WEBGL && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Hearthdelve.Shared.Save
{
    /// <summary>
    /// On the web, <c>Application.persistentDataPath</c> is an in-memory file system backed by the browser's IndexedDB,
    /// and file writes reach IndexedDB only when it is synced. Without this, every web save was lost on reload (found in
    /// the 4c web smoke test). <see cref="SaveStore"/> calls <see cref="Flush"/> after each write and delete; elsewhere
    /// it does nothing. The JavaScript side is <c>Plugins/WebGL/HearthdelveStorage.jslib</c>.
    /// </summary>
    public static class WebStorage
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        static extern void HearthdelveSyncFiles();

        public static void Flush() => HearthdelveSyncFiles();
#else
        public static void Flush() { }
#endif
    }
}
