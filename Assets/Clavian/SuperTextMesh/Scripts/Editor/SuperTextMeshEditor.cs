//Copyright (c) 2025-2026 Kai Clavier [kaiclavier.com] Do Not Distribute
#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Clavian.STM.Editor;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;


[CustomEditor(typeof(SuperTextMesh))]
[CanEditMultipleObjects] //sure why not
public class SuperTextMeshEditor : Editor
{
	private SuperTextMesh stm;
	private Rect r;
	private GUIStyle foldoutStyle = null;
	private GUIStyle textDataStyle = null;
	private Texture2D textDataIcon = null;
	private Rect tempRect;
	private GUIContent editTextDataContent = new GUIContent("");
	private bool textDataEditMode; //used to be stm.data.textDataEditMode
#if UNITY_2017_1_OR_NEWER
	private bool constantRepaint;

	public override bool RequiresConstantRepaint()
	{
		return constantRepaint;
	}
#endif
	private void OnEnable()
	{
		Undo.undoRedoPerformed += OnUndoRedo;
	}

	private void OnDisable()
	{
		Undo.undoRedoPerformed -= OnUndoRedo;
#if UNITY_2017_1_OR_NEWER
		if(stm != null && stm.data != null && stm.data.previewData != null)
		{
			stm.data.ClosePreview();
		}
#endif
	}

