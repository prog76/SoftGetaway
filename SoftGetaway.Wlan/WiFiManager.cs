using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using softGetaway.WiFi.WinAPI;
using System.Diagnostics;

namespace softGetaway.WiFi
{
    public class WiFiManager : IDisposable
    {
        private IntPtr _WlanHandle;
        private uint _ServerVersion;

        private wlanapi.WLAN_NOTIFICATION_CALLBACK _notificationCallback;

        private WLAN_HOSTED_NETWORK_STATE _HostedNetworkState;

        public WiFiManager()
        {
            this._notificationCallback = new wlanapi.WLAN_NOTIFICATION_CALLBACK(this.OnNotification);
            this.Init();
        }

        public void enableHosted()
        {
            wlanProperty<bool> prop = new wlanProperty<bool>(this._WlanHandle);
//            if (!prop.get(WLAN_HOSTED_NETWORK_OPCODE.wlan_hosted_network_opcode_enable))   
            if(this._HostedNetworkState == WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_unavailable)
            {
                Trace.TraceInformation("WiFi AdHoc Enabling");
                prop.set(WLAN_HOSTED_NETWORK_OPCODE.wlan_hosted_network_opcode_enable, true);
            }
            else onHostedNetworkAvailable();
        }

        private void Init()
        {
            try
            {
                WiFiUtils.Throw_On_Win32_Error(wlanapi.WlanOpenHandle(wlanapi.WLAN_CLIENT_VERSION_VISTA, IntPtr.Zero, out this._ServerVersion, ref this._WlanHandle));
                WLAN_NOTIFICATION_SOURCE notifSource;
                WiFiUtils.Throw_On_Win32_Error(wlanapi.WlanRegisterNotification(this._WlanHandle, WLAN_NOTIFICATION_SOURCE.All, true, this._notificationCallback, IntPtr.Zero, IntPtr.Zero, out notifSource));
                WLAN_HOSTED_NETWORK_REASON failReason = this.InitSettings();
                if (failReason != WLAN_HOSTED_NETWORK_REASON.wlan_hosted_network_reason_success)
                {
                    Trace.TraceInformation("WiFi Init Error WlanHostedNetworkInitSettings: " + failReason.ToString());
                    throw new Exception("Init Error WlanHostedNetworkInitSettings: " + failReason.ToString());
                }
            }
            catch
            {
                wlanapi.WlanCloseHandle(this._WlanHandle, IntPtr.Zero);
                Trace.TraceInformation("WiFi Init Error");
                throw;
            }
        }

        #region "Events"

        public event EventHandler HostedNetworkStarted;
        public event EventHandler HostedNetworkStopped;
        public event EventHandler HostedNetworkAvailable;
        public event EventHandler HostedNetworkUnavailable;

        public event EventHandler<WiFiManager.EventStationJoin> StationJoin;
        public event EventHandler<WiFiManager.EventStationLeave> StationLeave;
        public event EventHandler StationStateChange;

        public class EventStationJoin : EventArgs {
            public WiFiStation pStation;

            public EventStationJoin(WiFiStation pStation) {
                this.pStation = pStation;
            }
        }

        public class EventStationLeave : EventArgs {
            public string MACAddress;
            public EventStationLeave(string MACAddress) {
                this.MACAddress = MACAddress;
            }
        }

        #endregion

        #region "OnNotification"

        protected void onHostedNetworkStarted()
        {
            this._HostedNetworkState = WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_active;
            if (this.HostedNetworkStarted != null)
            {
                this.HostedNetworkStarted(this, EventArgs.Empty);
            }
        }

        protected void onHostedNetworkStopped()
        {
            this._HostedNetworkState = WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_idle;
            if (this.HostedNetworkStopped != null)
            {
                this.HostedNetworkStopped(this, EventArgs.Empty);
            }
        }

        protected void onHostedNetworkAvailable()
        {
            this._HostedNetworkState = WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_idle;
            if (this.HostedNetworkAvailable != null)
            {
                this.HostedNetworkAvailable(this, EventArgs.Empty);
            }
        }

        protected void onHostedNetworkUnavailable()
        {
            this._HostedNetworkState = WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_unavailable;
            if (this.HostedNetworkUnavailable != null)
            {
                this.HostedNetworkUnavailable(this, EventArgs.Empty);
            }
        }

        protected void onStationJoin(WLAN_HOSTED_NETWORK_PEER_STATE stationState)
        {
            var pStation = new WiFiStation(stationState);
            Trace.TraceInformation("WiFi station join {0}", pStation.MacAddress);
            if (this.StationJoin != null)
            {
                this.StationJoin(this, new EventStationJoin(pStation));
            }
        }

