using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class STMBaseData : ScriptableObject
{
#if UNITY_EDITOR
	public bool showFoldout = false;

	public void DoDrawCustomInspector(SuperTextMesh stm, SuperTextMeshData data)
	{
		Undo.RecordObject(this, "Edited STM Wave Data");
		var serializedData = new SerializedObject(this);
		serializedData.Update();
		DrawCustomInspector(stm, serializedData, data);
		EditorGUILayout.Space(); //////////////////SPACE
		if(this != null)serializedData.ApplyModifiedProperties(); //since break; cant be called
	}
	public abstract void DrawCustomInspector(SuperTextMesh stm, SerializedObject serializedData, SuperTextMeshData data);
#endif
}
