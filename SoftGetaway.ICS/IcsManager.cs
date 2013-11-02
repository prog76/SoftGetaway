using System;
using System.Collections.Generic;
using System.Linq;
using NETCONLib;
using System.Management;
using Microsoft.Win32;
using System.Net;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace IcsMgr
{
	public class OnNewIcsClientEvent : EventArgs
	{
		public String mac;
		public String ip;
	}
	public class IcsManager
	{
		protected INetSharingManager _NSManager;
		string privateIP;
		string privateStrGUID;
		string publicStrGuid;
		public static object connectionsLock;
		public static string autoPublicConnection = "{B6F98901-63A6-4936-BF2B-0B1B64D16005}";
		IcsConnection publicConn;

		public IcsManager()
		{
			this.Init();
			privateIP = "";
			privateStrGUID = "";
			connectionsLock = "";
			publicConn = null;
			updateConnections();
		}

		public void setPublicConnection(string guid)
		{
			publicStrGuid = guid;
            publicConn = null;
            if (publicStrGuid.Equals(autoPublicConnection))return;
			lock (connectionsLock)
			{
    			publicConn = (from c in Connections
								  where c.IsMatch(guid)
								  select c).FirstOrDefault();
			}
		}

		public void setPrivateConnection(string guid)
		{
			privateStrGUID = guid;
		}

		public bool IsPublicConnected()
		{
			if ((publicConn != null) && publicConn.IsConnected) return true;
			if (publicStrGuid.Equals(autoPublicConnection))
			{
				NetworkInterface nicInet = null;
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces()) {
                    IPv4InterfaceProperties ip4Props = nic.GetIPProperties().GetIPv4Properties();
                    if ((nic.GetIPProperties().GatewayAddresses.Count>0)&&(ip4Props!=null) && (nic.OperationalStatus == OperationalStatus.Up)) {
                        if (nicInet == null) nicInet = nic;
                        else
                            if (ip4Props.Index < nicInet.GetIPProperties().GetIPv4Properties().Index)
                                nicInet = nic;
                    }
                }
				if (nicInet != null)
					lock (connectionsLock)
					{
						publicConn = (from c in Connections
										  where !c.IsMatch(privateStrGUID) &&
											c.IsMatch(nicInet.Id)
										  select c).FirstOrDefault();
                        if(publicConn!=null)
                            Trace.TraceInformation("ICS: Detected internet connection {0}", nicInet.Name); 
					}
				else
				{
					Trace.TraceInformation("ICS: Unable to lookup internet connection");
				}
			}
			return ((publicConn != null) && publicConn.IsConnected);
		}

		public void Init()
		{
			this._NSManager = new NetSharingManagerClass();
		}

        void fixAdapters(string query) { //http://support.microsoft.com/kb/828807
            ManagementObjectSearcher wmiInterfaces = new ManagementObjectSearcher("/root/Microsoft/HomeNet", "SELECT * FROM HNet_ConnectionProperties WHERE "+query);
            foreach (ManagementObject wmiInterface in wmiInterfaces.Get()) {
                wmiInterface.SetPropertyValue("IsIcsPrivate", false);
                wmiInterface.SetPropertyValue("IsIcsPublic", false);
                wmiInterface.Put();
            }
        }

		public void EnableIcs()
		{
			if (!this.SharingInstalled)
			{
				Trace.TraceInformation("ICS: Sharing NOT Installed");
				throw new Exception("Sharing NOT Installed");
			}

			if (privateStrGUID.Equals(publicStrGuid))
			{
				Trace.TraceInformation("ICS:public=shared={0}", privateStrGUID);
				throw new Exception("Unable to share connection over itself");
			}

			try
			{
				setIpRegistry(privateStrGUID, "0.0.0.0", "255.255.255.0");
				setDHCPScope("255.255.255.255");
				IcsConnection privateConn;

                try {
                    publicConn.EnableAsPublic();
                } catch {
                    fixAdapters("IsIcsPublic = TRUE");
                    publicConn.EnableAsPublic();
                }

				lock (connectionsLock)
				{
					privateConn = (from c in Connections
										where c.IsMatch(privateStrGUID)
										select c).First();
				}
				Trace.TraceInformation("ICS: Sharing Enabling");

                try {
                    privateConn.EnableAsPrivate();
                } catch {
                    fixAdapters("IsIcsPrivate  = TRUE");
                    privateConn.EnableAsPrivate();
                }
			}
			catch (Exception e)
			{
				Trace.TraceInformation("ICS: Sharing failed to start {0}", e.Message);
			}
		}

		public void DisableIcsOnAll()
		{
			Trace.TraceInformation("ICS: Sharing disabling");
			lock (connectionsLock)
			{
				foreach (var conn in Connections)
				{
					if (conn.IsSupported)
					{
						conn.DisableSharing();
					}
				}
			}
			setIpRegistry(privateStrGUID, "0.0.0.0", "255.255.255.0");
			setDHCPScope("192.168.100.1");
		}

		private static List<IcsConnection> _Connections = null;
		public void updateConnections()
		{
			lock (connectionsLock)
			{
				_Connections = new List<IcsConnection>();
				try
				{
					foreach (INetConnection conn in this._NSManager.EnumEveryConnection)
					{
                        IcsConnection c = new IcsConnection(this._NSManager, conn);
                        try{
                            if(c.DeviceName!=null)
                              _Connections.Add(c);
                        }catch{
                        }
					};
				}
				catch
				{
					Trace.TraceInformation("ICS: Unable to get list of connections");
				}
			}
		}
		public static List<IcsConnection> Connections
		{
			get
			{
				return _Connections;
			}
		}

		public bool SharingInstalled
		{
			get
			{
				return this._NSManager.SharingInstalled;
			}
		}

		public string GetIP()
		{
			return privateIP;
		}

		public void SetIP(string IP)
		{
			this.privateIP = IP;
		}

		[DllImport("dhcpcsvc.dll", EntryPoint = "DhcpNotifyConfigChange")]
		public static extern uint DhcpNotifyConfigChange(
			 [In, MarshalAs(UnmanagedType.LPStr)] string serverName, // Local machine = NULL
			 [In, MarshalAs(UnmanagedType.LPStr)] string adapterName,
			 bool newIpAddress,
			 uint dwIpIndex, // IP address index (0 based)
			 uint dwIpAddress, // IP address (network order)
			 uint dwSubNetMask, // Subnet mask (network order)
			 int nDhcpAction); // 0:don't modify/1:enable/2:disable DHCP;

		private static bool setDHCPScope(string IpAddress)
		{
			RegistryKey myKey = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\services\\SharedAccess\\Parameters", true);
			myKey.SetValue("ScopeAddress", IpAddress, RegistryValueKind.String);
			myKey.SetValue("ScopeAddressBackup", IpAddress, RegistryValueKind.String);
			myKey.SetValue("StandaloneDhcpAddress", IpAddress, RegistryValueKind.String);
			return true;
		}

		public static void setIpRegistry(string guid, string IpAddress, string SubnetMask)
		{
			RegistryKey myKey = Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\services\\Tcpip\\Parameters\\Interfaces\\" + guid, true);
			myKey.SetValue("IPAddress", new string[] { IpAddress }, RegistryValueKind.MultiString);
			myKey.SetValue("SubnetMask", new string[] { SubnetMask }, RegistryValueKind.MultiString);
			uint res = DhcpNotifyConfigChange(null, guid, true, 0, inet_addr(IpAddress), inet_addr(SubnetMask), 2);
		}

		public static ManagementObject getWMI(string guid)
		{
			ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration");
			ManagementObjectCollection moc = mc.GetInstances();

			foreach (ManagementObject mo in moc)
			{
				// Make sure this is a IP enabled device. 
				// Not something like memory card or VM Ware
				if ((Boolean)mo["IPEnabled"] && mo["SettingID"].ToString().ToUpper().Equals(guid))
				{
					return mo;
				}
			}
			return null;
		}

		public static bool setIpWMI(string guid, string IpAddress, string SubnetMask)
		{
			try
			{
				var mo = getWMI(guid);
				ManagementBaseObject newIP = mo.GetMethodParameters("EnableStatic");
				newIP["IPAddress"] = new string[] { IpAddress };
				newIP["SubnetMask"] = new string[] { SubnetMask };
				ManagementBaseObject setIP = mo.InvokeMethod("EnableStatic", newIP, null);
				setIP.Dispose();
				return true;
			}
			catch (Exception e)
			{
				Trace.TraceInformation("Internet Sharing Unable to set up IP:" + e.Message);
			}
			return false;
		}

		private static uint inet_addr(string ip)
		{
			var addr = IPAddress.Parse(ip);
			return BitConverter.ToUInt32((addr.GetAddressBytes()), 0);
		}
	}
}
