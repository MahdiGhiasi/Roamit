using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using QuickShare.TextTransfer;
using Plugin.FilePicker;
using Android.Provider;
using Android.Database;
using Com.Github.Angads25.Filepicker.Model;
using Com.Github.Angads25.Filepicker.View;
using QuickShare.FileTransfer;
using System.Threading;
using Plugin.DeviceInfo;
using QuickShare.Droid.Helpers;
using Android.Views.Animations;

namespace QuickShare.Droid
{
    [Activity(Label = "SendPageActivity", Name = "com.ghiasi.quickshare.sendpage")]
    public class SendPageActivity : Activity
    {
        TextView sendStatus, sendProgressPercent;
        ProgressBar sendProgress, sendProgressIndeterminate;

        public static readonly int PickImageId = 1000;

        internal static bool IsInitialized = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.SendPage);

            string contentType = Intent.GetStringExtra("ContentType") ?? "Unknown";

            sendStatus = FindViewById<TextView>(Resource.Id.sendStatus);
            sendProgress = FindViewById<ProgressBar>(Resource.Id.sendProgress);
            sendProgressIndeterminate = FindViewById<ProgressBar>(Resource.Id.sendProgressIndeterminate);
            sendProgressPercent = FindViewById<TextView>(Resource.Id.sendProgressPercent);

            if (IsInitialized)
                return;
            IsInitialized = true;

            InitSpinner();

