using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace softGetawayHost
{
    [DataContract]
    public class DHCPPair
    {
        [DataMember]
        public string MacAddress { get; set; }
        [DataMember]
        public string IPAddress { get; set; }
        public DHCPPair(string mac, string ip)
        {
            this.MacAddress = mac;
            this.IPAddress = ip;
        }
    }
}
