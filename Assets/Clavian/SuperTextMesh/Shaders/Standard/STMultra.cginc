//Copyright (c) 2016-2026 Kai Clavier [kaiclavier.com] Do Not Distribute
//OMPUCO
#include "UnityCG.cginc"
struct Attributes
{
    //might have to be renamed "positionOS" for URP?
    float4 vertex : POSITION;
    float4 color : COLOR;
    float4 uv : TEXCOORD0;
    float4 uv2 : TEXCOORD1;
    float4 uv3 : TEXCOORD2;//y is character size
    //#if UNITY_VERSION < 202030
    //needs an extra channel to send data
    //...now, it's always there!
    float4 uv4 : TEXCOORD3;
    //#endif
    #if defined(UNITY_INSTANCING_ENABLED)
    UNITY_VERTEX_INPUT_INSTANCE_ID
    #endif
};

struct Varyings
{
    //does this have to be renamed "positionHCS" for URP?
    float4 vertex : SV_POSITION;
    float4 color : COLOR;
    float4 uv : TEXCOORD0;
    float4 dist : TEXCOORD1;
    float2 scale : TEXCOORD2;
    float2 quality : TEXCOORD3;
    #if defined(UNITY_INSTANCING_ENABLED)
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
    #endif
    //RectMask2D Support
    float4 mask : TEXCOORD4;
};

//Custom frag output struct that includes depth
//lets us control the depth/zbuffer output.

//but isn't used anymore, SV_Depth is used!
// struct fragOut
// {
//     float4 col : SV_TARGET;
//     float depth : DEPTH;
// };
//This allows us to make use of depth testing
//to prevent outlines from rendering over other
//character faces that they may occlude

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

    //RectMask2D Support
    float4 _ClipRect;
    float _UIMaskSoftnessX;
    float _UIMaskSoftnessY;

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
//int _ExpansionEffectActive; //only run extra steps if using
//int _UseOutline; //0 = dropshadow mode, 1 = outline mode (changes expansion rules as well)
//int _ExpansionEffect;

//float _Effect; //0 = none, 1 = dropshadow mode, 2 = outline, 3 = both, 4 = outline & thick dropshadow

static const float doublepi = 6.28318530718;

#if INVERT_VERTICES_ORDER
static const float2 quadOffset[4] =
{
    float2(1.0f,-1.0f),
    float2(-1.0f,-1.0f),
    float2(-1.0f,1.0f),
    float2(1.0f,1.0f)
};
#else
static const float2 quadOffset[4] =
{
    float2(-1.0f,1.0f),
    float2(1.0f,1.0f),
    float2(1.0f,-1.0f),
    float2(-1.0f,-1.0f)
};
#endif

inline float4 CopyOfUnityPixelSnap (float4 pos)
{
    float2 hpc = _ScreenParams.xy * 0.5f;
    #if  SHADER_API_PSSL
    // An old sdk used to implement round() as floor(x+0.5) current sdks use the round to even method so we manually use the old method here for compatabilty.
    float2 temp = ((pos.xy / pos.w) * hpc) + float2(0.5,0.5);
    float2 pixelPos = float2(floor(temp.x), floor(temp.y));
    #else
    float2 pixelPos = round ((pos.xy / pos.w) * hpc);
    #endif
    pos.xy = pixelPos / hpc * pos.w;
    return pos;
}


float2 ratio(float2 r)
{
    return float2(1.0,r.y/r.x);
}

float4 when_lt(float4 x, float4 y) {
    return max(sign(y - x), 0.0);
}

float4 when_ge(float4 x, float4 y) {
    return 1.0 - when_lt(x, y);
}

float4 blendOver(float4 a, float4 b)
{
    float newAlpha = lerp(b.w, 1.0, a.w);
    float3 newColor = lerp(b.www * b.xyz, a.xyz, a.w);
    float divideFactor = (newAlpha > 0.001 ? (1.0 / newAlpha) : 1.0);
    return float4(divideFactor * newColor, newAlpha);
}
//this just uses .xyz of a float4 anyway in unity
float4 ObjectToClipPos(float3 inPos)
{
    #if UNITY_VERSION < 540
        return mul(UNITY_MATRIX_MVP, float4(inPos,1.0)); //UNITY_SHADER_NO_UPGRADE
    #else
        //copied from UnityObjectToClipPos
        //return UnityObjectToClipPos(float4(inPos,1.0));
        #if defined(STEREO_CUBEMAP_RENDER_ON)
            float3 posWorld = mul(unity_ObjectToWorld, float4(inPos, 1.0)).xyz;
            float3 offset = ODSOffset(posWorld, unity_HalfStereoSeparation.x);
            return mul(UNITY_MATRIX_VP, float4(posWorld + offset, 1.0));
        #else
        // More efficient than computing M*VP matrix product
            return mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(inPos, 1.0)));
        #endif
    #endif
}
//adapted from here https://realtimevfx.com/t/camera-facing-uvs/384/17
float2 GetScreenUV(float2 clipPos)
{
    float4 SSobjectPosition = ObjectToClipPos(float3(0,0,0));
    float2 screenUV = float2(clipPos.x/_ScreenParams.x,clipPos.y/_ScreenParams.y);
    float screenRatio = _ScreenParams.y/_ScreenParams.x;

    screenUV.y -=0.5;
    screenUV.x -=0.5;

    screenUV.x -= SSobjectPosition.x/(2*SSobjectPosition.w);
    screenUV.y += SSobjectPosition.y/(2*SSobjectPosition.w); //switch sign depending on camera
    screenUV.y *= screenRatio;

    return screenUV;
};


