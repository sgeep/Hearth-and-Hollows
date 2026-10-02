//Copyright (c) 2016 Kai Clavier [kaiclavier.com] Do Not Distribute
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Voice Data", menuName = "Super Text Mesh/Voice Data", order = 1)]
public class STMVoiceData : STMBaseData{
	//public string name;
	[TextArea(3,10)]
	public string text;

	#if UNITY_EDITOR
	public override void DrawCustomInspector(SuperTextMesh stm, SerializedObject serializedData, SuperTextMeshData data){
	//gather parts for this data:
		SerializedProperty text = serializedData.FindProperty("text");
	//Title bar:
		STMCustomInspectorTools.DrawTitleBar(this,stm,data);
	//the rest:
		EditorGUILayout.PropertyField(text);
	}
	#endif
}
