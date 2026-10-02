//Copyright (c) 2016-2025 Kai Clavier [kaiclavier.com] Do Not Distribute
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Gradient Data", menuName = "Super Text Mesh/Gradient Data", order = 1)]
public class STMGradientData : STMBaseData{
	//public string name;
	public Gradient gradient;
	public float gradientSpread = 0.1f;
	public float scrollSpeed = 0.0f; //can be negative, or 0
	public GradientDirection direction = GradientDirection.Horizontal; //could vertical gradients be improved? position consistent?
	public enum GradientDirection{
		Horizontal,
		Vertical
	}
	public bool smoothGradient = true;
	
	public enum ColorMode
	{
		Normal,
		AlphaComposited,
		Multiply
	}
	[Tooltip("Normal: This gradient will set text color directly. " +
	         "AlphaComposited: The gradient will blend as if on another layer in an art program. " +
	         "Multiply: The gradient will multiply by text color.")]
	public ColorMode colorMode = ColorMode.Normal;

	private Color _temp;
	public Color32 Evaluate(Color originalColor, bool blendColor, bool blendAlpha, ColorMode thisColorMode, float time)
	{
		if(thisColorMode == ColorMode.AlphaComposited)
		{
			_temp = gradient.Evaluate(time);
			_temp.r = (_temp.a * _temp.r) + (originalColor.a * (1f - _temp.a) * originalColor.r);
			_temp.g = (_temp.a * _temp.g) + (originalColor.a * (1f - _temp.a) * originalColor.g);
			_temp.b = (_temp.a * _temp.b) + (originalColor.a * (1f - _temp.a) * originalColor.b);
			_temp.a = _temp.a + originalColor.a * (1f - _temp.a);
			if(!blendColor)
			{
				_temp.r = originalColor.r;
				_temp.g = originalColor.g;
				_temp.b = originalColor.b;
			}
			if(!blendAlpha)
			{
				_temp.a = originalColor.a;
			}
			return _temp;
		}
		else if(thisColorMode == ColorMode.Multiply)
		{
			_temp = gradient.Evaluate(time) * originalColor;
			if(!blendColor)
			{
				_temp.r = originalColor.r;
				_temp.g = originalColor.g;
				_temp.b = originalColor.b;
			}
			if(!blendAlpha)
			{
				_temp.a = originalColor.a;
			}
			return _temp;
		}

		_temp = gradient.Evaluate(time);
		if(!blendColor)
		{
			_temp.r = originalColor.r;
			_temp.g = originalColor.g;
			_temp.b = originalColor.b;
		}
		if(!blendAlpha)
		{
			_temp.a = originalColor.a;
		}
		return _temp;
	}
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
		SerializedProperty gradient = serializedData.FindProperty("gradient");
		SerializedProperty colorMode = serializedData.FindProperty("colorMode");
		SerializedProperty gradientSpread = serializedData.FindProperty("gradientSpread");
		SerializedProperty scrollSpeed = serializedData.FindProperty("scrollSpeed");
		SerializedProperty direction = serializedData.FindProperty("direction");
		SerializedProperty smoothGradient = serializedData.FindProperty("smoothGradient");
	//Title bar:
		STMCustomInspectorTools.DrawTitleBar(this,stm,data);
	//the rest:
		EditorGUILayout.PropertyField(gradient);
		EditorGUILayout.PropertyField(colorMode);
		EditorGUILayout.PropertyField(gradientSpread);
		EditorGUILayout.PropertyField(scrollSpeed);
		EditorGUILayout.PropertyField(direction);
		EditorGUILayout.PropertyField(smoothGradient);
	}
	#endif
}