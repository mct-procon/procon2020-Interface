using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameInterface.Network
{
    public class NetworkInformation
    {
        public string HostName { get; set; }
        public string URLStarts => !HostName.StartsWith("http://")
                    ? HostName.EndsWith("/") ? string.Concat("http://", HostName) : string.Concat("http://", HostName, "/")
                    : HostName.EndsWith("/") ? string.Concat(HostName) : string.Concat(HostName, "/");
        public string AuthenticationID { get; set; }
    }
}
