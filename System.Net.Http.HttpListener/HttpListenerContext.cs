namespace System.Net.Http
{
    public class HttpListenerContext
    {
        private readonly HttpListenerResponse response;

        public HttpListenerContext(HttpListenerRequest request, HttpListenerResponse response)
        {
            Request = request;
            this.response = response;
        }

        public HttpListenerRequest Request { get; }

        public HttpListenerResponse Response => response;
    }
}