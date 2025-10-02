using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public static class LoopSubdivision {
    static CMesh cmesh;
    static List<int> tri_table;
    static List<Vector3> geo_table;
    static List<int> opposite_table;

    public static void SetParameters(CMesh new_cmesh) {
        cmesh = new_cmesh;
        tri_table = cmesh.tri_table;
        geo_table = cmesh.geo_table;
        opposite_table = cmesh.opposite_table;
    }

    public static void Subdivide(CMesh new_cmesh, int times) {
        SetParameters(new_cmesh);
        Subdivide(times);
    }

    public static void Subdivide(CMesh new_cmesh, List<Edge> hard_edges,
                                 List<Vector3> hard_vertices, int times) {
        SetParameters(new_cmesh);
        for (int i = 0; i < times; i++) {
            SubdivideHelper(hard_edges, hard_vertices);
        }
    }

    public static void Subdivide(int times) {
        for (int i = 0; i < times; i++) {
            SubdivideHelper(cmesh.hard_edges,
                            cmesh.hard_vertices);
        }
    }

    public static void SubdivideHelper(List<Edge> hard_edges,
                                       List<Vector3> hard_vertices) {
        if (opposite_table.Count == 0) {
            BuildOppositeTable();
        }

        CMesh new_mesh = new CMesh();

        int start_corner, current_corner, n;
        Vector3 vertex, neighbor_sum, new_vertex;
        float beta;
        
        // Even Vertices
        for (int vertex_index = 0; vertex_index < geo_table.Count; vertex_index++) {
            start_corner = tri_table.FindIndex(v => v == vertex_index);
            current_corner = start_corner;
            vertex = geo_table[vertex_index];

            if (IsHardEdge(hard_edges, vertex)) {
                // Hard edge found.
                new_vertex = (0.75f * vertex) +
                    (0.125f * geo_table[tri_table[NextCorner(current_corner)]]) +
                    (0.125f * geo_table[tri_table[PreviousCorner(current_corner)]]);
                new_mesh.geo_table.Add(new_vertex);
            } else if (IsHardVertex(hard_vertices, vertex)) {
                // Hard vertex found.
                new_mesh.geo_table.Add(vertex);
            } else {
                // Normal subdivision.
                neighbor_sum = new Vector3();
                n = 0;

                do {
                    neighbor_sum += 
                        geo_table[tri_table[NextCorner(current_corner)]];
                    n++;
                    current_corner = Swing(current_corner);
                } while (start_corner != current_corner && 
                            current_corner != -1 && n < 10);

                beta = 0.0f;
                if (n > 3) {
                    beta = 3.0f / (8.0f * n);
                } else if (n == 3) {
                    beta = 3.0f / 16.0f;
                }

                new_vertex = vertex * (1 - n * beta);
                new_vertex = new_vertex + (neighbor_sum * beta);
                new_mesh.geo_table.Add(new_vertex);
            }
        }

        int[] oppositeEdgeTable = new int[tri_table.Count];

        HashSet<int> set1 = new HashSet<int>();

        Vector3 a, b, c, d, odd_vertex, vec1, vec2, vec3, vec4;

        // Odd Vertices
        for (int corner = 0; corner < tri_table.Count; corner++) {
            if (!set1.Contains(corner)) {
                a = geo_table[tri_table[NextCorner(corner)]];
                b = geo_table[tri_table[NextCorner(opposite_table[corner])]];
                c = geo_table[tri_table[corner]];
                d = geo_table[tri_table[opposite_table[corner]]];

                if (IsHardEdge(hard_edges, new Edge(a, b))) {
                    // hard_edge found
                    odd_vertex = (0.5f * a) + (0.5f * b);
                    new_mesh.geo_table.Add(odd_vertex);

                    // add to hard_edges
                    hard_edges.Add(new Edge(a, odd_vertex));
                    hard_edges.Add(new Edge(odd_vertex, b));

                    // add to set1 and oppositeEdgeTable
                    set1.Add(corner);
                    set1.Add(opposite_table[corner]);
                    oppositeEdgeTable[corner] = new_mesh.geo_table.Count - 1;
                    oppositeEdgeTable[opposite_table[corner]] =
                        new_mesh.geo_table.Count - 1;
                } else {
                    // normal odd subdivision
                    vec1 = a + b;
                    vec2 = c + d;
                    vec3 = vec1 * (3.0f / 8.0f);
                    vec4 = vec2 * (1.0f / 8.0f);
                    odd_vertex = vec3 + vec4;

                    set1.Add(corner);
                    set1.Add(opposite_table[corner]);
                    new_mesh.geo_table.Add(odd_vertex);

                    oppositeEdgeTable[corner] = new_mesh.geo_table.Count - 1;
                    oppositeEdgeTable[opposite_table[corner]] =
                        new_mesh.geo_table.Count - 1;
                }
            }
        }

        int odd_vertex1, odd_vertex2, odd_vertex3;
        for (int face = 0; face < tri_table.Count; face += 3) {
            odd_vertex1 = oppositeEdgeTable[PreviousCorner(face)];
            odd_vertex2 = oppositeEdgeTable[face];
            odd_vertex3 = oppositeEdgeTable[NextCorner(face)];

            // face1
            int vi = new_mesh.geo_table.Count;
            new_mesh.geo_table.Add(new_mesh.geo_table[tri_table[face]]);
            new_mesh.geo_table.Add(new_mesh.geo_table[odd_vertex1]);
            new_mesh.geo_table.Add(new_mesh.geo_table[odd_vertex3]);
            new_mesh.tri_table.Add(vi);
            new_mesh.tri_table.Add(vi + 1);
            new_mesh.tri_table.Add(vi + 2);

            // face2
            vi = new_mesh.geo_table.Count;
            new_mesh.geo_table.Add(new_mesh.geo_table[odd_vertex1]);
            new_mesh.geo_table.Add(new_mesh.geo_table[tri_table[NextCorner(face)]]);
            new_mesh.geo_table.Add(new_mesh.geo_table[odd_vertex2]);
            new_mesh.tri_table.Add(vi);
            new_mesh.tri_table.Add(vi + 1);
            new_mesh.tri_table.Add(vi + 2);

            // face3
            vi = new_mesh.geo_table.Count;
            new_mesh.geo_table.Add(new_mesh.geo_table[odd_vertex3]);
            new_mesh.geo_table.Add(new_mesh.geo_table[odd_vertex2]);
            new_mesh.geo_table.Add(new_mesh.geo_table[tri_table[PreviousCorner(face)]]);
            new_mesh.tri_table.Add(vi);
            new_mesh.tri_table.Add(vi + 1);
            new_mesh.tri_table.Add(vi + 2);

            // face4
            vi = new_mesh.geo_table.Count;
            new_mesh.geo_table.Add(new_mesh.geo_table[odd_vertex1]);
            new_mesh.geo_table.Add(new_mesh.geo_table[odd_vertex2]);
            new_mesh.geo_table.Add(new_mesh.geo_table[odd_vertex3]);
            new_mesh.tri_table.Add(vi);
            new_mesh.tri_table.Add(vi + 1);
            new_mesh.tri_table.Add(vi + 2);
        }

        cmesh.ClearTables();

        for (int i = 0; i < new_mesh.tri_table.Count; i++) {
            tri_table.Add(new_mesh.tri_table[i]);
        }

        for (int i = 0; i < new_mesh.geo_table.Count; i++) {
            geo_table.Add(new_mesh.geo_table[i]);
        }

        new_mesh.ClearTables();

        MeshUtils.AddUv(cmesh);
        cmesh.AddToMesh();
    }

    public static bool IsHardEdge(List<Edge> hard_edges, Vector3 vertex) {
        Edge edge;
        for (int i = 0; i < hard_edges.Count; i++) {
            edge = hard_edges[i];
            if (vertex == edge.v[0] || vertex == edge.v[1]) {
                return true;
            }
        }
        return false;
    }

    public static bool IsHardEdge(List<Edge> hard_edges, Edge _edge) {
        Edge edge;
        for (int i = 0; i < hard_edges.Count; i++) {
            edge = hard_edges[i];
            if (edge.EqualTo(_edge)) {
                return true;
            }
        }
        return false;
    }

    public static bool IsHardVertex(List<Vector3> hard_vertices,
                                    Vector3 _vertex) {
        Vector3 vertex;
        for (int i = 0; i < hard_vertices.Count; i++) {
            vertex = hard_vertices[i];
            if (vertex == _vertex) {
                return true;
            }
        }
        return false;
    }

    public static void BuildOppositeTable() {
        int[] opposite_temp = new int[tri_table.Count];
        for (int i = 0; i < tri_table.Count; i++) {
            opposite_temp[i] = -1;
        }

        for (int i = 0; i < tri_table.Count; i++) {
            for (int j = 0; j < tri_table.Count; j++) {
                if (geo_table[tri_table[NextCorner(i)]] == 
                        geo_table[tri_table[PreviousCorner(j)]] &&
                        geo_table[tri_table[PreviousCorner(i)]] == 
                        geo_table[tri_table[NextCorner(j)]]) {
                    opposite_temp[i] = j;
                    opposite_temp[j] = i;
                }
            }
        }

        for (int i = 0; i < tri_table.Count; i++) {
            opposite_table.Add(opposite_temp[i]);
        }
    }

    public static int PreviousCorner(int corner) {
        return NextCorner(NextCorner(corner));
    }

    public static int NextCorner(int corner) {
        return 3 * TriangleCorner(corner) + ((corner + 1) % 3);
    }

    public static int TriangleCorner(int corner) {
        return corner / 3;
    }

    public static int Swing(int corner) {
        int next = NextCorner(corner);
        int opposite = opposite_table[next];
        if (opposite == -1) {
            return -1;
        }
        int nextnext = NextCorner(opposite);
        return nextnext;
    }
}
