using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuickShare.Common.Extensions
{
    // from http://blog.briandrupieski.com/throttling-asynchronous-methods-in-csharp
    public static class ParallelForEach
    {
        public static async Task ParallelForEachAsync<T>(this IEnumerable<T> source, int degreeOfParallelism, Func<T, Task> body)
        {
            var tasks = new List<Task>();
            using (var throttler = new SemaphoreSlim(degreeOfParallelism))
            {
                foreach (var element in source)
                {
                    await throttler.WaitAsync();
                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            await body(element);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
                }
                await Task.WhenAll(tasks);
            }
        }
    }
}
