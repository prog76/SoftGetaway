using System.Runtime.Serialization;
using System.Collections.Generic;
using System;

namespace softGetawayHost
{
    [DataContract]
    public class softGetawayServiceState
    {
        [DataMember]
        public bool active { get; set; }
        [DataMember]
        public string publicConnectionGuid { get; set; }
        [DataMember]
        public string wifiSSID { get; set; }
        [DataMember]
        public string wifiPassword { get; set; }
        [DataMember]
        public int limitClientsCount { get; set; }
        [DataMember]
        public string privateIP { get; set; }
    }

    [DataContract]
    public class softGetawayPeersStorage {
        public softGetawayPeersStorage() {
            peers = new List<NetworkPeerStorage>();
        }
        [DataMember]
        public List<NetworkPeerStorage> peers { get; set; }
    }

    [DataContract]
    public class softGetawayPeersService {
        [DataMember]
        public List<NetworkPeerService> peers { get; set; }
    }
}
