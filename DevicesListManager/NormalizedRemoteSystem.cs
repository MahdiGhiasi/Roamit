namespace QuickShare.DevicesListManager
{
    public class NormalizedRemoteSystem
    {
        public string DisplayName { get; set; }
        public string Id { get; set; }
        public bool IsAvailableByProximity { get; set; }
        public bool IsAvailableBySpatialProximity { get; set; }
        public string Kind { get; set; }
        public NormalizedRemoteSystemStatus Status { get; set; }
        public string AppVersion { get; set; }
        public DeviceType Type { get; set; }
    }
    
    public enum NormalizedRemoteSystemStatus
    {
        Unavailable = 0,
        DiscoveringAvailability = 1,
        Available = 2,
        Unknown = 3
    }

    public enum DeviceType
    {
        Windows = 1,
        Android = 2,
        GraphWindowsDevice = 3,
        GraphUnknownDevice = 9999,
    }
}