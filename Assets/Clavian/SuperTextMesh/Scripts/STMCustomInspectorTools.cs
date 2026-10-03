//Copyright (c) 2016-2026 Kai Clavier [kaiclavier.com] Do Not Distribute
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
using System.Linq; //for checking keywords array
using Clavian.STM.Tools;

public static class STMCustomInspectorTools {
	public static void DrawTitleBar(Object myObject, SuperTextMesh stm, SuperTextMeshData data){
		if(myObject != null){
			EditorGUILayout.BeginHorizontal();
		//ping button:
			if(GUILayout.Button("Ping")){
				//EditorUtility.FocusProjectWindow(); this doesn't work for some reason
				EditorGUIUtility.PingObject(myObject); //select this object
			}
		//name:
			EditorGUI.BeginChangeCheck();
			myObject.name = EditorGUILayout.DelayedTextField(myObject.name);
			if(EditorGUI.EndChangeCheck()){
				AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(myObject), myObject.name);
				//Undo.RecordObject (myObject, "Change Asset Name");
				AssetDatabase.Refresh();
				if(data != null)
					data.RebuildDictionaries();
			}
		//delete button:
			if(GUILayout.Button("X"))
			{
				if(data != null)
					Undo.RecordObject(data, "Deleted Data");
				//var path = AssetDatabase.GetAssetPath(myObject);
				
				Undo.DestroyObjectImmediate(myObject);
				//AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(myObject));
				//AssetDatabase.DeleteAsset(path);
				//AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(myObject));
				AssetDatabase.MoveAssetToTrash(AssetDatabase.GetAssetPath(myObject));
				
				//Undo.DestroyObjectImmediate(myObject);
				AssetDatabase.Refresh();
				if(data != null)
					//data = null; //make this refresh, too
					data.RebuildDictionaries();
			}
			EditorGUILayout.EndHorizontal();
		}
	}
	public static string ClavianPath
    {
        get
        {
            string searchValue = "Clavian/SuperTextMesh/";
            string returnPath = "";
            string[] allPaths = AssetDatabase.GetAllAssetPaths();
            for (int i = 0; i < allPaths.Length; i++)
            {
                if (allPaths[i].Contains(searchValue))
                {
                    // This is the path we want! Let's strip out everything after the searchValue
                    returnPath = allPaths[i];
                    returnPath = returnPath.Remove(returnPath.IndexOf(searchValue));
                    returnPath += searchValue;
					break;
                }
            }

            return returnPath;
        }
    }
	/*
	public static void OnUndoRedo(){
		AssetDatabase.Refresh();
	}
	*/
	// public static void FinishItem(UnityEngine.Object myObject){
	//
	// }
	// public static void DrawCreateFolderButton(string buttonText, string parentFolder, string newFolder, SuperTextMesh stm){
	// 	if(GUILayout.Button(buttonText)){
	// 		AssetDatabase.CreateFolder(ClavianPath + "Resources/" + parentFolder, newFolder);
	// 		AssetDatabase.Refresh();
	// 		if(stm != null)
	// 			stm.data = null;
	// 	}
	// }
	public static void DrawCreateNewButton(string buttonText, string folderName, string typeName, SuperTextMesh stm,
		SuperTextMeshData data){
		if(GUILayout.Button(buttonText)){
			ScriptableObject newData = NewData(typeName);
			if(newData != null){
				AssetDatabase.CreateAsset(newData,AssetDatabase.GenerateUniqueAssetPath(ClavianPath + "Resources/" + folderName)); //save to file
				//Undo.undoRedoPerformed += OnUndoRedo; //subscribe to event
				Undo.RegisterCreatedObjectUndo(newData, buttonText);
				AssetDatabase.Refresh();
				if(data != null)
					data.RebuildDictionaries();
			}
		}
	}
	public static ScriptableObject NewData(string myType){
		switch(myType){
			case "STMAudioClipData": return ScriptableObject.CreateInstance<STMAudioClipData>();
			case "STMAutoClipData": return ScriptableObject.CreateInstance<STMAutoClipData>();
			case "STMColorData": return ScriptableObject.CreateInstance<STMColorData>();
			case "STMDelayData": return ScriptableObject.CreateInstance<STMDelayData>();
			case "STMDrawAnimData": return ScriptableObject.CreateInstance<STMDrawAnimData>();
			case "STMFontData": return ScriptableObject.CreateInstance<STMFontData>();
			case "STMGradientData": return ScriptableObject.CreateInstance<STMGradientData>();
			case "STMJitterData": return ScriptableObject.CreateInstance<STMJitterData>();
			case "STMMaterialData": return ScriptableObject.CreateInstance<STMMaterialData>();
			case "STMQuadData": return ScriptableObject.CreateInstance<STMQuadData>();
			case "STMSoundClipData": return ScriptableObject.CreateInstance<STMSoundClipData>();
			case "STMTextureData": return ScriptableObject.CreateInstance<STMTextureData>();
			case "STMVoiceData": return ScriptableObject.CreateInstance<STMVoiceData>();
			case "STMWaveData": return ScriptableObject.CreateInstance<STMWaveData>();
			default: Debug.Log("New data type unknown."); return null;
		}
	}

	public static bool ShaderFeatureToggle(Material mat, string featureName, string variableName, string label)
	{
		bool enabled = mat.IsKeywordEnabled(featureName);
		EditorGUI.BeginChangeCheck();
		enabled = EditorGUILayout.Toggle(label, enabled);//show the toggle
		if(EditorGUI.EndChangeCheck())
		{
			mat.SetInt(variableName, enabled ? 1 : 0); //call this too so newer unity versions don't break
			if(enabled)
			{
				mat.EnableKeyword(featureName);
			}
			else
			{
				mat.DisableKeyword(featureName);
			}
		}
		return enabled;
	}
	public static bool ShaderFeatureToggle(Material mat, string featureName, int variableId, string label)
	{
		bool enabled = mat.IsKeywordEnabled(featureName);
		EditorGUI.BeginChangeCheck();
		enabled = EditorGUILayout.Toggle(label, enabled);//show the toggle
		if(EditorGUI.EndChangeCheck())
		{
			mat.SetInt(variableId, enabled ? 1 : 0); //call this too so newer unity versions don't break
			if(enabled)
			{
				mat.EnableKeyword(featureName);
			}
			else
			{
				mat.DisableKeyword(featureName);
			}
		}
		return enabled;
	}
	public static void DrawMaterialEditor(Material mat, SuperTextMesh stm){
		//Just set these directly, why not. It's a custom inspector already, no need to bog this down even more
		Undo.RecordObject(mat, "Changed Super Text Mesh Material");
		//name changer
		EditorGUI.BeginChangeCheck();
		var x = EditorGUILayout.DelayedTextField("Material Name", mat.name);
		if(EditorGUI.EndChangeCheck()){
			AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(mat), x);
			//Undo.RecordObject (myObject, "Change Asset Name");
			AssetDatabase.Refresh();
			//stm.data = null;
		}

		int originalQueue = mat.renderQueue;
		EditorGUILayout.BeginHorizontal();
		//popup...
		GUIContent[] shaders = new GUIContent[] {new GUIContent("--Shader--"), 
												new GUIContent("Standard/Default"), //1
												new GUIContent("Standard/Ultra"), //2
												new GUIContent("URP/Default"), //3
												new GUIContent("URP/Ultra") //4
		};
		int newShaderIndex =
			EditorGUILayout.Popup(
				new GUIContent("", "Other shaders can be assigned, these are just the standard ones."), 0, shaders,
				GUILayout.Width(EditorGUIUtility.labelWidth));
		
		switch(newShaderIndex)
		{
			case 1:
				mat.shader = Shader.Find("Super Text Mesh/Standard/Default");
				break;
			case 2:
				mat.shader = Shader.Find("Super Text Mesh/Standard/Ultra");
				break;
			case 3:
				mat.shader = Shader.Find("Super Text Mesh/URP/Default");
				break;
			case 4:
				mat.shader = Shader.Find("Super Text Mesh/URP/Ultra");
				break;
		}
		
		// if(GUILayout.Button("Universal"))
		// {
		// 	mat.shader = Shader.Find("Super Text Mesh/Universal/Default");
		// }
		// if(GUILayout.Button("Ultra"))
		// {
		// 	mat.shader = Shader.Find("Super Text Mesh/Ultra");
		// }
		mat.shader = (Shader)EditorGUILayout.ObjectField("", mat.shader, typeof(Shader), false);
		EditorGUILayout.EndHorizontal();
		//set to correct value
		if(mat.HasProperty(ShaderIds.CUTOFF)){
			mat.SetFloat(ShaderIds.CUTOFF,0.0001f);
		}
		//set to correct value
		if(mat.HasProperty(ShaderIds.SHADOW_CUTOFF)){
			mat.SetFloat(ShaderIds.SHADOW_CUTOFF,0.5f);
		}

		//culling mode
		if(mat.HasProperty(ShaderIds.CULL_MODE)){
			UnityEngine.Rendering.CullMode cullMode = (UnityEngine.Rendering.CullMode)mat.GetInt(ShaderIds.CULL_MODE);
			cullMode = (UnityEngine.Rendering.CullMode)EditorGUILayout.EnumPopup("Cull Mode", cullMode);
			mat.SetInt(ShaderIds.CULL_MODE, (int)cullMode);
		}
		//draw on top? dont show for UI mode, where this is set differently based on canvas
		//actually, leaving this enabled for canvas for the time being, as having an override for this can potentially
		//fix some stuff with the Ultra shader. 2025-02-10
		if(stm != null)
		{
			if(mat.HasProperty(ShaderIds.Z_TEST_MODE))
			{
				int zTestMode = mat.GetInt(ShaderIds.Z_TEST_MODE);
				bool onTop = zTestMode == 8;
				onTop = EditorGUILayout.Toggle("Render On Top", onTop);
				//Always or LEqual //right now this is 6 and 2, but shouldn't it be 8 and 4??
				mat.SetInt(ShaderIds.Z_TEST_MODE, onTop ? 8 : 4);
			}

			if(mat.HasProperty(ShaderIds.Z_WRITE_MODE))
			{
				mat.SetInt(ShaderIds.Z_WRITE_MODE,
					//"Controls if this material writes to the ZBuffer or not. 
					EditorGUILayout.Popup("ZWrite",
						mat.GetInt(ShaderIds.Z_WRITE_MODE),
						new string[] { "Off", "On" }));
			}
		}
		
		
		/* 
		//masking
		if(mat.HasProperty("_MaskMode")){
			int maskMode = mat.GetInt("_MaskMode");
			//bool masked = maskMode == 1;
			//masked = EditorGUILayout.Toggle("Masked", masked);
			maskMode = EditorGUILayout.Popup("Mask Mode", maskMode, new string[]{"Outside","Inside"});
			//Always or LEqual
			mat.SetInt("_MaskMode", maskMode);
		}
		*/
		if(mat.GetTag("STMUberShader2", true, "Null") == "Yes")
		{
			//GUILayout.Label("Ompuco shader detected!");
			/*
			 * layers:
			 * settings that effect all:
			 *	sdf mode
			 *	pixel snap
			 *  offset space [relative to scale, relative to text]
			 *
			 * not shownnn but it's a layer:
			 * text
			 *  color
			 *
			 * outline
			 *  enabled
			 *  color
			 *  type [circle, square]
			 *  points (1-32)
			 *  (maybe some buttons here for 8-pt and 4pt pixel, and default)
			 *  distance/width
			 *  extrude boolean?
			 *  
			 *  
			 *
			 * blur? outline 2?
			 *
			 * dropshadow
			 *  enabled
			 *  color
			 *	extrude boolean?
			 *  
			 *  type [angle, position]
			 *  if angle...
			 *     angle
			 *     distance
			 *  if position
			 *     vector3 shadow
			 *	blur?
			 *
			 * maybe blur could be if the outline is additive or not?
			 */
			
			//extra copy of this, just for ultra shader, which can appear on UI.
			// if(stm.uiMode && mat.HasProperty(ShaderIds.Z_TEST_MODE))
			// {
			// 	int zTestMode = mat.GetInt(ShaderIds.Z_TEST_MODE);
			// 	bool onTop = zTestMode == 8;
			// 	onTop = EditorGUILayout.Toggle("Render On Top", onTop);
			// 	//Always or LEqual //right now this is 6 and 2, but shouldn't it be 8 and 4??
			// 	mat.SetInt(ShaderIds.Z_TEST_MODE, onTop ? 8 : 4);
			// }
			EditorGUILayout.LabelField("Ultra Shader Settings", EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
				

			ShaderFeatureToggle(mat, "PIXELSNAP_ON", "PixelSnap", "Pixel Snap");
			

#if UNITY_2017_1_OR_NEWER
#else
			if(stm.uiMode)
			{
				EditorGUILayout.HelpBox("Ultra Shader effects are not supported on Unity UI in Unity versions " +
				                        "older than 2017.1! You can use `STMMaskableGraphic' for " +
				                        "outline & shadow effects, instead!", MessageType.Warning);
			}
			if(!stm.uiMode) //in older versions of unity, these effects wont work w/ UI mode
			{
#endif
				var currentEffect = mat.GetInt(ShaderIds.EFFECT);
				EditorGUI.BeginChangeCheck();
				currentEffect = EditorGUILayout.Popup("Effect", currentEffect,
					new[] { "None", "Dropshadow", "Outline", "Dropshadow & Outline", "Thick Dropshadow & Outline" });
				if(EditorGUI.EndChangeCheck())
				{
					mat.SetInt(ShaderIds.EFFECT, currentEffect);
					if(currentEffect == 1)
					{
						mat.EnableKeyword("_EFFECT_DROPSHADOW");
						mat.DisableKeyword("_EFFECT_OUTLINE");
						mat.DisableKeyword("_EFFECT_BOTH");
						mat.DisableKeyword("_EFFECT_BOTHTHICK");
						mat.DisableKeyword("_EFFECT_NONE");
					}
					else if(currentEffect == 2)
					{
						mat.DisableKeyword("_EFFECT_DROPSHADOW");
						mat.EnableKeyword("_EFFECT_OUTLINE");
						mat.DisableKeyword("_EFFECT_BOTH");
						mat.DisableKeyword("_EFFECT_BOTHTHICK");
						mat.DisableKeyword("_EFFECT_NONE");
					}
					else if(currentEffect == 3)
					{
						mat.DisableKeyword("_EFFECT_DROPSHADOW");
						mat.DisableKeyword("_EFFECT_OUTLINE");
						mat.EnableKeyword("_EFFECT_BOTH");
						mat.DisableKeyword("_EFFECT_BOTHTHICK");
						mat.DisableKeyword("_EFFECT_NONE");
					}
					else if(currentEffect == 4)
					{
						mat.DisableKeyword("_EFFECT_DROPSHADOW");
						mat.DisableKeyword("_EFFECT_OUTLINE");
						mat.DisableKeyword("_EFFECT_BOTH");
						mat.EnableKeyword("_EFFECT_BOTHTHICK");
						mat.DisableKeyword("_EFFECT_NONE");
					}
					else
					{
						mat.DisableKeyword("_EFFECT_DROPSHADOW");
						mat.DisableKeyword("_EFFECT_OUTLINE");
						mat.DisableKeyword("_EFFECT_BOTH");
						mat.DisableKeyword("_EFFECT_BOTHTHICK");
						mat.EnableKeyword("_EFFECT_NONE");
					}

					//force material to update immediately in some versions of unity
					stm.validateAppearance = true;
				}
				if(currentEffect > 0)
				{
					if(mat.HasProperty(ShaderIds.EFFECT_SCALING))
					{
						//ShaderFeatureToggle(mat, "EFFECT_SCALING", ShaderIds.EFFECT_SCALING, "Effect Scaling");
						var currentScaling = mat.GetInt(ShaderIds.EFFECT_SCALING);
						EditorGUI.BeginChangeCheck();
						currentScaling = EditorGUILayout.Popup("Scaling Mode", currentScaling,
							new[] { "Scale with Object", "Scale with Text" });
						if(EditorGUI.EndChangeCheck())
						{
							mat.SetInt(ShaderIds.EFFECT_SCALING, currentScaling);
							if(currentScaling == 1)
							{
								mat.EnableKeyword("EFFECT_SCALING");
							}
							else
							{
								mat.DisableKeyword("EFFECT_SCALING");
							}
						}
					}
					//effect depth!
					if(mat.HasProperty(ShaderIds.EFFECT_DEPTH))
					{
						mat.SetFloat(ShaderIds.EFFECT_DEPTH, 
							EditorGUILayout.Slider(new GUIContent("Effect Depth", 
									"Depth between layers, smaller is closer together. Default and recommended value: 0.01. Larger values can render effects above other characters, and smaller values might not work properly on some GPUs."),
								mat.GetFloat(ShaderIds.EFFECT_DEPTH), 0.001f, 0.01f));
					}
				}

				/*
			EditorGUILayout.BeginHorizontal();
			//replace this with a shader feature:
			EditorGUILayout.LabelField("Outline", EditorStyles.boldLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
			var outlineEnabled = ShaderFeatureToggle(mat, "OUTLINE_ENABLED", "_OutlineEnabled", string.Empty);
			EditorGUILayout.EndHorizontal();
			*/
				if(currentEffect == 2 || currentEffect == 3 || currentEffect == 4)
				{
					EditorGUILayout.LabelField("Outline Settings", EditorStyles.boldLabel);
					EditorGUI.indentLevel++;
					if(mat.HasProperty(ShaderIds.OUTLINE_COLOR))
					{
						mat.SetColor(ShaderIds.OUTLINE_COLOR,
							EditorGUILayout.ColorField("Outline Color", mat.GetColor(ShaderIds.OUTLINE_COLOR)));
					}

					if(mat.HasProperty(ShaderIds.OUTLINE_TEXTURE))
					{
						mat.SetTexture(ShaderIds.OUTLINE_TEXTURE,
							(Texture)EditorGUILayout.ObjectField(
								new GUIContent("Outline Texture",
									"This texture's color is still multiplied by the color property! Set color to white to see the texture as-is."),
								mat.GetTexture(ShaderIds.OUTLINE_TEXTURE),
								typeof(Texture),
								false,
								GUILayout.Height(EditorGUIUtility.singleLineHeight)));
						if(mat.GetTexture(ShaderIds.OUTLINE_TEXTURE) != null)
						{
#if UNITY_5_6_OR_NEWER
							mat.SetTextureScale(ShaderIds.OUTLINE_TEXTURE,
								EditorGUILayout.Vector2Field("Outline Texture Tiling",
									mat.GetTextureScale(ShaderIds.OUTLINE_TEXTURE)));
							mat.SetTextureOffset(ShaderIds.OUTLINE_TEXTURE,
								EditorGUILayout.Vector2Field("Outline Texture Offset",
									mat.GetTextureOffset(ShaderIds.OUTLINE_TEXTURE)));
#else
							mat.SetTextureScale("_OutlineTexture",
								EditorGUILayout.Vector2Field("Outline Texture Tiling",
									mat.GetTextureScale("_OutlineTexture")));
							mat.SetTextureOffset("_OutlineTexture",
								EditorGUILayout.Vector2Field("Outline Texture Offset",
									mat.GetTextureOffset("_OutlineTexture")));
#endif
							if(mat.HasProperty(ShaderIds.OUTLINE_TEXTURE_SCROLL))
							{
								mat.SetVector(ShaderIds.OUTLINE_TEXTURE_SCROLL,
									EditorGUILayout.Vector2Field("Outline Texture Scroll",
										mat.GetVector(ShaderIds.OUTLINE_TEXTURE_SCROLL)));
							}
						}

					}

					if(mat.HasProperty(ShaderIds.OUTLINE_WIDTH))
					{
						mat.SetFloat(ShaderIds.OUTLINE_WIDTH,
							EditorGUILayout.FloatField("Outline Width", mat.GetFloat(ShaderIds.OUTLINE_WIDTH)));
					}

					if(mat.HasProperty(ShaderIds.OUTLINE_TYPE))
					{
						mat.SetInt(ShaderIds.OUTLINE_TYPE,
							EditorGUILayout.IntPopup("Type", mat.GetInt(ShaderIds.OUTLINE_TYPE), new[] { "Circle", "Square" },
								new[] { 0, 1 }));
					}

					if(mat.HasProperty(ShaderIds.OUTLINE_SAMPLES))
					{
						//taps used as it goes around
						mat.SetInt(ShaderIds.OUTLINE_SAMPLES,
							EditorGUILayout.IntSlider("Samples", mat.GetInt(ShaderIds.OUTLINE_SAMPLES), 1, 256));
					}

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Presets:", GUILayout.Width(EditorGUIUtility.labelWidth));
					if(GUILayout.Button("Default"))
					{
						mat.SetInt(ShaderIds.OUTLINE_TYPE, 0);
						mat.SetInt(ShaderIds.OUTLINE_SAMPLES, 32);
					}

					if(GUILayout.Button("Pixel 4"))
					{
						mat.SetInt(ShaderIds.OUTLINE_TYPE, 1);
						mat.SetInt(ShaderIds.OUTLINE_SAMPLES, 4);
					}

					if(GUILayout.Button("Pixel 8"))
					{
						mat.SetInt(ShaderIds.OUTLINE_TYPE, 1);
						mat.SetInt(ShaderIds.OUTLINE_SAMPLES, 8);
					}

					EditorGUILayout.EndHorizontal();

					//ShaderFeatureToggle(mat, "EFFECT_SCALING", "_EffectScaling", "Effect Scaling");
					EditorGUI.indentLevel--;
				}

				/*
				EditorGUILayout.BeginHorizontal();
				//replace this with a shader feature:
				EditorGUILayout.LabelField("Dropshadow", EditorStyles.boldLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
				var dropshadowEnabled = ShaderFeatureToggle(mat, "DROPSHADOW_ENABLED", "_DropshadowEnabled", string.Empty);
				EditorGUILayout.EndHorizontal();
				*/
				//make this a shader feature:
				if(currentEffect == 1 || currentEffect == 3 || currentEffect == 4)
				{
					EditorGUILayout.LabelField("Dropshadow Settings", EditorStyles.boldLabel);
					EditorGUI.indentLevel++;
					if(mat.HasProperty(ShaderIds.DROPSHADOW_COLOR))
					{
						//EditorGUILayout.PropertyField(shadowColor);
						mat.SetColor(ShaderIds.DROPSHADOW_COLOR,
							EditorGUILayout.ColorField("Shadow Color", mat.GetColor(ShaderIds.DROPSHADOW_COLOR)));
					}

					if(mat.HasProperty(ShaderIds.DROPSHADOW_TEXTURE))
					{
						mat.SetTexture(ShaderIds.DROPSHADOW_TEXTURE,
							(Texture)EditorGUILayout.ObjectField(
								new GUIContent("Dropshadow Texture",
									"This texture's color is still multiplied by the color property! Set color to white to see the texture as-is."),
								mat.GetTexture(ShaderIds.DROPSHADOW_TEXTURE),
								typeof(Texture),
								false,
								GUILayout.Height(EditorGUIUtility.singleLineHeight)));
						if(mat.GetTexture(ShaderIds.DROPSHADOW_TEXTURE) != null)
						{
#if UNITY_5_6_OR_NEWER
							mat.SetTextureScale(ShaderIds.DROPSHADOW_TEXTURE,
								EditorGUILayout.Vector2Field("Dropshadow Texture Tiling",
									mat.GetTextureScale(ShaderIds.DROPSHADOW_TEXTURE)));
							mat.SetTextureOffset(ShaderIds.DROPSHADOW_TEXTURE,
								EditorGUILayout.Vector2Field("Dropshadow Texture Offset",
									mat.GetTextureOffset(ShaderIds.DROPSHADOW_TEXTURE)));
#else
							mat.SetTextureScale("_DropshadowTexture",
								EditorGUILayout.Vector2Field("Dropshadow Texture Tiling",
									mat.GetTextureScale("_DropshadowTexture")));
							mat.SetTextureOffset("_DropshadowTexture",
								EditorGUILayout.Vector2Field("Dropshadow Texture Offset",
									mat.GetTextureOffset("_DropshadowTexture")));
#endif
							if(mat.HasProperty(ShaderIds.DROPSHADOW_TEXTURE_SCROLL))
							{
								mat.SetVector(ShaderIds.DROPSHADOW_TEXTURE_SCROLL,
									EditorGUILayout.Vector2Field("Dropshadow Texture Scroll",
										mat.GetVector(ShaderIds.DROPSHADOW_TEXTURE_SCROLL)));
							}
						}
					}

					//is the value set with a vector or rotation?
					//this is a setting on the shader too now
					if(mat.HasProperty(ShaderIds.DROPSHADOW_TYPE))
					{
						mat.SetInt(ShaderIds.DROPSHADOW_TYPE,
							EditorGUILayout.IntPopup("Type", mat.GetInt(ShaderIds.DROPSHADOW_TYPE), new[] { "Angle", "Vector" },
								new[] { 0, 1 }));
						if(mat.GetInt(ShaderIds.DROPSHADOW_TYPE) == 0)
						{
							if(mat.HasProperty(ShaderIds.DROPSHADOW_ANGLE))
							{
								//EditorGUILayout.PropertyField(shadowAngle);
								mat.SetFloat(ShaderIds.DROPSHADOW_ANGLE,
									EditorGUILayout.Slider("Dropshadow Angle", mat.GetFloat(ShaderIds.DROPSHADOW_ANGLE), 0f,
										360f));
							}

							if(mat.HasProperty(ShaderIds.DROPSHADOW_DISTANCE))
							{
								//EditorGUILayout.PropertyField(shadowDistance);
								mat.SetFloat(ShaderIds.DROPSHADOW_DISTANCE,
									EditorGUILayout.FloatField("Dropshadow Distance",
										mat.GetFloat(ShaderIds.DROPSHADOW_DISTANCE)));
							}
						}
						else
						{
							if(mat.HasProperty(ShaderIds.DROPSHADOW_ANGLE2))
							{
								mat.SetVector(ShaderIds.DROPSHADOW_ANGLE2,
									EditorGUILayout.Vector2Field("Dropshadow Vector",
										mat.GetVector(ShaderIds.DROPSHADOW_ANGLE2)));
							}
						}
					}

					//ShaderFeatureToggle(mat, "EFFECT_SCALING", "_EffectScaling", "Effect Scaling");
					EditorGUI.indentLevel--;
				}
#if UNITY_2017_1_OR_NEWER
#else
			}
#endif
				
			var sdfMode = ShaderFeatureToggle(mat, "SDF_MODE", ShaderIds.SDF_MODE, "SDF Mode");
			if(sdfMode)
			{
				EditorGUI.indentLevel++;
				if(mat.HasProperty(ShaderIds.BLEND))
				{
					//EditorGUILayout.PropertyField(shaderBlend);
					mat.SetFloat(ShaderIds.BLEND, EditorGUILayout.Slider("Blend", mat.GetFloat(ShaderIds.BLEND), 0.0001f, 1f));
				}
				
				if(mat.HasProperty(ShaderIds.EFFECT_BLEND))
				{
					//EditorGUILayout.PropertyField(shaderEffectBlend);
					mat.SetFloat(ShaderIds.EFFECT_BLEND, EditorGUILayout.Slider("Effect Blend", mat.GetFloat(ShaderIds.EFFECT_BLEND), 0.0001f, 1f));
				}

				if(mat.HasProperty(ShaderIds.SDF_CUTOFF))
				{
					mat.SetFloat(ShaderIds.SDF_CUTOFF,
						EditorGUILayout.Slider("Cutoff", mat.GetFloat(ShaderIds.SDF_CUTOFF), 0f, 1f));
				}

				EditorGUI.indentLevel--;
			}
		
			
			EditorGUI.indentLevel--;
		}
		//if this is the multishader
		if(mat.GetTag("STMUberShader", true, "Null") == "Yes")
		{

		//toggle SDF
			var sdfMode = ShaderFeatureToggle(mat, "SDF_MODE", ShaderIds.SDF_MODE, "SDF Mode");
			//#endif

			if(sdfMode)
			{//draw SDF-related properties
				if(mat.HasProperty(ShaderIds.BLEND)){
					//EditorGUILayout.PropertyField(shaderBlend);
					mat.SetFloat(ShaderIds.BLEND,EditorGUILayout.Slider("Blend",mat.GetFloat(ShaderIds.BLEND),0.0001f,1f));
				}
				if(mat.HasProperty(ShaderIds.SDF_CUTOFF)){
					mat.SetFloat(ShaderIds.SDF_CUTOFF,EditorGUILayout.Slider("SDF Cutoff",mat.GetFloat(ShaderIds.SDF_CUTOFF),0f,1f));
				}
			}
			//toggle Pixel Snap
			ShaderFeatureToggle(mat, "PIXELSNAP_ON", "PixelSnap", "Pixel Snap");
			
			//#endif
			if(mat.HasProperty(ShaderIds.SHADOW_COLOR)){
				//EditorGUILayout.PropertyField(shadowColor);
				mat.SetColor(ShaderIds.SHADOW_COLOR,EditorGUILayout.ColorField("Shadow Color",mat.GetColor(ShaderIds.SHADOW_COLOR)));
			}
			if(mat.HasProperty(ShaderIds.SHADOW_DISTANCE)){
				//EditorGUILayout.PropertyField(shadowDistance);
				mat.SetFloat(ShaderIds.SHADOW_DISTANCE,EditorGUILayout.FloatField("Shadow Distance",mat.GetFloat(ShaderIds.SHADOW_DISTANCE)));
			}
			if(mat.HasProperty(ShaderIds.VECTOR3_DROPSHADOW))
			{
				
				//toggle use vector 3
				var useVector3 =
					ShaderFeatureToggle(mat, "VECTOR3_DROPSHADOW", ShaderIds.VECTOR3_DROPSHADOW, "Vector3 Dropshadow");
				if(useVector3 && mat.HasProperty(ShaderIds.SHADOW_ANGLE3))
				{
					mat.SetVector(ShaderIds.SHADOW_ANGLE3,EditorGUILayout.Vector3Field("Shadow Angle3", mat.GetVector(ShaderIds.SHADOW_ANGLE3)));
				}
				else
				{
					//same as before
					if(mat.HasProperty(ShaderIds.SHADOW_ANGLE)){
						//EditorGUILayout.PropertyField(shadowAngle);
						mat.SetFloat(ShaderIds.SHADOW_ANGLE,EditorGUILayout.Slider("Shadow Angle",mat.GetFloat(ShaderIds.SHADOW_ANGLE),0f,360f));
					}
				}
			}
			else
			{
				if(mat.HasProperty(ShaderIds.SHADOW_ANGLE)){
					//EditorGUILayout.PropertyField(shadowAngle);
					mat.SetFloat(ShaderIds.SHADOW_ANGLE,EditorGUILayout.Slider("Shadow Angle",mat.GetFloat(ShaderIds.SHADOW_ANGLE),0f,360f));
				}
			}
			if(mat.HasProperty(ShaderIds.OUTLINE_COLOR)){
				//EditorGUILayout.PropertyField(outlineColor);
				mat.SetColor(ShaderIds.OUTLINE_COLOR,EditorGUILayout.ColorField("Outline Color",mat.GetColor(ShaderIds.OUTLINE_COLOR)));
			}
			if(mat.HasProperty(ShaderIds.OUTLINE_WIDTH)){
				//EditorGUILayout.PropertyField(outlineWidth);
				mat.SetFloat(ShaderIds.OUTLINE_WIDTH,EditorGUILayout.FloatField("Outline Width",mat.GetFloat(ShaderIds.OUTLINE_WIDTH)));
			}
			if(mat.HasProperty(ShaderIds.SQUARE_OUTLINE))
			{
				ShaderFeatureToggle(mat, "SQUARE_OUTLINE", ShaderIds.SQUARE_OUTLINE, "Square Outline");
			}
		}



		EditorGUILayout.BeginHorizontal();
		mat.renderQueue = EditorGUILayout.IntField("Render Queue", originalQueue);
		if(GUILayout.Button("Reset"))
		{
			mat.renderQueue = mat.shader.renderQueue;
		}
		EditorGUILayout.EndHorizontal();
	}
