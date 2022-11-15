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
        private readonly ConcurrentDictionary<string, object> _objects = new();

        public IDisposable AcquireLock(params string[] keys)
        {
            lock (_objects)
                foreach (var key in keys)
                    if (!_objects.ContainsKey(key))
                        _objects[key] = key;

            var blockedObjects = keys
                .Select(e => _objects[e])
                .OrderBy(e => e.ToString())
                .ToArray();

            var disposable = new Disposable(blockedObjects);

            foreach (var blockedObject in blockedObjects)
                try
                {
                    Monitor.Enter(blockedObject);
                }
                catch (ThreadAbortException)
                {
                    Monitor.Exit(blockedObject);
                }

            return disposable;
        }
    }

    public class Disposable : IDisposable
    {
        private readonly object[] _blockedObjects;

        public Disposable(IEnumerable<object> blockedObjects) => _blockedObjects = blockedObjects.ToArray();

        public void Dispose()
        {
            foreach (var blockedObject in _blockedObjects)
                if (Monitor.IsEntered(blockedObject))
                    Monitor.Exit(blockedObject);
        }
    }
}