            ProcessRequest(contentType);
        }

        private void InitSpinner()
        {
            var rotation = AnimationUtils.LoadAnimation(this, Resource.Animation.rotate);
            rotation.FillAfter = true;
            sendProgressIndeterminate.StartAnimation(rotation);

            sendProgress.Visibility = ViewStates.Invisible;
            sendProgressPercent.Visibility = ViewStates.Invisible;
            sendProgressIndeterminate.Visibility = ViewStates.Visible;
        }

        private async void ProcessRequest(string contentType)
        {
            switch (contentType)
            {
                case "Clipboard":
                    await SendText(GetClipboardText());
                    break;
                case "Picture":
                    await PickAndSendPicture();
                    break;
                case "File":
                    await PickAndSendFile();
                    break;
                case "Url":
                    await OpenUrl(GetClipboardText());
                    break;
                case "Share_File":
                    await SendFiles(Common.ShareFiles);
                    break;
                case "Share_Url":
                    await OpenUrl(Common.ShareText);
                    break;
                case "Share_Text":
                    await SendText(Common.ShareText);
                    break;
                case "Unknown":
                default:

                    break;
            }
        }

        private string GetFilePath(Android.Net.Uri uri)
        {
            string[] proj = { MediaStore.Images.ImageColumns.Data };
            var cursor = ManagedQuery(uri, proj, null, null, null);
            var colIndex = cursor.GetColumnIndex(MediaStore.Images.ImageColumns.Data);
            cursor.MoveToFirst();
            return cursor.GetString(colIndex);
        }

        private string GetRealPathFromURI(Android.Net.Uri contentURI)
        {
            ICursor cursor = ContentResolver.Query(contentURI, null, null, null, null);
            cursor.MoveToFirst();
            string documentId = cursor.GetString(0);
            documentId = documentId.Split(':')[1];
            cursor.Close();

            cursor = ContentResolver.Query(
            Android.Provider.MediaStore.Images.Media.ExternalContentUri,
            null, MediaStore.Images.Media.InterfaceConsts.Id + " = ? ", new[] { documentId }, null);
            cursor.MoveToFirst();
            string path = cursor.GetString(cursor.GetColumnIndex(MediaStore.Images.Media.InterfaceConsts.Data));
            cursor.Close();

            return path;
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if ((requestCode == PickImageId) && (resultCode == Result.Ok) && (data != null))
            {
                Android.Net.Uri uri = data.Data;
                Toast.MakeText(this, uri.Path, ToastLength.Long);

                //FindViewById<ImageView>(Resource.Id.previewImageView).SetImageURI(uri);
                //System.Diagnostics.Debug.WriteLine(GetFilePath(uri));
                //System.Diagnostics.Debug.WriteLine(GetRealPathFromURI(uri));
            }
        }

        private async Task PickAndSendPicture()
        {
            Intent = new Intent();
            Intent.SetType("*/*");
            Intent.SetAction(Intent.ActionGetContent);
            StartActivityForResult(Intent.CreateChooser(Intent, "Select Picture"), PickImageId);
        }

        private async Task PickAndSendFile()
        {
            //TODO: check permission.

            var filePickerTask = new TaskCompletionSource<string[]>();

            DialogProperties properties = new DialogProperties()
            {
                SelectionMode = DialogConfigs.MultiMode,
                SelectionType = DialogConfigs.FileSelect,
                Root = new Java.IO.File(DialogConfigs.DefaultDir),
                ErrorDir = new Java.IO.File(DialogConfigs.DefaultDir),
                Offset = new Java.IO.File(DialogConfigs.DefaultDir),
                Extensions = null
            };

            FilePickerDialog dialog = new FilePickerDialog(this, properties);
            dialog.SetTitle("Select files");
            dialog.DialogSelection += (ss, ee) =>
            {
                filePickerTask.TrySetResult(ee.P0);
            };
            dialog.DismissEvent += (ss, ee) =>
            {
                filePickerTask.TrySetResult(new string[] { });
            };
            dialog.Show();

            string[] files = await filePickerTask.Task;

            await SendFiles(files);
        }

        private async Task SendFiles(string[] files)
        {
            sendStatus.Text = "Connecting...";

            var result = await Common.PackageManager.Connect(Common.GetCurrentRemoteSystem(), false);

            if (result != QuickShare.Common.Rome.RomeAppServiceConnectionStatus.Success)
            {
                sendStatus.Text = $"Connect failed. ({result.ToString()})";
                return;
            }

            //Fix Rome Android bug (receiver app service closes after 5 seconds in first connection)
            Common.PackageManager.CloseAppService();
            result = await Common.PackageManager.Connect(Common.GetCurrentRemoteSystem(), false);

            if (result != QuickShare.Common.Rome.RomeAppServiceConnectionStatus.Success)
            {
                sendStatus.Text = $"Connect failed. ({result.ToString()})";
                return;
            }

            string sendingText = (files.Length == 1) ? "Sending file..." : "Sending files...";
            sendStatus.Text = "Preparing...";

            bool failed = false;
            string message = "";

            using (FileSender fs = new FileSender(Common.GetCurrentRemoteSystem(),
                                                  new WebServerComponent.WebServerGenerator(),
                                                  Common.PackageManager,
                                                  FindMyIPAddresses(),
                                                  CrossDeviceInfo.Current.Model))
            {
                sendProgress.Max = 1;
                fs.FileTransferProgress += (ss, ee) =>
                {
                    if (ee.State == FileTransferState.Error)
                    {
                        failed = true;
                        message = ee.Message;
                    }
                    else
                    {
                        RunOnUiThread(() =>
                        {
                            sendStatus.Text = sendingText;

                            SetProgressBarValue((int)ee.CurrentPart, (int)ee.Total + 1);
                        });
                    }
                };

                if (files.Length == 0)
                {
                    sendStatus.Text = "No files.";
                    //ViewModel.ProgressIsIndeterminate = false;
                    return;
                }
                else if (files.Length == 1)
                {
                    await Task.Run(async () =>
                    {
                        if (!await fs.SendFile(new PCLStorage.FileSystemFile(files[0])))
                            failed = true;
                    });
                }
                else
                {
                    await Task.Run(async () =>
                    {
                        if (!await fs.SendFiles(from x in files
                                                select new PCLStorage.FileSystemFile(x), DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + "\\"))
                            failed = true;
                    });
                }

                sendProgress.Progress = sendProgress.Max;
            }

            Dictionary<string, object> vs = new Dictionary<string, object>
            {
                { "Receiver", "System" },
                { "FinishService", "FinishService" },
            };
            await Common.PackageManager.Send(vs);

            SetProgressBarValueToMax();

            if (failed)
            {
                sendStatus.Text = "Failed.";
                System.Diagnostics.Debug.WriteLine("Send failed.\r\n\r\n" + message);
                //await (new MessageDialog("Send failed.\r\n\r\n" + message)).ShowAsync();
            }
            else
            {
                sendStatus.Text = "Finished.";
            }
        }

        private IEnumerable<string> FindMyIPAddresses()
        {
            return new string[] { NetworkHelper.GetLocalIPAddress() };
        }

        private string GetClipboardText()
        {
            ClipboardManager clipboard = (ClipboardManager)GetSystemService(Context.ClipboardService);
            return clipboard.Text;
        }

        private async Task OpenUrl(string url)
        {
            sendStatus.Text = "Connecting...";
            var result = await Common.PackageManager.LaunchUri(new Uri(url));

            SetProgressBarValueToMax();
            if (result == QuickShare.Common.Rome.RomeRemoteLaunchUriStatus.Success)
            {
                sendStatus.Text = "Finished.";
            }
            else
            {
                sendStatus.Text = result.ToString();
            }
        }

        private async Task SendText(string text)
        {
            sendStatus.Text = "Connecting...";
            var result = await Common.PackageManager.Connect(Common.GetCurrentRemoteSystem(), false);

            if (result != QuickShare.Common.Rome.RomeAppServiceConnectionStatus.Success)
            {
                sendStatus.Text = $"Connect failed. ({result.ToString()})";
                return;
            }

            //Fix Rome Android bug (receiver app service closes after 5 seconds in first connection)
            Common.PackageManager.CloseAppService();
            result = await Common.PackageManager.Connect(Common.GetCurrentRemoteSystem(), false);

            if (result != QuickShare.Common.Rome.RomeAppServiceConnectionStatus.Success)
            {
                sendStatus.Text = $"Connect failed. ({result.ToString()})";
                return;
            }

            TextSender textSender = new TextSender(Common.PackageManager, CrossDeviceInfo.Current.Model);

            textSender.TextSendProgress += (ee) =>
            {
                SetProgressBarValue(ee.SentParts, ee.TotalParts);
            };

            sendStatus.Text = "Sending...";

            bool sendResult = await textSender.Send(text, ContentType.ClipboardContent);

            SetProgressBarValueToMax();

            if (sendResult)
                sendStatus.Text = "Finished.";
            else
                sendStatus.Text = "Send failed.";

            sendProgress.Progress = sendProgress.Max;
        }

        private void SetProgressBarValue(int val, int max)
        {
            sendProgress.Max = max;
            sendProgress.Progress = val;

            int percent = (100 * val) / max;
            sendProgressPercent.Text = $"{percent}%";

            sendProgress.Visibility = ViewStates.Visible;
            sendProgressPercent.Visibility = ViewStates.Visible;
            sendProgressIndeterminate.Visibility = ViewStates.Invisible;
        }

        private void SetProgressBarValueToMax()
        {
            if (sendProgress.Max == 0)
                sendProgress.Max = 1;
            SetProgressBarValue(sendProgress.Max, sendProgress.Max);
        }
    }
}