void convertUVData(inout Attributes v)
{
    #if UNITY_VERSION < 202030
    //if unity version is before 2019.2.0, uv data is like this:
    //uv.xy = main texture uv
    //uv2.xy = mask texture uv
    //uv3.xyzw = ultra shader stuff
    //uv4.xyzw = more ultra shader stuff
    //so... fuse first two channels, and push the others back
    //new version is just 3 float4 channels
    v.uv.zw = v.uv2.xy;
    v.uv2.xyzw = v.uv3.xyzw;
    v.uv3.xyzw = v.uv4.xyzw;
    #endif
}

/*float3x3 RotationMatrixFromEuler(float x, float y, float z)
{
    float cx = cos(x);
    float sx = sin(x);
    float cy = cos(y);
    float sy = sin(y);
    float cz = cos(z);
    float sz = sin(z);
    //harder to read but i guess this is just how float 3x3 works anyway
    return float3x3(
        cz * cy, cz * sy * sx - sz * cx, cz * sy * cx + sz * sx,
        sz * cy, sz * sy * sx + cz * cx, sz * sy * cx - cz * sx,
        -sy, cy * sx, cy * cx
    );
}*/


void expandForEffects(inout Attributes v, uint id, out float4 dist, out float2 qScale)
{
	/*
	 *note for future update...
	 *texel size
	 *.x = 1/width
	 *.y = 1/height
	 *.z = width
	 *.y = height
	 */


    float texelFix;
    if(_MainTex_TexelSize.z < _MainTex_TexelSize.w)
    {
        texelFix = _MainTex_TexelSize.z / 8;
    }
    else
    {
        texelFix = _MainTex_TexelSize.z / 64;
    }
    
    float2 shadowOffset;
    if(_DropshadowType == 0)
    {
        shadowOffset = float2(sin((_DropshadowAngle / 360.0)*doublepi),cos((_DropshadowAngle / 360.0)*doublepi)) * _DropshadowDistance * texelFix / 128 ;
    }
    else
    {
        shadowOffset.x = -_DropshadowAngle2.x * texelFix / 128;
        shadowOffset.y = _DropshadowAngle2.y * texelFix / 128;
    }

    float2 os;

    //kai: this seems to fix it...? for dropshadows
    if(_MainTex_TexelSize.w > _MainTex_TexelSize.z) //if the texture is taller than it is wide
    {
        os.x = shadowOffset.x * _MainTex_TexelSize.x * 2;// * _MaskTex_TexelSize.xy;
        os.y = shadowOffset.y * _MainTex_TexelSize.y * 8;
    }
    else
    {
        os = shadowOffset * _MainTex_TexelSize.xy * 16;// * _MainTex_TexelSize.xy * 8;// * _MaskTex_TexelSize.xy;
    }
    
    //float2 adj = quadOffset[(id)%4]; //old method, works on everything but android
    //uint vertexIdx = abs(v.uv3.x) - 1.0f;
    float2 adj = quadOffset[abs(v.uv3.x) - 1.0f]; //using stored ID instead.
    //float vertexIdx = id - (4.0f * floor(id / 4.0f)); //was sent in the fix by Javier, but how exactly does this fix it on android?
    
    float oScale = v.uv3.y; //grab size of text
    bool upsideDown = oScale < 0;
    if(upsideDown)
    {
        oScale = -oScale;
    }
    
    float2 quality = v.uv3.zw; //grab quality of text atlas
    //os *= quality; //adjust vertex offset for quality

    qScale = oScale / quality; //divide size by quality
    float2 scale = 0;

    //WARNING: 8 IS ARBITRARY, NEED TO REPLACE TO ADJUST TO DIFFERENT SIZES

    //float2 ratio = v.uv2.xy;
    //ok so turns out that it's better to not scale via ratio.
    //By uniformly expanding the verts, we get enough extra space
    //for making outlines. Using ratios would cause any tall or
    //short (non-square) letters to have disproportionate outline area

    //v.uv2.xy can be retired now.
    
   
    
    //os /= qScale;
    
    #if _EFFECT_DROPSHADOW
        #if EFFECT_SCALING
        //remultiply by original size
        os *= oScale;
        #endif
    //minimize the number of pixels we raster onto
    //by only scaling in the direction of the shadow
        //dropshadow expansion
        if(sign(os.x) == sign(-adj.x))
        scale.x = abs(os.x)*8;
        if(sign(os.y) == sign(adj.y))
        scale.y = abs(os.y)*8;
		
        //scale.y *= _MainTex_TexelSize.x/_MainTex_TexelSize.y;
        scale *= ratio(_MainTex_TexelSize.xy);
        scale *=8;
    //not sure if it's much faster to run all this
    //or instead just rasterize across more pixels,
    //but these only run once per vert.

    //this only works for simple dropshadow though,
    //so for uniform expansion, we can use this
    #elif _EFFECT_OUTLINE

        //outline expansion

        os = abs(_OutlineWidth) * texelFix / 128  * min(_MainTex_TexelSize.x,_MainTex_TexelSize.y) * 16;
        #if EFFECT_SCALING
        //remultiply by original size
        os *= oScale;
        #endif
        scale = abs(os)*8; //kai: not perfect but changing this from 4 to 16 keeps it ahead of the outline
        //scale.y *= _MainTex_TexelSize.x/_MainTex_TexelSize.y;
        scale *= ratio(_MainTex_TexelSize.xy);
        scale *= 8;
	#elif _EFFECT_BOTH || _EFFECT_BOTHTHICK
        #if EFFECT_SCALING
        //remultiply by original size
        os *= oScale;
        #endif
		if(sign(os.x) == sign(-adj.x))
        scale.x = abs(os.x)*8;
        if(sign(os.y) == sign(adj.y))
        scale.y = abs(os.y)*8;

		os = abs(_OutlineWidth) * texelFix / 128  * min(_MainTex_TexelSize.x,_MainTex_TexelSize.y) * 16;
        #if EFFECT_SCALING
        //remultiply by original size
        os *= oScale;
        #endif
        scale += abs(os)*8;

		scale *= ratio(_MainTex_TexelSize.xy);
        scale *= 8;
    #endif


   

    //scale = abs(shadowOffset.y)*4; //uniform scaling
    //replace shadowOffset with what ever value we want to scale up by.
   

    //scale *= qScale; //adjust arbitrary scaling by size of text quad
    //this allows our quad mods to maintain relative scale with large/small text
    //rotate with transform...
    #if UI_MODE
    float3 scaleVert = float3(adj * scale * v.uv4.xy, 0.0);
    //float3x3 myMatrix = RotationMatrixFromEuler(trunc(v.uv4.w) / 100.0,
    //                                            frac(v.uv4.w) * 10.0,
    //                                            v.uv4.z);
    //rotate by quaternion:
    float4 quat = float4((trunc(v.uv4.z) / 100.0) - 1.0,
                        (frac(v.uv4.z) * 10.0) - 1.0,
                        (trunc(v.uv4.w) / 100.0) - 1.0,
                        (frac(v.uv4.w) * 10.0) - 1.0);
    float3 t = 2.0 * cross(quat.xyz, scaleVert);
    scaleVert = scaleVert + quat.w * t + cross(quat.xyz, t);
    v.vertex.xyz += scaleVert; //expand each quad around it's center
    #else
    v.vertex.xy += adj * scale;
    #endif
    //#endif
    if(v.uv3.x<0)//if rotated,
    {
        adj = adj.yx; //rotate our offsets for UV calculation
        scale = scale.yx;
    }
    adj.x*=-1;
    if(upsideDown)
    {
        adj.y*=-1;
    }
    //flip x (or y) of our offset since it doesn't behave if I don't

    
    //Could probably clean up this multiplication bit but not sure how yet.
    //Should really figure out what's happening here sooner or later...

    v.uv.xy -= v.uv2.zw; //center UVs around (0,0) for scaling
    v.uv.zw -= v.uv2.zw;

    float2 ogUV = v.uv.xy; //store this unscaled centered UV for clipping later


    v.uv.xy -= adj / qScale * scale / 4;
    
    v.uv.zw -= adj / qScale * scale / 4;
    

    dist.xy = v.uv.xy; //send scaled centered UV to frag (not abs yet since we need per-pixel interpolated data to compare)

    //used to be... 1 if rotated, 0 if not
    //dist.zw = abs(ogUV) * sign (.5-v.uv3.x); 
    //now, positive if rotated, negative if not already
    dist.zw = abs(ogUV) * sign (v.uv3.x);
    //send abs of unscaled centered UV to frag (abs since we only need the constant scale on all pixels)
    //also multiplied it by a sign to cheaply transfer over the rotation state
    //We could simplify this step by making a float2 in the V2F struct with
    //the nointerpolation tag (nointerpolation float2 ogUV : TEXCOORD3;) and send the raw data.
    //However, this isn't compatible with all of Unity's shader compilers
    //despite it being supported in most graphics platforms.

    v.uv.xy += v.uv2.zw;//move UV back to letter space
    v.uv.zw += v.uv2.zw;
    if(upsideDown)
    {
        //flip BACK so this info can be used by the dropshadow/outline!
        qScale = -qScale;
    }

}



