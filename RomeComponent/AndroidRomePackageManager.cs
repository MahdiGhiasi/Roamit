using QuickShare.Common;
using QuickShare.Common.Rome;
using QuickShare.DevicesListManager;
using QuickShare.Rome;
using QuickShare.UWP.Rome;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;

namespace QuickShare.UWP.Rome
{
    public class AndroidRomePackageManager : IRomePackageManager
    {
        //Singleton class
        static AndroidRomePackageManager _instance = null;
        public static AndroidRomePackageManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new AndroidRomePackageManager();

                return _instance;
            }
        }

        readonly int _maxRetryCount = 2;
        readonly double _maxSecondsForCarrier = 10.0;

        List<PackageManagerSendQueueItem> sendQueue = new List<PackageManagerSendQueueItem>();
        static SemaphoreSlim sendQueueSemaphore = new SemaphoreSlim(1, 1);

        NormalizedRemoteSystem nrs = null;
        string userId;
        List<string> whosNotMe;

        private AndroidRomePackageManager() { }

        public async Task MessageCarrierReceivedAsync(AppServiceRequest request)
        {
            int counter = 0;

            while (true)
            {
                await sendQueueSemaphore.WaitAsync();

                var queueItem = sendQueue.FirstOrDefault(x => x.RemoteSystemId == (string)request.Message["SenderId"]);

                if (queueItem == null)
                {
                    sendQueueSemaphore.Release();
                    Debug.WriteLine($"Queue is empty. Message Carrier is waiting for some message to arrive... {counter}");
                    await Task.Delay(1000);
                    counter++;
                    continue;
                }

                var vs = queueItem.Data.ToValueSet();

                var result = await request.SendResponseAsync(vs);

                queueItem.SetSendResult((RomeAppServiceResponseStatus)result);

                sendQueueSemaphore.Release();

                break;
            }
        }

        public async Task<RomeAppServiceResponse> Send(Dictionary<string, object> data)
        {
            return await Send(data, 0);
        }

        private async Task<RomeAppServiceResponse> Send(Dictionary<string, object> data, int tryCount)
        {
            var item = new PackageManagerSendQueueItem
            {
                RemoteSystemId = nrs.Id,
                Data = data,
            };

            await sendQueueSemaphore.WaitAsync();
            sendQueue.Add(item);
            sendQueueSemaphore.Release();

            TaskCompletionSource<RomeAppServiceResponseStatus> tcs = new TaskCompletionSource<RomeAppServiceResponseStatus>();

            item.SendFinished += (e) =>
            {
                tcs.SetResult(e.ResponseStatus);
            };

            Debug.WriteLine("Waiting for Message Carrier to arrive...");
            var result = await tcs.Task.WithTimeout(TimeSpan.FromSeconds(_maxSecondsForCarrier), RomeAppServiceResponseStatus.Unknown);

            //Timeout
            if (result == RomeAppServiceResponseStatus.Unknown)
            {
                if (tryCount < _maxRetryCount)
                {
                    Debug.WriteLine("Message Carrier timeout, will retry...");
                    return await Send(data, tryCount + 1);
                }
                else
                {
                    result = RomeAppServiceResponseStatus.RemoteSystemUnavailable;
                    Debug.WriteLine("Message Carrier didn't arrive :(");
                }
            }
            else
            {
                Debug.WriteLine("Message Carrier arrived.");
            }

            sendQueue.Remove(item);
            return new RomeAppServiceResponse
            {
                Message = new Dictionary<string, object>(),
                Status = result,
            };
        }

        public async Task<RomeAppServiceConnectionStatus> Connect(NormalizedRemoteSystem remoteSystem, string _userId, IEnumerable<string> _whosNotMe)
        {
            nrs = remoteSystem;
            userId = _userId;
            whosNotMe = new List<string>(_whosNotMe);

            return await Connect();
        }

        private async Task<RomeAppServiceConnectionStatus> Connect()
        {
            bool result = await Common.Service.DevicesLoader.RequestMessageCarrier(userId, nrs.Id, whosNotMe); 

            if (result)
                return RomeAppServiceConnectionStatus.Success;
            else
                return RomeAppServiceConnectionStatus.RemoteSystemUnavailable;
        }

        public static async Task<RomeRemoteLaunchUriStatus> LaunchStoreForApp(NormalizedRemoteSystem remoteSystem)
        {
            return await LaunchUri(new Uri(Common.Constants.GooglePlayAppUrl), remoteSystem);
        }

        public static async Task<RomeRemoteLaunchUriStatus> LaunchUri(Uri uri, NormalizedRemoteSystem remoteSystem)
        {
            throw new NotImplementedException();
        }
    }
}
