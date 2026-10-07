using System;
using System.Collections.Generic;
using Hearthdelve.Shared.Story;

namespace Hearthdelve.Shared.Save
{
    // JSON shapes (JsonUtility). Content is stored by stable id; enums by name so reordering
    // them never corrupts a save. Change these only together with a version bump + migration.

    /// <summary>
    /// Current save format (version 10: 4h Checkpoint B's world seed, the surface day's minute and Vigor, and the garden; 9: 4g
    /// Checkpoint B's opening and keeper; 8: 4g's story; 7: 4f Checkpoint C's new pieces and trophy homecoming; 6: 4f Checkpoint B's
    /// looks and finishes: each piece's colourway and palette, each area's floor and wall finish, owned finishes and the catalogue
    /// tier last announced; 5 moved 4f's barrels and glasses, 4 added furniture, 3 the bosses defeated).
    /// </summary>
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
        /// <summary>Version 8 (4g): the story.</summary>
        public StorySaveData story = new();
        /// <summary>Version 9 (4g Checkpoint B): quest objects (by id) and where each is.</summary>
        public List<QuestObjectData> questObjects = new();
        /// <summary>Version 10: the world's seed (one per game).</summary>
        public WorldData world = new();
        /// <summary>Version 10: the surface day in progress (meaningful in the daytime).</summary>
        public SurfaceData surface = new();
        /// <summary>Version 10: the garden's beds.</summary>
        public GardenSaveData garden = new();
    }

    [Serializable]
    public sealed class WorldData
    {
        public int seed;
    }

    [Serializable]
    public sealed class SurfaceData
    {
        /// <summary>The surface clock's minute of the day (0: not recorded, the morning).</summary>
        public int minute;
        public int vigorSpent;
    }

    [Serializable]
    public sealed class GardenSaveData
    {
        public bool initialized;
        public List<BedData> beds = new();
    }

    /// <summary>One bed, by stable id. Its looks aren't saved: they follow from the crop and its growth.</summary>
    [Serializable]
    public sealed class BedData
    {
        public string id;
        public string crop = string.Empty;
        public int plantedDay;
        public int grown;
        public int tendedDays;
        public int lastTendedDay;
        public int lastGrownDay;
    }

    [Serializable]
    public sealed class QuestObjectData
    {
        public string id;
        public string status;
    }

    /// <summary>
    /// Version 8 (4g Checkpoint A): the story. <see cref="openingComplete"/> is explicit: a new game starts false and plays the Act I
    /// opening; a save from before 4g is migrated with it true, so Continue never sends an old game through the opening or
    /// character creation. <see cref="dialogue"/> and <see cref="quests"/> are what the Dialogue System and Quest Machine adapters
    /// recorded; relationships are Hearth &amp; Hollows' own shape (<see cref="RelationshipData"/>).
    /// </summary>
    [Serializable]
    public sealed class StorySaveData
    {
        public bool openingComplete;
        /// <summary>Version 9: where the Act I opening is (OpeningStage by name).</summary>
        public string openingStage = string.Empty;
        /// <summary>Version 9: the keeper was made at character creation (or is a legacy keeper).</summary>
        public bool creationComplete;
        /// <summary>Version 9: onboarding prompts already shown.</summary>
        public List<string> seenHints = new();
        public PlayerProfile player = new();
        public string dialogue = string.Empty;
        public string quests = string.Empty;
        public RelationshipData relationships = new();
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
        /// <summary>Version 6: finishes owned (D5).</summary>
        public List<string> finishes = new();
        /// <summary>Version 6: the highest catalogue tier announced (D14).</summary>
        public int tierAnnounced;
        /// <summary>Version 7 (4f Checkpoint C): pieces found or earned and not yet seen in storage (the "new" badge).</summary>
        public List<string> newPieces = new();
        /// <summary>Version 7: a boss trophy waiting for its homecoming in Decorate Mode (empty: none).</summary>
        public string homecoming;
        /// <summary>
        /// Set only while migrating a version 6 save: starting pieces it never had (the Butcher Block) are given once, into
        /// storage, marked new. Never written by a save.
        /// </summary>
        [NonSerialized] public bool grantNewStarters;
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
        /// <summary>Version 6: the area's floor and wall finishes (empty: as first built).</summary>
        public string floor;
        public string wall;
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
        public int anchor;
        /// <summary>Version 6: its colourway and palette choices (empty: as drawn).</summary>
        public string variant;
        public string palette;
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
