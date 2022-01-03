using System;
using System.Runtime.InteropServices;
using LaserCore.EtherDream.Net.Dto;

namespace LaserCore.EtherDream.Net.Device
{
    public static class DacPoint
    {
        public static DacPointDto XYRgb(short x, short y, ushort r, ushort g, ushort b)
        {
            return new DacPointDto()
            {
                Control = 0,
                X = x,
                Y = y,
                R = r,
                G = g,
                B = b,
                I = 0,
                U1 = 0,
                U2 = 0
            };
        }

        public static DacPointDto XYLuma(short x, short y, ushort luma)

        {
            return new DacPointDto()
            {
                Control = 0,
                X = x,
                Y = y,
                R = luma,
                G = luma,
                B = luma,
                I = 0,
                U1 = 0,
                U2 = 0
            };
        }

        public static DacPointDto XYBlank(short x, short y)
        {
            return XYLuma(x, y, 0);
        }

        public static Span<byte> Serialize(DacPointDto point)
        {
            var bytes = MemoryMarshal.Cast<DacPointDto, byte>(MemoryMarshal.CreateSpan<DacPointDto>(ref point, 1));
            return bytes;
        }
    }
}