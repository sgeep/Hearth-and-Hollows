using System;

namespace Hearthdelve.Shared.Run
{
    /// <summary>
    /// What the current delve has found outside the satchel (4d step 3): so far, unbanked run Gold. It's secured by
    /// extracting and lost by dying; Gold already banked is never at risk. Pure logic.
    /// <para>
    /// Extension point: persistent finds that don't take satchel space (furnishing discoveries, quest objects) join here,
    /// with the same rule decided for each (GDD §6.6), and travel home in the <c>DelveReport</c>.
    /// </para>
    /// </summary>
    public sealed class RunLoot
    {
        readonly System.Collections.Generic.List<string> m_Bosses = new();

        /// <summary>Bosses defeated this delve, by stable id (4e): recorded in the save whatever the delve's end.</summary>
        public System.Collections.Generic.IReadOnlyList<string> BossesDefeated => m_Bosses;

        public void AddBossDefeated(string bossId)
        {
            if (!string.IsNullOrEmpty(bossId) && !m_Bosses.Contains(bossId)) m_Bosses.Add(bossId);
        }

        public int Gold { get; private set; }

        /// <summary>Raised whenever the run's Gold changes, with the new total.</summary>
        public event Action<int> GoldChanged;

        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            Gold += amount;
            GoldChanged?.Invoke(Gold);
        }
    }
}
