//Copyright (c) 2016 Kai Clavier [kaiclavier.com] Do Not Distribute
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Material Data", menuName = "Super Text Mesh/Material Data", order = 1)]
public class STMMaterialData : STMBaseData{
	//public string name;
	public Material material;

	#if UNITY_EDITOR
	public override void DrawCustomInspector(SuperTextMesh stm, SerializedObject serializedData, SuperTextMeshData data){
	//gather parts for this data:
		SerializedProperty material = serializedData.FindProperty("material");
	//Title bar:
		STMCustomInspectorTools.DrawTitleBar(this,stm, data);
	//the rest:
		EditorGUILayout.PropertyField(material);
		if(this.material != null){
			STMCustomInspectorTools.DrawMaterialEditor(this.material, stm);
		}
	}
	#endif
}