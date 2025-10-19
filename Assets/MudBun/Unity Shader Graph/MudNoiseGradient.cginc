/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

#ifndef MUDBUN_NOISE_GRADIENT
#define MUDBUN_NOISE_GRADIENT

#ifndef MUDBUN_UNITY_SHADER_GRAPH
#define MUDBUN_UNITY_SHADER_GRAPH
#endif

#include "Assets/MudBun/Shader/Noise/SimplexNoise3D.cginc"

void MudNoiseGradient_float
(
  float3 Position, 
  float NoiseSize, 
  float OffsetAmount, 

  out float3 Offset
)
{
  Offset = snoise_grad(Position / max(1e-6, NoiseSize)).xyz * OffsetAmount;
}

#endif
