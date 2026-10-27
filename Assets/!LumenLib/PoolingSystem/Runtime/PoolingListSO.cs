using System.Collections.Generic;
using UnityEngine;

namespace _LumenLib.PoolingSystem.Runtime
{
    [CreateAssetMenu(fileName = "Pooling List", menuName = "Lib/PoolingSystem/Pooling List", order = 0)]
    public class PoolingListSO : ScriptableObject
    {
        [field: SerializeField] public List<PoolItemSO> PoolList { get; private set; } = new();
    }
}
