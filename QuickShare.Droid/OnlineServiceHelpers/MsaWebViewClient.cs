using Android.App;
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

        readonly MsaAuthPurpose purpose;
        readonly Context context;
        public MsaWebViewClient(Context _context,MsaAuthPurpose _purpose)
        {
            purpose = _purpose;
            context = _context;
        }

        public override async void OnPageFinished(WebView view, string url)
        {
            System.Diagnostics.Debug.WriteLine(url);
            base.OnPageFinished(view, url);
            if (url.Contains("?code=") && !authComplete)
            {
                authComplete = true;
                System.Diagnostics.Debug.WriteLine("Page finished successfully");

                var uri = Android.Net.Uri.Parse(url);
                string authCode = uri.GetQueryParameter("code");
                AuthenticateDialog.authDialog.Dismiss();

                if (purpose == MsaAuthPurpose.App)
                {
                    MSAAuthenticator.SaveAuthenticationCode(authCode);
                    var result = await ServiceFunctions.RegisterDevice();

                    if (result)
                        AuthenticateDialog.authenticateTcs.SetResult(MsaAuthResult.Success);
                    else
                        AuthenticateDialog.authenticateTcs.SetResult(MsaAuthResult.FailedToRegister);
                }
                else if (purpose == MsaAuthPurpose.ProjectRomePlatform)
                {
                    Platform.SetAuthCode(authCode);
                    AuthenticateDialog.authenticateTcs.SetResult(MsaAuthResult.Success);
                }                
            }
            else if (url.Contains("error=access_denied"))
            {
                authComplete = true;
                System.Diagnostics.Debug.WriteLine("Page finished failed with ACCESS_DENIED_HERE");
                Intent resultIntent = new Intent();
                AuthenticateDialog.authDialog.Dismiss();
                AuthenticateDialog.authenticateTcs.SetResult(MsaAuthResult.CancelledByUser);
            }
        }
    }

    internal enum MsaAuthPurpose
    {
        ProjectRomePlatform,
        App,
    }

    internal enum MsaAuthResult
    {
        CancelledByUser,
        FailedToRegister,
        Success,
        CancelledByApp,
    }
}