using UnityEngine;
using System.Collections.Generic;

public static class MeshUtils {
    public static Mesh CopyMesh(Mesh mesh) {
        var new_mesh = new Mesh();
        new_mesh.vertices = mesh.vertices;
        new_mesh.triangles = mesh.triangles;
        new_mesh.uv = mesh.uv;
        new_mesh.normals = mesh.normals;
        new_mesh.tangents = mesh.tangents;
        new_mesh.colors = mesh.colors;
        return new_mesh;
    }
    
    public static CMesh BuildCp(CpState cp_state) {
        CMesh cmesh = new CMesh();
        int cp_count = cp_state.cp_count;
        Vector3[] prev_vertices = new Vector3[4];
        for (int i = 0; i < cp_count; i++) {
            float current_cp = i / (float) cp_count;
            // build control points
            cp_state.AddCp();
            // build geo_table and tri_table
            prev_vertices = AddVertsAndTris(i, current_cp, cp_state, 
                                                 cmesh, prev_vertices);
            // Update cp_position
            cp_state.UpdatePosition(current_cp);
        }

        AddUv(cmesh);
        cmesh.AddToMesh();
        return cmesh;
    }

    public static Vector3[] AddVertsAndTris(int i, float current_cp,
                                        CpState cp_state, CMesh cmesh, 
                                        Vector3[] prev_vertices) {
        Vector3 cp_pos = cp_state.position;
        Vector3 normal = cp_state.GetNormal(current_cp);
        Vector3 binormal = cp_state.GetBinormal(current_cp);

        Vector2 size_offset = cp_state.GetSizeOffset(current_cp);
        float width_offset = size_offset.x;
        float height_offset = size_offset.y;

        Vector4 orientation = cp_state.GetOrientation(current_cp);
        float width_type_left = orientation.x;
        float width_type_right = orientation.y;
        float height_type_top = orientation.z;
        float height_type_bottom = orientation.w;
        
        // top left corner
        Vector3 v0 = cp_pos + (normal * -width_offset * width_type_left) +
            (binormal * height_offset * height_type_top);
        // top right corner
        Vector3 v1 = cp_pos + (normal * width_offset * width_type_right) +
            (binormal * height_offset * height_type_top);
        // bottom right
        Vector3 v2 = cp_pos + (normal * width_offset * width_type_right) +
            (binormal * -height_offset * height_type_bottom);
        // bottom left
        Vector3 v3 = cp_pos + (normal * -width_offset * width_type_left) +
            (binormal * -height_offset * height_type_bottom);

        if (i == 0) {
            // Left face vertices.
            cmesh.AddVertices(v0, v1, v2, v3);
            // Left face triangles.
            cmesh.AddTriangle(0, 1, 3);
            cmesh.AddTriangle(3, 1, 2);
        } else if (i > 0) {
            int vi = cmesh.geo_table.Count;
            Vector3[] prev = prev_vertices;

            // Top face vertices and triangles.
            cmesh.AddVertices(prev[0], prev[1], v0, v1);
            cmesh.AddTriangle(vi + 1, vi, vi + 2);     // prev[1], prev[0], v0
            cmesh.AddTriangle(vi + 3, vi + 1, vi + 2); // v1, prev[1], v0

            // Bottom face vertices and triangles.
            vi = cmesh.geo_table.Count;
            cmesh.AddVertices(prev[2], prev[3], v2, v3);
            cmesh.AddTriangle(vi + 3, vi + 1, vi + 2); // v3, prev[3], v2
            cmesh.AddTriangle(vi + 2, vi + 1, vi);     // v2, prev[3], prev[2]

            // Front face vertices and triangles.
            vi = cmesh.geo_table.Count;
            cmesh.AddVertices(prev[1], prev[2], v1, v2);
            cmesh.AddTriangle(vi, vi + 2, vi + 1);     // prev[1], v1, prev[2]
            cmesh.AddTriangle(vi + 1, vi + 2, vi + 3); // prev[2], v1, v2

            // Back face vertices and triangles.
            vi = cmesh.geo_table.Count;
            cmesh.AddVertices(prev[0], prev[3], v0, v3);
            cmesh.AddTriangle(vi, vi + 1, vi + 3);     // prev[0], prev[3], v3 
            cmesh.AddTriangle(vi, vi + 3, vi + 2);     // prev[0], v3, v0

            if (i == cp_state.cp_count - 1) {
                // Right face vertices and triangles. Completes the CMesh.
                vi = cmesh.geo_table.Count;
                cmesh.AddVertices(v0, v1, v2, v3);
                cmesh.AddTriangle(vi, vi + 3, vi + 2); // v0, v3, v2
                cmesh.AddTriangle(vi, vi + 2, vi + 1); // v0, v2, v1
            }
        }

        return new Vector3[] { v0, v1, v2, v3 };
    }

