using System.Runtime.InteropServices;

namespace LaserCore.EtherDream.Net.Dto
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct QueueChangeCommandDto
    {
        public byte Command; /* 'q' (0x74) */
        public uint PointRate;
    }
}
