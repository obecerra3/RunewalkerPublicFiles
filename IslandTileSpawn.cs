using System.Collections.Generic;
using UnityEngine;

public static class IslandTileSpawn {
    public static int cull_tile_count = 0;

    public static void AddLandTile(Island island, HashSet<Vector2> cull_coords, float height, Vector2 tile_coord) {
        IslandTile island_tile = new IslandTile {
            height = height
        };

        IslandUtils.IslandHeightData island_height_data =
            IslandUtils.GetIslandHeightData(height);

        island_tile.level = island_height_data.level;
        island_tile.tilemap_index = island_height_data.tilemap_index;
        island_tile.tile_height = island_height_data.tile_height;
        island_tile.layer = island_height_data.layer;
        island_tile.coord = tile_coord;

        island_tile.island = island;

        island_tile.is_land = true;
        island.land_tiles[tile_coord] = island_tile;

        PopulateIslandTileMask(island, cull_coords, tile_coord, true);

        // Update the tilemasks of this tile and its moore neighborhood.
        Vector2[] moore_array = new Vector2[] {
                Vector2.up, Vector2.right, Vector2.down, Vector2.left,
                Vector2.up + Vector2.right, Vector2.down + Vector2.right,
                Vector2.down + Vector2.left, Vector2.up + Vector2.left
        };

        foreach (Vector2 moore_coord in moore_array) {
            Vector2 coord = tile_coord + moore_coord;
            if (island.land_tiles.ContainsKey(coord)) {
                PopulateIslandTileMask(island, cull_coords, coord, true);
            }
        }
    }

    // Initialize Island.land_tiles with height, level, tilemap_index, and
    // tile_height.
    public static void Load(Island island) {
        // 2D iteration across Island area to fill in Island tiles.
        // Populate IslandTile : height, level, tilemap_index, and tile_height.
        for (int y = island.size; y >= 0; y--) {
            for (int x = 0; x < island.size; x++) {
                // Get Height.
                float height = IslandUtils.GetHeight(island, x, y);
                float height_no_river = IslandUtils.GetHeightNoRiver(island, x, y);
                (float, RiverPoint) river_data = 
                    IslandUtils.GetRiverPointAndHeight(island, x, y, 10f);
                float river_height = river_data.Item1;
                RiverPoint river_point = river_data.Item2;

                // Check for ocean.
                if (height <= KIsland.OCEAN_LEVEL) {
                    continue;
                }

                // Key in the Island.land_tiles Dictionary is the real world
                // coordinate of the IslandTile.
                Vector2 tile_coord = island.top_left + new Vector2(x, -y);

                // Initialize the IslandTile.
                IslandTile island_tile = new IslandTile {
                    height = height
                };

                IslandUtils.IslandHeightData island_height_data =
                    IslandUtils.GetIslandHeightData(height);

                island_tile.level = island_height_data.level;
                island_tile.tilemap_index = island_height_data.tilemap_index;
                island_tile.tile_height = island_height_data.tile_height;
                island_tile.layer = island_height_data.layer;
                island_tile.coord = tile_coord;

                island_tile.island = island;

                if (height < KIsland.SAND_OUTER_RING_HEIGHT) {
                    // Check if the tile is a river tile or an ocean tile.
                    if (river_height < KIsland.SAND_OUTER_RING_HEIGHT) {
                        island_tile.render_sand_under = true;
                        island_tile.is_water = true;
                        island_tile.is_river = true;
                        island.river_tiles[tile_coord] = island_tile;
                        island_tile.river_point = river_point;
                    } else {
                        island_tile.render_sand_under = true;
                        island_tile.is_water = true;
                        island_tile.is_ocean = true;
                        island.ocean_tiles[tile_coord] = island_tile;
                    }
                } else if (height <= KIsland.SAND_HEIGHT) {
                    island_tile.is_sand = true;
                    island_tile.is_flat = true;
                    island.sand_tiles[tile_coord] = island_tile;
                    if (river_point != null) {
                        island_tile.is_river_sand = true;
                    }
                } else {
                    island_tile.is_land = true;
                    island.land_tiles[tile_coord] = island_tile;
                }
            }
        }

        PopulateIslandTileMasks(island);


        // Add sand tile underneath each land tile that is not flat.
        foreach (KeyValuePair<Vector2, IslandTile> kvp in island.land_tiles) {
            Vector2 tile_coord = kvp.Key;
            IslandTile island_tile = kvp.Value;
            if (!island_tile.is_flat) {
                island_tile.render_sand_under = true;
                island_tile.AddSandTileUnder();
                island.sand_tiles[tile_coord] = island_tile.tile_under;
            }
        }

        // Debug.Log("Cull Count: " + cull_tile_count);
    }

