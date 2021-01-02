using System.Runtime.InteropServices;
namespace EtherDream.Net.Dto
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe public struct QueueChangeCommandDto
    {
        public byte Command; /* 'q' (0x74) */
        public uint PointRate;
    }
}
