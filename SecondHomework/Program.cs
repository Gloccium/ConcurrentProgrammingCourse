using System;
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

    public class StringWrapper
    {
        public string String { get; }
        public StringWrapper(string @string) => String = @string;
    }

    public class MultiLock : IMultiLock
    {
        private readonly Dictionary<string, StringWrapper> _stringWrappers = new();

        public IDisposable AcquireLock(params string[] keys)
        {
            lock (_stringWrappers)
                foreach (var key in keys)
                    if (!_stringWrappers.ContainsKey(key))
                        _stringWrappers[key] = new StringWrapper(key);

            var blockedObjects = keys
                .Select(e => _stringWrappers[e])
                .OrderBy(e => e.String)
                .ToArray();

            var disposable = new Disposable(blockedObjects);

            foreach (var blockedObject in blockedObjects)
                Monitor.Enter(blockedObject);

            return disposable;
        }
    }

    public class Disposable : IDisposable
    {
        private readonly StringWrapper[] _blockedObjects;

        public Disposable(IEnumerable<StringWrapper> blockedObjects) => _blockedObjects = blockedObjects.ToArray();

        public void Dispose()
        {
            foreach (var blockedObject in _blockedObjects)
                if (Monitor.IsEntered(blockedObject))
                    Monitor.Exit(blockedObject);
        }
    }
}