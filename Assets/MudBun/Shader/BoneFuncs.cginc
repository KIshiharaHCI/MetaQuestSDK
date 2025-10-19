/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

#ifndef MUDBUN_BONE_FUNCS
#define MUDBUN_BONE_FUNCS

#include "BrushDefs.cginc"
#include "BrushFuncs.cginc"

#define kAutoRiggingNew     (0)
#define kAutoRiggingDefault (1)

int autoRiggingAlgorithm = kAutoRiggingNew;

float4 normalize_bone_weight(float4 boneWeight)
{
  return saturate(boneWeight / comp_sum(boneWeight) - 0.01f);
}

// reference: sdf_brush_apply
float bone_weight_t(SdfBrush brush, float distA, float distB)
{
  float t = 0.0f;

  switch (brush.op)
  {
    case kSdfUnionCubic:
    case kSdfUnionChamfer:
      t = dist_blend_weight(distA, distB, 2.5f);
      break;

    case kSdfSubtractCubic:
    case kSdfSubtractChamfer:
      t = 1.0f - saturate(2.0f * distB / max(kEpsilon, brush.blend));
      break;

    case kSdfIntersectCubic:
    case kSdfIntersectChamfer:
      t = 1.0f - saturate(-2.0f * distB / max(kEpsilon, brush.blend));
      break;

    case kSdfPipe:
      t = saturate(-distB / max(kEpsilon, brush.blend));
      break;

    case kSdfEngrave:
      t = 1.0f - saturate(abs(distB) / max(kEpsilon, brush.blend));
      break;

    /*
    case kSdfDye:
      t = 1.0f - saturate(max(0.0f, distB) / max(kEpsilon, brush.blend));
      break;
    */
    default:
      if (is_sdf_dye(brush.op))
      {
        t = 1.0f - saturate(max(0.0f, distB) / max(kEpsilon, brush.blend));
      }
      break;
  }

  return t;
}

int blend_bone_weights(float brushRes, SdfBrush brush, int brushBoneIndex, inout float4 boneRes, inout int4 boneIndex, inout float4 boneWeight)
{
  int iBone = -1;

  switch (autoRiggingAlgorithm)
  {
    case kAutoRiggingNew:
    {
      boneWeight *= 
        float4
        (
          1.0f - bone_weight_t(brush, boneRes.x, brushRes), 
          1.0f - bone_weight_t(brush, boneRes.y, brushRes), 
          1.0f - bone_weight_t(brush, boneRes.z, brushRes), 
          1.0f - bone_weight_t(brush, boneRes.w, brushRes)
        );

      float minBoneRes = min(boneRes.x, min(boneRes.y, min(boneRes.z, boneRes.w)));
      float brushWeight = bone_weight_t(brush, minBoneRes, brushRes);

      // this could probably be vectorized, but this is for off-line compute jobs so it's not that important
      if (brushWeight > boneWeight.x)
      {
        boneWeight.xyzw = float4(brushWeight, boneWeight.xyz);
        boneRes.xyzw = float4(brushRes, boneRes.xyz);
        boneIndex.xyzw = float4(brushBoneIndex, boneIndex.xyz);
        iBone = 0;
      }
      else if (brushWeight > boneWeight.y)
      {
        boneWeight.yzw = float3(brushWeight, boneWeight.yz);
        boneRes.yzw = float3(brushRes, boneRes.yz);
        boneIndex.yzw = float3(brushBoneIndex, boneIndex.yz);
        iBone = 1;
      }
      else if (brushWeight > boneWeight.z)
      {
        boneWeight.zw = float2(brushWeight, boneWeight.z);
        boneRes.zw = float2(brushRes, boneRes.z);
        boneIndex.zw = float2(brushBoneIndex, boneIndex.z);
        iBone = 2;
      }
      else if (brushWeight > boneWeight.w)
      {
        boneWeight.w = brushWeight;
        boneRes.w = brushRes;
        boneIndex.w = brushBoneIndex;
        iBone = 3;
      }
      else
      {
        return -1;
      }
    }
    break;

    case kAutoRiggingDefault:
    {
      // this could probably be vectorized, but this is for off-line compute jobs so it's not that important
      if (brushRes < boneRes.x)
      {
        boneRes.xyzw = float4(brushRes, boneRes.xyz);
        boneIndex.xyzw = float4(brushBoneIndex, boneIndex.xyz);
        iBone = 0;
      }
      else if (brushRes < boneRes.y)
      {
        boneRes.yzw = float3(brushRes, boneRes.yz);
        boneIndex.yzw = float3(brushBoneIndex, boneIndex.yz);
        iBone = 1;
      }
      else if (brushRes < boneRes.z)
      {
        boneRes.zw = float2(brushRes, boneRes.z);
        boneIndex.zw = float2(brushBoneIndex, boneIndex.z);
        iBone = 2;
      }
      else if (brushRes < boneRes.w)
      {
        boneRes.w = brushRes;
        boneIndex.w = brushBoneIndex;
        iBone = 3;
      }
      else
      {
        return -1;
      }

      boneWeight = 1.0f / max(kEpsilon, boneRes);
    }
    break;
  }

  // TODO: variable tightness?
  //boneWeight = pow(boneWeight, 0.25f);

  boneWeight = normalize_bone_weight(boneWeight);
  boneWeight = step(0.02f, boneWeight) * boneWeight; // trim tiny weights
  boneWeight = normalize_bone_weight(boneWeight);

  return iBone;
}

