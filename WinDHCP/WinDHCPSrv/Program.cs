using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WinDHCP.Library.Configuration;
using WinDHCP.Library;
using System.Diagnostics;
using System.Configuration;

namespace WinDHCPSrv
{
    class Program
    {
        private static Boolean ContainsSwitch(String[] args, String switchStr)
        {
            foreach (String arg in args)
            {
                if (arg.StartsWith("--") && arg.Length > 2 && switchStr.StartsWith(arg.Substring(2), StringComparison.OrdinalIgnoreCase) ||
                    (arg.StartsWith("/") || arg.StartsWith("-")) && arg.Length > 1 && switchStr.StartsWith(arg.Substring(1), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        static void Main(string[] args)
        {
            DhcpServerConfigurationSection dhcpConfig = ConfigurationManager.GetSection("dhcpServer") as DhcpServerConfigurationSection;
            DhcpServer server = new DhcpServer();

            if (dhcpConfig != null)
            {
                if (dhcpConfig != null)
                {
                    if (dhcpConfig.NetworkInterface >= 0)
                    {
                        server.DhcpInterface = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()[dhcpConfig.NetworkInterface];
                    }

                    server.StartAddress = InternetAddress.Parse(dhcpConfig.StartAddress.Trim());
                    server.EndAddress = InternetAddress.Parse(dhcpConfig.EndAddress.Trim());
                    server.Subnet = InternetAddress.Parse(dhcpConfig.Subnet.Trim());
                    server.Gateway = InternetAddress.Parse(dhcpConfig.Gateway.Trim());
                    server.LeaseDuration = dhcpConfig.LeaseDuration;
                    server.OfferTimeout = dhcpConfig.OfferTimeout;
                    server.DnsSuffix = dhcpConfig.DnsSuffix;

                    server.StartAddress = InternetAddress.Parse("2.0.4.1");
                    server.EndAddress = InternetAddress.Parse("2.0.4.10");
                    server.Subnet = InternetAddress.Parse("255.255.255.0");

                    foreach (InternetAddressElement dnsServer in dhcpConfig.DnsServers)
                    {
                        server.DnsServers.Add(InternetAddress.Parse(dnsServer.IPAddress.Trim()));
                    }

                    foreach (PhysicalAddressElement macAllow in dhcpConfig.MacAllowList)
                    {
                        if (macAllow.PhysicalAddress.Trim() == "*")
                        {
                            server.ClearAcls();
                            server.AllowAny = true;
                            break;
                        }
                        else
                        {
                            server.AddAcl(PhysicalAddress.Parse(macAllow.PhysicalAddress), false);
                        }
                    }

                    foreach (PhysicalAddressElement macDeny in dhcpConfig.MacDenyList)
                    {
                        if (macDeny.PhysicalAddress.Trim() == "*")
                        {
                            server.ClearAcls();
                            server.AllowAny = false;
                            break;
                        }
                        else
                        {
                            server.AddAcl(PhysicalAddress.Parse(macDeny.PhysicalAddress), true);
                        }
                    }

                    foreach (PhysicalAddressMappingElement macReservation in dhcpConfig.MacReservationList)
                    {
                        server.addReservation(macReservation.PhysicalAddress, macReservation.IPAddress);
                    }
                }

                server.Start();
                Console.WriteLine("DHCP Service Running.");
                Console.WriteLine("Hit [Enter] to Terminate.");

                Console.ReadLine();
            
            }

        }
    }
}
