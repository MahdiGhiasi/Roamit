using QuickShare.ViewModels.History;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace QuickShare
{
    public sealed partial class HistoryPage : Page
    {
        public ObservableCollection<HistoryItem> HistoryItems { get; set; } = new IncrementalLoadingCollection<HistoryItemSource, HistoryItem>(20, 1);

        public HistoryPage()
        {
            this.InitializeComponent();
        }
    }
}