        protected void onStationLeave(WLAN_HOSTED_NETWORK_PEER_STATE stationState)
        {
            string MACAddress = stationState.PeerMacAddress.ConvertToString();
            Trace.TraceInformation("WiFi station leave {0}", MACAddress);
            if (this.StationLeave != null)
            {
                this.StationLeave(this, new EventStationLeave(MACAddress));
            }
        }

        protected void onStationStateChange(WLAN_HOSTED_NETWORK_PEER_STATE stationState)
        {
            if (this.StationStateChange != null)
            {
                this.StationStateChange(this, EventArgs.Empty);
            }
        }

        protected void OnNotification(ref WLAN_NOTIFICATION_DATA notifData, IntPtr context)
        {
            switch (notifData.notificationCode)
            {
                case (int)WLAN_HOSTED_NETWORK_NOTIFICATION_CODE.wlan_hosted_network_state_change:

                    if (notifData.dataSize > 0 && notifData.dataPtr != IntPtr.Zero)
                    {
                        WLAN_HOSTED_NETWORK_STATE_CHANGE pStateChange = (WLAN_HOSTED_NETWORK_STATE_CHANGE)Marshal.PtrToStructure(notifData.dataPtr, typeof(WLAN_HOSTED_NETWORK_STATE_CHANGE));
                        switch (pStateChange.NewState)
                        {
                            case WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_active:
                                {
                                    Trace.TraceInformation("WiFi AdHoc Started");
                                    this.onHostedNetworkStarted();
                                }
                                break;

                            case WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_idle:
                                if (pStateChange.OldState == WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_active)
                                {
                                    Trace.TraceInformation("WiFi AdHoc Stopped");
                                    this.onHostedNetworkStopped();
                                }
                                else
                                {
                                    Trace.TraceInformation("WiFi AdHoc Available");
                                    this.onHostedNetworkAvailable();
                                }
                                break;

                            case WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_unavailable:
                                {
                                    Trace.TraceInformation("WiFi AdHoc Unavailable");
                                    if (pStateChange.OldState != WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_unavailable)
                                    {
                                        this.onHostedNetworkUnavailable();
                                    }
                                }
                                break;
                        }
                    }

                    break;

                case (int)WLAN_HOSTED_NETWORK_NOTIFICATION_CODE.wlan_hosted_network_peer_state_change:

                    if (notifData.dataSize > 0 && notifData.dataPtr != IntPtr.Zero)
                    {
                        WLAN_HOSTED_NETWORK_DATA_PEER_STATE_CHANGE pPeerStateChange = (WLAN_HOSTED_NETWORK_DATA_PEER_STATE_CHANGE)Marshal.PtrToStructure(notifData.dataPtr, typeof(WLAN_HOSTED_NETWORK_DATA_PEER_STATE_CHANGE));

                        if (pPeerStateChange.NewState.PeerAuthState == WLAN_HOSTED_NETWORK_PEER_AUTH_STATE.wlan_hosted_network_peer_state_authenticated)
                        {
                            // Station joined the hosted network
                            Trace.TraceInformation("WiFi Station join");
                            this.onStationJoin(pPeerStateChange.NewState);
                        }
                        else if (pPeerStateChange.NewState.PeerAuthState == WLAN_HOSTED_NETWORK_PEER_AUTH_STATE.wlan_hosted_network_peer_state_invalid)
                        {
                            // Station left the hosted network
                            Trace.TraceInformation("WiFi Station leave");
                            this.onStationLeave(pPeerStateChange.NewState);
                        }
                        else
                        {
                            // Authentication state changed
                            Trace.TraceInformation("WiFi Station change");
                            this.onStationStateChange(pPeerStateChange.NewState);
                        }
                    }

                    break;

                case (int)WLAN_HOSTED_NETWORK_NOTIFICATION_CODE.wlan_hosted_network_radio_state_change:
                    if (notifData.dataSize > 0 && notifData.dataPtr != IntPtr.Zero)
                    {
                        //WLAN_HOSTED_NETWORK_RADIO_STATE pRadioState = (WLAN_HOSTED_NETWORK_RADIO_STATE)Marshal.PtrToStructure(notifData.dataPtr, typeof(WLAN_HOSTED_NETWORK_RADIO_STATE));
                        // Do nothing for now
                    }
                    //else
                    //{
                    //    // // Shall NOT happen
                    //    // _ASSERT(FAILSE);
                    //}
                    break;
            }

        }

        #endregion

