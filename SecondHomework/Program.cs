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
            Array.Sort(keys);
            var blockedObjects = new List<object>();
            
            lock (_objects)
                foreach (var key in keys)
                {
                    if (!_objects.ContainsKey(key))
                        _objects[key] = new object();
                    
                    blockedObjects.Add(_objects[key]);
                }

            foreach (var blockedObject in blockedObjects)
            {
                try
                {
                    Monitor.Enter(blockedObject);
                }
                
                catch(Exception e)
                {
                    if (Monitor.IsEntered(blockedObject))
                        Monitor.Exit(blockedObject);
                    
                    throw new Exception($"{e}");
                }
            }

            return new Disposable(blockedObjects);
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
                if (Monitor.IsEntered(obj))
                    Monitor.Exit(obj);
        }
    }
}