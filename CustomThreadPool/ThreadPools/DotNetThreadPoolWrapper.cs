using System;
using System.Threading;

namespace CustomThreadPool
{
    public class DotNetThreadPoolWrapper : IThreadPool
    {
        private long processedTask = 0L;
        
        public void Enqueue(Action action)
        {
            System.Threading.ThreadPool.UnsafeQueueUserWorkItem(delegate
            {
                action.Invoke();
                Interlocked.Increment(ref processedTask);
            }, null);
        }

        public long TasksCount() => processedTask;
    }
}