        #region "Public Methods"

        public WLAN_HOSTED_NETWORK_REASON ForceStart()
        {
            WLAN_HOSTED_NETWORK_REASON failReason;
            WiFiUtils.Throw_On_Win32_Error(wlanapi.WlanHostedNetworkForceStart(this._WlanHandle, out failReason, IntPtr.Zero));

            this._HostedNetworkState = WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_active;

            return failReason;
        }

        public WLAN_HOSTED_NETWORK_REASON ForceStop()
        {
            switch (this._HostedNetworkState)
            {
                case WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_active:
                    Trace.TraceInformation("WiFi AdHoc Stopping");
                    WLAN_HOSTED_NETWORK_REASON failReason;
                    WiFiUtils.Throw_On_Win32_Error(wlanapi.WlanHostedNetworkForceStop(this._WlanHandle, out failReason, IntPtr.Zero));
                    this._HostedNetworkState = WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_idle;
                    return failReason;
                case WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_idle:
            //        this.onHostedNetworkAvailable();
                    break;
                case WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_unavailable:
            //        this.onHostedNetworkUnavailable();
                    break;
            }
            return WLAN_HOSTED_NETWORK_REASON.wlan_hosted_network_reason_success;
        }

        public WLAN_HOSTED_NETWORK_REASON StartUsing()
        {
            if (this._HostedNetworkState == WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_active)
            {
       //         this.onHostedNetworkStarted();
                return WLAN_HOSTED_NETWORK_REASON.wlan_hosted_network_reason_success;
            }
            Trace.TraceInformation("WiFi AdHoc Starting");
            WLAN_HOSTED_NETWORK_REASON failReason;
            WiFiUtils.Throw_On_Win32_Error(wlanapi.WlanHostedNetworkStartUsing(this._WlanHandle, out failReason, IntPtr.Zero));

            this._HostedNetworkState = WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_active;

            return failReason;
        }

        public WLAN_HOSTED_NETWORK_REASON StopUsing()
        {
            if (this._HostedNetworkState != WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_active)
            {
                this.onHostedNetworkAvailable();
                return WLAN_HOSTED_NETWORK_REASON.wlan_hosted_network_reason_success;
            }
            WLAN_HOSTED_NETWORK_REASON failReason;
            WiFiUtils.Throw_On_Win32_Error(wlanapi.WlanHostedNetworkStopUsing(this._WlanHandle, out failReason, IntPtr.Zero));

            this._HostedNetworkState = WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_idle;

            return failReason;
        }

        public WLAN_HOSTED_NETWORK_REASON InitSettings()
        {
            WLAN_HOSTED_NETWORK_REASON failReason;
            WiFiUtils.Throw_On_Win32_Error(wlanapi.WlanHostedNetworkInitSettings(this._WlanHandle, out failReason, IntPtr.Zero));
            return failReason;
        }

        public WLAN_HOSTED_NETWORK_REASON QuerySecondaryKey(out string passKey, out bool isPassPhrase, out bool isPersistent)
        {
            WLAN_HOSTED_NETWORK_REASON failReason;
            uint keyLen;
            WiFiUtils.Throw_On_Win32_Error(wlanapi.WlanHostedNetworkQuerySecondaryKey(this._WlanHandle, out keyLen, out passKey, out isPassPhrase, out isPersistent, out failReason, IntPtr.Zero));
            return failReason;
        }

        public WLAN_HOSTED_NETWORK_REASON SetSecondaryKey(string passKey)
        {
            WLAN_HOSTED_NETWORK_REASON failReason;

            WiFiUtils.Throw_On_Win32_Error(wlanapi.WlanHostedNetworkSetSecondaryKey(this._WlanHandle, (uint)(passKey.Length + 1), passKey, true, true, out failReason, IntPtr.Zero));

            return failReason;
        }

        public WLAN_HOSTED_NETWORK_STATUS QueryStatus()
        {
            WLAN_HOSTED_NETWORK_STATUS status;
            WiFiUtils.Throw_On_Win32_Error(wlanapi.WlanHostedNetworkQueryStatus(this._WlanHandle, out status, IntPtr.Zero));
            return status;
        }