    // Fill mask, mask2, is_ridge, is_flat, and cull for every tile in
    // island.land_tiles. 
    public static void PopulateIslandTileMasks(Island island) {
        // List of coords that represent illegal mask1 or mask2 values for
        // terrain, these illegal values create graphical artifacts since
        // ScriptableIslandTile does not know how to represent them. 
        var cull_coords = new HashSet<Vector2>();

        // Populate initial values for the mask, mask2, is_ridge, is_flat, and
        // tiles to cull. 
        foreach (var coord in island.land_tiles.Keys) {
            PopulateIslandTileMask(island, cull_coords, coord);
        }

        PadCoords(island, cull_coords);

        // Leads to super boring results.
        //CullCoords(island, cull_coords);

        // Populate initial values for the mask, mask2, is_ridge, is_flat, and
        // tiles to cull. 
        foreach (var coord in island.land_tiles.Keys)
            PopulateIslandTileMask(island, cull_coords, coord);
    }

    public static void PadCoords(Island island, HashSet<Vector2> cull_coords) {
        // Old way
        //PadCoordsHelper(island, cull_coords);

        // New way leads to weird results and bridges forming off of the island.
        // could be a way to create connected paths.

        var cull_coords_to_add = new HashSet<Vector2>();
        int try_count = 0;
        while (cull_coords.Count != 0) {
            PadCoordsHelper2(island, cull_coords, cull_coords_to_add);

            cull_coords.Clear();
            foreach (Vector2 cull_coord in cull_coords_to_add) {
                cull_coords.Add(cull_coord);
            }
            try_count++;
            if (try_count > 1) {
                break;
                Debug.LogError("Could not cull or pad them all sire");
            }
        }
    }

    private static void PadCoordsHelper(Island island, HashSet<Vector2> cull_coords) {
        foreach (var cull_coord in cull_coords) {
            IslandTile cull_tile = island.land_tiles[cull_coord];
            // for every cull coord we fill the moore neighborhood with a tile
            // of the same height if there is no tile there.
            Vector2[] moore_array = new Vector2[] {
                    Vector2.up, Vector2.right, Vector2.down, Vector2.left,
                    Vector2.up + Vector2.right, Vector2.down + Vector2.right,
                    Vector2.down + Vector2.left, Vector2.up + Vector2.left
                };

            foreach (Vector2 moore_coord in moore_array) {
                Vector2 coord = cull_coord + moore_coord;
                if (!island.land_tiles.ContainsKey(coord)) {
                    AddLandTile(island, new HashSet<Vector2>(), cull_tile.height, coord);
                    //DebugDraw.Instance.AddPlane("moore", new Vector3Int((int) coord.x, (int) coord.y, (int) cull_tile.tile_height - 1), Color.blue);
                }
            }
            //DebugDraw.Instance.AddPlane(
            //    new Vector3Int((int) cull_coord.x, (int) cull_coord.y, (int) cull_tile.tile_height - 1));
        }
    }

    private static void PadCoordsHelper2(Island island, HashSet<Vector2> cull_coords, HashSet<Vector2> cull_coords_to_add) {
        foreach (var cull_coord in cull_coords) {
            IslandTile cull_tile = island.land_tiles[cull_coord];
            // for every cull coord we fill the moore neighborhood with a tile
            // of the same height if there is no tile there.
            Vector2[] moore_array = new Vector2[] {
                    Vector2.up, Vector2.right, Vector2.down, Vector2.left,
                    Vector2.up + Vector2.right, Vector2.down + Vector2.right,
                    Vector2.down + Vector2.left, Vector2.up + Vector2.left
                };

            foreach (Vector2 moore_coord in moore_array) {
                Vector2 coord = cull_coord + moore_coord;
                if (!island.land_tiles.ContainsKey(coord)) {
                    AddLandTile(island, cull_coords_to_add, cull_tile.height, coord);
                    //DebugDraw.Instance.AddPlane("moore", new Vector3Int((int) coord.x, (int) coord.y, (int) cull_tile.tile_height - 1), Color.blue);
                }
            }
            //DebugDraw.Instance.AddPlane(
            //    new Vector3Int((int) cull_coord.x, (int) cull_coord.y, (int) cull_tile.tile_height - 1));
        }
    }

