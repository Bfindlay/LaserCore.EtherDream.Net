using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using LaserCore.EtherDream.Net.Dto;

namespace LaserCore.EtherDream.Net.Discovery
{
    public class DeviceDiscovery
    {
        private const int BroadcastPort = 7654;
        private readonly UdpClient _discoveryClient;

        public static ConcurrentDictionary<string, DacDto> DiscoveredDevices = new();

        public DeviceDiscovery()
        {
            _discoveryClient = new UdpClient(BroadcastPort);
            _discoveryClient.Client.ReceiveTimeout = 1000;
            DiscoveredDevices = new ConcurrentDictionary<string, DacDto>();

        }

        private static DacBroadcastDto Deserialize(byte[] param)
        {
            Span<byte> bytes = param;
            var dto = MemoryMarshal.Cast<byte, DacBroadcastDto>(bytes)[0];
            return dto;
        }

        public DacDto FindFirstDevice()
        {
            // TODO Handle socket no connection
            var remoteEp = new IPEndPoint(IPAddress.Parse("192.168.1.1"), BroadcastPort);

            while (true)
            {
                try
                {
                    var bytesReceived = _discoveryClient.Receive(ref remoteEp);
                    var identity = Deserialize(bytesReceived);
                    var etherDream = new DacDto
                    {
                        Identity = identity,
                        Ip = remoteEp.Address.ToString()
                    };

                    DiscoveredDevices.TryAdd(etherDream.Ip, etherDream);

                    return etherDream;
                }
                catch
                {
                    // NOOP
                }
            }
        }

        public IEnumerable<DacDto> GetAvailableDevices()
        {
            // TODO Handle socket no connection
            var remoteEp = new IPEndPoint(IPAddress.Parse("192.168.1.1"), BroadcastPort);

            // Try find a maximum of 2 devices
            while (_discoveryClient.Client.Available != 0)
            {
                try
                {
                    var bytesReceived = _discoveryClient.Receive(ref remoteEp);
                    var identity = Deserialize(bytesReceived);
                    var etherDream = new DacDto
                    {
                        Identity = identity,
                        Ip = remoteEp.Address.ToString()
                    };

                    DiscoveredDevices.TryAdd(etherDream.Ip, etherDream);
                }
                catch
                {
                    // NOOP
                }
            }
            return DiscoveredDevices.Values;
        }

        public static string GetDeviceName(DacDto dac)
        {
            unsafe
            {
                var identity = dac.Identity;
                var dacName =
                    $"Ether Dream {identity.MacAddress[3]:X2}{identity.MacAddress[4]:X2}{identity.MacAddress[5]:X2}";
                return dacName;
            }
        }

        public static string GetDeviceIp(DacDto dac)
        {
            return dac.Ip;
        }
    }
}
