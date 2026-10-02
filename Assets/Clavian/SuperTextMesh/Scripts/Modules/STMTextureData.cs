//Copyright (c) 2016-2025 Kai Clavier [kaiclavier.com] Do Not Distribute
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Texture Data", menuName = "Super Text Mesh/Texture Data", order = 1)]
public class STMTextureData : STMBaseData{
	//public string name;
	public Texture texture;
	public FilterMode filterMode;

	public enum ColorMode
	{
		Normal,
		Always,
		Multiply
	}
	[Tooltip("Normal: This texture will display as-is, but be effected by color alpha. " +
	         "Always: This texture will always render over white and full alpha. " +
	         "Multiply: This texture's color will be multiplied by text color.")]
	public ColorMode colorMode = ColorMode.Normal;
	
	public bool relativeToLetter = false; //will the texture be relative to each letter
	public bool scaleWithText = false;
	public Vector2 tiling = Vector2.one; //or scale
	public Vector2 offset = Vector2.zero;
	public Vector2 scrollSpeed = Vector2.one;
	//public float speed = 0.5f; //scroll speed
	//public float spread = 0.1f; //how far it stretches, in local

#if UNITY_EDITOR
	public override void DrawCustomInspector(SuperTextMesh stm, SerializedObject serializedData, SuperTextMeshData data){
#if UNITY_2017_1_OR_NEWER
		if(GUILayout.Button("Toggle Preview"))
		{
			data.TogglePreview("<c=" + this.name + ">Hello, World!", this);
		}
		if(data.previewData == this)
		{
			STMCustomInspectorTools.DrawRenderPreview(data, -1f);
		}
#endif
		//gather parts for this data:
		SerializedProperty texture = serializedData.FindProperty("texture");
		SerializedProperty colorMode = serializedData.FindProperty("colorMode");
		SerializedProperty filterMode = serializedData.FindProperty("filterMode");
		SerializedProperty relativeToLetter = serializedData.FindProperty("relativeToLetter");
		SerializedProperty scaleWithText = serializedData.FindProperty("scaleWithText");
		SerializedProperty tiling = serializedData.FindProperty("tiling");
		SerializedProperty offset = serializedData.FindProperty("offset");
		SerializedProperty scrollSpeed = serializedData.FindProperty("scrollSpeed");
		//Title bar:
		STMCustomInspectorTools.DrawTitleBar(this,stm,data);
		//the rest:
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(texture);
		EditorGUILayout.PropertyField(colorMode);
		EditorGUILayout.PropertyField(filterMode);
		EditorGUILayout.PropertyField(relativeToLetter);
		EditorGUILayout.PropertyField(scaleWithText);
		EditorGUILayout.PropertyField(tiling);
		EditorGUILayout.PropertyField(offset);
		EditorGUILayout.PropertyField(scrollSpeed);
		if(EditorGUI.EndChangeCheck())
		{
#if UNITY_2017_1_OR_NEWER
			data.forceRebuild = true;
#endif
		}
	}
#endif
}