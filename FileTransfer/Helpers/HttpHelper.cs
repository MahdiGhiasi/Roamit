using QuickShare.FileTransfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Helpers
{
    internal static class HttpHelper
    {
        internal static async Task<string> SendGetRequestAsync(string url)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
                return "";

            return await response.Content.ReadAsStringAsync();
        }

        internal static async Task<byte[]> DownloadDataFromUrl(string url, TimeSpan timeout, int maxTryCount)
        {
            int tryCount = 0;
            while (true)
            {
                try
                {
                    tryCount++;

                    HttpClient client = new HttpClient()
                    {
                        Timeout = timeout,
                    };

                    return await client.GetByteArrayAsync(url);
                }
                catch
                {
                    if (tryCount > maxTryCount)
                        throw new FailedToDownloadException();
                }
            }
        }
    }
}
