using System;
using System.Collections.Generic;
using System.Text;

namespace QuickShare.Common.Rome
{
    public class RomeAppServiceResponse
    {
        public RomeAppServiceResponseStatus Status { get; set; }
        public Dictionary<string, object> Message { get; set; }
    }
}
