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
using Firebase.Iid;
using System.Json;
using Firebase;
using Newtonsoft.Json;
using Plugin.SecureStorage;

namespace QuickShare.Droid.OnlineServiceHelpers
{
    internal static class ServiceFunctions
    {
        static string userId = "";
        internal static async Task<bool> RegisterWinDeviceIds(IEnumerable<string> ids)
        {
            try
            {
                if (CloudServiceAuthenticationHelper.IsAuthenticatedForApiV3())
                {
                    var apiLoginInfo = (CloudServiceAuthenticationHelper.GetApiLoginInfo());
                    var user = new QuickShare.Common.Service.v3.User(apiLoginInfo.AccountId, apiLoginInfo.Token);

                    var apiResult = await user.RegisterWinDeviceIds(ids.ToArray());

                    return apiResult;
                }

                await FindUserId();

                var jsonData = JsonConvert.SerializeObject(ids);
                var httpClient = new HttpClient();
                var result = await httpClient.PostAsync($"{QuickShare.Common.Constants.ServerAddress}/api/User/{userId}/WIDS", new StringContent(jsonData, Encoding.UTF8, "application/json"));
                var resultString = await result.Content.ReadAsStringAsync();

                if (resultString != "1, success")
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        internal static async Task<bool> RegisterDevice(Context _context)
        {
            try
            {
                var firebaseToken = FirebaseInstanceId.Instance.Token;

                if (firebaseToken == null)
                {
                    System.Diagnostics.Debug.WriteLine("firebaseToken is null. Device registration failed.");
                    return false;
                }


                Classes.Settings settings = new Classes.Settings(_context);

                var deviceName = settings.DeviceName;
                var osVersion = CrossDeviceInfo.Current.Version;
                var deviceUniqueId = GetDeviceUniqueId();
                var appVersion = Application.Context.ApplicationContext.PackageManager.GetPackageInfo(Application.Context.ApplicationContext.PackageName, 0).VersionName;


                if (CloudServiceAuthenticationHelper.IsAuthenticatedForApiV3())
                {
                    var apiLoginInfo = (CloudServiceAuthenticationHelper.GetApiLoginInfo());
                    var user = new QuickShare.Common.Service.v3.User(apiLoginInfo.AccountId, apiLoginInfo.Token);

                    var result = await user.RegisterDevice(deviceName, osVersion, deviceUniqueId, "Android", firebaseToken, appVersion);

                    if (result)
                        System.Diagnostics.Debug.WriteLine($"RegisterDevice Succeeded.");
                    else
                        System.Diagnostics.Debug.WriteLine($"RegisterDevice failed.");

                    return result;
                }
                else
                {
                    await FindUserId();

                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("name", deviceName),
                        new KeyValuePair<string, string>("osVersion", osVersion),
                        new KeyValuePair<string, string>("deviceId", deviceUniqueId),
                        new KeyValuePair<string, string>("type", "Android"),
                        new KeyValuePair<string, string>("token", firebaseToken),
                        new KeyValuePair<string, string>("appVersion", appVersion),
                    });

                    var myHttpClient = new HttpClient();
                    var response = await myHttpClient.PostAsync($"{QuickShare.Common.Constants.ServerAddress}/api/User/{userId}/RegisterDevice", formContent);
                    var responseText = await response.Content.ReadAsStringAsync();

                    if ((responseText == "1, registered") || (responseText == "2, updated"))
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RegisterDevice Failed due to unhandled exception: {ex.Message}");
                return false;
            }
        }

        internal static async Task<bool> RemoveDevice(Context _context)
        {
            try
            {
                await FindUserId();

                Classes.Settings settings = new Classes.Settings(_context);

                var deviceUniqueId = GetDeviceUniqueId();

                if (CloudServiceAuthenticationHelper.IsAuthenticatedForApiV3())
                {
                    var apiLoginInfo = (CloudServiceAuthenticationHelper.GetApiLoginInfo());
                    var user = new QuickShare.Common.Service.v3.User(apiLoginInfo.AccountId, apiLoginInfo.Token);

                    var result = await user.RemoveDevice(deviceUniqueId);

                    if (result)
                        System.Diagnostics.Debug.WriteLine($"RemoveDevice Succeeded.");
                    else
                        System.Diagnostics.Debug.WriteLine($"RemoveDevice failed.");

                    return result;
                }
                else
                {
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("deviceId", deviceUniqueId),
                    });

                    var myHttpClient = new HttpClient();
                    var response = await myHttpClient.PostAsync($"{QuickShare.Common.Constants.ServerAddress}/api/User/{userId}/RemoveDevice", formContent);
                    var responseText = await response.Content.ReadAsStringAsync();

                    if (responseText == "1, removed")
                    {
                        System.Diagnostics.Debug.WriteLine($"RemoveDevice Succeeded. Response was '{responseText}'");
                        return true;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"RemoveDevice Failed. Response was '{responseText}'");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RemoveDevice Failed due to unhandled exception: {ex.Message}");
                return false;
            }
        }


        internal static async Task<bool> SetCloudClipboardActivationStatus(bool activated)
        {
            var accountId = CrossSecureStorage.Current.GetValue("RoamitAccountId");

            if (accountId == null)
                return false;

            return await QuickShare.Common.Service.CloudClipboardService.SetCloudClipboardActivation(accountId, GetDeviceUniqueId(), activated);
        }

        internal static async Task<bool> GetCloudClipboardActivationStatus()
        {
            var accountId = CrossSecureStorage.Current.GetValue("RoamitAccountId");
            if (accountId == null)
                return false;

            var deviceId = GetDeviceUniqueId();

            var devices = await QuickShare.Common.Service.CloudClipboardService.GetDevices(accountId);
            var currentDevice = devices.FirstOrDefault(x => x?.DeviceID == deviceId);

            return currentDevice?.CloudClipboardEnabled ?? false;
        }

        private static async Task FindUserId()
        {
            if (userId == "")
                userId = await MSAAuthenticator.GetUserUniqueIdAsync();
        }

        internal static string GetDeviceUniqueId()
        {
            return GetHashString(CrossDeviceInfo.Current.Id);
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