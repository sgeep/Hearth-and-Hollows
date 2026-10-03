//Copyright (c) 2016-2026 Kai Clavier [kaiclavier.com] Do Not Distribute

//base stuff that can be used by any STM shader
#include "UnityCG.cginc"
struct appdata {
    float4 vertex : POSITION;
    float4 color : COLOR;
    //before 2020.3 this is split between uv1 and 2.
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
    float4 vertex : SV_POSITION;
    float4 color : COLOR;
    float4 uv : TEXCOORD0;
    #if defined(UNITY_STEREO_INSTANCING_ENABLED)
    UNITY_VERTEX_OUTPUT_STEREO
    #endif
    //RectMask2D Support
    float4 mask : TEXCOORD1;
};

sampler2D _MainTex;
uniform float4 _MainTex_ST;
sampler2D _MaskTex;
uniform float4 _MaskTex_ST;
float _Cutoff;

float _SDFCutoff;
float _Blend;

//RectMask2D Support
float4 _ClipRect;
float _UIMaskSoftnessX;
float _UIMaskSoftnessY;

void convertUVData(inout appdata v)
{
    #if UNITY_VERSION < 202030
    //if unity version is before 2020.3.0, uv data is like this:
    //uv.xy = main texture uv
    //uv2.xy = mask texture uv
    //so... fuse first two channels
    v.uv.zw = v.uv2.xy;
    #endif
}

v2f vert (appdata v)
{
    v2f o;
    #if UNITY_VERSION < 202030
    convertUVData(v);
    #endif
    //single-pass stereo rendering:
    #if defined(UNITY_STEREO_INSTANCING_ENABLED)
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    #endif
    #if UNITY_VERSION < 540
    o.vertex = mul(UNITY_MATRIX_MVP, v.vertex); //UNITY_SHADER_NO_UPGRADE
    #else
    o.vertex = UnityObjectToClipPos(v.vertex);
    #endif
    o.color = v.color;

    #if UNITY_VERSION < 202030
    o.uv.xy = TRANSFORM_TEX(v.uv.xy, _MainTex);
    o.uv.zw = TRANSFORM_TEX(v.uv2.xy, _MaskTex);
    #else
    o.uv.xy = TRANSFORM_TEX(v.uv.xy, _MainTex);
    o.uv.zw = TRANSFORM_TEX(v.uv.zw, _MaskTex);
    #endif
    
    //RectMask2D Support
    #if UNITY_VERSION < 202030
    o.mask = v.vertex;
    #else
    float2 pixelSize = o.vertex.w;
    pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));
    float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
    o.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));
    #endif
    
    #ifdef PIXELSNAP_ON
    o.vertex = UnityPixelSnap(o.vertex);
    #endif
    return o;
}

float4 when_lt(float4 x, float4 y) {
    return max(sign(y - x), 0.0);
}

float4 when_ge(float4 x, float4 y) {
    return 1.0 - when_lt(x, y);
}

//render normal text
float4 frag(v2f i) : SV_Target
{
    float4 text = tex2D(_MainTex, i.uv.xy);
    float4 mask = tex2D(_MaskTex, i.uv.zw);
    float4 col = float4(0.0f,0.0f,0.0f,0.0f);
    #if SDF_MODE
    //anything before this point is already cut by (0,0,0,0)
    //transparency to text
    col.rgb += (mask.rgb * i.color.rgb) * 
                when_ge(text.a, _SDFCutoff) * 
                when_lt(text.a, _SDFCutoff + _Blend);
    col.a += ((text.a - _SDFCutoff + (_Blend/100)) / _Blend * mask.a * i.color.a) * 
                //alpha greater or equal than cutoff
                when_ge(text.a, _SDFCutoff) * 
                //alpha less than blend point
                when_lt(text.a, _SDFCutoff + _Blend);
    //get color from mask & vertex
    col += (mask * i.color) * 
                //greater than blend point
                when_ge(text.a, _SDFCutoff + _Blend);

/*
    older SDF code, with SDF outline support. 
    keeping it around just in case, but it doesn't work too well
    with how STM uses generated SDFs. (no extra space for outlines)
    if(text.a < _SDFCutoff - _SDFOutlineWidthOut - _Blend)
    {
        col.rgb = i.color.rgb;
        col.a = 0; //cut!
    }
    else if(text.a < _SDFCutoff - _SDFOutlineWidthOut + _Blend)
    { //blend between cutoff and edge of letter
        //blend
        if(_SDFOutlineWidthIn > 0 || _SDFOutlineWidthOut > 0) //transparency to outline
        {//get color from outline, or inside depending on if there's an outline or not
            col.rgb = _OutlineColor.rgb;
            col.a = (text.a - _SDFCutoff + _SDFOutlineWidthOut + (_Blend/100)) / _Blend * _OutlineColor.a;
        }
        else //transparency to text
        {
            col.rgb = mask.rgb * i.color.rgb;
            col.a = (text.a - _SDFCutoff + (_Blend/100)) / _Blend * mask.a * i.color.a;
        }
    }
    else if(text.a < _SDFCutoff + _SDFOutlineWidthIn)
    { //inside outline
        col = _OutlineColor; //get color from outline
    }
    else if(text.a < _SDFCutoff + _SDFOutlineWidthIn + _Blend)
    { //blend between inside outline and inside color
        col = lerp(_OutlineColor, mask * i.color, (text.a - _SDFCutoff - _SDFOutlineWidthIn) / _Blend);
    }
    else
    {
        col = mask * i.color; //get color from mask & vertex
    }
*/
    #else
    col.rgb = mask.rgb * i.color.rgb;
    col.a = text.a * mask.a * i.color.a;
    #endif

    //RectMask2D Support
#if UNITY_VERSION < 202030
        //*this* method works... come on
    #if UI_MODE
            //adapted from UnityUI.cginc's UnityGet2DClipping()!
            //In what version of Unity did they stop using this? 2019.2?
            float2 inside = step(_ClipRect.xy, i.mask.xy) * step(i.mask.xy, _ClipRect.zw);
            col.a *= inside.x * inside.y;
    #endif
#else
    #ifdef UNITY_UI_CLIP_RECT
        half2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(i.mask.xy)) * i.mask.zw);
        col.a *= m.x * m.y;
    #endif
#endif

    clip(col.a - _Cutoff);
    return col;
}