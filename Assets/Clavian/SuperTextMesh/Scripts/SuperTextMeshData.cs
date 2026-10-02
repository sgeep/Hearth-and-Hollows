//Copyright (c) 2016-2025 Kai Clavier [kaiclavier.com] Do Not Distribute
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq; //converting arrays to dictionaries
using System.IO;
using UnityEngine.Serialization; //for getting folders
#if UNITY_EDITOR && UNITY_2017_1_OR_NEWER
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
#endif

[CreateAssetMenu(fileName = "New Text Data", menuName = "Super Text Mesh/Super Text Mesh Data", order = 0)]
public class SuperTextMeshData : ScriptableObject  //the actual textdata manager file
{
#if UNITY_EDITOR && UNITY_2017_1_OR_NEWER
	public PreviewRenderUtility prevRenderer;
	public STMBaseData previewData;
	private Scene previewScene;
	public SuperTextMesh previewTextMesh;
	public float previewStartTime;
	public bool forceRebuild;
	public Font previewFont;
	public FilterMode previewFilterMode = FilterMode.Bilinear;

	public void TogglePreview(string n, STMBaseData data)
	{
		//opening new
		if(previewData == null)
		{
			OpenPreview(n,data);
		}
		//opening another
		else if(previewData != data)
		{
			OpenPreview(n,data);
		}
		//closing
		else
		{
			ClosePreview();
		}
	}

	private void OpenPreview(string text, STMBaseData data)
	{
		previewScene = EditorSceneManager.NewPreviewScene();

		var newGo = EditorUtility.CreateGameObjectWithHideFlags("Preview Text", HideFlags.DontSave);
		SceneManager.MoveGameObjectToScene(newGo,previewScene);
		previewTextMesh = newGo.AddComponent<SuperTextMesh>();

		previewTextMesh.alignment = SuperTextMesh.Alignment.Center;
		previewTextMesh.anchor = TextAnchor.MiddleCenter;
		previewTextMesh.autoWrap = 0f;
		previewTextMesh.font = previewFont;
		previewTextMesh.filterMode = previewFilterMode;
		previewTextMesh.text = text;

		previewStartTime = Time.realtimeSinceStartup - previewTextMesh.totalReadTime;
		
		if (prevRenderer == null || prevRenderer.camera == null)
			prevRenderer = new PreviewRenderUtility();

		prevRenderer.camera.transform.position = new Vector3(0f,0f,-10f);
		prevRenderer.camera.transform.LookAt(Vector3.zero, Vector3.up);
		prevRenderer.camera.farClipPlane = 30;

		previewData = data;
	}
	public void ClosePreview()
	{
		if(previewTextMesh != null)
		{
			DestroyImmediate(previewTextMesh.gameObject);
		}
		EditorSceneManager.ClosePreviewScene(previewScene);
		if(prevRenderer != null)
			prevRenderer.Cleanup();
		
		previewData = null;
	}
	
#endif
	//[HideInInspector] public bool textDataEditMode = false; //whether this will show on objects or not
	
	[HideInInspector] public bool showEffectsFoldout = false;

	[HideInInspector] public bool showWavesFoldout = false;
	public Dictionary<string,STMWaveData> waves = new Dictionary<string,STMWaveData>();
	[HideInInspector] public bool showJittersFoldout = false;
	public Dictionary<string,STMJitterData> jitters = new Dictionary<string,STMJitterData>();
	[HideInInspector] public bool showDrawAnimsFoldout = false;
	public Dictionary<string,STMDrawAnimData> drawAnims = new Dictionary<string,STMDrawAnimData>();


	[HideInInspector] public bool showTextColorFoldout = false;

	[HideInInspector] public bool showColorsFoldout = false;
	public Dictionary<string,STMColorData> colors = new Dictionary<string,STMColorData>();
	[HideInInspector] public bool showGradientsFoldout = false;
	public Dictionary<string,STMGradientData> gradients = new Dictionary<string,STMGradientData>();
	[HideInInspector] public bool showTexturesFoldout = false;
	public Dictionary<string,STMTextureData> textures = new Dictionary<string,STMTextureData>();


    [HideInInspector] public bool showInlineFoldout = false;

    [HideInInspector] public bool showDelaysFoldout = false;
    public Dictionary<string,STMDelayData> delays = new Dictionary<string,STMDelayData>();
    [HideInInspector] public bool showVoicesFoldout = false;
    public Dictionary<string,STMVoiceData> voices = new Dictionary<string,STMVoiceData>();
    [HideInInspector] public bool showFontsFoldout = false;
    public Dictionary<string,STMFontData> fonts = new Dictionary<string,STMFontData>();
    [HideInInspector] public bool showSoundClipsFoldout = false;
    public Dictionary<string,STMSoundClipData> soundClips = new Dictionary<string,STMSoundClipData>();
    //public List<bool> showSoundClipFoldout = new List<bool>();
    [HideInInspector] public bool showAudioClipsFoldout = false;
    public Dictionary<string,STMAudioClipData> audioClips = new Dictionary<string,STMAudioClipData>();
    [HideInInspector] public bool showQuadsFoldout = false;
    public Dictionary<string,STMQuadData> quads = new Dictionary<string,STMQuadData>();
    [HideInInspector] public bool showMaterialsFoldout = false;
    public Dictionary<string,STMMaterialData> materials = new Dictionary<string,STMMaterialData>();


