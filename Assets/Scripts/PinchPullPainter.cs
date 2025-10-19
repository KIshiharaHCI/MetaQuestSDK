using UnityEngine;
using System.Collections.Generic;
using MudBun;


public class PinchPullPainter : MonoBehaviour
{
    [Header("MudBun")]
    public MudRenderer renderer;           // 拖拽场景中的 MudRenderer
    public PinchPullDistortion prefab;     // 预制的“捏/拉”Distortion 笔刷（见下文）

    [Header("Brush Params")]
    public float radius = 0.04f;           // 影响半径（米）
    public float strength = 0.015f;        // 位移强度（米，正=拉、负=捏）
    public float hardness = 0.6f;          // 0~1，越大边缘越硬
    public float selfBlend = 0.0f;         // 如需要与其他体平滑融合，可暴露

    private readonly List<PinchPullDistortion> pool = new();
    private PinchPullDistortion active;     // 正在“按住不放”时复用同一笔刷

    void Start()
    {
        // Keep a brush alive as soon as the component starts.
        active = GetBrush();
    }

    void Update()
    {
        if (active == null) active = GetBrush();

        var position = transform.position;
        var normal = transform.forward;
        if (normal == Vector3.zero) normal = Vector3.up;

        Stamp(active, position, normal);
    }

    private void Stamp(PinchPullDistortion brush, Vector3 pos, Vector3 normal)
    {
        // 朝向：让局部 +Z 对齐命中法线（HLSL 里将按 +Z 为“推/拉”方向）
        brush.transform.position = pos + normal * 0.0005f;
        brush.transform.rotation = Quaternion.LookRotation(normal, Vector3.up);

        // 参数更新（半径/强度/硬度/自混合等）
        brush.Radius = radius;
        brush.Strength = strength;
        brush.Hardness = hardness;
        brush.SelfBlend = selfBlend;

        // 若是“新增/删除”笔刷需要 Rescan；仅改参数/改 Transform 一般自动生效
        // renderer.RescanBrushes(); // 只有在你实例化/销毁时才需要
    }

    private PinchPullDistortion GetBrush()
    {
        foreach (var b in pool) if (!b.gameObject.activeSelf) { b.gameObject.SetActive(true); return b; }
        var inst = Instantiate(prefab, renderer.transform);
        pool.Add(inst);
        return inst;
    }
}
