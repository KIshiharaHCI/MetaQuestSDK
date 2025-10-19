/******************************************************************************/
/*
  Project   - MudBun
  Publisher - Long Bunny Labs
              http://LongBunnyLabs.com
  Author    - Ming-Lun "Allen" Chou
              http://AllenChou.net

  Based on project "webgl-noise" by Ashima Arts.
  Description : Array and textureless GLSL 2D simplex noise function.
      Author  : Ian McEwan, Ashima Arts.
  Maintainer  : ijm
      Lastmod : 20110822 (ijm)
      License : Copyright (C) 2011 Ashima Arts. All rights reserved.
                Distributed under the MIT License. See LICENSE file.
                https://github.com/ashima/webgl-noise
*/
/******************************************************************************/

#ifndef MUDBUN_RANDOM_NOISE
#define MUDBUN_RANDOM_NOISE

#include "NoiseCommon.cginc"

float mbn_rand(float s)
{
  return frac(sin(mbn_mod(s, 6.2831853)) * 43758.5453123);
}

float mbn_rand(float2 s)
{
  float d = dot(s + 0.1234567, float2(1111112.9819837, 78.237173));
  float m = mbn_mod(d, 6.2831853);
  return frac(sin(m) * 43758.5453123);
}

float mbn_rand(float3 s)
{
  float d = dot(s + 0.1234567, float3(11112.9819837, 378.237173, 3971977.9173179));
  float m = mbn_mod(d, 6.2831853);
  return frac(sin(m) * 43758.5453123);
}

float mbn_rand_range(float s, float a, float b)
{
  return a + (b - a) * mbn_rand(s);
}

float2 mbn_rand_range(float2 s, float2 a, float2 b)
{
  return a + (b - a) * mbn_rand(s);
}

float3 mbn_rand_range(float3 s, float3 a, float3 b)
{
  return a + (b - a) * mbn_rand(s);
}

float2 mbn_rand_uvec(float2 s)
{
  return normalize(float2(mbn_rand(s), mbn_rand(s * 1.23456789)) - 0.5);
}

float3 mbn_rand_uvec(float3 s)
{
  return normalize(float3(mbn_rand(s), mbn_rand(s * 1.23456789), mbn_rand(s * 9876.54321)) - 0.5);
}

float2 mbn_uvec(float2 s)
{
  return mbn_rand_uvec(s) * mbn_rand(s * 9876.54321);
}

float3 mbn_uvec(float3 s)
{
  return mbn_rand_uvec(s) * mbn_rand(s * 1357975.31313);
}

#endif
