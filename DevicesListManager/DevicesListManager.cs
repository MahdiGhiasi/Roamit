using Newtonsoft.Json;
using QuickShare.DataStore;
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
        Dictionary<string, uint> selectCounts;

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

            selectCounts = new Dictionary<string, uint>();
            try
            {
                DataStorageProviders.SettingsManager.OpenIfPossible();
                if (DataStorageProviders.SettingsManager.ContainsKey("selectCounts"))
                {
                    selectCounts = new Dictionary<string, uint>(JsonConvert.DeserializeObject<Dictionary<string, uint>>(DataStorageProviders.SettingsManager.GetItemContent("selectCounts")));
                }
                DataStorageProviders.SettingsManager.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Can't read selectCounts: " + ex.ToString());
            }
        }

        // Create the OnPropertyChanged method to raise the event
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void AddDevice(object o)
        {
            lock (RemoteSystems)
            {
                var newId = attrNormalizer.Normalize(o).Id;
                var existing = devices.FirstOrDefault(x => attrNormalizer.Normalize(x).Id == newId);
                if (existing != null)
                    devices.Remove(existing);

                devices.Add(o);
                Sort();
            }
        }

        public void RemoveDevice(object o)
        {
            lock (RemoteSystems)
            {
                devices.Remove(o);
            }
        }

        public void RemoveDeviceById(string id)
        {
            lock (RemoteSystems)
            {
                var d = devices.FirstOrDefault(x => attrNormalizer.Normalize(x).Id == id);
                if (d != null)
                    RemoveDevice(d);
            }
        }

        public void RemoveDeviceByName(string name)
        {
            lock (RemoteSystems)
            {
                var d = devices.Where(x => attrNormalizer.Normalize(x).DisplayName == name);
                if (d == null)
                    return;
                foreach (var i in d)
                {
                    RemoveDevice(i);
                }
            }
        }

        public void RemoveAndroidDevices()
        {
            lock (RemoteSystems)
            {
                devices.RemoveAll(x => ((x is NormalizedRemoteSystem) && ((x as NormalizedRemoteSystem).Kind == "QS_Android")));

                if (SelectedRemoteSystem.Kind == "QS_Android")
                {
                    SelectHighScoreItem();
                }
            }
        }

        public void Select(object o)
        {
            lock (RemoteSystems)
            {
                Select(o, true);
            }
        }

        Object dbLock = new Object();
        private async void Select(object o, bool updateHistory)
        {
            if (!(o is NormalizedRemoteSystem))
            {
                Select(attrNormalizer.Normalize(o), updateHistory);
                return;
            }

            var rs = o as NormalizedRemoteSystem;

            if (updateHistory)
            {
                if (selectCounts.ContainsKey(rs.Id))
                    selectCounts[rs.Id]++;
                else
                    selectCounts[rs.Id] = _initialCountValue;

                await DataStorageProviders.SettingsManager.OpenAsync();
                lock (dbLock)
                {
                    DataStorageProviders.SettingsManager.Add("selectCounts", JsonConvert.SerializeObject(selectCounts));
                    DataStorageProviders.SettingsManager.Close();
                }
            }

            SelectedRemoteSystem = rs;
            Sort();

            System.Diagnostics.Debug.WriteLine("Scores are:");
            foreach (var item in RemoteSystems)
            {
                System.Diagnostics.Debug.WriteLine(item.DisplayName + " : " + CalculateScore(item));
            }
            System.Diagnostics.Debug.WriteLine("");
            System.Diagnostics.Debug.WriteLine("");
        }

        private double CalculateScore(NormalizedRemoteSystem rs)
        {
            if (!selectCounts.ContainsKey(rs.Id))
            {
                if (rs.IsAvailableByProximity)
                    return 0.1;
                return 0;
            }
            
            uint maximum = selectCounts.Values.Max();
            double selectScore;
            if (maximum < 10)
                selectScore = selectCounts[rs.Id];
            else if (maximum < 20)
                selectScore = 3.0 * Math.Ceiling(((double)selectCounts[rs.Id]) / 3.0);
            else
                selectScore = 5.0 * Math.Ceiling(((double)selectCounts[rs.Id]) / 5.0);

            double proximityCoeff = 1.0;
            if (rs.IsAvailableByProximity)
                proximityCoeff = 1.2;

            return proximityCoeff * selectScore;
        }

        public List<NormalizedRemoteSystem> GetSortedList(NormalizedRemoteSystem selected)
        {
            lock (RemoteSystems)
            {
                var output = new List<NormalizedRemoteSystem>();
                foreach (var item in devices.Select(x => attrNormalizer.Normalize(x)).Where(x => x.Kind != "Unknown").OrderBy(x => x.DisplayName).OrderBy(x => CalculateScore(x)).OrderByDescending(x => x.IsAvailableByProximity))
                {
                    if (item.Id != selected?.Id)
                        output.Add(item);
                }

                return output;
            }
        }

        public void Sort()
        {
            lock (RemoteSystems)
            {
                RemoteSystems.Clear();
                foreach (var item in devices.Select(x => attrNormalizer.Normalize(x)).Where(x => x.Kind != "Unknown").OrderBy(x => x.DisplayName).OrderByDescending(x => CalculateScore(x)))
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
            lock (RemoteSystems)
            {
                if (RemoteSystems.Count == 0)
                    return null;

                SelectedRemoteSystem = null;
                Sort();

                NormalizedRemoteSystem output = RemoteSystems[0];
                Select(output, false);
                return output;
            }
        }
    }
}
