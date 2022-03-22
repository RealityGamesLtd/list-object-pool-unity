using System;
using UnityEngine;

namespace ObjectPool.Static
{
    /// <summary>
    /// Script that must be attatched to prefab for spawning in scroll list
    /// </summary>
    [Serializable]
    public class PoolPrefabS : MonoBehaviour, IPoolDataS
    {
        public int PoolElementIndex { get; set; }

        public void Setup(int poolElementIndex)
        {
            PoolElementIndex = poolElementIndex;
        }
    }
}