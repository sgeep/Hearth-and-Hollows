using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Hearthdelve.Shared.Audio
{
    /// <summary>
    /// Boot's fallback ear (2026-10-08). The content scenes' cameras carry the AudioListener, and <see cref="Game.GameFlow"/>
    /// unloads one set of scenes before it loads the next, so between them nothing was listening: Unity warned, and the music
    /// (which lives in Boot) went unheard. This listener is on exactly when no other enabled listener is loaded, sitting on the
    /// main camera if there is one, and off the moment a scene brings its own (so there's never more than one).
    /// </summary>
    [RequireComponent(typeof(AudioListener))]
    public sealed class ListenerKeeper : MonoBehaviour
    {
        AudioListener m_Own;
        readonly List<AudioListener> m_Others = new();

        void Awake()
        {
            m_Own = GetComponent<AudioListener>();
            Refresh();
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnLoaded;
            SceneManager.sceneUnloaded += OnUnloaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneUnloaded -= OnUnloaded;
        }

        void OnLoaded(Scene scene, LoadSceneMode mode) => Refresh();

        void OnUnloaded(Scene scene) => Refresh();

        /// <summary>Finds the scenes' listeners again (a scene came or went) and settles which one hears.</summary>
        void Refresh()
        {
            m_Others.Clear();
            foreach (AudioListener l in FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (l != m_Own) m_Others.Add(l);
            Settle();
        }

        void LateUpdate() => Settle();

        /// <summary>True when this listener is the one hearing (no scene has an enabled listener of its own).</summary>
        public bool Hearing => m_Own != null && m_Own.enabled;

        void Settle()
        {
            if (m_Own == null) return;
            bool another = false;
            foreach (AudioListener l in m_Others)
                if (l != null && l.isActiveAndEnabled)
                {
                    another = true;
                    break;
                }
            if (m_Own.enabled == another) m_Own.enabled = !another;
            if (!another)
            {
                Camera cam = Camera.main;
                if (cam != null) transform.position = cam.transform.position;
            }
        }
    }
}
