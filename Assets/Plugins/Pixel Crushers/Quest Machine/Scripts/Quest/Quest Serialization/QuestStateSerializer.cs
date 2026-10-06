// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

namespace PixelCrushers.QuestMachine
{

    /// <summary>
    /// Utility to serialize the minimum data necessary for design-time quests. Only saves:
    /// - Quest giver ID
    /// - Static tags
    /// - Times accepted
    /// - Time last accepted
    /// - Cooldown time remaining
    /// - Show in HUD
    /// - Quest state
    /// - Quest node states [skipped if inactive]
    /// - Counter values [skipped if inactive]
    /// - True condition count on all condition sets [skipped if inactive]
    /// - Quest indicator states [skipped if inactive]
    /// </summary>
    public static class QuestStateSerializer
    {

        /// <summary>
        /// - Version 4: Added saving of counter min & max values.
        /// - Version 3: Added counter count, node count, each conditionlist count, plus IDs.
        /// - Version 2: Added quest condition "alreadyTrue" values.
        /// - Version 1: Use for compatibility with saves made in QM version 1.2.29 or earlier.
        /// </summary>
        public static int version = 4;

        private const string VersionTagKey = "SerVer"; // Version info is stored in tags dictionary for backward compatibility.

        #region Serialize (Byte)

        /// <summary>
        /// Returns minimum save data for a design-time quest.
        /// </summary>
        /// <param name="quest">The quest to serialize.</param>
        /// <returns>A byte array containing states and counter values.</returns>
        public static byte[] Serialize(Quest quest)
        {
            if (quest == null) return null;
            using (var memoryStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(memoryStream))
                {
                    WriteQuestDataToStream(binaryWriter, quest);
                }
                return memoryStream.ToArray();
            }
        }

        private static void WriteQuestDataToStream(BinaryWriter binaryWriter, Quest quest)
        {
            if (quest == null) return;
            var state = quest.GetState();
            binaryWriter.Write((byte)state);
            quest.tagDictionary.dict[VersionTagKey] = version.ToString();
            WriteTagDictionaryToStream(binaryWriter, quest.tagDictionary, quest.name);
            binaryWriter.Write(StringField.GetStringValue(quest.questGiverID));
            binaryWriter.Write(quest.timesAccepted);
            quest.UpdateCooldown();
            binaryWriter.Write((double)quest.cooldownSecondsRemaining);
            binaryWriter.Write(quest.showInTrackHUD);
            WriteConditionSetDataToStream(binaryWriter, quest.autostartConditionSet);
            WriteConditionSetDataToStream(binaryWriter, quest.offerConditionSet);

            // Don't save the info below if waiting to start:
            if (state == QuestState.WaitingToStart && !quest.saveAllIfWaitingToStart) return;
            binaryWriter.Write((byte)quest.counterList.Count); //[version 3]
            for (int i = 0; i < quest.counterList.Count; i++)
            {
                binaryWriter.Write(quest.counterList[i].currentValue);
                binaryWriter.Write(quest.counterList[i].minValue); //[version 4]
                binaryWriter.Write(quest.counterList[i].maxValue); //[version 4]
            }
            binaryWriter.Write((byte)quest.nodeList.Count); //[version 3]
            for (int i = 0; i < quest.nodeList.Count; i++)
            {
                WriteQuestNodeDataToStream(binaryWriter, quest.nodeList[i]);
            }
            WriteQuestIndicatorsToStream(binaryWriter, quest.indicatorStates);
        }

        private static void WriteQuestNodeDataToStream(BinaryWriter binaryWriter, QuestNode node)
        {
            if (node == null) return;
            var state = node.GetState();
            binaryWriter.Write((byte)state);
            WriteConditionSetDataToStream(binaryWriter, node.conditionSet);
            WriteTagDictionaryToStream(binaryWriter, node.tagDictionary, StringField.GetStringValue(node.internalName));
        }