    [HideInInspector] public bool showAutomaticFoldout = false;

    [HideInInspector] public bool showAutoClipsFoldout = false;
    public Dictionary<string,STMAutoClipData> autoClips = new Dictionary<string,STMAutoClipData>();
    [HideInInspector] public bool showAutoDelaysFoldout = false;
    public Dictionary<string,STMAutoDelayData> autoDelays = new Dictionary<string,STMAutoDelayData>();


    [HideInInspector] public bool showSettingsFoldout = true;

	[Tooltip("This disables waves and jitters from effecting text position, which might be hard for some users to read.")]
    public bool disableAnimatedText = false;
	[Tooltip("Highly recommended do NOT change this value! This is the font that will be used on created STM components and when STM's text value is set to null. If this value is null, Super Text Mesh will search for Arial/LegacyRuntime. This value is also used when searching fallback fonts, as TextCore has a bug when loading Arial/LegacyRuntime.")]
	public Font defaultFont;
	public Color boundsColor = Color.blue;
	public Color textBoundsColor = Color.yellow;
	public Color finalTextBoundsColor = Color.grey;
	public float superscriptOffset = 0.5f;
	public float superscriptSize = 0.5f;
	public float subscriptOffset = -0.2f;
	public float subscriptSize = 0.5f;
	[Tooltip("If true, when multiple color tags are used (<c=myColor,myGradient>) their colours will always be multiplied instead of using whatever defined blending mode they have.")]
	public bool multiplyMultipleColorTags = false;
	
	public bool useTextCoreForFontLoading = false;
	[SerializeField][Tooltip("Fonts that will be requested for a character when initial font doesn't include said character. This will only work if the initial font is NOT dynamic, or using TextCore for font loading. Dynamic fonts can be given fallbacks to other dynamic fonts directly on their import settings! (Dynamic fonts will fall back to Arial/LegacyRuntime before considering this array.)")]
	private Font[] _fallbackFonts = new Font[0];
	
	[Tooltip("When 'insertHyphens' is true on a mesh, this decides which character will be added. Some fonts don't support soft hyphen, which is why this is here.")]
	public HyphenCharacter hyphenCharacter = HyphenCharacter.SoftHyphen;
	public enum HyphenCharacter
	{
		SoftHyphen,
		Hyphen
	}
	public Font inspectorFont;

	public Font[] fallbackFonts
	{
		get
		{
			if(_fallbackFonts != null && (_fallbackFonts.Length > 0 && _fallbackFonts[0] != null))
			{
				return _fallbackFonts;
			}

			return new Font[]{defaultFont};
		}
		set { _fallbackFonts = value; }
	}


	public void RebuildDictionaries(){
    	waves = Resources.LoadAll<STMWaveData>("STMWaves").ToDictionary(x => x.name, x => x);
    	jitters = Resources.LoadAll<STMJitterData>("STMJitters").ToDictionary(x => x.name, x => x);
    	drawAnims = Resources.LoadAll<STMDrawAnimData>("STMDrawAnims").ToDictionary(x => x.name, x => x);

    	colors = Resources.LoadAll<STMColorData>("STMColors").ToDictionary(x => x.name, x => x);
    	gradients = Resources.LoadAll<STMGradientData>("STMGradients").ToDictionary(x => x.name, x => x);
    	textures = Resources.LoadAll<STMTextureData>("STMTextures").ToDictionary(x => x.name, x => x);

    	delays = Resources.LoadAll<STMDelayData>("STMDelays").ToDictionary(x => x.name, x => x);
    	voices = Resources.LoadAll<STMVoiceData>("STMVoices").ToDictionary(x => x.name, x => x);
    	fonts = Resources.LoadAll<STMFontData>("STMFonts").ToDictionary(x => x.name, x => x);

    	soundClips = Resources.LoadAll<STMSoundClipData>("STMSoundClips").ToDictionary(x => x.name, x => x);
    	audioClips = Resources.LoadAll<STMAudioClipData>("STMAudioClips").ToDictionary(x => x.name, x => x);
    	quads = Resources.LoadAll<STMQuadData>("STMQuads").ToDictionary(x => x.name, x => x);
    	materials = Resources.LoadAll<STMMaterialData>("STMMaterials").ToDictionary(x => x.name, x => x);
	    
	    //make sure that they have distinct values!
    	autoClips = Resources.LoadAll<STMAutoClipData>("STMAutoClips").GroupBy(x => x.type == STMAutoClipData.Type.Quad ? x.quadName : x.character.ToString()).Select(x => x.First()).ToDictionary(x => x.type == STMAutoClipData.Type.Quad ? x.quadName : x.character.ToString(), x => x);
    	autoDelays = Resources.LoadAll<STMAutoDelayData>("STMAutoDelays").GroupBy(x => x.type == STMAutoDelayData.Type.Quad ? x.quadName : x.character.ToString()).Select(x => x.First()).ToDictionary(x => x.type == STMAutoDelayData.Type.Quad ? x.quadName : x.character.ToString(), x => x);
    }
}