    // Add uv according to the plane that part of the mesh is on,
    // if its on the xz plane use the xz coordinates as opposed to the 
    // xy coordinates where all of the y coordinates would be the same and
    // textures would be stretched.
    public static void AddUv(CMesh cmesh, bool draw_line = false) {
        cmesh.AddToMesh();
        var vert_count = cmesh.geo_table.Count;
        var uv = new Vector2[vert_count];
        var bounds = cmesh.mesh.bounds;
        var x_bounds = new Vector2(bounds.center.x - (bounds.size.x * 0.5f),
                                   bounds.center.x + (bounds.size.x * 0.5f));
        var y_bounds = new Vector2(bounds.center.y - (bounds.size.y * 0.5f),
                                   bounds.center.y + (bounds.size.y * 0.5f));
        var z_bounds = new Vector2(bounds.center.z - (bounds.size.z * 0.5f),
                                   bounds.center.z + (bounds.size.z * 0.5f));
        for (int i = 0; i < cmesh.geo_table.Count; i++) {
            Vector3 v = cmesh.geo_table[i];
            Vector3 n = cmesh.mesh.normals[i];
            Vector3 n_abs = n.Abs();
            float max_n = VectorUtils.MaxOf(n_abs);
            if (n_abs.x == max_n) {
                uv[i] = new Vector2(
                    Mathf.InverseLerp(y_bounds.x, y_bounds.y, v.y),
                    Mathf.InverseLerp(z_bounds.x, z_bounds.y, v.z));
            } else if (n_abs.y == max_n) {
                uv[i] = new Vector2(
                    Mathf.InverseLerp(x_bounds.x, x_bounds.y, v.x),
                    Mathf.InverseLerp(z_bounds.x, z_bounds.y, v.z));
            } else if (n_abs.z == max_n) {
                uv[i] = new Vector2(
                    Mathf.InverseLerp(x_bounds.x, x_bounds.y, v.x),
                    Mathf.InverseLerp(y_bounds.x, y_bounds.y, v.y));
            }
            if (draw_line) {
                // Draws a line showing the normal at each vertice.
                Debug.DrawLine(v, v + n * 0.5f, Color.red, 60);
            }
        }
        cmesh.uv = new List<Vector2>(uv);
    }

