using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common
{
    public interface IWebServer : IDisposable
    {
        void StartWebServer(string _ip, int _port);
        string DefaultRootPage();
        
        void AddResponseUrl(string url, string response);
        void AddResponseUrl(string url, byte[] response);
        void AddResponseUrl(string url, Func<IWebServer, RequestDetails, string> response);
        void AddResponseUrl(string url, Func<IWebServer, RequestDetails, byte[]> response);
        void AddResponseUrl(string url, Func<IWebServer, RequestDetails, Task<string>> response);
        void AddResponseUrl(string url, Func<IWebServer, RequestDetails, Task<byte[]>> response);
        
        void ClearResponseUrls();
        void RemoveResponseUrl(string url);

        void StopListener();
    }
}
