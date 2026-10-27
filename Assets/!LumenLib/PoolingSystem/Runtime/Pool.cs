using System.Collections.Generic;
using UnityEngine;

namespace _LumenLib.PoolingSystem.Runtime
{
    public class Pool
    {
        private Stack<IPoolable> _pool;
        private Transform _parentTrm;
        private IPoolable _poolable;
        private GameObject _prefab;

        public Pool(IPoolable poolable, Transform parent, int count)
        {
            _pool = new Stack<IPoolable>(count);
            _parentTrm = parent;
            _poolable = poolable;
            _prefab = poolable.GameObject;

            for (int i = 0; i < count; i++)
            {
                IPoolable item = CreatePoolItem();
                _pool.Push(item);
            }
        }
        
        private IPoolable CreatePoolItem()
        {
            GameObject gameObject = Object.Instantiate(_prefab, _parentTrm);
            gameObject.SetActive(false);
            gameObject.name = _poolable.Item.ItemName;
            return gameObject.GetComponent<IPoolable>();
        }

        public IPoolable Pop()
        {
            IPoolable item = null;
            
            if (_pool.Count <= 0)
                item = CreatePoolItem();
            
            else
                item = _pool.Pop();
            
            return item;
        }
        
        public void Push(IPoolable item)
        {
            item.GameObject.SetActive(false);
            _pool.Push(item);
        }
    }
}