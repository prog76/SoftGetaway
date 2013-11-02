using System.Runtime.InteropServices;

namespace softGetaway.WiFi.WinAPI
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DOT11_BSSID_LIST
    {
        NDIS_OBJECT_HEADER header;
        uint uNumOfEntries; // ULONG
        uint uTotalNumOfEntries; // ULONG
        DOT11_MAC_ADDRESS[] BSSIDs;
    }
}
