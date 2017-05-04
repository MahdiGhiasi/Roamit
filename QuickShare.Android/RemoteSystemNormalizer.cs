using Microsoft.ConnectedDevices;
using QuickShare.DevicesListManager;
using QuickShare.Droid.RomeComponent;
using System;

namespace QuickShare.Droid
{
    internal class RemoteSystemNormalizer : IAttributesNormalizer
    {
        public NormalizedRemoteSystem Normalize(object o)
        {
            var rs = o as RemoteSystem;
            if (rs == null)
                throw new InvalidCastException();
            
            return new NormalizedRemoteSystem
            {
                DisplayName = rs.DisplayName,
                Id = rs.Id,
                IsAvailableByProximity = rs.IsAvailableByProximity,
                IsAvailableBySpatialProximity = rs.IsAvailableByProximity,
                Kind = rs.Kind.ToString(),
                Status = rs.Status.ConvertToNormalizedRemoteSystemStatus(),
            };
        }
    }
}