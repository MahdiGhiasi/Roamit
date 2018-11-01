using QuickShare.Common.Service.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common.Service.v2
{
    public class User
    {
        public static async Task<APIv3LoginInfo> MigrateToV3(string accountId)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var formContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("jwt", Secrets.GenerateAPIMigrateClaim(accountId)),
                    });
                    var result = await httpClient.PostAsync($"{Constants.ServerAddress}/v2/User/Migrate", formContent);
                    var s = await result.Content.ReadAsStringAsync();

                    if (s.Split(',')[0] == "1")
                    {
                        var parts = s.Split('\n');
                        return new APIv3LoginInfo(Guid.Parse(parts[1]), parts[2]);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in v2.MigrateToV3: {ex.Message}");
                return null;
            }
        }
    }
}