    public static void CullCoords(Island island, HashSet<Vector2> cull_coords) {
        var cull_coords_to_remove = new HashSet<Vector2>();
        var cull_coords_to_add = new HashSet<Vector2>();
        do {
            // Demote tile in island.land_tiles. This needs to be done before
            // the next step, or else the newly calculated mask values will be
            // for the same configuration of tiles.
            cull_coords_to_remove.Clear();
            cull_coords_to_add.Clear();

            foreach (var cull_coord in cull_coords) {
                var tile = island.land_tiles[cull_coord];
                if (tile.IsDemoteToSand()) {
                    if (!island.sand_tiles.ContainsKey(cull_coord)) {
                        island.sand_tiles[cull_coord] = tile.DemoteCopy();
                    }
                    island.land_tiles.Remove(cull_coord);
                    continue;
                } else {
                    tile.Demote();
                }
                PopulateIslandTileMask(island, cull_coords_to_add, cull_coord);
            }

            // Check Moore neighborhood of cull_coords and put current
            // cull_coord in cull_coords_to_remove.
            foreach (var cull_coord in cull_coords) {
                // Cardinal Neighbors. U, R, D, L
                PopulateIslandTileMask(island, cull_coords_to_add,
                    cull_coord + Vector2.up);
                PopulateIslandTileMask(island, cull_coords_to_add,
                    cull_coord + Vector2.right);
                PopulateIslandTileMask(island, cull_coords_to_add,
                    cull_coord + Vector2.down);
                PopulateIslandTileMask(island, cull_coords_to_add,
                    cull_coord + Vector2.left);
                // Ordinal Neighbors. UR, RD, DL, UL
                PopulateIslandTileMask(island, cull_coords_to_add,
                    cull_coord + Vector2.up + Vector2.right);
                PopulateIslandTileMask(island, cull_coords_to_add,
                    cull_coord + Vector2.right + Vector2.down);
                PopulateIslandTileMask(island, cull_coords_to_add,
                    cull_coord + Vector2.down + Vector2.left);
                PopulateIslandTileMask(island, cull_coords_to_add,
                    cull_coord + Vector2.up + Vector2.left);

                // Add current cull_coord to cull_coords_to_remove.
                if (!cull_coords_to_add.Contains(cull_coord))
                    cull_coords_to_remove.Add(cull_coord);
            }

            // Add cull_coords_to_add to cull_coords.
            foreach (var cull_coord in cull_coords_to_add)
                cull_coords.Add(cull_coord);

            // Remove cull_coords_to_remove from cull_coords.
            foreach (var cull_coord in cull_coords_to_remove)
                cull_coords.Remove(cull_coord);

        // Once cull_coords is empty we can assume island.land_tiles has been culled.
        } while (cull_coords.Count != 0);
    }

