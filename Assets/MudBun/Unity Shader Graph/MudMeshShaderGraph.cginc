/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

#ifndef MUDBUN_MESH_SHADER_GRAPH
#define MUDBUN_MESH_SHADER_GRAPH

#ifndef MUDBUN_UNITY_SHADER_GRAPH
#define MUDBUN_UNITY_SHADER_GRAPH
#endif

#include "Assets/MudBun/Shader/Render/ShaderCommon.cginc"
#include "Assets/MudBun/Shader/Render/MeshCommon.cginc"

void MudBunMeshVertex_float
(
  float VertexID, 

  out float3 PositionWs, 
  out float3 PositionLs, 
  out float3 NormalWs, 
  out float3 NormalLs, 
  out float3 TangentWs, 
  out float3 TangentLs, 
  out float4 Color, 
  out float4 EmissionHash, 
  out float Metallic, 
  out float Smoothness, 
  out float4 TextureWeight, 
  out float SdfValue, 
  out float3 Outward2dNormalLs, 
  out float3 Outward2dNormalWs 
)
{
  float4 positionWs;
  float2 metallicSmoothness;

  mudbun_mesh_vert
  (
    uint(VertexID), 
    positionWs, 
    PositionLs, 
    NormalWs, 
    NormalLs, 
    TangentWs, 
    TangentLs, 
    Color, 
    EmissionHash, 
    metallicSmoothness, 
    TextureWeight, 
    SdfValue, 
    Outward2dNormalLs, 
    Outward2dNormalWs
  );

  PositionWs = positionWs.xyz;
  Metallic = metallicSmoothness.x;
  Smoothness = metallicSmoothness.y;
}

#endif
