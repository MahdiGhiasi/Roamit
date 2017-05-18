using QuickShare.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Droid.WebServerComponent
{
    public class WebServer : IWebServer
    {
        HttpListener listener;
        Dictionary<string, object> Urls = new Dictionary<string, object>();

        string ip;
        int port;

        public void StartWebServer(string _ip, int _port)
        {
            ip = _ip;
            port = _port;

            listener = new HttpListener();
            listener.Prefixes.Add($"http://{_ip}:{_port.ToString()}/");

            listener.Start();
            listener.BeginGetContext(HandleRequest, listener);

            ClearResponseUrls();
        }

        Random rand = new Random();

        private async void HandleRequest(IAsyncResult result)
        {
            HttpListenerContext context = null;
            try
            {
                context = listener.EndGetContext(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to process request. {ex.Message}");
                return;
            }

            //string response = "<html>Hello World " + rand.Next(9999) + "</html>";

            System.Diagnostics.Debug.WriteLine($"Received request to {context.Request.Url.AbsolutePath}");

            byte[] buffer;

            if (Urls.ContainsKey(context.Request.Url.AbsolutePath))
            {
                RequestDetails rd = new RequestDetails
                {
                    Headers = context.Request.Headers.AllKeys.ToDictionary(x => x, x => context.Request.Headers[x]),
                    Host = context.Request.Url.Host,
                    HttpMethod = context.Request.HttpMethod,
                    InputStream = context.Request.InputStream,
                    KeepAlive = context.Request.KeepAlive,
                    ProtocolVersion = context.Request.ProtocolVersion.ToString(),
                    RemoteEndpointAddress = context.Request.RemoteEndPoint.Address.ToString(),
                    Url = context.Request.Url,
                };

                var value = Urls[context.Request.Url.AbsolutePath];
                if (value is string)
                {
                    buffer = Encoding.UTF8.GetBytes(value as string);
                }
                else if (value is byte[])
                {
                    buffer = (byte[])value;
                }
                else if (value is Func<IWebServer, RequestDetails, string>)
                {
                    var output = ((Func<IWebServer, RequestDetails, string>)value).Invoke(this, rd);
                    buffer = Encoding.UTF8.GetBytes(output);
                }
                else if (value is Func<IWebServer, RequestDetails, byte[]>)
                {
                    buffer = ((Func<IWebServer, RequestDetails, byte[]>)value).Invoke(this, rd);
                }
                else if (value is Func<IWebServer, RequestDetails, Task<string>>)
                {
                    var output = await((Func<IWebServer, RequestDetails, Task<string>>)value).Invoke(this, rd);
                    buffer = Encoding.UTF8.GetBytes(output);
                }
                else if (value is Func<IWebServer, RequestDetails, Task<byte[]>>)
                {
                    buffer = await((Func<IWebServer, RequestDetails, Task<byte[]>>)value).Invoke(this, rd);
                }
                else
                {
                    buffer = Encoding.UTF8.GetBytes("<html><body>Invalid url handler.</body></html>");
                }
            }
            else
            {
                buffer = Encoding.UTF8.GetBytes("<html><body>Invalid Request.</body></html>");
            }

            try
            {
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.OutputStream.Close();

                listener.BeginGetContext(HandleRequest, listener);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to send response to request {context.Request.Url.AbsolutePath} - {ex.Message}");
            }
        }

        public void AddResponseUrl(string url, string response) { AddResponseUrlInternal(url, response); }
        public void AddResponseUrl(string url, byte[] response) { AddResponseUrlInternal(url, response); }
        public void AddResponseUrl(string url, Func<IWebServer, RequestDetails, string> response) { AddResponseUrlInternal(url, response); }
        public void AddResponseUrl(string url, Func<IWebServer, RequestDetails, byte[]> response) { AddResponseUrlInternal(url, response); }
        public void AddResponseUrl(string url, Func<IWebServer, RequestDetails, Task<string>> response) { AddResponseUrlInternal(url, response); }
        public void AddResponseUrl(string url, Func<IWebServer, RequestDetails, Task<byte[]>> response) { AddResponseUrlInternal(url, response); }

        private void AddResponseUrlInternal(string url, object response)
        {
            if (Urls.ContainsKey(url))
                Urls.Remove(url);

            Urls.Add(url, response);
        }

        public void ClearResponseUrls()
        {
            Urls.Clear();
            AddRootPage();
        }

        public void RemoveResponseUrl(string url)
        {
            Urls.Remove(url);
        }

        private void AddRootPage()
        {
            AddResponseUrl("/", DefaultRootPage());
        }

        public string DefaultRootPage()
        {
            return "<html><head><title>QuickShare</title></head><body><h3>Hello from QuickShare :)</h3></body></html>";
        }

        public void Dispose()
        {
            listener.Close();
        }
    }
}
