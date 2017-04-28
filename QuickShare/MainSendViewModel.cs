using System.ComponentModel;

namespace QuickShare
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

        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}