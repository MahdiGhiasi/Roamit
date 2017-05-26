using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace QuickShare.MicrosoftGraphFunctions
{
    public class Graph
    {
        string token;
        
        public Graph(string _token)
        {
            token = _token;
        }

        public async Task<string> GetUserUniqueIdAsync()
        {
            string userId = "";

            string resultString = await SendGetRequestWithTokenAsync("https://graph.microsoft.com/v1.0/me", token);
            var result = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(resultString);
            userId = result["id"].ToString();

            return userId;
        }

        private async Task<string> SendGetRequestWithTokenAsync(string url, string accesstoken)
        {
            HttpClient cl = new HttpClient();

            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, url);
            msg.Headers.Clear();
            msg.Headers.Authorization = new AuthenticationHeaderValue("bearer", accesstoken);

            var response = await cl.SendAsync(msg);
            var resultString = await response.Content.ReadAsStringAsync();
            return resultString;
        }
    }
}
