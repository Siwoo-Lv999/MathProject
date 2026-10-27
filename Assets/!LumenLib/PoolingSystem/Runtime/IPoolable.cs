using UnityEngine;

namespace _LumenLib.PoolingSystem.Runtime
{
    public interface IPoolable
    {
        public PoolItemSO Item { get; }
        public GameObject GameObject { get; }

        public void ResetItem();
    }
}