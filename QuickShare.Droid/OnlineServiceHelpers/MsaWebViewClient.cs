using Android.App;
using Android.Content;
using Android.Util;
using Android.Webkit;
using Firebase.Iid;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.ConnectedDevices;
using Plugin.SecureStorage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            Debug.WriteLine(url);
            base.OnPageFinished(view, url);
            if (url.Contains("?code=") && !authComplete)
            {
                authComplete = true;
                Debug.WriteLine("Page finished successfully");

                var uri = Android.Net.Uri.Parse(url);
                string authCode = uri.GetQueryParameter("code");
                AuthenticateDialog.authDialog.Dismiss();

                if (purpose == MsaAuthPurpose.App)
                {
                    MSAAuthenticator.SaveAuthenticationCode(authCode);
                    var result = await ServiceFunctions.RegisterDevice(context);

                    if (result)
                        AuthenticateDialog.authenticateTcs.TrySetResult(MsaAuthResult.Success);
                    else
                        AuthenticateDialog.authenticateTcs.TrySetResult(MsaAuthResult.FailedToRegister);
                }
                else if (purpose == MsaAuthPurpose.ProjectRomePlatform)
                {
                    Platform.SetAuthCode(authCode);
                    AuthenticateDialog.authenticateTcs.TrySetResult(MsaAuthResult.Success);
                }                
            }
            else if (url.Contains("error=access_denied"))
            {
                authComplete = true;
                System.Diagnostics.Debug.WriteLine("Page finished failed with ACCESS_DENIED_HERE");
                Intent resultIntent = new Intent();
                AuthenticateDialog.authDialog.Dismiss();
                AuthenticateDialog.authenticateTcs.TrySetResult(MsaAuthResult.CancelledByUser);
            }
            else if ((url.Contains("/v3/Graph/Welcome")))
            {
                var queryStrings = QueryHelpers.ParseQuery(new Uri(url).Query);

                if (queryStrings.ContainsKey("accountId") && queryStrings.ContainsKey("token"))
                {
                    string id = queryStrings["accountId"][0];
                    string token = queryStrings["token"][0];

                    CrossSecureStorage.Current.SetValue("RoamitAccountId", id);
                    CrossSecureStorage.Current.SetValue("RoamitAccountToken", token);

                    var result = await ServiceFunctions.RegisterDevice(context);

                    if (result)
                        AuthenticateDialog.authenticateTcs.TrySetResult(MsaAuthResult.Success);
                    else
                        AuthenticateDialog.authenticateTcs.TrySetResult(MsaAuthResult.FailedToRegister);
                }
                else
                {
                    AuthenticateDialog.authenticateTcs.TrySetResult(MsaAuthResult.CancelledByUser);
                }

                AuthenticateDialog.authDialog.Dismiss();
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