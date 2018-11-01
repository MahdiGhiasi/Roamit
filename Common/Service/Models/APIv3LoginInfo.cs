using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common.Service.Models
{
    public class APIv3LoginInfo
    {
        public Guid AccountId { get; }
        public string Token { get; }

        public APIv3LoginInfo(Guid accountId, string token)
        {
            AccountId = accountId;
            Token = token;
        }
    }
}
