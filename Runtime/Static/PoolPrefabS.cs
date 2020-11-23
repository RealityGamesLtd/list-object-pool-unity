using System;
using UnityEngine;

namespace ObjectPool.Static
{
    /// <summary>
    /// Script that must be attatched to prefab for spawning in scroll list
    /// </summary>
    [Serializable]
    public class PoolPrefabS : MonoBehaviour
    {
        public int PoolElementIndex { get; set; }

        public void SetIndex(int poolElementIndex)
        {
            PoolElementIndex = poolElementIndex;
        }
    }
}