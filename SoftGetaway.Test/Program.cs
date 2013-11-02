using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VirtualRouterHost;
using System.Threading;
using System.Diagnostics;

namespace VirtualRouterTest
{
    class Program
    {
        static void Main(string[] args)
        {
            wlanEnabler enabler = new wlanEnabler();
            VirtualRouterHost.IVirtualRouterHost host;
            host = new VirtualRouterHost.VirtualRouterHost();
            host.SetPassword("egordasha");
            host.SetConnectionSettings("User-AP", 10);
            IEnumerable<SharableConnection> list = host.GetSharableConnections();
            SharableConnection ics = (from c in list
                                      where c.Name.Contains("vpnclient")
                                      select c).First();
            if (host.Start(ics) && host.SetIP("2.0.4.1"))
                Console.ReadLine();
        }
    }

    class wlanEnabler
    {
        void wlanCmd(string cmd)
        {
            try
            {
                Process netsh = new Process();
                netsh.StartInfo.FileName = @"C:\Windows\System32\netsh.exe";
                netsh.StartInfo.Arguments = "wlan " + cmd;
                netsh.Start();
                netsh.WaitForExit();
            }
            catch (Exception ex)
            {

                //EventLog.WriteEntry("Netsh failed (start) :" + Environment.NewLine + ex.ToString());
            }
        }

        public void enable()
        {
            wlanCmd("set hostednetwork mode=allow");
        }
    }

}
