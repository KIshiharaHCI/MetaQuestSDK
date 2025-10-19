/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

using System;
using System.Runtime.InteropServices;

using UnityEngine;

#if MUDBUN_BURST
using Unity.Burst;
#endif

namespace MudBun
{
  [StructLayout(LayoutKind.Sequential, Pack = 0)]
  [Serializable]
#if MUDBUN_BURST
  [BurstCompile]
#endif
  public struct SdfBrushMaterial
  {
    public static readonly int Stride = 4 * sizeof(int) + 16 * sizeof(float);

    public Color Color;
    public Color EmissionHash;
    public Vector4 MetallicSmoothnessSizeTightness;
    public Vector4 TextureWeight;

    public int BrushIndex;
    public int Padding0;
    public int Padding1;
    public int Padding2;

    public static SdfBrushMaterial New =>
      new SdfBrushMaterial()
      {
        Color = Color.white, 
        EmissionHash = Color.black, 
        MetallicSmoothnessSizeTightness = Vector4.zero, 
        TextureWeight = Vector4.zero, 
        BrushIndex = -1, 
        Padding0 = 0, 
        Padding1 = 0, 
        Padding2 = 0, 
      };

#if MUDBUN_BURST
    [BurstCompile]
#endif
    public static void Lerp(in SdfBrushMaterial a, in SdfBrushMaterial b, float t, out SdfBrushMaterial ret)
    {
      ret = 
        new SdfBrushMaterial()
        {
          Color = Color.Lerp(a.Color, b.Color, t), 
          EmissionHash = Color.Lerp(a.EmissionHash, b.EmissionHash, t), 
          MetallicSmoothnessSizeTightness = Vector4.Lerp(a.MetallicSmoothnessSizeTightness, b.MetallicSmoothnessSizeTightness, t), 
          TextureWeight = Vector4.Lerp(a.TextureWeight, b.TextureWeight, t), 
        };
      ret.EmissionHash.a = t < 0.5f ? a.EmissionHash.a : b.EmissionHash.a;
      ret.BrushIndex = t < 0.5f ? a.BrushIndex : b.BrushIndex;
    }
  }

  [StructLayout(LayoutKind.Sequential, Pack = 0)]
  [Serializable]
  public struct SdfBrushMaterialCompressed
  {
    public static readonly int Stride = 4 * sizeof(uint) + 4 * sizeof(float);

    public uint Color;
    public uint EmissionTightness;
    public uint TextureWeight;
    public int BrushIndex;

    public float MetallicSmoothness;
    public float Size;
    public float Hash;
    public float Padding0;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 0)]
  [Serializable]
  public struct SdfBrush
  {
    public static readonly int Stride = 8 * sizeof(int) + 32 * sizeof(float);

    public enum TypeEnum
    {
      Nop = -1, 

      // groups
      GroupStart = -2, 
      GroupEnd = -3, 

      // primitives
      Box = 0, 
      Sphere, 
      Cylinder, 
      Torus, 
      SolidAngle, 

      // effects
      Particle = 100, 
      ParticleSystem, 
      UniformNoise, 
      CurveSimple, 
      CurveFull, 

      // distortion
      FishEye = 200, 
      Pinch, 
      Twist, 
      Quantize, 

      // modifiers
      Onion = 300, 
      NoiseModifier, 
    }

    public enum OperatorEnum
    {
      // OG
      Union       =  0, 
      Subtract    =  1, 
      Intersect   =  2, 
      Dye         =  3, 
      CullInside  =  4, 
      CullOutside =  5, 

      // 1.4.44
      Pipe        =  6, 
      Engrave     =  7, 

      NoOp        = -1, 
    }

    // values are base index for union
    public enum BooleanOperatorTypeEnum
    {
      //Quadratic =  8, 
      Cubic     =  0, 
      //Round     = 11, 
      Chamfer   = 14, 
    }

    // only expose the 4 most common ones to save compile time
    public enum DyeBlendModeEnum
    {
      Overwrite         = OperatorEnum.Dye, 
      //Burn              = 17, 
      //Darken            = 18, 
      //Difference        = 19, 
      //Dodge             = 20, 
      //Divide            = 21, 
      //Exclusion         = 22, 
      //HardLight         = 23, 
      //HardMix           = 24, 
      //Lighten           = 25, 
      //LightBurn         = 26, 
      //LinearDodge       = 27, 
      //LinearLight       = 28, 
      //LinearLightAddSub = 29, 
      Multiply          = 30, 
      [InspectorName("Paint (2x Multiply)")] Paint             = 38, 
      //Negation          = 31, 
      Overlay           = 32, 
      //PinLight          = 33, 
      Screen            = 34, 
      //SoftLight         = 35, 
      //Subtract          = 36, 
      //VividLight        = 37, 
    }

    public static bool IsDyeOperator(OperatorEnum op)
    {
      if (op == OperatorEnum.Dye)
        return true;

      if ((int) op >= 17 && (int)op <= 38)
        return true;

      return false;
    }

    public static int GetShaderOperatorIntValue(OperatorEnum op, BooleanOperatorTypeEnum type, DyeBlendModeEnum dyeBlendMode)
    {
      int res = (int) op;
      switch (op)
      {
        case OperatorEnum.Union:
        case OperatorEnum.Subtract:
        case OperatorEnum.Intersect:
          res += (int) type;
          break;

        case OperatorEnum.Dye:
          res = (int) dyeBlendMode;
          break;
      }
      return res;
    }

    public enum BoundaryShapeEnum
    {
      Box, 
      Sphere, 
      Cylinder, 
      Torus, 
      SolidAngle, 
    }

    public enum NoiseTypeEnum
    {
      Perlin = -1, 
      BakedPerlin, 
      Triangle, 
    }

    public enum FlagBit
    {
      Hidden, 
      FlipX, 
      MirrorX, 
      CountAsBone, 
      CreateMirroredBone, 
      ContributeMaterial, 
      LockNoisePosition, 
      SphericalNoiseCoordinates, 
    }

    public int Type;
    public int Operator;
    public int Proxy;
    public int Index;

    public Vector3 Position;
    public float Blend;

    public Quaternion Rotation;

    public Vector3 Size;
    public float Radius;

    public Vector4 Data0;
    public Vector4 Data1;
    public Vector4 Data2;
    public Vector4 Data3;

    public Bits32 Flags;
    public int MaterialIndex;
    public int BoneIndex;
    public int Padding0;

    public float Hash;
    public float Padding1;
    public float Padding2;
    public float Padding3;

    public static SdfBrush New()
    {
      SdfBrush brush;
      brush.Type = -1;
      brush.Operator = 0;
      brush.Proxy = -1;
      brush.Index = -1;

      brush.Position = Vector3.zero;
      brush.Blend = 0.0f;

      brush.Rotation = Quaternion.identity;

      brush.Size = Vector3.one;
      brush.Radius = 0.0f;

      brush.Data0 = Vector4.zero;
      brush.Data1 = Vector4.zero;
      brush.Data2 = Vector4.zero;
      brush.Data3 = Vector4.zero;

      brush.Flags = new Bits32(0);
      brush.MaterialIndex = -1;
      brush.BoneIndex = -1;
      brush.Padding0 = 0;

      brush.Hash = 0.0f;
      brush.Padding1 = 0.0f;
      brush.Padding2 = 0.0f;
      brush.Padding3 = 0.0f;

      return brush;
    }
  }
}