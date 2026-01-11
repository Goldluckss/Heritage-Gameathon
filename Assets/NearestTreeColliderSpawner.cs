using UnityEngine;
using System.Collections.Generic;

// Spawns pooled collider GameObjects for terrain trees near the player.
// Recommended: use a simple collider prefab (Capsule/Sphere), not a MeshCollider.
public class NearestTreeColliderSpawner : MonoBehaviour
{
    [Header("References")]
    public Terrain terrain;                   // leave null to use Terrain.activeTerrain
    public Transform player;                  // leave null to use Camera.main

    [Header("Spawn Settings")]
    public float spawnRadius = 12f;           // within this radius we'll spawn colliders
    public float despawnRadius = 18f;         // outside this radius we'll remove them
    public float updateInterval = 0.5f;       // seconds between checks (lower = more responsive)
    public int maxActiveColliders = 20;       // hard cap on number of spawned colliders

    [Header("Collider Prefab (optional)")]
    public GameObject colliderPrefab;         // prefab should contain a Collider (capsule/sphere) and no heavy components

    // internal
    Dictionary<int, GameObject> active = new Dictionary<int, GameObject>(); // treeIndex => instance
    Queue<GameObject> pool = new Queue<GameObject>();
    float nextCheckTime = 0f;
    TerrainData terrainData;
    Vector3 terrainPos;
    Vector3 terrainSize;

    void Start()
    {
        if (terrain == null) terrain = Terrain.activeTerrain;
        if (player == null && Camera.main != null) player = Camera.main.transform;
        if (terrain == null || player == null)
        {
            Debug.LogError("NearestTreeColliderSpawner: missing terrain or player.");
            enabled = false;
            return;
        }

        terrainData = terrain.terrainData;
        terrainPos = terrain.transform.position;
        terrainSize = terrainData.size;

        // Pre-warm pool with a few objects (optional)
        for (int i = 0; i < Mathf.Min(4, maxActiveColliders); i++)
            pool.Enqueue(CreateNewColliderObject());
    }

    GameObject CreateNewColliderObject()
    {
        GameObject go;
        if (colliderPrefab != null)
        {
            go = Instantiate(colliderPrefab);
            go.SetActive(false);
            return go;
        }

        go = new GameObject("TreeCollider_Pooled");
        // default: CapsuleCollider approximating a tree trunk
        CapsuleCollider cap = go.AddComponent<CapsuleCollider>();
        cap.center = new Vector3(0f, 1.5f, 0f);
        cap.height = 3f;
        cap.radius = 0.5f;
        go.SetActive(false);
        return go;
    }

    GameObject GetFromPool()
    {
        if (pool.Count > 0) return pool.Dequeue();
        return CreateNewColliderObject();
    }

    void ReturnToPool(GameObject go)
    {
        go.SetActive(false);
        pool.Enqueue(go);
    }

    void Update()
    {
        if (Time.time < nextCheckTime) return;
        nextCheckTime = Time.time + updateInterval;
        UpdateNearbyTreeColliders();
    }

    void UpdateNearbyTreeColliders()
    {
        TreeInstance[] trees = terrainData.treeInstances;
        if (trees == null || trees.Length == 0) return;

        Vector3 playerPos = player.position;

        // 1) find candidate trees within spawnRadius
        //    use squared distances for speed
        float spawnRadiusSqr = spawnRadius * spawnRadius;
        float despawnRadiusSqr = despawnRadius * despawnRadius;

        // track which indices should remain
        HashSet<int> shouldKeep = new HashSet<int>();

        // iterate and spawn for ones inside spawnRadius (or keep existing if already spawned)
        for (int i = 0; i < trees.Length; i++)
        {
            // quick early-out if we've reached cap
            if (active.Count >= maxActiveColliders && shouldKeep.Count >= maxActiveColliders) break;

            TreeInstance ti = trees[i];
            Vector3 worldPos = new Vector3(
                ti.position.x * terrainSize.x,
                ti.position.y * terrainSize.y,
                ti.position.z * terrainSize.z
            ) + terrainPos;

            float distSqr = (playerPos - worldPos).sqrMagnitude;
            if (distSqr <= spawnRadiusSqr)
            {
                shouldKeep.Add(i);
                // spawn if not already active and under cap
                if (!active.ContainsKey(i) && active.Count < maxActiveColliders)
                {
                    GameObject go = GetFromPool();
                    go.transform.position = worldPos;
                    // rotate according to tree rotation (TreeInstance rotation is radians)
                    go.transform.rotation = Quaternion.Euler(0f, ti.rotation * Mathf.Rad2Deg, 0f);
                    // scale according to prototype scale (approx)
                    go.transform.localScale = new Vector3(ti.widthScale, ti.heightScale, ti.widthScale);
                    go.SetActive(true);
                    active.Add(i, go);
                }
            }
        }

        // 2) despawn those outside despawnRadius or not in shouldKeep
        List<int> toRemove = new List<int>();
        foreach (var kv in active)
        {
            int idx = kv.Key;
            GameObject go = kv.Value;
            Vector3 goPos = go.transform.position;
            float distSqr = (playerPos - goPos).sqrMagnitude;
            if (distSqr > despawnRadiusSqr || !shouldKeep.Contains(idx))
            {
                toRemove.Add(idx);
            }
        }
        foreach (int idx in toRemove)
        {
            GameObject go = active[idx];
            active.Remove(idx);
            ReturnToPool(go);
        }
    }

    // Optional: draw debug gizmos
    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(player.position, spawnRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(player.position, despawnRadius);
        }
    }
}