        private static void WriteConditionSetDataToStream(BinaryWriter binaryWriter, QuestConditionSet conditionSet)
        {
            if (conditionSet == null) return;
            binaryWriter.Write((byte)conditionSet.numTrueConditions);
            // [version 2] (alreadyTrue), [version 3]condition count:
            binaryWriter.Write((byte)conditionSet.conditionList.Count); //[version 3]
            for (int i = 0; i < conditionSet.conditionList.Count; i++)
            {
                if (conditionSet.conditionList[i] == null) continue;
                binaryWriter.Write(conditionSet.conditionList[i].alreadyTrue);
            }
        }

        private static void WriteTagDictionaryToStream(BinaryWriter binaryWriter, TagDictionary tags, string questOrNodeName)
        {
            if (tags == null) return;
            binaryWriter.Write(tags.dict.Count);
            foreach (var kvp in tags.dict)
            {
                if (string.IsNullOrEmpty(kvp.Key))
                {
                    if (Debug.isDebugBuild) Debug.LogWarning("Quest Machine: While serializing quest tags, found a tag with a blank name in " + questOrNodeName + ".");
                }
                else
                {
                    binaryWriter.Write(kvp.Key);
                    binaryWriter.Write(kvp.Value);
                }
            }
        }

        private static void WriteQuestIndicatorsToStream(BinaryWriter binaryWriter, Dictionary<string, QuestIndicatorState> indicatorRecords)
        {
            if (indicatorRecords == null) return;
            binaryWriter.Write(indicatorRecords.Count);
            foreach (var kvp in indicatorRecords)
            {
                binaryWriter.Write(kvp.Key);
                binaryWriter.Write((int)kvp.Value);
            }
        }

        #endregion

        #region Deserialize (Byte)

        /// <summary>
        /// Copies data from a byte array into an existing design-time quest.
        /// </summary>
        /// <param name="quest">The quest to receive the data.</param>
        /// <param name="bytes">A byte array generated by the Serialize method.</param>
        public static void DeserializeInto(Quest quest, byte[] bytes, bool allowThrowExceptions = false)
        {
            if (quest == null || bytes == null) return;
            using (var memoryStream = new MemoryStream(bytes))
            {
                using (var binaryReader = new BinaryReader(memoryStream))
                {
                    if (QuestMachine.allowExceptions)
                    {
                        ReadQuestDataFromStream(binaryReader, quest);
                    }
                    else
                    {
                        try
                        {
                            ReadQuestDataFromStream(binaryReader, quest);
                        }
                        catch (System.Exception e)
                        {
                            if (allowThrowExceptions) throw e;
                        }
                    }
                }
            }
        }

        private static void ReadQuestDataFromStream(BinaryReader binaryReader, Quest quest)
        {
            if (quest == null) return;
            var state = (QuestState)binaryReader.ReadByte();
            ReadTagDictionaryFromStream(binaryReader, quest.tagDictionary, quest.name);
            int savedVersion = GetSavedVersion(quest);
            quest.questGiverID.value = binaryReader.ReadString();
            quest.timesAccepted = binaryReader.ReadInt32();
            quest.cooldownSecondsRemaining = (float)binaryReader.ReadDouble();
            quest.showInTrackHUD = binaryReader.ReadBoolean();
            ReadConditionSetDataFromStream(binaryReader, quest.autostartConditionSet, savedVersion);
            ReadConditionSetDataFromStream(binaryReader, quest.offerConditionSet, savedVersion);
            if (state == QuestState.WaitingToStart && !quest.saveAllIfWaitingToStart)
            {
                quest.SetState(state, false);
                return;
            }

            // Don't load the info below if waiting to start:
            ReadCountersFromStream(binaryReader, quest, savedVersion);
            ReadQuestNodesFromStream(binaryReader, quest, savedVersion);
            ReadQuestIndicatorsFromStream(binaryReader, quest.indicatorStates);
            quest.SetRuntimeReferences();
            quest.SetState(state, false);
            QuestMachineMessages.QuestStateChanged(quest, quest.id, quest.GetState());
        }

        private static int GetSavedVersion(Quest quest)
        {
            string value;
            if (quest.tagDictionary.dict.TryGetValue(VersionTagKey, out value))
            {
                return SafeConvert.ToInt(value);
            }
            else
            {
                return 2; // Assume version 2 if no version tag.
            }
        }

