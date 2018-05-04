using System.ComponentModel;
using Windows.UI.Core;
using Windows.UI.Xaml;
using System;

namespace QuickShare.ViewModels
{
    public class MainSendViewModel : INotifyPropertyChanged
    {
        private CoreDispatcher dispatcher;

        public MainSendViewModel(CoreDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;
        }

        string sendStatus;
        public string SendStatus
        {
            get
            {
                return sendStatus;
            }
            set
            {
                sendStatus = value;
                OnPropertyChanged("SendStatus");
            }
        }

        double progressMaximum;
        public double ProgressMaximum
        {
            get
            {
                return progressMaximum;
            }
            set
            {
                progressMaximum = value;
                OnPropertyChanged("ProgressMaximum");
            }
        }

        double progressValue;
        public double ProgressValue
        {
            get
            {
                return progressValue;
            }
            set
            {
                progressValue = value;
                OnPropertyChanged("ProgressValue");
            }
        }

        bool progressIsIndeterminate;
        public bool ProgressIsIndeterminate
        {
            get
            {
                return progressIsIndeterminate;
            }
            set
            {
                progressIsIndeterminate = value;
                OnPropertyChanged("ProgressIsIndeterminate");
            }
        }

        Visibility unlockNoticeVisibility;
        public Visibility UnlockNoticeVisibility
        {
            get
            {
                return unlockNoticeVisibility;
            }
            set
            {
                unlockNoticeVisibility = value;
                OnPropertyChanged("UnlockNoticeVisibility");
            }
        }

        Visibility leaveScreenOnNoticeVisibility;
        public Visibility LeaveScreenOnNoticeVisibility
        {
            get
            {
                return leaveScreenOnNoticeVisibility;
            }
            set
            {
                leaveScreenOnNoticeVisibility = value;
                OnPropertyChanged("LeaveScreenOnNoticeVisibility");
            }
        }

        Visibility goBackButtonVisibility = Visibility.Collapsed;
        public Visibility GoBackButtonVisibility
        {
            get
            {
                return goBackButtonVisibility;
            }
            set
            {
                goBackButtonVisibility = value;
                OnPropertyChanged("GoBackButtonVisibility");
            }
        }

        Visibility progressPercentIndicatorVisibility = Visibility.Visible;
        public Visibility ProgressPercentIndicatorVisibility
        {
            get
            {
                return progressPercentIndicatorVisibility;
            }
            set
            {
                progressPercentIndicatorVisibility = value;
                OnPropertyChanged("ProgressPercentIndicatorVisibility");
            }
        }

        string progressSpeed;

        public string ProgressCaption
        {
            get
            {
                return progressSpeed;
            }
            set
            {
                progressSpeed = value;
                OnPropertyChanged("ProgressCaption");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        protected async void OnPropertyChanged(string name)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            });
        }
    }
}