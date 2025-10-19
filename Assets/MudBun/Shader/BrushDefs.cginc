/*****************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

#ifndef MUDBUN_BRUSH_DEFS
#define MUDBUN_BRUSH_DEFS

#include "Math/Codec.cginc"

#define kSdfNoOp                (-1)

#define kSdfBeginGroup          (-2)
#define kSdfEndGroup            (-3)

// primitives
#define kSdfBox                  (0)
#define kSdfSphere               (1)
#define kSdfCylinder             (2)
#define kSdfTorus                (3)
#define kSdfSolidAngle           (4)

// effects
#define kSdfParticle           (100)
#define kSdfParticleSystem     (101)
#define kSdfNoiseVolume        (102)
#define kSdfCurveSimple        (103)
#define kSdfCurveFull          (104)

// distortion
#define kSdfFishEye            (200)
#define kSdfPinch              (201)
#define kSdfTwist              (202)
#define kSdfQuantize           (203)

// modifiers
#define kSdfOnion              (300)
#define kSdfNoiseModifier      (301)

// operators
// OG
#define kSdfUnionCubic           (0)
#define kSdfSubtractCubic        (1)
#define kSdfIntersectCubic       (2)
#define kSdfDye                  (3)
#define kSdfCullInside           (4)
#define kSdfCullOutside          (5)
// 1.4.44
#define kSdfPipe                 (6)
#define kSdfEngrave              (7)
#define kSdfUnionQuad            (8)
#define kSdfSubtractQuad         (9)
#define kSdfIntersectQuad       (10)
#define kSdfUnionRound          (11)
#define kSdfSubtractRound       (12)
#define kSdfIntersectRound      (13)
#define kSdfUnionChamfer        (14)
#define kSdfSubtractChamfer     (15)
#define kSdfIntersectChamfer    (16)
// 1.6.52 dye blends
#define kSdfDyeBlendModeBegin   (17)
#define kSdfDyeBlendBurn        kSdfDyeBlendModeBegin
#define kSdfDyeBlendDarken      (18)
#define kSdfDyeBlendDifference  (19)
#define kSdfDyeBlendDodge       (20)
#define kSdfDyeBlendDivide      (21)
#define kSdfDyeBlendExclusion   (22)
#define kSdfDyeBlendHardLight   (23)
#define kSdfDyeBlendHardMix     (24)
#define kSdfDyeBlendLighten     (25)
#define kSdfDyeBlendLightBurn   (26)
#define kSdfDyeBlendLinearDodge (27)
#define kSdfDyeBlendLinearLight (28)
#define kSdfDyeBlendLinearLightAddSub (29)
#define kSdfDyeBlendMultiply    (30)
#define kSdfDyeBlendNegation    (31)
#define kSdfDyeBlendOverlay     (32)
#define kSdfDyeBlendPinLight    (33)
#define kSdfDyeBlendScreen      (34)
#define kSdfDyeBlendSoftLight   (35)
#define kSdfDyeBlendSubtract    (36)
#define kSdfDyeBlendVividLight  (37)
#define kSdfDyeBlendModePaint   (38)
#define kSdfDyeBlendmodeEnd     (kSdfDyeBlendModePaint + 1)

// OG
#define kSdfDistort           (-100)
#define kSdfModify            ( 100)

// flags
#define kSdfBrushFlagsHidden                    (1 << 0)
#define kSdfBrushFlagsFlipX                     (1 << 1)
#define kSdfBrushFlagsMirrorX                   (1 << 2)
#define kSdfBrushFlagsCountAsBone               (1 << 3)
#define kSdfBrushFlagsCreateMirroredBone        (1 << 4)
#define kSdfBrushFlagsContributeMaterial        (1 << 5)
#define kSdfBrushFlagsLockNoisePosition         (1 << 6)
#define kSdfBrushFlagsSphericalNoiseCoordinates (1 << 7)

// boundaries
#define kSdfNoiseBoundaryBox        (0)
#define kSdfNoiseBoundarySphere     (1)
#define kSdfNoiseBoundaryCylinder   (2)
#define kSdfNoiseBoundaryTorus      (3)
#define kSdfNoiseBoundarySolidAngle (4)

// noise types
#define kSdfNoiseTypePerlin         (-1)
#define kSdfNoiseTypeCachedPerlin   (0)
#define kSdfNoiseTypeTriangle       (1)

#define kMaxBrushGroupDepth (6)

int is_sdf_dye(int op)
{
  return op == kSdfDye || (op >= kSdfDyeBlendModeBegin && op <= kSdfDyeBlendmodeEnd);
}

struct SdfBrushMaterial
{
  float4 color;
  float4 emissionHash;
  float4 metallicSmoothnessSizeTightness;
  float4 textureWeight;

  int iBrush;
  int padding0;
  int padding1;
  int padding2;
};

struct SdfBrushMaterialCompressed
{
  uint color;
  uint emissionTightness;
  uint textureWeight;
  int iBrush;

  float metallicSmoothness;
  float size;
  float hash;
  float padding0;
};

SdfBrushMaterialCompressed pack_material(SdfBrushMaterial mat)
{
  SdfBrushMaterialCompressed m;

  m.color = pack_rgba(mat.color);
  m.emissionTightness = pack_rgba(float4(mat.emissionHash.rgb, mat.metallicSmoothnessSizeTightness.w));
  m.textureWeight = pack_rgba(mat.textureWeight);
  m.iBrush = mat.iBrush;

  m.metallicSmoothness = pack_saturated(mat.metallicSmoothnessSizeTightness.xy);
  m.size = mat.metallicSmoothnessSizeTightness.z;
  m.hash = mat.emissionHash.a;
  m.padding0 = 0.0f;

  return m;
}

SdfBrushMaterial unpack_material(SdfBrushMaterialCompressed mat)
{
  float4 emissionTightness = unpack_rgba(mat.emissionTightness);

  SdfBrushMaterial m;
  m.color = unpack_rgba(mat.color);
  m.emissionHash.rgb = emissionTightness.rgb;
  m.emissionHash.a = mat.hash;
  m.metallicSmoothnessSizeTightness = float4(unpack_saturated(mat.metallicSmoothness), mat.size, emissionTightness.w);
  m.textureWeight = unpack_rgba(mat.textureWeight);
  m.iBrush = mat.iBrush;

  return m;
}

struct SdfBrush
{
  int type;
  int op;
  int iProxy;
  int index;

  float3 position;
  float blend;

  float4 rotation;

  float3 size;
  float radius;

  float4 data0;
  float4 data1;
  float4 data2;
  float4 data3;

  uint flags;
  int materialIndex;
  int boneIndex;
  int padding0;

  float hash;
  float padding1;
  float padding2;
  float padding3;
};

StructuredBuffer<SdfBrush> aBrush;
StructuredBuffer<SdfBrushMaterial> aBrushMaterial;
int numBrushes;

float surfaceShift;

#endif

