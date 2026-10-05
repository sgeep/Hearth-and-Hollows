using System;
using Hearthdelve.Shared.Ingredients;
using UnityEngine;

namespace Hearthdelve.Dungeon.Rooms
{
    /// <summary>How one floor of a run is shaped and how hard it is (4d). Tuning, not final balance.</summary>
    [Serializable]
    public sealed class FloorTuning
    {
        [Tooltip("Fights on the way through the floor (the start room, the rope room and the hole room aren't fights).")]
        [Range(1, 8)] public int minFights = 3;
        [Range(1, 8)] public int maxFights = 4;
        [Tooltip("At most this many rooms side by side at one step (2 = a choice of two).")]
        [Range(1, 3)] public int maxWidth = 2;
        [Tooltip("Chance that a step offers a choice of rooms. Every floor has at least one choice.")]
        [Range(0f, 1f)] public float branchChance = 0.6f;
        [Tooltip("Chance that two side-by-side routes cross over at a step.")]
        [Range(0f, 1f)] public float crossChance = 0.35f;
        [Tooltip("Chance of an extra, early rope out partway through the floor (every floor already ends with one).")]
        [Range(0f, 1f)] public float earlyExtractionChance = 0.3f;
        [Tooltip("Enemies per fight.")]
        [Range(0, 12)] public int minEnemies = 2;
        [Range(0, 12)] public int maxEnemies = 3;
        [Tooltip("How often each enemy is chosen (bats need a perch; with none free, another kind is chosen).")]
        [Min(0f)] public float slimeWeight = 1f;
        [Min(0f)] public float batWeight = 0.3f;
        [Min(0f)] public float spiderWeight = 0.2f;

        [Header("Room rewards (4d step 3)")]
        [Tooltip("How often a fight's reward is run Gold.")]
        [Min(0f)] public float goldWeight = 1f;
        [Tooltip("How often a fight's reward is a dungeon ingredient.")]
        [Min(0f)] public float ingredientWeight = 1f;
        [Min(0f), Tooltip("How likely a fight is to give a run power, against Gold and ingredients (step 4).")]
        public float powerWeight = 0.6f;
        [Min(0)] public int minGold = 10;
        [Min(0)] public int maxGold = 18;
        [Tooltip("Parts in an ingredient reward.")]
        [Range(1, 3)] public int minParts = 1;
        [Range(1, 3)] public int maxParts = 2;
        [Tooltip("Chance an ingredient reward is fine quality rather than standard.")]
        [Range(0f, 1f)] public float fineChance = 0.2f;

        public float Weight(EnemyKind kind) => kind switch
        {
            EnemyKind.Slime => slimeWeight,
            EnemyKind.Bat => batWeight,
            _ => spiderWeight,
        };
    }

    /// <summary>
    /// TEMPORARY (4d): the stand-in fight in the boss arena, so a full run can end. The Biome 1 boss replaces it in 4e;
    /// the arena layout and the run structure stay.
    /// </summary>
    [Serializable]
    public sealed class ArenaPlaceholder
    {
        [Min(0)] public int slimes = 4;
        [Min(0)] public int bats = 2;
        [Min(0)] public int spiders = 2;
    }

    /// <summary>A dungeon ingredient a room can give, how often, and from which floor down.</summary>
    [Serializable]
    public sealed class IngredientRewardOption
    {
        public IngredientDefinition ingredient;
        [Min(0f)] public float weight = 1f;
        [Range(1, 3)] public int fromFloor = 1;
    }

    /// <summary>The run's shape: three floors (top to bottom), the arena's stand-in fight, and what rooms can give.</summary>
    [Serializable]
    public sealed class RunTuning
    {
        public FloorTuning[] floors = Defaults();
        [Tooltip("Dungeon ingredients rooms can give: things the surface can't (GDD §4.8, §5.5).")]
        public IngredientRewardOption[] ingredientRewards = Array.Empty<IngredientRewardOption>();
        [Tooltip("The run powers a power room can offer (step 4). Empty: no power rooms.")]
        public Hearthdelve.Shared.Run.RunPowerDefinition[] powers = Array.Empty<Hearthdelve.Shared.Run.RunPowerDefinition>();
        [Tooltip("TEMPORARY: the arena's stand-in fight until the 4e boss.")]
        public ArenaPlaceholder arenaPlaceholder = new();
        [Tooltip("The Cellars' boss (4e): the arena's encounter. Without one, the stand-in fight above.")]
        public Hearthdelve.Dungeon.Bosses.BossDefinition boss;
        [Tooltip("The campfire lit in the room before the boss's arena (4e playtest).")]
        public CampfireSettings campfire = new();
        [Tooltip("A smaller campfire by each floor's hole down, for delvers going deeper (4e playtest).")]
        public CampfireSettings floorCampfire = new() { restoreFraction = 0.25f, restoreSeconds = 1.5f };

        public const int Floors = 3;

        public FloorTuning Floor(int index) => floors != null && floors.Length > 0 ? floors[Mathf.Clamp(index, 0, floors.Length - 1)] : new FloorTuning();

        /// <summary>Deeper floors: more enemies, more mixed groups, more Gold and better parts.</summary>
        public static FloorTuning[] Defaults() => new[]
        {
            new FloorTuning { minEnemies = 2, maxEnemies = 3, slimeWeight = 1f, batWeight = 0.25f, spiderWeight = 0.1f, minGold = 10, maxGold = 18, minParts = 1, maxParts = 2, fineChance = 0.2f },
            new FloorTuning { minEnemies = 3, maxEnemies = 4, slimeWeight = 1f, batWeight = 0.4f, spiderWeight = 0.35f, minGold = 18, maxGold = 30, minParts = 1, maxParts = 2, fineChance = 0.45f },
            new FloorTuning { minEnemies = 4, maxEnemies = 5, slimeWeight = 0.8f, batWeight = 0.5f, spiderWeight = 0.6f, minGold = 30, maxGold = 45, minParts = 2, maxParts = 3, fineChance = 0.7f },
        };
    }

    /// <summary>What the generator needs to know about an authored room.</summary>
    public readonly struct RoomCatalogEntry
    {
        public readonly string Id;
        public readonly RoomKind Kind;
        public readonly int Exits;
        public readonly int GroundSpawns;
        public readonly int PerchSpawns;

        public RoomCatalogEntry(string id, RoomKind kind, int exits, int groundSpawns, int perchSpawns)
        {
            Id = id;
            Kind = kind;
            Exits = exits;
            GroundSpawns = groundSpawns;
            PerchSpawns = perchSpawns;
        }
    }
}
