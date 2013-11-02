using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using softGetaway.WiFi;
using IcsMgr;
using System.ServiceModel;
using softGetaway.WiFi.WinAPI;
using System.Net;
using WinDHCP.Library;
using System.Diagnostics;
using System.Threading;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization;
using System.Net.NetworkInformation;
using System.Management;
using softGetaway;
using System.ServiceProcess;


namespace softGetawayHost {
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class softGetawayHost : ISoftGetawayHost, IDisposable {
        IcsManager icsManager;
        getawayState state_;
        bool shouldStart_, publicConnected_;
        privateNetManager privateManager;
        Timer watchDog;
        softEventQueue<SOFTEVENT> events;
        bool disposed;
        static Notifier notifier = new Notifier("sftGtw");

        public event EventHandler OnRouterStart;

        public void watchTimer(Object state) {
            ServiceController[] services = ServiceController.GetServices();
            stopService("Wlansvc", services, false);
            startService("Wlansvc", services);
            events.put(SOFTEVENT.LOADINIT, 0);
        }

        public void Dispose() {
            if (!disposed) {
                events.put(SOFTEVENT.EXIT, 0);
                notifier.Dispose();
                disposed = true;
                events.Dispose();
            }
        } 

        void stopService(string serviceName, ServiceController[] services, bool notify = true) {
            var service = services.FirstOrDefault(s => s.ServiceName == serviceName);
            if (service == null) return;
            if (service.Status != ServiceControllerStatus.Stopped) {
                if (notify)
                   Trace.TraceWarning("STOPPING CONFLICTED SERVICE " + serviceName + ". Please disable this one.");
                service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(10000));
            }
        }

