using System;
using UnityEngine;

namespace ObjectPool.Dynamic
{
    /// <summary>
    /// Script that must be attatched to prefab for spawning in scroll list
    /// </summary>
    [Serializable]
    public class PoolPrefabD : MonoBehaviour, IPoolDataD
    {
        public string PoolElementId { get; set; }
        public float PrefabHeight { get; set; }
        public float PrefabVerticalPosition { get; set; }
        public Action DisposeCallback { get; private set; }

        public void Setup(string id, float height, float verticalPosition, Action disposeCallback)
        {
            PoolElementId = id;
            PrefabHeight = height;
            PrefabVerticalPosition = verticalPosition;
            DisposeCallback = disposeCallback;
        }

        public void Dispose()
        {
            DisposeCallback?.Invoke();
        }
    }
}