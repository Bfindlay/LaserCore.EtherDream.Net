using System.Runtime.InteropServices;

namespace EtherDream.Net.Dto
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe public struct DacDto
    {
        public DacBroadcastDto Identity;
        public string Ip;
    }
}
