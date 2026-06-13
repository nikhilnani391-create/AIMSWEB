using UnityEngine;
using System;
using System.Collections.Generic;

namespace FreeFire.Inventory
{
    [Serializable]
    public struct LootSpawnZone
    {
        public string ZoneName;
        public Vector3 Center;
        public float Radius;
        public int MinItems;
        public int MaxItems;
        public float RareItemBonusWeight;
    }

    public struct WorldLootItem
    {
        public Guid NetworkId;
        public LootItemDefinition Definition;
        public Vector3 WorldPosition;
        public bool IsPickedUp;
        public float SpawnTime;
    }

    public class LootSpawner : MonoBehaviour
    {
        public static LootSpawner Instance { get; private set; }

        [Header("Spawn Configuration")]
        [SerializeField] private int totalLootItems = 800;
        [SerializeField] private float minSpawnHeight = 0.3f;
        [SerializeField] private float maxTerrainSampleHeight = 500f;
        [SerializeField] private LayerMask terrainMask;

        [Header("Urban Zone Definitions")]
        [SerializeField] private LootSpawnZone[] spawnZones;

        private readonly Dictionary<Guid, WorldLootItem> activeLoot =
            new Dictionary<Guid, WorldLootItem>();

        private readonly Dictionary<Guid, WorldLootItem> pickedUpLoot =
            new Dictionary<Guid, WorldLootItem>();

        public int ActiveLootCount => activeLoot.Count;

        public event Action<WorldLootItem> OnLootSpawned;
        public event Action<Guid, uint> OnLootPickedUp;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SpawnAllLoot()
        {
            activeLoot.Clear();
            pickedUpLoot.Clear();

            if (spawnZones == null || spawnZones.Length == 0)
            {
                GenerateDefaultZones();
            }

            int itemsPerZone = totalLootItems / spawnZones.Length;
            int remainder = totalLootItems % spawnZones.Length;

            for (int z = 0; z < spawnZones.Length; z++)
            {
                int itemCount = itemsPerZone + (z < remainder ? 1 : 0);
                LootSpawnZone zone = spawnZones[z];

                int extraItems = UnityEngine.Random.Range(zone.MinItems, zone.MaxItems + 1);
                itemCount = Mathf.Max(itemCount, extraItems);

                for (int i = 0; i < itemCount; i++)
                {
                    SpawnLootInZone(zone);
                }
            }

            Debug.Log($"[LootSpawner] Spawned {activeLoot.Count} loot items across {spawnZones.Length} zones.");
        }

        private void SpawnLootInZone(LootSpawnZone zone)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * zone.Radius;
            Vector3 candidatePos = zone.Center + new Vector3(randomCircle.x, maxTerrainSampleHeight, randomCircle.y);

            Vector3 spawnPos;
            if (Physics.Raycast(candidatePos, Vector3.down, out RaycastHit hit,
                    maxTerrainSampleHeight * 2f, terrainMask))
            {
                spawnPos = hit.point + Vector3.up * minSpawnHeight;
            }
            else
            {
                spawnPos = new Vector3(candidatePos.x, minSpawnHeight, candidatePos.z);
            }

            LootItemDefinition itemDef = LootTable.Instance.GetRandomItem();

            var worldItem = new WorldLootItem
            {
                NetworkId = Guid.NewGuid(),
                Definition = itemDef,
                WorldPosition = spawnPos,
                IsPickedUp = false,
                SpawnTime = Time.time
            };

            activeLoot[worldItem.NetworkId] = worldItem;
            OnLootSpawned?.Invoke(worldItem);
        }

        public bool TryPickUpItem(Guid itemNetworkId, uint playerId, Vector3 playerPosition)
        {
            if (!activeLoot.TryGetValue(itemNetworkId, out WorldLootItem item))
                return false;

            if (item.IsPickedUp)
                return false;

            float distance = Vector3.Distance(playerPosition, item.WorldPosition);
            if (distance > 3f)
            {
                Debug.LogWarning($"[LootSpawner] Player {playerId} too far from item " +
                                 $"({distance:F1}m > 3m max).");
                return false;
            }

            item.IsPickedUp = true;
            activeLoot.Remove(itemNetworkId);
            pickedUpLoot[itemNetworkId] = item;

            OnLootPickedUp?.Invoke(itemNetworkId, playerId);

            Debug.Log($"[LootSpawner] Player {playerId} picked up {item.Definition.DisplayName}");
            return true;
        }

        public List<WorldLootItem> GetNearbyLoot(Vector3 position, float radius)
        {
            var nearby = new List<WorldLootItem>();
            float sqrRadius = radius * radius;

            foreach (var kvp in activeLoot)
            {
                if ((kvp.Value.WorldPosition - position).sqrMagnitude <= sqrRadius)
                {
                    nearby.Add(kvp.Value);
                }
            }

            return nearby;
        }

        private void GenerateDefaultZones()
        {
            spawnZones = new LootSpawnZone[]
            {
                new LootSpawnZone { ZoneName = "Pochinok", Center = new Vector3(500, 0, 500), Radius = 200, MinItems = 40, MaxItems = 60, RareItemBonusWeight = 1.0f },
                new LootSpawnZone { ZoneName = "Clock Tower", Center = new Vector3(-300, 0, 800), Radius = 150, MinItems = 35, MaxItems = 50, RareItemBonusWeight = 1.5f },
                new LootSpawnZone { ZoneName = "Factory", Center = new Vector3(0, 0, -200), Radius = 180, MinItems = 30, MaxItems = 45, RareItemBonusWeight = 1.2f },
                new LootSpawnZone { ZoneName = "Peak", Center = new Vector3(-600, 0, -400), Radius = 120, MinItems = 25, MaxItems = 40, RareItemBonusWeight = 1.8f },
                new LootSpawnZone { ZoneName = "Katulistiwa", Center = new Vector3(700, 0, -500), Radius = 200, MinItems = 35, MaxItems = 50, RareItemBonusWeight = 1.0f },
                new LootSpawnZone { ZoneName = "Mars Electric", Center = new Vector3(-800, 0, 200), Radius = 130, MinItems = 25, MaxItems = 35, RareItemBonusWeight = 1.3f },
                new LootSpawnZone { ZoneName = "Bimasakti", Center = new Vector3(200, 0, -700), Radius = 160, MinItems = 30, MaxItems = 45, RareItemBonusWeight = 1.1f },
                new LootSpawnZone { ZoneName = "Rim Nam", Center = new Vector3(-200, 0, 400), Radius = 140, MinItems = 30, MaxItems = 40, RareItemBonusWeight = 1.0f },
            };
        }

        public WorldLootItem? GetItem(Guid networkId)
        {
            if (activeLoot.TryGetValue(networkId, out WorldLootItem item))
                return item;
            return null;
        }
    }
}