Varyings vert (Attributes v, uint id : SV_VERTEXID)
{
    #if UNITY_VERSION < 202030
    convertUVData(v);
    #endif
    
    Varyings o;
    
    //single-pass stereo rendering:
    //#if defined(UNITY_STEREO_INSTANCING_ENABLED)
    #if defined(UNITY_INSTANCING_ENABLED)
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    #endif
    
    #if _EFFECT_DROPSHADOW || _EFFECT_OUTLINE || _EFFECT_BOTH || _EFFECT_BOTHTHICK
    expandForEffects(v, id, o.dist, o.scale);
    #else
    //still have to set the full struct
    //to prevent compilation errors.
    o.dist = 0;
    //not sure if this abs is needed
    o.scale = abs(v.uv3.yy)/v.uv3.zw;
    #endif
    
    o.quality = v.uv3.zw;

    
    o.vertex = ObjectToClipPos(v.vertex.xyz);

    o.color = v.color;
 
    o.uv.xy = TRANSFORM_TEX(v.uv.xy, _MainTex);
    o.uv.zw = TRANSFORM_TEX(v.uv.zw, _MaskTex);
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
    o.vertex = CopyOfUnityPixelSnap(o.vertex);
    #endif
    return o;
}
//fragment functions
float offsetClip (float4 dist){
    return step(0,abs(dist.z)-abs(dist.x)) * step(0,abs(dist.w)-abs(dist.y));
}

