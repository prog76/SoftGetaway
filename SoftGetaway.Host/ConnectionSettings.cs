using System.Runtime.Serialization;

namespace softGetawayHost
{
    [DataContract]
    public class ConnectionSettings
    {
        [DataMember]
        public string SSID { get; set; }
        [DataMember]
        public int MaxPeerCount { get; set; }
        [DataMember]
        public string Password { get; set; }
    }
}
