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

        List<PackageManagerSendQueueItem> sendQueue = new List<PackageManagerSendQueueItem>();
        static SemaphoreSlim sendQueueSemaphore = new SemaphoreSlim(1, 1);

        NormalizedRemoteSystem nrs = null;        

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
            var result = await tcs.Task;
            Debug.WriteLine("Message Carrier arrived.");

            sendQueue.Remove(item);
            return new RomeAppServiceResponse
            {
                Message = new Dictionary<string, object>(),
                Status = result,
            };
        }

        public async Task<RomeAppServiceConnectionStatus> Connect(NormalizedRemoteSystem remoteSystem, string userId, IEnumerable<string> whosNotMe)
        {
            nrs = remoteSystem;

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
