// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;

namespace PixelCrushers.QuestMachine
{

    /// <summary>
    /// Custom inspector for QuestDatabase.
    /// </summary>
    [CustomEditor(typeof(QuestDatabase), true)]
    public class QuestDatabaseEditor : UnityEditor.Editor
    {

        private ReorderableList questReorderableList { get; set; }
        private string entityTypeFolderPath = string.Empty;
        private bool showQuestRelations = false;
        private TextTable textTable;
        private bool includeStringAssets = true;
        private bool abbreviateFieldNames;
        private bool prependQuestIDs;

        private static GUIContent ExportTextLabel = new GUIContent("Export All Text", "Export all quest text from all quests above into text table.");
        private static GUIContent IncludeStringAssetsLabel = new GUIContent("Include String Assets", "Also move text from StringAsset references into text table.");
        private static GUIContent AbbreviateFieldNamesLabel = new GUIContent("Abbreviate Field Names", "Abbreviate field names to 32 characters instead of using full text as field name.");
        private static GUIContent PrependQuestIDsLabel = new GUIContent("Prepend Quest IDs", "Prepend quest IDs to field names. Useful to prevent multiple quests from using the same field name.");

        protected virtual void OnEnable()
        {
            Undo.undoRedoPerformed += RepaintEditorWindow;
            QuestEditorWindow.currentInspectors.Add(this);
            questReorderableList = new ReorderableList(serializedObject, serializedObject.FindProperty("m_questAssets"), true, true, true, true);
            questReorderableList.drawHeaderCallback = OnDrawHeader;
            questReorderableList.onSelectCallback = OnChangeSelection;
            questReorderableList.onAddCallback = OnAddElement;
            questReorderableList.onRemoveCallback = OnRemoveElement;
            questReorderableList.drawElementCallback = OnDrawElement;
            questReorderableList.onReorderCallback = OnReorder;
        }

        protected virtual void OnDisable()
        {
            Undo.undoRedoPerformed -= RepaintEditorWindow;
            QuestEditorWindow.currentInspectors.Remove(this);
        }

        protected void RepaintEditorWindow()
        {
            QuestEditorWindow.RepaintNow();
        }

        public override void OnInspectorGUI()
        {
#if DEBUG_QUEST_EDITOR
            var key = "PixelCrushers.QuestMachine.EditorPrefsDebug.DefaultInspectorFoldout";
            var foldout = EditorPrefs.GetBool(key);
            var newFoldout = EditorGUILayout.Foldout(foldout, "Default Inspector");
            if (newFoldout != foldout) EditorPrefs.SetBool(key, newFoldout);
            if (newFoldout) base.OnInspectorGUI();
#endif

            serializedObject.Update();
            DrawDescription();
            DrawQuestList();
            serializedObject.ApplyModifiedProperties();
            DrawGetAllInSceneButton();
            DrawExportToTextTable();
            DrawImagesList();
        }

        private void DrawDescription()
        {
            var descriptionProperty = serializedObject.FindProperty("m_description");
            if (descriptionProperty == null) return;
            EditorGUILayout.PropertyField(descriptionProperty);
        }

        private void DrawQuestList()
        {
            if (!showQuestRelations && !(0 <= questReorderableList.index && questReorderableList.index <= questReorderableList.count))
            {
                questReorderableList.index = 0;
                SetQuestInEditorWindow(questReorderableList.index);
            }

            questReorderableList.DoLayoutList();
        }

        private void OnDrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Quest Assets");
        }