#include "../Customization/CustomBone.cginc"

void sdf_apply_brush_bone_weights(float3 p, SdfBrush b, float brushRes, inout float4 boneRes, inout int4 boneIndex, inout float4 boneWeight)
{
  float3 pRel = quat_rot(quat_inv(b.rotation), p - b.position);

  switch (b.type)
  {
    case kSdfCurveSimple:
    {
      float2 curveRes = sdf_bezier(p, b.data0.xyz, b.data2.xyz, b.data1.xyz);
      float brushRes = curveRes.x;
      float resA = brushRes;
      float resB = brushRes;
      float resC = brushRes;
      int brushBoneIndexA = b.boneIndex;
      int brushBoneIndexB = b.boneIndex + 1;
      int brushBoneIndexC = b.boneIndex + 2;
      int iBoneA = blend_bone_weights(resA, b, brushBoneIndexA, boneRes, boneIndex, boneWeight);
      int iBoneB = blend_bone_weights(resB, b, brushBoneIndexB, boneRes, boneIndex, boneWeight);
      int iBoneC = blend_bone_weights(resC, b, brushBoneIndexC, boneRes, boneIndex, boneWeight);
      float aBoneWeight[4] = { boneWeight.x, boneWeight.y, boneWeight.z, boneWeight.w };
      if (iBoneA >= 0 && iBoneB >= 0)
      {
        float t = bone_weight_t(b, resA, resB);
        float boneWeightAB = aBoneWeight[iBoneA] + aBoneWeight[iBoneB];
        aBoneWeight[iBoneA] += (1.0f - t) * boneWeightAB;
        aBoneWeight[iBoneB] += t * boneWeightAB;
      }
      if (iBoneB >= 0 && iBoneC >= 0)
      {
        float t = bone_weight_t(b, resB, resC);
        float boneWeightBC = aBoneWeight[iBoneB] + aBoneWeight[iBoneC];
        aBoneWeight[iBoneB] += (1.0f - t) * boneWeightBC;
        aBoneWeight[iBoneC] += t * boneWeightBC;
      }
      boneWeight = float4(aBoneWeight[0], aBoneWeight[1], aBoneWeight[2], aBoneWeight[3]);

      break;
    }

    case kSdfCurveFull:
    {
      int numPoints = int(b.data0.x) - 2;
      bool useNoise = false;//(b.data0.z > 0.0f);
      for (int i = 0; i < numPoints; ++i)
      {
        int iBrush = b.index + (useNoise ? 3 : 2) + i;
        float3 pointPos = aBrush[iBrush].data0.xyz;

        /*
        float maxSegDist = 0.0f;
        if (i > 0)
        {
          float3 prevPointPos = aBrush[iBrush - 1].data0.xyz;
          maxSegDist = max(maxSegDist, length(prevPointPos - pointPos));
        }
        if (i < numPoints - 1)
        {
          float3 nextPointPos = aBrush[iBrush - 1].data0.xyz;
          maxSegDist = max(maxSegDist, length(nextPointPos - pointPos));
        }

        float pDist = length(pRel - pointPos);
        if (maxSegDist > 0.0f && pDist > maxSegDist)
          continue;
        */

        float pointRes = sdf_sphere(p - aBrush[iBrush].data0.xyz, aBrush[iBrush].data0.w);
        int pointBoneIndex = b.boneIndex + i;
        blend_bone_weights(pointRes, b, pointBoneIndex, boneRes, boneIndex, boneWeight);
      }
      break;
    }

    case kSdfBox:
    case kSdfSphere:
    case kSdfCylinder:
    case kSdfTorus:
    case kSdfSolidAngle:
    case kSdfParticle:
    case kSdfParticleSystem:
    case kSdfNoiseVolume:
    {
      blend_bone_weights(brushRes, b, b.boneIndex, boneRes, boneIndex, boneWeight);
      break;
    }

    case kSdfNoOp:
      break;

    default:
    {
      apply_custom_brush_bone_weights(p, pRel, b, brushRes, boneRes, boneIndex, boneWeight);
      break;
    }
  }
}

void compute_brush_bone_weights(float3 p, out int4 boneIndex, out float4 boneWeight)
{
  boneIndex = -1;
  boneWeight = 1e-6f;

  float res = kInfinity;
  float4 boneRes = kInfinity;
  for (int iBrush = 0; iBrush < numBrushes; ++iBrush)
  {
    res = sdf_brush(res, p, aBrush[iBrush]);

    // not sure why Metal doesn't like this check...
    #if !defined(SHADER_API_METAL)
      if ((aBrush[iBrush].flags & kSdfBrushFlagsCountAsBone) == 0)
        continue;
    #endif

    sdf_apply_brush_bone_weights(p, aBrush[iBrush], res, boneRes, boneIndex, boneWeight);
  }
}

#endif

