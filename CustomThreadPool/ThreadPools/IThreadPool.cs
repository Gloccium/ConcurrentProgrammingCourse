using System;

namespace CustomThreadPool
{
    public interface IThreadPool
    {
        void Enqueue(Action action);
        long TasksCount();
    }
}