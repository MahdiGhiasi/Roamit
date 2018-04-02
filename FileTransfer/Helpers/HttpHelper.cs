using QuickShare.FileTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.FileTransfer.Helpers
{
    internal static class HttpHelper
    {
        internal static async Task<string> SendGetRequestAsync(string url, double timeout = 3.0, int maxTryCount = 3)
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
                    var response = await httpClient.GetAsync(url);

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

        internal static async Task<byte[]> DownloadDataFromUrl(string url, double timeout = 5.0, int maxTryCount = 3)
        {
            int tryCount = 0;
            while (true)
            {
                try
                {
                    tryCount++;

                    HttpClient client = new HttpClient()
                    {
                        Timeout = TimeSpan.FromSeconds(timeout),
                    };

                    return await client.GetByteArrayAsync(url);
                }
                catch (Exception ex)
                {
                    if (tryCount > maxTryCount)
                        throw new FailedToDownloadException(url, timeout, maxTryCount, ex);
                }
            }
        }
    }
}
