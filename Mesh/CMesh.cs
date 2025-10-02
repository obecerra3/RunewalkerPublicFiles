using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// Short for CornerMesh
public class CMesh {
    public Mesh mesh;
    public List<Vector3> geo_table;
    public List<int> tri_table;
    public List<int> opposite_table;
    public List<Vector2> uv;
    public List<Vector3> hard_vertices;
    public List<Edge> hard_edges;

    public CMesh() {
        mesh = new Mesh();
        geo_table = new List<Vector3>();
        tri_table = new List<int>();
        opposite_table = new List<int>();
        uv = new List<Vector2>();
        hard_vertices = new List<Vector3>();
        hard_edges = new List<Edge>();
    }

    public void ClearTables() {
        geo_table.Clear();
        tri_table.Clear();
        opposite_table.Clear();
        uv.Clear();
    }

    public void AddTriangle(int v1, int v2, int v3) {
        tri_table.Add(v1);
        tri_table.Add(v2);
        tri_table.Add(v3);
    }

    public void AddTriangleReverse(int v1, int v2, int v3) {
        tri_table.Add(v3);
        tri_table.Add(v2);
        tri_table.Add(v1);
    }

    public void AddToMesh() {
        mesh.Clear();
        mesh.SetVertices(geo_table.ToArray());
        mesh.SetUVs(0, uv.ToArray());
        mesh.SetTriangles(tri_table.ToArray(), 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
    }

    public void AddVertices(params Vector3[] vertices) {
        foreach (Vector3 vertice in vertices) {
            geo_table.Add(vertice);
        }
    }
}
