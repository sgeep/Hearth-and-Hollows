// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEditor;

namespace PixelCrushers.QuestMachine
{

    /// <summary>
    /// Custom inspector GUI when the Quest itself (and not a QuestNode) is 
    /// selected in the Quest Editor window.
    /// </summary>
    public class QuestMainInspectorGUI
    {

        private QuestConditionSetInspectorGUI m_autostartConditionSetInspectorGUI = null;
        private QuestConditionSetInspectorGUI m_offerConditionSetInspectorGUI = null;
        private QuestStateInfoInspectorGUI m_inactiveStateInfoInspectorGUI = null;
        private QuestStateInfoInspectorGUI m_activeStateInfoInspectorGUI = null;
        private QuestStateInfoInspectorGUI m_successfulStateInfoInspectorGUI = null;
        private QuestStateInfoInspectorGUI m_failedStateInfoInspectorGUI = null;
        private QuestStateInfoInspectorGUI m_abandonedStateInfoInspectorGUI = null;
        private QuestContentListInspectorGUI m_offerContentListDrawer = null;
        private QuestContentListInspectorGUI m_offerConditionsUnmetContentListDrawer = null;
        private QuestCounterListInspectorGUI m_counterListInspectorGUI = null;
        private QuestNodeOrderInspectorGUI m_nodeOrderInspectorGUI = null;

        private static GUIContent AddDefaultOfferContentLabel = new GUIContent("Add Default Content", "Shortcut to add default content items to Offer Text.");

        public void Draw(SerializedObject serializedObject)
        {
            if (serializedObject == null) return;
            DrawMainInfo(serializedObject);
            QuestEditorUtility.EditorGUILayoutVerticalSpace(2);
            DrawAutostartConditions(serializedObject);
            QuestEditorUtility.EditorGUILayoutVerticalSpace(2);
            DrawOfferConditions(serializedObject);
            QuestEditorUtility.EditorGUILayoutVerticalSpace(2);
            DrawStateInfo(serializedObject);
            QuestEditorUtility.EditorGUILayoutVerticalSpace(2);
            DrawCounters(serializedObject);
            QuestEditorUtility.EditorGUILayoutVerticalSpace(2);
            DrawNodes(serializedObject);
        }