        private void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (!(0 <= index && index < questReorderableList.serializedProperty.arraySize)) return;
            var buttonWidth = 48f;
            var questRect = new Rect(rect.x, rect.y + 1, rect.width - buttonWidth - 2, EditorGUIUtility.singleLineHeight);
            var buttonRect = new Rect(rect.x + rect.width - buttonWidth, rect.y + 1, buttonWidth, EditorGUIUtility.singleLineHeight);
            var questProperty = questReorderableList.serializedProperty.GetArrayElementAtIndex(index);
            var isQuestAssigned = questProperty.objectReferenceValue != null;
            var buttonGUIContent = isQuestAssigned ? new GUIContent("Edit", "Edit in Quest Editor window.") : new GUIContent("New", "Create new quest asset in this slot.");
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(questRect, questProperty, GUIContent.none, false);
            if (EditorGUI.EndChangeCheck()) SetQuestInEditorWindow(index);
            if (GUI.Button(buttonRect, buttonGUIContent))
            {
                if (!isQuestAssigned)
                {
                    try
                    {
                        questProperty.objectReferenceValue = QuestEditorAssetUtility.CreateNewQuestAssetFromDialog();
                    }
                    catch (System.NullReferenceException)
                    { } // Property might not be serialized yet.
                }
                QuestEditorWindow.ShowWindow();
                SetQuestInEditorWindow(index);
                questReorderableList.index = index;
            }
        }

        private void OnReorder(ReorderableList list)
        {
            if (QuestEditorWindow.instance == null) return;
            SetQuestInEditorWindow(list.index);
        }

        private void OnChangeSelection(ReorderableList list)
        {
            SetQuestInEditorWindow(list.index);
        }

        private void OnRemoveElement(ReorderableList list)
        {
            var element = (0 <= list.index && list.index < list.count) ? list.serializedProperty.GetArrayElementAtIndex(list.index) : null;
            var quest = (element != null) ? (element.objectReferenceValue as Quest) : null;
            var permanentlyDelete = false;
            if (quest != null)
            {
                var option = EditorUtility.DisplayDialogComplex("Remove Quest",
                    "Do you want to remove the reference to '" + quest.title.value + "' from this database or permanently delete the quest asset from your whole project?",
                    "Remove Reference", "Permanently Delete", "Cancel");
                if (option == 2) return; // Cancel.
                permanentlyDelete = (option == 1);
                //ReorderableList.defaultBehaviours.DoRemoveButton(list);
                if (permanentlyDelete)
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(quest));
                }
            }
            //--- In Unity 6+, defaultBehaviors.DoRemoveButton doesn't remove element.
            //ReorderableList.defaultBehaviours.DoRemoveButton(list);
            serializedObject.ApplyModifiedProperties();
            (target as QuestDatabase).questAssets.Remove(quest);
            serializedObject.Update();
            OnChangeSelection(list);
            GUIUtility.ExitGUI();
        }

        private void OnAddElement(ReorderableList list)
        {
            ReorderableList.defaultBehaviours.DoAddButton(list);
            var newIndex = list.serializedProperty.arraySize - 1;
            var element = list.serializedProperty.GetArrayElementAtIndex(newIndex);
            if (element != null) element.objectReferenceValue = null;
        }

        private void SetQuestInEditorWindow(int questListIndex)
        {
            if (!QuestEditorWindow.isOpen) return;
            serializedObject.ApplyModifiedProperties();
            var questDatabase = target as QuestDatabase;
            if (questDatabase == null) return;
            if (!(0 <= questListIndex && questListIndex < questDatabase.questAssets.Count)) return;
            QuestEditorWindow.ShowWindow();
            var quest = questDatabase.questAssets[questListIndex];
            QuestEditorWindow.instance.SelectQuest(quest);
        }

        private void DrawGetAllInSceneButton()
        {
            try
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Show Quest Relations", "Show how all quests in database are linked."), GUILayout.Width(136)))
                {
                    showQuestRelations = true;
                    QuestEditorWindow.ShowWindow();
                    QuestEditorWindow.instance.ShowQuestRelations(target as QuestDatabase);
                }
                if (GUILayout.Button("Add All In Scene", GUILayout.Width(128)))
                {
                    if (EditorUtility.DisplayDialog("Add All In Scene", "Add all quests assigned in the current scene?", "OK", "Cancel"))
                    {
                        AddAllInScene();
                    }
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        private void AddAllInScene()
        {
            var database = target as QuestDatabase;
            if (database == null) return;
            Undo.RecordObject(database, "Add Quests In Scene");
            foreach (var questListContainer in GameObjectUtility.FindObjectsByType<QuestListContainer>())
            {
                foreach (var quest in questListContainer.questList)
                {
                    if (quest.isAsset && !database.questAssets.Contains(quest))
                    {
                        database.questAssets.Add(quest);
                    }
                }
            }
            EditorUtility.SetDirty(target);
        }

        #region Export To Text Table

        private void DrawExportToTextTable()
        {
            bool clickedExport = false;
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            EditorGUILayout.BeginHorizontal();
            textTable = EditorGUILayout.ObjectField(textTable, typeof(TextTable), false) as TextTable;
            EditorGUI.BeginDisabledGroup(textTable == null);
            clickedExport = GUILayout.Button(ExportTextLabel, GUILayout.Width(128));
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            includeStringAssets = EditorGUILayout.Toggle(IncludeStringAssetsLabel, includeStringAssets);
            abbreviateFieldNames = EditorGUILayout.Toggle(AbbreviateFieldNamesLabel, abbreviateFieldNames);
            prependQuestIDs = EditorGUILayout.Toggle(PrependQuestIDsLabel, prependQuestIDs);
            if (clickedExport) 
            {
                if (EditorUtility.DisplayDialog("Export All Quests' Text",
                    "This will REMOVE all quest text in all quests assigned to this quest database, put the text in this text table, and link the quests to the text table. Continue?", "OK", "Cancel"))
                {
                    ExportQuestsToTextTable();
                }
                GUIUtility.ExitGUI();
            }
        }

        private void ExportQuestsToTextTable()
        {
            var questList = (target as QuestDatabase).questAssets;
            foreach (Quest quest in questList)
            {
                if (quest == null) continue;
                Undo.RecordObject(quest, "Fill Text Table");
                QuestTextToTextTableWizard.MoveTextToTextTable(quest, textTable, includeStringAssets, abbreviateFieldNames, prependQuestIDs);
                EditorUtility.SetDirty(quest);
            }
            EditorUtility.SetDirty(textTable);
            QuestEditorWindow.RepaintNow();
            QuestEditorWindow.RepaintInspectorsNow();
            if (TextTableEditorWindow.instance != null)
            {
                Selection.activeObject = null;
                Selection.activeObject = textTable;
            }
        }

        #endregion

        #region EntityType Images

        private void DrawImagesList()
        {
            EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
            QuestEditorPrefs.databaseImagesFoldout = QuestEditorUtility.EditorGUILayoutFoldout("EntityType Images", "Images used by procedural entity types. If you're not procedurally generating quests, you can ignore this section.", QuestEditorPrefs.databaseImagesFoldout);
            if (!QuestEditorPrefs.databaseImagesFoldout) return;

            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_images"), true);
            serializedObject.ApplyModifiedProperties();
            try
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(new GUIContent("Scan EntityTypes...", "Adds images used by EntityTypes in a specified folder."), GUILayout.Width(128)))
                {
                    if (string.IsNullOrEmpty(entityTypeFolderPath)) entityTypeFolderPath = Application.dataPath;
                    entityTypeFolderPath = EditorUtility.OpenFolderPanel("Scan EntityTypes In", entityTypeFolderPath, string.Empty);
                    if (!string.IsNullOrEmpty(entityTypeFolderPath) && Directory.Exists(entityTypeFolderPath))
                    {
                        Undo.RecordObject(target, "Add EntityType Images");
                        ScanEntityTypesInFolder(entityTypeFolderPath);
                        EditorUtility.SetDirty(target);
                    }
                }
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
            }
        }

        private void ScanEntityTypesInFolder(string path)
        {
            var database = target as QuestDatabase;
            if (database == null) return;
            Undo.RecordObject(database, "Add Images");
            var dataPathLength = Application.dataPath.Length - "Assets".Length;
            var filenames = Directory.GetFiles(path, "*.asset", SearchOption.AllDirectories);
            foreach (var filename in filenames)
            {
                var relativeFilename = filename.Substring(dataPathLength).Replace("\\", "/");
                var entityType = AssetDatabase.LoadAssetAtPath<EntityType>(relativeFilename);
                if (entityType != null && entityType.image != null && !database.images.Contains(entityType.image))
                {
                    database.images.Add(entityType.image);
                }
            }
        }

        #endregion

    }
}
