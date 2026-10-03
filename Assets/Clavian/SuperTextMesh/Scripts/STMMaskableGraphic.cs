//Copyright (c) 2025-2026 Kai Clavier [kaiclavier.com] Do Not Distribute
//True Shadow belongs to Tai's Assets [leloctai.com]

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
[CustomEditor(typeof(STMMaskableGraphic))]
[CanEditMultipleObjects] //sure why not
public class STMMaskableGraphicEditor : Editor
{
	public override void OnInspectorGUI()
	{
		var o = target as STMMaskableGraphic;
		if(o == null) return;
		serializedObject.Update(); //for onvalidate stuff!

		// if(o.displayIncompatibilityWarning)
		// {
		// 	EditorGUILayout.HelpBox(
		// 	"Effect Components that use IMeshModifer (e.g. 'Shadow' and 'Outline') are " +
		// 	"incompatible with Super Text Mesh's Ultra Shader's own effects. " +
		// 	"If this Super Text Mesh component uses the Ultra Shader with Effects " +
		// 	"enabled, IMeshMofidier effects will be disabled on *all materials* on this mesh.", MessageType.Warning);
		// }
		
		o.raycastTarget = EditorGUILayout.Toggle("Raycast Target", o.raycastTarget);
		//EditorGUILayout.PropertyField(serializedObject.FindProperty("raycastTarget"));
		o.maskable = EditorGUILayout.Toggle("Maskable", o.maskable);

		o.meshGenerationMode = (STMMaskableGraphic.MeshGenerationMode)EditorGUILayout.EnumPopup(new GUIContent("Mesh Generation Mode", "Changes the Mesh Generation method."), o.meshGenerationMode);

		//outline inherits from shadow so that's why this is written like this.
		var myShadow = o.gameObject.GetComponent<UnityEngine.UI.Shadow>();
		if(myShadow == null || myShadow.GetType() != typeof(UnityEngine.UI.Shadow))
		{
			if(GUILayout.Button("Add Shadow"))
			{
				Undo.AddComponent<UnityEngine.UI.Shadow>(o.gameObject);
			}
		}

		if(o.gameObject.GetComponent<UnityEngine.UI.Outline>() == null)
		{
			if(GUILayout.Button("Add Outline"))
			{
				Undo.AddComponent<UnityEngine.UI.Outline>(o.gameObject);
			}
		}
#if LETAI_TRUESHADOW
		if(GUILayout.Button("Add True Shadow"))
		{
			Undo.AddComponent<LeTai.TrueShadow.TrueShadow>(o.gameObject);
		}
#endif
		serializedObject.ApplyModifiedProperties();
		if(GUI.changed)
		{
			o.DoOnValidate();
		}
	}
}
#endif

[Obsolete]
public class STMMaskableGraphic : MaskableGraphic
#if LETAI_TRUESHADOW
	, LeTai.TrueShadow.PluginInterfaces.ITrueShadowCustomHashProvider
	, LeTai.TrueShadow.PluginInterfaces.ITrueShadowCasterSubMeshMaterialProvider
	, LeTai.TrueShadow.PluginInterfaces.ITrueShadowCasterMeshProvider
