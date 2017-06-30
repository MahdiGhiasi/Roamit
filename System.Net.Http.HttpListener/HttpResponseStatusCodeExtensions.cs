namespace System.Net.Http
{
    public static class HttpResponseStatusCodeExtensions
    {
        public static void NotFound(this HttpListenerResponse response)
        {
            response.StatusCode = 404;
            response.ReasonPhrase = "Not Found";
        }

        public static void InternalServerError(this HttpListenerResponse response)
        {
            response.StatusCode = 500;
            response.ReasonPhrase = "Internal Server Error";
        }

        public static void MethodNotAllowed(this HttpListenerResponse response)
        {
            response.StatusCode = 405;
            response.ReasonPhrase = "Method Not Allowed";
        }

        public static void NotImplemented(this HttpListenerResponse response)
        {
            response.StatusCode = 501;
            response.ReasonPhrase = "Not Implemented";
        }

        public static void Forbidden(this HttpListenerResponse response)
        {
            response.StatusCode = 403;
            response.ReasonPhrase = "Forbidden";
        }

    }
}
