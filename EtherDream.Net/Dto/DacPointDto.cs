using System;
using System.Runtime.InteropServices;

namespace LaserCore.EtherDream.Net.Dto
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DacPointDto
    {
        public ushort Control;
        public short X;
        public short Y;
        public ushort R;
        public ushort G;
        public ushort B;
        public ushort I;
        public ushort U1;
        public ushort U2;
    }
}
