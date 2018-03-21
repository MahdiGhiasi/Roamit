using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using QuickShare.Common;
using QuickShare.Common.Rome;
using Microsoft.AspNetCore.WebUtilities;
using System.Diagnostics;

namespace QuickShare.FileTransfer
{
    public partial class ServerIPFinder : IDisposable
    {
        public delegate void IPDetectionCompletedEventHandler(object sender, IPDetectionCompletedEventArgs e);
        public event IPDetectionCompletedEventHandler IPDetectionCompleted;

        List<KeyValuePair<string, IWebServer>> servers;

        IWebServerGenerator webServerGenerator;
        IRomePackageManager packageManager;

        public ServerIPFinder(IWebServerGenerator _webServerGenerator, IRomePackageManager _packageManager)
        {
            webServerGenerator = _webServerGenerator;
            packageManager = _packageManager;
        }

        public async Task<bool> StartFindingMyLocalIP(IEnumerable<string> myIPs)
        {
            var key = RandomFunctions.RandomString(10);

            servers = StartListeners(myIPs, key);

            System.Diagnostics.Debug.WriteLine("Waiting...");

            Dictionary<string, object> vs = new Dictionary<string, object>
            {
                { "Receiver", "ServerIPFinder" },
                { "IPs", JsonConvert.SerializeObject(myIPs) },
                { "DefaultMessage", webServerGenerator.GenerateInstance().DefaultRootPage() },
                { "InterruptKey", key }
            };

            var response = await packageManager.Send(vs);
            if (response.Status == RomeAppServiceResponseStatus.Success)
            {
                return true;
            }

            return false;
        }

        private void StopListeners(List<KeyValuePair<string, IWebServer>> servers)
        {
            foreach (var item in servers)
            {
                item.Value.StopListener();
            }
            servers.Clear();
        }

        private string WebServerFetched(IWebServer sender, RequestDetails request)
        {
            Debug.WriteLine("ServerIPFinder: Fetched. Stopping listeners...");
            StopListeners(servers);

            IPDetectionCompletedEventArgs ea;
            try
            {
                var query = QueryHelpers.ParseQuery(request.Url.Query);
                
                var success = (query["success"][0].ToLower() == "true");
                var message = "";

                if (!success)
                    message = query["message"][0];

                ea = new IPDetectionCompletedEventArgs()
                {
                    Success = success,
                    Message = message,
                    MyIP = request.Host,
                    TargetIP = request.RemoteEndpointAddress
                };
            }
            catch (Exception ex)
            {
                ea = new IPDetectionCompletedEventArgs()
                {
                    Success = false,
                    Message = ex.Message,
                    MyIP = request.Host,
                    TargetIP = request.RemoteEndpointAddress
                };
            }

            Task.Run(() =>
            {
                IPDetectionCompleted?.Invoke(this, ea);
            });

            return "success";
        }

        private List<KeyValuePair<string, IWebServer>> StartListeners(IEnumerable<string> IPs, string communicationKey)
        {
            var servers = new List<KeyValuePair<string, IWebServer>>();

            foreach (var item in IPs)
            {
                IWebServer ws = webServerGenerator.GenerateInstance();
                ws.StartWebServer(item, Constants.IPFinderCommunicationPort);

                ws.AddResponseUrl("/" + communicationKey + "/", (Func<IWebServer, RequestDetails, string>)WebServerFetched);

                System.Diagnostics.Debug.WriteLine($"Started listener at {item}:{Constants.IPFinderCommunicationPort}. url is /{communicationKey}/");

                servers.Add(new KeyValuePair<string, IWebServer>(item, ws));
            }

            return servers;
        }

        static string senderIP = "";

        public static async Task ReceiveRequest(Dictionary<string, object> request)
        {
            string interruptKey = "";

            try
            {
                if (!request.ContainsKey("IPs"))
                    throw new Exception("Invalid request. (A)");
                if (!request.ContainsKey("DefaultMessage"))
                    throw new Exception("Invalid request. (B)");
                if (!request.ContainsKey("InterruptKey"))
                    throw new Exception("Invalid request. (C)");

                var expectedMessage = request["DefaultMessage"] as string;

                interruptKey = request["InterruptKey"] as string;

                var IPs = JsonConvert.DeserializeObject<List<string>>(request["IPs"] as string);

                List<Task<bool>> tasks = new List<Task<bool>>();

                foreach (var ip in IPs)
                    tasks.Add(CheckIP(ip, expectedMessage));

                var success = await tasks.LogicalAny();

                if (!success)
                    throw new Exception("Couldn't find the ip.");

                await NotifySender(senderIP, interruptKey, "success=true");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                if ((interruptKey != "") && (senderIP != ""))
                {
                    await NotifySender(senderIP, interruptKey, "success=false&message=" + System.Net.WebUtility.UrlEncode(ex.Message));
                }
            }
        }

        private static async Task NotifySender(string senderIP, string key)
        {
            var httpClient = new HttpClient();

            try
            {
                await httpClient.GetAsync("http://" + senderIP + ":" + Constants.IPFinderCommunicationPort.ToString() + "/" + key + "/");
            }
            catch { }
        }

        private static async Task NotifySender(string senderIP, string key, string additional)
        {
            var httpClient = new HttpClient();

            try
            {
                var response = await httpClient.GetAsync("http://" + senderIP + ":" + Constants.IPFinderCommunicationPort.ToString() + "/" + key + "/?" + additional);
            }
            catch { }
        }

        private static async Task<bool> CheckIP(string ip, string expectedMessage)
        {
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(3),
            };

            try
            {
                var response = await httpClient.GetAsync("http://" + ip + ":" + Constants.IPFinderCommunicationPort.ToString() + "/");
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    if (body == expectedMessage)
                    {
                        senderIP = ip;
                        System.Diagnostics.Debug.WriteLine(ip);
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (servers != null)
                foreach (var item in servers)
                {
                    item.Value.StopListener();
                    item.Value.Dispose();
                }
        }
    }
}
