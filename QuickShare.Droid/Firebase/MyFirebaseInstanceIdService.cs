using System;
using Android.App;
using Android.Content;
using Firebase.Iid;
using Android.Util;
using System.Threading.Tasks;
using QuickShare.Droid.OnlineServiceHelpers;

namespace QuickShare.Droid.Firebase
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class MyFirebaseIIDService : FirebaseInstanceIdService
    {
        const string TAG = "MyFirebaseIIDService";
        public override void OnTokenRefresh()
        {
            var refreshedToken = FirebaseInstanceId.Instance.Token;
            Log.Debug(TAG, "Refreshed token: " + refreshedToken);
            SendRegistrationToServer(refreshedToken);
        }

        void SendRegistrationToServer(string token)
        {
            Task.Run(async () =>
            {
                await ServiceFunctions.RegisterDevice();
            });
        }
    }
}