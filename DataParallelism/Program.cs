using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LogParsing.LogParsers;

namespace DataParallelismTask
{
    public class PlinqParser : ILogParser
    {
        private readonly FileInfo _fileInfo;
        private readonly Func<string, string> _getId;

        public PlinqParser(FileInfo fileInfo, Func<string, string> getId)
        {
            _fileInfo = fileInfo;
            _getId = getId;
        }

        public string[] ExtractIds()
        {
            var fileLines = File.ReadLines(_fileInfo.FullName);
            return fileLines
                .AsParallel()
                .Select(_getId)
                .Where(id => id != null)
                .ToArray();
        }
    }
    
    public class ParallelParser : ILogParser
    {
        private readonly FileInfo _fileInfo;
        private readonly Func<string, string> _getId;

        public ParallelParser(FileInfo fileInfo, Func<string, string> getId)
        {
            _fileInfo = fileInfo;
            _getId = getId;
        }

        public string[] ExtractIds()
        {
            var fileLines = File.ReadLines(_fileInfo.FullName);
            var concurBag = new ConcurrentBag<string>();
            Parallel.ForEach(fileLines, n =>
            {
                if (_getId(n) is { } id) concurBag.Add(id);
            });
            return concurBag.ToArray();
        }
    }
    
    public class ThreadParser : ILogParser
    {
        private readonly FileInfo _fileInfo;
        private readonly Func<string, string> _getId;

        public ThreadParser(FileInfo fileInfo, Func<string, string> getId)
        {
            _fileInfo = fileInfo;
            _getId = getId;
        }

        public string[] ExtractIds()
        {
            var threads = new Thread[Environment.ProcessorCount * 3];
            var concurStack = new ConcurrentStack<string>(File.ReadLines(_fileInfo.FullName));
            var concurBag = new ConcurrentBag<string>();

            for (var i = 0; i < threads.Length; i++)
            {
                var thread = MakeThread(concurStack, concurBag);
                
                thread.Start();
                threads[i] = thread;
            }

            JoinThreads(threads);
            return concurBag.ToArray();
        }

        private static void JoinThreads(IEnumerable<Thread> threads)
        {
            foreach (var thread in threads)
                thread.Join();
        }

        private Thread MakeThread(ConcurrentStack<string> concurStack, ConcurrentBag<string> concurBag)
        {
            var thread = new Thread(start: () =>
            {
                while (concurStack.TryPop(out var result))
                {
                    var id = _getId(result);
                    if (id != null)
                        concurBag.Add(id);
                }
            });
            return thread;
        }
    }
}