        private void DrawMainInfo(SerializedObject serializedObject)
        {
            UnityEngine.Assertions.Assert.IsNotNull(serializedObject, "Quest Machine: Internal error - serializedObject is null.");
            QuestEditorPrefs.mainInfoFoldout = QuestEditorUtility.EditorGUILayoutFoldout("Quest Info", "Main quest info.", QuestEditorPrefs.mainInfoFoldout);
            if (!QuestEditorPrefs.mainInfoFoldout) return;

            try
            {
                QuestEditorUtility.EditorGUILayoutBeginGroup();
                var idProperty = serializedObject.FindProperty("m_id");
                var titleProperty = serializedObject.FindProperty("m_title");
                var iconProperty = serializedObject.FindProperty("m_icon");
                var groupProperty = serializedObject.FindProperty("m_group");
                var labelsProperty = serializedObject.FindProperty("m_labels");
                var questGiverIDProperty = serializedObject.FindProperty("m_questGiverID");
                var textTableProperty = serializedObject.FindProperty("m_textTable");
                var isTrackableProperty = serializedObject.FindProperty("m_isTrackable");
                var showInTrackHUDProperty = serializedObject.FindProperty("m_showInTrackHUD");
                var isAbandonableProperty = serializedObject.FindProperty("m_isAbandonable");
                var rememberIfAbandonedProperty = serializedObject.FindProperty("m_rememberIfAbandoned");
                var clearIndicatorsWhenCompleteProperty = serializedObject.FindProperty("m_clearIndicatorsWhenComplete");
                var deleteWhenCompleteProperty = serializedObject.FindProperty("m_deleteWhenComplete");
                var infinitelyRepeatableProperty = serializedObject.FindProperty("m_infinitelyRepeatable");
                var maxTimesProperty = serializedObject.FindProperty("m_maxTimes");
                var timesAcceptedProperty = serializedObject.FindProperty("m_timesAccepted");
                var cooldownSecondsProperty = serializedObject.FindProperty("m_cooldownSeconds");
                var noRepeatIfSuccessfulProperty = serializedObject.FindProperty("m_noRepeatIfSuccessful");
                var saveAllIfWaitingToStartProperty = serializedObject.FindProperty("m_saveAllIfWaitingToStart");
                var stateProperty = serializedObject.FindProperty("m_state");
                UnityEngine.Assertions.Assert.IsNotNull(idProperty, "Quest Machine: Internal error - m_id is null.");
                UnityEngine.Assertions.Assert.IsNotNull(titleProperty, "Quest Machine: Internal error - m_title is null.");
                UnityEngine.Assertions.Assert.IsNotNull(iconProperty, "Quest Machine: Internal error - m_icon is null.");
                UnityEngine.Assertions.Assert.IsNotNull(groupProperty, "Quest Machine: Internal error - m_group is null.");
                UnityEngine.Assertions.Assert.IsNotNull(labelsProperty, "Quest Machine: Internal error - m_labels is null.");
                UnityEngine.Assertions.Assert.IsNotNull(questGiverIDProperty, "Quest Machine: Internal error - m_questGiverID is null.");
                UnityEngine.Assertions.Assert.IsNotNull(isTrackableProperty, "Quest Machine: Internal error - m_isTrackable is null.");
                UnityEngine.Assertions.Assert.IsNotNull(showInTrackHUDProperty, "Quest Machine: Internal error - m_showInTrackHUD is null.");
                UnityEngine.Assertions.Assert.IsNotNull(isAbandonableProperty, "Quest Machine: Internal error - m_isAbandonable is null.");
                UnityEngine.Assertions.Assert.IsNotNull(rememberIfAbandonedProperty, "Quest Machine: Internal error - m_rememberIfAbandoned is null.");
                UnityEngine.Assertions.Assert.IsNotNull(maxTimesProperty, "Quest Machine: Internal error - m_maxTimes is null.");
                UnityEngine.Assertions.Assert.IsNotNull(timesAcceptedProperty, "Quest Machine: Internal error - m_timesAccepted is null.");
                UnityEngine.Assertions.Assert.IsNotNull(cooldownSecondsProperty, "Quest Machine: Internal error - m_cooldownSeconds is null.");
                UnityEngine.Assertions.Assert.IsNotNull(noRepeatIfSuccessfulProperty, "Quest Machine: Internal error - m_noRepeatIfSuccessful is null.");
                UnityEngine.Assertions.Assert.IsNotNull(saveAllIfWaitingToStartProperty, "Quest Machine: Internal error - m_saveAllIfWaitingToStart is null.");
                UnityEngine.Assertions.Assert.IsNotNull(stateProperty, "Quest Machine: Internal error - m_state is null.");
                if (idProperty == null || titleProperty == null || groupProperty == null || labelsProperty == null ||
                    iconProperty == null || questGiverIDProperty == null || isTrackableProperty == null || showInTrackHUDProperty == null ||
                    isAbandonableProperty == null || rememberIfAbandonedProperty == null || maxTimesProperty == null ||
                    timesAcceptedProperty == null || cooldownSecondsProperty == null || noRepeatIfSuccessfulProperty == null ||
                    saveAllIfWaitingToStartProperty == null || stateProperty == null || infinitelyRepeatableProperty == null) return;

                EditorGUILayout.PropertyField(idProperty, true);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(titleProperty, true);
                EditorGUILayout.PropertyField(groupProperty, true);
                QuestEditorUtility.EditorGUILayoutBeginIndent();
                EditorGUILayout.PropertyField(labelsProperty, true);
                QuestEditorUtility.EditorGUILayoutEndIndent();
                EditorGUILayout.PropertyField(iconProperty);
                EditorGUILayout.PropertyField(questGiverIDProperty, true);
                if (textTableProperty != null)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(textTableProperty, true);
                    if (EditorGUI.EndChangeCheck() && textTableProperty.objectReferenceValue != null)
                    {
                        var textTable = textTableProperty.objectReferenceValue as TextTable;
                        QuestEditorUtility.SetDefaultTextTable(idProperty, textTable);
                        QuestEditorUtility.SetDefaultTextTable(titleProperty, textTable);
                        QuestEditorUtility.SetDefaultTextTable(groupProperty, textTable);
                        QuestEditorUtility.SetDefaultTextTable(questGiverIDProperty, textTable);
                    }
                }
                EditorGUILayout.PropertyField(isTrackableProperty);
                if (isTrackableProperty.boolValue) EditorGUILayout.PropertyField(showInTrackHUDProperty);
                EditorGUILayout.PropertyField(isAbandonableProperty);
                if (isAbandonableProperty.boolValue) EditorGUILayout.PropertyField(rememberIfAbandonedProperty);
                if (clearIndicatorsWhenCompleteProperty != null) EditorGUILayout.PropertyField(clearIndicatorsWhenCompleteProperty);
                if (deleteWhenCompleteProperty != null) EditorGUILayout.PropertyField(deleteWhenCompleteProperty);
                EditorGUILayout.PropertyField(infinitelyRepeatableProperty);
                if (infinitelyRepeatableProperty.boolValue == false)
                {
                    EditorGUILayout.PropertyField(maxTimesProperty);
                }
                if (maxTimesProperty.intValue > 1 || infinitelyRepeatableProperty.boolValue)
                {
                    EditorGUILayout.PropertyField(cooldownSecondsProperty);
                    EditorGUILayout.PropertyField(noRepeatIfSuccessfulProperty);
                }
                EditorGUILayout.PropertyField(saveAllIfWaitingToStartProperty);
                var prevState = stateProperty.enumValueIndex;
                EditorGUILayout.PropertyField(stateProperty, new GUIContent("Current State", "Current quest state."));
                var newState = stateProperty.enumValueIndex;
                if (Application.isPlaying && newState != prevState)
                {
                    // State changed in editor at runtime. Perform runtime state change with all the associated processing:
                    stateProperty.enumValueIndex = prevState;
                    serializedObject.ApplyModifiedProperties();
                    QuestEditorWindow.selectedQuest.SetState((QuestState)newState);
                    serializedObject.Update();
                }
            }
            finally
            {
                QuestEditorUtility.EditorGUILayoutEndGroup();
            }
        }

