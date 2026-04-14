using System.Runtime.InteropServices;

namespace LaserCore.EtherDream.Net.Dto
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DacResponseDto
    {
        public byte Response;
        public byte Command;
        public DacStatusDto DacStatus;
    }
}
