using System;
using System.Runtime.InteropServices;
using LaserCore.Etherdream.Net.Dto;
using LaserCore.Etherdream.Net.Enums;

namespace LaserCore.Etherdream.Net.Device
{
    public static class BeginCommand
    {
        public static Span<byte> serialize(uint rate)
        {
            var cmd = new BeginCommandDto()
            {
                Command = (byte)CommandCodeType.Begin,
                LowWaterMark = 0, // not used
                PointRate = rate
            };
            Span<byte> bytes = MemoryMarshal.Cast<BeginCommandDto, byte>(MemoryMarshal.CreateSpan<BeginCommandDto>(ref cmd, 1));
            return bytes;
        }
    }
}
