using UnityEngine;

namespace _LumenLib.PoolingSystem.Runtime
{
    [CreateAssetMenu(fileName = "Pool Item", menuName = "Lib/PoolingSystem/Pool Item", order = 0)]
    public class PoolItemSO : ScriptableObject
    {
        [field: SerializeField] public string ItemName { get; private set; }
        [field: SerializeField] public GameObject Prefab { get; private set; }
        [field: SerializeField] public int PoolSize { get; private set; }
        
        private void OnValidate()
        {
            if (Prefab != null)
            {
                IPoolable item = Prefab.GetComponent<IPoolable>();
                if (item == null)
                {
                    Prefab = null;
                    Debug.LogWarning("Can not find IPoolable component");
                }
            }
        }
    }
}