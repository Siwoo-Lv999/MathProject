using System.Collections.Generic;
using UnityEngine;
using Services = _LumenLib.ServiceLocator.ServiceLocator;

namespace _LumenLib.PoolingSystem.Runtime
{
    [DefaultExecutionOrder(-1000)]
    public class ObjectPool : MonoBehaviour
    {
        [SerializeField] private PoolingListSO poolList;
        private readonly Dictionary<string, Pool> _poolDict = new();

        private void Awake()
        {
            if (Services.TryGet<ObjectPool>(out var existing) && existing != null && existing != this)
            {
                Destroy(gameObject);
                return;
            }

            _poolDict.Clear();

            if (poolList == null || poolList.PoolList == null)
            {
                Debug.LogError("Pooling list is not assigned.", this);
                return;
            }

            foreach (var item in poolList.PoolList)
            {
                if (item == null)
                {
                    Debug.LogError("Pooling list contains a null item.", this);
                    continue;
                }

                CreatePool(item.ItemName, item.Prefab, item.PoolSize);
            }

            Services.Register<ObjectPool>(this);
        }

        private void OnDestroy()
        {
            Services.Unregister<ObjectPool>(this);
        }

        private void CreatePool(string itemName, GameObject prefab, int count)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                Debug.LogError("Pool item name is empty.", this);
                return;
            }

            if (prefab == null)
            {
                Debug.LogError($"Pool prefab is missing: {itemName}", this);
                return;
            }

            if (_poolDict.ContainsKey(itemName))
            {
                Debug.LogError($"Duplicate pool item name was ignored: {itemName}", this);
                return;
            }

            IPoolable poolable = prefab.GetComponent<IPoolable>();
            if (poolable == null || poolable.Item == null || poolable.GameObject == null)
            {
                Debug.LogError($"Pool prefab must have a valid IPoolable component: {prefab.name}", prefab);
                return;
            }

            Pool pool = new Pool(poolable, transform, Mathf.Max(0, count));
            _poolDict.Add(itemName, pool);
        }

        public IPoolable Pop(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return null;

            if (_poolDict.TryGetValue(itemName, out Pool pool))
            {
                IPoolable item = pool.Pop();
                item.ResetItem();
                return item;
            }

            return null;
        }
        
        public void Push(IPoolable item)
        {
            if (item == null || item.Item == null || item.GameObject == null)
                return;

            if (_poolDict.TryGetValue(item.Item.ItemName, out Pool pool))
            {
                pool.Push(item);
            }
        }
    }
}
