using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.ViewModels
{
    public class SendFailedViewModel
    {
        string errorTitle;
        public string ErrorTitle
        {
            get
            {
                return errorTitle;
            }
            set
            {
                errorTitle = value;
                OnPropertyChanged("ErrorTitle");
            }
        }

        string errorDescription;
        public string ErrorDescription
        {
            get
            {
                return errorDescription;
            }
            set
            {
                errorDescription = value;
                OnPropertyChanged("ErrorDescription");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
            catch { }
        }
    }
}