    // Populate mask, mask2, is_ridge, is_flat, and cull for an individual tile.
    public static void PopulateIslandTileMask(Island island,
        HashSet<Vector2> cull_coords, Vector2 coord, bool picky_cull = false) {
        // If this coord does not have tile, return.
        if (!island.land_tiles.TryGetValue(coord, out var tile))
            return;

        tile.is_ridge = false;
        tile.is_flat = false;
        tile.can_host_spawn = false;

        // Get Cardinal Neighbors mask[] values.
        // UP, RIGHT, DOWN, LEFT

        tile.mask = new int[KWorld.LEVELS];

        var up_coord = coord + Vector2.up;
        if (island.land_tiles.TryGetValue(up_coord, out var up_tile))
            for (var i = 0; i <= tile.tilemap_index; i++)
                tile.mask[i] += (up_tile.tilemap_index >= i) ? 1 : 0;

        var right_coord = coord + Vector2.right;
        if (island.land_tiles.TryGetValue(right_coord, out var right_tile))
            for (var i = 0; i <= tile.tilemap_index; i++)
                tile.mask[i] += (right_tile.tilemap_index >= i) ? 2 : 0;

        var down_coord = coord + Vector2.down;
        if (island.land_tiles.TryGetValue(down_coord, out var down_tile))
            for (var i = 0; i <= tile.tilemap_index; i++)
                tile.mask[i] += (down_tile.tilemap_index >= i) ? 4 : 0;

        var left_coord = coord + Vector2.left;
        if (island.land_tiles.TryGetValue(left_coord, out var left_tile))
            for (var i = 0; i <= tile.tilemap_index; i++)
                tile.mask[i] += (left_tile.tilemap_index >= i) ? 8 : 0;

        // Get Ordinal Neighbors mask[] values.
        // UP_RIGHT, DOWN_RIGHT, DOWN_LEFT, UP_LEFT

        tile.mask2 = new int[KWorld.LEVELS];

        var up_right_coord = coord + Vector2.up + Vector2.right;
        if (island.land_tiles.TryGetValue(up_right_coord, out var up_right_tile))
            for (var i = 0; i <= tile.tilemap_index; i++)
                tile.mask2[i] += (up_right_tile.tilemap_index >= i) ? 1 : 0;

        var right_down_coord = coord + Vector2.right + Vector2.down;
        if (island.land_tiles.TryGetValue(right_down_coord, out var right_down_tile))
            for (var i = 0; i <= tile.tilemap_index; i++)
                tile.mask2[i] += (right_down_tile.tilemap_index >= i) ? 2 : 0;

        var down_left_coord = coord + Vector2.down + Vector2.left;
        if (island.land_tiles.TryGetValue(down_left_coord, out var down_left_tile))
            for (var i = 0; i <= tile.tilemap_index; i++)
                tile.mask2[i] += (down_left_tile.tilemap_index >= i) ? 4 : 0;

        var up_left_coord = coord + Vector2.up + Vector2.left;
        if (island.land_tiles.TryGetValue(up_left_coord, out var up_left_tile))
            for (var i = 0; i <= tile.tilemap_index; i++)
                tile.mask2[i] += (up_left_tile.tilemap_index >= i) ? 8 : 0;

        // Set is_ridge, is_flat, and cull using new mask data.

        var mask = (KIsland.MaskValue) tile.mask[tile.tilemap_index];
        var mask2 = (KIsland.Mask2Value) tile.mask2[tile.tilemap_index];

        // Cull check.
        if (picky_cull) {
            if (CheckCullTilePicky(island, coord, mask, mask2, cull_coords)) {
                cull_tile_count++;
                return;
            } else if (cull_coords.Contains(coord)) {
                cull_coords.Remove(coord);
            }
        } else if (CheckCullTile(island, coord, mask, mask2, cull_coords)) {
            cull_tile_count++;
            return;
        } else if (cull_coords.Contains(coord)) {
            cull_coords.Remove(coord);
        }

        // URL then it is a ridge.
        if (mask == KIsland.MaskValue.URL) {
            tile.is_ridge = true;
            return;
        }

        // Without ALL Cardinal neighbors the tile both cannot be a ridge
        // and cannot be flat.
        if (mask != KIsland.MaskValue.URDL)
            return;

        // RD or DL empty then it is a ridge.
        if (mask2 == KIsland.Mask2Value.UR_DL_UL
            || mask2 == KIsland.Mask2Value.UR_RD_UL) {
            tile.is_ridge = true;
            return;
            // Case for a flat tile has all ordinal neighbors.  
        } else if (mask2 == KIsland.Mask2Value.UR_RD_DL_UL) {
            tile.is_flat = true;
            tile.can_host_spawn = true;
            return;
        }
    }

    // Use the mask and mask2 values for a tile to determine if the tile should
    // be culled.
    // CASE 1
    // 0110
    // 1111
    // This case leads to adjacent corner edges instead of a horizontal
    // top flat edge.
    // CASE 2
    // 1111
    // 0110
    // This case leads to adjacent straight vertical edges instead of
    // a flat bottom horizontal edge.
    // CASE 3
    // 0001
    // 0011
    // 0011
    // 0001
    // CASE 4
    // 1000
    // 1100
    // 1100
    // 1000