        private void DrawAutostartConditions(SerializedObject serializedObject)
        {
            UnityEngine.Assertions.Assert.IsNotNull(serializedObject, "Quest Machine: Internal error - serializedObject is null.");
            QuestEditorPrefs.autostartFoldout = QuestEditorUtility.EditorGUILayoutFoldout("Autostart", "Conditions that autostart the quest when true.", QuestEditorPrefs.autostartFoldout);
            if (!QuestEditorPrefs.autostartFoldout) return;

            try
            {
                QuestEditorUtility.EditorGUILayoutBeginGroup();
                EditorGUILayout.HelpBox("If you specify conditions below, then the quest will automatically become active when the conditions become true.", MessageType.None);
                if (m_autostartConditionSetInspectorGUI == null) m_autostartConditionSetInspectorGUI = new QuestConditionSetInspectorGUI();
                m_autostartConditionSetInspectorGUI.Draw(serializedObject.FindProperty("m_autostartConditionSet"));
            }
            finally
            {
                QuestEditorUtility.EditorGUILayoutEndGroup();
            }
        }

        private void DrawOfferConditions(SerializedObject serializedObject)
        {
            UnityEngine.Assertions.Assert.IsNotNull(serializedObject, "Quest Machine: Internal error - serializedObject is null.");
            QuestEditorPrefs.offerFoldout = QuestEditorUtility.EditorGUILayoutFoldout("Offer", "Conditions that must be true before the quest giver can offer the quest.", QuestEditorPrefs.offerFoldout);
            if (!QuestEditorPrefs.offerFoldout) return;

            try
            {
                QuestEditorUtility.EditorGUILayoutBeginGroup();
                EditorGUILayout.HelpBox("If you specify conditions below, then they must be true before the quest giver can offer the quest.", MessageType.None);
                if (m_offerConditionSetInspectorGUI == null) m_offerConditionSetInspectorGUI = new QuestConditionSetInspectorGUI();
                m_offerConditionSetInspectorGUI.Draw(serializedObject.FindProperty("m_offerConditionSet"));
                if (m_offerConditionsUnmetContentListDrawer == null) m_offerConditionsUnmetContentListDrawer =
                        new QuestContentListInspectorGUI(new GUIContent("Offer Conditions Unmet Text", "Show this dialogue content when the offer conditions are unmet."), QuestContentCategory.OfferConditionsUnmet);
                if (m_offerContentListDrawer == null) m_offerContentListDrawer =
                        new QuestContentListInspectorGUI(new GUIContent("Offer Text", "Show this dialogue text to offer the quest to the player."), QuestContentCategory.Offer);
                m_offerConditionsUnmetContentListDrawer.Draw(serializedObject, serializedObject.FindProperty("m_offerConditionsUnmetContentList"), true);
                var offerContentListProperty = serializedObject.FindProperty("m_offerContentList");
                m_offerContentListDrawer.Draw(serializedObject, offerContentListProperty, true);
                if (offerContentListProperty.arraySize == 0)
                {
                    // Show wizard button to add default content:
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button(AddDefaultOfferContentLabel, GUILayout.Width(200)))
                    {
                        AddDefaultOfferContent(offerContentListProperty);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            finally
            {
                QuestEditorUtility.EditorGUILayoutEndGroup();
            }
        }

        private void AddDefaultOfferContent(SerializedProperty offerContentListProperty)
        {
            var heading = AddContentToList<HeadingTextQuestContent>(offerContentListProperty);
            heading.useQuestTitle = true;
            AddContentToList<BodyTextQuestContent>(offerContentListProperty);
            var rewardsHeading = AddContentToList<HeadingTextQuestContent>(offerContentListProperty);
            rewardsHeading.headingLevel = 2;
            rewardsHeading.headingText.text = "Rewards";
        }

        private T AddContentToList<T>(SerializedProperty contentListProperty) where T : QuestContent
        { 
            var content = ScriptableObjectUtility.CreateScriptableObject<T>();
            content.name = typeof(T).Name;
            content.SetDefaultTextTable(QuestEditorWindow.selectedQuest.textTable);
            AssetUtility.AddToAsset(content, QuestEditorWindow.selectedQuestSerializedObject.targetObject);
            QuestEditorWindow.UpdateSelectedQuestSerializedObject();
            try
            {
                contentListProperty.arraySize++;
                contentListProperty.GetArrayElementAtIndex(contentListProperty.arraySize - 1).objectReferenceValue = content;
                contentListProperty.serializedObject.ApplyModifiedProperties();
            }
            catch (System.NullReferenceException e)
            {
#if UNITY_EDITOR
                Debug.LogError("Unity's AssetDatabase couldn't add the content subasset to the quest. The project's AssetDatabase may contain a corrupt asset. Exception: " + e.Message);
#endif
            }
            QuestEditorWindow.ApplyModifiedPropertiesFromSelectedQuestSerializedObject();
            return content;
        }

        private void DrawStateInfo(SerializedObject serializedObject)
        {
            UnityEngine.Assertions.Assert.IsNotNull(serializedObject, "Quest Machine: Internal error - serializedObject is null.");
            var foldout = QuestEditorPrefs.GetStateInfoFoldout(0);
            var newFoldout = QuestEditorUtility.EditorGUILayoutFoldout("States", "Content specific to each state that the quest can be in.", foldout);
            if (newFoldout != foldout)
            {
                QuestEditorPrefs.ToggleStateInfoFoldout(0);
            }
            if (!newFoldout) return;

            try
            {
                QuestEditorUtility.EditorGUILayoutBeginGroup();
                EditorGUILayout.HelpBox("This section contains information specific to each state that the quest can be in.", MessageType.None);
                var stateInfoListProperty = serializedObject.FindProperty("m_stateInfoList");
                UnityEngine.Assertions.Assert.IsNotNull(stateInfoListProperty, "Quest Machine: Internal error - m_stateInfoList is null.");
                if (stateInfoListProperty == null) return;
                if (stateInfoListProperty.arraySize == 0) stateInfoListProperty.arraySize = System.Enum.GetNames(typeof(QuestState)).Length;
                if (m_inactiveStateInfoInspectorGUI == null) m_inactiveStateInfoInspectorGUI = new QuestStateInfoInspectorGUI();
                if (m_activeStateInfoInspectorGUI == null) m_activeStateInfoInspectorGUI = new QuestStateInfoInspectorGUI();
                if (m_successfulStateInfoInspectorGUI == null) m_successfulStateInfoInspectorGUI = new QuestStateInfoInspectorGUI();
                if (m_failedStateInfoInspectorGUI == null) m_failedStateInfoInspectorGUI = new QuestStateInfoInspectorGUI();
                if (m_abandonedStateInfoInspectorGUI == null) m_abandonedStateInfoInspectorGUI = new QuestStateInfoInspectorGUI();
                m_inactiveStateInfoInspectorGUI.Draw(serializedObject, stateInfoListProperty.GetArrayElementAtIndex((int)QuestState.WaitingToStart), 0, QuestState.WaitingToStart);
                m_activeStateInfoInspectorGUI.Draw(serializedObject, stateInfoListProperty.GetArrayElementAtIndex((int)QuestState.Active), 0, QuestState.Active);
                m_successfulStateInfoInspectorGUI.Draw(serializedObject, stateInfoListProperty.GetArrayElementAtIndex((int)QuestState.Successful), 0, QuestState.Successful);
                m_failedStateInfoInspectorGUI.Draw(serializedObject, stateInfoListProperty.GetArrayElementAtIndex((int)QuestState.Failed), 0, QuestState.Failed);
                m_abandonedStateInfoInspectorGUI.Draw(serializedObject, stateInfoListProperty.GetArrayElementAtIndex((int)QuestState.Abandoned), 0, QuestState.Abandoned);
            }
            finally
            {
                QuestEditorUtility.EditorGUILayoutEndGroup();
            }
        }

        private void DrawCounters(SerializedObject serializedObject)
        {
            UnityEngine.Assertions.Assert.IsNotNull(serializedObject, "Quest Machine: Internal error - serializedObject is null.");
            QuestEditorPrefs.countersFoldout = QuestEditorUtility.EditorGUILayoutFoldout("Counters", "Counters used in this quest.", QuestEditorPrefs.countersFoldout);
            if (!QuestEditorPrefs.countersFoldout) return;

            try
            {
                QuestEditorUtility.EditorGUILayoutBeginGroup();
                EditorGUILayout.HelpBox("Quest nodes can track activity using any counters you define below. Examples include counters to track the number collected of a specific item, number of enemies defeated, number of levers pulled, etc.", MessageType.None);
                if (m_counterListInspectorGUI == null) m_counterListInspectorGUI = new QuestCounterListInspectorGUI();
                m_counterListInspectorGUI.Draw(serializedObject, serializedObject.FindProperty("m_counterList"));
            }
            finally
            {
                QuestEditorUtility.EditorGUILayoutEndGroup();
            }
        }

        private void DrawNodes(SerializedObject serializedObject)
        {
            UnityEngine.Assertions.Assert.IsNotNull(serializedObject, "Quest Machine: Internal error - serializedObject is null.");
            QuestEditorPrefs.nodesFoldout = QuestEditorUtility.EditorGUILayoutFoldout("Node Order for UIs", "Order in which nodes provide their contributions to UI output.", QuestEditorPrefs.nodesFoldout);
            if (!QuestEditorPrefs.nodesFoldout) return;

            try
            {
                QuestEditorUtility.EditorGUILayoutBeginGroup();
                EditorGUILayout.HelpBox("Nodes will provide their contributions to UI output in the order below.", MessageType.None);
                if (m_nodeOrderInspectorGUI == null) m_nodeOrderInspectorGUI = new QuestNodeOrderInspectorGUI();
                m_nodeOrderInspectorGUI.Draw(serializedObject);
            }
            finally
            {
                QuestEditorUtility.EditorGUILayoutEndGroup();
            }
        }

    }
}
