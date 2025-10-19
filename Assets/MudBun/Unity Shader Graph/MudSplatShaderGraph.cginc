/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

#ifndef MUDBUN_SPLAT_SHADER_GRAPH
#define MUDBUN_SPLAT_SHADER_GRAPH

#ifndef MUDBUN_UNITY_SHADER_GRAPH
#define MUDBUN_UNITY_SHADER_GRAPH
#endif

#if defined(SHADERPASS)
  #if (!defined(MUDBUN_HDRP) && (!definedSHADERPASS == SHADERPASS_SHADOWCASTER)) || (defined(MUDBUN_HDRP) && (SHADERPASS == SHADERPASS_SHADOWS))
    #define MUDBUN_SHADOW_PASS
  #endif
#endif

#include "Assets/MudBun/Shader/Render/ShaderCommon.cginc"
#include "Assets/MudBun/Shader/Render/SplatCommon.cginc"


void MudBunSplatVertex_float
(
  float VertexID, 

  out float3 PositionWs, 
  out float3 PositionLs, 
  out float3 NormalWs, 
  out float3 NormalLs, 
  out float3 TangentWs, 
  out float3 TangentLs, 
  out float3 CenterWs, 
  out float3 CenterLs,
  out float4 Color, 
  out float4 EmissionHash, 
  out float Metallic, 
  out float Smoothness, 
  out float2 TextureUv, 
  out float4 TextureWeight, 
  out float SdfValue, 
  out float3 Outward2dNormalLs, 
  out float3 Outward2dNormalWs 
)
{
  float4 positionWs;
  float2 metallicSmoothness;

  mudbun_splat_vert
  (
    uint(VertexID), 
    positionWs, 
    PositionLs, 
    NormalWs, 
    NormalLs, 
    TangentWs, 
    TangentLs, 
    CenterWs, 
    CenterLs, 
    Color, 
    EmissionHash, 
    metallicSmoothness, 
    TextureUv, 
    TextureWeight, 
    SdfValue, 
    Outward2dNormalLs, 
    Outward2dNormalWs
  );

  PositionWs = positionWs.xyz;
  Metallic = metallicSmoothness.x;
  Smoothness = metallicSmoothness.y;
}

void MudBunQuadSplats_float
(
  out bool QuadSplats
)
{
#ifdef MUDBUN_QUAD_SPLATS
  QuadSplats = true;
#else
  QuadSplats = false;
#endif
}


#endif
