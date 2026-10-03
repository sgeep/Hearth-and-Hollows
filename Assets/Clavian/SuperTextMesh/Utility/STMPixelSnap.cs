//Copyright (c) 2025 Kai Clavier [kaiclavier.com] Do Not Distribute
using System;
using System.Collections;
using System.Collections.Generic;
using Clavian;
using UnityEngine;

/*
 * Moves vertices around to snap to a grid.
 * Really just needed for pixel text that's center-aligned!
 */

[ExecuteInEditMode]
[RequireComponent(typeof(SuperTextMesh))]
public class STMPixelSnap : MonoBehaviour
{
	[SerializeField]
	private SuperTextMesh stm;
    
	[Tooltip("Value in world space that this element should snap by.")]
	[Range(0.00001f,1f)]
	public float snapping = 0.01f;
	private void Reset()
	{
		this.stm = GetComponent<SuperTextMesh>();
	}
	private void OnEnable()
	{
		if(stm != null)
		{
			stm.OnVertexMod += Align;
            
			if(stm.gameObject.activeInHierarchy)
			{
				stm.Rebuild();
			}
		}
	}
	private void OnDisable()
	{
		if(stm != null)
		{
			stm.OnVertexMod -= Align;
            
			if(stm.gameObject.activeInHierarchy)
			{
				stm.Rebuild();
			}
		}
	}
	
	[ContextMenu("ForceRebuild")]
	private void ForceRebuild()
	{
		stm.Rebuild();
	}
	
	private void Align(Vector3[] verts, Vector3[] middles, Vector3[] positions)
	{
		var diffX = stm.finalWidth % snapping; //difference to next snap value.
		var diffY = stm.finalHeight % snapping;
		Vector2 moveBy = Vector2.zero;
		if(diffX < snapping / 2f)
		{
			moveBy.x -= diffX;
		}
		else
		{
			moveBy.x += diffX;
		}
		if(diffY < snapping / 2f)
		{
			moveBy.y -= diffY;
		}
		else
		{
			moveBy.y += diffY;
		}
		//convert to correct space
		moveBy = stm.t.InverseTransformVector(moveBy);
		if(moveBy == Vector2.zero) return;
		for(var i = 0; i < verts.Length; i++)
		{
			verts[i].x += moveBy.x;
			verts[i].y += moveBy.y;
		}
	}
}