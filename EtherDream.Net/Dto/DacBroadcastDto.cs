using System.Runtime.InteropServices;

namespace LaserCore.EtherDream.Net.Dto
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct DacBroadcastDto
    {
        public fixed byte MacAddress[6];
        public ushort HwVersion;
        public ushort SwVersion;
        public ushort BufferCapacity;
        public uint MaxPointRate;
        public DacStatusDto DacStatus;
    }
}
