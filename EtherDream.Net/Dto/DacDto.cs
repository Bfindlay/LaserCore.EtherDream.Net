using System.Runtime.InteropServices;

namespace LaserCore.EtherDream.Net.Dto
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DacDto
    {
        public DacBroadcastDto Identity;
        public string Ip;
    }
}
