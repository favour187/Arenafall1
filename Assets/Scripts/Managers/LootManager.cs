using System.Collections.Generic;
using UnityEngine;
using ArenaFall.Core;
using ArenaFall.Data;
using ArenaFall.Gameplay.Inventory;

namespace ArenaFall.Managers
{
    /// <summary>
    /// Manages loot spawning, distribution, and respawning across the map.
    /// Uses loot tables for weighted random item selection.
    /// </summary>
    public class LootManager : MonoBehaviour
    {
        [Header("Loot Configuration")]
        [SerializeField] private LootTableData[] _lootTables;
        [SerializeField] private Transform[] _lootSpawnPoints;
        [SerializeField] private GameObject _lootItemPrefab;

        [Header("Respawn")]
        [SerializeField] private bool _enableRespawn = false;
        [SerializeField] private float _respawnDelay = 60f;

        private List<LootSpawn> _activeLoot = new();
        private Dictionary<string, LootTableData> _tableCache = new();

        public static LootManager Instance { get; private set; }

        private class LootSpawn
        {
            public Transform spawnPoint;
            public GameObject instance;
            public LootTableData table;
            public float respawnTime;
            public bool isActive;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            ServiceLocator.Register<LootManager>(this);
        }

        private void Start()
        {
            CacheLootTables();
            SpawnInitialLoot();
        }

        private void Update()
        {
            if (!_enableRespawn) return;

            foreach (var loot in _activeLoot)
            {
                if (!loot.isActive)
                {
                    loot.respawnTime -= Time.deltaTime;
                    if (loot.respawnTime <= 0)
                    {
                        RespawnLoot(loot);
                    }
                }
            }
        }

        private void CacheLootTables()
        {
            foreach (var table in _lootTables)
            {
                if (table != null && !_tableCache.ContainsKey(table.tableId))
                {
                    _tableCache[table.tableId] = table;
                }
            }
        }

        private void SpawnInitialLoot()
        {
            foreach (var spawnPoint in _lootSpawnPoints)
            {
                if (spawnPoint == null) continue;

                var table = SelectLootTableForZone(spawnPoint.position);
                if (table != null)
                {
                    SpawnLootAtPoint(spawnPoint, table);
                }
            }

            Debug.Log($"[LootManager] Spawned {_activeLoot.Count} loot items");
        }

        /// <summary>
        /// Spawn loot at a specific point using a given loot table.
        /// </summary>
        public GameObject SpawnLootAtPoint(Transform spawnPoint, LootTableData table)
        {
            if (table == null || _lootItemPrefab == null) return null;

            // Select random item from loot table
            var entry = SelectRandomEntry(table);
            if (entry == null) return null;

            // Check spawn chance
            if (Random.value > entry.spawnChance) return null;

            // Create loot instance
            GameObject lootObj = Instantiate(_lootItemPrefab, spawnPoint.position, Quaternion.identity);
            var lootItem = lootObj.GetComponent<LootItem>();
            
            if (lootItem != null)
            {
                int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
                lootItem.Initialize(entry.item, amount);
            }

            // Track for respawn
            var spawn = new LootSpawn
            {
                spawnPoint = spawnPoint,
                instance = lootObj,
                table = table,
                isActive = true,
                respawnTime = _respawnDelay
            };
            
            _activeLoot.Add(spawn);

            return lootObj;
        }

        /// <summary>
        /// Spawn loot at a random point around a position.
        /// </summary>
        public void SpawnLootAtPosition(Vector3 position, LootTableData table, float radius = 3f)
        {
            Vector3 randomOffset = Random.insideUnitSphere * radius;
            randomOffset.y = 0;
            
            var spawnPoint = new GameObject("TempSpawnPoint").transform;
            spawnPoint.position = position + randomOffset;
            
            SpawnLootAtPoint(spawnPoint, table);
            
            Destroy(spawnPoint.gameObject, 0.1f);
        }

        private LootEntry SelectRandomEntry(LootTableData table)
        {
            if (table.lootEntries.Count == 0) return null;

            // Weighted random selection
            int totalWeight = 0;
            foreach (var entry in table.lootEntries)
            {
                totalWeight += entry.weight;
            }

            int randomValue = Random.Range(0, totalWeight);
            int currentWeight = 0;

            foreach (var entry in table.lootEntries)
            {
                currentWeight += entry.weight;
                if (randomValue < currentWeight)
                {
                    return entry;
                }
            }

            return table.lootEntries[0];
        }

        private LootTableData SelectLootTableForZone(Vector3 position)
        {
            // Select based on zone type (simplified)
            if (_lootTables.Length > 0)
            {
                return _lootTables[Random.Range(0, _lootTables.Length)];
            }
            return null;
        }

        private void RespawnLoot(LootSpawn loot)
        {
            if (loot.instance != null)
            {
                Destroy(loot.instance);
            }

            SpawnLootAtPoint(loot.spawnPoint, loot.table);
            loot.isActive = true;
        }

        /// <summary>
        /// Mark loot as collected (for respawn tracking).
        /// </summary>
        public void OnLootCollected(GameObject lootObject)
        {
            foreach (var loot in _activeLoot)
            {
                if (loot.instance == lootObject)
                {
                    loot.isActive = false;
                    loot.respawnTime = _respawnDelay;
                    break;
                }
            }
        }

        /// <summary>
        /// Register a loot spawn point.
        /// </summary>
        public void RegisterSpawnPoint(Transform point)
        {
            var list = new List<Transform>(_lootSpawnPoints);
            if (!list.Contains(point))
            {
                list.Add(point);
                _lootSpawnPoints = list.ToArray();
            }
        }

        /// <summary>
        /// Get a loot table by ID.
        /// </summary>
        public LootTableData GetLootTable(string tableId)
        {
            return _tableCache.TryGetValue(tableId, out var table) ? table : null;
        }
    }
}
