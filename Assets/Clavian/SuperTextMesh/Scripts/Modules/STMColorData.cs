//Copyright (c) 2016 Kai Clavier [kaiclavier.com] Do Not Distribute
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Color Data", menuName = "Super Text Mesh/Color Data", order = 1)]
public class STMColorData : STMBaseData{
	//public string name;
	public Color color = Color.white;
	public STMColorData(){
		this.color = Color.white; //sure?
	}
	public STMColorData(Color color){
		this.color = color;
	}
	#if UNITY_EDITOR
	public override void DrawCustomInspector(SuperTextMesh stm, SerializedObject serializedData, SuperTextMeshData data){
	//gather parts for this data:
		SerializedProperty color = serializedData.FindProperty("color");
	//Title bar:
		STMCustomInspectorTools.DrawTitleBar(this,stm, data);
	//the rest:
		EditorGUILayout.PropertyField(color);
	}
	#endif
}