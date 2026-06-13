using UnityEngine;
using System;

namespace FreeFire.GlooWall
{
    public class GlooWallEntity : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float maxHealth = 500f;
        [SerializeField] private float lifetime = 45f;

        private float currentHealth;
        private float spawnTime;
        private uint ownerId;
        private Guid networkId;
        private bool isDestroyed;

        public float CurrentHealth => currentHealth;
        public float HealthPercent => currentHealth / maxHealth;
        public Guid NetworkId => networkId;
        public uint OwnerId => ownerId;

        public event Action<Guid> OnGlooWallDestroyed;

        public void Initialize(uint owner, Guid netId, Vector3 position, Quaternion rotation)
        {
            ownerId = owner;
            networkId = netId;
            currentHealth = maxHealth;
            spawnTime = Time.time;
            isDestroyed = false;

            transform.position = position;
            transform.rotation = rotation;

            var collider = gameObject.GetComponent<BoxCollider>();
            if (collider == null)
            {
                collider = gameObject.AddComponent<BoxCollider>();
            }
            collider.size = new Vector3(2.5f, 2.0f, 0.3f);
            collider.center = new Vector3(0, 1.0f, 0);

            gameObject.layer = LayerMask.NameToLayer("Default");

            Debug.Log($"[GlooWall] Spawned: owner={ownerId}, netId={networkId}, hp={maxHealth}");
        }

        private void Update()
        {
            if (isDestroyed) return;

            if (Time.time - spawnTime >= lifetime)
            {
                DestroyWall();
            }
        }

        public float ApplyDamage(float damage)
        {
            if (isDestroyed) return 0f;

            float actualDamage = Mathf.Min(damage, currentHealth);
            currentHealth -= actualDamage;

            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                DestroyWall();
            }

            return actualDamage;
        }

        private void DestroyWall()
        {
            if (isDestroyed) return;

            isDestroyed = true;
            OnGlooWallDestroyed?.Invoke(networkId);

            Debug.Log($"[GlooWall] Destroyed: netId={networkId}");

            Destroy(gameObject, 0.1f);
        }

        public float GetRemainingLifetime()
        {
            return Mathf.Max(0f, lifetime - (Time.time - spawnTime));
        }
    }
}
