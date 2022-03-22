namespace ObjectPool.Dynamic
{
    public interface IPoolDataD : IDisposable
    {
        string PoolElementId { get; set; }
        float PrefabHeight { get; set; }
        float PrefabVerticalPosition { get; set; }
    }
}