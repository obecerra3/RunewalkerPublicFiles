using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TreeBuilder : MonoBehaviour {
    public int tree_seed;

    public List<Branch> branch_list = new List<Branch>();
    public List<GameObject> branch_go_list = new List<GameObject>();

    public List<GameObject> leaf_go_list = new List<GameObject>();
    public List<GameObject> flower_go_list = new List<GameObject>();
    public List<GameObject> branch_leaf_go_list = new List<GameObject>();

    // affected by gravity.
    public float plagiotropic;
    // growing upwards.
    public float orthotropic;

    public float internode_radius;
    public float internode_length;
    public float min_radius_per_growth = 0f;
    public System.Func<Branch, Bud, float, float> RadiusPerGrowth;
    public System.Func<Branch, Bud, float> InternodeLengthPerGrowth;
    public int internodes_per_growth;

    public string[] ramification_types = { "continuous", "rhythmic", "diffuse", "palm", "pine" };
    public string ramification_type;

    // TODO: this determines how the leaves are placed on a branch.
    public string[] phyllotaxy_types = { "distic", "spiral" };
    public string phyllotaxy_type;

    // TODO: implement this to restrict the max amount of orders of branch_list
    // that can be created.
    public int max_order;

    // How many new branch_list can sprout from an axillary bud.
    public int min_buds_per_node;
    public int max_buds_per_node;

    public float pause_prob;
    public float death_prob;
    public float ramification_prob;

    public float min_dimension_trunk = 5;
    public float max_dimension_trunk = 10;
    public float min_dimension = 3;
    public float max_dimension = 10;

    public int growth_cycles;

    public float conical;

    public GameObject parent_object;

    public float trunk_wiggle = 0.0f;
    public float trunk_radius_mult = 1f;
    public float trunk_length_mult = 1f;

    // Determines the offset of new buds on an axillary bud and the offset
    // off new buds on the growth direction.
    public float wiggle;

    public bool has_leaf = false;
    public Mesh leaf_mesh;
    public Material leaf_material;
    public GameObject leaf_go;
    public int leaf_density = 1;
    public List<TransformUtility> leaf_transforms;

    public bool has_flower = false;
    public GameObject flower_go;
    public float flower_chance = 0.05f;

    public int leaf_rotation_y_min = -75;
    public int leaf_rotation_y_max = 75;

    public Material branch_material;
    public Material order_2_material;

    public int axillary_dimension_max = 10;
    public int axillary_dimension_min = 3;

    public float first_bud_height = 0f;

    public void SetBranchMaterial(Material material, Material order_2_material = null) {
        this.branch_material = material;
        this.order_2_material = order_2_material;
    }

    public void SetLeaf(Mesh mesh, Material material) {
        has_leaf = true;
        this.leaf_mesh = mesh;
        this.leaf_material = material;
    }

    public void SetLeaf(GameObject leaf_go) {
        has_leaf = true;
        this.leaf_go = leaf_go;
    }

    public void SetFlower(GameObject flower_go) {
        has_flower = true;
        this.flower_go = flower_go;
    }

    public void SetLeafTransforms(List<TransformUtility> leaf_transforms) {
        this.leaf_transforms = leaf_transforms;
    }

    public void RandomDefaultFromSeed(int seed, GameObject new_parent_object) {
        tree_seed = seed;
        Random.InitState(tree_seed);

        plagiotropic = Random.Range(0.4f, 1.2f);
        if (Random.value > 0.5f) {
            orthotropic = Random.Range(-1f, -0.1f);
        } else {
            orthotropic =  Random.Range(0.1f, 1f);
        }

        internode_radius = Random.Range(0.05f, 0.15f);
        internode_length = Random.Range(0.5f, 1.5f);

        internodes_per_growth = Random.Range(1, 5);

        ramification_type = ramification_types[Random.Range(0, 3)];
        phyllotaxy_type = phyllotaxy_types[Random.Range(0, 1)];

        max_order = Random.Range(6, 9);
        min_buds_per_node = Random.Range(1, 2);

        pause_prob = Random.Range(0.1f, 0.2f);
        death_prob = Random.Range(0.05f, 0.2f);
        ramification_prob = Random.Range(0.2f, 0.3f);

        growth_cycles = Random.Range(3, 5);

        min_dimension_trunk = Random.Range(1, 9);
        min_dimension = Random.Range(2, 5);

        conical = Random.Range(0.05f, 0.1f);

        wiggle = Random.Range(0.25f, 0.75f);

        // Instantiate first branch
        Vector3 tangent = new Vector3(0.1f * plagiotropic, 1, 0.1f * plagiotropic).normalized;
        Branch initial_branch = new Branch(this, new Vector3(0, 0, 0), tangent, 1);
        initial_branch.bud_list[0].age = 1;
        branch_list.Add(initial_branch);

        parent_object = new_parent_object;

        RadiusPerGrowth = DefaultRadiusPerGrowth;
    }

    public void CreateBranches() {
        //build branch_list data structure
        List<Branch> new_branch_list;
        for (int g = 0; g < growth_cycles; g++) {
            new_branch_list = new List<Branch>();
            for (int b = 0; b < branch_list.Count; b++) {
                branch_list[b].Grow(new_branch_list);
            }
            if (new_branch_list.Count > 0) {
                branch_list.AddRange(new_branch_list);
            }
        }

        List<Branch> empty_branch_list = new List<Branch>();
        for (int b = 0; b < branch_list.Count; b++) {
            if (branch_list[b].bud_list.Count == 1) {
                empty_branch_list.Add(branch_list[b]);
            }
        }
        for (int b = 0; b < empty_branch_list.Count; b++) {
            branch_list.Remove(empty_branch_list[b]);
        }
    }

    public void RenderBranches() {
        // Render branch_list by creating new GameObjects with a mesh and texture
        for (int b = 0; b < branch_list.Count; b++) {
            Branch branch = branch_list[b];
            GameObject branch_go = new GameObject();
            branch_go.layer = LayerMask.NameToLayer("Tree");

            MeshFilter mesh_filter = (MeshFilter) branch_go.AddComponent<MeshFilter>();
            MeshRenderer mesh_renderer = (MeshRenderer) branch_go.AddComponent<MeshRenderer>();
            mesh_renderer.shadowCastingMode = ShadowCastingMode.Off;
            mesh_renderer.receiveShadows = false;
            mesh_renderer.material = branch_material;
            if (branch.order == 2 && order_2_material != null) {
                mesh_renderer.material = order_2_material;
            }
            Renderer renderer = branch_go.GetComponent<Renderer>();

            Mesh branch_mesh = CreateBranchMesh(branch);
            mesh_filter.mesh = branch_mesh;
            MeshUtils.AddUvYZ(branch_mesh);

            branch_go.name = "Branch order: " + branch.order;
            branch_go.transform.parent = parent_object.transform;
            branch_go_list.Add(branch_go);

            // Add the local branch leaf go list as child transforms and then
            // clear the list.
            foreach (GameObject leaf_go in branch_leaf_go_list) {
                leaf_go.transform.parent = branch_go.transform;
            }
            branch_leaf_go_list.Clear();
        }
    }

    public Mesh CreateBranchMesh(Branch branch) {
        Mesh branch_mesh = new Mesh();

        Bud current_bud;
        Bud next_bud;
        Vector3 point_on_internode;

        int column = 0;
        int row = 0;

        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<int> triangles = new List<int>();

        int vertex_index = 0;

        float theta = 0;
        float radius = 0;
        float y_step_size = 0.25f;
        int circle_step_size = 16;

        Vector3 apical_bud_pos = branch.bud_list[branch.bud_list.Count - 1].position;

        for (int b = 0; b < branch.bud_list.Count - 1; b++) {

            //// Don't build the mesh past this point.
            //if (branch.order == 1 && b == branch.bud_list.Count - 3) {
            //    //y_step_size *= 0.5f;
            //    break;
            //}

            current_bud = branch.bud_list[b];
            next_bud = branch.bud_list[b + 1];
            if (next_bud.apical) {
                y_step_size *= 0.5f;
            }
            for (float t = 0; t < 1; t += y_step_size) {
                point_on_internode = Degree1Bezier(t, current_bud.position, next_bud.position);
                // DebugInternodeSphere(point_on_internode);
                column = 0;
                radius = RadiusPerGrowth(branch, current_bud, Vector3.Distance(apical_bud_pos, point_on_internode));
                for (theta = 0; theta < 2f*Mathf.PI; theta += 2f*Mathf.PI / circle_step_size) {
                    vertices.Add(point_on_internode + GetVertexOffset(theta, current_bud.tangent, radius, branch.order));
                    uv.Add(new Vector2(column / (circle_step_size + 1), row));
                    vertex_index++;
                    column++;
                }
                row++;
            }

            // Add leaves to the branch.
            if (has_leaf && branch.order >= 2 && !current_bud.axillary &&
                !current_bud.apical && !current_bud.initial && ramification_type != "palm") {
                if (leaf_go != null) {
                    for (int i = 0; i < leaf_density; i++) {
                        GameObject leaf_clone = Instantiate(leaf_go);
                        leaf_clone.layer = LayerMask.NameToLayer("Tree");
                        leaf_clone.SetActive(true);
                        //leaf_clone.AddComponent<Leaf>();
                        float r = InternodeLengthPerGrowth(branch, current_bud) * 2f;
                        Vector3 random_offset = new Vector3(Random.Range(-r, r), Random.Range(0, r), Random.Range(-r, r));
                        leaf_clone.transform.position = current_bud.position + LeafOffset(current_bud, radius) + random_offset;
                        leaf_clone.transform.parent = parent_object.transform;

                        Vector3 local_scale = leaf_clone.transform.localScale;
                        local_scale.x *= Random.Range(0.5f, 1f);
                        local_scale.y *= Random.Range(0.5f, 1f);
                        leaf_clone.transform.localScale = local_scale;

                        Vector3 local_euler = leaf_clone.transform.localEulerAngles;
                        local_euler.x = Random.Range(45f, 75f);
                        local_euler.y = Random.Range(leaf_rotation_y_min, leaf_rotation_y_max);
                        //local_euler.y = Random.Range(0f, 180f);
                        leaf_clone.transform.localEulerAngles = local_euler;

                        leaf_go_list.Add(leaf_clone);
                        branch_leaf_go_list.Add(leaf_clone);

                        if (has_flower && Random.value < flower_chance) {
                            // Lets put a flower here too lads.
                            GameObject flower_clone = Instantiate(flower_go);
                            flower_clone.layer = LayerMask.NameToLayer("Tree");
                            flower_clone.SetActive(true);
                            //leaf_clone.AddComponent<Leaf>();
                            r = InternodeLengthPerGrowth(branch, current_bud) * 2f;
                            random_offset = new Vector3(Random.Range(-r, r), Random.Range(0, r), Random.Range(-r, r));
                            flower_clone.transform.position = current_bud.position + LeafOffset(current_bud, radius) + random_offset;
                            flower_clone.transform.parent = parent_object.transform;

                            local_scale = flower_clone.transform.localScale;
                            local_scale.x *= Random.Range(0.5f, 1f);
                            local_scale.y *= Random.Range(0.5f, 1f);
                            flower_clone.transform.localScale = local_scale;

                            local_euler = flower_clone.transform.localEulerAngles;
                            local_euler.x = Random.Range(45f, 75f);
                            local_euler.y = Random.Range(leaf_rotation_y_min, leaf_rotation_y_max);
                            //local_euler.y = Random.Range(0f, 180f);
                            flower_clone.transform.localEulerAngles = local_euler;

                            leaf_go_list.Add(flower_clone); // adding this to leaf go list for rotate logic in tree plant.
                            flower_go_list.Add(flower_clone);
                            branch_leaf_go_list.Add(flower_clone);
                        }
                    }
                } else {
                    // Using a leaf mesh, eww.
                    GameObject leaf_object = new GameObject("Leaf");
                    leaf_object.layer = LayerMask.NameToLayer("Tree");
                    leaf_object.AddComponent<Leaf>();
                    MeshFilter mesh_filter = (MeshFilter) leaf_object.AddComponent<MeshFilter>();
                    MeshRenderer mesh_renderer = (MeshRenderer) leaf_object.AddComponent<MeshRenderer>();
                    mesh_renderer.shadowCastingMode = ShadowCastingMode.Off;
                    mesh_renderer.receiveShadows = false;
                    mesh_renderer.material = leaf_material;
                    leaf_object.transform.position = current_bud.position + LeafOffset(current_bud, radius);
                    leaf_object.transform.parent = parent_object.transform;
                    Renderer renderer = leaf_object.GetComponent<Renderer>();
                    mesh_filter.mesh = leaf_mesh;

                    leaf_go_list.Add(leaf_object);
                    branch_leaf_go_list.Add(leaf_object);
                }
            }
        }

        if (has_leaf && leaf_go != null && branch.order == 2 && ramification_type == "palm" && leaf_transforms != null) {
            GameObject leaf_clone = Instantiate(leaf_go);
            leaf_clone.SetActive(true);
            leaf_clone.transform.parent = parent_object.transform;
            leaf_clone.transform.localPosition = leaf_transforms[0].localPosition;
            leaf_clone.transform.localEulerAngles = leaf_transforms[0].localEulerAngles;
            leaf_clone.transform.localScale = leaf_transforms[0].localScale;
            leaf_transforms.RemoveAt(0);

            leaf_go_list.Add(leaf_clone);
            branch_leaf_go_list.Add(leaf_clone);
        }

        vertices.Add(apical_bud_pos);
        uv.Add(new Vector2(1.0f, 1.0f));

        //divide each value in row by row max - 1
        for (int i = 0; i < uv.Count; i++) {
            //draw vertices
            // debugInternodeSphere(vertices[i], Color.blue);
            uv[i] = new Vector2(uv[i].x, uv[i].y / (row - 1));
        }

        for (int v = 0; v < vertices.Count - 1; v++) {
            //for drawing the very tip of the branch
            if (v >= vertices.Count - circle_step_size - 1) {
                if ((v + 1) % circle_step_size == 0) {
                    triangles.Add(v);
                    triangles.Add(v - circle_step_size + 1);
                    triangles.Add(vertices.Count - 1);
                } else {
                    triangles.Add(v);
                    triangles.Add(v + 1);
                    triangles.Add(vertices.Count - 1);
                }
            } else if ((v + 1) % circle_step_size == 0) {
                //for drawing last vertice in ring of internode
                triangles.Add(v);
                triangles.Add(v - circle_step_size + 1);
                triangles.Add(v + circle_step_size);
                triangles.Add(v - circle_step_size + 1);
                triangles.Add(v + 1);
                triangles.Add(v + circle_step_size);
            } else {
                //for drawing ring of internode
                triangles.Add(v);
                triangles.Add(v + 1);
                triangles.Add(v + circle_step_size);
                triangles.Add(v + 1);
                triangles.Add(v + 1 + circle_step_size);
                triangles.Add(v + circle_step_size);
            }
        }

        branch_mesh.vertices = vertices.ToArray();
        branch_mesh.triangles = triangles.ToArray();
        branch_mesh.uv = uv.ToArray();
        branch_mesh.RecalculateNormals();

        return branch_mesh;
    }

    public Vector3 LeafOffset(Bud bud, float radius) {
        float theta = Random.Range(0f, 2f*Mathf.PI);
        Vector3 tangent = bud.tangent;
        Vector3 normal = Vector3.Cross(tangent, Vector3.right).normalized;
        Vector3 binormal = Vector3.Cross(tangent, normal).normalized;
        Vector3 offset = ((normal * Mathf.Cos(theta)) + (binormal * Mathf.Sin(theta))) * radius;
        return offset;
    }

    public Vector3 GetVertexOffset(float theta, Vector3 tangent, float radius, int order) {
        Vector3 normal;

        if (tangent.Equals(Vector3.up) || tangent.Equals(Vector3.down)) {
            normal = Vector3.Cross(tangent, Vector3.right).normalized;
        } else {
            normal = Vector3.Cross(tangent, Vector3.down).normalized;
        }

        Vector3 binormal = Vector3.Cross(tangent, normal).normalized;

        return ((normal * Mathf.Cos(theta)) + (binormal * Mathf.Sin(theta))) * radius;
    }

    // 
    public float DefaultRadiusPerGrowth(Branch branch, Bud bud, float distance_to_apical) {
        float radius = internode_radius * (1f / (branch.order * 2f));
        radius *= Mathf.Min((distance_to_apical + 0.6f), 1f);

        // trunk radius multiplier
        if (branch.order == 1) {
            radius *= trunk_radius_mult;
        }

        // The top of the trunk should not appear.
        if (branch.order == 1 && distance_to_apical < 0.3f) {
            radius = 0f;
        }
        
        return radius;
    }

    public float PomeloRadiusPerGrowth(Branch branch, Bud bud, float distance_to_apical) {
        float radius = internode_radius * (1f / (branch.order * 2f));

        radius *= Mathf.Min((distance_to_apical + 0.6f), 1f);

        // trunk radius multiplier
        if (branch.order == 1) {
            radius *= 0.8f;
        }
        // The top of the trunk should not appear.
        if (branch.order == 1 && distance_to_apical < 0.3f) {
            radius = 0f;
        }

        return radius;
    }

    public float DefaultInternodeLength(Branch branch, Bud bud) {
        if (branch.parent_bud == null) {
            float trunk_value = internode_length * (1f / (branch.order * 1.75f)) * ((1f / bud.dimension) * 2f);
            // trunk length multiplier
            if (branch.order == 1) {
                trunk_value *= trunk_length_mult;
            }
            return trunk_value;
        }
        float parent_dimension = branch.parent_bud.dimension;
        float value = internode_length * (1f / (branch.order * 3f)) * ((1f / bud.dimension) * 2f) * ((1f / (parent_dimension * 0.75f)));
        return value;
    }

    // This just makes cyclinders.
    public float CylinderRadiusPerGrowth(Branch branch, Bud bud, float distance_to_apical) {
        float radius = internode_radius;
        return radius;
    }

    // Makes a cactus like smooth cone shape towards the top.
    public float SmoothPointRadiusPerGrowth(Branch branch, Bud bud, float distance_to_apical) {
        float radius = internode_radius;
        if (distance_to_apical < 0.3f) {
            // Multiplying by 0.8f makes the shape slightly sharper at the
            // top.
            radius *= Mathf.Sqrt((distance_to_apical * 0.8f) / 0.3f);
        }
        return radius;
    }

    public float PineRadiusPerGrowth(Branch branch, Bud bud, float distance_to_apical) {
        float radius = internode_radius;
        if (branch.order == 2) {
            radius *= (1f / (float) branch.order) * 1.5f;
        } else if (branch.order == 3) {
            radius *= (1f / (float) branch.order) * 1f;
        }
        float linear_radius = radius * distance_to_apical * 0.5f;
        radius = Mathf.Max(radius, linear_radius);
        radius = Mathf.Min(radius * Mathf.Sqrt((distance_to_apical * 0.3f) / 0.3f), radius);
        return radius;
    }

    public float PineInternodeLength(Branch branch, Bud bud) {
        if (branch.order == 1) {
            return internode_length;
        } else if (branch.order == 2) {
            return internode_length * (1f / branch.order) * (1f / (branch.parent_bud.dimension)) * 2f;
        } else {
            return internode_length * (1f / branch.order) * (1f / (branch.parent_bud.dimension)) * 1f;
        }
    }

    public float MushroomRadiusPerGrowth(Branch branch, Bud bud, float distance_to_apical) {
        // Internode radius multiplier to make it skinner compared to the cap.
        float radius = internode_radius;
        if (distance_to_apical < 0.3f) {
            radius *= Mathf.Sqrt(10f * ((distance_to_apical) / 0.3f));
        }
        return radius;
    }

    public float FlatCapRadiusPerGrowth(Branch branch, Bud bud, float distance_to_apical) {
        float radius = internode_radius;
        if (distance_to_apical < 0.3f) {
            radius -= (1f - distance_to_apical);
        }
        return radius;
    }

    public Vector3 Degree1Bezier(float t, Vector3 point1, Vector3 point2) {
        return ((1 - t) * point1) + (t * point2);
    }

    public void DebugDrawBuds(Branch branch) {
        GameObject sphere;
        Bud bud;
        for (int b = 0; b < branch.bud_list.Count; b++) {
            if (branch.order == 1) {
                bud = branch.bud_list[b];
                sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.localScale *= 0.5f;
                sphere.transform.position = bud.position;
                sphere.transform.parent = parent_object.transform;
                Renderer renderer = sphere.GetComponent<Renderer>();
                renderer.material.color = Color.green;
            }
        }
    }

    public float InternodeLengthPerGrowthPerOrder(Branch branch, Bud bud) {
        return internode_length * (1f / (branch.order * 0.15f));
    }

    public float InternodeLengthPerGrowthSame(Branch branch, Bud bud) {
        return internode_length;
    }

    public float PalmRadiusPerGrowth(Branch branch, Bud bud, float distance_to_apical) {
        float radius = internode_radius;
        if (branch.order == 1) {
            float dist_square = Mathf.Pow(distance_to_apical, 2f);
            radius = Mathf.Max(radius * dist_square * 1f, radius * Mathf.Sqrt((distance_to_apical * 0.3f) / 0.3f));
        } else {
            radius *= (1f / (float) branch.order) * 0.5f;
            float linear_radius = radius * distance_to_apical * 0.5f;
            radius = Mathf.Max(radius, linear_radius);
            radius = Mathf.Min(radius * Mathf.Sqrt((distance_to_apical * 0.3f) / 0.3f), radius);
        }
        return radius;
    }

    public float PalmInternodeLength(Branch branch, Bud bud) {
        if (branch.order == 1) {
            return internode_length;
        }
        return internode_length * 0.15f;
    }

    public void CreateStaghorn() {
        // Create various renderings of the tree and rotate them.
        List<GameObject> clones = new List<GameObject>();
        //float angle_range = 60f;
        Vector3[] euler_angles = new Vector3[] {
            new Vector3(49.72f, -22f, 51.3f),
            new Vector3(34f, 289.3f, 2.5f),
            new Vector3(9.2f, -15.3f, -41.8f),
        };

        float[] scales = new float[] {
            1.32f, 1.35f, 1.03f,
        };

        for (int i = 0; i < 3; i++) {
            GameObject clone = Instantiate(gameObject);
            //clone.transform.Rotate(
            //    Random.Range(-angle_range, angle_range),
            //    Random.Range(-angle_range, angle_range),
            //    Random.Range(-angle_range, angle_range), Space.World);
            //clone.transform.localScale *= Random.Range(0.5f, 1.5f);
            clone.transform.Rotate(euler_angles[i], Space.World);
            clone.transform.localScale *= scales[i];
            clones.Add(clone);

            // Remove some branch_list.
            if (Random.value < 0.6f) {
                TreeBuilder tree_builder = clone.GetComponent<TreeBuilder>();
                Destroy(tree_builder.branch_go_list[1]);
            }
        }

        foreach (GameObject clone in clones) {
            clone.transform.SetParent(transform);
        }
    }

    public void CreateElkhorn() {
        // Remove some branch_list.
        Destroy(branch_go_list[0]);
        foreach (GameObject go in branch_go_list) {
            if (Random.value < 0.1f) {
                Destroy(go);
            }
        }
    }

    public void RemoveBranchesOverOrder(int order_to_remove) {
        // Remove some branch_list.
        List<Branch> branch_to_remove = new List<Branch>();
        List<GameObject> branch_go_to_remove = new List<GameObject>();
        for (int i = 1; i < branch_list.Count; i++) {
            Branch branch = branch_list[i];
            GameObject branch_go = branch_go_list[i];
            // need to remove leaves and branches that are not in view according
            // to the perspective.
            if (branch.order >= order_to_remove) {
                branch_to_remove.Add(branch);
                branch_go_to_remove.Add(branch_go);
            }
        }

        for (int i = 0; i < branch_to_remove.Count; i++) {
            branch_list.Remove(branch_to_remove[i]);
            GameObject branch_go = branch_go_to_remove[i];
            branch_go_list.Remove(branch_go);
            branch_go.transform.SetParent(null);
            Destroy(branch_go);
        }
    }
}
