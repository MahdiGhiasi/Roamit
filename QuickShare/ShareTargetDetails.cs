using System.Runtime.Serialization;

namespace QuickShare
{
    [DataContract]
    internal class ShareTargetDetails
    {
        [DataMember]
        public string Type { get; set; }
    }
}