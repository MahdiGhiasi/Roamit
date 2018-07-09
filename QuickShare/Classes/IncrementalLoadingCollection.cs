using QuickShare.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace QuickShare
{
    // From https://marcominerva.wordpress.com/2013/05/22/implementing-the-isupportincrementalloading-interface-in-a-window-store-app/
    // with small modifications

    public interface IIncrementalSource<T>
    {
        Task<IEnumerable<T>> GetPagedItems(int pageIndex, int pageSize);
    }

    public class IncrementalLoadingCollection<T, I> : ObservableCollection<I>,
    ISupportIncrementalLoading
    where T : IIncrementalSource<I>, new()
    {
        public delegate void LoadFinishedEventHandler(EventArgs e);
        public event LoadFinishedEventHandler LoadFinished;

        private T source;
        private int itemsPerPart;
        private int itemsPerPage;
        private bool hasMoreItems;
        private int currentPage;

        private int partsCount;

        public Func<I, bool> VisibilityDecider { get; }

        public IncrementalLoadingCollection(int preferredItemsPerPage = 20, int partCoefficient = 3) :
            this(item => true, preferredItemsPerPage, partCoefficient)
        {
        }

        public IncrementalLoadingCollection(Func<I, bool> visibilityDecider, int preferredItemsPerPage = 20, int partCoefficient = 3)
        {
            source = new T();
            itemsPerPart = Math.Max(preferredItemsPerPage / partCoefficient, 5);
            itemsPerPage = itemsPerPart * partCoefficient;
            hasMoreItems = true;
            partsCount = partCoefficient;
            this.VisibilityDecider = visibilityDecider;
        }

        public bool HasMoreItems
        {
            get { return hasMoreItems; }
        }

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            var dispatcher = Window.Current.Dispatcher;

            return Task.Run<LoadMoreItemsResult>(
            async () =>
            {
                uint resultCount = 0;
                IEnumerable<I> result = null;

                for (int i = 0; i < partsCount; i++)
                {
                    //dispatcher.RunAsync doesn't wait for task to be done, so we use this method instead.
                    await DispatcherEx.RunTaskAsync(dispatcher, async () =>
                    {
                        result = await source.GetPagedItems(currentPage++, itemsPerPart);

                        if (result == null || result.Count() == 0)
                        {
                            hasMoreItems = false;
                            LoadFinished?.Invoke(new EventArgs());
                        }
                        else
                        {
                            resultCount = (uint)result.Count();

                            foreach (I item in result)
                                if (VisibilityDecider(item))
                                    this.Add(item);
                        }
                    }, CoreDispatcherPriority.High);
                }
                return new LoadMoreItemsResult() { Count = resultCount };
            }).AsAsyncOperation<LoadMoreItemsResult>();
        }
    }
}
