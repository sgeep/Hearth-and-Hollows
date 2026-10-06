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

        readonly System.Collections.Generic.List<string> m_Curios = new();

        /// <summary>
        /// Furnishings found on this delve (4f Checkpoint C, D9), by furniture id, in the order found: run-bound like the run's
        /// Gold, owned for good only if the delve ends with extraction, lost on death, never in the Lockbox.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<string> Curios => m_Curios;
        /// <summary>How many of <see cref="Curios"/> enemies dropped (the delve's cap counts these).</summary>
        public int CuriosDropped { get; private set; }

        /// <summary>Raised when a curio is picked up, with its id.</summary>
        public event Action<string> CurioAdded;

        public void AddCurio(string furnitureId, bool fromEnemy = false)
        {
            if (string.IsNullOrEmpty(furnitureId)) return;
            m_Curios.Add(furnitureId);
            if (fromEnemy) CuriosDropped++;
            CurioAdded?.Invoke(furnitureId);
        }

        /// <summary>An enemy's curio is on the floor (counted when it drops, so the cap holds even if it's left lying).</summary>
        public void NoteEnemyDrop() => CuriosDropped++;

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
