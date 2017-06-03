using Android.Content;
using Android.Util;
using Android.Webkit;
using Firebase.Iid;
using Microsoft.ConnectedDevices;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace QuickShare.Droid.OnlineServiceHelpers
{
    internal class MsaWebViewClient : WebViewClient
    {
        bool authComplete = false;

        private readonly MainActivity _parentActivity;
        public MsaWebViewClient(MainActivity activity)
        {
            _parentActivity = activity;
        }

        public override async void OnPageFinished(WebView view, string url)
        {
            base.OnPageFinished(view, url);
            if (url.Contains("?code=") && !authComplete)
            {
                authComplete = true;
                System.Diagnostics.Debug.WriteLine("Page finished successfully");

                var uri = Android.Net.Uri.Parse(url);
                string authCode = uri.GetQueryParameter("code");
                _parentActivity._authDialog.Dismiss();

                MSAAuthenticator.SaveAuthenticationCode(authCode);

                await ServiceFunctions.RegisterDevice();

                Platform.SetAuthCode(authCode);
            }
            else if (url.Contains("error=access_denied"))
            {
                authComplete = true;
                System.Diagnostics.Debug.WriteLine("Page finished failed with ACCESS_DENIED_HERE");
                Intent resultIntent = new Intent();
                _parentActivity.SetResult(0, resultIntent);
                _parentActivity._authDialog.Dismiss();
            }
        }
    }
}