	override public void OnInspectorGUI(){
		serializedObject.Update(); //for onvalidate stuff!
		stm = target as SuperTextMesh; //get this text mesh as a component
#if UNITY_2017_1_OR_NEWER
		constantRepaint = stm.data.previewData != null;
#endif
	//Actually Drawing it to the inspector:
		r = GUILayoutUtility.GetRect(EditorGUIUtility.fieldWidth, 0f); //get width on inspector, minus scrollbar

		if(foldoutStyle == null)
		{
			foldoutStyle = new GUIStyle(EditorStyles.foldout); //create a new foldout style, for the bold foldout headers
			foldoutStyle.fontStyle = FontStyle.Bold; //set it to look like a header
		}
	//TEXT DATA ICON
		//Object textDataObject = stm.data; //get text data object
		if(textDataStyle == null)
		{
			textDataStyle = new GUIStyle(EditorStyles.label);
		}
		//textDataStyle.fixedWidth = 14;
		//textDataStyle.fixedHeight = 14;
		//Get Texture2D one of these two ways:
		//Texture2D textDataIcon = AssetDatabase.LoadAssetAtPath("Assets/Clavian/SuperTextMesh/Scripts/SuperTextMeshDataIcon.png", typeof(Texture2D)) as Texture2D;
		if(textDataIcon == null)
		{
			textDataIcon = EditorGUIUtility.ObjectContent(stm.data, typeof(SuperTextMeshData)).image as Texture2D;
			textDataStyle.normal.background = textDataIcon; //apply
			textDataStyle.active.background = textDataIcon;
		}

		editTextDataContent.tooltip = "Edit TextData";
		tempRect.Set(r.width - 2, r.y, 16, 16);
		if(GUI.Button(tempRect, editTextDataContent, textDataStyle)){ //place at exact spot
			//EditorWindow.GetWindow()
			//EditorUtility.FocusProjectWindow();
			//Selection.activeObject = textDataObject; //go to textdata!
			//EditorGUIUtility.PingObject(textDataObject);
			textDataEditMode = !textDataEditMode; //show textdata inspector!
			//if(textDataEditMode){
			//	= null;
			//}
#if UNITY_2017_1_OR_NEWER
			if(textDataEditMode == false)
			{
				if(stm.data.previewData != null)
				{
					stm.data.ClosePreview();
				}
			}
#endif
		}

		if(!textDataEditMode)
		{
			tempRect.Set(r.width - 184, r.y, 180, 16);
			GUI.Label(tempRect, "Click here to edit text data! ->");
		}

		if(textDataEditMode){//show textdata file instead
			
			if(!SuperTextMeshWindow.IsOpen)
			{
				//editTextDataContent.tooltip = "Edit TextData (Pop Out)";
				tempRect.Set(r.width - 124, r.y, 120, 16);
				if(GUI.Button(tempRect, "Pop Out Editor"))
				{
					//place at exact spot
					SuperTextMeshWindow.ShowWindow();
					
					//and go back
					textDataEditMode = false;
				}
			}
		//Draw it!
			EditorGUILayout.Space(); //////////////////SPACE
			EditorGUILayout.Space(); //////////////////SPACE
			EditorGUILayout.Space(); //////////////////SPACE
			EditorGUILayout.HelpBox("Editing Text Data. Click the [T] to exit!", MessageType.None, true);

			SuperTextMeshWindow.DrawGUIFor(stm, stm.data);
			

			
		}else{ //draw actual text mesh inspector:

			#if UNITY_2022_1_OR_NEWER
			if(stm.t.GetComponentInParent<Canvas>(true) != null && !stm.uiMode)
			#else
			if(stm.t.GetComponentInParent<Canvas>() != null && !stm.uiMode)
			#endif
			{
				EditorGUILayout.Space();
				EditorGUILayout.Space();
				EditorGUILayout.Space();
				EditorGUILayout.HelpBox("Configuration Error! GameObject is child of a canvas, but does not have a RectTransform.", MessageType.Error);
				if(GUILayout.Button("Click here to fix!"))
				{
					stm.gameObject.AddComponent<RectTransform>();
					//also, fix components too...
					DestroyImmediate(stm.r);
					DestroyImmediate(stm.f);
					if(stm.c == null)
					{
							
					}
				}
			}
			#if UNITY_2022_1_OR_NEWER
			if(stm.t.GetComponentInParent<Canvas>(true) == null && stm.uiMode)
			#else
			if(stm.t.GetComponentInParent<Canvas>() == null && stm.uiMode)
			#endif
			{
				EditorGUILayout.Space();
				EditorGUILayout.Space();
				EditorGUILayout.Space();
				EditorGUILayout.HelpBox("Configuration Error! GameObject is not child of a canvas, but has a RectTransform.", MessageType.Error);
				if(GUILayout.Button("Click here to fix!"))
				{
					stm.gameObject.AddComponent<Transform>();
					//fix components as well
					DestroyImmediate(stm.c);
					if(stm.r == null)
					{
							
					}
					if(stm.f == null)
					{
							
					}
				}
			}
			if(stm.t.GetComponent<CanvasRenderer>() != null && (stm.t.GetComponent<MeshRenderer>() != null || stm.t.GetComponent<MeshFilter>() != null))
			{
				EditorGUILayout.Space();
				EditorGUILayout.Space();
				EditorGUILayout.Space();
				EditorGUILayout.HelpBox("Configuration Error! Mesh has more than one renderer.", MessageType.Error);
				if(GUILayout.Button("Click here to fix!"))
				{
					if(stm.uiMode)
					{
						DestroyImmediate(stm.r);
						DestroyImmediate(stm.f);
						if(stm.c == null)
						{
							
						}
					}
					else
					{
						DestroyImmediate(stm.c);
						if(stm.r == null)
						{
							
						}
						if(stm.f == null)
						{
							
						}
					}
				}
			}
			Font oldFont = GUI.skin.font;
			if(stm.data != null && stm.data.inspectorFont != null)
			{
				GUI.skin.font = stm.data.inspectorFont;
			}
			EditorGUILayout.PropertyField(serializedObject.FindProperty("_text"));
			GUI.skin.font = oldFont;
/*			
			EditorGUILayout.LabelField("Text");
			Vector2 scroll = Vector2.zero;
			scroll = EditorGUILayout.BeginScrollView(scroll, false, true);
			GUIStyle textAreaStyle = new GUIStyle(GUI.skin.textArea);
			stm._text = EditorGUILayout.TextArea(stm._text, textAreaStyle, new GUILayoutOption[]{GUILayout.MinHeight(80), GUILayout.MaxHeight(200), GUILayout.ExpandHeight(false), GUILayout.Width(Screen.width - 50)});
			EditorGUILayout.EndScrollView();
*/
			stm.showAppearanceFoldout = EditorGUILayout.Foldout(stm.showAppearanceFoldout, "Appearance", foldoutStyle);
			if(stm.showAppearanceFoldout)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("font"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("_color")); //richtext default stuff...
				EditorGUILayout.PropertyField(serializedObject.FindProperty("_fade"));
				//stm.color = EditorGUILayout.ColorField("Color", stm.color);
				if(stm.bestFit == SuperTextMesh.BestFitMode.Always)//no editing value
				{ 
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.FloatField("Size", stm.size * stm.bestFitMulti);
					EditorGUI.EndDisabledGroup();
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PropertyField(serializedObject.FindProperty("minSize"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSize"));
					EditorGUILayout.EndHorizontal();
				}
				else if(stm.bestFit == SuperTextMesh.BestFitMode.SquishAlways)
				{
					//EditorGUILayout.BeginHorizontal();
					//EditorGUILayout.PropertyField(serializedObject.FindProperty("size"));
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.FloatField("Size", stm.size * stm.bestFitMulti);
					EditorGUI.EndDisabledGroup();
					//EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PropertyField(serializedObject.FindProperty("minSize"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSize"));
					EditorGUILayout.EndHorizontal();
				}
				else if(stm.bestFit == SuperTextMesh.BestFitMode.OverLimit)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PropertyField(serializedObject.FindProperty("size"));
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.FloatField(stm.size * stm.bestFitMulti);
					EditorGUI.EndDisabledGroup();
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PropertyField(serializedObject.FindProperty("minSize"));
					EditorGUILayout.EndHorizontal();
				}
				else if(stm.bestFit == SuperTextMesh.BestFitMode.SquishOverLimit)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PropertyField(serializedObject.FindProperty("size"));
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.FloatField(stm.size * stm.bestFitMulti);
					EditorGUI.EndDisabledGroup();
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PropertyField(serializedObject.FindProperty("minSize"));
					EditorGUILayout.EndHorizontal();
				}
				else if(stm.bestFit == SuperTextMesh.BestFitMode.MultilineEven || stm.bestFit == SuperTextMesh.BestFitMode.MultilineFull)
				{ 
					//EditorGUILayout.BeginHorizontal();
					//EditorGUILayout.PropertyField(serializedObject.FindProperty("size"));
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.FloatField("Size", stm.size * stm.bestFitMulti);
					EditorGUI.EndDisabledGroup();
					//EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PropertyField(serializedObject.FindProperty("minSize"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSize"));
					EditorGUILayout.EndHorizontal();
					//EditorGUILayout.PropertyField(serializedObject.FindProperty("multilineRatio"));
				}
				else //no best fit value
				{
					EditorGUILayout.PropertyField(serializedObject.FindProperty("size"));
				}
				EditorGUILayout.PropertyField(serializedObject.FindProperty("bestFit"));

				if(stm.font != null)
				{
					EditorGUI.BeginDisabledGroup(!stm.font.dynamic);
					EditorGUILayout.PropertyField(serializedObject.FindProperty("style"));
					EditorGUI.EndDisabledGroup();
				}
				EditorGUILayout.PropertyField(serializedObject.FindProperty("richText"));

				EditorGUILayout.Space(); //////////////////SPACE
				
				if(stm.font != null)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUI.BeginDisabledGroup((!stm.font.dynamic && stm.font.fontSize != 0) || stm.autoQuality);
					EditorGUILayout.PropertyField(serializedObject.FindProperty("quality")); //text rendering
					EditorGUI.EndDisabledGroup();
					if(stm.uiMode)
					{
						EditorGUILayout.PropertyField(serializedObject.FindProperty("autoQuality"));
					}
					EditorGUILayout.EndHorizontal();
				}
				EditorGUILayout.PropertyField(serializedObject.FindProperty("filterMode"));
				if(!stm.uiMode)
				{
					SerializedObject sr = new SerializedObject(stm.r);
					if(sr != null)
					{
						sr.Update();
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PropertyField(sr.FindProperty("m_CastShadows"));
						//this works, but hiding for now since most included shaders are unlit:
						//EditorGUILayout.PropertyField(sr.FindProperty("m_ReceiveShadows"));
						EditorGUILayout.EndHorizontal();
                        sr.ApplyModifiedProperties();
					}
					
				}
				else
				{
					//masking options!
				}
				
				if(stm.textMaterial.GetTag("STMMaskingSupport", true, "Null") == "Yes")
				{
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField(serializedObject.FindProperty("maskMode"));
					//stm.maskMode = (SuperTextMesh.MaskMode)EditorGUILayout.EnumPopup(new GUIContent("Mask Mode", 
					//		"This value controls how UI Text reacts to the 'Mask' component, and how non-UI text reacts to the 'Sprite Mask' component. It has no effect on RectMask2D."), 
					//	stm.maskMode);
					if(EditorGUI.EndChangeCheck())
					{
						//EditorUtility.SetDirty(stm);
						serializedObject.ApplyModifiedProperties();
						//reapply material
						//not needed anymore...?
						//stm.RecalculateMasking();
						
						//serializedObject.ApplyModifiedProperties();
						return;
						//probably need to do a full rebuild, since that masking recalculation is just for UI mode
						//stm.Rebuild();
					}
					
					

					if(stm.uiMode && stm.gameObject.GetComponent<STMMaskableGraphic>() == null)
					{
						//if there is a material modifier or mesh modifier, but no maskablegraphic, show a warning.
						if(stm.gameObject.GetComponent<IMaterialModifier>() != null ||
						   stm.gameObject.GetComponent<IMeshModifier>() != null)
						{
							EditorGUILayout.HelpBox("This Super Text Mesh object has an IMaterialModifier or " +
							                        "IMeshModifier component (Shadow, Outline, etc.), but these will not " +
							                        "work without a STMMaskableGraphic component! Please press the button " +
							                        "below to remedy this!", MessageType.Error);
						}
						if(GUILayout.Button("Enable support for RectMask2D & UI Effects"))
						{
							Undo.AddComponent<STMMaskableGraphic>(stm.gameObject);
						}
					}
				}
				
				//EditorGUILayout.BeginHorizontal();
				//if(GUILayout.Button("Ping")){
					//EditorUtility.FocusProjectWindow();
				//	EditorGUIUtility.PingObject(stm.textMaterial); //select this object
				//}
		//Materials
				EditorGUILayout.BeginHorizontal();
				if(GUILayout.Button("New"))
				{
					//Debug.Log(ClavianPath);

					//create new material in the correct folder
					//give it the correct default shader
					//assign it to this text mesh
					string whatShader = stm.uiMode ? "Super Text Mesh/Standard/Default" : "Super Text Mesh/Standard/Default";
					Material newMaterial = new Material(Shader.Find(whatShader));
					if(!AssetDatabase.IsValidFolder(STMCustomInspectorTools.ClavianPath + "Materials")){
						//create folder if it doesn't exist yet
						AssetDatabase.CreateFolder(STMCustomInspectorTools.ClavianPath.Remove(STMCustomInspectorTools.ClavianPath.Length - 1), "Materials");
					}
					AssetDatabase.CreateAsset(newMaterial, AssetDatabase.GenerateUniqueAssetPath(STMCustomInspectorTools.ClavianPath + "Materials/NewMaterial.mat"));
					stm.textMaterial = newMaterial;
				}
				EditorGUILayout.PropertyField(serializedObject.FindProperty("textMaterial")); //appearance
				EditorGUILayout.EndHorizontal();
				//EditorGUILayout.EndHorizontal();

				if(stm.textMaterial != null){
					stm.showMaterialFoldout = EditorGUILayout.Foldout(stm.showMaterialFoldout, "Material", foldoutStyle);
					if(stm.showMaterialFoldout){ //show shader settings
						EditorGUI.BeginChangeCheck();
						//Undo.RecordObject(stm, "Changed Super Text Mesh Material");
						STMCustomInspectorTools.DrawMaterialEditor(stm.textMaterial, stm);
						if(EditorGUI.EndChangeCheck())
						{
							stm.Rebuild();
						}
					}
				}
			}

