using System;
using UnityEngine;

namespace ObjectPool.Dynamic
{
    /// <summary>
    /// Script that must be attatched to prefab for spawning in scroll list
    /// </summary>
    [Serializable]
    public abstract class ParametrizedPoolPrefabD<TData, TEnum> : PoolPrefabD, IParametrizedPoolDataD<TEnum>
        where TData: IParametrizedPoolDataD<TEnum> 
        where TEnum: Enum
    {
        public abstract TEnum PoolElementType { get; }

        public abstract void HandleDataOnSetup(TData data);

        public virtual void Setup(TData data)
        {
            Setup(data.PoolElementId, data.PrefabHeight, data.PrefabVerticalPosition);
            HandleDataOnSetup(data);
        }
    }
}