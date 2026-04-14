using System;
using System.Runtime.InteropServices;

namespace LaserCore.EtherDream.Net.Dto
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DataCommandDto
    {
        public byte Command; /* ‘d’ (0x64) */
        public ushort NPoints;
        public DacPointDto[] Points;
    }
}
