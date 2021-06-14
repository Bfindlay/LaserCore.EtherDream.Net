using System.Runtime.InteropServices;

namespace LaserCore.Etherdream.Net.Dto
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe public struct DacResponseDto
    {
        public byte Response;
        public byte Command;
        public DacStatusDto DacStatus;
    }


}
