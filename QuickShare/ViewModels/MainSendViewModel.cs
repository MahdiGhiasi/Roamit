using System.ComponentModel;
using Windows.UI.Xaml;

namespace QuickShare.ViewModels
{
    public class MainSendViewModel : INotifyPropertyChanged
    {
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

        int progressMaximum;
        public int ProgressMaximum
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

        int progressValue;
        public int ProgressValue
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


        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}