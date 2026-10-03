//Copyright (c) 2016 Kai Clavier [kaiclavier.com] Do Not Distribute
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Audio Clip Data", menuName = "Super Text Mesh/Audio Clip Data", order = 1)]
public class STMAudioClipData : STMBaseData{
	//public string name;
	public AudioClip[] clips;

	#if UNITY_EDITOR
	public override void DrawCustomInspector(SuperTextMesh stm, SerializedObject serializedData, SuperTextMeshData data){
	//gather parts for this data:
		SerializedProperty clips = serializedData.FindProperty("clips");
	//Title bar:
		STMCustomInspectorTools.DrawTitleBar(this, stm, data);
	//the rest:
		EditorGUILayout.PropertyField(clips,true);
	}
	#endif
}