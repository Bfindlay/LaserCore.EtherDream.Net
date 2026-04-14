using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using LaserCore.EtherDream.Net.Dto;

namespace LaserCore.EtherDream.Net.Discovery
{
    public class DeviceDiscovery : IDisposable
    {
        private const int BroadcastPort = 7654;
        private readonly UdpClient _discoveryClient;
        private bool _disposed;

        public static ConcurrentDictionary<string, DacDto> DiscoveredDevices = new();

        public DeviceDiscovery()
        {
            _discoveryClient = new UdpClient(BroadcastPort);
            _discoveryClient.Client.ReceiveTimeout = 1000;
        }

        private static DacBroadcastDto Deserialize(byte[] param)
        {
            Span<byte> bytes = param;
            var dto = MemoryMarshal.Cast<byte, DacBroadcastDto>(bytes)[0];
            return dto;
        }

        public DacDto FindFirstDevice(int timeoutMs = 10000)
        {
            var remoteEp = new IPEndPoint(IPAddress.Any, BroadcastPort);
            var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);

            while (DateTime.UtcNow < deadline)
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
                catch (SocketException)
                {
                    // Receive timeout — loop and retry until deadline
                }
            }

            throw new TimeoutException($"No EtherDream device found within {timeoutMs}ms");
        }

        public IEnumerable<DacDto> GetAvailableDevices()
        {
            var remoteEp = new IPEndPoint(IPAddress.Any, BroadcastPort);

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
                catch (SocketException)
                {
                    break;
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

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _discoveryClient?.Dispose();
        }
    }
}
