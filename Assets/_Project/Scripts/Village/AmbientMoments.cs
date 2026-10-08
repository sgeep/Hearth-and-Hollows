using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Game;
using Hearthdelve.Shared.Story;
using Hearthdelve.Tavern.Scene;
using UnityEngine;

namespace Hearthdelve.Village
{
    /// <summary>One pair's overheard exchange: who, how close together, and (optionally) doing what.</summary>
    [Serializable]
    public sealed class AmbientMoment
    {
        [Tooltip("The Dialogue System conversation (Ambient/…): its graph decides what's said, in priority order.")] public string conversation;
        public string first;
        public string second;
        [Tooltip("Only while the first is doing this (empty: anything).")] public string firstDoing;
        [Tooltip("Only while the second is doing this (empty: anything).")] public string secondDoing;
        [Min(0.5f), Tooltip("Tiles apart, at most.")] public float within = 4f;
    }

    /// <summary>
    /// When the village's people talk among themselves (4h Checkpoint D): a short authored list of pairs, each heard at most once a
    /// day, only when both are about and standing close, the keeper is near enough to overhear, and nothing else is on screen; and
    /// never more often than every so many seconds across the whole surface, so Kariaston never turns into chatter. What they say
    /// is the story's (<see cref="IBarkService"/>): once-only lines mark their own variables there.
    /// </summary>
    public sealed class AmbientMoments : MonoBehaviour
    {
        [SerializeField] List<AmbientMoment> m_Moments = new();
        [SerializeField, Tooltip("The surface area these happen in (kariaston, tavern).")] string m_Area = SurfaceArea.KariastonId;
        [SerializeField, Min(1f), Tooltip("Tiles: the keeper must be this close to both to overhear.")] float m_Overhear = 12f;
        [SerializeField, Min(0f), Tooltip("Real seconds between any two exchanges anywhere on the surface.")] float m_Cooldown = 35f;
        [SerializeField, Min(0.1f)] float m_CheckEvery = 1f;

        /// <summary>Real seconds since the last exchange began, across every scene's moments (one surface, one quiet).</summary>
        static float s_Quiet = float.PositiveInfinity;
        static int s_CountedFrame = -1;
        /// <summary>Which pairs have spoken today (in memory: a reload may let one speak again, which is harmless).</summary>
        static readonly Dictionary<string, int> s_Heard = new();

        float m_UntilCheck;
        Transform m_Keeper;

        public IReadOnlyList<AmbientMoment> Moments => m_Moments;

        public void Configure(string area, List<AmbientMoment> moments, float cooldown = 35f)
        {
            m_Area = area;
            m_Moments = moments ?? new List<AmbientMoment>();
            m_Cooldown = cooldown;
        }

        /// <summary>Tests: forget the cooldown and today's pairs.</summary>
        public static void ResetForTests()
        {
            s_Quiet = float.PositiveInfinity;
            s_Heard.Clear();
        }

        void Update()
        {
            // One shared quiet, counted once a frame however many scenes have moments.
            if (s_CountedFrame != Time.frameCount)
            {
                s_CountedFrame = Time.frameCount;
                s_Quiet += Time.unscaledDeltaTime;
            }
            m_UntilCheck -= Time.unscaledDeltaTime;
            if (m_UntilCheck > 0f) return;
            m_UntilCheck = m_CheckEvery;
            TryNow();
        }

        /// <summary>Plays the first moment that can happen now; true if one began.</summary>
        public bool TryNow()
        {
            IBarkService barks = StoryServices.Barks;
            GameFlow flow = GameFlow.Instance;
            TavernDirector director = TavernDirector.Instance;
            if (barks == null || barks.IsBarking || flow == null || !flow.InGame || director == null || director.Phase != TavernPhase.Daytime) return false;
            if (s_Quiet < m_Cooldown) return false;
            SurfaceArea here = SurfaceArea.Current;
            if (here == null || here.Id != m_Area) return false;
            if (m_Keeper == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                m_Keeper = player != null ? player.transform : null;
            }
            if (m_Keeper == null) return false;
            int day = flow.State.Day;
            foreach (AmbientMoment m in m_Moments)
            {
                if (m == null || string.IsNullOrEmpty(m.conversation)) continue;
                if (s_Heard.TryGetValue(m.conversation, out int heard) && heard == day) continue;
                if (!Ready(m.first, m.firstDoing, out Transform a) || !Ready(m.second, m.secondDoing, out Transform b)) continue;
                if (Vector2.Distance(a.position, b.position) > m.within) continue;
                if (Vector2.Distance(m_Keeper.position, a.position) > m_Overhear || Vector2.Distance(m_Keeper.position, b.position) > m_Overhear) continue;
                s_Heard[m.conversation] = day;
                if (!barks.Play(m.conversation)) continue;   // nothing to say today: try another pair
                s_Quiet = 0f;
                return true;
            }
            return false;
        }

        /// <summary>About, standing still where they belong, and (if asked) doing the right thing.</summary>
        static bool Ready(string id, string doing, out Transform at)
        {
            at = Speakers.Find(id);
            if (at == null) return false;
            Villager v = Villager.Find(id);
            if (v != null && v.transform == at && (v.Walking || v.Indoors)) return false;
            return string.IsNullOrEmpty(doing) || Hearthdelve.Shared.Village.VillageLife.Doing(id) == doing;
        }
    }
}
