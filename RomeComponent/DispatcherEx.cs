using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace QuickShare.Common
{
    public static class DispatcherEx
    {
        public static async Task RunOnCoreDispatcherIfPossible(Action action, bool runAnyway = true)
        {
            CoreDispatcher dispatcher = null;

            try
            {
                dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            }
            catch { }

            if (dispatcher != null)
            {
                await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { action.Invoke(); });
            }
            else if (runAnyway)
            {
                action.Invoke();
            }
        }

        internal static async Task RunOnCoreDispatcherIfPossible(Func<Task> action, bool runAnyway = true)
        {
            CoreDispatcher dispatcher = null;

            try
            {
                dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            }
            catch { }

            if (dispatcher != null)
            {
                await dispatcher.RunTaskAsync(async () => { await action(); });
            }
            else if (runAnyway)
            {
                await action();
            }
        }

        public static async Task<T> RunTaskAsync<T>(this CoreDispatcher dispatcher,
            Func<Task<T>> func, CoreDispatcherPriority priority = CoreDispatcherPriority.Normal)
        {
            var taskCompletionSource = new TaskCompletionSource<T>();
            await dispatcher.RunAsync(priority, async () =>
            {
                try
                {
                    taskCompletionSource.SetResult(await func());
                }
                catch (Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }
            });

            return await taskCompletionSource.Task;
        }

        // There is no TaskCompletionSource<void> so we use a bool that we throw away.
        public static async Task RunTaskAsync(this CoreDispatcher dispatcher,
            Func<Task> func, CoreDispatcherPriority priority = CoreDispatcherPriority.Normal) =>
            await RunTaskAsync(dispatcher, async () => { await func(); return false; }, priority);
    }
}
