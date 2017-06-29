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
                new KeyValuePair<string, string>("scope", "User.Read"),
                new KeyValuePair<string, string>("client_id", Config.Secrets.ClientId2),
            });

            var myHttpClient = new HttpClient();
            var response = await myHttpClient.PostAsync("https://login.live.com/oauth20_token.srf", formContent);
            var json = await response.Content.ReadAsStringAsync();
            var results = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            if (!results.ContainsKey("access_token"))
            {
                //Code is probably expired. Try using refresh_token instead.
                var refreshToken = CrossSecureStorage.Current.GetValue("refreshToken");

                if (refreshToken == null)
                    throw new Exception("No refresh token.");

                var formContent2 = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "refresh_token"),
                    new KeyValuePair<string, string>("refresh_token", refreshToken),
                    new KeyValuePair<string, string>("redirect_uri", "https://login.live.com/oauth20_desktop.srf"),
                    new KeyValuePair<string, string>("scope", "User.Read"),
                    new KeyValuePair<string, string>("client_id", Config.Secrets.ClientId2),
                });

                var myHttpClient2 = new HttpClient();
                var response2 = await myHttpClient2.PostAsync("https://login.live.com/oauth20_token.srf", formContent2);
                var json2 = await response2.Content.ReadAsStringAsync();
                results = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json2);
            }

            CrossSecureStorage.Current.SetValue("refreshToken", results["refresh_token"]);
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
            if (CrossSecureStorage.Current.HasKey("UserUniqueId"))
            {
                return CrossSecureStorage.Current.GetValue("UserUniqueId");
            }
            else
            {
                var graph = new Graph(await GetAccessToken());
                var id = await graph.GetUserUniqueIdAsync();

                CrossSecureStorage.Current.SetValue("UserUniqueId", id);

                return id;
            }
        }

        internal static bool HasUserUniqueId()
        {
            if (CrossSecureStorage.Current.HasKey("UserUniqueId"))
                return true;
            return false;
        }

        internal static void DeleteUserUniqueId()
        {
            if (HasUserUniqueId())
                CrossSecureStorage.Current.DeleteKey("UserUniqueId");
        }
    }
}