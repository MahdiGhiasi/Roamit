using Android.Content;
using Android.Util;
using Android.Webkit;
using Microsoft.ConnectedDevices;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

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

        public override async void OnPageFinished(WebView view, string url)
        {
            base.OnPageFinished(view, url);
            if (url.Contains("?code=") && !authComplete)
            {
                authComplete = true;
                System.Diagnostics.Debug.WriteLine("Page finished successfully");

                var uri = Android.Net.Uri.Parse(url);
                string token = uri.GetQueryParameter("code");
                _parentActivity._authDialog.Dismiss();

                //await GetUserId(token);

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

        private async Task<string> GetUserId(string token)
        {
            string userId = "";

            Log.WriteLine(LogPriority.Debug, "Msa", "Trying to get access token...");
            string accessToken = await GetAccessToken(token);

            Log.WriteLine(LogPriority.Debug, "Msa", "Trying to get user info...");
            try
            {
                string resultString = await SendGetRequestWithToken("https://graph.microsoft.com/v1.0/me", accessToken);
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(resultString);
                userId = result["id"].ToString();

                Log.WriteLine(LogPriority.Debug, "Msa", userId);
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogPriority.Debug, "Msa", ex.Message);
            }

            {
                /* v1.0 doesn't work, so we use beta */
                string devices = await SendGetRequestWithToken("https://graph.microsoft.com/beta/me/devices", accessToken);
                Log.WriteLine(LogPriority.Debug, "Devices", devices);
            }

            return userId;
        }

        private static async Task<string> SendGetRequestWithToken(string url, string accesstoken)
        {
            HttpClient cl = new HttpClient();

            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, url);
            msg.Headers.Clear();
            msg.Headers.Authorization = new AuthenticationHeaderValue("bearer", accesstoken);

            var response = await cl.SendAsync(msg);
            var resultString = await response.Content.ReadAsStringAsync();
            return resultString;
        }

        private async Task<string> GetAccessToken(string code)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", "https://login.live.com/oauth20_desktop.srf"),
                new KeyValuePair<string, string>("scope", "User.Read Device.Read"),
                new KeyValuePair<string, string>("client_id", Config.Secrets.CLIENT_ID),
            });

            var myHttpClient = new HttpClient();
            var response = await myHttpClient.PostAsync("https://login.live.com/oauth20_token.srf", formContent);
            var json = await response.Content.ReadAsStringAsync();
            var results = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            return results["access_token"];
        }
    }
}