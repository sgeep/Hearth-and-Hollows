//Copyright (c) 2016 Kai Clavier [kaiclavier.com] Do Not Distribute
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Jitter Data", menuName = "Super Text Mesh/Jitter Data", order = 1)]
public class STMJitterData : STMBaseData{
	//public string name;
	public float amount;
	public bool perlin = false;
	public float perlinTimeMulti = 20f;
	public AnimationCurve distance = new AnimationCurve(new Keyframe(0f,0f,1f,1f), new Keyframe(1f,1f,1f,1f)); //genereate linear curve //how far the jitter will travel from the origin, on average
	public AnimationCurve distanceOverTime = new AnimationCurve(new Keyframe(0f,1f,0f,0f), new Keyframe(1f,1f,0f,0f));
	[Range(0.0001f, 100f)]
	public float distanceOverTimeMulti = 1f;

	// public JitterRange range = JitterRange.Character;
	// public enum JitterRange
	// {
	// 	Character,
	// 	Mesh
	// }
	
	#if UNITY_EDITOR
	public override void DrawCustomInspector(SuperTextMesh stm, SerializedObject serializedData, SuperTextMeshData data){
#if UNITY_2017_1_OR_NEWER
		if(GUILayout.Button("Toggle Preview"))
		{
			data.TogglePreview("<j=" + this.name + ">Hello, World!", this);
		}

		if(data.previewData == this)
		{
			var clamped = (distanceOverTime.postWrapMode == WrapMode.Clamp ||
			               distanceOverTime.postWrapMode == WrapMode.Once ||
			               distanceOverTime.postWrapMode == WrapMode.ClampForever);
			var loop = clamped ? distanceOverTime.keys[distanceOverTime.length - 1].time : -1f;
			STMCustomInspectorTools.DrawRenderPreview(data, 1f);
		}
#endif
	//gather parts for this data:
		SerializedProperty amount = serializedData.FindProperty("amount");
		SerializedProperty perlin = serializedData.FindProperty("perlin");
		SerializedProperty perlinTimeMulti = serializedData.FindProperty("perlinTimeMulti");
		//SerializedProperty distance = serializedData.FindProperty("distance");
		//SerializedProperty distanceOverTime = serializedData.FindProperty("distanceOverTime");
		SerializedProperty distanceOverTimeMulti = serializedData.FindProperty("distanceOverTimeMulti");
	//Title bar:
		STMCustomInspectorTools.DrawTitleBar(this, stm,data);
	//the rest:
		EditorGUILayout.PropertyField(amount);
		EditorGUILayout.PropertyField(perlin);
		if(perlin.boolValue){
			EditorGUILayout.PropertyField(perlinTimeMulti);
		}
		//EditorGUILayout.PropertyField(distance);
		//EditorGUILayout.PropertyField(distanceOverTime);
		distance = EditorGUILayout.CurveField("Distance", distance);
		distanceOverTime = EditorGUILayout.CurveField("Distance Over Time", distanceOverTime);
		
		EditorGUILayout.PropertyField(distanceOverTimeMulti);
	}
	#endif
}