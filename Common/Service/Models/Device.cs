using QuickShare.DevicesListManager;
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
        public string FormFactor { get; set; }
    }
}
