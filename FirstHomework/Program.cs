using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace FirstHomework
{
    internal static class Program
    {
        private static Stopwatch _stopwatch;

        private static void Main()
        {
            var process = Process.GetCurrentProcess();
            var lastProcessorCore = (IntPtr) Math.Pow(2, Environment.ProcessorCount - 1);
            process.PriorityClass = ProcessPriorityClass.RealTime;
            process.ProcessorAffinity = lastProcessorCore;

            const int dataCapacity = 10;
            var data = new List<long>(dataCapacity);

            MeasureTime(data);
            Console.WriteLine($"The average time is: {data.Average()}");
        }

        private static void MeasureTime(ICollection<long> data)
        {
            for (var i = 0; i < 50; i++)
            {
                _stopwatch = new Stopwatch();

                var firstThread = new Thread(() =>
                {
                    _stopwatch.Start();
                    while (_stopwatch.IsRunning)
                    {
                    }
                })
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Normal,
                };
                firstThread.Start();


                var secondThread = new Thread(_stopwatch.Stop)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.Highest,
                };
                secondThread.Start();

                secondThread.Join();
                firstThread.Join();

                data.Add(_stopwatch.ElapsedMilliseconds);
            }
        }
    }
}