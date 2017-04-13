using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickShare.Common
{
    public interface IWebServerGenerator
    {
        IWebServer GenerateInstance();
    }
}
