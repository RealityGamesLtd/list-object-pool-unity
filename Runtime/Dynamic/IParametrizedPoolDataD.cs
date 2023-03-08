using System;

namespace ObjectPool.Dynamic
{
    public interface IParametrizedPoolDataD<TEnum> : IPoolDataD where TEnum: Enum
    {
        TEnum PoolElementType { get; }
    }
}