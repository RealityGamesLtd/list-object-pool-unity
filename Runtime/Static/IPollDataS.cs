namespace ObjectPool.Static
{
    public interface IPoolDataS : IDisposable
    {
        int PoolElementIndex { get; set; }
    }
}