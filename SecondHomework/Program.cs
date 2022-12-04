using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SecondHomework
{
    internal static class Program
    {
        private static void Main()
        {
        }
    }

    public interface IMultiLock
    {
        public IDisposable AcquireLock(params string[] keys);
    }

    public class MultiLock : IMultiLock
    {
        private readonly Dictionary<string, object> _objects = new();

        public IDisposable AcquireLock(params string[] keys)
        {
            foreach (var key in keys)
            {
                if (!_objects.ContainsKey(key))
                    _objects[key] = new object();
            }

            Array.Sort(keys);

            try
            {
                foreach (var key in keys)
                    Monitor.Enter(_objects[key]);

                return new Disposable(keys.Reverse());
            }

            catch (Exception)
            {
                foreach (var key in keys.Reverse())
                    if (Monitor.IsEntered(_objects[key]))
                        Monitor.Exit(key);
                
                throw;
            }
        }
    }

    public class Disposable : IDisposable
    {
        private readonly IEnumerable<object> _blockedObjects;

        public Disposable(IEnumerable<object> blockedObjects)
        {
            _blockedObjects = blockedObjects;
        }

        public void Dispose()
        {
            foreach (var obj in _blockedObjects)
                Monitor.Exit(obj);
        }
    }
}