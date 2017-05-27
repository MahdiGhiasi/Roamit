using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace QuickShare.DevicesListManager
{
    public class DevicesListManager : INotifyPropertyChanged
    {
        private readonly uint _initialCountValue = 5;

        string dataFileLocation;
        IAttributesNormalizer attrNormalizer;

        List<object> devices = new List<object>();

        public ObservableCollection<NormalizedRemoteSystem> RemoteSystems { get; private set; } = new ObservableCollection<NormalizedRemoteSystem>();

        public event PropertyChangedEventHandler PropertyChanged;
        Dictionary<string, uint> selectCounts = new Dictionary<string, uint>();

        NormalizedRemoteSystem selectedRemoteSystem;
        public NormalizedRemoteSystem SelectedRemoteSystem
        {
            get
            {
                return selectedRemoteSystem;
            }
            set
            {
                selectedRemoteSystem = value;
                OnPropertyChanged("SelectedRemoteSystem");
            }
        }

        public DevicesListManager(string _dataFileLocation, IAttributesNormalizer _attributesNormalizer)
        {
            dataFileLocation = _dataFileLocation;
            attrNormalizer = _attributesNormalizer;
        }

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void AddDevice(object o)
        {
            devices.Add(o);
            Sort();
        }

        public void RemoveDevice(object o)
        {
            devices.Remove(o);
        }

        public void RemoveDeviceById(string id)
        {
            var d = devices.FirstOrDefault(x => attrNormalizer.Normalize(x).Id == id);
            if (d != null)
                RemoveDevice(d);
        }

        public void RemoveDeviceByName(string name)
        {
            var d = devices.Where(x => attrNormalizer.Normalize(x).DisplayName == name);
            if (d == null)
                return;
            foreach (var i in d)
            {
                RemoveDevice(i);
            }
        }

        public void Select(object o)
        {
            if (o is NormalizedRemoteSystem)
            {
                var rs = o as NormalizedRemoteSystem;
                if (selectCounts.ContainsKey(rs.Id))
                    selectCounts[rs.Id]++;
                else
                    selectCounts[rs.Id] = _initialCountValue;

                SelectedRemoteSystem = rs;
                Sort();
            }
            else
            {
                Select(attrNormalizer.Normalize(o));
            }
        }

        private double CalculateScore(NormalizedRemoteSystem rs)
        {
            if (!selectCounts.ContainsKey(rs.Id))
                return 0;

            return Math.Ceiling(((double)selectCounts[rs.Id]) / 10.0);
        }

        public List<NormalizedRemoteSystem> GetSortedList(NormalizedRemoteSystem selected)
        {
            var output = new List<NormalizedRemoteSystem>();
            foreach (var item in devices.Select(x => attrNormalizer.Normalize(x)).Where(x => x.Kind != "Unknown").OrderBy(x => x.DisplayName).OrderBy(x => CalculateScore(x)).OrderByDescending(x => x.IsAvailableByProximity))
            {
                if (item.Id != selected?.Id)
                    output.Add(item);
            }

            return output;
        }

        public void Sort()
        {
            lock (RemoteSystems)
            {
                RemoteSystems.Clear();
                foreach (var item in devices.Select(x => attrNormalizer.Normalize(x)).Where(x => x.Kind != "Unknown").OrderBy(x => x.DisplayName).OrderBy(x => CalculateScore(x)).OrderByDescending(x => x.IsAvailableByProximity))
                {
                    if (item.Id != SelectedRemoteSystem?.Id)
                        RemoteSystems.Add(item);
                }
            }
        }

        public bool IsAndroidDevicePresent
        {
            get
            {
                return (devices.Select(x => attrNormalizer.Normalize(x)).FirstOrDefault(x => (x.Kind == "Unknown")) != null);
            }
        }

        public NormalizedRemoteSystem SelectHighScoreItem()
        {
            if (RemoteSystems.Count == 0)
                return null;

            SelectedRemoteSystem = null;
            Sort();

            NormalizedRemoteSystem output = RemoteSystems[0];
            Select(output);
            return output;
        }
    }
}
