//Copyright (c) 2016-2025 Kai Clavier [kaiclavier.com] Do Not Distribute
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Quad Data", menuName = "Super Text Mesh/Quad Data", order = 1)]
public class STMQuadData : STMBaseData{
	public Texture texture; //materials should automatically join if texture matches
	//[Tooltip("If a quad is a silhouette, it won't use the color from its texture, just the alpha. If it's a silhouette, it can be effected by text color.")]
	//public bool silhouette = false;

	public enum ColorMode
	{
		Normal,
		Always,
		Silhouette,
		Multiply,
		Blend
	}
	[Tooltip("Normal: This quad will render with its texture color, but still inherit alpha from other effects. " +
	         "Always: This quad will render with its texture color unmodified. " +
	         "Silhouette: This quad will use the alpha from its texture, but the colour (including texture) from text. " +
	         "Multiply: This quad will render with its texture colors, but this can be multiplied by text color. Textures can not render on this." + 
	         "Blend: This quad will render with its texture color, but inherit colors from other effects.")]
	public ColorMode colorMode = ColorMode.Normal;
	
	public bool overrideFilterMode = false;
	public FilterMode filterMode = FilterMode.Bilinear; //default
	//[Range(1,64)]
	public bool isSilhouette
	{
		get
		{
			return colorMode == ColorMode.Silhouette;
		}
	}
	public int columns = 1; //how many columns it will have, MUST stay above 0
	public int rows = 1;
	public int iconIndex = 0; //for non-animating icons

	

	public float animDelay = 0f; //delay between frames, if <= 0, no animation
	public int[] frames;
	//public int currentFrame = 0; //keep track of frame
	public Vector2 size = Vector2.one; //1,1 means full width & height of a normal letter. determines spacing.
	public Vector3 offset = Vector3.zero;
	//public Vector2 tiling = Vector2.one; //the uvs of the texture to use
	//public Vector2 offset = Vector2.zero;
	public float advance; //spacing afterwards...?
/*
	public Vector2 tiling{ //get the tiling for this texture
		get{

		}
	}
*/
	public Vector3 TopLeftVert{ //shorthand for the corners
		get{
			return new Vector3(0f, size.y, 0f) + offset; 
		}
	}
	public Vector3 TopRightVert{ //shorthand for the corners
		get{
			return new Vector3(size.x, size.y, 0f) + offset;
		}
	}
	public Vector3 BottomRightVert{ //shorthand for the corners
		get{
			return new Vector3(size.x, 0f, 0f) + offset;
		}
	}
	public Vector3 BottomLeftVert{ //shorthand for the corners
		get{
			return new Vector3(0f, 0f, 0f) + offset;
		}
	}
	public Vector3 Middle
	{
		get
		{
			return new Vector3(size.x * 0.5f, size.y * 0.5f, 0f) + offset;
		}
	}

	public Vector2 UvTopLeft(float myTime, int myIconIndex){ 
		return new Vector2(0f, uvSize.y) + UvOffset(myTime, myIconIndex); 
	}
	public Vector2 UvTopRight(float myTime, int myIconIndex){ 
		return uvSize + UvOffset(myTime, myIconIndex);
	}
	public Vector2 UvBottomRight(float myTime, int myIconIndex){ 
		return new Vector2(uvSize.x, 0f) + UvOffset(myTime, myIconIndex);
	}
	public Vector2 UvBottomLeft(float myTime, int myIconIndex){ 
		return UvOffset(myTime, myIconIndex);
	}
	public Vector2 UvMiddle(float myTime, int myIconIndex)
	{
		return (uvSize * 0.5f) + UvOffset(myTime, myIconIndex);
	}
	public Vector2 uvSize {
		get
		{
			return new Vector2(1f / (float)columns, 1f / (float)rows);
		}
	}
	public Vector2 pixelSize
	{
		get
		{
			return new Vector2(uvSize.x * texture.width, uvSize.y * texture.height);
		}
	}
	public Vector2 UvOffset(float myTime, int myIconIndex){
		FixColumnCount();
		/*
		XNXX N would be at position "13"
		XXOX O would be at position "10"
		XXXX grid width is columns wide
		XXXX use modulo
		*/
		if(myIconIndex < 0 && (columns > 1 || rows > 1) && animDelay > 0f && frames.Length > 0f){
			myIconIndex = frames[(int)Mathf.Floor(myTime / animDelay) % frames.Length]; //dont bother wrapping frames, since they're set manually
			//myIconIndex %= (columns * columns); //wrap...
		}else{
			myIconIndex = myIconIndex > -1 ? myIconIndex : iconIndex; //if it's greater than -1, use override
			myIconIndex = myIconIndex % (columns * rows); //wrap it!
		}
		
		//10.4 seconds in, 0.2 sec delay, get index 2 (3rd one)
		
		int row = (int)Mathf.Floor((float)myIconIndex / (float)columns);
		//int row = myIconIndex % rows;
		int column = myIconIndex % columns;
		return new Vector2((float)column / (float)columns, (float)row / (float)rows);
	}
	void OnValidate(){
		FixColumnCount();
	}
	void FixColumnCount(){
		if(columns < 1) columns = 1;
		if(rows < 1)rows = 1;
	}
	#if UNITY_EDITOR
	public override void DrawCustomInspector(SuperTextMesh stm, SerializedObject serializedData, SuperTextMeshData data){
#if UNITY_2017_1_OR_NEWER
		if(GUILayout.Button("Toggle Preview"))
		{
			data.TogglePreview("<q=" + this.name + ">Hello, World!<q=" + this.name + ">", this);
		}
		if(data.previewData == this)
		{
			STMCustomInspectorTools.DrawRenderPreview(data, -1f);
		}
#endif
	//Title bar:
		STMCustomInspectorTools.DrawTitleBar(this,stm,data);
	//the rest:
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField(serializedData.FindProperty("texture"));
		if(this.texture != null){
			EditorGUILayout.PropertyField(serializedData.FindProperty("colorMode"));
			EditorGUILayout.PropertyField(serializedData.FindProperty("overrideFilterMode"));
			EditorGUI.BeginDisabledGroup(!this.overrideFilterMode);
			EditorGUILayout.PropertyField(serializedData.FindProperty("filterMode"));
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space(); //////////////////SPACE
			EditorGUILayout.PropertyField(serializedData.FindProperty("columns"));
			EditorGUILayout.PropertyField(serializedData.FindProperty("rows"));
			EditorGUILayout.Space(); //////////////////SPACE
			if(this.animDelay <= 0f){
				EditorGUILayout.PropertyField(serializedData.FindProperty("iconIndex")); //use single icon index
			}
			if(this.columns > 1){
				EditorGUILayout.PropertyField(serializedData.FindProperty("animDelay"));
				if(this.animDelay > 0f){
					EditorGUILayout.PropertyField(serializedData.FindProperty("frames"), true); //iterate thru multiple
				}
			}
			EditorGUILayout.Space(); //////////////////SPACE
		}
		EditorGUILayout.PropertyField(serializedData.FindProperty("size"));
		EditorGUILayout.PropertyField(serializedData.FindProperty("offset"));
		EditorGUILayout.PropertyField(serializedData.FindProperty("advance"));
		if(EditorGUI.EndChangeCheck())
		{
#if UNITY_2017_1_OR_NEWER
			data.forceRebuild = true;
#endif
		}
	}
	#endif

}