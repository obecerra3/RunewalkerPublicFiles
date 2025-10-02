using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CpState {
    public List<Vector3> cps = new List<Vector3>();
    public Vector3 position;
    public List<float> distances;
    public List<float> bend_points;
    public List<Vector3> directions;
    public List<Vector3> normals;
    public List<Vector3> binormals;
    public List<Vector2> size_offsets; //(width, height)
    public List<Vector4> orientations;
    public float length;
    public int cp_count;

    public CpState(int cp_count, Vector3 position, List<float> distances,
                   List<float> bend_points, List<Vector3> directions,
                   List<Vector2> size_offsets, List<Vector4> orientations) {
        this.cp_count = cp_count;
        this.position = position;
        this.distances = distances;
        this.bend_points = bend_points;
        this.directions = directions;
        this.size_offsets = size_offsets;
        this.orientations = orientations;

        CalculateNormalsAndBinormals();

        CalculateLength();
    }

    public CpState(int cp_count, Vector3 position, List<float> distances,
               List<float> bend_points, List<Vector3> directions,
               Vector2 size_offset, List<Vector4> orientations) {
        this.cp_count = cp_count;
        this.position = position;
        this.distances = distances;
        this.bend_points = bend_points;
        this.directions = directions;
        size_offsets = new List<Vector2>();
        for (int i = 0; i < cp_count; i++) {
            size_offsets.Add(size_offset);
        }
        this.orientations = orientations;

        CalculateNormalsAndBinormals();

        CalculateLength();
    }

    public void CalculateNormalsAndBinormals() {
        normals = new List<Vector3>();
        binormals = new List<Vector3>();
        Vector3 direction, normal, binormal;
        for (int i = 0; i < directions.Count; i++) {
            direction = directions[i];
            if (direction.Equals(Vector3.up) || direction.Equals(Vector3.down)) {
                normal = Vector3.Cross(direction, Vector3.right).normalized;
            } else {
                normal = Vector3.Cross(direction, Vector3.down).normalized;
            }
            binormal = Vector3.Cross(direction, normal).normalized;
            this.normals.Add(normal);
            this.binormals.Add(binormal);
        }
    }

    public void CalculateLength() {
        var temp_length = 0f;
        for (int i = 0; i < distances.Count; i++) {
            temp_length += distances[i];
        }
        this.length = temp_length;
    }

    public Vector4 GetOrientation(float current_cp) {
        return orientations[Mathf.RoundToInt(current_cp * orientations.Count)];
    }

    public void AddCp() {
        cps.Add(position);
    }

    public float GetTotalDistanceTo(float current_cp) {
        float j = 0;
        float total_distance = 0;
        for (int i = 0; i < distances.Count; i++) {
            j = i / (float) distances.Count;
            total_distance += GetDistance(j);
            if (j == current_cp) {
                return total_distance;
            }
        }
        return length;
    }

    public float GetDistance(float current_cp) {
        return distances[Mathf.RoundToInt(current_cp * distances.Count)];
    }

    public Vector3 GetDirection(float current_cp) {
        for (int i = 0; i < bend_points.Count; i++) {
            if (current_cp <= bend_points[i]) {
                return directions[i];
            }
        }
        return directions[directions.Count - 1];
    }

    public Vector3 GetNormal(float current_cp) {
        for (int i = 0; i < bend_points.Count; i++) {
            if (current_cp < bend_points[i]) {
                return normals[i];
            }
        }
        return normals[normals.Count - 1];
    }

    public Vector3 GetBinormal(float current_cp) {
        for (int i = 0; i < bend_points.Count; i++) {
            if (current_cp < bend_points[i]) {
                return binormals[i];
            }
        }
        return binormals[binormals.Count - 1];
    }

    public Vector2 GetSizeOffset(float current_cp) {
        return size_offsets[Mathf.RoundToInt(current_cp * size_offsets.Count)];
    }

    public void UpdatePosition(float current_cp) {
        position += GetDirection(current_cp) * GetDistance(current_cp);
    }

    public Vector3 GetNextNormal(float current_cp) {
        for (int i = 0; i < bend_points.Count; i++) {
            if (current_cp < bend_points[i]) {
                return normals[Mathf.Min(i + 1, normals.Count - 1)];
            }
        }
        return normals[normals.Count - 1];
    }

    public Vector3 GetNextBinormal(float current_cp) {
        for (int i = 0; i < bend_points.Count; i++) {
            if (current_cp < bend_points[i]) {
                return binormals[Mathf.Min(i + 1, binormals.Count - 1)];
            }
        }
        return binormals[binormals.Count - 1];
    }
}