			//EditorGUILayout.Space(); //////////////////SPACE
			stm.showPositionFoldout = EditorGUILayout.Foldout(stm.showPositionFoldout, "Position", foldoutStyle);
			if(stm.showPositionFoldout){
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PropertyField(serializedObject.FindProperty("baseOffset")); //physical stuff
				EditorGUILayout.LabelField("Relative", GUILayout.MaxWidth(50f));
				stm.relativeBaseOffset = EditorGUILayout.Toggle(stm.relativeBaseOffset, GUILayout.MaxWidth(16f));
				EditorGUILayout.EndHorizontal();
				if(stm.uiMode){
					string[] anchorNames = new string[]{"Top", "Middle", "Bottom"};
					int[] anchorValues = new int[]{0,3,6};
					EditorGUI.BeginChangeCheck();
					int resultEnumValue = EditorGUILayout.IntPopup("Anchor", (int)Mathf.Floor((float)stm.anchor / 3f) * 3, anchorNames, anchorValues);
					if(EditorGUI.EndChangeCheck())
					{
						serializedObject.FindProperty("anchor").enumValueIndex = resultEnumValue;
					}
				}else{
					EditorGUILayout.PropertyField(serializedObject.FindProperty("anchor"));
				}
				//if(!uiMode.boolValue){ //restrict this to non-ui only...?
					EditorGUILayout.PropertyField(serializedObject.FindProperty("alignment"));

				//}
				EditorGUILayout.Space(); //////////////////SPACE
				EditorGUILayout.PropertyField(serializedObject.FindProperty("lineSpacing")); //text formatting
				EditorGUILayout.PropertyField(serializedObject.FindProperty("characterSpacing"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("tabSize"));
				EditorGUILayout.Space(); //////////////////SPACE
				if(!stm.uiMode){ //wrapping text works differently for UI:
					EditorGUILayout.PropertyField(serializedObject.FindProperty("autoWrap")); //automatic...
					if(stm.autoWrap > 0f){
						EditorGUILayout.PropertyField(serializedObject.FindProperty("breakText"));
					//	EditorGUILayout.PropertyField(serializedObject.FindProperty("smartBreak"));
						EditorGUILayout.PropertyField(serializedObject.FindProperty("insertHyphens"));
					}
					EditorGUILayout.PropertyField(serializedObject.FindProperty("verticalLimit"));
					if(stm.verticalLimit > 0f){
						EditorGUILayout.PropertyField(serializedObject.FindProperty("verticalLimitMode"));
					}
				}else{
					EditorGUILayout.PropertyField(serializedObject.FindProperty("uiWrap"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("uiLimit"));
					EditorGUILayout.Space(); //////////////////SPACE
					EditorGUILayout.PropertyField(serializedObject.FindProperty("breakText"));
				//	EditorGUILayout.PropertyField(serializedObject.FindProperty("smartBreak"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("insertHyphens"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("verticalLimitMode"));
				}
			}
			//EditorGUILayout.Space(); //////////////////SPACE
			stm.showTimingFoldout = EditorGUILayout.Foldout(stm.showTimingFoldout, "Timing", foldoutStyle);
			if(stm.showTimingFoldout){
				EditorGUILayout.PropertyField(serializedObject.FindProperty("ignoreTimeScale"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("disableAnimatedText"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("forceAnimation"));
				EditorGUILayout.Space(); //////////////////SPACE
				
				EditorGUI.BeginDisabledGroup(stm.drawOrder == SuperTextMesh.DrawOrder.AllAtOnce);
				EditorGUILayout.PropertyField(serializedObject.FindProperty("readDelay")); //technical stuff
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(stm.readDelay == 0f);

				EditorGUILayout.PropertyField(serializedObject.FindProperty("autoRead"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("rememberReadPosition"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("drawOrder"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("drawAnimName"));
				//stuff that needs progamming to work:
				stm.showFunctionalityFoldout = EditorGUILayout.Foldout(stm.showFunctionalityFoldout, "Functionality", foldoutStyle);
				if(stm.showFunctionalityFoldout){
					EditorGUILayout.PropertyField(serializedObject.FindProperty("speedReadScale"));
					EditorGUILayout.Space(); //////////////////
                    EditorGUI.BeginDisabledGroup(stm.undrawOrder == SuperTextMesh.DrawOrder.AllAtOnce);
					EditorGUILayout.PropertyField(serializedObject.FindProperty("unreadDelay"));
					EditorGUI.EndDisabledGroup();
					EditorGUILayout.PropertyField(serializedObject.FindProperty("undrawOrder"));
					EditorGUILayout.PropertyField(serializedObject.FindProperty("undrawAnimName"));
				}
				//GUIContent drawAnimLabel = new GUIContent("Draw Animation", "What draw animation will be used. Can be customized with TextData.");
				//selectedAnim.intValue = EditorGUILayout.Popup("Draw Animation", selectedAnim.intValue, stm.DrawAnimStrings());
				stm.showAudioFoldout = EditorGUILayout.Foldout(stm.showAudioFoldout, "Audio", foldoutStyle);
				if(stm.showAudioFoldout){
					//EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel); //HEADER
					EditorGUILayout.PropertyField(serializedObject.FindProperty("audioSource"));
					if(stm.audioSource != null){ //flag
						EditorGUILayout.PropertyField(serializedObject.FindProperty("audioClips"), true); //yes, show children
						EditorGUILayout.PropertyField(serializedObject.FindProperty("stopPreviousSound"));
						EditorGUILayout.PropertyField(serializedObject.FindProperty("pitchMode"));
						if(stm.pitchMode == SuperTextMesh.PitchMode.Normal){
							//nothing!
						}
						else if(stm.pitchMode == SuperTextMesh.PitchMode.Single){
							EditorGUILayout.PropertyField(serializedObject.FindProperty("overridePitch"));
						}
						else{ //random between two somethings
							EditorGUILayout.PropertyField(serializedObject.FindProperty("minPitch"));
							EditorGUILayout.PropertyField(serializedObject.FindProperty("maxPitch"));
						}
						if(stm.pitchMode == SuperTextMesh.PitchMode.Perlin){
							EditorGUILayout.PropertyField(serializedObject.FindProperty("perlinPitchMulti"));
						}
						if(stm.speedReadScale < 1000f){
							EditorGUILayout.PropertyField(serializedObject.FindProperty("speedReadPitch"));
						}
					}
				}
				EditorGUI.EndDisabledGroup();
			}
			stm.showEventFoldout = EditorGUILayout.Foldout(stm.showEventFoldout, "Events", foldoutStyle);
			if(stm.showEventFoldout){
				EditorGUILayout.PropertyField(serializedObject.FindProperty("onRebuildEvent"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("onPrintEvent"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("onCompleteEvent"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("onUndrawnEvent"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("onVertexMod"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("onPreParse"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("onCustomEvent"));
			}

			stm.showBetaFoldout = EditorGUILayout.Foldout(stm.showBetaFoldout, "Beta", foldoutStyle);
			if(stm.showBetaFoldout)
			{
				EditorGUILayout.PropertyField(serializedObject.FindProperty("useSubmeshObjects")); //physical stuff
				EditorGUILayout.PropertyField(serializedObject.FindProperty("rtl"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("removeEmoji"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("rebuildOnRectChange"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultWaveData"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultJitterData"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultColorData"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultGradientData"));
				EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultTextureData"));
				/*
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PropertyField(serializedObject.FindProperty("validateMesh"));
				if(!stm.validateMesh)
				{
					if(GUILayout.Button("Validate"))
					{
						//stm.OnValidate();
						stm.OnValidate();
						stm.validateAppearance = true;
						stm.Update();
					}
				}
				EditorGUILayout.EndHorizontal();
				*/
				//EditorGUILayout.PropertyField(serializedObject.FindProperty("debugMode"));
				if(stm.debugMode)
				{
					//EditorGUILayout.IntField("Submesh Count", stm.submeshes.Count);
					//EditorGUILayout.IntField("Textmesh Submeshes", stm.textMesh.subMeshCount);
					/*
					 * submesh count is right
					 * materials are right
					 * uvs are right for sure...
					 * tris? lets see... yeah it looks correct, too, pointing to the right verts from the right submesh
					 * the mesh is correct, so maybe its sending info to the shader wrong...?
					 * like the material in the shader isn't showing the right texture on preview, is that a clue?
					 *
					 * if i revert the shader code, maskablegraphic will sometimes save STm and allow it to render
					 */
				}
			}

			//EditorGUILayout.Space(); //////////////////SPACE
			
		}

		serializedObject.ApplyModifiedProperties();
	}

	private void OnUndoRedo()
	{
		
		//AssetDatabase.SaveAssets();
		//AssetDatabase.Refresh();
		if(stm != null && stm.data != null)
			stm.data.RebuildDictionaries(); //make this refresh, too
	}

	private void OnUpdateMeshAnimation()
	{
		stm.SetMesh(Time.time);
	}
}
#endif