float4 outlineColor(Varyings i)
{
    // float2 flip = 1.0f;
    // if (_ProjectionParams.x < 0)
    // {
    //     flip.y = -1.0f;
    // }
    //GetScreenUV(i.vertex.xy) * _OutlineTex ... for screen space.
    //using mask for now, will this work right in versions before 2020.3?
    float2 uvc = (i.mask.xy * _OutlineTexture_ST.xy) - _OutlineTexture_ST.zw + (_Time.yy * _OutlineTextureScroll.xy/* * flip.xy*/);
    float4 tex = tex2D(_OutlineTexture, uvc);
    tex.rgb = _OutlineColor.rgb * tex.rgb;
    tex.a = _OutlineColor.a * tex.a;
    return tex;
}


void outlineDraw(inout float4 col, Varyings i, inout float zed)
{
    //only run if text alpha isn't fully opaque
    if(col.a == 1.0)
        return;
    zed -= 1.0-col.a;
    //adjust depth to avoid drawing over other letters


    float4 dropText = 0;
    float4 dropMask = 0;


    //float2 os = float2(sin((_DropshadowAngle/360.0)*doublepi), cos((_DropshadowAngle/360.0)*doublepi)) * _DropshadowDistance * texelFix / 128  * _MainTex_TexelSize.y * 4 * (max(i.scale.x,i.scale.y)/min(i.scale.x,i.scale.y)) * i.quality;
    bool upsideDown = i.scale.y < 0;
    if(upsideDown)
    {
        i.scale.y = -i.scale.y;
        i.scale.x = -i.scale.x;
    }
    float texelFix;
    if(_MainTex_TexelSize.z < _MainTex_TexelSize.w)
    {
        #if EFFECT_SCALING
        texelFix = _MainTex_TexelSize.z / 32;
        #else
        texelFix = _MainTex_TexelSize.z / 8;
        #endif
    }
    else
    {
        texelFix = _MainTex_TexelSize.z / 64;
    }
    
    float4 outline = float4(outlineColor(i).rgb,0);
    //outline samples = taps
    for(int n = 0; n < _OutlineSamples; n++)
    {

        #if EFFECT_SCALING
        float2 circ = float2(sin((float(n)/(_OutlineSamples))*doublepi), cos((float(n)/(_OutlineSamples))*doublepi)) * abs(_OutlineWidth) * texelFix / 128  * _MainTex_TexelSize.y * 256 * i.quality;
        #else
        //float circscale = abs(_OutlineWidth) * texelFix / 128  * _MainTex_TexelSize.y * 256 * (min(i.scale.x,i.scale.y)/max(i.scale.x,i.scale.y)) * ratio(i.quality) /  max(i.scale.x,i.scale.y);
        float2 circ = float2(sin((float(n)/(_OutlineSamples))*doublepi), cos((float(n)/(_OutlineSamples))*doublepi)) * abs(_OutlineWidth) * texelFix / 128  * _MainTex_TexelSize.y * 256 * (min(i.scale.x,i.scale.y)/max(i.scale.x,i.scale.y)) * ratio(i.quality) /  max(i.scale.x,i.scale.y);
        #endif
        if(_OutlineType > 0)
        {
            #if EFFECT_SCALING
            circ = clamp(circ*2, -abs(_OutlineWidth) * texelFix / 128  * _MainTex_TexelSize.y * 256 * i.quality,
                         abs(_OutlineWidth) * texelFix / 128  * _MainTex_TexelSize.y * 256 * i.quality);
            #else
            circ = clamp(circ*2, -abs(_OutlineWidth) * texelFix / 128  * _MainTex_TexelSize.y * 256 * (min(i.scale.x,i.scale.y)/max(i.scale.x,i.scale.y)) * ratio(i.quality) /  max(i.scale.x,i.scale.y),
                         abs(_OutlineWidth) * texelFix / 128  * _MainTex_TexelSize.y * 256 * (min(i.scale.x,i.scale.y)/max(i.scale.x,i.scale.y)) * ratio(i.quality) /  max(i.scale.x,i.scale.y));
            #endif
        }
        circ/=2; //keep it even with outlines (why did this break? find out later)
        // if(i.dist.z<.0)
        // {
        //     if(_MainTex_TexelSize.w > _MainTex_TexelSize.z) //if the texture is taller than it is wide
        //     {
        //         //kai: this seems to correct for over-shaped offset
        //         circ.x /= 2;
        //         circ.y *=2;
        //     }
        //     circ=circ.yx; 
        //     circ.x = -circ.x; //flip sideways uvs
        // }
        if(upsideDown)
        {
            circ.y = -circ.y;
        }
        
        dropText = tex2Dlod(_MainTex, float4(i.uv.xy + circ,  0, 0));
        dropMask = tex2Dlod(_MaskTex, float4(i.uv.zw + circ, 0, 0));
        
        float outlineAlpha = 0;
        #if SDF_MODE
            //col.rgb += (dropMask.rgb * i.color.rgb) * 
            ////        when_ge(dropText.a, _SDFCutoff) * 
            //        when_lt(dropText.a, _SDFCutoff + _Blend);
            outlineAlpha += ((dropText.a - _SDFCutoff + (_EffectBlend/100)) / _EffectBlend * dropMask.a * i.color.a) * 
                        //alpha greater or equal than cutoff
                        when_ge(dropText.a, _SDFCutoff).x * 
                        //alpha less than blend point
                        when_lt(dropText.a, _SDFCutoff + _EffectBlend).x;
            //get color from dropMask & vertex
            outlineAlpha += (dropMask.a * i.color.a) * 
                        //greater than blend point
                        when_ge(dropText.a, _SDFCutoff + _EffectBlend).x;
        #else
        outlineAlpha = dropText.a * dropMask.a * i.color.a;
        #endif
        //float4 shadowCol = float4(dropMask.rgb * i.color.rgb, dropText.a * dropMask.a * i.color.a) * _DropshadowColor;
        //float outlineAlpha = dropText.a * dropMask.a * i.color.a;
        //SHEEPISH COMMENT: Old line multiplies shadow color by base color, making some combinations impossible (IE red & green)
        //and means the shadow can never be brighter than the base. Was that intentional? Commented out for now.
        outlineAlpha *= offsetClip(i.dist + float4(circ, 0,0));

        //outline = lerp(outline, _OutlineColor, outlineAlpha);
        //lerp from alpha/zero to ShadowColor as we sample along the circle & succeed
        //just kidding: new method
        outline.a = max(outline.a, outlineAlpha);
    }
    //max

    //don't use this one, creates a gap in HDRP:
    //col=lerp(saturate(outline), col, (col.a));
    //instead...
    //col=float4(lerp(saturate(outline), col, (col.a)).rgb, max(col.a,outline.a));
    outline.rgb *= lerp(float3(1.0, 1.0, 1.0),col.rgb,saturate(col.a));
    outline.a *= lerp(outlineColor(i).a, outline.a, saturate(col.a));
    // if(col.a < outline.a)
    // {
    //     col=float4(lerp(saturate(outline), col, col.a).rgb, max(col.a,outline.a));
    // }

    col = blendOver(col, outline);
    //col = max(col, outline);

    //since zwrite is on, we want to discard pixels so as to not
    //occlude anything with this quad's presence in depth buffer

    //cutoff excess letters with offset

}


