using UnityEngine;
using System.Collections.Generic;

public class RiverPoint {
    public Vector2 pos;
    public Vector2 dir;
    public float width;
    public RiverPoint(Vector2 pos, Vector2 dir, float width) {
        this.pos = pos;
        this.dir = dir;
        this.width = width;
    }
}

public class RiverSpawn : MonoBehaviour {
    public Island island;
    public List<RiverPoint> river_point_list = new List<RiverPoint>();
    public Vector2 start_pos;
    public Vector2 end_pos;
    public const float step_distance = 1f;
    public Material material;
    public Material map_material;
    public Material sand_material;
    public Material sand_map_material;
    public float animation_time = 0f;
    public bool activated = false;
    public List<GameObject> splash_go_list = new List<GameObject>();
    public List<GameObject> lily_pad_go_list = new List<GameObject>();
    public GameObject parent_go;
    public List<GameObject> sound_go_list = new List<GameObject>();
    public GameObject sound_parent_go;
    public Vector3 main_start_to_end_direction;

    public void Load(Island island) {
        #if MAC_MODE
            return;
        #endif
        this.island = island;

        parent_go = new GameObject("RiverSpawn");
        parent_go.transform.SetParent(transform);

        sound_parent_go = new GameObject("RiverSounds");
        sound_parent_go.transform.SetParent(parent_go.transform);

        LoadPoints();
    }

    public void Activate() {
        #if MAC_MODE
            return;
        #endif
        activated = true;

        ActivateMaterial();
        ActivateSandMaterial();

        ActivateSplashPs();
        ActivateLilyPads();
        ActivateSounds();
    }

    public void Deactivate() {
        activated = false;

        DeactivateSplashPs();
        DeactivateLilyPads();
        DeactivateSounds();
    }

    public void ActivateMaterial() {
        // Include the material in the beach ocean.
        if (material == null) {
            LoadMaterial();
            LoadMapMaterial();
        }
        GameObject beach_ocean_game_go = BeachOcean.Instance.active_island_game_go;
        beach_ocean_game_go.GetComponent<MeshRenderer>().materials = new Material[] {
            BeachOcean.Instance.active_game_material,
            material
        };
        GameObject beach_ocean_map_go = BeachOcean.Instance.active_island_map_go;
        beach_ocean_map_go.GetComponent<MeshRenderer>().materials = new Material[] {
            BeachOcean.Instance.active_game_material,
            map_material
        };
    }

    public void ActivateSandMaterial() {
        // Set BeachSand materials list to include sand_material
        if (sand_material == null) {
            LoadSandMaterial();
            LoadSandMapMaterial();
        }
        GameObject beach_sand_game_go = BeachSand.Instance.active_island_game_go;
        beach_sand_game_go.GetComponent<MeshRenderer>().materials = new Material[] {
            BeachSand.Instance.active_game_material,
            sand_material
        };
        GameObject beach_sand_map_go = BeachSand.Instance.active_island_map_go;
        beach_sand_map_go.GetComponent<MeshRenderer>().materials = new Material[] {
            BeachSand.Instance.active_game_material,
            sand_map_material
        };
    }

    public void LoadMaterial() {
        IslandTextures island_textures = IslandTextures.Instance;
        string shader_name = "Custom/River";
        Color color = new Color(0.65f, 0.87f, 1.0f);
        if (island.biome.name == "tundra" || island.biome.name == "taiga") {
            shader_name = "Custom/RiverTundra";
            color = new Color(1f, 0.98f, 0.87f);
        }
        material = new Material(Shader.Find(shader_name));
        material.SetColor("_Color", color);
        material.SetTexture("_HeightTex",
                            island_textures.GetRiverTexture(island));
        material.SetFloat("_IslandSize", island.size);
        material.SetVector("_WorldSize",
            new Vector2(KWorld.WIDTH, KWorld.LENGTH));
        material.SetVector("_IslandTopLeft",
            new Vector2(island.top_left.x + KWorld.WIDTH,
                island.top_left.y + KWorld.LENGTH));
        material.SetTexture("_WaterHTex",
                                        island_textures.water_h_t);
        material.SetTexture("_NoTileVarTex",
                                        island_textures.no_tile_var);
        material.SetTexture("_FoamTex", island_textures.bubbles);
        material.SetTexture("_WaterNormalTex", island_textures.water_normal);
        material.SetTexture("_CalmWaterNormalTex", island_textures.calm_water_normal);
        material.SetTexture("_WaterSparklesTex", island_textures.water_sparkles);
    }

