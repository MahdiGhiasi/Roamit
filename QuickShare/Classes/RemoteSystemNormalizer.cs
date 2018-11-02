using QuickShare.DevicesListManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.System.RemoteSystems;

namespace QuickShare.Classes
{
    internal class RemoteSystemNormalizer : IAttributesNormalizer
    {
        public NormalizedRemoteSystem Normalize(object o)
        {
            if (o is NormalizedRemoteSystem)
                return (NormalizedRemoteSystem)o;

            var rs = o as RemoteSystem;
            if (rs == null)            
                throw new InvalidCastException();
            

            return new NormalizedRemoteSystem
            {
                DisplayName = rs.DisplayName,
                Id = rs.Id,
                IsAvailableByProximity = rs.IsAvailableByProximity,
                IsAvailableBySpatialProximity = rs.IsAvailableBySpatialProximity,
                Kind = rs.Kind,
                Status = (NormalizedRemoteSystemStatus)((int)(rs.Status)),
                Type = DeviceType.Windows,
            };
        }
    }
}
