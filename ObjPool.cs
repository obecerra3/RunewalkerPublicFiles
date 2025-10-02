using UnityEngine;
using System.Collections.Generic;
using System;

public class ObjPool : Singleton<ObjPool> {
    public Dictionary<string, PoolObject> pool_object_dict =
        new Dictionary<string, PoolObject>();

    public bool Contains(string name) {
        return pool_object_dict.ContainsKey(name);
    }

    public void Add(GameObject new_go, int size, bool can_extend = true,
                    Action<GameObject> deep_copy = null) {
        // check if new_go in pool_object_dict
        if (pool_object_dict.ContainsKey(new_go.name)) {
            Debug.Log("Cannot Add, exists in ObjPool! name: " + new_go.name);
            return;
        }

        // Create parent_obj and pool to instantiate new PoolObject
        GameObject new_go_parent = new GameObject(new_go.name + "_parent");
        new_go_parent.transform.parent = gameObject.transform;
        var go_list = new List<GameObject>();
        for (int i = 0; i < size; ++i) {
            var go = Instantiate(new_go, new_go_parent.transform);
            go.name = new_go.name;
            if (deep_copy != null) {
                deep_copy(go);
            }
            go.SetActive(false);
            go_list.Add(go);
        }
        PoolObject pool_object = new PoolObject(new_go_parent, go_list,
                                                can_extend, deep_copy);
        pool_object.go_to_copy = new_go;
        new_go.transform.SetParent(new_go_parent.transform);
        new_go.SetActive(false);
        pool_object_dict.Add(new_go.name, pool_object);
    }

    public GameObject Get(string name) {
        try {
            // Get inactive object in pool_list.
            var pool_list = pool_object_dict[name].pool_list;
            foreach (var go in pool_list) {
                if (!go.activeInHierarchy) {
                    pool_object_dict[name].pool_list.Remove(go);
                    pool_object_dict[name].used_list.Add(go);
                    go.SetActive(true);
                    return go;
                }
            }

            if (pool_object_dict[name].can_extend) {
                // No inactive object found, expand obj_list.
                var go_to_copy = pool_object_dict[name].go_to_copy;
                var new_go = Instantiate(
                    go_to_copy, pool_object_dict[name].parent_go.transform);
                if (pool_object_dict[name].deep_copy != null) {
                    pool_object_dict[name].deep_copy(new_go);
                }
                new_go.name = go_to_copy.name;
                new_go.SetActive(true);
                pool_object_dict[name].used_list.Add(new_go);
                pool_object_dict[name].extend_count++;
                return new_go;
            }
            Debug.Log("Cannot extend ObjPool name: " + name);
            return null;
        } catch {
            Debug.Log("ObjPool name: " + name + ", does not exist in pool!");
            return null;
        }
    }

    public void Free(GameObject go) {
        go.SetActive(false);
        try {
            go.transform.SetParent(pool_object_dict[go.name].parent_go.transform);
            pool_object_dict[go.name].used_list.Remove(go);
            pool_object_dict[go.name].pool_list.Add(go);
        } catch {
            Debug.Log("ObjPool.Free Error for go.name: " + go.name);
        }
    }

    public void DebugPrint() {
        foreach (PoolObject pool_obj in pool_object_dict.Values) {
            if (pool_obj.extend_count < 50) {
                continue;
            }
            Debug.Log(pool_obj.parent_go.name + ": " + pool_obj.extend_count);
        }
    }

    void OnDestroy() {
        DebugPrint();
    }

    public PoolObject GetAll(string name) {
        return pool_object_dict[name];
    }
}

public class PoolObject {
    public GameObject parent_go;
    public List<GameObject> pool_list;
    public List<GameObject> used_list = new List<GameObject>();
    public bool can_extend;
    public Action<GameObject> deep_copy;
    public int extend_count;
    public GameObject go_to_copy;

    public PoolObject(GameObject parent_go, List<GameObject> pool_list,
                      bool can_extend, Action<GameObject> deep_copy) {
        this.parent_go = parent_go;
        this.pool_list = pool_list;
        this.can_extend = can_extend;
        this.deep_copy = deep_copy;
    }
}
