namespace System.Net.Http
{
    public sealed class HttpListenerRequestHeaders : HttpListenerHeaders
    {
        private HttpListenerHeaderValueCollection<string> accept;
        private HttpListenerHeaderValueCollection<string> acceptCharset;
        private HttpListenerHeaderValueCollection<string> acceptLanguage;
        private HttpListenerHeaderValueCollection<string> acceptEncoding;
        private DateTime accepDatetime;
        private string host;

        public HttpListenerRequestHeaders(HttpListenerRequest request)
        {
            Request = request;
        }

        public string Host
        {
            get
            {
                if (host == null)
                {
                    var hostString = string.Empty;
                    if (TryGetValue("Host", out hostString))
                    {
                        host = hostString;
                    }
                }
                return host;
            }
        }

        #region Accept Headers

        public HttpListenerHeaderValueCollection<string> Accept
        {
            get
            {
                if (accept == null)
                {
                    accept = new HttpListenerHeaderValueCollection<string>(this, "Accept");
                }
                return accept;
            }
        }

        public HttpListenerHeaderValueCollection<string> AcceptEncoding
        {
            get
            {
                if (acceptEncoding == null)
                {
                    acceptEncoding = new HttpListenerHeaderValueCollection<string>(this, "Accept-Encoding");
                }
                return acceptEncoding;
            }
        }

        public HttpListenerHeaderValueCollection<string> AcceptCharset
        {
            get
            {
                if (acceptCharset == null)
                {
                    acceptCharset = new HttpListenerHeaderValueCollection<string>(this, "Accept-Charset");
                }
                return acceptCharset;
            }
        }

        public HttpListenerHeaderValueCollection<string> AcceptLanguage
        {
            get
            {
                if (acceptLanguage == null)
                {
                    acceptLanguage = new HttpListenerHeaderValueCollection<string>(this, "Accept-Language");
                }
                return acceptLanguage;
            }
        }

        public DateTime AcceptDateTime
        {
            get
            {
                if (accepDatetime == default(DateTime))
                {
                    string headerValue = string.Empty;
                    if(TryGetValue("Accept-Datetime", out headerValue))
                    {
                        accepDatetime = DateTime.Parse(headerValue);
                    }
                }
                return accepDatetime;
            }
        }

        internal HttpListenerRequest Request { get; set; }

        #endregion
    }
}
