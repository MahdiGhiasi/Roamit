using Android.Content;
using Android.Webkit;
using Microsoft.ConnectedDevices;

namespace QuickShare.Droid
{
    internal class MsaWebViewClient : WebViewClient
    {
        bool authComplete = false;

        private readonly MainActivity _parentActivity;
        public MsaWebViewClient(MainActivity activity)
        {
            _parentActivity = activity;
        }

        public override void OnPageFinished(WebView view, string url)
        {
            base.OnPageFinished(view, url);
            if (url.Contains("?code=") && !authComplete)
            {
                authComplete = true;
                System.Diagnostics.Debug.WriteLine("Page finished successfully");

                var uri = Android.Net.Uri.Parse(url);
                string token = uri.GetQueryParameter("code");
                _parentActivity._authDialog.Dismiss();
                Platform.SetAuthCode(token);
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