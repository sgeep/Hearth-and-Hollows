using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelCrushers.QuestMachine
{

    [Serializable]
    public class StaticQuestJsonSaveData
    {

        public int version;
        public QuestState state;
        public TagDictionary tags;
        public List<string> tagValues;
        public string questGiverID;
        public int timesAccepted;
        public float cooldownRemaining;
        public bool showInTrackHUD;
        public ConditionSetJsonSaveData autostart;
        public ConditionSetJsonSaveData offer;
        public List<QuestCounterJsonSaveData> counters;
        public List<QuestNodeJsonSaveData> nodes;
        public List<string> indicatorIDs;
        public List<QuestIndicatorState> indicatorStates;

    }

    [Serializable]
    public class ConditionSetJsonSaveData
    {
        public int numTrue;
        public List<bool> alreadyTrue;
    }

    [Serializable]
    public class QuestCounterJsonSaveData
    { 
        public string name;
        public int value;
        public bool saveMinMaxValues = false;
        public int minValue;
        public int maxValue;
    }

    [Serializable]
    public class QuestNodeJsonSaveData
    {
        public string id;
        public QuestNodeState state;
        public ConditionSetJsonSaveData conditionSet;
        public TagDictionary tags;
    }

}
