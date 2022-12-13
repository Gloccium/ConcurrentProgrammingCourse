using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CustomThreadPool
{
    public class ThreadPool : IThreadPool
    {
        private readonly Queue<Action> _queue = new Queue<Action>();
        private readonly Dictionary<int, WorkStealingQueue<Action>> _queues = new Dictionary<int, WorkStealingQueue<Action>>();
        private long _processedTask;

        public ThreadPool()
        {
            var threads = MakeThreads(Processing, Environment.ProcessorCount * 3);

            foreach (var thread in threads)
            {
                _queues[thread.ManagedThreadId] = new WorkStealingQueue<Action>();
                thread.Start();
            }
        }
        
        private static IEnumerable<Thread> MakeThreads(Action action, int processorCount)
        {
            return Enumerable
                .Range(0, processorCount)
                .Select(_ => new Thread(() => action()) {IsBackground = true})
                .ToArray();
        }
        
        public void Enqueue(Action action)
        {
            if (!_queues.ContainsKey(Thread.CurrentThread.ManagedThreadId))
                lock (_queue)
                {
                    _queue.Enqueue(action);
                    Monitor.Pulse(_queue);
                }
            else
                _queues[Thread.CurrentThread.ManagedThreadId].LocalPush(action);
        }
        
        public long TasksCount() => _processedTask;

        private void Processing()
        {
            while (true)
            {
                Action task = null;
                
                if (!_queues[Thread.CurrentThread.ManagedThreadId].LocalPop(ref task))
                {
                    task = QueueLocker();
                    
                    Stealing(task);
                }
                else
                {
                    task();
                    Interlocked.Increment(ref _processedTask);
                }
            }
        }

        private Action QueueLocker()
        {
            Action task;
            lock (_queue)
            {
                if (_queue.TryDequeue(out task))
                    _queues[Thread.CurrentThread.ManagedThreadId].LocalPush(task);
                else
                {
                    var flag = false;
                    foreach (var id in _queues)
                    {
                        if (id.Key == Thread.CurrentThread.ManagedThreadId || id.Value.IsEmpty) continue;
                        flag = true;
                        break;
                    }

                    if (!flag)
                        Monitor.Wait(_queue);
                }
            }

            return task;
        }

        private void Stealing(Action task)
        {
            if (task != null) return;
            {
                var first = new KeyValuePair<int, WorkStealingQueue<Action>>();
                foreach (var id in _queues)
                {
                    if (id.Key == Thread.CurrentThread.ManagedThreadId || id.Value.IsEmpty) continue;
                    first = id;
                    break;
                }

                var queueToSteal = first.Value;

                if (queueToSteal is null || !queueToSteal.TrySteal(ref task)) return;

                task();
                Interlocked.Increment(ref _processedTask);
            }
        }
    }
}