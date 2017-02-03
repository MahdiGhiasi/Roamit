using QuickShare.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Storage;

namespace QuickShare.FileSendReceive
{
    public static class FileReceiver
    {
        static List<string> downloading = new List<string>();

        public delegate void ReceiveFileProgressEventHandler(FileTransferProgressEventArgs e);
        public static event ReceiveFileProgressEventHandler FileTransferProgress;

        public static async Task ReceiveRequest(AppServiceRequest request)
        {
            var key = (string)request.Message["DownloadKey"];

            if (downloading.Contains(key))
                return;

            downloading.Add(key);

            Debug.WriteLine("Receive begin");

            var slicesCount = (uint)request.Message["SlicesCount"];
            var fileName = (string)request.Message["FileName"];
            var dateModifiedMilliseconds = (long)request.Message["DateModified"];
            var dateCreatedMilliseconds = (long)request.Message["DateCreated"];
            var fileSize = (ulong)request.Message["FileSize"];
            var directory = (string)request.Message["Directory"];
            var serverIP = (string)request.Message["ServerIP"];

            var dateModified = DateTimeOffset.FromUnixTimeMilliseconds(dateModifiedMilliseconds).LocalDateTime;
            var dateCreated = DateTimeOffset.FromUnixTimeMilliseconds(dateCreatedMilliseconds).LocalDateTime;

            var futureAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;

            StorageFolder downloadMainFolder;
            if (!futureAccessList.ContainsItem("downloadMainFolder"))
            {
                await ReceiveFailed(serverIP, key, "Default downloads folder hasn't been set.");
                return;
            }

            downloadMainFolder = await futureAccessList.GetItemAsync("downloadMainFolder") as StorageFolder;

            StorageFolder downloadFolder = await CreateDirectoryIfNecessary(downloadMainFolder, directory);

            StorageFile file = await CreateFile(downloadFolder, fileName);

            using (var stream = (await downloadFolder.OpenStreamForWriteAsync(file.Name, CreationCollisionOption.OpenIfExists)))
            {
                for (uint i = 0; i < slicesCount; i++)
                {
                    string url = "http://" + serverIP + ":" + Constants.CommunicationPort.ToString() + "/" + key + "/" + i + "/";

                    byte[] buffer = await DownloadDataFromUrl(url);

                    await stream.WriteAsync(buffer, 0, buffer.Length);

                    FileTransferProgress?.Invoke(new FileTransferProgressEventArgs { CurrentPart = i + 1, Total = slicesCount});
                    System.Diagnostics.Debug.WriteLine((i + 1) + " / " + slicesCount);
                }

                await stream.FlushAsync();
            }

            //Debug.WriteLine(dateModified);
            //Debug.WriteLine(dateCreated);

            //System.IO.File.SetLastWriteTime(file.Path, dateModified);
            //System.IO.File.SetCreationTime(file.Path, dateCreated);

            downloading.Remove(key);

            await ReceiveSuccessful(serverIP, key);
        }

        private static async Task<byte[]> DownloadDataFromUrl(string url)
        {
            HttpClient client = new HttpClient();

            Debug.WriteLine("Downloading " + url);

            return await client.GetByteArrayAsync(url);
        }

        private static async Task<StorageFile> CreateFile(StorageFolder downloadFolder, string fileName)
        {
            return await downloadFolder.CreateFileAsync(fileName, CreationCollisionOption.GenerateUniqueName);
        }

        private static async Task<StorageFolder> CreateDirectoryIfNecessary(StorageFolder downloadMainFolder, string directory)
        {
            string[] directories = directory.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

            if (directories.Count() == 0)
                return downloadMainFolder;

            throw new NotImplementedException();
        }

        private static async Task ReceiveFailed(string serverIP, string key, string message)
        {
            var httpClient = new HttpClient();

            try
            {
                await httpClient.GetAsync("http://" + serverIP + ":" + Constants.CommunicationPort.ToString() + "/" + key + "/finish/?success=false&message=" + System.Net.WebUtility.UrlEncode(message));
            }
            catch { }
        }

        private static async Task ReceiveSuccessful(string serverIP, string key)
        {
            var httpClient = new HttpClient();

            try
            {
                await httpClient.GetAsync("http://" + serverIP + ":" + Constants.CommunicationPort.ToString() + "/" + key + "/finish/?success=true");
            }
            catch { }
        }
    }
}