float4 dropshadowColor(Varyings i)
{
    // float2 flip = 1.0f;
    // if (_ProjectionParams.x < 0)
    // {
    //     flip.y = -1.0f;
    // }
    float2 uvc = (i.mask.xy * _DropshadowTexture_ST.xy) - _DropshadowTexture_ST.zw + (_Time.yy * _DropshadowTextureScroll.xy/* * flip.xy*/);
    float4 tex = tex2D(_DropshadowTexture, uvc);
    tex.rgb = _DropshadowColor.rgb * tex.rgb;
    tex.a = _DropshadowColor.a * tex.a;
    return tex;
}


void dropShadow(inout float4 col, Varyings i, inout float zed)
{
    if(col.a == 1.0)
        return;
    zed -= 1.0-col.a;
    //float2 circ = float2(sin(((_DropshadowAngle)/360.0)*doublepi), cos(((_DropshadowAngle)/360.0)*doublepi)) * _MainTex_TexelSize.x * 4 * abs(_OutlineWidth) * texelFix / 128  * i.quality /i.scale.x;
    //float2 shadowOffset = float2(sin((_DropshadowAngle / 360.0)*doublepi),cos((_DropshadowAngle / 360.0)*doublepi)) * _DropshadowDistance * texelFix / 128 ;
    //float2 os = shadowOffset * sign(i.dist.z) * _MainTex_TexelSize.x * 4 *(i.scale.y/i.scale.x) * i.quality / i.scale.x;
    // * _MainTex_TexelSize.x * 4 * _ShadowDistance * i.quality /i.scale.x
    float texelFix;
    bool upsideDown = i.scale.y < 0;
    if(upsideDown)
    {
        i.scale.y = -i.scale.y;
        i.scale.x = -i.scale.x;
    }
    if(_MainTex_TexelSize.z < _MainTex_TexelSize.w)
    {
        #if EFFECT_SCALING
        texelFix = _MainTex_TexelSize.z / 32;
        #else
        texelFix = _MainTex_TexelSize.z / 8;
        #endif
    }
    else
    {
        texelFix = _MainTex_TexelSize.z / 64;
    }
   
    float2 os;
    if(_DropshadowType == 0)
    {
        #if EFFECT_SCALING
        os = float2(sin((_DropshadowAngle/360.0)*doublepi), cos((_DropshadowAngle/360.0)*doublepi)) * _DropshadowDistance * texelFix / 128  * _MainTex_TexelSize.y * 256 * i.quality;  
        #else
        os = float2(sin((_DropshadowAngle/360.0)*doublepi), cos((_DropshadowAngle/360.0)*doublepi)) * _DropshadowDistance * texelFix / 128  * _MainTex_TexelSize.y * 256 * (min(i.scale.x,i.scale.y)/max(i.scale.x,i.scale.y)) * ratio(i.quality) /  max(i.scale.x,i.scale.y);  
        #endif
    }
    else
    {
        float angle = atan2(-_DropshadowAngle2.x, _DropshadowAngle2.y);
        float distance = length(_DropshadowAngle2);
        #if EFFECT_SCALING
        os = float2(sin((angle)), cos((angle))) * distance * texelFix / 128  * _MainTex_TexelSize.y * 256 * i.quality; 
        #else
        os = float2(sin((angle)), cos((angle))) * distance * texelFix / 128  * _MainTex_TexelSize.y * 256 * (min(i.scale.x,i.scale.y)/max(i.scale.x,i.scale.y)) * ratio(i.quality) /  max(i.scale.x,i.scale.y); 
        #endif
    }
    os /= 2; //stay consistent with outline
        
    if(upsideDown) //upside-down?
    {
        os.y = -os.y;
    }

    /*
         *kai: thought this would work but it doesnt
        if(_MainTex_TexelSize.w > _MainTex_TexelSize.z) //if the texture is taller than it is wide
        {
            os=os.yx;
            os.x = -os.x; //flip sideways uvs
            os.y = -os.y;
            //os.y *= 16;
        }
        */
    /*if(i.dist.z<.0) //sideways
    {
        if(_MainTex_TexelSize.w > _MainTex_TexelSize.z) //if the texture is taller than it is wide
        {
            //kai: this seems to correct for over-shaped offset
            os.x /= 2;
            os.y *=2;
        }
        os=os.yx;
        os.x = -os.x; //flip sideways uvs
        os.y = -os.y;
    }*/
    if(i.dist.z<.0)
    {
        if(_MainTex_TexelSize.w > _MainTex_TexelSize.z)
        {
            os.x /= 2;
            os.y *=2;
        }
        os=os.yx;
        os.x = -os.x; //flip sideways uvs
        os.y = -os.y;
    }

    //if we're only doing this once

	
    float4 dropText = tex2D(_MainTex, i.uv.xy + os);
    float4 dropMask = tex2D(_MaskTex, i.uv.zw + os);
    float4 shadow = float4(dropshadowColor(i).rgb,0);
    
    float shadowAlpha = 0;
    #if SDF_MODE
        
        //col.rgb += (dropMask.rgb * i.color.rgb) * 
        ////        when_ge(dropText.a, _SDFCutoff) * 
        //        when_lt(dropText.a, _SDFCutoff + _Blend);
        shadowAlpha += ((dropText.a - _SDFCutoff + (_EffectBlend/100)) / _EffectBlend * dropMask.a * i.color.a) * 
                    //alpha greater or equal than cutoff
                    when_ge(dropText.a, _SDFCutoff) * 
                    //alpha less than blend point
                    when_lt(dropText.a, _SDFCutoff + _EffectBlend);
        //get color from dropMask & vertex
        shadowAlpha += (dropMask.a * i.color.a) * 
                    //greater than blend point
                    when_ge(dropText.a, _SDFCutoff + _EffectBlend);
    #else
    shadowAlpha = dropText.a * dropMask.a * i.color.a;
    #endif
    //float4 shadowCol = float4(dropMask.rgb * i.color.rgb, dropText.a * dropMask.a * i.color.a) * _DropshadowColor;
    //SHEEPISH COMMENT: Old line multiplies shadow color by base color (i.color), making some combinations impossible (IE red & green)
    //and also means the shadow can never be brighter than the base. Was that intentional? Commented out for now, replaced with below.
    //float4 shadowCol = float4(dropMask.rgb, dropText.a * dropMask.a * i.color.a) * _DropshadowColor;

        
    shadowAlpha *= offsetClip(i.dist + float4(os, 0,0));
    //shadowCol.a*=saturate(offsetClip(i.dist + float4(os.xy, 0,0)));
    //shadow = lerp(shadow, _DropshadowColor, shadowAlpha);
    shadow.a = max(shadow.a, shadowAlpha);

    shadow.rgb *= lerp(float3(1.0, 1.0, 1.0),col.rgb,saturate(col.a));

    //shadow.a *= _DropshadowColor.a; //this works but looks jank
    shadow.a *= lerp(dropshadowColor(i).a, shadow.a, col.a);
        
    //if(col.a < shadow.a)
    //{
        //col=float4(lerp(saturate(shadow), col, col.a).rgb, max(col.a, shadow.a));
            
    //}
    
    col = blendOver(col, shadow);

       
    //since zwrite is on, we want to discard pixels so as to not
    //occlude anything with this quad's presence in depth buffer

    //cutoff excess letters with offset
}


