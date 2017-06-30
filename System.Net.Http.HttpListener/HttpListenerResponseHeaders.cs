namespace System.Net.Http
{
    public sealed class HttpListenerResponseHeaders : HttpListenerHeaders
    {
        private HttpListenerHeaderValueCollection<string> contentEncoding;
        private Uri location;

        public HttpListenerResponseHeaders(HttpListenerResponse response)
        {
            Response = response;
        }

        public Uri Location
        {
            get
            {
                if (location == null)
                {
                    string locationString;
                    if (TryGetValue("Location", out locationString))
                    {
                        location = new Uri(locationString);
                    }
                }
                return location;
            }

            set
            {
                if (!value.Equals(location))
                {
                    location = value;
                    if (location == null)
                        return;
                    this["Location"] = location.ToString();
                }
            }
        }

        #region Content Headers

        public HttpListenerHeaderValueCollection<string> ContentEncoding => contentEncoding ?? (contentEncoding = new HttpListenerHeaderValueCollection<string>(this, "Content-Encoding"));

        internal HttpListenerResponse Response { get; set; }

        #endregion
    }
}