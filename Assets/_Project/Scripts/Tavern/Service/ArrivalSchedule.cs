using System.Collections.Generic;
using Hearthdelve.Core.Random;
using Hearthdelve.Tavern.Customers;

namespace Hearthdelve.Tavern.Service
{
    /// <summary>When the next customer walks in, and what type they are. Pure logic.</summary>
    public sealed class ArrivalSchedule
    {
        readonly ServiceSettings m_Settings;
        readonly IReadOnlyList<CustomerProfile> m_Profiles;
        readonly IRandom m_Random;
        float m_UntilNext;

        public ArrivalSchedule(ServiceSettings settings, IReadOnlyList<CustomerProfile> profiles, IRandom random, float firstArrival = 1.5f)
        {
            m_Settings = settings;
            m_Profiles = profiles;
            m_Random = random;
            m_UntilNext = firstArrival;
        }

        /// <summary>Returns a profile when someone should arrive this tick (and the door is open), else null.</summary>
        public CustomerProfile Tick(float deltaTime, bool canAdmit)
        {
            m_UntilNext -= deltaTime;
            if (m_UntilNext > 0f || !canAdmit) return null;
            m_UntilNext = m_Settings.minArrivalGap + m_Random.Value() * (m_Settings.maxArrivalGap - m_Settings.minArrivalGap);
            return PickProfile();
        }

        public CustomerProfile PickProfile()
        {
            if (m_Profiles == null || m_Profiles.Count == 0) return null;
            float total = 0f;
            foreach (var p in m_Profiles) total += p != null ? p.spawnWeight : 0f;
            float pick = m_Random.Value() * total;
            foreach (var p in m_Profiles)
            {
                if (p == null) continue;
                pick -= p.spawnWeight;
                if (pick < 0f) return p;
            }
            return m_Profiles[m_Profiles.Count - 1];
        }
    }
}
