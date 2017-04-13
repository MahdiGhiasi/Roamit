using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QuickShare.Common
{
    public class RequestDetails
    {
        public string RemoteEndpointAddress { get; set; }
        public Uri Url { get; set; }
        public string Host { get; set; }
        public string HttpMethod { get; set; }
        public IDictionary<string, object> Headers { get; set; }
        public Stream InputStream { get; set; }
        public string ProtocolVersion { get; set; }
        public bool KeepAlive { get; set; }
    }
}
