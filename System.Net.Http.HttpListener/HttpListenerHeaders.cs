using System.Collections.Generic;
using System.Text;

namespace System.Net.Http
{
    public class HttpListenerHeaders : Dictionary<string, string>
    {
        private HttpListenerHeaderValueCollection<string> contentType;
        private HttpListenerHeaderValueCollection<string> connection;
        private string contentMD5;
        private bool? isRequestHeaders;

        private HttpListenerResponse response;
        private HttpListenerRequest request;

        internal void ParseHeaderLines(IEnumerable<string> lines)
        {
            foreach (var headerLine in lines)
            {
                var parts = headerLine.Split(':');
                var key = parts[0];
                var value = parts[1].Trim();
                Add(key, value);
            }
        }

        private string MakeHeaderString()
        {
            var sb = new StringBuilder();
            foreach (var header in this)
            {
                sb.Append($"{header.Key}: {header.Value}\r\n");
            }
            return sb.ToString();
        }

        public override string ToString()
        {
            return MakeHeaderString();
        }

        public HttpListenerHeaderValueCollection<string> Connection
        {
            get
            {
                if (connection == null)
                {
                    connection = new HttpListenerHeaderValueCollection<string>(this, "Connection");
                }
                return connection;
            }
        }

        #region Content Headers

        public HttpListenerHeaderValueCollection<string> ContentType
        {
            get
            {
                if (contentType == null)
                {
                    contentType = new HttpListenerHeaderValueCollection<string>(this, "Content-Type");
                }
                return contentType;
            }
        }

        public long ContentLength
        {
            get
            {
                if (IsRequestHeaders)
                {
                    string headerValue = string.Empty;
                    if (TryGetValue("Content-Length", out headerValue))
                    {
                        return int.Parse((string)headerValue);
                    }
                    return -1;
                }
                else
                {
                    var response = GetResponse();
                    return response.OutputStream.Length;
                }
            }
        }

        public string ContentMd5
        {
            get
            {
                if (contentMD5 == null)
                {
                    if (TryGetValue("Content-MD5", out contentMD5))
                    {
                        return contentMD5;
                    }
                }
                return null;
            }

            #endregion
        }

        private bool IsRequestHeaders
        {
            get
            {
                if (isRequestHeaders == null)
                {
                    isRequestHeaders = this is HttpListenerRequestHeaders;
                }
                return isRequestHeaders.GetValueOrDefault();
            }
        }

        private void ThrowIfSetterNotSupported()
        {
            if (IsRequestHeaders)
                throw new NotSupportedException("This header cannot be set for requests.");
        }

        private HttpListenerRequest GetRequest()
        {
            if (response == null)
            {
                var headers = this as HttpListenerRequestHeaders;
                request = headers.Request;
            }
            return request;
        }

        private HttpListenerResponse GetResponse()
        {
            if (response == null)
            {
                var headers = this as HttpListenerResponseHeaders;
                response = headers.Response;
            }
            return response;
        }
    }
}