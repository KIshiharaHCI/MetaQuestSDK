using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class EditableMesh : MonoBehaviour
{
    [Header("Deform Settings")]
    [Tooltip("将同一空间位置的重复顶点成组移动，避免UV/硬边拆点产生裂缝")] 
    public bool weldDuplicateVertices = true;

    [Tooltip("判断重复顶点的容差（本地坐标系）")] 
    public float weldEpsilon = 1e-4f;

    [Tooltip("变形后是否重算切线（如使用法线贴图材质建议开启）")] 
    public bool recalcTangents = false;

    public AnimationCurve falloff = AnimationCurve.EaseInOut(0, 1, 1, 0); // r=0 强，r=1 弱
    public bool recalcNormals = true;
    public bool recalcBounds = true;

    [Tooltip("每多少次变形更新一次MeshCollider（昂贵操作）。0=从不自动更新。")]
    public int colliderUpdateInterval = 5;

    MeshFilter _mf;
    MeshCollider _mc;
    Mesh _runtimeMesh;              // 实例副本
    Vector3[] _baseVertices;        // 初始顶点（局部）
    Vector3[] _workVertices;        // 当前顶点（局部）
    Vector3[] _normals;             // 当前法线（局部）
    int _deformCountSinceLastColliderUpdate = 0;

    // 将同一空间位置（在导入时被UV/法线拆分而复制的顶点）分成焊接组
    System.Collections.Generic.List<int>[] _weldGroups;

    void Awake()
    {
        _mf = GetComponent<MeshFilter>();
        _mc = GetComponent<MeshCollider>();

        // 复制成可写的运行时 Mesh
        var src = _mf.sharedMesh;
        _runtimeMesh = Instantiate(src);
        _runtimeMesh.name = src.name + " (Editable Runtime)";
        _mf.sharedMesh = _runtimeMesh; // 用 sharedMesh 替换，保证渲染和碰撞一致来源

        // 顶点与法线缓存
        _baseVertices  = _runtimeMesh.vertices;
        _workVertices  = _runtimeMesh.vertices;
        _normals       = _runtimeMesh.normals;
        if (_normals == null || _normals.Length != _workVertices.Length)
        {
            _runtimeMesh.RecalculateNormals();
            _normals = _runtimeMesh.normals;
        }

        // 初始给 MeshCollider（如果有）
        if (_mc) _mc.sharedMesh = _runtimeMesh;

        // 预计算焊接组，避免沿硬边/UV缝的裂开
        if (weldDuplicateVertices)
            BuildWeldGroups();
    }

    // 基于初始顶点位置将重复顶点（UV缝/硬边导致的拆点）分组
    void BuildWeldGroups()
    {
        var verts = _runtimeMesh.vertices; // 使用当前（和_baseVertices等价）
        var map = new System.Collections.Generic.Dictionary<UnityEngine.Vector3Int, System.Collections.Generic.List<int>>();
        float inv = 1f / Mathf.Max(1e-12f, weldEpsilon);
        for (int i = 0; i < verts.Length; i++)
        {
            Vector3 v = verts[i];
            var key = new Vector3Int(
                Mathf.RoundToInt(v.x * inv),
                Mathf.RoundToInt(v.y * inv),
                Mathf.RoundToInt(v.z * inv)
            );
            if (!map.TryGetValue(key, out var list))
            {
                list = new System.Collections.Generic.List<int>(4);
                map.Add(key, list);
            }
            list.Add(i);
        }
        _weldGroups = new System.Collections.Generic.List<int>[map.Count];
        int k = 0;
        foreach (var kv in map)
            _weldGroups[k++] = kv.Value;
    }

    /// <summary>
    /// 在世界坐标下应用一次“画笔”。
    /// centerW: 画笔中心（world）
    /// radius: 画笔半径（world）
    /// strength: 形变强度（正=沿法线外推，负=内凹）
    /// mode: Push/Pull/Smooth（此MVP只做 Push/Pull；Smooth 给出占位）
    /// </summary>
    public void ApplyBrushWorld(Vector3 centerW, float radius, float strength, BrushMode mode = BrushMode.Push)
    {
        if (_workVertices == null || _workVertices.Length == 0) return;

        var t = transform;
        float r2 = radius * radius;
        bool anyChanged = false;

        // 如果开启焊接，则以“顶点组”为单位一起移动，避免缝隙
        if (weldDuplicateVertices && _weldGroups != null && _weldGroups.Length > 0)
        {
            for (int g = 0; g < _weldGroups.Length; g++)
            {
                var group = _weldGroups[g];
                int i0 = group[0];
                Vector3 vL0 = _workVertices[i0];
                Vector3 vW0 = t.TransformPoint(vL0);
                Vector3 toCenter = vW0 - centerW;
                float d2 = toCenter.sqrMagnitude;
                if (d2 > r2) continue;

                float d = Mathf.Sqrt(d2);
                float u = Mathf.Clamp01(d / radius);
                float w = falloff.Evaluate(u);

                // 组平均法线（在世界坐标）
                Vector3 nW = Vector3.zero;
                for (int k = 0; k < group.Count; k++)
                {
                    int idx = group[k];
                    Vector3 nL = _normals[idx];
                    nW += t.TransformDirection(nL);
                }
                nW = nW.normalized;

                Vector3 deltaW;
                switch (mode)
                {
                    case BrushMode.Pull:
                        deltaW = -nW * (strength * w);
                        break;
                    case BrushMode.Smooth:
                        // TODO: 邻接/拉普拉斯平滑；MVP 先不处理
                        deltaW = Vector3.zero;
                        break;
                    case BrushMode.Push:
                    default:
                        deltaW =  nW * (strength * w);
                        break;
                }
                if (deltaW.sqrMagnitude <= 0f) continue;

                // 用向量变换转换到局部（避免平移影响），并对整组加同一位移
                Vector3 deltaL = t.InverseTransformVector(deltaW);
                for (int k = 0; k < group.Count; k++)
                {
                    int idx = group[k];
                    _workVertices[idx] += deltaL;
                }
                anyChanged = true;
            }
        }
        else
        {
            // 原始逐顶点版本（保留备用）
            for (int i = 0; i < _workVertices.Length; i++)
            {
                Vector3 vL = _workVertices[i];
                Vector3 vW = t.TransformPoint(vL);
                Vector3 toCenter = vW - centerW;
                float d2v = toCenter.sqrMagnitude;
                if (d2v > r2) continue;

                float d = Mathf.Sqrt(d2v);
                float u = Mathf.Clamp01(d / radius);
                float w = falloff.Evaluate(u);

                Vector3 nL = _normals[i];
                Vector3 nW = t.TransformDirection(nL).normalized;

                Vector3 deltaW;
                switch (mode)
                {
                    case BrushMode.Pull:  deltaW = -nW * (strength * w); break;
                    case BrushMode.Smooth: deltaW = Vector3.zero; break;
                    default:              deltaW =  nW * (strength * w); break;
                }
                if (deltaW.sqrMagnitude <= 0f) continue;

                Vector3 deltaL = t.InverseTransformVector(deltaW);
                _workVertices[i] += deltaL;
                anyChanged = true;
            }
        }

        if (!anyChanged) return;

        // 回写
        _runtimeMesh.vertices = _workVertices;

        if (recalcNormals)
        {
            _runtimeMesh.RecalculateNormals();
            _normals = _runtimeMesh.normals;
        }
        if (recalcTangents)
        {
            // 只有在需要切线的材质情况下才做（开销较大）
            _runtimeMesh.RecalculateTangents();
        }
        if (recalcBounds)
        {
            _runtimeMesh.RecalculateBounds();
        }

        // 视情况更新 MeshCollider（昂贵：避免每帧都做）
        if (_mc && colliderUpdateInterval > 0)
        {
            _deformCountSinceLastColliderUpdate++;
            if (_deformCountSinceLastColliderUpdate >= colliderUpdateInterval)
            {
                _mc.sharedMesh = null;
                _mc.sharedMesh = _runtimeMesh;
                _deformCountSinceLastColliderUpdate = 0;
            }
        }
    }

    /// <summary>重置网格到初始形状。</summary>
    public void ResetMesh()
    {
        if (_baseVertices == null) return;
        _workVertices = (Vector3[])_baseVertices.Clone();
        _runtimeMesh.vertices = _workVertices;
        _runtimeMesh.RecalculateNormals();
        _runtimeMesh.RecalculateBounds();
        _normals = _runtimeMesh.normals;
        if (_mc)
        {
            _mc.sharedMesh = null;
            _mc.sharedMesh = _runtimeMesh;
        }
    }

    public enum BrushMode { Push, Pull, Smooth }
}