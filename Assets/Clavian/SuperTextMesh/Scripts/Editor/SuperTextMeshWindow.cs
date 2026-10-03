//Copyright (c) 2026 Kai Clavier [kaiclavier.com] Do Not Distribute
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Clavian.STM.Editor
{
#if UNITY_EDITOR
	public class SuperTextMeshWindow : EditorWindow
	{
		private Vector2 scroll;
		public static SuperTextMeshWindow Instance
		{
			get;
			private set;
		}
		public static bool IsOpen
		{
			get
			{
				return Instance != null;
			}
		}

		private SuperTextMeshData _data;
		public SuperTextMeshData data{
			get{
				if(_data == null) {
					_data = Resources.Load("SuperTextMeshData") as SuperTextMeshData;
					if(_data != null)
						_data.RebuildDictionaries(); //rebuild dictionaries
				}
				return _data;
			}set{
				_data = value;
			}
		}
		[MenuItem("Window/Super Text Mesh Data")]
		public static void ShowWindow() {
			//show window
			SuperTextMeshWindow window = EditorWindow.GetWindow<SuperTextMeshWindow>();
			//get icon
			Texture2D icon = (Texture2D)AssetDatabase.LoadAssetAtPath(Tools.AssetPath + "/Scripts/Editor/EditorIcon.png", typeof(Texture2D));
			//apply to header
			window.titleContent = new GUIContent(" Text Data", icon);
		}
		void OnEnable()
		{
			Instance = this;
			Undo.undoRedoPerformed += OnUndoRedoCallback;
		}
		void OnDisable()
		{
#if UNITY_2017_1_OR_NEWER
			if(data.previewData != null)
			{
				data.ClosePreview();
			}
#endif
			Undo.undoRedoPerformed -= OnUndoRedoCallback;
		}
		
#if UNITY_2017_1_OR_NEWER
		void Update()
		{
			if(data.previewData != null)
			{
				Repaint();
			}
		}
#endif
		void OnUndoRedoCallback()
		{
			if(data != null)
				data.RebuildDictionaries();
			Repaint();
		}
		void OnInspectorUpdate()
		{
			Repaint();
		}

		void OnGUI()
		{
			scroll = EditorGUILayout.BeginScrollView(scroll);
			DrawGUIFor(null, data);
			EditorGUILayout.EndScrollView();
		}
		
		public static int GetSortOrder(Object o)
		{
			#if UNITY_6000_5_OR_NEWER
			return -o.GetHashCode();
			#else
			return -o.GetInstanceID();
			#endif
		}

		public static void DrawGUIFor(SuperTextMesh stm, SuperTextMeshData data)
		{
			var foldoutStyle = new GUIStyle(EditorStyles.foldout); //create a new foldout style, for the bold foldout headers
			foldoutStyle.fontStyle = FontStyle.Bold; //set it to look like a header
			//TEXT DATA ICON
			//Object textDataObject = stm.data; //get text data object

			//var textDataStyle = new GUIStyle(EditorStyles.label);
			
			
			var serializedData = new SerializedObject(data);
			serializedData.Update();
			
			data.showEffectsFoldout = EditorGUILayout.Foldout(data.showEffectsFoldout, "Effects", foldoutStyle);
			if(data.showEffectsFoldout){
				EditorGUI.indentLevel++;
			//Waves:
				data.showWavesFoldout = EditorGUILayout.Foldout(data.showWavesFoldout, new GUIContent("Waves", "<w=name>"), foldoutStyle);
				if(data.showWavesFoldout){
				//Gather all data:
					foreach(KeyValuePair<string, STMWaveData> i in data.waves.OrderBy(x => GetSortOrder(x.Value))){
						EditorGUI.indentLevel++;
						i.Value.showFoldout = EditorGUILayout.Foldout(i.Value.showFoldout, i.Key, foldoutStyle);
						EditorGUI.indentLevel--;
						if(i.Value.showFoldout) if(i.Value != null)i.Value.DoDrawCustomInspector(stm, data); //draw a custom inspector for each wave
					}
				//Create new button:
					STMCustomInspectorTools.DrawCreateNewButton("Create New Wave", "STMWaves/New Wave.asset", "STMWaveData", stm, data);
				}
			//Jitters:
				data.showJittersFoldout = EditorGUILayout.Foldout(data.showJittersFoldout, new GUIContent("Jitters", "<j=name>"), foldoutStyle);
				if(data.showJittersFoldout){
				//Gather all data:
					foreach(KeyValuePair<string, STMJitterData> i in data.jitters.OrderBy(x => GetSortOrder(x.Value))){
						EditorGUI.indentLevel++;
						i.Value.showFoldout = EditorGUILayout.Foldout(i.Value.showFoldout, i.Key, foldoutStyle);
						EditorGUI.indentLevel--;
						if(i.Value.showFoldout) if(i.Value != null)i.Value.DoDrawCustomInspector(stm, data); //draw a custom inspector for each Jitter
					}
				//Create new button:
					STMCustomInspectorTools.DrawCreateNewButton("Create New Jitter", "STMJitters/New Jitter.asset", "STMJitterData", stm, data);
				}
			//Draw Animations:
				data.showDrawAnimsFoldout = EditorGUILayout.Foldout(data.showDrawAnimsFoldout, new GUIContent("DrawAnims", "<drawAnim=name>"), foldoutStyle);
				if(data.showDrawAnimsFoldout){
				//Gather all data:
					foreach(KeyValuePair<string, STMDrawAnimData> i in data.drawAnims.OrderBy(x => GetSortOrder(x.Value))){
						EditorGUI.indentLevel++;
						i.Value.showFoldout = EditorGUILayout.Foldout(i.Value.showFoldout, i.Key, foldoutStyle);
						EditorGUI.indentLevel--;
						if(i.Value.showFoldout) if(i.Value != null)i.Value.DoDrawCustomInspector(stm, data); //draw a custom inspector for each Jitter
					}
				//Create new button:
					STMCustomInspectorTools.DrawCreateNewButton("Create New DrawAnim", "STMDrawAnims/New DrawAnim.asset", "STMDrawAnimData", stm, data);
				}
				EditorGUI.indentLevel--;
			}
			data.showTextColorFoldout = EditorGUILayout.Foldout(data.showTextColorFoldout, new GUIContent("Text Color", "All of these can be used with <c=name>"), foldoutStyle);
			if(data.showTextColorFoldout){
				EditorGUI.indentLevel++;
			//Colors:
				data.showColorsFoldout = EditorGUILayout.Foldout(data.showColorsFoldout, new GUIContent("Colors", "<c=name>"), foldoutStyle);
				if(data.showColorsFoldout){
				//Gather all data
					foreach(KeyValuePair<string, STMColorData> i in data.colors.OrderBy(x => GetSortOrder(x.Value))){
						EditorGUI.indentLevel++;
						i.Value.showFoldout = EditorGUILayout.Foldout(i.Value.showFoldout, i.Key, foldoutStyle);
						EditorGUI.indentLevel--;
						if(i.Value.showFoldout) if(i.Value != null)i.Value.DoDrawCustomInspector(stm, data); //draw a custom inspector for each Jitter
					}
				//Create new button:
					STMCustomInspectorTools.DrawCreateNewButton("Create New Color", "STMColors/New Color.asset", "STMColorData", stm, data);
				}
			//Gradients:
				data.showGradientsFoldout = EditorGUILayout.Foldout(data.showGradientsFoldout, new GUIContent("Gradients", "<c=name>"), foldoutStyle);
				if(data.showGradientsFoldout){
				//Gather all data:
					foreach(KeyValuePair<string, STMGradientData> i in data.gradients.OrderBy(x => GetSortOrder(x.Value))){ //reorder so the order stays consistent
						EditorGUI.indentLevel++;
						i.Value.showFoldout = EditorGUILayout.Foldout(i.Value.showFoldout, i.Key, foldoutStyle);
						EditorGUI.indentLevel--;
						if(i.Value.showFoldout) if(i.Value != null)i.Value.DoDrawCustomInspector(stm, data); //draw a custom inspector for each Jitter
					}
				//Create new button:
					STMCustomInspectorTools.DrawCreateNewButton("Create New Gradient", "STMGradients/New Gradient.asset", "STMGradientData", stm, data);
				}
			//Textures:
				data.showTexturesFoldout = EditorGUILayout.Foldout(data.showTexturesFoldout, new GUIContent("Textures", "<c=name>"), foldoutStyle);
				if(data.showTexturesFoldout){
				//Gather all data:
					foreach(KeyValuePair<string, STMTextureData> i in data.textures.OrderBy(x => GetSortOrder(x.Value))){
						EditorGUI.indentLevel++;
						i.Value.showFoldout = EditorGUILayout.Foldout(i.Value.showFoldout, i.Key, foldoutStyle);
						EditorGUI.indentLevel--;
						if(i.Value.showFoldout) if(i.Value != null)i.Value.DoDrawCustomInspector(stm, data); //draw a custom inspector for each Jitter
					}
				//Create new button:
					STMCustomInspectorTools.DrawCreateNewButton("Create New Texture", "STMTextures/New Texture.asset", "STMTextureData", stm, data);
				}
				EditorGUI.indentLevel--;
			}
			data.showInlineFoldout = EditorGUILayout.Foldout(data.showInlineFoldout, "Inline", foldoutStyle);
			if(data.showInlineFoldout){
				EditorGUI.indentLevel++;
			//Delays:
				data.showDelaysFoldout = EditorGUILayout.Foldout(data.showDelaysFoldout, new GUIContent("Delays", "<d=name>"), foldoutStyle);
				if(data.showDelaysFoldout){
				//Gather all data:
					foreach(KeyValuePair<string, STMDelayData> i in data.delays.OrderBy(x => GetSortOrder(x.Value))){
						EditorGUI.indentLevel++;
						i.Value.showFoldout = EditorGUILayout.Foldout(i.Value.showFoldout, i.Key, foldoutStyle);
						EditorGUI.indentLevel--;
						if(i.Value.showFoldout) if(i.Value != null)i.Value.DoDrawCustomInspector(stm, data); //draw a custom inspector for each Jitter
					}
				//Create new button:
					STMCustomInspectorTools.DrawCreateNewButton("Create New Delay", "STMDelays/New Delay.asset", "STMDelayData", stm, data);
				}
			//Voices:
				data.showVoicesFoldout = EditorGUILayout.Foldout(data.showVoicesFoldout, new GUIContent("Voices", "<v=name>"), foldoutStyle);
				if(data.showVoicesFoldout){
				//Gather all data:
					foreach(KeyValuePair<string, STMVoiceData> i in data.voices.OrderBy(x => GetSortOrder(x.Value))){
						EditorGUI.indentLevel++;
						i.Value.showFoldout = EditorGUILayout.Foldout(i.Value.showFoldout, i.Key, foldoutStyle);
						EditorGUI.indentLevel--;
						if(i.Value.showFoldout) if(i.Value != null)i.Value.DoDrawCustomInspector(stm, data); //draw a custom inspector for each Jitter
					}
				//Create new button:
					STMCustomInspectorTools.DrawCreateNewButton("Create New Voice", "STMVoices/New Voice.asset", "STMVoiceData", stm, data);
				}
			//Fonts:
				data.showFontsFoldout = EditorGUILayout.Foldout(data.showFontsFoldout, new GUIContent("Fonts", "<f=name>"), foldoutStyle);
				if(data.showFontsFoldout){
				//Gather all data:
					foreach(KeyValuePair<string, STMFontData> i in data.fonts.OrderBy(x => GetSortOrder(x.Value))){
						EditorGUI.indentLevel++;
						i.Value.showFoldout = EditorGUILayout.Foldout(i.Value.showFoldout, i.Key, foldoutStyle);
						EditorGUI.indentLevel--;
						if(i.Value.showFoldout) if(i.Value != null)i.Value.DoDrawCustomInspector(stm, data); //draw a custom inspector for each Jitter
					}
				//Create new button:
					STMCustomInspectorTools.DrawCreateNewButton("Create New Font", "STMFonts/New Font.asset", "STMFontData", stm, data);
				}
			//AudioClips:
				data.showAudioClipsFoldout = EditorGUILayout.Foldout(data.showAudioClipsFoldout, new GUIContent("AudioClips", "<audioClips=name>"), foldoutStyle);
				if(data.showAudioClipsFoldout){
				//Gather all data:
					foreach(KeyValuePair<string, STMAudioClipData> i in data.audioClips.OrderBy(x => GetSortOrder(x.Value))){
						EditorGUI.indentLevel++;
						i.Value.showFoldout = EditorGUILayout.Foldout(i.Value.showFoldout, i.Key, foldoutStyle);
						EditorGUI.indentLevel--;
						if(i.Value.showFoldout) if(i.Value != null)i.Value.DoDrawCustomInspector(stm, data); //draw a custom inspector for each Jitter
					}
				//Create new button:
					STMCustomInspectorTools.DrawCreateNewButton("Create New Audio Clip", "STMAudioClips/New Audio Clip.asset", "STMAudioClipData", stm, data);
				}
			//Sound Clips:
			//This one's a bit different! Since it's folders of clips...
				data.showSoundClipsFoldout = EditorGUILayout.Foldout(data.showSoundClipsFoldout, new GUIContent("Sound Clips", "<clips=name>"), foldoutStyle);
				if(data.showSoundClipsFoldout)
				{
				//Gather all data:
					foreach(KeyValuePair<string, STMSoundClipData> i in data.soundClips.OrderBy(x => GetSortOrder(x.Value)))
					{
						EditorGUI.indentLevel++;
						i.Value.showFoldout = EditorGUILayout.Foldout(i.Value.showFoldout, i.Key, foldoutStyle);
						EditorGUI.indentLevel--;
						if(i.Value.showFoldout) if(i.Value != null)i.Value.DoDrawCustomInspector(stm, data); //draw a custom inspector for each Jitter
					}
				//Create new button:
					STMCustomInspectorTools.DrawCreateNewButton("Create New Sound Clip", "STMSoundClips/New Sound Clip.asset", "STMSoundClipData", stm, data);
				}
			//Quads:
				data.showQuadsFoldout = EditorGUILayout.Foldout(data.showQuadsFoldout, new GUIContent("Quads", "<q=name>"), foldoutStyle);
				if(data.showQuadsFoldout)
				{
					EditorGUILayout.HelpBox("Columns: Amount of icons along the x-axis of the texture. \n" + 
					                        "Rows: Amount of icons along the y-axis of the texture. \n" + 
					                        "Index: Starting at the bottom-left corner, which icon is being used, going along the x-axis then y-axis.\n" + 
					                        "For further information on how columns, rows, and index work, please refer to the sample image under 'Quads' in the documentation.", MessageType.None, true);
				//Gather all data:
					foreach(KeyValuePair<string, STMQuadData> i in data.quads.OrderBy(x => GetSortOrder(x.Value)))
					{
						EditorGUI.indentLevel++;
						i.Value.showFoldout = EditorGUILayout.Foldout(i.Value.showFoldout, i.Key, foldoutStyle);
						EditorGUI.indentLevel--;
						if(i.Value.showFoldout) if(i.Value != null)i.Value.DoDrawCustomInspector(stm, data); //draw a custom inspector for each Jitter
					}
				//Create new button:
					STMCustomInspectorTools.DrawCreateNewButton("Create New Quad", "STMQuads/New Quad.asset", "STMQuadData", stm, data);
				}
			//Materials:
				data.showMaterialsFoldout = EditorGUILayout.Foldout(data.showMaterialsFoldout, new GUIContent("Materials", "<m=name>"), foldoutStyle);
				if(data.showMaterialsFoldout)
				{
				//Gather all data:
					foreach(KeyValuePair<string, STMMaterialData> i in data.materials.OrderBy(x => GetSortOrder(x.Value)))
					{
						EditorGUI.indentLevel++;
						i.Value.showFoldout = EditorGUILayout.Foldout(i.Value.showFoldout, i.Key, foldoutStyle);
						EditorGUI.indentLevel--;
						if(i.Value.showFoldout) if(i.Value != null)i.Value.DoDrawCustomInspector(stm, data); //draw a custom inspector for each Jitter
					}
				//Create new button:
					STMCustomInspectorTools.DrawCreateNewButton("Create New Material", "STMMaterials/New Material.asset", "STMMaterialData", stm, data);
				}

				EditorGUI.indentLevel--;
			}

			data.showAutomaticFoldout = EditorGUILayout.Foldout(data.showAutomaticFoldout, "Automatic", foldoutStyle);
			if(data.showAutomaticFoldout)
			{
				EditorGUI.indentLevel++;
			//AutoDelays:
				data.showAutoDelaysFoldout = EditorGUILayout.Foldout(data.showAutoDelaysFoldout, "AutoDelays", foldoutStyle);
				if(data.showAutoDelaysFoldout){
				//Gather all data:
					foreach(KeyValuePair<string, STMAutoDelayData> i in data.autoDelays.OrderBy(x => GetSortOrder(x.Value)))
					{
						EditorGUI.indentLevel++;
						i.Value.showFoldout = EditorGUILayout.Foldout(i.Value.showFoldout, i.Key, foldoutStyle);
						EditorGUI.indentLevel--;
						if(i.Value.showFoldout) if(i.Value != null)i.Value.DoDrawCustomInspector(stm, data); //draw a custom inspector for each Jitter
					}
				//Create new button:
					STMCustomInspectorTools.DrawCreateNewButton("Create New Auto Delay", "STMAutoDelays/New Auto Delay.asset", "STMDelayData", stm, data);
				}
			//AutoClips:
				data.showAutoClipsFoldout = EditorGUILayout.Foldout(data.showAutoClipsFoldout, "AutoClips", foldoutStyle);
				if(data.showAutoClipsFoldout)
				{
				//Gather all data:
					//STMSoundClipData[] allAutoClips = Resources.LoadAll<STMSoundClipData>("STMAutoClips").OrderBy(x => -x.GetInstanceID()).ToArray(); //do this so order keeps consistent
					foreach(KeyValuePair<string, STMAutoClipData> i in data.autoClips.OrderBy(x => GetSortOrder(x.Value))){
						EditorGUI.indentLevel++;
						i.Value.showFoldout = EditorGUILayout.Foldout(i.Value.showFoldout, i.Key, foldoutStyle);
						EditorGUI.indentLevel--;
						if(i.Value.showFoldout) if(i.Value != null)i.Value.DoDrawCustomInspector(stm, data); //draw a custom inspector for each Jitter
					}
				//Create new button:
					STMCustomInspectorTools.DrawCreateNewButton("Create New Auto Clip", "STMAutoClips/New Auto Clip.asset", "STMAutoClipData", stm, data);
				}
				EditorGUI.indentLevel--;
			}
			data.showSettingsFoldout = EditorGUILayout.Foldout(data.showSettingsFoldout, "Settings", foldoutStyle);
			if(data.showSettingsFoldout)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(serializedData.FindProperty("disableAnimatedText"), true);
				
				EditorGUILayout.PropertyField(serializedData.FindProperty("boundsColor"));
				EditorGUILayout.PropertyField(serializedData.FindProperty("textBoundsColor"));
				EditorGUILayout.PropertyField(serializedData.FindProperty("finalTextBoundsColor"));
				EditorGUILayout.PropertyField(serializedData.FindProperty("superscriptOffset"));
				EditorGUILayout.PropertyField(serializedData.FindProperty("superscriptSize"));
				EditorGUILayout.PropertyField(serializedData.FindProperty("subscriptOffset"));
				EditorGUILayout.PropertyField(serializedData.FindProperty("subscriptSize"));
				EditorGUILayout.PropertyField(serializedData.FindProperty("inspectorFont"));
				EditorGUILayout.PropertyField(serializedData.FindProperty("multiplyMultipleColorTags"));
				EditorGUILayout.PropertyField(serializedData.FindProperty("hyphenCharacter"));
				EditorGUILayout.PropertyField(serializedData.FindProperty("_fallbackFonts"), true);
#if UNITY_2017_1_OR_NEWER
				EditorGUILayout.PropertyField(serializedData.FindProperty("previewFont"));
				EditorGUILayout.PropertyField(serializedData.FindProperty("previewFilterMode"));
#endif
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Experimental", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(serializedData.FindProperty("useTextCoreForFontLoading"));
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Don't Change This", EditorStyles.boldLabel);
				//EditorGUI.BeginDisabledGroup(true);
				//hopefully this will never get un-assigned.......
				EditorGUILayout.PropertyField(serializedData.FindProperty("defaultFont"));
				//EditorGUI.EndDisabledGroup();

				EditorGUI.indentLevel--;
			}
			if(GUILayout.Button("Refresh Database"))
			{
				if(data != null)
					data.RebuildDictionaries();
			}
			if(GUI.changed)
			{
				//OnDataUpdated(stm);
				if(stm != null)
					stm.SetMesh(-1f);
				if(data != null)
					EditorUtility.SetDirty(data);
#if UNITY_2017_1_OR_NEWER
				if(data != null && data.previewData != null && data.forceRebuild)
				{
					data.forceRebuild = false;
					data.previewTextMesh.Rebuild();
				}
#endif
			}
			serializedData.ApplyModifiedProperties();
		}
	}
#endif
}