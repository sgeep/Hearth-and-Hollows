using UnityEngine;

namespace Clavian.STM.Tools
{
	public static class ShaderIds
	{
		public static readonly int MAIN_TEX = Shader.PropertyToID("_MainTex");
		public static readonly int MASK_TEX = Shader.PropertyToID("_MaskTex");
		public static readonly int BASE_MAP = Shader.PropertyToID("_BaseMap");
		public static readonly int Z_TEST_MODE = Shader.PropertyToID("_ZTestMode");
		public static readonly int Z_WRITE_MODE = Shader.PropertyToID("_ZWriteMode");
		public static readonly int FAKE_TEXEL_SIZE = Shader.PropertyToID("_FakeTexelSize");
		public static readonly int CAST_SHADOWS = Shader.PropertyToID("_CastShadows");
		public static readonly int UI_MODE = Shader.PropertyToID("_UIMode");
		public static readonly int STENCIL_COMP = Shader.PropertyToID("_StencilComp");
		public static readonly int STENCIL = Shader.PropertyToID("_Stencil");
		public static readonly int STENCIL_OP = Shader.PropertyToID("_StencilOp");
		public static readonly int STENCIL_WRITE_MASK = Shader.PropertyToID("_StencilWriteMask");
		public static readonly int STENCIL_READ_MASK = Shader.PropertyToID("_StencilReadMask");
		public static readonly int CUTOFF = Shader.PropertyToID("_Cutoff");
		public static readonly int SHADOW_CUTOFF = Shader.PropertyToID("_ShadowCutoff");
		public static readonly int CULL_MODE = Shader.PropertyToID("_CullMode");
		public static readonly int EFFECT = Shader.PropertyToID("_Effect");
		public static readonly int OUTLINE_COLOR = Shader.PropertyToID("_OutlineColor");
		public static readonly int OUTLINE_TEXTURE = Shader.PropertyToID("_OutlineTexture");
		public static readonly int OUTLINE_TEXTURE_SCROLL = Shader.PropertyToID("_OutlineTextureScroll");
		public static readonly int OUTLINE_WIDTH = Shader.PropertyToID("_OutlineWidth");
		public static readonly int OUTLINE_TYPE = Shader.PropertyToID("_OutlineType");
		public static readonly int OUTLINE_SAMPLES = Shader.PropertyToID("_OutlineSamples");
		public static readonly int DROPSHADOW_COLOR = Shader.PropertyToID("_DropshadowColor");
		public static readonly int DROPSHADOW_TEXTURE = Shader.PropertyToID("_DropshadowTexture");
		public static readonly int DROPSHADOW_TEXTURE_SCROLL = Shader.PropertyToID("_DropshadowTextureScroll");
		public static readonly int DROPSHADOW_TYPE = Shader.PropertyToID("_DropshadowType");
		public static readonly int DROPSHADOW_ANGLE = Shader.PropertyToID("_DropshadowAngle");
		public static readonly int DROPSHADOW_ANGLE2 = Shader.PropertyToID("_DropshadowAngle2");
		public static readonly int DROPSHADOW_DISTANCE = Shader.PropertyToID("_DropshadowDistance");
		public static readonly int SDF_MODE = Shader.PropertyToID("_SDFMode");
		public static readonly int BLEND = Shader.PropertyToID("_Blend");
		public static readonly int EFFECT_BLEND = Shader.PropertyToID("_EffectBlend");
		public static readonly int SDF_CUTOFF = Shader.PropertyToID("_SDFCutoff");
		public static readonly int SHADOW_COLOR = Shader.PropertyToID("_ShadowColor");
		public static readonly int SHADOW_DISTANCE = Shader.PropertyToID("_ShadowDistance");
		public static readonly int VECTOR3_DROPSHADOW = Shader.PropertyToID("_Vector3Dropshadow");
		public static readonly int SHADOW_ANGLE3 = Shader.PropertyToID("_ShadowAngle3");
		public static readonly int SHADOW_ANGLE = Shader.PropertyToID("_ShadowAngle");
		public static readonly int SQUARE_OUTLINE = Shader.PropertyToID("_SquareOutline");
		public static readonly int EFFECT_DEPTH = Shader.PropertyToID("_EffectDepth");
		public static readonly int EFFECT_SCALING = Shader.PropertyToID("_EffectScaling");
		public static readonly int EFFECT_IS_URP = Shader.PropertyToID("_IsURP");
	}
}