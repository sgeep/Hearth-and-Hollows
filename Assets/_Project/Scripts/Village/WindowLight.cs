using Hearthdelve.Shared.Characters;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Surface;
using Hearthdelve.Shared.Village;
using Hearthdelve.Tavern.Scene;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Hearthdelve.Village
{
    /// <summary>
    /// A small light at Ogrin's window on some evenings (4h Checkpoint D: foreshadowing only). After five, on a seeded evening
    /// (<see cref="DayRule.GlimmerEvening"/>, only once Gimp has come up: the Hollows are known to have neighbours by then), while
    /// Ogrin is in: a pale mote drifting at the glass, with a faint glow, fading in and out. Nothing names it, nothing explains it;
    /// Ogrin calls it "my light" if asked that evening. Presentation only, nothing saved.
    /// </summary>
    public sealed class WindowLight : MonoBehaviour
    {
        [SerializeField] SpriteRenderer m_Mote;
        [SerializeField] Sprite[] m_Frames;
        [SerializeField] Light2D m_Glow;
        [SerializeField, Min(0.01f)] float m_FrameSeconds = 0.1f;
        [SerializeField] Color m_Tint = new(0.82f, 0.92f, 1f, 0.9f);
        [SerializeField, Min(0.1f)] float m_FadeSeconds = 1.2f;

        Vector3 m_Home;
        float m_Shown;
        float m_Time;

        /// <summary>Showing now (tests).</summary>
        public bool Lit => m_Shown > 0.01f;

        public void Configure(SpriteRenderer mote, Sprite[] frames, Light2D glow)
        {
            m_Mote = mote;
            m_Frames = frames;
            m_Glow = glow;
        }

        void Awake()
        {
            m_Home = m_Mote != null ? m_Mote.transform.localPosition : Vector3.zero;
            Apply();
        }

        /// <summary>Whether tonight's light is at the window now.</summary>
        public static bool Wanted()
        {
            GameFlow flow = GameFlow.Instance;
            TavernDirector director = TavernDirector.Instance;
            if (flow == null || !flow.InGame || director == null || director.Phase != TavernPhase.Daytime) return false;
            if (SurfaceTime.Band != SurfaceBand.Evening) return false;
            ScheduleWorld? world = VillageLife.World();
            if (world == null || !world.Value.BeatSeen(CommunityRules.GimpIntro) || !VillageDays.Holds(DayRule.GlimmerEvening, world.Value)) return false;
            Villager ogrin = Villager.Find(CharacterIds.Ogrin);
            return ogrin != null && ogrin.Indoors;
        }

        void Update()
        {
            float target = Wanted() ? 1f : 0f;
            m_Shown = Mathf.MoveTowards(m_Shown, target, Time.deltaTime / m_FadeSeconds);
            m_Time += Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            if (m_Mote == null) return;
            bool on = m_Shown > 0.01f;
            m_Mote.enabled = on;
            if (m_Glow != null) m_Glow.enabled = on;
            if (!on) return;
            if (m_Frames != null && m_Frames.Length > 0) m_Mote.sprite = m_Frames[(int)(m_Time / m_FrameSeconds) % m_Frames.Length];
            // A slow loop at the glass, never still, never going far.
            m_Mote.transform.localPosition = m_Home + new Vector3(Mathf.Sin(m_Time * 0.9f) * 0.3f, Mathf.Sin(m_Time * 1.7f) * 0.15f, 0f);
            Color c = m_Tint;
            c.a *= m_Shown;
            m_Mote.color = c;
            if (m_Glow != null) m_Glow.intensity = 0.7f * m_Shown;
        }
    }
}
