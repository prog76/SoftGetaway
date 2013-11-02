using System;
using System.Runtime.Serialization;
using IcsMgr;

namespace softGetawayHost
{
    [DataContract]
    public class SharableConnection
    {
        public SharableConnection() { }

        public SharableConnection(IcsConnection conn)
        {
            this.Name = conn.Name;
            this.DeviceName = conn.DeviceName;
            this.Guid = conn.Guid;
        }
        
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string DeviceName { get; set; }
        [DataMember]
        public string Guid { get; set; }
    }
}