using System;

namespace ObjectPool
{
    public interface IDisposable
    {
        Action DisposeCallback { get; set; }
        void Dispose();
    }
}