    public static void AddUv(Mesh mesh, bool draw_line = false) {
        Vector2[] uv = new Vector2[mesh.vertices.Length];
        var bounds = mesh.bounds;
        var x_bounds = new Vector2(bounds.center.x - (bounds.size.x * 0.5f),
                                   bounds.center.x + (bounds.size.x * 0.5f));
        var y_bounds = new Vector2(bounds.center.y - (bounds.size.y * 0.5f),
                                   bounds.center.y + (bounds.size.y * 0.5f));
        var z_bounds = new Vector2(bounds.center.z - (bounds.size.z * 0.5f),
                                   bounds.center.z + (bounds.size.z * 0.5f));
        for (int i = 0; i < mesh.vertices.Length; i++) {
            Vector3 v = mesh.vertices[i];
            Vector3 n = mesh.normals[i];
            Vector3 n_abs = n.Abs();
            float max_n = VectorUtils.MaxOf(n_abs);
            if (n_abs.x == max_n) {
                uv[i] = new Vector2(
                    Mathf.InverseLerp(y_bounds.x, y_bounds.y, v.y),
                    Mathf.InverseLerp(z_bounds.x, z_bounds.y, v.z));
            } else if (n_abs.y == max_n) {
                uv[i] = new Vector2(
                    Mathf.InverseLerp(x_bounds.x, x_bounds.y, v.x),
                    Mathf.InverseLerp(z_bounds.x, z_bounds.y, v.z));
            } else if (n_abs.z == max_n) {
                uv[i] = new Vector2(
                    Mathf.InverseLerp(x_bounds.x, x_bounds.y, v.x),
                    Mathf.InverseLerp(y_bounds.x, y_bounds.y, v.y));
            }
            if (draw_line) {
                // Draws a line showing the normal at each vertice.
                Debug.DrawLine(v, v + n * 0.5f, Color.red, 60);
            }
        }
        mesh.uv = uv;
    }

    // Add uv that are based on the xy coordinates.
    public static void AddUvXY(CMesh cmesh) {
        Vector2[] uv = new Vector2[cmesh.geo_table.Count];
        var bounds = cmesh.mesh.bounds;
        var x_bounds = new Vector2(bounds.center.x - (bounds.size.x * 0.5f),
                                   bounds.center.x + (bounds.size.x * 0.5f));
        var y_bounds = new Vector2(bounds.center.y - (bounds.size.y * 0.5f),
                                   bounds.center.y + (bounds.size.y * 0.5f));

        for (int i = 0; i < cmesh.geo_table.Count; i++) {
            Vector3 vertice = cmesh.geo_table[i];
            uv[i] = new Vector2(
                Mathf.InverseLerp(x_bounds.x, x_bounds.y, vertice.x),
                Mathf.InverseLerp(y_bounds.x, y_bounds.y, vertice.y));
        }
        cmesh.uv = new List<Vector2>(uv);
    }

    public static void AddUvXY(Mesh mesh) {
        Vector2[] uv = new Vector2[mesh.vertices.Length];
        var bounds = mesh.bounds;
        var x_bounds = new Vector2(bounds.center.x - (bounds.size.x * 0.5f),
                                   bounds.center.x + (bounds.size.x * 0.5f));
        var y_bounds = new Vector2(bounds.center.y - (bounds.size.y * 0.5f),
                                   bounds.center.y + (bounds.size.y * 0.5f));

        for (int i = 0; i < mesh.vertices.Length; i++) {
            Vector3 vertice = mesh.vertices[i];
            uv[i] = new Vector2(
                Mathf.InverseLerp(x_bounds.x, x_bounds.y, vertice.x),
                Mathf.InverseLerp(y_bounds.x, y_bounds.y, vertice.y));
        }
        mesh.uv = uv;
    }

    // Add uv that are based on the xz coordinates.
    public static void AddUvXZ(CMesh cmesh) {
        Vector2[] uv = new Vector2[cmesh.geo_table.Count];
        var bounds = cmesh.mesh.bounds;
        var x_bounds = new Vector2(bounds.center.x - (bounds.size.x * 0.5f),
                                   bounds.center.x + (bounds.size.x * 0.5f));
        var z_bounds = new Vector2(bounds.center.z - (bounds.size.z * 0.5f),
                                   bounds.center.z + (bounds.size.z * 0.5f));

        for (int i = 0; i < cmesh.geo_table.Count; i++) {
            Vector3 vertice = cmesh.geo_table[i];
            uv[i] = new Vector2(
                Mathf.InverseLerp(x_bounds.x, x_bounds.y, vertice.x),
                Mathf.InverseLerp(z_bounds.x, z_bounds.y, vertice.z));
        }
        cmesh.uv = new List<Vector2>(uv);
    }

