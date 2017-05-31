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
using Plugin.DeviceInfo;
using System.Security.Cryptography;
using System.Net.Http;
using System.Threading.Tasks;

namespace QuickShare.Droid.OnlineServiceHelpers
{
    internal static class ServiceFunctions
    {
        internal static async Task<bool> RegisterDevice()
        {
            var userId = await MSAAuthenticator.GetUserUniqueIdAsync();
            var deviceName = CrossDeviceInfo.Current.Model;
            var deviceName2 = Android.OS.Build.Model;
            var osVersion = CrossDeviceInfo.Current.Version;
            var deviceUniqueId = GetHashString(CrossDeviceInfo.Current.Id);

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("UserId", userId),
                new KeyValuePair<string, string>("DeviceName", deviceName),
                new KeyValuePair<string, string>("OSVersion", osVersion),
                new KeyValuePair<string, string>("DeviceId", deviceUniqueId),
            });

            var myHttpClient = new HttpClient();
            var response = await myHttpClient.PostAsync(QuickShare.Common.Constants.ServerAddress + "/api/User/RegisterDevice", formContent);
            var responseText = await response.Content.ReadAsStringAsync();

            if ((responseText == "1, REGISTERED") || (responseText == "2, UPDATED"))
            {
                System.Diagnostics.Debug.WriteLine($"RegisterDevice Succeeded. Response was '{responseText}'");
                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"RegisterDevice Failed. Response was '{responseText}'");
                return false;
            }
        }

        private static byte[] GetHash(string inputString)
        {
            HashAlgorithm algorithm = SHA256.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        private static string GetHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }
    }
}