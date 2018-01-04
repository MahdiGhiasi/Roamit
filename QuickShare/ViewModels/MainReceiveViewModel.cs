using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace QuickShare.ViewModels
{
    public class MainReceiveViewModel : INotifyPropertyChanged
    {
        string receiveStatus;
        public string ReceiveStatus
        {
            get
            {
                return receiveStatus;
            }
            set
            {
                receiveStatus = value;
                OnPropertyChanged("ReceiveStatus");
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

        Visibility dontCloseWindowNoticeVisibility;
        public Visibility DontCloseWindowNoticeVisibility
        {
            get
            {
                return dontCloseWindowNoticeVisibility;
            }
            set
            {
                dontCloseWindowNoticeVisibility = value;
                OnPropertyChanged("DontCloseWindowNoticeVisibility");
            }
        }

        Visibility dontSwitchAppsNoticeVisibility;
        public Visibility DontSwitchAppsNoticeVisibility
        {
            get
            {
                return dontSwitchAppsNoticeVisibility;
            }
            set
            {
                dontSwitchAppsNoticeVisibility = value;
                OnPropertyChanged("DontSwitchAppsNoticeVisibility");
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
        public string ProgressSpeed
        {
            get
            {
                return progressSpeed;
            }
            set
            {
                progressSpeed = value;
                OnPropertyChanged("ProgressSpeed");
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
