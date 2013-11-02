using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using softGetaway.WiFi;
using WinDHCP.Library;
using System.Diagnostics;
using IcsMgr;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace softGetawayHost {
    class privateNetManager {
        WiFiManager wlanManager;
        DhcpServer dhcpManager;

        public string connectionGuid;

        public event EventHandler networkAvailable, networkUnavailable;
        public Dictionary<string, NetworkPeerService> Peers;

        public privateNetManager() {
            Peers = new Dictionary<string, NetworkPeerService>();
            dhcpManager = new DhcpServer();
            dhcpManager.ClearAcls();
            dhcpManager.AllowAny = true;
            dhcpManager.OnNewClient += new EventHandler<OnNewDHCPClientEvent>(dhcpManager_OnNewClient);
        }

        void dhcpManager_OnNewClient(object sender, OnNewDHCPClientEvent e) {
            Peers[e.mac].updateIP(e.ip);
        }

        void wlanManager_StationLeave(object sender, WiFiManager.EventStationLeave e) {
            if (Peers.ContainsKey(e.MACAddress))
                Peers[e.MACAddress].leaveWiFi();
        }

        void wlanManager_StationJoin(object sender, WiFiManager.EventStationJoin e) {
            if (!Peers.ContainsKey(e.pStation.MacAddress))
                Peers.Add(e.pStation.MacAddress, new NetworkPeerService());
            Peers[e.pStation.MacAddress].updateWiFi(e.pStation);
        }

        void wlanManager_HostedNetworkStarted(object sender, EventArgs e) {
            wlanManager_HostedNetworkAvailable(sender, e);
        }

        void wlanManager_HostedNetworkAvailable(object sender, EventArgs e) {
            if (networkAvailable != null)
                networkAvailable(this, null);
        }

        void wlanManager_HostedNetworkUnavailable(object sender, EventArgs e) {
            connectionGuid = null;
            if (networkUnavailable != null)
                networkUnavailable(this, null);
        }

        public void init() {
            this.wlanManager = new WiFiManager();
            this.wlanManager.HostedNetworkAvailable += new EventHandler(wlanManager_HostedNetworkAvailable);
            this.wlanManager.HostedNetworkUnavailable += new EventHandler(wlanManager_HostedNetworkUnavailable);
            this.wlanManager.HostedNetworkStarted += new EventHandler(wlanManager_HostedNetworkStarted);
            this.wlanManager.StationJoin += new EventHandler<WiFiManager.EventStationJoin>(wlanManager_StationJoin);
            this.wlanManager.StationLeave += new EventHandler<WiFiManager.EventStationLeave>(wlanManager_StationLeave);
        }

        public void enable() {
            wlanManager.enableHosted();
        }

        public void networkStop() {
            dhcpManager.Stop();
            this.wlanManager.StopHostedNetwork();
        }

        internal void startNetwork() {
            this.wlanManager.StartHostedNetwork();
        }

        public bool setIP(string ip) {
            Trace.TraceInformation("Interface IP Changing");
            for (int i = 0; i < 5; i++) {
                if (IcsManager.setIpWMI(connectionGuid, ip, "255.255.255.0")) {
                    Trace.TraceInformation("Interface IP Changed");
                    break;
                } else
                    Trace.TraceInformation("Interface IP Change failed");
                Thread.Sleep(2000);
            }
            string trimmedIp = ip.TrimEnd("0123456789".ToCharArray());
            InternetAddress privateAddress = InternetAddress.Parse(ip);
            dhcpManager.DhcpInterfaceAddress = IPAddress.Parse(ip);
            dhcpManager.StartAddress = InternetAddress.Parse(trimmedIp + "1");
            dhcpManager.EndAddress = InternetAddress.Parse(trimmedIp + "255");
            dhcpManager.Subnet = InternetAddress.Parse("255.255.255.0");
            dhcpManager.Gateway = privateAddress;
            dhcpManager.DnsServers.Add(privateAddress);
            for (int i = 1; i < 5; i++) {
                try {
                    Socket sock = dhcpManager.configureSocket();
                    sock.Close();
                    dhcpManager.Start();
                    return true;
                } catch (SocketException e) {
                    if (e.ErrorCode == 10049) {
                        Trace.TraceInformation("Interface IP waiting for ready.");
                        dhcpManager.Stop();
                        Thread.Sleep(2000);
                    }
                }
            }
            return false;
        }

        public void stopDHCP() {
            dhcpManager.Stop();
        }

        public bool Started { get { return wlanManager.IsHostedNetworkStarted; } }

        public void updateConnectionGuid() {
            if (connectionGuid == null) {
                connectionGuid = this.wlanManager.HostedNetworkInterfaceGuid;
                if (connectionGuid == null) {
                    lock (IcsManager.connectionsLock) {
                        connectionGuid = (
                             from c in IcsManager.Connections
                             where (c.IsSupported) && (c.props.DeviceName.ToLower().Contains("microsoft virtual wifi miniport adapter"))
                             select c.Guid).FirstOrDefault();
                    }
                }
            };
        }

        public bool setConnection(ConnectionSettings settings) {
            try {
                this.wlanManager.SetConnectionSettings(settings.SSID, settings.MaxPeerCount);
                this.wlanManager.SetSecondaryKey(settings.Password);
                return true;
            } catch {
                return false;
            };
        }

        public ConnectionSettings getConnection() {
            try {
                string ssid;
                int maxNumberOfPeers;
                this.wlanManager.QueryConnectionSettings(out ssid, out maxNumberOfPeers);
                string passKey = string.Empty;
                bool isPassPhrase;
                bool isPersistent;

                var r = this.wlanManager.QuerySecondaryKey(out passKey, out isPassPhrase, out isPersistent);

                return new ConnectionSettings() {
                    SSID = ssid,
                    MaxPeerCount = maxNumberOfPeers,
                    Password = passKey
                };
            } catch {
                return null;
            }
        }

        internal void SetPeer(string macAddress, NetworkPeerService peer) {
            Peers[macAddress].updatePeer(peer, dhcpManager);
        }

        internal void AddPeerFromStorage(NetworkPeerStorage p) {
            Peers[p.MACAddress] = new NetworkPeerService(p, dhcpManager);
        }
    }
}
