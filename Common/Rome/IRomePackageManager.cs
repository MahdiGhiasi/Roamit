using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common.Rome
{
    public interface IRomePackageManager
    {
        Task InitializeDiscovery();
        void Initialize(string appServiceName);
        Task<RomeAppServiceConnectionStatus> Connect(object _remoteSystem, bool keepAlive);
        Task<RomeRemoteLaunchUriStatus> LaunchStoreForApp();
        Task<RomeRemoteLaunchUriStatus> LaunchUri(Uri uri);
        Task<RomeAppServiceResponse> Send(Dictionary<string, object> data);
    }
}
