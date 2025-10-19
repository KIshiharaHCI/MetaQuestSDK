using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal runtime remesher:
/// - Longest-edge triangle split (with shared-edge midpoint cache)
/// - Lightweight Laplacian smoothing (λ-only; shrink-minimizing variants can be added)
/// Call periodically to maintain triangle quality under deformation.
/// </summary>
[RequireComponent(typeof(MeshFilter))]
public class RuntimeRemesher : MonoBehaviour
{
    [Header("Remesh Trigger")]
    public bool autoRun = true;
    [Tooltip("Run a remesh step every X seconds while enabled.")]
    public float intervalSeconds = 0.25f;

    [Header("Split Settings")]
    [Tooltip("Edges longer than this (in world units) will be split.")]
    public float maxEdgeLength = 0.05f;
    [Tooltip("Maximum number of triangle splits per step to avoid runaway growth.")]
    public int splitsPerStep = 200;

    [Header("Smoothing")]
    [Tooltip("Laplacian smoothing iterations after splits (0 = off).")]
    public int smoothIterations = 1;
    [Tooltip("Smoothing step size (0..1). 0.2 is a gentle relax.")]
    [Range(0f, 1f)]
    public float smoothLambda = 0.2f;

    [Header("Optional Collider Refresh")]
    public MeshCollider meshCollider;           // leave null to skip

    MeshFilter _mf;
    Mesh _mesh;
    Transform _t;

    float _timer;

    void Awake()
    {
        _mf = GetComponent<MeshFilter>();
        _mesh = _mf.sharedMesh;
        _t = transform;

        if (_mesh == null) return;
        // Do NOT instantiate here; another component (e.g., EditableMesh) may already own the runtime instance.
        // We'll always fetch the latest reference right before remeshing.

        if (meshCollider == null)
            meshCollider = GetComponent<MeshCollider>();
        if (meshCollider)
            meshCollider.sharedMesh = _mesh;
    }

    void Update()
    {
        if (!autoRun) return;
        _timer += Time.deltaTime;
        if (_timer >= intervalSeconds)
        {
            _timer = 0f;
            // Always sync the mesh reference in case another script swapped sharedMesh.
            _mesh = _mf.sharedMesh;
            RemeshStep();
            // log in unity
            Debug.Log("Remesh step performed. Vertex count: " + _mesh.vertexCount + ", Triangle count: " + (_mesh.triangles.Length / 3));
        }
    }

    public void RemeshStep()
    {
        if (_mesh == null) return;

        // Make sure we operate on the current mesh instance
        if (_mesh != _mf.sharedMesh) _mesh = _mf.sharedMesh;

        // Safety: if mesh is not readable/writable, bail.
        #if UNITY_2021_3_OR_NEWER
        if (!_mesh.isReadable) { Debug.LogWarning("RuntimeRemesher: Mesh is not readable."); return; }
        #endif

        // Pull data into lists we can mutate
        var verts = new List<Vector3>(_mesh.vertices);
        var tris  = new List<int>(_mesh.triangles);

        // Split long edges (limit by budget)
        int splits = SplitLongEdges(verts, tris, maxEdgeLength, splitsPerStep);

        // Optional smoothing (simple Laplacian)
        if (smoothIterations > 0 && verts.Count > 0)
        {
            // Build adjacency once
            var adjacency = BuildVertexAdjacency(verts.Count, tris);
            LaplacianSmooth(verts, adjacency, smoothIterations, smoothLambda, _t);
        }

        // Push results back
        _mesh.SetVertices(verts);
        _mesh.SetTriangles(tris, 0, true);
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();

        if (_mf.sharedMesh != _mesh)
        {
            Debug.LogWarning("RuntimeRemesher: MeshFilter.sharedMesh was swapped during remesh; reassigning to keep renderer in sync.");
            _mf.sharedMesh = _mesh;
        }

        if (meshCollider)
        {
            // Refresh collider (expensive; do this only as often as you need)
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = _mesh;
        }
    }

    // --- Core: split long edges ------------------------------------------------

    struct EdgeKey
    {
        public int a, b; // a < b
        public EdgeKey(int i, int j)
        {
            if (i < j) { a = i; b = j; } else { a = j; b = i; }
        }
        public override int GetHashCode() => (a * 73856093) ^ (b * 19349663);
        public override bool Equals(object obj) => obj is EdgeKey k && k.a == a && k.b == b;
    }