void thickDropshadowDraw(inout float4 col, Varyings i, inout float zed)
{
    //only run if text alpha isn't fully opaque
    if(col.a == 1.0)
        return;
    zed -= 1.0-col.a;
    //adjust depth to avoid drawing over other letters


    float4 dropText = 0;
    float4 dropMask = 0;


    //float2 os = float2(sin((_DropshadowAngle/360.0)*doublepi), cos((_DropshadowAngle/360.0)*doublepi)) * _DropshadowDistance * texelFix / 128  * _MainTex_TexelSize.y * 4 * (max(i.scale.x,i.scale.y)/min(i.scale.x,i.scale.y)) * i.quality;
    bool upsideDown = i.scale.y < 0;
    if(upsideDown)
    {
        i.scale.y = -i.scale.y;
        i.scale.x = -i.scale.x;
    }


    float texelFix;
    if(_MainTex_TexelSize.z < _MainTex_TexelSize.w)
    {
        #if EFFECT_SCALING
        texelFix = _MainTex_TexelSize.z / 32;
        #else
        texelFix = _MainTex_TexelSize.z / 8;
        #endif
    }
    else
    {
        texelFix = _MainTex_TexelSize.z / 64;
    }

//from dropshadow:
	float2 os;
    if(_DropshadowType == 0)
    {
        #if EFFECT_SCALING
        os = float2(sin((_DropshadowAngle/360.0)*doublepi), cos((_DropshadowAngle/360.0)*doublepi)) * _DropshadowDistance * texelFix / 128  * _MainTex_TexelSize.y * 256 * i.quality;  
        #else
        os = float2(sin((_DropshadowAngle/360.0)*doublepi), cos((_DropshadowAngle/360.0)*doublepi)) * _DropshadowDistance * texelFix / 128  * _MainTex_TexelSize.y * 256 * (min(i.scale.x,i.scale.y)/max(i.scale.x,i.scale.y)) * ratio(i.quality) /  max(i.scale.x,i.scale.y);  
        #endif
    }
    else
    {
        float angle = atan2(-_DropshadowAngle2.x, _DropshadowAngle2.y);
        float distance = length(_DropshadowAngle2);
        #if EFFECT_SCALING
        os = float2(sin((angle)), cos((angle))) * distance * texelFix / 128  * _MainTex_TexelSize.y * 256 * i.quality; 
        #else
        os = float2(sin((angle)), cos((angle))) * distance * texelFix / 128  * _MainTex_TexelSize.y * 256 * (min(i.scale.x,i.scale.y)/max(i.scale.x,i.scale.y)) * ratio(i.quality) /  max(i.scale.x,i.scale.y); 
        #endif
    }
    os /= 2; //stay consistent with outline
        
    if(upsideDown) //upside-down?
    {
        os.y = -os.y;
    }
    /*if(i.dist.z<.0) //sideways
    {
        if(_MainTex_TexelSize.w > _MainTex_TexelSize.z) //if the texture is taller than it is wide
        {
            //kai: this seems to correct for over-shaped offset
            os.x /= 2;
            os.y *=2;
        }
        os=os.yx;
        os.x = -os.x; //flip sideways uvs
        os.y = -os.y;
    }*/
    if(i.dist.z<.0)
    {
        if(_MainTex_TexelSize.w > _MainTex_TexelSize.z)
        {
            os.x /= 2;
            os.y *=2;
        }
        os=os.yx;
        os.x = -os.x; //flip sideways uvs
        os.y = -os.y;
    }

	//...and back to outline!
    
    float4 outline = float4(dropshadowColor(i).rgb,0);
    //outline samples = taps
    for(int n = 0; n < _OutlineSamples; n++)
    {
        #if EFFECT_SCALING
        float2 circ = float2(sin((float(n)/(_OutlineSamples))*doublepi), cos((float(n)/(_OutlineSamples))*doublepi)) * abs(_OutlineWidth) * texelFix / 128  * _MainTex_TexelSize.y * 256 * i.quality;
        #else
        //float circscale = abs(_OutlineWidth) * texelFix / 128  * _MainTex_TexelSize.y * 256 * (min(i.scale.x,i.scale.y)/max(i.scale.x,i.scale.y)) * ratio(i.quality) /  max(i.scale.x,i.scale.y);
        float2 circ = float2(sin((float(n)/(_OutlineSamples))*doublepi), cos((float(n)/(_OutlineSamples))*doublepi)) * abs(_OutlineWidth) * texelFix / 128  * _MainTex_TexelSize.y * 256 * (min(i.scale.x,i.scale.y)/max(i.scale.x,i.scale.y)) * ratio(i.quality) /  max(i.scale.x,i.scale.y);
        #endif
        if(_OutlineType > 0)
        {
            #if EFFECT_SCALING
            circ = clamp(circ*2, -abs(_OutlineWidth) * texelFix / 128  * _MainTex_TexelSize.y * 256 * i.quality,
                         abs(_OutlineWidth) * texelFix / 128  * _MainTex_TexelSize.y * 256 * i.quality);
            #else
            circ = clamp(circ*2, -abs(_OutlineWidth) * texelFix / 128  * _MainTex_TexelSize.y * 256 * (min(i.scale.x,i.scale.y)/max(i.scale.x,i.scale.y)) * ratio(i.quality) /  max(i.scale.x,i.scale.y),
                         abs(_OutlineWidth) * texelFix / 128  * _MainTex_TexelSize.y * 256 * (min(i.scale.x,i.scale.y)/max(i.scale.x,i.scale.y)) * ratio(i.quality) /  max(i.scale.x,i.scale.y));
            #endif
        }
        circ/=2; //keep it even with outlines (why did this break? find out later)
        // if(i.dist.z<.0)
        // {
        //     if(_MainTex_TexelSize.w > _MainTex_TexelSize.z) //if the texture is taller than it is wide
        //     {
        //         //kai: this seems to correct for over-shaped offset
        //         circ.x /= 2;
        //         circ.y *=2;
        //     }
        //     circ=circ.yx; 
        //     circ.x = -circ.x; //flip sideways uvs
        // }
        
        if(upsideDown)
        {
            circ.y = -circ.y;
        }
        

        circ += os;
        
        dropText = tex2Dlod(_MainTex, float4(i.uv.xy + circ,  0, 0));
        dropMask = tex2Dlod(_MaskTex, float4(i.uv.zw + circ, 0, 0));
        
        float outlineAlpha = 0;
        #if SDF_MODE
            //col.rgb += (dropMask.rgb * i.color.rgb) * 
            ////        when_ge(dropText.a, _SDFCutoff) * 
            //        when_lt(dropText.a, _SDFCutoff + _Blend);
            outlineAlpha += ((dropText.a - _SDFCutoff + (_EffectBlend/100)) / _EffectBlend * dropMask.a * i.color.a) * 
                        //alpha greater or equal than cutoff
                        when_ge(dropText.a, _SDFCutoff) * 
                        //alpha less than blend point
                        when_lt(dropText.a, _SDFCutoff + _EffectBlend);
            //get color from dropMask & vertex
            outlineAlpha += (dropMask.a * i.color.a) * 
                        //greater than blend point
                        when_ge(dropText.a, _SDFCutoff + _EffectBlend);
        #else
        outlineAlpha = dropText.a * dropMask.a * i.color.a;
        #endif
        //float4 shadowCol = float4(dropMask.rgb * i.color.rgb, dropText.a * dropMask.a * i.color.a) * _DropshadowColor;
        //float outlineAlpha = dropText.a * dropMask.a * i.color.a;
        //SHEEPISH COMMENT: Old line multiplies shadow color by base color, making some combinations impossible (IE red & green)
        //and means the shadow can never be brighter than the base. Was that intentional? Commented out for now.
        outlineAlpha *= offsetClip(i.dist + float4(circ, 0,0));

        //outline = lerp(outline, _OutlineColor, outlineAlpha);
        //lerp from alpha/zero to ShadowColor as we sample along the circle & succeed
        //just kidding: new method
        outline.a = max(outline.a, outlineAlpha);
    }
    //max

    //don't use this one, creates a gap in HDRP:
    //col=lerp(saturate(outline), col, (col.a));
    //instead...
    //col=float4(lerp(saturate(outline), col, (col.a)).rgb, max(col.a,outline.a));
    outline.rgb *= lerp(float3(1.0, 1.0, 1.0),col.rgb,saturate(col.a));
    outline.a *= lerp(dropshadowColor(i).a, outline.a, saturate(col.a));
    // if(col.a < outline.a)
    // {
    //     col=float4(lerp(saturate(outline), col, col.a).rgb, max(col.a,outline.a));
    // }

    col = blendOver(col, outline);
    //col = max(col, outline);

    //since zwrite is on, we want to discard pixels so as to not
    //occlude anything with this quad's presence in depth buffer

    //cutoff excess letters with offset

}






