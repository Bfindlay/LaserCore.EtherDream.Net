using System.Runtime.InteropServices;

namespace EtherDream.Net.Dto
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe public struct DacBroadcastDto
    {
        public fixed byte MacAddress[6];
        public ushort HwVersion;
        public ushort SwVersion;
        public ushort BufferCapacity;
        public uint MaxPointRate;
        public DacStatusDto DacStatus;
    }
}
