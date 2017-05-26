using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.HelperClasses
{
    internal static class MSAAuthenticator
    {
        internal static async Task<string> GetAccessTokenAsync(string permissions)
        {
            var authenticator = new Windows.Security.Authentication.OnlineId.OnlineIdAuthenticator();
            var serviceTicketRequest = new Windows.Security.Authentication.OnlineId.OnlineIdServiceTicketRequest(permissions, "DELEGATION");

            var result = await authenticator.AuthenticateUserAsync(serviceTicketRequest);

            return result.Tickets[0].Value;
        }
    }
}
