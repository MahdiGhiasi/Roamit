using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Webkit;
using System.Threading.Tasks;
using QuickShare.Common;

namespace QuickShare.Droid.OnlineServiceHelpers
{
    internal static class AuthenticateDialog
    {
        private static WebView webView;
        internal static Dialog authDialog;
        internal static TaskCompletionSource<MsaAuthResult> authenticateTcs;
        internal static MsaAuthPurpose lastDialogPurpose;

        internal static async Task<MsaAuthResult> ShowAsync(Context context, MsaAuthPurpose purpose)
        {
            if (purpose == MsaAuthPurpose.App)
            {
                string url = $"{Constants.ServerAddress}/v3/Authenticate/Graph";
                lastDialogPurpose = purpose;
                return await ShowAsync(context, purpose, url);
            }
            else
                throw new InvalidOperationException("AuthenticateDialog.Show() can't determine url");
        }

        internal static async Task<MsaAuthResult> ShowAsync(Context context, MsaAuthPurpose purpose, string oauthUrl)
        {
            authenticateTcs = new TaskCompletionSource<MsaAuthResult>();
            lastDialogPurpose = purpose;

            authDialog = new Dialog(context, Android.Resource.Style.ThemeLightNoTitleBarFullScreen);

            authDialog.CancelEvent += (ss, ee) => 
            {
                authenticateTcs.TrySetResult(MsaAuthResult.CancelledByUser);
            };

            var linearLayout = new LinearLayout(authDialog.Context);
            webView = new WebView(authDialog.Context);
            linearLayout.AddView(webView);
            authDialog.SetContentView(linearLayout);

            webView.SetWebChromeClient(new WebChromeClient());
            webView.Settings.JavaScriptEnabled = true;
            webView.Settings.DomStorageEnabled = true;
            webView.LoadUrl(oauthUrl);

            webView.SetWebViewClient(new MsaWebViewClient(context, purpose));
            authDialog.Show();
            authDialog.SetCancelable(true);

            return await authenticateTcs.Task;
        }

        internal static void HideIfPurposeIs(MsaAuthPurpose purpose)
        {
            if (lastDialogPurpose == purpose)
                Hide();
        }

        internal static void Hide()
        {
            try
            {
                if (authDialog?.IsShowing == true)
                {
                    authDialog.Dismiss();
                    authenticateTcs.TrySetResult(MsaAuthResult.CancelledByApp);
                }
            }
            catch { }
        }
    }
}