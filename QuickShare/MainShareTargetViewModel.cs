using System.ComponentModel;
using Windows.UI.Xaml;

namespace QuickShare
{
    public class MainShareTargetViewModel : INotifyPropertyChanged
    {
        Visibility shareStorageItemVisibility;
        public Visibility ShareStorageItemVisibility
        {
            get
            {
                return shareStorageItemVisibility;
            }
            private set
            {
                shareStorageItemVisibility = value;
                OnPropertyChanged("ShareStorageItemVisibility");
            }
        }

        Visibility shareTextVisibility;
        public Visibility ShareTextVisibility
        {
            get
            {
                return shareTextVisibility;
            }
            private set
            {
                shareTextVisibility = value;
                OnPropertyChanged("ShareTextVisibility");
            }
        }

        Visibility shareUrlVisibility;
        public Visibility ShareUrlVisibility
        {
            get
            {
                return shareUrlVisibility;
            }
            private set
            {
                shareUrlVisibility = value;
                OnPropertyChanged("ShareUrlVisibility");
            }
        }

        bool appActionsAvailable = true;
        public bool AppActionsAvailable
        {
            get
            {
                return appActionsAvailable;
            }
            set
            {
                appActionsAvailable = value;
                OnPropertyChanged("AppActionsAvailable");
            }
        }

        string previewText;
        public string PreviewText
        {
            get
            {
                return previewText;
            }
            set
            {
                previewText = value;
                OnPropertyChanged("PreviewText");
            }
        }

        public void ShowShareText()
        {
            ShareTextVisibility = Visibility.Visible;
            ShareUrlVisibility = Visibility.Collapsed;
            ShareStorageItemVisibility = Visibility.Collapsed;
        }

        public void ShowShareUrl()
        {
            ShareTextVisibility = Visibility.Collapsed;
            ShareUrlVisibility = Visibility.Visible;
            ShareStorageItemVisibility = Visibility.Collapsed;
        }

        public void ShowShareStorageItem()
        {
            ShareTextVisibility = Visibility.Collapsed;
            ShareUrlVisibility = Visibility.Collapsed;
            ShareStorageItemVisibility = Visibility.Visible;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}