    public void LoadMapMaterial() {
        IslandTextures island_textures = IslandTextures.Instance;
        string shader_name = "Custom/RiverMap";
        Color color = new Color(0.65f, 0.87f, 1.0f);
        if (island.biome.name == "tundra" || island.biome.name == "taiga") {
            shader_name = "Custom/RiverTundra";
            color = new Color(1f, 0.98f, 0.87f);
        }
        map_material = new Material(Shader.Find(shader_name));
        map_material.SetColor("_Color", color);
        map_material.SetTexture("_HeightTex",
                            island_textures.GetRiverTexture(island));
        map_material.SetFloat("_IslandSize", island.size);
        map_material.SetVector("_WorldSize",
            new Vector2(KWorld.WIDTH, KWorld.LENGTH));
        map_material.SetVector("_IslandTopLeft",
            new Vector2(island.top_left.x + KWorld.WIDTH,
                island.top_left.y + KWorld.LENGTH));
        map_material.SetTexture("_WaterHTex",
                                        island_textures.water_h_t);
        map_material.SetTexture("_NoTileVarTex",
                                        island_textures.no_tile_var);
        map_material.SetTexture("_FoamTex", island_textures.foam_2);
        map_material.SetTexture("_WaterNormalTex", island_textures.water_normal);
    }

    public void LoadSandMaterial() {
        IslandTextures island_textures = IslandTextures.Instance;
        string shader_name = "Custom/RiverSand";
        Color color = new Color(1f, 0.98f, 0.87f);
        Texture2D sand_texture = Resources.Load<Texture2D>("Textures/IslandTextures/pebbles");
        if (island.biome.name == "tundra" || island.biome.name == "taiga") {
            shader_name = "Custom/RiverSandTundra";
            color = new Color(1f, 0.98f, 0.87f);
        }
        Texture2D sand_texture2 = sand_texture;
        if (island.biome.name == "tundra" || island.biome.name == "taiga") {
            shader_name = "Custom/RiverSandTundra";
            color = new Color(1f, 0.98f, 0.87f);
        }
        sand_material = new Material(Shader.Find(shader_name));
        sand_material.SetColor("_Color", color);
        Texture2D height_texture = island_textures.GetRiverSandTexture(island);
        sand_material.SetTexture("_HeightTex", height_texture);
        sand_material.SetFloat("_IslandSize", island.size);
        sand_material.SetVector("_WorldSize",
            new Vector2(KWorld.WIDTH, KWorld.LENGTH));
        sand_material.SetVector("_IslandTopLeft",
            new Vector2(island.top_left.x + KWorld.WIDTH,
                island.top_left.y + KWorld.LENGTH));
        sand_material.SetTexture("_SandTex", sand_texture);
        sand_material.SetTexture("_SandTex2", sand_texture2);
        sand_material.SetTexture("_NoTileVarTex", island_textures.no_tile_var);
        sand_material.SetTexture("_NormalMap", island_textures.pebbles_normal);
        sand_material.renderQueue = 2001;
    }