        public WLAN_HOSTED_NETWORK_REASON SetConnectionSettings(string hostedNetworkSSID, int maxNumberOfPeers)
        {
            WLAN_HOSTED_NETWORK_REASON failReason;

            WLAN_HOSTED_NETWORK_CONNECTION_SETTINGS settings = new WLAN_HOSTED_NETWORK_CONNECTION_SETTINGS();
            settings.hostedNetworkSSID = WiFiUtils.ConvertStringToDOT11_SSID(hostedNetworkSSID);
            settings.dwMaxNumberOfPeers = (uint)maxNumberOfPeers;

            wlanProperty<WLAN_HOSTED_NETWORK_CONNECTION_SETTINGS> prop = new wlanProperty<WLAN_HOSTED_NETWORK_CONNECTION_SETTINGS>(this._WlanHandle);
            prop.set(WLAN_HOSTED_NETWORK_OPCODE.wlan_hosted_network_opcode_connection_settings, settings, out failReason);

            return failReason;
        }

        public WLAN_OPCODE_VALUE_TYPE QueryConnectionSettings(out string hostedNetworkSSID, out int maxNumberOfPeers)
        {
            wlanProperty<WLAN_HOSTED_NETWORK_CONNECTION_SETTINGS> prop = new wlanProperty<WLAN_HOSTED_NETWORK_CONNECTION_SETTINGS>(this._WlanHandle);
            WLAN_OPCODE_VALUE_TYPE opcode;
            var settings = prop.get(WLAN_HOSTED_NETWORK_OPCODE.wlan_hosted_network_opcode_connection_settings, out opcode);

            hostedNetworkSSID = settings.hostedNetworkSSID.ConvertToString();

            maxNumberOfPeers = (int)settings.dwMaxNumberOfPeers;
            return opcode;
        }

        public void StartHostedNetwork()
        {
            try
            {
                var failReason = this.StartUsing();
                if (failReason != WLAN_HOSTED_NETWORK_REASON.wlan_hosted_network_reason_success)
                {
                    Trace.TraceInformation("WiFi Could Not Start Hosted Network: " + failReason.ToString());
                    throw new Exception("Could Not Start Hosted Network!\n\n" + failReason.ToString());
                }
            }
            catch
            {
                Trace.TraceInformation("WiFi Could Not Start Hosted Network");
                this.ForceStop();
                throw;
            }
        }

        public void StopHostedNetwork()
        {
            this.ForceStop();
        }

        #endregion

        #region "Properties"

        public string HostedNetworkInterfaceGuid
        {
            get
            {
                var status = this.QueryStatus();
                if (status.IPDeviceID == Guid.Empty) return null;
                return "{"+status.IPDeviceID.ToString().ToUpper()+"}";
            }
        }

        public WLAN_HOSTED_NETWORK_STATE HostedNetworkState
        {
            get
            {
                return this._HostedNetworkState;
            }
        }

        public bool IsHostedNetworkStarted
        {
            get
            {
                return (this._HostedNetworkState == WLAN_HOSTED_NETWORK_STATE.wlan_hosted_network_active);
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            this.ForceStop();

            if (this._WlanHandle != IntPtr.Zero)
            {
                wlanapi.WlanCloseHandle(this._WlanHandle, IntPtr.Zero);
            }
        }

        #endregion
    }

    class wlanProperty<T>
    {
        IntPtr handle, dataPtr;


        public wlanProperty(IntPtr handle_)
        {
            handle = handle_;
        }

        public T get(WLAN_HOSTED_NETWORK_OPCODE code, out WLAN_OPCODE_VALUE_TYPE opcode)
        {
            uint dataSize;
            WiFiUtils.Throw_On_Win32_Error(
                wlanapi.WlanHostedNetworkQueryProperty(
                    handle,
                    code,
                    out dataSize, out dataPtr, out opcode, IntPtr.Zero
                )
            );

            var data = (T)Marshal.PtrToStructure(dataPtr, typeof(T));
            return data;
        }

        public void set(WLAN_HOSTED_NETWORK_OPCODE code, T Value, out WLAN_HOSTED_NETWORK_REASON failReason)
        {
            dataPtr = Marshal.AllocHGlobal(Marshal.SizeOf(Value));
            Marshal.StructureToPtr(Value, dataPtr, false);

            WiFiUtils.Throw_On_Win32_Error(
                wlanapi.WlanHostedNetworkSetProperty(
                    handle,
                    code,
                    (uint)Marshal.SizeOf(Value), dataPtr, out failReason, IntPtr.Zero
                )
            );

        }

        public void set(WLAN_HOSTED_NETWORK_OPCODE code, T Value)
        {
            WLAN_HOSTED_NETWORK_REASON failReason;
            set(code, Value, out failReason);
        }

        public T get(WLAN_HOSTED_NETWORK_OPCODE code)
        {
            WLAN_OPCODE_VALUE_TYPE opcode;
            return get(code, out opcode);
        }
    }
}
