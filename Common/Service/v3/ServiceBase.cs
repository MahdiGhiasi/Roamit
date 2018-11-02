using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common.Service.v3
{
    public abstract class ServiceBase
    {
        private readonly string apiEndpoint;
        private readonly string serviceEndpoint;
        private readonly Guid accountId;
        private readonly string token;

        public ServiceBase(string apiEndpoint, string serviceEndpoint, Guid accountId, string token)
        {
            this.apiEndpoint = apiEndpoint;
            this.serviceEndpoint = serviceEndpoint;
            this.accountId = accountId;
            this.token = token;
        }

        protected Task<HttpResponseMessage> SendGetRequest(string endpoint)
        {
            return SendGetRequest(endpoint, new KeyValuePair<string, string>[] { });
        }

        protected async Task<HttpResponseMessage> SendGetRequest(string endpoint, IEnumerable<KeyValuePair<string, string>> data)
        {
            using (var httpClient = new HttpClient())
            {
                string queryString = string.Join("&", data.Select(x => $"{WebUtility.UrlEncode(x.Key)}={WebUtility.UrlEncode(x.Value)}"));
                if (queryString.Length > 0)
                    queryString = "?" + queryString;
                
                httpClient.DefaultRequestHeaders.Add("Authorization", $"{accountId}.{token}");
                var response = await httpClient.GetAsync($"{Constants.ServerAddress}/{apiEndpoint}/{serviceEndpoint}/{endpoint}{queryString}", HttpCompletionOption.ResponseContentRead);

                return response;
            }
        }

        protected async Task<HttpResponseMessage> SendPostRequest(string endpoint, Dictionary<string, object> data, HttpPostContentType contentType)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"{accountId}.{token}");

                var url = $"{Constants.ServerAddress}/{apiEndpoint}/{serviceEndpoint}/{endpoint}";
                if (contentType == HttpPostContentType.FormUrlEncoded)
                {
                    var formData = data.Select(x => new KeyValuePair<string, string>(x.Key, x.Value as string));
                    var formContent = new FormUrlEncodedContent(formData);

                    var response = await httpClient.PostAsync(url, formContent);
                    return response;
                }
                else if (contentType == HttpPostContentType.Json)
                {
                    var json = JsonConvert.SerializeObject(data);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(url, content);
                    return response;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        protected enum HttpPostContentType
        {
            FormUrlEncoded,
            Json,
        }
    }
}
