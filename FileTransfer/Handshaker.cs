using QuickShare.Common;
using QuickShare.Common.Rome;
using QuickShare.FileTransfer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileTransfer
{
    internal class Handshaker : IDisposable
    {
        readonly TimeSpan handshakeTimeout = TimeSpan.FromSeconds(6);

        private TaskCompletionSource<bool> ipFinderTcs;

        private IEnumerable<string> myIPs;

        private IPDetectionCompletedEventArgs ipFinderResult = null;
        private ServerIPFinder ipFinder;

        public Handshaker(IWebServerGenerator webServerGenerator, IRomePackageManager packageManager, IEnumerable<string> myIPs)
        {
            this.myIPs = myIPs;

            ipFinder = new ServerIPFinder(webServerGenerator, packageManager);
            ipFinder.IPDetectionCompleted += IpFinder_IPDetectionCompleted;
        }

        public void Dispose()
        {
            ipFinder.Dispose();
        }

        /// <summary>
        /// Handshakes with the remote device and returns the IP of current device as seen by remote device.
        /// </summary>
        /// <returns>Returns the IP of current device as seen by remote device.</returns>
        public async Task<string> Handshake(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (myIPs.Count() == 1)
            {
                Debug.WriteLine("Device has only one IP, so IPFinder will not be called.");
                return myIPs.First();
            }
            else if (ipFinderResult != null && ipFinderResult.Success)
            {
                return ipFinderResult.MyIP;
            }
            else
            {
                ipFinderTcs = new TaskCompletionSource<bool>();

                var registration = cancellationToken.Register(() =>
                {
                    ipFinderTcs.TrySetResult(false);
                });

                try
                {
                    if (await ipFinder.StartFindingMyLocalIP(myIPs))
                    {
                        var handshakeResult = await ipFinderTcs.Task.WithTimeout(handshakeTimeout);
                        if (handshakeResult == true && ipFinderResult != null)
                        {
                            return ipFinderResult.MyIP;
                        }
                    }

                    Debug.WriteLine("Sending handshake message failed.");
                    return "";
                }
                finally
                {
                    registration.Dispose();
                }
            }
        }

        private void IpFinder_IPDetectionCompleted(object sender, IPDetectionCompletedEventArgs e)
        {
            Debug.WriteLine("IpFinder_IPDetectionCompleted.");
            ipFinderResult = e;
            ipFinderTcs.TrySetResult(true);
        }
    }
}
