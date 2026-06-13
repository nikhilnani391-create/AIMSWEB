using UnityEngine;
using System;
using System.Collections.Generic;
using FreeFire.Inventory;

namespace FreeFire.GlooWall
{
    public struct DeployGlooWallCommand
    {
        public uint PlayerId;
        public Vector3 TargetPosition;
        public Quaternion TargetRotation;
        public float Timestamp;
    }

    public class GlooWallManager : MonoBehaviour
    {
        public static GlooWallManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameObject glooWallPrefab;
        [SerializeField] private int maxActiveWallsPerPlayer = 3;
        [SerializeField] private int maxTotalWalls = 100;
        [SerializeField] private float deployValidationRadius = 5f;

        private readonly Dictionary<Guid, GlooWallEntity> activeWalls =
            new Dictionary<Guid, GlooWallEntity>();

        private readonly Dictionary<uint, List<Guid>> playerWalls =
            new Dictionary<uint, List<Guid>>();

        public int TotalActiveWalls => activeWalls.Count;

        public event Action<Guid, Vector3, Quaternion, uint> OnWallSpawned;
        public event Action<Guid> OnWallDestroyed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public bool ValidateAndSpawnWall(DeployGlooWallCommand command)
        {
            if (activeWalls.Count >= maxTotalWalls)
            {
                Debug.LogWarning("[GlooWallManager] Max total walls reached.");
                return false;
            }

            if (playerWalls.TryGetValue(command.PlayerId, out List<Guid> walls))
            {
                walls.RemoveAll(id => !activeWalls.ContainsKey(id));

                if (walls.Count >= maxActiveWallsPerPlayer)
                {
                    Debug.LogWarning($"[GlooWallManager] Player {command.PlayerId} " +
                                     $"reached max walls ({maxActiveWallsPerPlayer}).");
                    return false;
                }
            }

            float slopeAngle = GetSlopeAtPosition(command.TargetPosition);
            if (slopeAngle > 45f)
            {
                Debug.LogWarning("[GlooWallManager] Slope too steep for placement.");
                return false;
            }

            Guid wallNetId = Guid.NewGuid();
            SpawnWall(wallNetId, command.PlayerId, command.TargetPosition, command.TargetRotation);
            return true;
        }

        private void SpawnWall(Guid networkId, uint ownerId, Vector3 position, Quaternion rotation)
        {
            GameObject wallObj;

            if (glooWallPrefab != null)
            {
                wallObj = Instantiate(glooWallPrefab, position, rotation);
            }
            else
            {
                wallObj = CreateDefaultWallMesh(position, rotation);
            }

            var entity = wallObj.GetComponent<GlooWallEntity>();
            if (entity == null)
            {
                entity = wallObj.AddComponent<GlooWallEntity>();
            }

            entity.Initialize(ownerId, networkId, position, rotation);
            entity.OnGlooWallDestroyed += HandleWallDestroyed;

            activeWalls[networkId] = entity;

            if (!playerWalls.ContainsKey(ownerId))
            {
                playerWalls[ownerId] = new List<Guid>();
            }
            playerWalls[ownerId].Add(networkId);

            OnWallSpawned?.Invoke(networkId, position, rotation, ownerId);

            Debug.Log($"[GlooWallManager] Wall spawned: {networkId} by player {ownerId}");
        }

        private void HandleWallDestroyed(Guid wallId)
        {
            if (activeWalls.TryGetValue(wallId, out GlooWallEntity entity))
            {
                entity.OnGlooWallDestroyed -= HandleWallDestroyed;

                uint ownerId = entity.OwnerId;
                activeWalls.Remove(wallId);

                if (playerWalls.TryGetValue(ownerId, out List<Guid> walls))
                {
                    walls.Remove(wallId);
                }

                OnWallDestroyed?.Invoke(wallId);
            }
        }

        public bool DamageWall(Guid wallId, float damage)
        {
            if (!activeWalls.TryGetValue(wallId, out GlooWallEntity entity))
                return false;

            entity.ApplyDamage(damage);
            return true;
        }

        private float GetSlopeAtPosition(Vector3 position)
        {
            if (Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f))
            {
                return Vector3.Angle(hit.normal, Vector3.up);
            }
            return 0f;
        }

        private static GameObject CreateDefaultWallMesh(Vector3 position, Quaternion rotation)
        {
            var wallObj = new GameObject("GlooWall");
            wallObj.transform.position = position;
            wallObj.transform.rotation = rotation;

            var meshFilter = wallObj.AddComponent<MeshFilter>();
            var meshRenderer = wallObj.AddComponent<MeshRenderer>();

            meshFilter.mesh = CreateCurvedWallMesh();
            meshRenderer.material = new Material(Shader.Find("Standard"));
            meshRenderer.material.color = new Color(0.2f, 0.8f, 0.3f, 0.8f);

            return wallObj;
        }

        private static Mesh CreateCurvedWallMesh()
        {
            int segments = 8;
            float width = 2.5f;
            float height = 2.0f;
            float curvature = 0.4f;

            var mesh = new Mesh();
            int vertCount = (segments + 1) * 2;
            var vertices = new Vector3[vertCount];
            var triangles = new int[segments * 6];
            var uvs = new Vector2[vertCount];

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float x = Mathf.Lerp(-width / 2f, width / 2f, t);
                float z = -Mathf.Sin(t * Mathf.PI) * curvature;

                vertices[i * 2] = new Vector3(x, 0, z);
                vertices[i * 2 + 1] = new Vector3(x, height, z);

                uvs[i * 2] = new Vector2(t, 0);
                uvs[i * 2 + 1] = new Vector2(t, 1);
            }

            for (int i = 0; i < segments; i++)
            {
                int baseIdx = i * 6;
                int vertIdx = i * 2;

                triangles[baseIdx] = vertIdx;
                triangles[baseIdx + 1] = vertIdx + 1;
                triangles[baseIdx + 2] = vertIdx + 2;

                triangles[baseIdx + 3] = vertIdx + 1;
                triangles[baseIdx + 4] = vertIdx + 3;
                triangles[baseIdx + 5] = vertIdx + 2;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        public void CleanupAllWalls()
        {
            foreach (var kvp in activeWalls)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }
            activeWalls.Clear();
            playerWalls.Clear();
        }
    }
}