    static int SplitLongEdges(List<Vector3> verts, List<int> tris, float maxEdgeLenWorld, int budget,
                              Transform t = null)
    {
        if (t == null) t = (Transform)null; // optional

        int splitsDone = 0;
        float maxLen2 = maxEdgeLenWorld * maxEdgeLenWorld;

        // Cache for edge midpoints shared by adjacent triangles
        var midpointIndex = new Dictionary<EdgeKey, int>(capacity: tris.Count);

        // Iterate over triangles. We’ll modify in place; use index i stepping carefully.
        for (int i = 0; i < tris.Count && splitsDone < budget; i += 3)
        {
            int i0 = tris[i];
            int i1 = tris[i + 1];
            int i2 = tris[i + 2];

            Vector3 v0L = verts[i0], v1L = verts[i1], v2L = verts[i2];

            // Measure edge lengths in WORLD space so threshold is scale-invariant
            Vector3 v0W = t ? t.TransformPoint(v0L) : v0L;
            Vector3 v1W = t ? t.TransformPoint(v1L) : v1L;
            Vector3 v2W = t ? t.TransformPoint(v2L) : v2L;

            float l01 = (v0W - v1W).sqrMagnitude;
            float l12 = (v1W - v2W).sqrMagnitude;
            float l20 = (v2W - v0W).sqrMagnitude;

            // Find the longest edge
            int a = -1, b = -1, other = -1;
            float longest = l01; a = i0; b = i1; other = i2;
            if (l12 > longest) { longest = l12; a = i1; b = i2; other = i0; }
            if (l20 > longest) { longest = l20; a = i2; b = i0; other = i1; }

            if (longest <= maxLen2) continue; // triangle is fine

            // Get or create midpoint index for edge (a,b)
            int mid;
            var ek = new EdgeKey(a, b);
            if (!midpointIndex.TryGetValue(ek, out mid))
            {
                Vector3 midL = 0.5f * (verts[a] + verts[b]);
                mid = verts.Count;
                verts.Add(midL);
                midpointIndex[ek] = mid;
            }

            // Replace the current triangle (a,b,other) with two:
            // (mid, other, a) and (mid, b, other)
            tris[i]     = mid;
            tris[i + 1] = other;
            tris[i + 2] = a;
            tris.Insert(i + 3, mid);
            tris.Insert(i + 4, b);
            tris.Insert(i + 5, other);

            splitsDone++;

            // Advance to next triangle after the newly inserted one
            // We already processed current "slot"; skip the inserted triangle this frame
            i += 3;
        }

        return splitsDone;
    }

    // --- Smoothing -------------------------------------------------------------

    static List<int>[] BuildVertexAdjacency(int vertexCount, List<int> tris)
    {
        var adj = new List<int>[vertexCount];
        for (int i = 0; i < vertexCount; i++) adj[i] = new List<int>(6);

        void Link(int x, int y)
        {
            if (!adj[x].Contains(y)) adj[x].Add(y);
            if (!adj[y].Contains(x)) adj[y].Add(x);
        }

        for (int i = 0; i < tris.Count; i += 3)
        {
            int i0 = tris[i], i1 = tris[i + 1], i2 = tris[i + 2];
            Link(i0, i1); Link(i1, i2); Link(i2, i0);
        }
        return adj;
    }

    static void LaplacianSmooth(List<Vector3> verts, List<int>[] adj, int iterations, float lambda, Transform t)
    {
        // Simple umbrella operator in LOCAL space (keeps it cheap).
        // For less shrinkage, add a Taubin μ step afterward (μ ≈ -0.53 * λ).
        int n = verts.Count;
        var temp = new Vector3[n];

        for (int it = 0; it < iterations; it++)
        {
            for (int i = 0; i < n; i++)
            {
                var nbrs = adj[i];
                if (nbrs.Count == 0) { temp[i] = verts[i]; continue; }

                Vector3 avg = Vector3.zero;
                for (int k = 0; k < nbrs.Count; k++) avg += verts[nbrs[k]];
                avg /= nbrs.Count;

                temp[i] = Vector3.Lerp(verts[i], avg, lambda);
            }
            // write back
            for (int i = 0; i < n; i++) verts[i] = temp[i];
        }
    }
}