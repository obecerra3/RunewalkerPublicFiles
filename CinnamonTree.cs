using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class CinnamonTree : TreePlant {
    public static GameObject stage_0_go;
    public static GameObject leaf_go;

    public static GameObject CreateLeafGo() {
        if (leaf_go != null) {
            return leaf_go;
        }
        leaf_go = new GameObject("CinnamonLeaf");
        leaf_go.transform.position = Vector3.zero;
        SpriteRenderer sprite_renderer = leaf_go.AddComponent<SpriteRenderer>();
        sprite_renderer.sprite = GameSprites.Instance.GetSpriteByName("Textures/IslandTextures/cinnamon_leaf", 10, 14);
        leaf_go.transform.localScale = new Vector3(0.007f, 0.007f, 0.007f);
        sprite_renderer.material = Resources.Load<Material>("Materials/Tree/TreeLeafMaterial");
        sprite_renderer.material.SetInt("_Pixels", 15);
        sprite_renderer.material.renderQueue = 3003;
        sprite_renderer.sortingLayerName = "Objects2";
        leaf_go.SetActive(false);
        return leaf_go;
    }

    public static void Build() {
        Random.State initial_state = Random.state;
        //10, 18, 19, 21, 26 okay
        Random.InitState(10);
        // "CinnamonTree"
        GameObject tree_go = new GameObject();
        TreePlant tree = PlantSpawn.Instance.InitTree("cinnamon_tree", tree_go);
        tree.item_color = new Color32(181, 128, 60, 255);
        tree.health = 5;
        tree.spawn_on_sand = false;
        tree.spawn_order = 2;
        tree.cardinal_neighbors = false;
        tree.leaf_pollen_chance = 0.025f;
        // Stage 0
        PlantStage ps = new PlantStage("Cinnammon Tree", true, BuildStage0(), new float[] { 1.0f });
        ps.has_soil = true;
        float stage_0_freq = 0.8f;
        float stage_0_spawn_chance = 0.25f;
        ps.InitNoise("caribbean", new int[] { 888 }, new float[] { stage_0_freq },
            new float[] { 0.8f }, new float[] { stage_0_spawn_chance });
        ps.InitNoise("pacific", new int[] { 888 }, new float[] { stage_0_freq },
            new float[] { 0.8f }, new float[] { stage_0_spawn_chance });
        ps.InitCapsuleCollider(false, Vector3.zero, 2, 0.5f, 0.2f);
        ps.InitCapsuleCollider(true, Vector3.zero, 2, 0.5f, 0.25f);
        ps.InitSpawnDistance(5f);

        tree.AddStage(0, ps);

        // Obj Pool
        ObjPool.Instance.Add(tree_go, 5);

        Random.state = initial_state;
    }

    public static List<GameObject> BuildStage0() {
        // Initialize the material.
        Material branch_material = Resources.Load<Material>(
            "Materials/Tree/CinnamonTreeMaterial");
        branch_material.renderQueue = 3002;

        // Build a child tree object for the staghorn.
        GameObject tree_go = new GameObject("CinnamonTreeStage0");
        TreeBuilder tree_builder = tree_go.AddComponent<TreeBuilder>();

        // Set the procedural parameters for the tree.
        tree_builder.plagiotropic = 0.2f;
        tree_builder.orthotropic = 0.7f;
        tree_builder.internode_radius = 0.1f;
        tree_builder.internode_length = 0.8f;
        tree_builder.internodes_per_growth = 1;
        tree_builder.ramification_type = "diffuse";
        tree_builder.max_order = 3;
        tree_builder.min_buds_per_node = 1;
        tree_builder.max_buds_per_node = 3;
        tree_builder.pause_prob = 0.1f;
        tree_builder.death_prob = 0.1f;
        tree_builder.ramification_prob = 0.1f;
        tree_builder.growth_cycles = 25;
        tree_builder.min_dimension_trunk = 4;
        tree_builder.max_dimension_trunk = 5;
        tree_builder.min_dimension = 3;
        tree_builder.max_dimension = 5;
        tree_builder.leaf_density = 3;

        tree_builder.conical = 0.2f;
        tree_builder.wiggle = 0.3f;
        tree_builder.axillary_dimension_min = 2;
        tree_builder.axillary_dimension_max = 6;

        // Create the tree shadow.
        GameObject shadow_go = Instantiate(Resources.Load<GameObject>("Prefabs/TreeShadow"));
        shadow_go.transform.SetParent(tree_go.transform);

        // Instantiate first branch.
        float p = tree_builder.plagiotropic;
        Vector3 tangent = new Vector3(0.1f * p, 1, 0.1f * p).normalized;
        Branch branch = new Branch(tree_builder, Vector3.zero, tangent, 1);
        branch.bud_list[0].age = 1;
        tree_builder.branch_list.Add(branch);

        // Set the parent and RadiusPerGrowth func.
        tree_builder.parent_object = tree_go;
        tree_builder.RadiusPerGrowth = tree_builder.DefaultRadiusPerGrowth;
        tree_builder.InternodeLengthPerGrowth = tree_builder.DefaultInternodeLength;

        // Create the rendering of the tree.
        tree_builder.SetLeaf(CreateLeafGo());
        tree_builder.CreateBranches();
        tree_builder.SetBranchMaterial(branch_material);
        tree_builder.RenderBranches();
        tree_go.transform.eulerAngles = new Vector3(-80, 5, 0);
        tree_go.transform.localScale = new Vector3(6, 6f, 6);

        stage_0_go = tree_go;
        return new List<GameObject> { tree_go };
    }
}
