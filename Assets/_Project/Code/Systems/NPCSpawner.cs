using UnityEngine;
using System.Collections.Generic;

namespace FeedTheNight.NPCs
{
    [AddComponentMenu("FeedTheNight/NPCs/NPC Spawner")]
    public class NPCSpawner : MonoBehaviour
    {
        [Header("NPC Prefabs")]
        [Tooltip("Drag and drop your NPC prefabs here.")]
        public List<GameObject> npcPrefabs;

        [Header("Spawn Zones")]
        [Tooltip("GameObjects with Box Colliders that define the spawn areas.")]
        public List<BoxCollider> spawnZones;

        [Header("Spawn Configuration")]
        [Tooltip("Maximum number of active NPCs spawned by this spawner.")]
        public int maxNPCs = 15;
        
        [Tooltip("Should NPCs spawn automatically on start?")]
        public bool spawnOnStart = true;
        
        [Tooltip("How many NPCs to spawn immediately on start (capped by Max NPCs).")]
        public int initialSpawnCount = 5;

        [Tooltip("Time interval in seconds between spawning new NPCs.")]
        public float spawnInterval = 3f;

        [Header("Ground Placement (Optional)")]
        [Tooltip("If true, the spawner will cast a ray downward to place the NPC exactly on the ground/floor.")]
        public bool projectToGround = true;
        [Tooltip("Layers that count as ground for the raycast.")]
        public LayerMask groundLayers = ~0;
        [Tooltip("Max distance for the ground raycast.")]
        public float groundCheckDistance = 10f;

        private List<GameObject> _spawnedNPCs = new List<GameObject>();
        private float _spawnTimer;

        private void Start()
        {
            if (spawnOnStart)
            {
                int countToSpawn = Mathf.Min(initialSpawnCount, maxNPCs);
                for (int i = 0; i < countToSpawn; i++)
                {
                    SpawnNPC();
                }
            }
            _spawnTimer = spawnInterval;
        }

        private void Update()
        {
            // Clean up destroyed NPCs from the list
            _spawnedNPCs.RemoveAll(item => item == null);

            // Spawn loop over time
            if (_spawnedNPCs.Count < maxNPCs)
            {
                _spawnTimer -= Time.deltaTime;
                if (_spawnTimer <= 0f)
                {
                    SpawnNPC();
                    _spawnTimer = spawnInterval;
                }
            }
        }

        public GameObject SpawnNPC()
        {
            if (npcPrefabs == null || npcPrefabs.Count == 0)
            {
                Debug.LogWarning($"[NPCSpawner - {gameObject.name}] No NPC prefabs assigned!");
                return null;
            }

            if (spawnZones == null || spawnZones.Count == 0)
            {
                Debug.LogWarning($"[NPCSpawner - {gameObject.name}] No spawn zones (Box Colliders) assigned!");
                return null;
            }

            // 1. Pick a random prefab
            GameObject prefabToSpawn = npcPrefabs[Random.Range(0, npcPrefabs.Count)];
            if (prefabToSpawn == null) return null;

            // 2. Pick a random spawn zone
            BoxCollider selectedZone = spawnZones[Random.Range(0, spawnZones.Count)];
            if (selectedZone == null) return null;

            // 3. Find a random point inside the BoxCollider bounds
            Vector3 spawnPosition = GetRandomPointInBox(selectedZone);

            // 4. Project to ground if enabled
            if (projectToGround)
            {
                RaycastHit hit;
                // Cast ray down from the selected point
                if (Physics.Raycast(spawnPosition, Vector3.down, out hit, groundCheckDistance, groundLayers))
                {
                    spawnPosition = hit.point;
                }
            }

            // 5. Instantiate the NPC
            GameObject spawnedNPC = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
            _spawnedNPCs.Add(spawnedNPC);

            Debug.Log($"[NPCSpawner - {gameObject.name}] Spawned {spawnedNPC.name} at {spawnPosition} inside zone {selectedZone.gameObject.name}");
            return spawnedNPC;
        }

        private Vector3 GetRandomPointInBox(BoxCollider box)
        {
            // Note: box.bounds is in world space, which is perfect!
            Bounds bounds = box.bounds;
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomY = Random.Range(bounds.min.y, bounds.max.y);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);

            return new Vector3(randomX, randomY, randomZ);
        }

        // Draw helper gizmos in Editor
        private void OnDrawGizmosSelected()
        {
            if (spawnZones == null) return;

            Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
            foreach (var zone in spawnZones)
            {
                if (zone != null)
                {
                    // Draw a semi-transparent solid box and wire box
                    Bounds bounds = zone.bounds;
                    Gizmos.DrawCube(bounds.center, bounds.size);
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                }
            }
        }
    }
}
