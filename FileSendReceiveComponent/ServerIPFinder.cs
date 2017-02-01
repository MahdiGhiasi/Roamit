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

namespace QuickShare.FileSendReceive
{
    public class ServerIPFinder
    {
        //Singleton class
        static ServerIPFinder _instance = null;
        public static ServerIPFinder Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ServerIPFinder();

                return _instance;
            }
        }
        private ServerIPFinder() { }

        int clientAnswerStatus = 0;

        public delegate void IPDetectionCompletedEventHandler(object sender, IPDetectionCompletedEventArgs e);
        public event IPDetectionCompletedEventHandler IPDetectionCompleted;

        List<KeyValuePair<string, WebServer>> servers;

        public async Task<bool> StartFindingMyLocalIP()
        {
            var successKey = RandomFunctions.RandomString(10);

            List<string> IPs = FindMyIPAddresses().ToList();
            servers = StartListeners(IPs, successKey);

            ValueSet vs = new ValueSet();
            vs.Add("Receiver", "ServerIPFinder");
            vs.Add("IPs", JsonConvert.SerializeObject(IPs));
            vs.Add("DefaultMessage", WebServer.DefaultRootPage());
            vs.Add("SuccessKey", successKey);

            var response = await RomePackageManager.Instance.Send(vs);
            if (response.Status == Windows.ApplicationModel.AppService.AppServiceResponseStatus.Success)
            {
                return true;
            }

            return false;
        }

        private async void WaitForAnswer()
        {
            clientAnswerStatus = 0;

            await Task.Delay(TimeSpan.FromSeconds(10));

            if (clientAnswerStatus != 1)
            {
                //Failed
                clientAnswerStatus = 2;

                IPDetectionCompleted?.Invoke(this, new IPDetectionCompletedEventArgs
                {
                    IP = "",
                    Success = false
                });

                StopListeners(servers);
            }
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
            clientAnswerStatus = 1;

            IPDetectionCompleted?.Invoke(this, null);

            return "success";
        }

        private List<KeyValuePair<string, WebServer>> StartListeners(List<string> IPs, string successRandomString)
        {
            var servers = new List<KeyValuePair<string, WebServer>>();

            foreach (var item in IPs)
            {
                WebServer ws = new WebServer(item, 8081);

                ws.AddResponseUrl("/" + successRandomString + "/", (Func<WebServer, HttpListenerRequest, string>)WebServerFetched);

                servers.Add(new KeyValuePair<string, WebServer>(item, ws));
            }

            return servers;
        }

        string senderIP = "";
        public async Task ReceiveRequest(AppServiceRequest request)
        {
            ValueSet vs = new ValueSet();

            vs = new ValueSet();

            try
            {
                if (!request.Message.ContainsKey("IPs"))
                    throw new Exception("Invalid request. (A)");
                if (!request.Message.ContainsKey("DefaultMessage"))
                    throw new Exception("Invalid request. (B)");


                var expectedMessage = request.Message["DefaultMessage"] as string;

                var IPs = JsonConvert.DeserializeObject<List<string>>(request.Message["IPs"] as string);

                List<Task<bool>> tasks = new List<Task<bool>>();

                foreach (var ip in IPs)
                    tasks.Add(CheckIP(ip, expectedMessage));

                var success = await tasks.LogicalAny();

                if (!success)
                    throw new Exception("Couldn't find the ip.");

                System.Diagnostics.Debug.WriteLine(senderIP);

            }
            catch (Exception ex)
            {
                vs.Add("Exception", ex.Message);
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            //await RomePackageManager.Instance.Send(vs);
        }

        private async Task<bool> CheckIP(string ip, string expectedMessage)
        {
            var httpClient = new HttpClient();

            try
            {
                var response = await httpClient.GetAsync("http://" + ip + ":8081/");
                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine(body);
                    if (body == expectedMessage)
                    {
                        senderIP = ip;
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
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
