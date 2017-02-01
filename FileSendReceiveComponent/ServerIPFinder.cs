using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Networking.Connectivity;
using Windows.ApplicationModel.AppService;
using System.Net.Http;
using Windows.UI.Popups;
using QuickShare.Server;
using QuickShare.Rome;
using Windows.UI.Notifications;
using QuickShare.Common;
using Windows.Foundation;

namespace QuickShare.FileSendReceive
{
    public partial class ServerIPFinder
    {
        public delegate void IPDetectionCompletedEventHandler(object sender, IPDetectionCompletedEventArgs e);
        public event IPDetectionCompletedEventHandler IPDetectionCompleted;

        List<KeyValuePair<string, WebServer>> servers;

        public async Task<bool> StartFindingMyLocalIP()
        {
            var key = RandomFunctions.RandomString(10);

            List<string> IPs = FindMyIPAddresses().ToList();
            servers = StartListeners(IPs, key);

            ValueSet vs = new ValueSet();
            vs.Add("Receiver", "ServerIPFinder");
            vs.Add("IPs", JsonConvert.SerializeObject(IPs));
            vs.Add("DefaultMessage", WebServer.DefaultRootPage());
            vs.Add("InterruptKey", key);

            var response = await RomePackageManager.Instance.Send(vs);
            if (response.Status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success)
            {
                return true;
            }

            return false;
        }

        private void StopListeners(List<KeyValuePair<string, WebServer>> servers)
        {
            foreach (var item in servers)
            {
                item.Value.Dispose();
            }
            servers.Clear();
        }

        private string WebServerFetched(WebServer sender, HttpListenerRequest request)
        {
            StopListeners(servers);

            IPDetectionCompletedEventArgs ea;
            try
            {
                var query = new WwwFormUrlDecoder(request.Url.Query);

                var success = (query.GetFirstValueByName("success").ToLower() == "true");
                var message = "";

                if (!success)
                    message = query.GetFirstValueByName("message");

                ea = new IPDetectionCompletedEventArgs()
                {
                    Success = success,
                    Message = message,
                    MyIP = request.Host,
                    TargetIP = request.RemoteEndpoint.Address.ToString()
                };
            }
            catch (Exception ex)
            {
                ea = new IPDetectionCompletedEventArgs()
                {
                    Success = false,
                    Message = ex.Message,
                    MyIP = request.Host,
                    TargetIP = request.RemoteEndpoint.Address.ToString()
                };
            }

#pragma warning disable CS4014
            DispatcherEx.RunOnCoreDispatcherIfPossible(() =>
            {
                IPDetectionCompleted?.Invoke(this, ea);
            });
#pragma warning restore CS4014

            return "success";
        }

        private List<KeyValuePair<string, WebServer>> StartListeners(List<string> IPs, string communicationKey)
        {
            var servers = new List<KeyValuePair<string, WebServer>>();

            foreach (var item in IPs)
            {
                WebServer ws = new WebServer(item, Constants.CommunicationPort);

                ws.AddResponseUrl("/" + communicationKey + "/", (Func<WebServer, HttpListenerRequest, string>)WebServerFetched);

                servers.Add(new KeyValuePair<string, WebServer>(item, ws));
            }

            return servers;
        }

        static string senderIP = "";

        public static async Task ReceiveRequest(AppServiceRequest request)
        {
            string interruptKey = "";

            try
            {
                if (!request.Message.ContainsKey("IPs"))
                    throw new Exception("Invalid request. (A)");
                if (!request.Message.ContainsKey("DefaultMessage"))
                    throw new Exception("Invalid request. (B)");
                if (!request.Message.ContainsKey("InterruptKey"))
                    throw new Exception("Invalid request. (C)");

                var expectedMessage = request.Message["DefaultMessage"] as string;

                interruptKey = request.Message["InterruptKey"] as string;

                var IPs = JsonConvert.DeserializeObject<List<string>>(request.Message["IPs"] as string);

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

        public static IEnumerable<string> FindMyIPAddresses()
        {
            List<string> ipAddresses = new List<string>();
            var hostnames = NetworkInformation.GetHostNames();
            foreach (var hn in hostnames)
            {
                if (hn.Type == Windows.Networking.HostNameType.Ipv4)
                {
                    //IanaInterfaceType == 71 => Wifi
                    //IanaInterfaceType == 6 => Ethernet (Emulator)
                    if (hn.IPInformation != null &&
                    (hn.IPInformation.NetworkAdapter.IanaInterfaceType == 71
                    || hn.IPInformation.NetworkAdapter.IanaInterfaceType == 6))
                    {
                        string ipAddress = hn.DisplayName;
                        ipAddresses.Add(ipAddress);
                    }
                }
            }

            return ipAddresses;
        }
    }
}
