using System.Collections.Generic;
using UnityEngine;

namespace Hearthdelve.Village
{
    /// <summary>
    /// A window that's lit while someone is home behind it (the owner's request, 2026-10-08): Ogrin's, at Grim's cottage, is warm
    /// while he's in bed and dark when he's out. The lit glass is a drawing laid over the building's own window (a child of the
    /// building, so it moves with it); the schedule's window anchor says when (<see cref="ScheduleAnchor.SetOccupied"/>), by the
    /// anchor's id. Order doesn't matter: whichever of the two wakes first, the window shows the anchor's last word.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class LitWindow : MonoBehaviour
    {
        [SerializeField, Tooltip("The schedule anchor behind this window (Ogrin's: \"ogrin.window\").")] string m_Anchor;

        static readonly Dictionary<string, bool> s_Lit = new();
        static readonly List<LitWindow> s_All = new();

        SpriteRenderer m_Glass;

        public string Anchor => m_Anchor;
        public bool IsLit => m_Glass != null && m_Glass.enabled;

        public void Configure(string anchor) => m_Anchor = anchor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Lit.Clear();
            s_All.Clear();
        }

        /// <summary>Someone is (or isn't) behind the window of this anchor.</summary>
        public static void Set(string anchor, bool lit)
        {
            if (string.IsNullOrEmpty(anchor)) return;
            s_Lit[anchor] = lit;
            foreach (LitWindow w in s_All)
                if (w.m_Anchor == anchor) w.Show(lit);
        }

        /// <summary>Whether the window of this anchor is lit now (tests).</summary>
        public static bool IsLitAt(string anchor) => s_All.Exists(w => w.m_Anchor == anchor && w.IsLit);

        void Awake() => m_Glass = GetComponent<SpriteRenderer>();

        void OnEnable()
        {
            s_All.Add(this);
            Show(s_Lit.TryGetValue(m_Anchor ?? string.Empty, out bool lit) && lit);
        }

        void OnDisable() => s_All.Remove(this);

        void Show(bool lit)
        {
            if (m_Glass != null && m_Glass.enabled != lit) m_Glass.enabled = lit;
        }
    }
}
