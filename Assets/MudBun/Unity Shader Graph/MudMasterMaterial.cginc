/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

#ifndef MUDBUN_MESH_MASTER_MATERIAL
#define MUDBUN_MESH_MASTER_MATERIAL

#include "Assets/MudBun/Shader/Render/ShaderCommon.cginc"

void MudMasterMaterial_float
(
  out float4 Color, 
  out float4 Emission, 
  out float Metallic, 
  out float Smoothness
)
{
  Color = _Color;
  Emission = _Emission;
  Metallic = _Metallic;
  Smoothness = _Smoothness;
}

#endif
