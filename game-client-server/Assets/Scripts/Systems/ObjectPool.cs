using UnityEngine;
using System.Collections.Generic;

namespace FreeFire.Systems
{
    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        private readonly Dictionary<string, Queue<GameObject>> pools =
            new Dictionary<string, Queue<GameObject>>();

        private readonly Dictionary<string, GameObject> prefabRegistry =
            new Dictionary<string, GameObject>();

        private readonly Dictionary<GameObject, string> activeObjects =
            new Dictionary<GameObject, string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void RegisterPool(string poolId, GameObject prefab, int initialSize)
        {
            if (pools.ContainsKey(poolId)) return;

            prefabRegistry[poolId] = prefab;
            pools[poolId] = new Queue<GameObject>();

            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateNewInstance(poolId, prefab);
                obj.SetActive(false);
                pools[poolId].Enqueue(obj);
            }
        }

        public GameObject Get(string poolId, Vector3 position, Quaternion rotation)
        {
            if (!pools.ContainsKey(poolId))
            {
                Debug.LogWarning($"[ObjectPool] Pool '{poolId}' not registered.");
                return null;
            }

            GameObject obj;

            if (pools[poolId].Count > 0)
            {
                obj = pools[poolId].Dequeue();

                while (obj == null && pools[poolId].Count > 0)
                {
                    obj = pools[poolId].Dequeue();
                }

                if (obj == null)
                {
                    obj = CreateNewInstance(poolId, prefabRegistry[poolId]);
                }
            }
            else
            {
                obj = CreateNewInstance(poolId, prefabRegistry[poolId]);
            }

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            activeObjects[obj] = poolId;
            return obj;
        }

        public void Return(GameObject obj)
        {
            if (obj == null) return;

            if (!activeObjects.TryGetValue(obj, out string poolId))
            {
                Destroy(obj);
                return;
            }

            activeObjects.Remove(obj);
            obj.SetActive(false);
            obj.transform.SetParent(transform);

            if (pools.ContainsKey(poolId))
            {
                pools[poolId].Enqueue(obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        public void ReturnDelayed(GameObject obj, float delay)
        {
            if (obj == null) return;

            StartCoroutine(ReturnAfterDelay(obj, delay));
        }

        private System.Collections.IEnumerator ReturnAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            Return(obj);
        }

        private GameObject CreateNewInstance(string poolId, GameObject prefab)
        {
            var obj = Instantiate(prefab, transform);
            obj.name = $"{poolId}_pooled";
            return obj;
        }

        public int GetPoolSize(string poolId)
        {
            return pools.ContainsKey(poolId) ? pools[poolId].Count : 0;
        }

        public int GetActiveCount(string poolId)
        {
            int count = 0;
            foreach (var kvp in activeObjects)
            {
                if (kvp.Value == poolId) count++;
            }
            return count;
        }

        public void ClearPool(string poolId)
        {
            if (!pools.ContainsKey(poolId)) return;

            while (pools[poolId].Count > 0)
            {
                var obj = pools[poolId].Dequeue();
                if (obj != null) Destroy(obj);
            }

            var toRemove = new List<GameObject>();
            foreach (var kvp in activeObjects)
            {
                if (kvp.Value == poolId) toRemove.Add(kvp.Key);
            }

            foreach (var obj in toRemove)
            {
                activeObjects.Remove(obj);
                if (obj != null) Destroy(obj);
            }
        }
    }
}
