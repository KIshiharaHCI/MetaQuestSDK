using UnityEngine;
using UnityEditor;

public class MeshSubdivider : EditorWindow
{
    [MenuItem("Tools/Subdivide Selected Mesh")]
    static void SubdivideMesh()
    {
        var obj = Selection.activeGameObject;
        if (obj == null) return;

        var meshFilter = obj.GetComponent<MeshFilter>();
        if (meshFilter == null) return;

        Mesh mesh = Instantiate(meshFilter.sharedMesh);
        meshFilter.sharedMesh = Subdivide(mesh);
    }

    static Mesh Subdivide(Mesh mesh)
    {
        // 简单示例：将每个三角面分成4个小三角
        var verts = mesh.vertices;
        var tris = mesh.triangles;

        var newVerts = new System.Collections.Generic.List<Vector3>();
        var newTris = new System.Collections.Generic.List<int>();

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 v1 = verts[tris[i]];
            Vector3 v2 = verts[tris[i + 1]];
            Vector3 v3 = verts[tris[i + 2]];

            Vector3 v12 = (v1 + v2) * 0.5f;
            Vector3 v23 = (v2 + v3) * 0.5f;
            Vector3 v31 = (v3 + v1) * 0.5f;

            int i1 = newVerts.Count; newVerts.Add(v1);
            int i2 = newVerts.Count; newVerts.Add(v2);
            int i3 = newVerts.Count; newVerts.Add(v3);
            int i12 = newVerts.Count; newVerts.Add(v12);
            int i23 = newVerts.Count; newVerts.Add(v23);
            int i31 = newVerts.Count; newVerts.Add(v31);

            newTris.AddRange(new int[] { i1, i12, i31 });
            newTris.AddRange(new int[] { i12, i2, i23 });
            newTris.AddRange(new int[] { i23, i3, i31 });
            newTris.AddRange(new int[] { i12, i23, i31 });
        }

        Mesh newMesh = new Mesh();
        newMesh.vertices = newVerts.ToArray();
        newMesh.triangles = newTris.ToArray();
        newMesh.RecalculateNormals();
        newMesh.RecalculateBounds();
        return newMesh;
    }
}
