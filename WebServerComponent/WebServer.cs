using QuickShare.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.Storage.Streams;

namespace QuickShare.UWP
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

            listener = new HttpListener(IPAddress.Parse(_ip), _port);
            listener.Request += Listener_Request;
            listener.Start();

            AddRootPage();
        }

        public string DefaultRootPage()
        {
            return "<html><head><title>QuickShare</title></head><body><h3>Hello from QuickShare :)</h3></body></html>";
        }

        private void AddRootPage()
        {
            AddResponseUrl("/", DefaultRootPage());
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

        private async void Listener_Request(object sender, HttpListenerRequestEventArgs e)
        {
            if (Urls.ContainsKey(e.Request.Url.AbsolutePath))
            {
                RequestDetails rd = new RequestDetails
                {
                    Headers = new Dictionary<string, string>(e.Request.Headers.Select(x => new KeyValuePair<string, string>(x.Key, x.Value.ToString())).ToDictionary(x => x.Key, x => x.Value)),
                    Host = e.Request.Host,
                    HttpMethod = e.Request.HttpMethod,
                    InputStream = e.Request.InputStream,
                    KeepAlive = e.Request.KeepAlive,
                    ProtocolVersion = e.Request.ProtocolVersion,
                    RemoteEndpointAddress = e.Request.RemoteEndpoint.Address.ToString(),
                    Url = e.Request.Url,
                };

                var value = Urls[e.Request.Url.AbsolutePath];
                if (value is string)
                {
                    await e.Response.WriteAsync(value as string);
                }
                else if (value is byte[])
                {
                    byte[] b = (byte[])value;
                    await e.Response.OutputStream.WriteAsync(b, 0, b.Count());
                }
                else if (value is Func<IWebServer, RequestDetails, string>)
                {
                    var output = ((Func<IWebServer, RequestDetails, string>)value).Invoke(this, rd);
                    await e.Response.WriteAsync(output);
                }
                else if (value is Func<IWebServer, RequestDetails, byte[]>)
                {
                    var output = ((Func<IWebServer, RequestDetails, byte[]>)value).Invoke(this, rd);
                    await e.Response.OutputStream.WriteAsync(output, 0, output.Count());
                }
                else if (value is Func<IWebServer, RequestDetails, Task<string>>)
                {
                    var output = await ((Func<IWebServer, RequestDetails, Task<string>>)value).Invoke(this, rd);
                    await e.Response.WriteAsync(output);
                }
                else if (value is Func<IWebServer, RequestDetails, Task<byte[]>>)
                {
                    var output = await ((Func<IWebServer, RequestDetails, Task<byte[]>>)value).Invoke(this, rd);
                    await e.Response.OutputStream.WriteAsync(output, 0, output.Count());
                }
                else
                {
                    await e.Response.WriteAsync("<html><body>Invalid url handler.</body></html>");
                }
            }
            else
            {
                await e.Response.WriteAsync("<html><body>Invalid Request.</body></html>");
            }

            e.Response.Close();
        }

        public void Dispose()
        {
            try
            {
                listener.Close();
            }
            catch { }
        }

    }
}
