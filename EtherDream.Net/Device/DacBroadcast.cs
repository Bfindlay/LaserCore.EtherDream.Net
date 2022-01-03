﻿using System;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using LaserCore.EtherDream.Net.Dto;

namespace LaserCore.EtherDream.Net.Device
{
    public static class DacBroadcast
    {
        public static PhysicalAddress ParseMacAddress(byte[] bytes)
        {
            if (bytes.Length < 6)
            {
                throw new Exception($"Response expected to be 6 bytes but was {bytes.Length}");
            }

            return new PhysicalAddress(bytes);
        }

        public static string ParseDeviceName(byte[] bytes)
        {
            if (bytes.Length < 6)
            {
                throw new Exception($"Response expected to be 6 bytes but was {bytes.Length}");
            }
            return $"Ether Dream {bytes[3]:X2}{bytes[4]:X2}{bytes[5]:X2}";

        }

        public static DacBroadcastDto Parse(byte[] bytes)
        {
            if (bytes.Length < 36)
            {
                throw new Exception($"Response expected to be 36 bytes but was {bytes.Length}");
            }
            Span<byte> span = bytes;
            var broadcast = MemoryMarshal.Cast<byte, DacBroadcastDto>(span)[0];
            return broadcast;
        }

        public static ushort GetBufferCapacity(DacDto dac)
        {
            return dac.Identity.BufferCapacity;
        }

        public static ushort GetHwVersion(DacDto dac)
        {
            return dac.Identity.HwVersion;
        }
        public static ushort GetSwVersion(DacDto dac)
        {
            return dac.Identity.SwVersion;
        }

        public static uint GetMaxPointRate(DacDto dac)
        {
            return dac.Identity.MaxPointRate;
        }
    }
}
