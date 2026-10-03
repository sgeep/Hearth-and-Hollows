//Copyright (c) 2026 Kai Clavier [kaiclavier.com] Do Not Distribute
// using System; // DRAQUE
// using System.Reflection; // DRAQUE
using UnityEngine;
using UnityEngine.UI;

/*
 * limit of 1 special rebuild per frame...?
 */
public class SuperTextMeshSubmesh : MonoBehaviour
{
	[HideInInspector] public SuperTextMesh superTextMesh;
	private Transform t;
	private RectTransform rt;
	private GameObject go;
	private MeshRenderer r;
	private MeshFilter f;
	private CanvasRenderer c;
	private STMMaskableGraphic mask;
	private bool uiMode = false;
	private STMMaskableGraphic stmMask;
	[HideInInspector] public int submeshIndex;
	[HideInInspector] public Mesh mesh;

	public void Initialize(SuperTextMesh stm, int submeshIndex)
	{
		//called by STM.
		superTextMesh = stm;
		this.submeshIndex = submeshIndex;
		t = this.transform;
		go = this.gameObject;
		//for debugging, HideFlags.DontSave. Otherwise... HideFlags.HideAndDontSave
		go.hideFlags = HideFlags.HideAndDontSave;
		t.SetParent(stm.t);
		t.localPosition = Vector3.zero;
		uiMode = stm.uiMode;
		if(uiMode)
		{
			rt = GetComponent<RectTransform>();
			rt.sizeDelta = stm.tr.sizeDelta;
			c = GetComponent<CanvasRenderer>();
			//also add any effects.
			stmMask = stm.GetComponent<STMMaskableGraphic>();
			if(stmMask != null)
			{
				mask = go.AddComponent<STMMaskableGraphic>();
				mask.raycastTarget = stmMask.raycastTarget;
				mask.maskable = stmMask.maskable;
				// mask.meshGenerationMode = STMMaskableGraphic.MeshGenerationMode.SubMeshes;
				var effects = stm.GetComponents<BaseMeshEffect>();
				foreach(var effect in effects)
				{
					CopyComponent(effect, go);
				}
			}
			
		}
		else
		{
			f = GetComponent<MeshFilter>();
			r = GetComponent<MeshRenderer>();
		}


		mesh = new Mesh();
		mesh.MarkDynamic();
		
	}

	public void Set(SuperTextMesh stm, SubmeshData data)
	{
		Clear();
		
		//apply based on submesh data...
		mesh.vertices = stm.activeVerts;
		mesh.colors32 = stm.activeCol32s;
#if UNITY_2020_3_OR_NEWER
		//arrays are supported instead of just lists
		mesh.SetUVs(0, stm.endUv);
		mesh.SetUVs(1, stm.endUv2);
		mesh.SetUVs(2, stm.endUv3);
		mesh.SetUVs(3, stm.endUv4);
#else
		//can only handle Vector2 in first two channels due to VertexHelper
		mesh.uv = stm.ToVector2Array(stm.endUv, true); //uv.xy
		mesh.uv2 = stm.ToVector2Array(stm.endUv, false); //uv.zw
		mesh.SetUVs(2, stm.ArrayToList(stm.endUv2));
		mesh.SetUVs(3, stm.ArrayToList(stm.endUv3));
#endif
		mesh.SetTriangles(data.tris, 0);
		//Debug.Log("I have this many tris!" + mesh.triangles.Length);
		mesh.UploadMeshData(false);
		
		ApplyMesh();
	}

	public void ApplyMaterial(Material mat)
	{
		if(r == null) return;
		r.sharedMaterials = new Material[]{mat};
	}

	public void ApplyMaterialUI(Material mat)
	{
		if(c == null) return;
		c.materialCount = 1;
		c.SetMaterial(mat, 0);
		//Debug.Log("yep");
	}

	public void Clear()
	{
		mesh.Clear();
	}
	
	void ApplyMesh()
	{
		if(uiMode) //UI mode
		{
			//if(c == null) c = this.GetComponent<CanvasRenderer>();
			//if(c == null) c = this.AddComponent<CanvasRenderer>();
			c.SetMesh(mesh);
			//Debug.Log("Setting mesh.");
		}
		else
		{
			//if(f == null) f = this.GetComponent<MeshFilter>();
			//if(f == null) f = this.AddComponent<MeshFilter>();
			f.sharedMesh = mesh;
			//Debug.Log("Setting mesh 2.");
		}
		
	}

	// DRAQUE -->
	//from https://discussions.unity.com/t/how-to-get-a-component-from-an-object-and-add-it-to-another-copy-components-at-runtime/80939/11
	// void CopyComponent(Component component, GameObject target)
	// {
	// 	Type type = component.GetType();
	// 	target.AddComponent(type);
	// 	PropertyInfo[] propInfo = type.GetProperties(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
	// 	foreach (var property in propInfo)
	// 	{
	// 		if (property.Name == "rect") continue;
	// 		property.SetValue(target.GetComponent(type), property.GetValue(component, null), null);
	// 	}
	// }

	void CopyComponent(BaseMeshEffect component, GameObject target)
	{
		var outline = component as Outline;
		if (!ReferenceEquals(outline, null))
			CopyShadowSettings(outline, target.AddComponent<Outline>());
		else
		{
			var shadow = component as Shadow;
			if (!ReferenceEquals(shadow, null))
				CopyShadowSettings(shadow, target.AddComponent<Shadow>());
			else if (component is PositionAsUV1)
				target.AddComponent<PositionAsUV1>();
			else
				Debug.LogWarning(string.Format("Unexpected component of type {0}", component.GetType()));
		}
	}

	// Because this is not generalized, values must be pulled explicitly
	void CopyShadowSettings(Shadow source, Shadow target)
	{
		target.effectColor = source.effectColor;
		target.effectDistance = source.effectDistance;
		target.useGraphicAlpha = source.useGraphicAlpha;
		target.enabled = source.enabled;
	}
	// <-- DRAQUE
}