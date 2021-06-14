using System;
using System.Runtime.InteropServices;

namespace LaserCore.Etherdream.Net.Dto
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe public struct DataCommandDto
    {
        public byte Command; /* ‘d’ (0x64) */
        public ushort NPoints;
        public DacPointDto[] Points;
    }
}
