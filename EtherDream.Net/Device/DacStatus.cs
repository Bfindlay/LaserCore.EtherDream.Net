using System;
using System.Runtime.InteropServices;
using LaserCore.EtherDream.Net.Dto;

namespace LaserCore.EtherDream.Net.Device
{
    public static class DacStatus
    {
        public static DacStatusDto ParseDacStatus(byte[] bytes)
        {
            if (bytes.Length != 20)
            {
                throw new Exception($"Response expected to be 20 bytes but was {bytes.Length}");
            }
            Span<byte> span = bytes;
            var status = MemoryMarshal.Cast<byte, DacStatusDto>(span)[0];
            return status;
        }

        public static DacStatusDto ParseDacStatus(Span<byte> span)
        {
            if (span.Length != 20)
            {
                throw new Exception($"Response expected to be 20 bytes but was {span.Length}");
            }

            var status = MemoryMarshal.Cast<byte, DacStatusDto>(span)[0];
            return status;
        }
    }
}