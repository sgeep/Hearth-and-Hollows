using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Customization;
using UnityEngine;

namespace Hearthdelve.Shared.Run
{
    /// <summary>One furnishing a region of the Hollows can give, who gives it, and how often.</summary>
    [Serializable]
    public sealed class CurioEntry
    {
        public FurnitureDefinition piece;
        [Min(0f)] public float weight = 1f;
        [Tooltip("Enemies (by EnemyDefinition id) that can drop it. Empty: no enemy drops it.")]
        public string[] droppedBy = Array.Empty<string>();
        [Tooltip("Can a room's curio reward (RewardKind.Curio) be this piece?")]
        public bool inRoomCaches = true;

        public bool From(string origin) =>
            origin == CurioRules.RoomOrigin ? inRoomCaches : droppedBy != null && Array.IndexOf(droppedBy, origin) >= 0;
    }

    /// <summary>
    /// A region's furnishing discoveries (4f Checkpoint C; plan §14, D8, D9): what can be found, rare enemy drops, and the
    /// duplicate rules. Every value here is tuning, not law.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Curio Pool", fileName = "CurioPool_")]
    public sealed class CurioPool : ScriptableObject
    {
        public List<CurioEntry> entries = new();
        [Range(0f, 1f), Tooltip("Chance a kill of an enemy that can drop something drops a curio.")]
        public float enemyDropChance = 0.04f;
        [Min(0), Tooltip("At most this many curios dropped by enemies in one delve (room rewards don't count).")]
        public int maxEnemyDropsPerDelve = 2;
        [Min(1f), Tooltip("D8: weight multiplier for pieces not owned yet, so collections fill before copies pile up.")]
        public float firstCopyWeight = 3f;
        [Tooltip("When the pool has nothing left to give, the reward becomes this much run gold instead (no new currency).")]
        [Min(0)] public int fallbackGoldMin = 25;
        [Min(0)] public int fallbackGoldMax = 40;
    }

    /// <summary>The roll (pure, seeded through <see cref="Core.Random.IRandom"/>).</summary>
    public static class CurioRules
    {
        /// <summary>The origin of a room's curio reward, as opposed to an enemy's id.</summary>
        public const string RoomOrigin = "room";

        /// <summary>
        /// Which piece a curio is, or null when the pool has nothing left for this origin (the caller gives run gold).
        /// Unique pieces already owned, or already found on this delve, are out of the roll (D8); pieces not owned yet are
        /// weighted up by <paramref name="firstCopyWeight"/>.
        /// </summary>
        public static FurnitureDefinition Roll(IReadOnlyList<CurioEntry> entries, string origin, Func<string, int> owned,
            IReadOnlyCollection<string> foundThisDelve, Core.Random.IRandom random, float firstCopyWeight)
        {
            if (entries == null || random == null) return null;
            var options = new List<(FurnitureDefinition piece, float weight)>();
            float total = 0f;
            foreach (CurioEntry entry in entries)
            {
                float weight = Weight(entry, origin, owned, foundThisDelve, firstCopyWeight);
                if (weight <= 0f) continue;
                options.Add((entry.piece, weight));
                total += weight;
            }
            if (total <= 0f) return null;
            float roll = random.Value() * total;
            foreach (var (piece, weight) in options)
            {
                if (roll < weight) return piece;
                roll -= weight;
            }
            return options[^1].piece;
        }

        /// <summary>An entry's weight in the roll (0: it can't be given).</summary>
        public static float Weight(CurioEntry entry, string origin, Func<string, int> owned, IReadOnlyCollection<string> foundThisDelve, float firstCopyWeight)
        {
            if (entry?.piece == null || entry.weight <= 0f || !entry.From(origin)) return 0f;
            string id = entry.piece.id;
            int have = owned != null ? owned(id) : 0;
            bool foundNow = foundThisDelve != null && Contains(foundThisDelve, id);
            if (entry.piece.unique && (have > 0 || foundNow)) return 0f;
            return have == 0 && !foundNow ? entry.weight * Math.Max(1f, firstCopyWeight) : entry.weight;
        }

        /// <summary>Does an enemy drop a curio now? The delve's cap (<see cref="CurioPool.maxEnemyDropsPerDelve"/>) comes first.</summary>
        public static bool EnemyDrops(CurioPool pool, int droppedThisDelve, float roll) =>
            pool != null && droppedThisDelve < pool.maxEnemyDropsPerDelve && roll < pool.enemyDropChance;

        public static int FallbackGold(CurioPool pool, Core.Random.IRandom random)
        {
            if (pool == null || random == null) return 0;
            return random.Range(Math.Min(pool.fallbackGoldMin, pool.fallbackGoldMax), Math.Max(pool.fallbackGoldMin, pool.fallbackGoldMax));
        }

        static bool Contains(IReadOnlyCollection<string> list, string id)
        {
            foreach (string s in list)
                if (s == id) return true;
            return false;
        }
    }
}
