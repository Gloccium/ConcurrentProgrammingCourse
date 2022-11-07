using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace FirstHomework
{
    internal static class Program
    {
        private static void DoWorkload()
        {
            for (var i = 0; i < 300; ++i)
                i.GetHashCode();
        }
        
        public static void Main()
        {
            var process = Process.GetCurrentProcess();
            process.ProcessorAffinity = (IntPtr)Math.Pow(2, Environment.ProcessorCount - 1);
            process.PriorityClass = ProcessPriorityClass.RealTime;
            
            var data = new List<Tuple<int, long>>();
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            var firstThread = new Thread(() =>
            {
                for (var i = 0; i < 100000; i++)
                {
                    lock (data)
                    {
                        DoWorkload();
                        data.Add(Tuple.Create(1, stopwatch.ElapsedMilliseconds));
                    }
                }
            });
            
            var secondThread = new Thread(() =>
            {
                for (var i = 0; i < 100000; i++)
                {
                    lock (data)
                    {
                        DoWorkload();
                        data.Add(Tuple.Create(2, stopwatch.ElapsedMilliseconds));
                    }
                }
            });
            
            firstThread.Start();
            secondThread.Start();
            firstThread.Join();
            secondThread.Join();
            
            var count = 1;
            long sum = 0;
            long temp = 0;
            var threadsCount = 1;
            foreach (var item in data.Where(item => item.Item1 != threadsCount))
            {
                threadsCount = item.Item1;
                count++;
                sum += (item.Item2 - temp);
                temp = item.Item2;
            }
            Console.WriteLine(sum / count);
        }
    }
}