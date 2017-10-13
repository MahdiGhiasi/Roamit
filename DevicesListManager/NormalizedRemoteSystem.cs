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
    }
    
    public enum NormalizedRemoteSystemStatus
    {
        Unavailable = 0,
        DiscoveringAvailability = 1,
        Available = 2,
        Unknown = 3
    }
}