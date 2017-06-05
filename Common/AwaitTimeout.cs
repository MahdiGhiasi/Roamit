using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common
{
    public static class AwaitTimeout
    {
        public static Task<TResult> WithTimeout<TResult>(this Task<TResult> task, TimeSpan timeout, TResult failedValue)
        {
            var timeoutTask = Task.Delay(timeout).ContinueWith(_ => failedValue, TaskContinuationOptions.ExecuteSynchronously);
            return Task.WhenAny(task, timeoutTask).Unwrap();
        }

        public static Task<TResult> WithTimeout<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout).ContinueWith(_ => default(TResult), TaskContinuationOptions.ExecuteSynchronously);
            return Task.WhenAny(task, timeoutTask).Unwrap();
        }

        public static Task WithTimeout(this Task task, TimeSpan timeout)
        {
            var timeoutTask = Task.Delay(timeout);
            return Task.WhenAny(task, timeoutTask).Unwrap();
        }
    }
}