#endif
{
	private SuperTextMesh _superTextMesh;
	private SuperTextMeshSubmesh _superTextMeshSubmesh;

	public bool submeshMode = false;
	
	private List<VertexHelper> _vertexHelpers = new List<VertexHelper>();

	private List<Mesh> _workerMeshes = new List<Mesh>();
	private Mesh _workerMesh;

	private int _subMeshCount;
	public enum MeshGenerationMode 
	{
		MergedStream,
		SubMeshes,
		SplitSubMeshes
	}

	[SerializeField]
	private MeshGenerationMode _meshGenerationMode = MeshGenerationMode.MergedStream;
	public MeshGenerationMode meshGenerationMode
	{
		get
		{
			return _meshGenerationMode;
		}
		set
		{
			useLegacyMeshGeneration = value != MeshGenerationMode.MergedStream;
			_meshGenerationMode = value;
		}
	}
	
#if LETAI_TRUESHADOW
	private LeTai.TrueShadow.TrueShadow[] _shadows;
	private int _shadowSearchLength = -1;
#endif
	
#if UNITY_EDITOR
	//internal bool displayIncompatibilityWarning;
#endif

	public override Texture mainTexture
	{
		get
		{
			return GetCachedMaterialForRendering(0).mainTexture;
		}
	}

	public override Material material
	{
		get { return canvasRenderer.GetMaterial(0); }
		set { canvasRenderer.SetMaterial(value, 0); }
	}

	private void GatherComponents()
	{
		if(_superTextMesh == null) _superTextMesh = GetComponent<SuperTextMesh>();
		if(_superTextMeshSubmesh == null) _superTextMeshSubmesh = GetComponent<SuperTextMeshSubmesh>();
		if(_superTextMeshSubmesh != null) submeshMode = true;
		if(submeshMode)
		{
			_superTextMesh = _superTextMeshSubmesh.superTextMesh;
		}
	}

	protected override void OnEnable()
	{
		
		GatherComponents();
		
		_superTextMesh.OnRebuildEvent += OnRebuild;
		_superTextMesh.OnSetMeshEvent += OnSetMesh;
		_superTextMesh.OnCompleteEvent += OnComplete;
		
		_workerMesh = new Mesh();
		_workerMesh.MarkDynamic();
		this.meshGenerationMode = this.meshGenerationMode; //reapply legacy mode!
		
#if LETAI_TRUESHADOW
		_shadows = GetComponents<LeTai.TrueShadow.TrueShadow>();
		_shadowSearchLength = _shadows.Length;
#endif
		
#if UNITY_EDITOR
		_setDirtyThisFrame = true;
#endif
		base.OnEnable();

		if(_superTextMesh.isActiveAndEnabled)
		{
			_superTextMesh.SpecialRebuild(true);
		}
	}

	protected override void OnDisable()
	{

		_superTextMesh.OnRebuildEvent -= OnRebuild;
		_superTextMesh.OnSetMeshEvent -= OnSetMesh;
		_superTextMesh.OnCompleteEvent -= OnComplete;
		
		base.OnDisable();

		if(_workerMesh != null)
		{
			if(Application.isPlaying) Destroy(_workerMesh);
			else DestroyImmediate(_workerMesh);
		}

		if(_superTextMesh != null && _superTextMesh.isActiveAndEnabled)
        {
            _superTextMesh.SpecialRebuild(false);
        }
	}
	
	protected override void OnDestroy()
	{
		base.OnDestroy();
		foreach(var mesh in _workerMeshes)
		{
			if(mesh != null)
			{
				if(Application.isPlaying) Destroy(mesh);
				else DestroyImmediate(mesh);
			}
		}
	}

	#if UNITY_EDITOR
	protected override void Reset()
	{
		base.Reset();

	}
	#endif

#if UNITY_EDITOR
	internal void DoOnValidate()
	{
		OnValidate();
	}
	protected override void OnValidate()
	{
		
		GatherComponents();
#if LETAI_TRUESHADOW
		_shadows = GetComponents<LeTai.TrueShadow.TrueShadow>();
		_shadowSearchLength = _shadows.Length;
		// if(_shadows.Length > 0)
		// {
		// 	meshGenerationMode = MeshGenerationMode.SubMeshes;
		// }
#endif
		base.OnValidate();
	}
#endif


	private void OnRebuild()
	{
		if (canvasRenderer == null || canvasRenderer.cull)
			return;

		if(_superTextMesh == null || _superTextMesh.textMesh == null)
			return;

		if(!_superTextMesh.gameObject.activeSelf)
			return;
		
		UpdateGeometry();
					
		UpdateMaterial();
		
#if UNITY_EDITOR
		_setDirtyThisFrame = true; //don't call again.
#endif

	}

	//used to be LateUpdate
	private void OnSetMesh()
	{
		// if(_superTextMesh.isAnimating || _superTextMesh.reading || _superTextMesh.unreading || _superTextMesh.forceAnimation)
		// {
			UpdateGeometry();
		// }
#if UNITY_EDITOR
		if(_setDirtyThisFrame)
		{
			_setDirtyThisFrame = false;
		}
#endif
	}

	private void OnComplete()
	{
		UpdateGeometry();
		
		//UpdateMaterial();
		
#if UNITY_EDITOR
		_setDirtyThisFrame = true; //don't call again.
#endif
	}


	public override void Rebuild(CanvasUpdate update)
	{
		//do nothing!
		//STM's rebuild callback will handle it after STM is done
	}
#if UNITY_EDITOR
	private bool _setDirtyThisFrame;
#endif
	public override void SetVerticesDirty()
	{
#if UNITY_EDITOR
		if (!IsActive())
			return;
#endif
		
		base.SetVerticesDirty();
		
		//since this just syncs with STM... this means another component has set this.
#if UNITY_EDITOR
		if(!_setDirtyThisFrame)
		{
#endif
			OnRebuild();
#if UNITY_EDITOR
			_setDirtyThisFrame = true;
		}
#endif
	}

	private Material[] _cacheMaterials = new Material[0];
	//for use with shadow and other modifiers.
	protected override void UpdateMaterial()
	{
		if (!IsActive())
			return;

		if(canvasRenderer.materialCount == 0) return;
		
		if(_cacheMaterials.Length != canvasRenderer.materialCount)
			Array.Resize(ref _cacheMaterials, canvasRenderer.materialCount);
		
		for(int i = 0; i < canvasRenderer.materialCount; i++)
		{
			_cacheMaterials[i] = GetMaterialForRendering(i);
			canvasRenderer.SetMaterial(_cacheMaterials[i], i);
			//canvasRenderer.SetTexture(myMaterial.mainTexture);
		}
	}

	private Material GetCachedMaterialForRendering(int index)
	{
		if(_cacheMaterials.Length > index)
			return _cacheMaterials[index];
		return null;
	}
	private Material GetMaterialForRendering(int index)
	{
		//replacing materialForRendering...
		var components = new List<Component>();
		GetComponents(typeof(IMaterialModifier), components);
		
		//the shader needs to work with UNITY_UI_CLIP_RECT for this to work right.
		//this took multiple YEARS to find btw because the docs for RectMask2D has no mention of this.
		//it doesn't seem that the material needs to be duplicated for this to play nice
		//var currentMat = _superTextMesh.submeshes[index].sharedMaterialData.material;
		var currentMat = canvasRenderer.GetMaterial(index);
		for(var i = 0; i < components.Count; i++)
			currentMat = ((IMaterialModifier)components[i]).GetModifiedMaterial(currentMat);

		return currentMat;
	}
	//implementation as IMaterialModifier
	//override since STM handles this itself
	public override Material GetModifiedMaterial(Material baseMaterial)
	{
		//honestly not sure why this works, shouldn't it break material refs? ah well...
		_superTextMesh.RecalculateMasking();
		return baseMaterial;
	}
	
	private Vector3[] midVerts = new Vector3[0];
	private Color32[] midCol32 = new Color32[0];
	private Vector4[] endUv = new Vector4[0];
	// private Vector2[] endUv = new Vector2[0];
	//private Vector4[] endUv2 = new Vector4[0]; //overlay images
	private Vector4[] endUv2 = new Vector4[0]; //ratios of each letter, to be embedded into uv3

	private Vector4[] endUv3 = new Vector4[0];
	private Vector4[] endUv4 = new Vector4[0];
	//private int[][] tris = new int[1][];
	private List<List<int>> tris = new List<List<int>>();
	private List<UIVertex>[] vertexStreams = new List<UIVertex>[0];

	//private Vector3[] vertexStream0;

	//this replaces VertexHelper.FillMesh:
	private void MergeMesh(Mesh mesh)
	{
		//use vertex streams to fill this mesh
		//so this is like VertexHelper.FillMesh but taking multiple vertex streams and compressing into one mesh
		//so, go through every VertexHelper and combine stuff.
		//also Mesh.CombineMeshes() doesn't work right for this, so...
		
		//clear the goal mesh
		mesh.Clear();
		//get one vertex stream for each submesh
		if(vertexStreams.Length != _subMeshCount)
		{
			Array.Resize(ref vertexStreams, _subMeshCount);
		}

		var triCount = 0;
		var vertCount = 0;
		for(int i = 0; i < _subMeshCount; i++)
		{
			//populate each
			if(vertexStreams[i] == null)
				vertexStreams[i] = new List<UIVertex>();
			_vertexHelpers[i].GetUIVertexStream(vertexStreams[i]);
			triCount += vertexStreams[i].Count;
		}

		for(int i = 0; i < triCount; i += 6)
		{
			vertCount+=4;
		}
		
		
		if(midVerts.Length != vertCount)
			Array.Resize(ref midVerts, vertCount);
		if(midCol32.Length != vertCount)
			Array.Resize(ref midCol32, vertCount);
		if(endUv.Length != vertCount)
			Array.Resize(ref endUv, vertCount);
		if(endUv2.Length != vertCount)
			Array.Resize(ref endUv2, vertCount);
		if(endUv3.Length != vertCount)
			Array.Resize(ref endUv3, vertCount);
		if(endUv4.Length != vertCount)
			Array.Resize(ref endUv4, vertCount);
		
		tris.Clear();
		
		
		//manual merge...
		int vertIndex = 0;
		int triIndex = 0;
		//for each submesh...
		for(int i = 0; i < _subMeshCount; i++)
		{
			tris.Add(new List<int>());
			//counting thru tris
			for(int v = 0; v < vertexStreams[i].Count; v+=6)
			{
				WriteToVerts(i, v, 0, ref vertIndex);
				WriteToVerts(i, v, 1, ref vertIndex);
				WriteToVerts(i, v, 2, ref vertIndex);
				WriteToVerts(i, v, 5, ref vertIndex);
			}
			//generate tri indexes
			for(int t = 0; t < vertexStreams[i].Count; t+=6)
			{
				tris[i].Add(triIndex+0);
				tris[i].Add(triIndex+1);
				tris[i].Add(triIndex+2);
				tris[i].Add(triIndex+0);
				tris[i].Add(triIndex+2);
				tris[i].Add(triIndex+3);
				triIndex += 4;
			}
			
		}

		//apply same as STM
		mesh.vertices = midVerts;
		mesh.colors32 = midCol32;
		//should work in 2019.3 but alas
#if UNITY_2020_3_OR_NEWER
		mesh.SetUVs(0, endUv);
		mesh.SetUVs(1, endUv2);
		mesh.SetUVs(2, endUv3);
		mesh.SetUVs(3, endUv4);
#else
		mesh.uv = _superTextMesh.ToVector2Array(endUv,true);
		mesh.uv2 = _superTextMesh.ToVector2Array(endUv,false);
		//mesh.SetUVs(1, _superTextMesh.ArrayToList(endUv2));
		//mesh.SetUVs(2, _superTextMesh.ArrayToList(endUv3));
#endif
		mesh.subMeshCount = _subMeshCount;
		for(int i = 0; i < _subMeshCount; i++)
		{
			mesh.SetTriangles(tris[i], i);
		}
		//mesh.UploadMeshData(false);
	}

	private void MergeMeshLegacy(Mesh mesh)
	{
		
	}
	private void WriteToVerts(int i, int v, int t, ref int vertIndex)
	{
		midVerts[vertIndex] = vertexStreams[i][v+t].position;
		midCol32[vertIndex] = vertexStreams[i][v+t].color;
#if UNITY_2020_3_OR_NEWER
		//vector4s are supported, and are sent this way!
		endUv[vertIndex] = vertexStreams[i][v+t].uv0;
		endUv2[vertIndex] = vertexStreams[i][v+t].uv1;
		endUv3[vertIndex] = vertexStreams[i][v+t].uv2;
		endUv4[vertIndex] = vertexStreams[i][v+t].uv3;
#else
		//can only send vector2s... so make sure this is what the vertex stream gets if 2019.2+!
		endUv[vertIndex].x = vertexStreams[i][v+t].uv0.x;
		endUv[vertIndex].y = vertexStreams[i][v+t].uv0.y;
		endUv[vertIndex].z = vertexStreams[i][v+t].uv1.x;
		endUv[vertIndex].w = vertexStreams[i][v+t].uv1.y;
#endif
		vertIndex++;
	}

	protected override void OnPopulateMesh(VertexHelper vh)
	{
		//do nothing, not used
		//add more if needed
		while(_vertexHelpers.Count < _subMeshCount)
		{
			_vertexHelpers.Add(new VertexHelper());
		}
		//clear all
		foreach(var v in _vertexHelpers)
		{
			v.Clear();
		}
		/*
		 * The STM mesh has ONE set of verts, and multiple sets of tris.
		 * here, we are trying to make that into multiple vertex streams, so it can be modified
		 * and then be combined back into one mesh with proper submeshes.
		 * so... using the submesh data stored in STM for this:
		 */
		if(submeshMode)
		{
			//do one specific submesh
			PopulateSubmesh(_vertexHelpers[0], _superTextMeshSubmesh.submeshIndex);
		}
		else
		{
			//do all on one mesh
			for(int i = 0; i < _subMeshCount; i++)
            {
            	PopulateSubmesh(_vertexHelpers[i], i);
            }
		}
		
	}

	private void PopulateSubmesh(VertexHelper vh, int i)
	{
		//Debug.Log("midverts has this many: " + _superTextMesh.activeVerts.Length + 
		//          " ...and there are this many tris: " + (_superTextMesh.submeshes[i].tris.Count));
		var data = _superTextMesh.submeshes[i];
		int triPosition = 0;
		for(int t=0; t<data.tris.Count; t+=6) //go quad-by-quad... so 6 at a time
		{
			//so for every 6 indices, that's 1 quad, and 4 verts!
			int index = data.tris[t]; //start index of this quad (pattern is always 012,023; 456,467)
			for(int v = 0; v < 4; v++) //add 4 verts for this index
			{
#if UNITY_2020_3_OR_NEWER
				//instead of getting from the mesh, get data from STM to be more efficient
				//this would work in 2020.2, but updating for consistency
				 vh.AddVert(_superTextMesh.activeVerts[v+index],
                     				_superTextMesh.activeCol32s[v+index],
                     				_superTextMesh.endUv[v+index],
                     				_superTextMesh.endUv2[v+index],
                     				_superTextMesh.endUv3[v+index],
			                        _superTextMesh.endUv4[v+index],
			                        Vector3.zero, 
			                        Vector4.zero);
/*#elif UNITY_2019_3_OR_NEWER
				//SAYS it supports vector4, but it does not, UIVertex just has Vector2s until 2020.2
				//at this point tho, this has incoming Vector4 data, so reverse convert?
				//and that means after, re-join!!!
				vh.AddVert(_superTextMesh.activeVerts[v+index],
                     				_superTextMesh.activeCol32s[v+index],
                     				_superTextMesh.ToVector2Array(_superTextMesh.endUv, true)[v+index],
									_superTextMesh.ToVector2Array(_superTextMesh.endUv, false)[v+index],
									Vector4.zero,
									Vector4.zero,
			                        Vector3.zero, 
			                        Vector4.zero);*/
#else
				//older versions just support vector2 in the first two channels
				vh.AddVert(_superTextMesh.activeVerts[v+index],
					_superTextMesh.activeCol32s[v+index],
					_superTextMesh.ToVector2Array(_superTextMesh.endUv, true)[v+index],
					_superTextMesh.ToVector2Array(_superTextMesh.endUv, false)[v+index],
					Vector3.zero, 
					Vector4.zero);
#endif
			}
			//and we have the tri data, so just add it here
			vh.AddTriangle(triPosition + 0, triPosition + 1, triPosition + 2);
			vh.AddTriangle(triPosition + 0, triPosition + 2, triPosition + 3);
			triPosition += 4; //triangles increment one at a time, since this will be a new mesh.
		}
	}

	//vertex streams to meshes
	//might be needed if useLegacyMeshGeneration gets stuck on
	private void PrintMeshes()
	{
		//take all vertexhelpers and make into meshes for legacy mode
		while(_workerMeshes.Count < _subMeshCount)
		{
			var newMesh = new Mesh();
			newMesh.MarkDynamic();
			_workerMeshes.Add(newMesh);
		}
		//clear all
		foreach(var m in _workerMeshes)
		{
			m.Clear();
		}

		for(int i = 0; i < _subMeshCount; i++)
		{
			_vertexHelpers[i].FillMesh(_workerMeshes[i]);
		}
	}
	
	[Obsolete("Use OnPopulateMesh(VertexHelper vh) instead. Or don't, Unity's not getting rid of this lol.", false)]
	protected override void OnPopulateMesh(Mesh m)
	{
		OnPopulateMesh((VertexHelper)null); //call this...
		if(meshGenerationMode == MeshGenerationMode.SplitSubMeshes)
		{
			PrintMeshes();
		}
		else
		{
			MergeMesh(m);
		}
	}
	
	//for shadow and other modifiers
	protected override void UpdateGeometry()
	{
		if(_superTextMesh.activeVerts.Length == 0)
			return;

		if(_superTextMesh.textMesh == null)
			return;
		
		if(_superTextMesh.submeshes.Count != _superTextMesh.textMesh.subMeshCount)
			return;
#if UNITY_EDITOR
		//displayIncompatibilityWarning = false;
#endif
		if(submeshMode)
		{
			_subMeshCount = 1;
		}
		else
		{
			_subMeshCount = _superTextMesh.submeshes.Count;
		}
		
		if(useLegacyMeshGeneration)
		{
			DoLegacyMeshGeneration();
		}
		else
		{
			DoMeshGeneration();
		}
#if LETAI_TRUESHADOW
		UpdateTrueShadowHash();
		trueShadowCasterMeshChanged?.Invoke(_workerMesh);
#endif
	}

	private void DoMeshGeneration()
	{
		//This is DoMeshGeneration() now:
		if (rectTransform != null && rectTransform.rect.width >= 0 && rectTransform.rect.height >= 0)
		{
			OnPopulateMesh((VertexHelper)null);
		}
		else
		{
			foreach(var v in _vertexHelpers)
			{
				v.Clear();
			}
			//just return. whatever
			return;
		}

		var components = new List<Component>();
		GetComponents(typeof(IMeshModifier), components);
		//modify every submesh...
		for(var i = 0; i < components.Count; i++)
		{
#if UNITY_2017_1_OR_NEWER
			for(var j=0; j<_subMeshCount; j++)
#else
			int j = 0;
#endif
			{
				/*var mat = canvasRenderer.GetMaterial(j);
				//if this is the ultra shader...
				if(mat.GetTag("STMUberShader2", true, "Null") == "Yes")
				{
					if(mat.GetInt("_Effect") != 0)
					{
						//this surprisingly does work now so I guess I'll leave it in?
#if UNITY_EDITOR
						//displayIncompatibilityWarning = true;
#endif
						//return;
					}
				}
				*/
				((IMeshModifier)components[i]).ModifyMesh(_vertexHelpers[j]);
			}
		}

		if(_workerMesh != null)
		{
			MergeMesh(_workerMesh);
            canvasRenderer.SetMesh(_workerMesh);
		}
		
	}

	private void DoLegacyMeshGeneration()
	{
		if (rectTransform != null && rectTransform.rect.width >= 0 && rectTransform.rect.height >= 0)
		{
#pragma warning disable 618
			OnPopulateMesh(_workerMesh);
#pragma warning restore 618
		}
		else
		{
			_workerMesh.Clear();
		}

		var components = new List<Component>();
		GetComponents(typeof(IMeshModifier), components);

		for (var i = 0; i < components.Count; i++)
		{
			if(meshGenerationMode == MeshGenerationMode.SplitSubMeshes)
			{
				for(var j = 0; j < _subMeshCount; j++)
				{
#pragma warning disable 618
					((IMeshModifier)components[i]).ModifyMesh(_workerMeshes[j]);
#pragma warning restore 618
				}
			}
			else
			{
#pragma warning disable 618
				((IMeshModifier)components[i]).ModifyMesh(_workerMesh);
#pragma warning restore 618
			}
		}

		if(meshGenerationMode == MeshGenerationMode.SplitSubMeshes)
		{
			CombineInstance[] combine = new CombineInstance[_subMeshCount];
			for(var i = 0; i < _subMeshCount; i++)
			{
				combine[i].mesh = _workerMeshes[i];
			}

			_workerMesh.CombineMeshes(combine, false, false);
		}

		canvasRenderer.SetMesh(_workerMesh);
	}
	
#if LETAI_TRUESHADOW
	public void UpdateTrueShadowHash()
	{
		for(int i = 0; i < _shadowSearchLength; i++)
		{
			_shadows[i].CustomHash = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
		}
	}
	public Material GetTrueShadowCasterMaterialForSubMesh(int subMeshIndex)
	{
		return GetCachedMaterialForRendering(subMeshIndex);
	}
	
	public event Action<Mesh> trueShadowCasterMeshChanged;
#endif
}
