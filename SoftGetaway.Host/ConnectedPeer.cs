using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using softGetaway.WiFi.WinAPI;
using softGetaway.WiFi;
using System.IO;

namespace softGetawayHost {
    [DataContract]
    public class NetworkPeerService {
        [DataMember]
        public NetworkPeerStorage storage { get; set; }
        [DataMember]
        public NetworkPeerType type { get; set; }

        [DataMember]
        public bool isSetIP { get; set; }

        [DataMember]
        public bool isSetHostName { get; set; }

        [DataMember]
        public bool isActive { get; set; }

        void init() {
            type = NetworkPeerType.Unknown;
            isActive = false;
            isSetIP = storage.IPAddress != null;
            isSetHostName = storage.HostName != null;
        }

        public NetworkPeerService() {
            storage = new NetworkPeerStorage();
        }

        public NetworkPeerService(NetworkPeerStorage p, WinDHCP.Library.DhcpServer dhcpManager) {
            this.storage = p;
            init();
            if (isSetIP) {
                dhcpManager.addReservation(storage.MACAddress, storage.IPAddress);
                if (isSetHostName)
                    updateHostName(storage.HostName);
            }
        }

        internal void updateIP(WinDHCP.Library.InternetAddress internetAddress) {
            storage.IPAddress = internetAddress.ToString();
            if (isSetHostName)
                updateHostName(storage.HostName);
        }

        internal void updateWiFi(WiFiStation wiFiStation) {
            type = NetworkPeerType.WiFi;
            isActive = true;
            this.storage.MACAddress = wiFiStation.MacAddress;
        }

        internal void leaveWiFi() {
            type = NetworkPeerType.Unknown;
            isActive = false;
        }

        bool needUpdate(bool oldSet, bool set, string oldVal, string val) {
            if (oldSet != set) return true;
            if (set && val != null && oldVal != val) return true;
            return false;
        }

        internal void updatePeer(NetworkPeerService peer, WinDHCP.Library.DhcpServer dhcpManager) {
            if (needUpdate(isSetIP, peer.isSetIP, storage.IPAddress, peer.storage.IPAddress)) {
                isSetIP = peer.isSetIP;
                if (isSetIP) {
                    storage.IPAddress = peer.storage.IPAddress;
                    dhcpManager.addReservation(peer.storage.MACAddress, storage.IPAddress);
                } else {
                    dhcpManager.removeReservation(peer.storage.MACAddress);
                }
            }
            if (needUpdate(isSetHostName, peer.isSetHostName, storage.HostName, peer.storage.HostName)) {
                isSetHostName = peer.isSetHostName;
                if (isSetHostName) {
                    //TODO add hostname to dns
                    storage.HostName = peer.storage.HostName;
                } else {
                    //TODO remove hostname from dns
                    storage.HostName = null;
                }
                updateHostName(storage.HostName);
            }
        }

        void updateHostName(String hostName) {
            string hostFile = Environment.GetEnvironmentVariable("SystemRoot") + "\\System32\\drivers\\etc\\hosts";
            bool needUpdate = false;
            using (FileStream fs = File.Open(hostFile, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
                List<string> lines = new List<string>();
                using (StreamReader sr = new StreamReader(fs)) {
                    while (!sr.EndOfStream) {
                        string line = sr.ReadLine();
                        if (!line.Contains(storage.MACAddress))
                            lines.Add(line);
                        else
                            needUpdate = !(line.Contains(storage.IPAddress) && line.Contains(storage.HostName));
                    }
                    if ((hostName != null) || (needUpdate)) {
                        fs.Seek(0, SeekOrigin.Begin);
                        if (hostName != null)
                            lines.Add(storage.IPAddress + " " + storage.HostName + " # softGetaway Entry for " + storage.MACAddress);
                        using (StreamWriter sw = new StreamWriter(fs))
                            sw.Write(lines.Aggregate((a, b) => a + Environment.NewLine + b));
                    }
                }
            }
        }
    }

    [DataContract]
    public class NetworkPeerStorage : ICloneable {
        #region ICloneable Members
        [DataMember]
        public string MACAddress { get; set; }

        [DataMember]
        public string IPAddress { get; set; }

        [DataMember]
        public string HostName { get; set; }

        [DataMember]
        public string iconFile { get; set; }

        public NetworkPeerStorage() {
            IPAddress = null;
            HostName = null;
        }
        #endregion

        public object Clone() {
            return this.MemberwiseClone();
        }
    }

    [DataContract]
    public enum NetworkPeerType {
        [EnumMember]
        Unknown,
        [EnumMember]
        WiFi,
        [EnumMember]
        Ethernet,
        [EnumMember]
        Bluetooth
    }
}
