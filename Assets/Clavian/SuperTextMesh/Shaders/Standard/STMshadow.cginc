//Copyright (c) 2016-2026 Kai Clavier [kaiclavier.com] Do Not Distribute
#include "UnityCG.cginc"
//base stuff that can be used by any STM shader

struct appdata
{
    float4 vertex : POSITION;
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


struct v2f
{
    V2F_SHADOW_CASTER;
    float4 color : COLOR;
    float4 uv : TEXCOORD0;
    #if defined(UNITY_STEREO_INSTANCING_ENABLED)
    UNITY_VERTEX_OUTPUT_STEREO
    #endif
};

sampler2D _MainTex;
uniform float4 _MainTex_ST;
sampler2D _MaskTex;
uniform float4 _MaskTex_ST;
float _SDFCutoff;
float _ShadowCutoff;

v2f vert (appdata v, uint id : SV_VERTEXID)
{
    #if IS_URP
    Varyings o;
    #else
    v2f o;
    #endif
    //single-pass stereo rendering:
    #if defined(UNITY_STEREO_INSTANCING_ENABLED)
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    #endif
    o.color = v.color;
    #if UNITY_VERSION < 202030
    o.uv.xy = TRANSFORM_TEX(v.uv.xy, _MainTex);
    o.uv.zw = TRANSFORM_TEX(v.uv2.xy, _MaskTex);
    #else
    o.uv.xy = TRANSFORM_TEX(v.uv.xy, _MainTex);
    o.uv.zw = TRANSFORM_TEX(v.uv.zw, _MaskTex);
    #endif
    TRANSFER_SHADOW_CASTER(o)
    return o;
}

float4 when_lt(float4 x, float4 y) {
    return max(sign(y - x), 0.0);
}
float4 when_ge(float4 x, float4 y) {
    return 1.0 - when_lt(x, y);
}

half4 frag(v2f i) : SV_Target
{
    half4 col = half4(0,0,0,0);
    half4 text = tex2D(_MainTex, i.uv.xy);
    half4 mask = tex2D(_MaskTex, i.uv.zw);
    // #if SDF_MODE
    // col += (mask * i.color) * when_ge(text.a, _SDFCutoff);
    // #else
    col = text * mask * i.color;
    //#endif
    clip(col.a - _ShadowCutoff);
    //if(col.a < _ShadowCutoff) discard;
    SHADOW_CASTER_FRAGMENT(i)
    return col;
}