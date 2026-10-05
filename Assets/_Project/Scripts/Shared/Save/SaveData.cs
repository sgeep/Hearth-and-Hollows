using System;
using System.Collections.Generic;

namespace Hearthdelve.Shared.Save
{
    // JSON shapes (JsonUtility). Content is stored by stable id; enums by name so reordering
    // them never corrupts a save. Change these only together with a version bump + migration.

    /// <summary>Current save format (version 3: version 2 plus the bosses defeated, 4e).</summary>
    [Serializable]
    public sealed class SaveData
    {
        public int version;
        public int day;
        public string phase;
        public int gold;
        public int renown;
        public List<StackData> storeroom = new();
        public List<UpgradeData> upgrades = new();
        public MealData meal = new();
        /// <summary>Version 3 (4e): each boss defeated, by stable id, and how many times.</summary>
        public List<BossClearData> bosses = new();
    }

    [Serializable]
    public sealed class BossClearData
    {
        public string id;
        public int clears;
    }

    [Serializable]
    public sealed class StackData
    {
        public string ingredient;
        public string quality;
        public string prep;
        public int count;
        public float freshness;
    }

    [Serializable]
    public sealed class UpgradeData
    {
        public string id;
        public int level;
    }

    [Serializable]
    public sealed class MealData
    {
        public string kind;
        public float amount;
        public string recipe;
    }

    /// <summary>
    /// Version 1: the first, minimal format (day, gold, and storeroom parts without prep state
    /// or freshness). Only read, to migrate old saves.
    /// </summary>
    [Serializable]
    public sealed class SaveDataV1
    {
        public int version;
        public int day;
        public int gold;
        public List<StackDataV1> storeroom = new();
    }

    [Serializable]
    public sealed class StackDataV1
    {
        public string ingredient;
        public int quality;
        public int count;
    }

    [Serializable]
    sealed class VersionProbe
    {
        public int version;
    }
}
