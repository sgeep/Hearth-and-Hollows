// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEditor;

namespace PixelCrushers.QuestMachine
{

    /// <summary>
    /// Custom inspector for SceneEventQuestAction assets.
    /// </summary>
    [CustomEditor(typeof(SceneEventQuestAction), true)]
    public class SceneEventQuestActionEditor : QuestSubassetEditor
    {
        private QuestMachineSceneEvents m_questMachineSceneEvents;
        private SerializedObject m_questMachineSceneEventsSerializedObject;

        protected override void Draw()
        {
            if (serializedObject == null) return;
            serializedObject.Update();
            EditorGUILayout.HelpBox("This action invokes a UnityEvent in a specific scene.", MessageType.None);
            var guidProperty = serializedObject.FindProperty("m_guid");

            // Draw scene-specific event:
            var sceneEventGuid = guidProperty.stringValue;
            int sceneEventIndex = -1;
            if (string.IsNullOrEmpty(sceneEventGuid))
            {
                // If this entry is not associated with a scene event, show Add button:
                if (GUILayout.Button(new GUIContent("Add Scene Event", "Add a UnityEvent that operates on GameObjects in the currently-open scene.")))
                {
                    MakeSureQuestMachineSceneEventsExists();
                    sceneEventIndex = QuestMachineSceneEvents.AddNewSceneEvent(out sceneEventGuid);
                    guidProperty.stringValue = sceneEventGuid;
                }
            }
            else
            {
                // Make sure our serialized object points to this scene's QuestMachineSceneEvents:
                if (m_questMachineSceneEvents == null || m_questMachineSceneEventsSerializedObject == null)
                {
                    m_questMachineSceneEvents = PixelCrushers.GameObjectUtility.FindFirstObjectByType<QuestMachineSceneEvents>();
                    m_questMachineSceneEventsSerializedObject = (m_questMachineSceneEvents != null) 
                        ? new SerializedObject(m_questMachineSceneEvents) : null;
                }
                // Then check if the entry's scene event is defined in this scene:
                sceneEventIndex = QuestMachineSceneEvents.GetSceneEventIndex(sceneEventGuid, m_questMachineSceneEvents);
            }
            if (sceneEventIndex == -1 && !string.IsNullOrEmpty(sceneEventGuid))
            {
                // If scene event is assigned but not in this scene, show Delete button:
                GUILayout.Label("Scene Event operates in another scene.");
                if (GUILayout.Button("Delete Scene Event"))
                {
                    guidProperty.stringValue = string.Empty;
                }
            }
            if (sceneEventIndex != -1 && m_questMachineSceneEventsSerializedObject != null)
            {
                // Scene event is in this scene. Draw it:
                m_questMachineSceneEventsSerializedObject.Update();
                var sceneEventsListProperty = m_questMachineSceneEventsSerializedObject.FindProperty("sceneEvents");
                if (sceneEventsListProperty != null && 0 <= sceneEventIndex && sceneEventIndex < sceneEventsListProperty.arraySize)
                {
                    var sceneEventProperty = sceneEventsListProperty.GetArrayElementAtIndex(sceneEventIndex);
                    EditorGUILayout.LabelField("Scene Event", EditorStyles.boldLabel);
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(sceneEventProperty.FindPropertyRelative("guid"), true);
                    EditorGUI.EndDisabledGroup();
                    if (sceneEventProperty != null)
                    {
                        EditorGUILayout.PropertyField(sceneEventProperty.FindPropertyRelative("onExecute"), true);
                    }
                }
                m_questMachineSceneEventsSerializedObject.ApplyModifiedProperties();
                if (GUILayout.Button("Delete Scene Event"))
                {
                    QuestMachineSceneEvents.RemoveSceneEvent(sceneEventGuid);
                    guidProperty.stringValue = string.Empty;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void MakeSureQuestMachineSceneEventsExists()
        {
            if (m_questMachineSceneEvents == null)
            {
                m_questMachineSceneEvents = PixelCrushers.GameObjectUtility.FindFirstObjectByType<QuestMachineSceneEvents>();
                if (m_questMachineSceneEvents == null)
                {
                    var go = new GameObject("Quest Machine Scene Events");
                    m_questMachineSceneEvents = go.AddComponent(PixelCrushers.TypeUtility.GetWrapperType(typeof(QuestMachineSceneEvents))) as QuestMachineSceneEvents;
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                }
            }
        }

    }
}
