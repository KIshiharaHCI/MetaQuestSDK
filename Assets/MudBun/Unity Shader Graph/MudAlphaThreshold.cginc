/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net
*/
/******************************************************************************/

#ifndef MUDBUN_ALPHA_THRESHOLD
#define MUDBUN_ALPHA_THRESHOLD

#include "Assets/MudBun/Shader/Render/ShaderCommon.cginc"

void MudAlphaThreshold_float
(
  float2 ScreenPos, 
  UnityTexture2D DitherNoiseTexture, 
  float DitherNoiseTextureSize, 
  float AlphaIn, 
  float AlphaCutoutThreshold, 
  float Dithering, 
  out float AlphaOut, 
  out float AlphaThreshold
)
{
  float ditherThreshold = 0.0f;
  ditherThreshold = SAMPLE_TEXTURE2D(DitherNoiseTexture.tex, DitherNoiseTexture.samplerstate, ScreenPos / DitherNoiseTextureSize).r;
  ditherThreshold = 0.98f * (ditherThreshold - 0.5f) + 0.5f;

  AlphaOut = saturate(1.02f * (AlphaIn - 0.5f) + 0.5f);
  AlphaThreshold = lerp(AlphaCutoutThreshold, max(AlphaCutoutThreshold, ditherThreshold), Dithering);
}

#endif
