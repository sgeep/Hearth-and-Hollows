using UnityEngine;

namespace Hearthdelve.Dungeon.Bosses
{
    /// <summary>
    /// A boss (4e): its stable id (saves, story and 4f's trophy key on it), the prefab the arena spawns, and the
    /// encounter's rules. The fight itself (health, attacks, timings) is the prefab's <c>EnemyDefinition</c>. The name
    /// shown is the UI string <c>boss.&lt;id&gt;</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Hearthdelve/Dungeon/Boss", fileName = "Boss_")]
    public sealed class BossDefinition : ScriptableObject
    {
        [Tooltip("Stable: saved first-clear records and 4f's trophy refer to it.")]
        public string id;
        public GameObject prefab;

        [Header("The encounter")]
        [Range(0f, 1f), Tooltip("Passive Essence drain while the boss is active (0 = paused). Hits still cost Essence.")]
        public float drainMultiplierWhileActive;
        [Min(0f), Tooltip("Seconds of entrance after the gates seal before the boss acts.")]
        public float entranceSeconds = 1.6f;
    }
}