        private static void ReadCountersFromStream(BinaryReader binaryReader, Quest quest, int savedVersion)
        {
            int numCounters = (savedVersion >= 3) ? binaryReader.ReadByte() : quest.counterList.Count;
            for (int i = 0; i < numCounters; i++)
            {
                var value = binaryReader.ReadInt32();
                int minValue = 0;
                int maxValue = 99;
                if (savedVersion >= 4)
                {
                    minValue = binaryReader.ReadInt32();
                    maxValue = binaryReader.ReadInt32();
                }
                if (i < quest.counterList.Count)
                {
                    var counter = quest.counterList[i];
                    if (savedVersion >= 4)
                    {
                        counter.minValue = minValue;
                        counter.maxValue = maxValue;
                    }
                    counter.SetValue(value, QuestCounterSetValueMode.DontInformListeners);
                }
            }
        }

        private static void ReadQuestNodesFromStream(BinaryReader binaryReader, Quest quest, int savedVersion)
        {
            int numNodes = (savedVersion >= 3) ? binaryReader.ReadByte() : quest.nodeList.Count;
            for (int i = 0; i < numNodes; i++)
            {
                if (i < quest.nodeList.Count)
                {
                    ReadQuestNodeDataFromStream(binaryReader, quest.nodeList[i], savedVersion);
                }
            }
        }

        private static void ReadQuestNodeDataFromStream(BinaryReader binaryReader, QuestNode node, int savedVersion)
        {
            var questNodeState = (QuestNodeState)binaryReader.ReadByte();
            ReadConditionSetDataFromStream(binaryReader, node.conditionSet, savedVersion);
            ReadTagDictionaryFromStream(binaryReader, node.tagDictionary, StringField.GetStringValue(node.internalName));
            node.SetState(questNodeState, false);
        }

        private static void ReadConditionSetDataFromStream(BinaryReader binaryReader, QuestConditionSet conditionSet, int savedVersion)
        {
            conditionSet.numTrueConditions = Mathf.Clamp(binaryReader.ReadByte(), 0, conditionSet.conditionList.Count);
            int numConditions = (savedVersion >= 3) ? binaryReader.ReadByte() : conditionSet.conditionList.Count;
            if (savedVersion >= 2)
            {
                for (int i = 0; i < numConditions; i++)
                {
                    if (!binaryReader.BaseStream.CanRead) continue;
                    var alreadyTrue = binaryReader.ReadBoolean();
                    if (i >= conditionSet.conditionList.Count || conditionSet.conditionList[i] == null) continue;
                    conditionSet.conditionList[i].alreadyTrue = alreadyTrue;
                }
            }
        }

