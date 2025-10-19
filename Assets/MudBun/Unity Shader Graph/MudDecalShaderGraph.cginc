/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

#ifndef MUDBUN_DECAL_SHADER_GRAPH
#define MUDBUN_DECAL_SHADER_GRAPH

#ifndef MUDBUN_UNITY_SHADER_GRAPH
#define MUDBUN_UNITY_SHADER_GRAPH
#endif

#include "Assets/MudBun/Shader/Render/ShaderCommon.cginc"
#include "Assets/MudBun/Shader/Decal.cginc"

void MudBunDecal_float
(
  float3 PositionWs, 

  out float4 Color
)
{
  DecalResults res = sdf_decal(PositionWs);
  Color = res.mat.color * _Color;
}

#endif
