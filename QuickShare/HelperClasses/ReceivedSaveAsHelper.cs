using PCLStorage;
using QuickShare.Common;
using QuickShare.DataStore;
using QuickShare.ToastNotifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace QuickShare.HelperClasses
{
    internal static class ReceivedSaveAsHelper
    {
        public delegate void ReceivedSaveAsProgressEventHandler(double percent);
        public static event ReceivedSaveAsProgressEventHandler SaveAsProgress;

        public static async Task SaveAs(Guid guid)
        {
            //Windows 10 Mobile has a bug causing PickSingleFolderAsync() to throw exception if called immediately after launch.
            if (DeviceInfo.FormFactorType == DeviceInfo.DeviceFormFactorType.Phone)
                await Task.Delay(TimeSpan.FromSeconds(1.5));

            FolderPicker fp = new FolderPicker()
            {
                SuggestedStartLocation = PickerLocationId.Downloads,
            };
            fp.FileTypeFilter.Add("*");
            var selectedFolder = await fp.PickSingleFolderAsync();

            var futureAccessList = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList;
            FutureAccessListHelper.MakeSureFutureAccessListIsNotFull();
            futureAccessList.AddOrReplace(selectedFolder.Path.Replace(":", "").Replace('\\', '/'), selectedFolder);

            try
            {
                Common.Classes.ReceivedSaveAsHelper.SaveAsProgress += ReceivedSaveAsHelper_SaveAsProgress;
                await Common.Classes.ReceivedSaveAsHelper.SaveAs(guid: guid,
                    selectedFolder: new WinRTFolder(selectedFolder),
                    defaultDownloadFolder: (await DownloadFolderHelper.GetDefaultDownloadFolderAsync()).Path,
                    pathToFileConverter: async path =>
                    {
                        return new WinRTFile(await StorageFile.GetFileFromPathAsync(path));
                    },
                    pathToFolderConverter: async path =>
                    {
                        return new WinRTFolder(await StorageFolder.GetFolderFromPathAsync(path));
                    });

                Toaster.ShowFileReceiveFinishedSavedAsNotification(guid);
            }
            catch (Common.Classes.SaveAsFailedException ex)
            {
                ToastFunctions.SendToast(ex.Message, ex.ExtraDetails);
            }
            finally
            {
                Common.Classes.ReceivedSaveAsHelper.SaveAsProgress -= ReceivedSaveAsHelper_SaveAsProgress;
            }
        }

        private static void ReceivedSaveAsHelper_SaveAsProgress(double percent)
        {
            SaveAsProgress?.Invoke(percent);
        }
    }
}
