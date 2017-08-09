using System.Runtime.Serialization;

namespace QuickShare.ViewModels.ShareTarget
{
    [DataContract]
    internal class ShareTargetDetails
    {
        [DataMember]
        public string Type { get; set; }
    }
}