    public void LoadSandMapMaterial() {
        IslandTextures island_textures = IslandTextures.Instance;
        string shader_name = "Custom/RiverSand";
        Color color = new Color(1f, 0.98f, 0.87f);
        Texture2D sand_texture = island_textures.sand_pebbles;
        if (island.biome.name == "tundra" || island.biome.name == "taiga") {
            shader_name = "Custom/RiverSandTundra";
            color = new Color(1f, 0.98f, 0.87f);
            sand_texture = island_textures.sand_pebbles;
        }
        Texture2D sand_texture2 = island_textures.pebbles;
        if (island.biome.name == "tundra" || island.biome.name == "taiga") {
            shader_name = "Custom/RiverSandTundra";
            color = new Color(1f, 0.98f, 0.87f);
            sand_texture2 = island_textures.pebbles;
        }
        sand_map_material = new Material(Shader.Find(shader_name));
        sand_map_material.SetColor("_Color", color);
        Texture2D height_texture = island_textures.GetRiverSandTexture(island);
        sand_map_material.SetTexture("_HeightTex", height_texture);
        sand_map_material.SetFloat("_IslandSize", island.size);
        sand_map_material.SetVector("_WorldSize",
            new Vector2(KWorld.WIDTH, KWorld.LENGTH));
        sand_map_material.SetVector("_IslandTopLeft",
            new Vector2(island.top_left.x + KWorld.WIDTH,
                island.top_left.y + KWorld.LENGTH));
        sand_map_material.SetTexture("_SandTex", sand_texture);
        sand_map_material.SetTexture("_SandTex2", sand_texture2);
        sand_map_material.SetTexture("_NoTileVarTex", island_textures.no_tile_var);
        sand_map_material.SetTexture("_NormalMap", island_textures.pebbles_normal);
        sand_map_material.SetFloat("_Multiplier", 2f);
        sand_map_material.renderQueue = 2001;
    }

    public void LoadPoints() {
        float left_x = island.top_left.x;
        float size = island.size;
        start_pos = new Vector2(
            Random.Range(left_x + size * 0.25f, left_x + size * 0.75f),
            island.top_left.y);

        end_pos = new Vector2(
            Random.Range(left_x + size * 0.25f, left_x + size * 0.75f),
            island.bot_left.y);

        float total_distance = Vector2.Distance(start_pos, end_pos);
        Vector2 start_to_end_direction = (end_pos - start_pos).normalized;
        main_start_to_end_direction = start_to_end_direction;
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        Vector2 previous_pos = start_pos;
        float step_count = total_distance / step_distance;
        for (float i = 0; i < step_count; i += step_distance) {
            Vector2 pos = start_pos + start_to_end_direction * i;

            pos += Vector2.one * noise.GetNoise(pos.x * 10f, pos.y * 5f) * 10f;

            // This avoids weird bugs with the river point directions in the shaders.
            if (Vector2.Distance(pos, previous_pos) < 1f) {
                continue;
            }
            Vector2Int index = island.WorldPosToKey(pos);
            float height = IslandUtils.GetHeightNoRiver(island, index.x, index.y);
            if (height < KIsland.SAND_OUTER_RING_HEIGHT - 0.075f) {
                continue;
            }
            Vector2 dir = start_to_end_direction;
            //Vector2 dir = (pos - previous_pos).normalized;
            //dir = (start_to_end_direction + (dir * 0.25f)).normalized;
            //if (dir == Vector2.zero) {
            //    dir = (end_pos - start_pos).normalized;
            //}
            float width_noise = Mathf.Max(0.75f, noise.GetNoise(pos.x * 3f, pos.y * 3f) * 3f);
            float width = width_noise;
            RiverPoint point = new RiverPoint(pos, dir, width);
            river_point_list.Add(point);
            previous_pos = pos;
            // DebugDraw.Instance.AddPlane("river", new Vector3(point.x, point.y, -3f), Color.blue, 1f);
            if (i % 2 == 0) {
                LoadSoundAt(point);
            }
        }
        //LoadTributary();
    }

