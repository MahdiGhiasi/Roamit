using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common.Service.Models
{
    public class Device
    {
        public DeviceType Type { get; set; }
        public string DeviceID { get; set; }
        public string FriendlyName { get; set; }
        public string AppVersion { get; set; }
    }

    public enum DeviceType
    {
        Windows = 1,
        Android = 2,
    }
}