    public static bool CheckCullTile(Island island, Vector2 coord,
        KIsland.MaskValue mask, KIsland.Mask2Value mask2,
        HashSet<Vector2> cull_coords) {
        switch (mask) {
            // The case for a single line of tiles in any direction, or an
            // isolate tile.
            case (KIsland.MaskValue.U):
            case (KIsland.MaskValue.R):
            case (KIsland.MaskValue.D):
            case (KIsland.MaskValue.L):
            case (KIsland.MaskValue.NONE):
            case (KIsland.MaskValue.RL):
            case (KIsland.MaskValue.UD):
                cull_coords.Add(coord);
                return true;

            case (KIsland.MaskValue.DL): {
                    var left_coord = coord + Vector2.left;
                    // CASE 1
                    if (island.land_tiles.TryGetValue(left_coord, out var tile)
                        && tile.GetMask() == KIsland.MaskValue.RD) {
                        cull_coords.Add(coord);
                        cull_coords.Add(left_coord);
                        return true;
                    }
                    var down_coord = coord + Vector2.down;
                    // CASE 4
                    if (island.land_tiles.TryGetValue(down_coord, out tile)) {
                        if (tile.GetMask() == KIsland.MaskValue.NONE)
                            PopulateIslandTileMask(
                                island, cull_coords, down_coord);

                        if (tile.GetMask() == KIsland.MaskValue.UL) {
                            cull_coords.Add(coord);
                            cull_coords.Add(down_coord);
                            return true;
                        }
                    }
                    break;
                }
            // Need to check both left and right since a cull update may miss
            // the left corner edge check.
            case (KIsland.MaskValue.RD): {
                    var right_coord = coord + Vector2.right;
                    // CASE 1
                    if (island.land_tiles.TryGetValue(right_coord, out var tile)) {
                        // tile.GetMask() CANNOT be NONE here if the first tile
                        // has a mask of RD, this means right_coord is
                        // uninitialized.
                        if (tile.GetMask() == KIsland.MaskValue.NONE)
                            PopulateIslandTileMask(
                                island, cull_coords, right_coord);

                        if (tile.GetMask() == KIsland.MaskValue.DL) {
                            cull_coords.Add(coord);
                            cull_coords.Add(right_coord);
                            return true;
                        }
                    }
                    var down_coord = coord + Vector2.down;
                    // CASE 3
                    if (island.land_tiles.TryGetValue(down_coord, out tile)) {
                        if (tile.GetMask() == KIsland.MaskValue.NONE)
                            PopulateIslandTileMask(
                                island, cull_coords, down_coord);

                        if (tile.GetMask() == KIsland.MaskValue.UR) {
                            cull_coords.Add(coord);
                            cull_coords.Add(down_coord);
                            return true;
                        }
                    }
                    break;
                }

            // This case leads to adjacent edges like an icicle instead of a
            // flat bottom edge.
            case (KIsland.MaskValue.UL): {
                    var left_coord = coord + Vector2.left;
                    // CASE 2
                    if (island.land_tiles.TryGetValue(left_coord, out var tile)
                        && tile.GetMask() == KIsland.MaskValue.UR) {
                        cull_coords.Add(coord);
                        cull_coords.Add(left_coord);
                        return true;
                    }
                    var up_coord = coord + Vector2.up;
                    // CASE 4
                    if (island.land_tiles.TryGetValue(up_coord, out tile)) {
                        if (tile.GetMask() == KIsland.MaskValue.DL) {
                            cull_coords.Add(coord);
                            cull_coords.Add(up_coord);
                            return true;
                        }
                    }
                    break;
                }
            case (KIsland.MaskValue.UR): {
                    var right_coord = coord + Vector2.right;
                    // CASE 2
                    if (island.land_tiles.TryGetValue(right_coord, out var tile)) {
                        if (tile.GetMask() == KIsland.MaskValue.NONE)
                            PopulateIslandTileMask(
                                island, cull_coords, right_coord);

                        if (tile.GetMask() == KIsland.MaskValue.UL) {
                            cull_coords.Add(coord);
                            cull_coords.Add(right_coord);
                            return true;
                        }
                    }
                    var up_coord = coord + Vector2.up;
                    // CASE 3
                    if (island.land_tiles.TryGetValue(up_coord, out tile)) {
                        if (tile.GetMask() == KIsland.MaskValue.RD) {
                            cull_coords.Add(coord);
                            cull_coords.Add(up_coord);
                            return true;
                        }
                    }
                    // New case that looks like this
                    // 110
                    // 0x1
                    // 001
                    if (mask2 == KIsland.Mask2Value.RD_UL) {
                        cull_coords.Add(coord);
                    }
                    break;
                }
            case (KIsland.MaskValue.RDL): {
                // New case that looks like this
                // 001
                // 1X1
                // 110
                if (mask2 == KIsland.Mask2Value.UR_DL) {
                    cull_coords.Add(coord);
                    return true;
                }
                // New case that looks like this
                // 100
                // 1X1
                // 011
                if (mask2 == KIsland.Mask2Value.RD_UL) {
                    cull_coords.Add(coord);
                    return true;
                }
                break;
            }
            // TODO NOT WORKING
            case (KIsland.MaskValue.URDL): {
                // New case that looks like this
                // 011
                // 1X1
                // 110
                if (mask2 == KIsland.Mask2Value.UR_DL) {
                    cull_coords.Add(coord);
                    return true;
                }
                // New case that looks like this
                // 110
                // 1X1
                // 011
                if (mask2 == KIsland.Mask2Value.RD_UL) {
                    cull_coords.Add(coord);
                    return true;
                }
                break;
            }
            //case (KIsland.MaskValue.URD): {
            //    // New case that looks like this
            //    // 111
            //    // 011
            //    // 011
            //    if (mask2 == KIsland.Mask2Value.RD_UL) {
            //        cull_coords.Add(coord);
            //        return true;
            //    }
            //    break;
            //}
        }
        return false;
    }