        private static void ReadTagDictionaryFromStream(BinaryReader binaryReader, TagDictionary tags, string questOrNodeName)
        {
            if (tags == null) return;
            tags.dict.Clear();
            var count = binaryReader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var key = binaryReader.ReadString();
                var value = binaryReader.ReadString();
                if (string.IsNullOrEmpty(key))
                {
                    //---Suppress warning: if (Debug.isDebugBuild) Debug.LogWarning("Quest Machine: While deserializing quest tags, found a tag with a blank name in " + questOrNodeName + ".");
                }
                else if (tags.dict.ContainsKey(key))
                {
                    //---Suppress warning: if (Debug.isDebugBuild) Debug.LogWarning("Quest Machine: While deserializing quest tags, found two tags with the name '" + key + "' in " + questOrNodeName + ".");
                }
                else
                {
                    tags.dict.Add(key, value);
                }
            }
        }

        private static void ReadQuestIndicatorsFromStream(BinaryReader binaryReader, Dictionary<string, QuestIndicatorState> indicatorRecords)
        {
            if (indicatorRecords == null) return;
            indicatorRecords.Clear();
            var count = binaryReader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var key = binaryReader.ReadString();
                var value = binaryReader.ReadInt32();
                indicatorRecords.Add(key, (QuestIndicatorState)value);
            }
        }

        #endregion

        #region Serialize (JSON)

        /// <summary>
        /// Serializes the minimum quest data necessary to a JSON string.
        /// Note: Uses reference values for tag dictionaries.
        /// </summary>
        public static string SerializeJson(Quest quest)
        {
            if (quest == null) return string.Empty;
            try
            {
                var data = new StaticQuestJsonSaveData();
                data.version = version;
                data.state = quest.GetState();
                data.tags = quest.tagDictionary; // Note: Reference value.
                data.questGiverID = StringField.GetStringValue(quest.questGiverID);
                quest.timesAccepted = quest.timesAccepted;
                quest.UpdateCooldown();
                data.cooldownRemaining = quest.cooldownSecondsRemaining;
                data.showInTrackHUD = quest.showInTrackHUD;
                SerializeConditionSetDataToJsonData(quest.autostartConditionSet, out data.autostart);
                SerializeConditionSetDataToJsonData(quest.offerConditionSet, out data.offer);

                // Don't save the info below if waiting to start:
                if (data.state != QuestState.WaitingToStart || quest.saveAllIfWaitingToStart)
                {
                    data.counters = SerializeCountersToJsonData(quest.counterList);
                    data.nodes = SerializeNodesToJsonData(quest.nodeList);
                    SerializeIndicatorsToJsonData(quest.indicatorStates, out data.indicatorIDs, out data.indicatorStates);
                }

                //Debug.Log(JsonUtility.ToJson(data));
                return JsonUtility.ToJson(data);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                return string.Empty;
            }
        }

        private static void SerializeConditionSetDataToJsonData(QuestConditionSet conditionSet, 
            out ConditionSetJsonSaveData data)
        {
            data = new ConditionSetJsonSaveData();
            data.alreadyTrue = new List<bool>();
            if (conditionSet == null) return;
            data.numTrue = conditionSet.numTrueConditions;
            foreach (var condition in conditionSet.conditionList)
            {
                data.alreadyTrue.Add(condition != null ? condition.alreadyTrue : false);
            }
        }

        private static List<QuestCounterJsonSaveData> SerializeCountersToJsonData(List<QuestCounter> counterList)
        {
            var data = new List<QuestCounterJsonSaveData>();
            foreach (var counter in counterList)
            {
                var counterData = new QuestCounterJsonSaveData();
                if (counter != null)
                {
                    counterData.name = StringField.GetStringValue(counter.name);
                    counterData.value = counter.currentValue;
                    counterData.saveMinMaxValues = true;
                    counterData.minValue = counter.minValue;
                    counterData.maxValue = counter.maxValue;
                }
                data.Add(counterData);
            }
            return data;
        }

        private static List<QuestNodeJsonSaveData> SerializeNodesToJsonData(List<QuestNode> nodeList)
        {
            var data = new List<QuestNodeJsonSaveData>();
            foreach (var node in nodeList)
            {
                var nodeData = new QuestNodeJsonSaveData();
                if (node != null)
                {
                    nodeData.id = StringField.GetStringValue(node.id);
                    nodeData.state = node.GetState();
                    SerializeConditionSetDataToJsonData(node.conditionSet, out nodeData.conditionSet);
                    nodeData.tags = node.tagDictionary;
                }
                data.Add(nodeData);
            }
            return data;
        }

        private static void SerializeIndicatorsToJsonData(Dictionary<string, QuestIndicatorState> dict, 
            out List<string> indicatorIDs, out List<QuestIndicatorState> indicatorStates)
        {
            indicatorIDs = new List<string>();
            indicatorStates = new List<QuestIndicatorState>();
            if (dict == null) return;
            foreach (var kvp in dict)
            {
                indicatorIDs.Add(kvp.Key);
                indicatorStates.Add(kvp.Value);
            }
        }

        #endregion

        #region Deserialize (JSON)

        public static void DeserializeJsonInto(Quest quest, string json)
        {
            if (quest == null || string.IsNullOrEmpty(json)) return;
            var data = JsonUtility.FromJson<StaticQuestJsonSaveData>(json);
            if (json == null)
            {
                Debug.LogWarning($"Quest Machine: Unable to deserialize JSON data for quest '{quest.id}': {json}", quest);
                return;
            }
            try
            {
                var state = data.state;
                DeserializeJsonDataTagDictionary(data.tags, quest.tagDictionary);
                quest.questGiverID.value = data.questGiverID;
                quest.timesAccepted = data.timesAccepted;
                quest.cooldownSecondsRemaining = data.cooldownRemaining;
                quest.showInTrackHUD = data.showInTrackHUD;
                DeserializeJsonDataConditionSet(data.autostart, quest.autostartConditionSet);
                DeserializeJsonDataConditionSet(data.offer, quest.offerConditionSet);
                if (state == QuestState.WaitingToStart && !quest.saveAllIfWaitingToStart)
                {
                    quest.SetState(state, false);
                    return;
                }

                // Don't load the info below if waiting to start:
                DeserializeJsonDataCounters(data.counters, quest.counterList);
                DeserializeJsonDataNodes(data.nodes, quest);
                DeserializeJsonDataIndicators(data.indicatorIDs, data.indicatorStates, quest.indicatorStates);
                quest.SetRuntimeReferences();
                quest.SetState(state, false);
                QuestMachineMessages.QuestStateChanged(quest, quest.id, quest.GetState());

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void DeserializeJsonDataTagDictionary(TagDictionary dataTags, TagDictionary tagDictionary)
        {
            if (dataTags == null || tagDictionary == null) return;
            tagDictionary.dict.Clear();
            dataTags.CopyInto(tagDictionary);
        }

        private static void DeserializeJsonDataConditionSet(ConditionSetJsonSaveData data, QuestConditionSet conditionSet)
        {
            if (data == null || conditionSet == null || conditionSet.conditionList == null) return;
            conditionSet.numTrueConditions = data.numTrue;
            for (int i = 0; i < Mathf.Min(data.alreadyTrue.Count, conditionSet.conditionList.Count); i++)
            {
                if (conditionSet.conditionList[i] == null) continue;
                conditionSet.conditionList[i].alreadyTrue = data.alreadyTrue[i];
            }
        }

        private static void DeserializeJsonDataCounters(List<QuestCounterJsonSaveData> dataCounters, List<QuestCounter> counters)
        {
            if (dataCounters == null || counters == null) return;
            foreach (var counterData in dataCounters)
            {
                if (counterData == null) continue;
                var counter = counters.Find(x => StringField.GetStringValue(x.name) == counterData.name);
                if (counter == null) continue;
                if (counterData.saveMinMaxValues)
                {
                    counter.minValue = counterData.minValue;
                    counter.maxValue = counterData.maxValue;
                }
                counter.SetValue(counterData.value, QuestCounterSetValueMode.DontInformListeners);
            }
        }

        private static void DeserializeJsonDataNodes(List<QuestNodeJsonSaveData> dataNodes, Quest quest)
        {
            if (dataNodes == null || quest == null) return;
            foreach (var nodeData in dataNodes)
            {
                if (nodeData == null) continue;
                var node = quest.GetNode(nodeData.id);
                if (node == null)
                {
                    Debug.LogWarning($"Quest Machine: Can't find node with ID '{nodeData.id}' while deserializing JSON into quest '{quest.id}'", quest);
                    continue;
                }
                var questNodeState = nodeData.state;
                DeserializeJsonDataConditionSet(nodeData.conditionSet, node.conditionSet);
                DeserializeJsonDataTagDictionary(nodeData.tags, node.tagDictionary);
                node.SetState(questNodeState, false);
            }
        }

        private static void DeserializeJsonDataIndicators(List<string> indicatorIDs, 
            List<QuestIndicatorState> indicatorStates, 
            Dictionary<string, QuestIndicatorState> dict)
        {
            if (indicatorIDs == null || indicatorStates == null || dict == null) return;
            dict.Clear();
            for (int i = 0; i < Mathf.Min(indicatorIDs.Count, indicatorStates.Count); i++)
            {
                dict.Add(indicatorIDs[i], indicatorStates[i]);
            }
        }

        #endregion

    }
}