        void startService(string serviceName, ServiceController[] services) {
            var service = services.FirstOrDefault(s => s.ServiceName == serviceName);
            if (service == null) return;
            if (service.Status != ServiceControllerStatus.Running) {
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(10000));
            }
        }

        void startTimer() {
            watchDog.Change(20000, Timeout.Infinite);
        }


        public softGetawayHost() {
            watchDog = new Timer(new TimerCallback(watchTimer), this, Timeout.Infinite, Timeout.Infinite);
            ServiceController[] services = ServiceController.GetServices();
            stopService("Realtek11nCU", services);
            stopService("RealtekCU", services);
            System.Diagnostics.Trace.Listeners.Add(new GetawayTraceListener());
            events = new softEventQueue<SOFTEVENT>();
            events.processor += new EventHandler<softEventQueue<SOFTEVENT>.EventType>(events_processor);
            events.put(SOFTEVENT.LOAD, 0);
            events.start();
            disposed = false;
            notifier.notify("load");
        }

        void events_processor(object sender, softEventQueue<SOFTEVENT>.EventType e) {
            switch (e.ev) {
                case SOFTEVENT.LOAD:
                    this.icsManager = new IcsManager();
                    state_ = getawayState.Idle;
                    shouldStart_ = false;
                    privateManager = new privateNetManager();
                    events.put(SOFTEVENT.LOADINIT, 0);
                    break;
                case SOFTEVENT.LOADINIT:
                    try {
                        privateManager.init();
                    } catch {
                        startTimer();
                        break;
                    }
                    events.put(SOFTEVENT.LOAD_SAVED_STATE, 0);
                    NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(NetworkChange_NetworkAddressChanged);
                    NetworkChange.NetworkAvailabilityChanged += new NetworkAvailabilityChangedEventHandler(NetworkChange_NetworkAvailabilityChanged);
                    privateManager.networkAvailable += new EventHandler(privateManager_networkAvailable);
                    privateManager.networkUnavailable += new EventHandler(privateManager_networkUnavailable);
                    break;
                case SOFTEVENT.LOAD_SAVED_STATE:
                    LoadConfig();
                    if (shouldStart_)
                        Start();
                    break;
                case SOFTEVENT.NETWORKADDRESSCHANGED:
                    networkAddressChanged();
                    break;
                case SOFTEVENT.STARTINIT:
                    privateManager.enable();
                    startTimer();
                    break;
                case SOFTEVENT.RENEWIP:
                    reNewIp();
                    startTimer();
                    break;
                case SOFTEVENT.UPDATECONNECTIONS:
                    icsManager.updateConnections();
                    break;
                case SOFTEVENT.RESTARTPRIVATE:
                    intStop();
                    if (shouldStart_)
                        privateManager.enable();
                    break;
                case SOFTEVENT.TESTPRIVATE:
                    testPrivateConnected();
                    break;
                case SOFTEVENT.FIREROUTESTART:
                    watchDog.Change(Timeout.Infinite, Timeout.Infinite);
                    if (OnRouterStart != null) OnRouterStart(this, null);
                    break;
                case SOFTEVENT.INTSTOP:
                    intStop();
                    break;
            }
        }

        void privateManager_networkUnavailable(object sender, EventArgs e) {
            events.put(SOFTEVENT.UPDATECONNECTIONS, 0);
            events.put(SOFTEVENT.RESTARTPRIVATE, 0);
        }

        void privateManager_networkAvailable(object sender, EventArgs e) {
            events.put(SOFTEVENT.UPDATECONNECTIONS, 0);
            if (shouldStart_)
                events.put(SOFTEVENT.TESTPRIVATE, 0);
        }

        void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e) {
            Trace.TraceInformation("Network availability changed");
        }

        void NetworkChange_NetworkAddressChanged(object sender, EventArgs e) {
            Trace.TraceInformation("Network address changed");
            events.put(SOFTEVENT.UPDATECONNECTIONS, 0);
            events.put(SOFTEVENT.NETWORKADDRESSCHANGED, 0);
        }

        void networkAddressChanged() {
            if (shouldStart_) {
                if (!icsManager.IsPublicConnected()) {
                    publicConnected_ = false;
                } else
                    if (!publicConnected_) {
                        publicConnected_ = true;
                        events.put(SOFTEVENT.INTSTOP, 0);
                        events.put(SOFTEVENT.TESTPRIVATE, 0);
                    } else {
                        if (state_ == getawayState.StartingIP) {
                            events.put(SOFTEVENT.RENEWIP, 0);
                            return;
                        }
                        if (state_ != getawayState.Started) return;
                        var currentPrivateConnection = IcsManager.getWMI(privateManager.connectionGuid);
                        if (currentPrivateConnection != null) {
                            string[] addresses = (string[])currentPrivateConnection["IPAddress"];
                            if (!addresses[0].Equals(GetIP())) {
                                Trace.TraceInformation("Private network address changed to:" + addresses[0]);
                                events.put(SOFTEVENT.RENEWIP, 0);
                            }
                        }
                    }
            }
        }

        void testPrivateConnected() {
            if (state_ == getawayState.Starting) return;
            if (state_ == getawayState.StartingIP) return;
            if (state_ == getawayState.Started) return;
            privateManager.updateConnectionGuid();
            if (privateManager.connectionGuid != null) {
                if (privateManager.Started)
                    privateManager.networkStop();
                else {
                    Trace.TraceInformation("Getaway starting");
                    state_ = getawayState.Starting;
                    if (publicConnected_)
                        try {
                            if (this.icsManager.SharingInstalled) {
                                this.icsManager.setPrivateConnection(privateManager.connectionGuid);
                                privateManager.startNetwork();
                                icsManager.EnableIcs();
                                state_ = getawayState.StartingIP;
                                return;
                            }
                        } catch (Exception e) {
                            Trace.TraceInformation("Getaway start failed: {0}", e.Message);
                        }
                    state_ = getawayState.StartFailed;
                }
            }
        }

        private void intStop() {
            if (state_ == getawayState.Stopping) return;
            if (state_ == getawayState.Stopped) return;
            if (state_ == getawayState.StopFailed) return;
            Trace.TraceInformation("Getaway stopping");
            state_ = getawayState.Stopping;
            try {
                privateManager.networkStop();
                if (this.icsManager.SharingInstalled) {
                    this.icsManager.DisableIcsOnAll();
                }
                state_ = getawayState.Stopped;
            } catch {
                state_ = getawayState.StopFailed;
            }
        }

        #region IsoftGetawayHost Members

        public void Start() {
            //          if (DateTime.Now > DateTime.ParseExact("15.04.2013", "dd.MM.yyyy", null)) return; //Date
            if (this.currentSharedConnectionGuid != null) {
                shouldStart_ = true;
                icsManager.setPublicConnection(this.currentSharedConnectionGuid);
                publicConnected_ = icsManager.IsPublicConnected();

                state_ = getawayState.Initialization;
                events.put(SOFTEVENT.STARTINIT, 0);
            }
        }

        public void Start(string sharedConnectionGuid) {
            this.currentSharedConnectionGuid = sharedConnectionGuid;
            Start();
        }

        public void Stop() {
            shouldStart_ = false;
            events.put(SOFTEVENT.INTSTOP, 0);
        }

        public bool SetPrivateConnectionSettings(ConnectionSettings settings) {
            return privateManager.setConnection(settings);
        }

        public ConnectionSettings GetPrivateConnectionSettings() {
            return privateManager.getConnection();
        }

        public IEnumerable<SharableConnection> GetSharableConnections() {
            List<IcsConnection> connections;
            try {
                lock (IcsManager.connectionsLock) {
                    connections = IcsManager.Connections;
                }
            } catch {
                connections = new List<IcsConnection>();
            }
            // Empty item to signify No Connection Sharing
            yield return new SharableConnection() { DeviceName = "Auto", Guid = IcsManager.autoPublicConnection, Name = "Auto" };

            if (connections != null) {
                foreach (var conn in connections) {
                    if (conn.IsSupported) {
                        yield return new SharableConnection(conn);
                    }
                }
            }
            yield return new SharableConnection() { DeviceName = "None", Guid = null, Name = "None" };
        }

        public IEnumerable<NetworkPeerService> GetPeers() {
            foreach (var v in privateManager.Peers) {
                v.Value.storage.MACAddress = v.Key;
                yield return v.Value;
            }
        }

        public void SetPeer(NetworkPeerService peer) {
            privateManager.SetPeer(peer.storage.MACAddress, peer);
        }

        public string GetSharedConnection() {
            return this.currentSharedConnectionGuid;
        }

        public string GetIP() {
            return this.icsManager.GetIP();
        }

        public bool SetIP(string IP) {
            this.icsManager.SetIP(IP);
            if (state_ == getawayState.Started)
                return reNewIp();
            return true;
        }

        string strStateFileName = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "getaway.");
        private string currentSharedConnectionGuid;

        object objectFromFile(string fileName, Type type) {
            object obj;
            try {
                if (File.Exists(fileName)) {
                    Trace.TraceInformation("Getaway loading config {0}", fileName);
                    using (var file = new StreamReader(fileName)) {
                        var serializer = new DataContractSerializer(type);
                        obj = serializer.ReadObject(file.BaseStream);
                        file.Close();
                        return obj;
                    }
                }
            } catch (Exception ex) {
                Trace.TraceInformation("Getaway load config failed: " + ex.ToString());
            }
            return null;
        }

        public void LoadConfig() {
            softGetawayServiceState serviceState = (softGetawayServiceState)objectFromFile(strStateFileName + "config", typeof(softGetawayServiceState));
            if (serviceState != null) {
                // Set SSID and Password
                SetPrivateConnectionSettings(new ConnectionSettings() {
                    SSID = serviceState.wifiSSID,
                    MaxPeerCount = serviceState.limitClientsCount,
                    Password = serviceState.wifiPassword
                });
                SetIP(serviceState.privateIP);
                softGetawayPeersStorage peersState;
                peersState = (softGetawayPeersStorage)objectFromFile(strStateFileName + "peers", typeof(softGetawayPeersStorage));
                if ((peersState != null) && (peersState.peers != null)) {
                    foreach (var p in peersState.peers)
                        privateManager.AddPeerFromStorage(p);
                }
                this.currentSharedConnectionGuid = serviceState.publicConnectionGuid;
                shouldStart_ = serviceState.active;
            }
        }

        void objectToFile(string fileName, Object obj) {
            StreamWriter sw = null;
            try {
                sw = new StreamWriter(new FileStream(fileName, FileMode.Create));
                var stream = sw.BaseStream;
                var serializer = new DataContractSerializer(obj.GetType());
                serializer.WriteObject(stream, obj);
            } catch (Exception ex) {
                Trace.TraceInformation("Getaway save config failed: " + ex.ToString());
            } finally {
                if (sw != null) {
                    if (sw.BaseStream != null)
                        sw.BaseStream.Close();
                    else
                        sw.Close();
                }
            }
        }

        public void SaveConfig() {
            Trace.TraceInformation("Getaway saving config");

            var serviceState = new softGetawayServiceState();
            serviceState.active = shouldStart_;

            var connGuid = GetSharedConnection();

            if (connGuid != null) {
                serviceState.publicConnectionGuid = connGuid;
            }

            var connSettings = GetPrivateConnectionSettings();
            serviceState.wifiSSID = connSettings.SSID;
            serviceState.limitClientsCount = connSettings.MaxPeerCount;

            serviceState.wifiPassword = connSettings.Password;

            serviceState.privateIP = GetIP();
            objectToFile(strStateFileName + "config", serviceState);
            var peersState = new softGetawayPeersStorage();
            foreach (var peerService in GetPeers()) {
                NetworkPeerStorage storage = (NetworkPeerStorage)peerService.storage.Clone();
                if (!peerService.isSetHostName) storage.HostName = null;
                if (!peerService.isSetIP) storage.IPAddress = null;
                peersState.peers.Add(storage);
            }
            objectToFile(strStateFileName + "peers", peersState);

            Trace.TraceInformation("Getaway config saved");
        }

        public getawayState GetState() {
            return state_;
        }

        public bool IsShouldStart() {
            return shouldStart_;
        }

        public IEnumerable<String> GetTraceLines() {
            return GetawayTraceListener.getLines();
        }

        #endregion

        bool reNewIp() {
            string ip = GetIP();
            privateManager.stopDHCP();
            if ((privateManager.connectionGuid != null) && !String.IsNullOrEmpty(ip) && shouldStart_) {
                if (privateManager.setIP(ip)) {
                    events.put(SOFTEVENT.FIREROUTESTART, 0);
                    state_ = getawayState.Started;
                    notifier.notify("start");
                } else {
                    state_ = getawayState.StartFailed;
                    events.put(SOFTEVENT.RENEWIP, 0);
                    return false;
                }
            }
            if (OnRouterStart != null) OnRouterStart(this, null);
            state_ = getawayState.Started;
            notifier.notify("start");
            return true;
        }
    }
    enum SOFTEVENT {
        LOAD_SAVED_STATE,
        EXIT,
        LOAD,
        LOADINIT,
        STARTINIT,
        UPDATECONNECTIONS,
        RESTARTPRIVATE,
        TESTPRIVATE,
        NETWORKADDRESSCHANGED,
        RENEWIP,
        FIREROUTESTART,
        INTSTOP
    }
}
