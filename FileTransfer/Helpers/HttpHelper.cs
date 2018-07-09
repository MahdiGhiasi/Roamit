using QuickShare.FileTransfer;
using QuickShare.FileTransfer.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuickShare.FileTransfer.Helpers
{
    internal static class HttpHelper
    {
        internal static async Task<string> SendGetRequestAsync(string url, double timeout = 3.0, int maxTryCount = 3, CancellationToken cancellationToken = default(CancellationToken))
        {
            int tryCount = 0;
            while (true)
            {
                try
                {
                    tryCount++;

                    var httpClient = new HttpClient()
                    {
                        Timeout = TimeSpan.FromSeconds(timeout),
                    };
                    var response = await httpClient.GetAsync(url, cancellationToken);

                    if (!response.IsSuccessStatusCode)
                        throw new FailedToDownloadException(url, response.ReasonPhrase);

                    return await response.Content.ReadAsStringAsync();
                }
                catch (Exception ex)
                {
                    if (tryCount > maxTryCount)
                    {
                        if (ex is FailedToDownloadException)
                            throw ex;
                        else
                            throw new FailedToDownloadException(url, timeout, maxTryCount, ex);
                    }
                }
            }
        }

        internal static async Task<byte[]> DownloadDataFromUrl(string url, double timeout = 5.0, int maxTryCount = 3, CancellationToken cancellationToken = default(CancellationToken))
        {
            int tryCount = 0;

            var cancellationTcs = new TaskCompletionSource<byte[]>();
            var cancellationRegistration = cancellationToken.Register(s => ((TaskCompletionSource<byte[]>)s).SetResult(null), cancellationTcs);

            try
            {
                while (true)
                {
                    try
                    {
                        tryCount++;

                        HttpClient client = new HttpClient()
                        {
                            Timeout = TimeSpan.FromSeconds(timeout),
                        };

                        return await Task.WhenAny(client.GetByteArrayAsync(url), cancellationTcs.Task).Result;
                    }
                    catch (Exception ex)
                    {
                        if (tryCount > maxTryCount)
                            throw new FailedToDownloadException(url, timeout, maxTryCount, ex);
                    }
                }
            }
            finally
            {
                cancellationRegistration.Dispose();
            }
        }
    }
}
