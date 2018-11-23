using System;
using Android.App;
using Android.Content;
using Firebase.Messaging;
using Android.Media;
using Android.Support.V4.App;
using QuickShare.Droid.Services;
using QuickShare.TextTransfer;
using Android.Util;
using QuickShare.Droid.Classes;
using Android.Preferences;
using Plugin.SecureStorage;
using QuickShare.Droid.Activities;

namespace QuickShare.Droid.Firebase
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseListenerService : FirebaseMessagingService
    {
        readonly string TAG = "MyFirebaseListenerService";

        public MyFirebaseListenerService()
        {
            SecureStorageImplementation.StoragePassword = Config.Secrets.SecureStoragePassword;
        }

        public override async void OnMessageReceived(RemoteMessage message)
        {
            base.OnMessageReceived(message);

            try
            {
                if (!message.Data.ContainsKey("Action"))
                {
                    throw new InvalidOperationException();
                }
                else if (message.Data["Action"] == "Wake")
                {
                    Classes.Notification.SendDebugNotification(this, "Wake", "Wake");
                    var intent = new Intent(this, typeof(MessageCarrierService));
                    intent.PutExtra("Action", "Wake");
                    StartService(intent);
                }
                else if (message.Data["Action"] == "SendCarrier")
                {
                    var intent = new Intent(this, typeof(MessageCarrierService));
                    intent.PutExtra("Action", "SendCarrier");
                    intent.PutExtra("DeviceId", message.Data["WakerDeviceId"]);

                    Android.Util.Log.Debug("CARRIER_DEBUG", "1");

                    StartService(intent);
                }
                else if (message.Data["Action"] == "Payload")
                {
                    var intent = new Intent(this, typeof(WaiterService));
                    intent.PutExtra("Data", message.Data["Data"]);

                    StartService(intent);
                }
                else if ((message.Data["Action"] == "LaunchUrl") && (message.Data.ContainsKey("Url")))
                {
                    try
                    {
                        string url = message.Data["Url"];
                        LaunchHelper.LaunchUrl(this, url);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug(TAG, ex.Message);
                        ToastHelper.ShowToast(this, "Couldn't launch URL.", Android.Widget.ToastLength.Long);
                    }
                }
                else if ((message.Data["Action"] == "FastClipboard") && (message.Data.ContainsKey("SenderName")) && (message.Data.ContainsKey("Text")))
                {
                    string senderName = message.Data["SenderName"];
                    string text = message.Data["Text"];

                    Guid guid = await TextReceiver.QuickTextReceivedAsync(senderName, text);

                    await ClipboardHelper.CopyTextToClipboard(this, guid);
                }
                else if (message.Data["Action"] == "CloudClipboard")
                {
                    if (message.Data.ContainsKey("Data"))
                    {
                        string text = message.Data["Data"];

                        var settings = new Settings(this);

                        if (message.Data.ContainsKey("AccountId"))
                        {
                            if (CrossSecureStorage.Current.HasKey("RoamitAccountId"))
                                CrossSecureStorage.Current.DeleteKey("RoamitAccountId");

                            CrossSecureStorage.Current.SetValue("RoamitAccountId", message.Data["AccountId"]);
                        }

                        if (settings.CloudClipboardReceiveMode == CloudClipboardReceiveMode.Automatic)
                        {
                            CloudClipboardNotifier.SetCloudClipboardValue(this, text);
                        }
                        else
                        {
                            settings.CloudClipboardText = text;
                            CloudClipboardNotifier.SendCloudClipboardNotification(this, text);
                        }
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            catch (InvalidOperationException)
            {
                Classes.Notification.SendNotification(this, $"Action '{message.Data["Action"]}' not supported.", "Please make sure the app is updated to enjoy latest features.");
            }
        }
    }
}