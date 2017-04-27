using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using QuickShare.Common;
using QuickShare.Common.Rome;
using Microsoft.AspNetCore.WebUtilities;

namespace QuickShare.FileTransfer
{
    public partial class ServerIPFinder
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

        public async Task<bool> StartFindingMyLocalIP(List<string> myIPs)
        {
            var key = RandomFunctions.RandomString(10);

            servers = StartListeners(myIPs, key);

            Dictionary<string, object> vs = new Dictionary<string, object>();
            vs.Add("Receiver", "ServerIPFinder");
            vs.Add("IPs", JsonConvert.SerializeObject(myIPs));
            vs.Add("DefaultMessage", webServerGenerator.GenerateInstance().DefaultRootPage());
            vs.Add("InterruptKey", key);

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
                item.Value.Dispose();
            }
            servers.Clear();
        }

        private string WebServerFetched(IWebServer sender, RequestDetails request)
        {
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

            IPDetectionCompleted?.Invoke(this, ea);

            return "success";
        }

        private List<KeyValuePair<string, IWebServer>> StartListeners(List<string> IPs, string communicationKey)
        {
            var servers = new List<KeyValuePair<string, IWebServer>>();

            foreach (var item in IPs)
            {
                IWebServer ws = webServerGenerator.GenerateInstance();
                ws.StartWebServer(item, Constants.CommunicationPort);

                ws.AddResponseUrl("/" + communicationKey + "/", (Func<IWebServer, RequestDetails, string>)WebServerFetched);

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
                await httpClient.GetAsync("http://" + senderIP + ":" + Constants.CommunicationPort.ToString() + "/" + key + "/");
            }
            catch { }
        }

        private static async Task NotifySender(string senderIP, string key, string additional)
        {
            var httpClient = new HttpClient();

            try
            {
                var response = await httpClient.GetAsync("http://" + senderIP + ":" + Constants.CommunicationPort.ToString() + "/" + key + "/?" + additional);
            }
            catch { }
        }

        private static async Task<bool> CheckIP(string ip, string expectedMessage)
        {
            var httpClient = new HttpClient();

            try
            {
                var response = await httpClient.GetAsync("http://" + ip + ":" + Constants.CommunicationPort.ToString() + "/");
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
    }
}