    public void LoadTributary() {
        int t_start_index = Mathf.FloorToInt(
            Random.Range(river_point_list.Count * 0.4f, river_point_list.Count * 0.6f));
        RiverPoint point = river_point_list[t_start_index];
        Vector2 t_start_pos = point.pos;
        Vector2 t_end_pos = new Vector2(
            island.top_left.x + island.size,
            island.bot_left.y + island.size * Random.Range(0.2f, 0.3f));

        float total_distance = Vector2.Distance(t_start_pos, t_end_pos);
        Vector2 start_to_end_direction = (t_end_pos - t_start_pos).normalized;
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        Vector2 previous_pos = t_start_pos;
        for (float i = 0; i < total_distance / step_distance; i += step_distance) {
            Vector2 pos = t_start_pos + start_to_end_direction * i;
            pos += Vector2.one * noise.GetNoise(pos.x * 2f, pos.y * 2f) * 7;
            // This avoids weird bugs with the river point directions in the shaders.
            if (Vector2.Distance(pos, previous_pos) < 1f) {
                continue;
            }
            Vector2Int index = island.WorldPosToKey(pos);
            float height = IslandUtils.GetHeightNoRiver(island, index.x, index.y);
            if (height < KIsland.SAND_OUTER_RING_HEIGHT - 0.075f) {
                continue;
            }
            Vector2 dir = start_to_end_direction;//(pos - previous_pos).normalized;
            //if (dir == Vector2.zero) {
            //    dir = (t_end_pos - t_start_pos).normalized;
            //}
            float width_noise = noise.GetNoise(pos.x * 2f, pos.y * 2f) * 4f;
            float width = 1f + width_noise;
            point = new RiverPoint(pos, dir, width);
            river_point_list.Add(point);
            previous_pos = pos;
            // DebugDraw.Instance.AddPlane("river", new Vector3(point.x, point.y, -3f), Color.blue, 1f);

            if (i % 3 == 0) {
                LoadSoundAt(point);
            }
        }
    }

    public RiverPoint GetClosestRiverPoint(Vector2 coord) {
        coord = new Vector2(
            island.top_left.x + coord.x,
            island.top_left.y - coord.y);
        float dist_to_river = island.size;
        RiverPoint closest_river_point = river_point_list[0];
        foreach (RiverPoint river_point in river_point_list) {
            Vector2 pos = river_point.pos;
            float dist = Vector2.Distance(coord, pos);
            // test out adding the width into consideration of distance.
            dist -= river_point.width;
            if (dist < dist_to_river) {
                dist_to_river = dist;
                closest_river_point = river_point;
            }
        }
        return closest_river_point;
    }

    public Vector2 GetRiverDirection(Vector2 coord) {
        coord = new Vector2(
            island.top_left.x + coord.x,
            island.top_left.y - coord.y);
        float dist_to_river = 7f;
        List<Vector2> direction_list = new List<Vector2>();
        foreach (RiverPoint river_point in river_point_list) {
            Vector2 pos = river_point.pos;
            float dist = Vector2.Distance(coord, pos);
            if (dist < dist_to_river) {
                direction_list.Add(river_point.dir * (7f - dist));
            }
        }
        Vector2 sum_dir = Vector2.zero;
        foreach (Vector2 dir in direction_list) {
            sum_dir += dir;
        }
        sum_dir.Normalize();
        return sum_dir;
    }

    public void LoadSplashPs() {
        foreach (KeyValuePair<Vector2, IslandTile> kvp in island.GetAllWaterTiles()) {
            IslandTile tile = kvp.Value;
            Vector2 coord = kvp.Key;
            float tile_height = tile.height;
            // Get height.
            bool spawn_chance = Random.value < 0.25f;
            if (tile.is_river && spawn_chance && tile_height > KIsland.INNER_WATER_RING_HEIGHT &&
                tile_height < KIsland.SANDY_WATER_HEIGHT) {
                GameObject splash_go = Instantiate(Resources.Load<GameObject>("Prefabs/PFx/RiverSplash"));
                splash_go.transform.parent = parent_go.transform;
                splash_go.transform.position = new Vector3(coord.x, coord.y, tile.tile_height);
                splash_go_list.Add(splash_go);
                splash_go.SetActive(false);
            }
        }
    }

