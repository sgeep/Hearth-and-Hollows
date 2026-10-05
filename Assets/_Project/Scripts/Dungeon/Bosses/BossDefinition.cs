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

        [Header("Feeding (the Larder Troll, 4e step 2): it eats parts left on the floor to recover")]
        public FeedingSettings feeding = new();

        [Header("Frenzy: the second phase")]
        public FrenzySettings frenzy = new();
    }

    /// <summary>A boss that eats the parts on the floor (4e step 2). The player can take them first, or hit it hard to spoil the meal.</summary>
    [System.Serializable]
    public sealed class FeedingSettings
    {
        public bool enabled = true;
        [Tooltip("What its slams shake loose: existing Cellars parts (they're ordinary parts: the player can take them home).")]
        public Hearthdelve.Shared.Ingredients.IngredientDefinition[] scraps = System.Array.Empty<Hearthdelve.Shared.Ingredients.IngredientDefinition>();
        [Min(1f), Tooltip("A slam shakes parts loose at most this often, in seconds.")]
        public float dropEverySeconds = 14f;
        [Min(1)] public int partsPerDrop = 2;
        [Min(1), Tooltip("No more parts are shaken loose while this many lie on the floor.")]
        public int maxOnFloor = 4;
        [Min(0.1f), Tooltip("Seconds it eats one part: the window to spoil the meal.")]
        public float eatSeconds = 1.5f;
        [Range(0f, 0.5f), Tooltip("Health back per part eaten, as a share of its maximum.")]
        public float healFraction = 0.08f;
        [Min(1f), Tooltip("Damage taken while eating that spoils the meal: the part is ruined and nothing is healed.")]
        public float spoilDamage = 40f;
        [Min(0f), Tooltip("Seconds after a meal before it goes for another part.")]
        public float eatCooldown = 2.5f;
    }

    /// <summary>A boss's second phase: below a share of its health it roars and fights harder (4e step 2).</summary>
    [System.Serializable]
    public sealed class FrenzySettings
    {
        public bool enabled = true;
        [Range(0.05f, 0.95f), Tooltip("Health share at which the frenzy starts.")]
        public float atHealth = 0.5f;
        [Min(0f), Tooltip("Seconds of roaring (it can't be hurt, and doesn't act).")]
        public float roarSeconds = 1.2f;
        [Min(1f), Tooltip("Its walking and charging speed in the frenzy.")]
        public float speedMultiplier = 1.2f;
        [Tooltip("A slam is followed at once by a second.")]
        public bool chainSlams = true;
        [Tooltip("Each charge spills a part where it started.")]
        public bool chargesSpillScraps = true;
        public Color tint = new(1f, 0.72f, 0.66f);
    }
}
