// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using UnityEditor;

namespace PixelCrushers.QuestMachine
{

    /// <summary>
    /// Custom inspector for HeadingTextQuestContent assets.
    /// </summary>
    [CustomEditor(typeof(BodyTextQuestContent), true)]
    public class BodyTextQuestUIContentEditor : QuestSubassetEditor
    {

        protected override void Draw()
        {
            if (serializedObject == null) return;
            var bodyTextProperty = serializedObject.FindProperty("m_bodyText");
            var setColorProperty = serializedObject.FindProperty("m_setColor");
            var colorProperty = serializedObject.FindProperty("m_color");
            UnityEngine.Assertions.Assert.IsNotNull(bodyTextProperty, "Quest Machine: Internal error - m_headingText is null.");
            if (bodyTextProperty == null) return;
            EditorGUILayout.PropertyField(bodyTextProperty, true);
            if (setColorProperty != null)
            {
                EditorGUILayout.PropertyField(setColorProperty);
                if (setColorProperty.boolValue) EditorGUILayout.PropertyField(colorProperty, true);
            }
        }

    }
}
