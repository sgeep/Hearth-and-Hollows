//Copyright (c) 2026 Kai Clavier [kaiclavier.com] Do Not Distribute
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct Attributes {
	float3 positionOS : POSITION;
	float4 color : COLOR;
	#if UNITY_VERSION < 202030
	float4 uv : TEXCOORD0; //still a float4 for use later
	float2 uv2 : TEXCOORD1;
	#else
	float4 uv : TEXCOORD0;
	#endif
	#if defined(UNITY_STEREO_INSTANCING_ENABLED)
	UNITY_VERTEX_INPUT_INSTANCE_ID
	#endif
};

struct Varyings {
	float4 positionCS : SV_POSITION;
	float4 color : COLOR;
	float4 uv : TEXCOORD0;
	#if defined(UNITY_STEREO_INSTANCING_ENABLED)
	UNITY_VERTEX_OUTPUT_STEREO
	#endif
};

CBUFFER_START(UnityPerMaterial)
	sampler2D _MainTex;
	uniform float4 _MainTex_ST;
	uniform float4 _MainTex_TexelSize;
	sampler2D _MaskTex;
	uniform float4 _MaskTex_ST;
	uniform float4 _MaskTex_TexelSize;
	float _Cutoff;

	float _SDFCutoff;
	float _Blend;
	float _EffectBlend;

	//float2 shadowOffset; //change all references of this to whatever the shadow offset will be
	float4 _DropshadowColor; //change all references of this to whatever the shadow color will be
	sampler2D _DropshadowTexture;
	uniform float4 _DropshadowTexture_ST;
	float4 _DropshadowTextureScroll;
	float _DropshadowAngle;
	float3 _DropshadowAngle2;
	float _DropshadowType;
	float _DropshadowDistance;


	float4 _OutlineColor; //change all references of this to whatever the shadow color will be
	sampler2D _OutlineTexture;
	uniform float4 _OutlineTexture_ST;
	float4 _OutlineTextureScroll;
	float _OutlineWidth; 
	float _OutlineType; //circle or square
	float _OutlineSamples; //taps that are sampled...

	float _EffectDepth;

	float _ShadowCutoff;

CBUFFER_END

//RectMask2D Support
float4 _ClipRect;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;

Varyings vert(Attributes input) {
	Varyings o;
	//single-pass stereo rendering:
	#if defined(UNITY_STEREO_INSTANCING_ENABLED)
	UNITY_SETUP_INSTANCE_ID(v);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
	#endif
	o.color = input.color;
	#if UNITY_VERSION < 202030
	o.uv.xy = TRANSFORM_TEX(input.uv.xy, _MainTex);
	o.uv.zw = TRANSFORM_TEX(input.uv2.xy, _MaskTex);
	#else
	o.uv.xy = TRANSFORM_TEX(input.uv.xy, _MainTex);
	o.uv.zw = TRANSFORM_TEX(input.uv.zw, _MaskTex);
	#endif
	VertexPositionInputs posnInputs = GetVertexPositionInputs(input.positionOS);
	o.positionCS = posnInputs.positionCS;
	// TRANSFER_SHADOW_CASTER(o)
	return o;
}

float4 when_lt(float4 x, float4 y) {
	return max(sign(y - x), 0.0);
}
float4 when_ge(float4 x, float4 y) {
	return 1.0 - when_lt(x, y);
}

half4 frag(Varyings input) : SV_TARGET {
	half4 col = half4(0,0,0,0);
	half4 text = tex2D(_MainTex, input.uv.xy);
	half4 mask = tex2D(_MaskTex, input.uv.zw);
	// #if SDF_MODE
	// col = text * (mask * i.color) * when_ge(text.a, _SDFCutoff);
	// #else
	col = text * mask * input.color;
	//#endif
	clip(col.a - _ShadowCutoff);
	//if(col.a < _ShadowCutoff) discard;
	#if UI_MODE
	return 0;
	#else
	return col;
	#endif
}