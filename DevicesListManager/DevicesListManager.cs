using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QuickShare.DevicesListManager
{
    public class DevicesListManager
    {
        private readonly uint _initialCountValue = 5;

        string dataFileLocation;
        IAttributesNormalizer attrNormalizer;

        List<object> devices = new List<object>();

        public ObservableCollection<NormalizedRemoteSystem> RemoteSystems { get; private set; } = new ObservableCollection<NormalizedRemoteSystem>();
        public NormalizedRemoteSystem SelectedRemoteSystem { get; set; }

        Dictionary<string, uint> selectCounts = new Dictionary<string, uint>();

        public DevicesListManager(string _dataFileLocation, IAttributesNormalizer _attributesNormalizer)
        {
            dataFileLocation = _dataFileLocation;
            attrNormalizer = _attributesNormalizer;
        }

        public void AddDevice(object o)
        {
            devices.Add(o);
            Sort();
        }

        public void RemoveDevice(object o)
        {
            devices.Remove(o);
            Sort();
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
        
        public void Select(NormalizedRemoteSystem rs)
        {
            if (selectCounts.ContainsKey(rs.Id))
                selectCounts[rs.Id]++;
            else
                selectCounts[rs.Id] = _initialCountValue;

            SelectedRemoteSystem = rs;
            Sort();
        }

        private double CalculateScore(NormalizedRemoteSystem rs)
        {
            if (!selectCounts.ContainsKey(rs.Id))
                return 0;

            return Math.Ceiling(((double)selectCounts[rs.Id]) / 10.0);
        }

        public void Sort()
        {
            RemoteSystems.Clear();
            foreach (var item in devices.Select(x => attrNormalizer.Normalize(x)).OrderBy(x => x.DisplayName).OrderBy(x => CalculateScore(x)).OrderByDescending(x => x.IsAvailableByProximity))
            {
                if (item.Id != SelectedRemoteSystem?.Id)
                    RemoteSystems.Add(item);
            }
        }
    }
}