    public static void AddUvXZ(Mesh mesh) {
        Vector2[] uv = new Vector2[mesh.vertices.Length];
        var bounds = mesh.bounds;
        var x_bounds = new Vector2(bounds.center.x - (bounds.size.x * 0.5f),
                                   bounds.center.x + (bounds.size.x * 0.5f));
        var z_bounds = new Vector2(bounds.center.z - (bounds.size.z * 0.5f),
                                   bounds.center.z + (bounds.size.z * 0.5f));

        for (int i = 0; i < mesh.vertices.Length; i++) {
            Vector3 vertice = mesh.vertices[i];
            uv[i] = new Vector2(
                Mathf.InverseLerp(x_bounds.x, x_bounds.y, vertice.x),
                Mathf.InverseLerp(z_bounds.x, z_bounds.y, vertice.z));
        }
        mesh.uv = uv;
    }

    // Add uv that are based on the yz coordinates.
    public static void AddUvYZ(CMesh cmesh) {
        Vector2[] uv = new Vector2[cmesh.geo_table.Count];
        var bounds = cmesh.mesh.bounds;
        var y_bounds = new Vector2(bounds.center.y - (bounds.size.y * 0.5f),
                                   bounds.center.y + (bounds.size.y * 0.5f));
        var z_bounds = new Vector2(bounds.center.z - (bounds.size.z * 0.5f),
                                   bounds.center.z + (bounds.size.z * 0.5f));

        for (int i = 0; i < cmesh.geo_table.Count; i++) {
            Vector3 vertice = cmesh.geo_table[i];
            uv[i] = new Vector2(
                Mathf.InverseLerp(y_bounds.x, y_bounds.y, vertice.y),
                Mathf.InverseLerp(z_bounds.x, z_bounds.y, vertice.z));
        }
        cmesh.uv = new List<Vector2>(uv);
    }

    public static void AddUvYZ(Mesh mesh) {
        Vector2[] uv = new Vector2[mesh.vertices.Length];
        var bounds = mesh.bounds;
        var y_bounds = new Vector2(bounds.center.y - (bounds.size.y * 0.5f),
                                   bounds.center.y + (bounds.size.y * 0.5f));
        var z_bounds = new Vector2(bounds.center.z - (bounds.size.z * 0.5f),
                                   bounds.center.z + (bounds.size.z * 0.5f));

        for (int i = 0; i < mesh.vertices.Length; i++) {
            Vector3 vertice = mesh.vertices[i];
            uv[i] = new Vector2(
                Mathf.InverseLerp(y_bounds.x, y_bounds.y, vertice.y),
                Mathf.InverseLerp(z_bounds.x, z_bounds.y, vertice.z));
        }
        mesh.uv = uv;
    }

    public static void RotateMesh(Mesh mesh, Vector3 euler_angles) {
        Quaternion q = new Quaternion();
        q.eulerAngles = euler_angles;

        Vector3[] new_vertices = new Vector3[mesh.vertices.Length];

        for (int i = 0; i < mesh.vertices.Length; i++) {
            new_vertices[i] = q * mesh.vertices[i];
        }

        mesh.vertices = new_vertices;
    }

    public static Vector2 GetBoundsX(Mesh mesh) {
        var bounds = mesh.bounds;
        return new Vector2(bounds.center.x - (bounds.size.x * 0.5f),
                           bounds.center.x + (bounds.size.x * 0.5f));
    }

    public static Vector2 GetBoundsY(Mesh mesh) {
        var bounds = mesh.bounds;
        return new Vector2(bounds.center.y - (bounds.size.y * 0.5f),
                           bounds.center.y + (bounds.size.y * 0.5f));
    }

    public static Vector2 GetBoundsZ(Mesh mesh) {
        var bounds = mesh.bounds;
        return new Vector2(bounds.center.z - (bounds.size.z * 0.5f),
                           bounds.center.z + (bounds.size.z * 0.5f));
    }
}