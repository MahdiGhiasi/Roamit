using QuickShare.DevicesListManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common.Service.Models.v3
{
    public class DeviceBasic3
    {
        public string AccountID { get; set; }
        public string DeviceID { get; set; }
        public string Name { get; set; }
        public DeviceType Kind { get; set; }
        public string FormFactor { get; set; }
        public string Status { get; set; }
        public bool CloudClipboardEnabled { get; set; }
        public string AppVersion { get; set; }
    }
}
