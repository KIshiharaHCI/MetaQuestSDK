using System;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using MudBun;

// 继承 Distortion：对空间点做位移场（p -> p + disp）
public class PinchPullDistortion : CustomDistortion
{
    [SerializeField, Min(0f)] private float radius = 0.04f;
    [SerializeField] private float strength = 0.015f;   // 正=拉，负=捏
    [SerializeField, Range(0f, 1f)] private float hardness = 0.6f;
    [SerializeField, Range(0f, 1f)] private float selfBlend = 0.0f;

    public float Radius { get => radius; set => radius = Mathf.Max(0f, value); }
    public float Strength { get => strength; set => strength = value; }
    public float Hardness { get => hardness; set => hardness = Mathf.Clamp01(value); }
    public float SelfBlend { get => selfBlend; set => selfBlend = Mathf.Clamp01(value); }

    // AABB：以半径和强度为界，给足空间，避免裁剪变形
    public Aabb Bounds
    {
        get
        {
            float r = radius + Mathf.Abs(strength) * 1.5f;
            return new Aabb(-Vector3.one * r, Vector3.one * r);
        }
    }

    // 把自定义参数打进 SdfBrush（或 MudBun 提供的自定义数据槽）
    public int FillComputeData(NativeArray<SdfBrush> a, int start)
    {
        var b = SdfBrush.New();
        b.Type = 999;            // your custom type id; must not conflict with built-ins
        
        void TryPackIntoFloat4(string name, float x, float y, float z, float w)
        {
            object boxed = b;
            var fi = typeof(SdfBrush).GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (fi != null)
            {
                var t = fi.FieldType;
                if (t.FullName == "Unity.Mathematics.float4" || t.Name == "float4")
                {
                    var ctor = t.GetConstructor(new Type[] { typeof(float), typeof(float), typeof(float), typeof(float) });
                    if (ctor != null)
                    {
                        var vec = ctor.Invoke(new object[] { x, y, z, w });
                        fi.SetValue(boxed, vec);
                        b = (SdfBrush)boxed;
                    }
                }
            }
        }

        // pack parameters to match HLSL: data0=(strength, radius, hardness, selfBlend)
        TryPackIntoFloat4("Data0",  strength, radius, hardness, selfBlend);
        TryPackIntoFloat4("Data",   strength, radius, hardness, selfBlend);
        TryPackIntoFloat4("Params", strength, radius, hardness, selfBlend);
        
        // Assign back to array
        a[start] = b;
        return 1;
    }
}