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

namespace QuickShare.Windows
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
        public void AddResponseUrl(string url, Func<IWebServer, HttpRequest, string> response) { AddResponseUrlInternal(url, response); }
        public void AddResponseUrl(string url, Func<IWebServer, HttpRequest, byte[]> response) { AddResponseUrlInternal(url, response); }
        public void AddResponseUrl(string url, Func<IWebServer, HttpRequest, Task<string>> response) { AddResponseUrlInternal(url, response); }
        public void AddResponseUrl(string url, Func<IWebServer, HttpRequest, Task<byte[]>> response) { AddResponseUrlInternal(url, response); }

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
                else if (value is Func<WebServer, HttpListenerRequest, string>)
                {
                    var output = ((Func<WebServer, HttpListenerRequest, string>)value).Invoke(this, e.Request);
                    await e.Response.WriteAsync(output);
                }
                else if (value is Func<WebServer, HttpListenerRequest, byte[]>)
                {
                    var output = ((Func<WebServer, HttpListenerRequest, byte[]>)value).Invoke(this, e.Request);
                    await e.Response.OutputStream.WriteAsync(output, 0, output.Count());
                }
                else if (value is Func<WebServer, HttpListenerRequest, Task<string>>)
                {
                    var output = await ((Func<WebServer, HttpListenerRequest, Task<string>>)value).Invoke(this, e.Request);
                    await e.Response.WriteAsync(output);
                }
                else if (value is Func<WebServer, HttpListenerRequest, Task<byte[]>>)
                {
                    var output = await ((Func<WebServer, HttpListenerRequest, Task<byte[]>>)value).Invoke(this, e.Request);
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