    public static bool CheckCullTilePicky(Island island, Vector2 coord,
        KIsland.MaskValue mask, KIsland.Mask2Value mask2,
        HashSet<Vector2> cull_coords) {
        switch (mask) {
            // The case for a single line of tiles in any direction, or an
            // isolate tile.
            case (KIsland.MaskValue.U):
            case (KIsland.MaskValue.R):
            case (KIsland.MaskValue.D):
            case (KIsland.MaskValue.L):
            case (KIsland.MaskValue.NONE):
            case (KIsland.MaskValue.RL):
            case (KIsland.MaskValue.UD):
                cull_coords.Add(coord);
                return true;

            case (KIsland.MaskValue.UR): {
                // New case that looks like this
                // 110
                // 0x1
                // 001
                if (mask2 == KIsland.Mask2Value.RD_UL) {
                    cull_coords.Add(coord);
                }
                break;
            }
            case (KIsland.MaskValue.RDL): {
                // New case that looks like this
                // 001
                // 1X1
                // 110
                if (mask2 == KIsland.Mask2Value.UR_DL) {
                    cull_coords.Add(coord);
                    return true;
                }
                // New case that looks like this
                // 100
                // 1X1
                // 011
                if (mask2 == KIsland.Mask2Value.RD_UL) {
                    cull_coords.Add(coord);
                    return true;
                }
                break;
            }
            case (KIsland.MaskValue.URDL): {
                // New case that looks like this
                // 011
                // 1X1
                // 110
                if (mask2 == KIsland.Mask2Value.UR_DL) {
                    cull_coords.Add(coord);
                    return true;
                }
                // New case that looks like this
                // 110
                // 1X1
                // 011
                if (mask2 == KIsland.Mask2Value.RD_UL) {
                    cull_coords.Add(coord);
                    return true;
                }
                break;
            }
            //case (KIsland.MaskValue.URD): {
            //    // New case that looks like this
            //    // 111
            //    // 011
            //    // 011
            //    if (mask2 == KIsland.Mask2Value.RD_UL) {
            //        cull_coords.Add(coord);
            //        return true;
            //    }
            //    break;
            //}
        }
        return false;
    }
}