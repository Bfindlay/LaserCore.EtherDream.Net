using System;
using EtherDream.Net.Dto;
using EtherDream.Net.Enums;
using System.Runtime.InteropServices;
namespace EtherDream.Net.Device
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
