using System;

namespace ObjectPool
{
    public interface IDisposable
    {
        Action DisposeCallback { get; }
        void Dispose();
    }
}

