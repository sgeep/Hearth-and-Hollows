Shader "Super Text Mesh/URP/Ultra"
{
	Properties 
	{
		_MainTex ("Font Texture", 2D) = "white" {}
		_MaskTex ("Mask Texture", 2D) = "white" {}
		[Toggle(SDF_MODE)] _SDFMode ("Toggle SDF Mode", Float) = 0
		[ShowIf(SDF_MODE)] _SDFCutoff ("SDF Cutoff", Range(0,1)) = 0.5
		[ShowIf(SDF_MODE)] _Blend ("Blend Width", Range(0.0001,1)) = 0.05
		[ShowIf(SDF_MODE)] _EffectBlend ("Effect Blend Width", Range(0.0001,1)) = 0.05
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
		_ShadowCutoff ("Shadow Cutoff", Range(0,1)) = 0.5
		_Cutoff ("Cutoff", Range(0,1)) = 0.0001 //text cutoff
		[Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 0
		[Enum(Normal,4,On Top,8)] _ZTestMode ("ZTest Mode", Float) = 4
		[Enum(Off,0,On,1)] _ZWriteMode ("ZWrite Mode", Float) = 1
		
		_StencilComp ("Stencil Comparison", Float) = 8 //_StencilComp
		_Stencil ("Stencil Mode", Float) = 0 //_Stencil
		_StencilOp ("Stencil Operation", Float) = 0 //_StencilOp
        _StencilWriteMask ("Stencil Write Mask", Float) = 255 //_StencilWriteMask
		_StencilReadMask ("Stencil Read Mask", Float) = 255 //_StencilReadMask
		_ColorMask ("Color Mask", Float) = 15

		//[Enum(None, 0, Drop Shadow, 1, Outline, 2)] _ExpansionEffect ("Enable Expansion Effects (outline/dropshadows)", int) = 0
		
		[Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
		[MaterialToggle] _CastShadows ("Cast Shadows", Float) = 0
		[Toggle(UI_MODE)] _UIMode ("UI Mode", Float) = 0 //for use with OLD versions of unity... this is really just _CastShadows reversed lol
		
		//enum mode for now
		//https://docs.unity3d.com/ScriptReference/MaterialPropertyDrawer.html
		[KeywordEnum(None, Dropshadow, Outline, Both, BothThick)] _Effect ("Effect Mode", Float) = 0
		//outline settings
		//[Toggle(EFFECT_OUTLINE)] _OutlineEnabled ("Outline Enabled", Float) = 0
		[ShowIf(EFFECT_OUTLINE)] _OutlineColor ("Outline Color", Color) = (0,0,0,1)
		[ShowIf(EFFECT_OUTLINE)] _OutlineTexture ("Outline Texture", 2D) = "white" {}
		[ShowIf(EFFECT_OUTLINE)] _OutlineTextureScroll ("Outline Texture Scroll", Vector) = (1,1,0)
        [ShowIf(EFFECT_OUTLINE)] _OutlineWidth ("Outline Width", Float) = 0.05
		[ShowIf(EFFECT_OUTLINE)][Enum(Circle, 0, Square, 1)] _OutlineType ("Outline Type", Float) = 0
		[ShowIf(EFFECT_OUTLINE)] _OutlineSamples  ("Outline Samples", Range(1, 256)) = 32
		//dropshadow
		//[Toggle(EFFECT_DROPSHADOW)] _DropshadowEnabled ("Dropshadow Enabled", Float) = 0
		[ShowIf(EFFECT_DROPSHADOW)] _DropshadowColor ("Dropshadow Color", Color) = (0,0,0,1)
		[ShowIf(EFFECT_DROPSHADOW)] _DropshadowTexture ("Dropshadow Texture", 2D) = "white" {}
		[ShowIf(EFFECT_DROPSHADOW)] _DropshadowTextureScroll ("Dropshadow Texture Scroll", Vector) = (1,1,0)
		[ShowIf(EFFECT_DROPSHADOW)][Enum(Angle, 0, Vector, 1)] _DropshadowType ("Dropshadow Type", Float) = 0
		[ShowIf(EFFECT_DROPSHADOW)] _DropshadowAngle ("Dropshadow Angle", Range(0,360)) = 135
		[ShowIf(EFFECT_DROPSHADOW)] _DropshadowDistance ("Dropshadow Distance", Float) = 1
		[ShowIf(EFFECT_DROPSHADOW)] _DropshadowAngle2 ("Dropshadow Vector", Vector) = (1,-1,0)
		
		[ShowIf(EFFECT_DROPSHADOW, EFFECT_OUTLINE)][Toggle(EFFECT_SCALING)] _EffectScaling ("Effect Scaling", Float) = 1
		[ShowIf(EFFECT_DROPSHADOW, EFFECT_OUTLINE)] _EffectDepth ("Effect Depth", Range(0.001, 1)) = 0.01
		/*
		what do i want for inspector...
		*/
	}
	SubShader {
		Tags {
			"RenderPipeline" = "UniversalPipeline"
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"STMUberShader2"="Yes"
			"STMMaskingSupport"="Yes"
		}
		Stencil
		{  
			Ref [_Stencil]  //Customize this value  
			Comp [_StencilComp] //Customize the compare function  
			Pass [_StencilOp]
			ReadMask [_StencilReadMask]
			WriteMask [_StencilWriteMask]
		}
		
		LOD 100

		Lighting Off
		Cull [_CullMode]
		ZTest [_ZTestMode]
		ZWrite [_ZWriteMode] //this can be off if there's no dropshadow/outline. those use the z buffer.
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask [_ColorMask]

		Pass 
		{
			//this page has an explanation of multi-compile:
			//https://docs.unity3d.com/2019.3/Documentation/Manual/SL-MultipleProgramVariants.html
			//https://docs.unity3d.com/Manual/SL-MultipleProgramVariants.html
			HLSLPROGRAM
			#include "STMultraURP.cginc"
			//needed for instancing
			#pragma target 4.0
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_local __ UNITY_UI_CLIP_RECT
			#pragma multi_compile_instancing //instancing
			//only works when non-local. Also do not put comments on the same line as #pragma pre-2017.1
			#pragma multi_compile __ UI_MODE
			//#pragma shader_feature EFFECT_SCALING
			#pragma shader_feature SDF_MODE
			#pragma shader_feature PIXELSNAP_ON
			#pragma shader_feature EFFECT_SCALING
			//for now, only allow one at a time
			#pragma shader_feature _EFFECT_NONE _EFFECT_DROPSHADOW _EFFECT_OUTLINE _EFFECT_BOTH _EFFECT_BOTHTHICK
			
			#define INVERT_VERTICES_ORDER (SHADER_API_VULKAN || SHADER_API_GLES || SHADER_API_GLES3)
			
			//#pragma shader_feature_local 
			ENDHLSL
		}
		
		//note on this:
		//disabled by the very end of shadow frag for UI elements.
		ZWrite [_CastShadows]
		Pass
		{
			Tags {"LightMode"="ShadowCaster"}

			HLSLPROGRAM
			#include "STMshadowURP.cginc"
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile __ UI_MODE
			#pragma shader_feature SDF_MODE
			ENDHLSL
		}
	}
	FallBack "GUI/Text Shader"
}