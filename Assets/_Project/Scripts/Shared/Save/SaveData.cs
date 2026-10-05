using System;
using System.Collections.Generic;

namespace Hearthdelve.Shared.Save
{
    // JSON shapes (JsonUtility). Content is stored by stable id; enums by name so reordering
    // them never corrupts a save. Change these only together with a version bump + migration.

    /// <summary>Current save format (version 4: version 3 plus furniture, 4f; version 3 added the bosses defeated, 4e).</summary>
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
        /// <summary>Version 4 (4f): owned furniture and each area's layout.</summary>
        public FurnitureSaveData furniture = new();
    }

    /// <summary>
    /// Version 4 (4f). <see cref="initialized"/> is false in saves migrated from version 3: the game then grants the
    /// starting furniture as it loads them (the starting layout lives in content, not in the save format).
    /// </summary>
    [Serializable]
    public sealed class FurnitureSaveData
    {
        public bool initialized;
        public int nextUid = 1;
        public List<OwnedPieceData> owned = new();
        public List<AreaSaveData> areas = new();
    }

    [Serializable]
    public sealed class OwnedPieceData
    {
        public string id;
        public int count;
    }

    [Serializable]
    public sealed class AreaSaveData
    {
        public string id;
        public List<PieceData> pieces = new();
    }

    /// <summary>A placed piece: definition id, footprint cell, quarter turns, mirror, nudge in art pixels, host uid.</summary>
    [Serializable]
    public sealed class PieceData
    {
        public int uid;
        public string def;
        public int x;
        public int y;
        public int turns;
        public bool flip;
        public int nx;
        public int ny;
        public int host = -1;
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
