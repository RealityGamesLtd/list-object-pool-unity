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
        public Action DisposeCallback { get; private set; }

        public void Setup(int poolElementIndex, Action disposeCallback)
        {
            PoolElementIndex = poolElementIndex;
            DisposeCallback = disposeCallback;
        }

        public void Dispose()
        {
            DisposeCallback?.Invoke();
        }
    }
}