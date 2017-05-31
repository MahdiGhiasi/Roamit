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
using System.Threading.Tasks;
using System.Net.Http;
using Plugin.SecureStorage;
using QuickShare.MicrosoftGraphFunctions;

namespace QuickShare.Droid.OnlineServiceHelpers
{
    internal static class MSAAuthenticator
    {
        static MSAAuthenticator()
        {
            SecureStorageImplementation.StoragePassword = Config.Secrets.SecureStoragePassword;
        }

        internal static async Task<string> GetAccessToken(string code)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", "https://login.live.com/oauth20_desktop.srf"),
                new KeyValuePair<string, string>("scope", "User.Read Device.Read"),
                new KeyValuePair<string, string>("client_id", Config.Secrets.ClientId),
            });

            var myHttpClient = new HttpClient();
            var response = await myHttpClient.PostAsync("https://login.live.com/oauth20_token.srf", formContent);
            var json = await response.Content.ReadAsStringAsync();
            var results = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            return results["access_token"];
        }

        internal static async Task<string> GetAccessToken()
        {
            return await GetAccessToken(GetAuthenticationCode());
        }

        internal static void SaveAuthenticationCode(string code)
        {
            if (CrossSecureStorage.Current.HasKey("MSACode"))
                CrossSecureStorage.Current.DeleteKey("MSACode");

            CrossSecureStorage.Current.SetValue("MSACode", code);
        }

        internal static string GetAuthenticationCode()
        {
            if (!CrossSecureStorage.Current.HasKey("MSACode"))
                return "";

            var code = CrossSecureStorage.Current.GetValue("MSACode");
            return code;
        }

        internal static async Task<string> GetUserUniqueIdAsync()
        {
            var graph = new Graph(await GetAccessToken());
            return await graph.GetUserUniqueIdAsync();
        }
    }
}