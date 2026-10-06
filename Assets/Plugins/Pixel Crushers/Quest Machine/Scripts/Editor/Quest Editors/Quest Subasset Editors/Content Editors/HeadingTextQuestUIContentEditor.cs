// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEditor;

namespace PixelCrushers.QuestMachine
{

    /// <summary>
    /// Custom inspector for HeadingTextQuestContent assets.
    /// </summary>
    [CustomEditor(typeof(HeadingTextQuestContent), true)]
    public class HeadingTextQuestUIContentEditor : QuestSubassetEditor
    {

        protected override void Draw()
        {
            if (serializedObject == null) return;
            var useQuestTitleProperty = serializedObject.FindProperty("m_useQuestTitle");
            var headingLevelProperty = serializedObject.FindProperty("m_headingLevel");
            var headingTextProperty = serializedObject.FindProperty("m_headingText");
            var setColorProperty = serializedObject.FindProperty("m_setColor");
            var colorProperty = serializedObject.FindProperty("m_color");
            UnityEngine.Assertions.Assert.IsNotNull(useQuestTitleProperty, "Quest Machine: Internal error - m_useQuestTitle is null.");
            UnityEngine.Assertions.Assert.IsNotNull(headingLevelProperty, "Quest Machine: Internal error - m_headingLevel is null.");
            UnityEngine.Assertions.Assert.IsNotNull(headingTextProperty, "Quest Machine: Internal error - m_headingText is null.");
            //UnityEngine.Assertions.Assert.IsNotNull(setColorProperty, "Quest Machine: Internal error - m_setColor is null.");
            //UnityEngine.Assertions.Assert.IsNotNull(colorProperty, "Quest Machine: Internal error - m_color is null.");
            if (useQuestTitleProperty == null || headingLevelProperty == null || headingTextProperty == null) return;
                //setColorProperty == null || colorProperty == null) return;
            EditorGUILayout.PropertyField(useQuestTitleProperty);
            EditorGUILayout.PropertyField(headingLevelProperty);
            if (!useQuestTitleProperty.boolValue) EditorGUILayout.PropertyField(headingTextProperty, true);
            if (setColorProperty != null)
            {
                EditorGUILayout.PropertyField(setColorProperty);
                if (setColorProperty.boolValue) EditorGUILayout.PropertyField(colorProperty, true);
            }
        }

    }
}