#if UNITY_2017_1_OR_NEWER
	public static void DrawRenderPreview(SuperTextMeshData data, float loop)
	{
		if(data == null || data.prevRenderer == null) return;
		GUILayout.Box(GUIContent.none, GUIStyle.none, GUILayout.Height(Screen.width * 0.3f));
		Rect r = GUILayoutUtility.GetLastRect();

		if(loop > 0f)
		{
			if(Time.realtimeSinceStartup - data.previewStartTime > loop)
			{
				data.previewStartTime = Time.realtimeSinceStartup;
				data.previewTextMesh.Read(); //start reading again!
			}
		}

		data.prevRenderer.BeginPreview(r, GUIStyle.none);
		
		if (data.previewTextMesh != null)
		{
			for(int i = 0; i < data.previewTextMesh.submeshes.Count; i++)
			{
				data.prevRenderer.DrawMesh(
					data.previewTextMesh.textMesh,
					data.previewTextMesh.t.localToWorldMatrix,
					data.previewTextMesh.r.sharedMaterials[i], i);
			}
		}
		////
		Unsupported.SetRenderSettingsUseFogNoDirty(false);
		data.prevRenderer.camera.Render();
		Unsupported.SetRenderSettingsUseFogNoDirty(false);

		Texture texture = data.prevRenderer.EndPreview();
		GUI.DrawTexture(r, texture);
	}
#endif
}
#endif
