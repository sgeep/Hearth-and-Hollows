//Copyright (c) 2016-2023 Kai Clavier [kaiclavier.com] Do Not Distribute
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Delay Data", menuName = "Super Text Mesh/Delay Data", order = 1)]
public class STMDelayData : STMBaseData{
	//public string name;
	[Tooltip("Amount of additional delays to be applied. eg. If text delay is normally 0.1 and this value is 3, it will cause a delay of 0.4 seconds in total. (0.1 + (3 * 0.1))")]
	public int count = 0;
	
	#if UNITY_EDITOR
	public override void DrawCustomInspector(SuperTextMesh stm, SerializedObject serializedData, SuperTextMeshData data){
	//gather parts for this data:
		SerializedProperty count = serializedData.FindProperty("count");
	//Title bar:
		STMCustomInspectorTools.DrawTitleBar(this,stm, data);
	//the rest:
		EditorGUILayout.PropertyField(count);
	}
	#endif
}