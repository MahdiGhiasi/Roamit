using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common.Rome
{
    public interface IRomePackageManager
    {
        void Initialize(string appServiceName);
        Task<RomeAppServiceConnectionStatus> Connect(object _remoteSystem, bool keepAlive);
        Task<RomeAppServiceConnectionStatus> Connect(object _remoteSystem, bool keepAlive, Uri wakeUri);
        Task<RomeRemoteLaunchUriStatus> LaunchStoreForApp();
        Task<RomeRemoteLaunchUriStatus> LaunchUri(Uri uri);
        Task<RomeRemoteLaunchUriStatus> LaunchUri(Uri uri, object remoteSystemOverride);
        Task<RomeAppServiceResponse> Send(Dictionary<string, object> data);
    }
}
