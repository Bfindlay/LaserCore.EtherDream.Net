using System.Runtime.InteropServices;

namespace LaserCore.EtherDream.Net.Dto
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BeginCommandDto
    {
        public byte Command; /* 'b' (0x62) */
        public ushort LowWaterMark;
        public uint PointRate;
    }
}