    public void ActivateSplashPs() {
        if (splash_go_list.Count == 0) {
            LoadSplashPs();
        }
        foreach (GameObject splash_go in splash_go_list) {
            splash_go.SetActive(true);
            splash_go.GetComponent<ParticleSystem>().Play();
        }
    }

    public void DeactivateSplashPs() {
        foreach (GameObject splash_go in splash_go_list) {
            splash_go.GetComponent<ParticleSystem>().Stop();
            splash_go.SetActive(false);
        }
    }

    public void LoadLilyPads() {
        foreach (KeyValuePair<Vector2, IslandTile> kvp in island.GetAllWaterTiles()) {
            IslandTile tile = kvp.Value;
            Vector2 coord = kvp.Key;
            float tile_height = tile.height;
            float distance_from_center = Vector2.Distance(coord, island.center);
            float noise = NoisePCG.SeedNoise(69, coord.x, coord.y, 3f);
            bool spawn_chance = Random.value < 0.25f && noise > 0.6f;
            if (tile.is_river && spawn_chance && tile_height > KIsland.OUTER_WATER_RING_HEIGHT &&
                tile_height < KIsland.SANDY_WATER_HEIGHT && distance_from_center < island.size * 0.3f) {

                int lily_count = Random.Range(1, 3);
                for (int i = 0; i < lily_count; i++) {
                    Vector2 spawn_coord = new Vector2(coord.x + Random.Range(-0.5f, 0.5f), coord.y + Random.Range(-0.5f, 0.5f));
                    GameObject go = Instantiate(Resources.Load<GameObject>("Prefabs/LilyPad"));
                    go.transform.parent = parent_go.transform;
                    go.transform.position = new Vector3(spawn_coord.x, spawn_coord.y, tile.tile_height);
                    go.transform.localScale = go.transform.localScale * Random.Range(0.35f, 0.8f);
                    go.transform.localEulerAngles = new Vector3(0f, 0f, Random.Range(0f, 360f));
                    SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                    sr.color = new Color(Random.Range(0.7f, 1f), Random.Range(0.7f, 1f), Random.Range(0.7f, 1f), 1f);
                    float sin_offset = Random.Range(1f, 2f);
                    sr.material.SetFloat("_SinOffset", sin_offset);
                    foreach (Transform child in go.transform) {
                        SpriteRenderer child_sr = child.gameObject.GetComponent<SpriteRenderer>();
                        if (child_sr != null) {
                            child_sr.material.SetFloat("_SinOffset", sin_offset);
                        }
                    }
                    lily_pad_go_list.Add(go);
                    go.SetActive(false);
                }
            }
        }
    }

    public void ActivateLilyPads() {
        if (lily_pad_go_list.Count == 0) {
            LoadLilyPads();
        }
        foreach (GameObject go in lily_pad_go_list) {
            go.SetActive(true);
            go.GetComponentInChildren<ParticleSystem>().Play();
        }
    }

    // TODO fix.
    public void DeactivateLilyPads() {
        foreach (GameObject go in lily_pad_go_list) {
            go.SetActive(false);
            go.GetComponentInChildren<ParticleSystem>().Stop();
        }
    }

    public void LoadSoundAt(RiverPoint river_point) {
        string sound_name = (Random.value < 0.3f) ? "River" : "RiverSoft";
        GameObject go = SoundManager.Instance.Play3DLoopAtPoint(sound_name, river_point.pos);
        go.transform.SetParent(sound_parent_go.transform);
        sound_go_list.Add(go);
    }


    public void ActivateSounds() {
        foreach (GameObject go in sound_go_list) {
            go.SetActive(true);
        }
    }

    public void DeactivateSounds() {
        foreach (GameObject go in sound_go_list) {
            go.SetActive(false);
        }
    }
}
