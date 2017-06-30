namespace System.Net.Http
{
    public sealed class HttpListenerRequestEventArgs : EventArgs
    {
        internal HttpListenerRequestEventArgs(HttpListenerRequest request, HttpListenerResponse response)
        {
            Request = request;
            Response = response;
        }

        /// <summary>
        /// Gets the Request.
        /// </summary>
        public HttpListenerRequest Request { get; private set; }

        /// <summary>
        /// Gets the Response.
        /// </summary>
        public HttpListenerResponse Response { get; private set; }
    }
}