inline float UnityLinearEyeDepth( float z )
{
    return 1.0 / (_ZBufferParams.z * z + _ZBufferParams.w);
}

inline float LinearEyeDepthToOutDepth(float z)
{
    return (1 - _ZBufferParams.w * z) / (_ZBufferParams.z * z);
}


half4 frag(Varyings i, out float outDepth : SV_Depth) : SV_Target
{
    half4 o;

    #if defined(UNITY_INSTANCING_ENABLED)
    UNITY_SETUP_INSTANCE_ID(i);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
    #endif
    
    float4 text = tex2D(_MainTex, i.uv.xy);
    float4 mask = tex2D(_MaskTex, i.uv.zw);
    float4 col = float4(0,0,0,0);
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
    #else
    col.rgb = mask.rgb * i.color.rgb;
    col.a = text.a * mask.a * i.color.a;
    #endif

    
    col.a *= offsetClip(i.dist);
    //stop other letters on the atlas from rendering


    float zed = 0;
    //used to offset depth
    //idea here is... start at max possible value for zed to go down by.
    //text will at most float 0.02 units above where it is

    #if _EFFECT_OUTLINE || _EFFECT_DROPSHADOW || _EFFECT_BOTH || _EFFECT_BOTHTHICK
        #if _EFFECT_DROPSHADOW
                zed += 1;
                dropShadow(col, i, zed);
        #elif _EFFECT_OUTLINE
                zed += 1;
                outlineDraw(col, i, zed);
		#elif _EFFECT_BOTH
                zed += 2;
				outlineDraw(col, i, zed);
				dropShadow(col, i, zed);
        #elif _EFFECT_BOTHTHICK
                zed += 2;
                outlineDraw(col, i, zed);
                thickDropshadowDraw(col, i, zed);
        #endif
		if(col.a<=0)
        	discard;
        //zed -= (1.0 - col.a);
    #else
    //zed += 0;
    //this will zfight with things at the same layer, which will be consistent with how stuff is meant to work!
    #endif

    //adding instead of subtracting here would always result in no clipping, but
    //would render over previous characters! trying to avoid that.
    #if UI_MODE
    zed -= 999999;
    #endif
    
    // #if !IS_URP
    outDepth = LinearEyeDepthToOutDepth(UnityLinearEyeDepth(i.vertex.z) - zed * _EffectDepth);
    // #endif
    //in the future, try this... but for now, screen space overlay canvas
    //sets z test mode to always anyway.

    //If rendering on top & using expansion effects (dropshadows/outlines),
    //do not use ZTest always!

    //Instead, set the depth to the front & use ZTest Less.
    //This allows for the same functionality, while preventing
    //the expansion effects from drawing over other text faces.

    //o.depth = 1 + zed * .01;//1.0/(_ZBufferParams.x*(i.vertex.w-zed)+_ZBufferParams.y);
    
    //RectMask2D Support
    #if UNITY_VERSION < 202030
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

    o = col;
    
    clip(col.a - _Cutoff);
    return o;
}