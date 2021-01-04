using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using EtherDream.Net.Dto;
using System.Collections.Concurrent;
namespace EtherDream.Net.Discovery
{

    public class DeviceDiscovery
    {
        private readonly int Broadcast_Port = 7654;
        private UdpClient _discoveryClient;

        public static ConcurrentDictionary<string, DacDto> DiscoveredDevices = new ConcurrentDictionary<string, DacDto>();

        //TODO Handle find more than one device

        public DeviceDiscovery()
        {
            _discoveryClient = new UdpClient(Broadcast_Port);
            _discoveryClient.Client.ReceiveTimeout = 1000;
            DiscoveredDevices = new ConcurrentDictionary<string, DacDto>();

        }

        private DacBroadcastDto Deserialize(byte[] param)
        {
            Span<byte> bytes = param;
            DacBroadcastDto dto = MemoryMarshal.Cast<byte, DacBroadcastDto>(bytes)[0];
            return dto;
        }
        

        public  DacDto FindFirstDevice()
        {
            // TODO Handle socket no connection

            var remoteEP = new IPEndPoint(IPAddress.Any, Broadcast_Port);
            byte[] bytesReceived = _discoveryClient.Receive(ref remoteEP);

            var identity = Deserialize(bytesReceived);
            DacDto etherDream = new DacDto();
            etherDream.Identity = identity;
            etherDream.Ip = remoteEP.Address.ToString();

            DiscoveredDevices.TryAdd(etherDream.Ip, etherDream);

            return etherDream;

        }

        public static string GetDeviceName(DacDto dac)
        {
            unsafe
            {
                var identity = dac.Identity;
                string dacName = String.Format("Ether Dream {0:X2}{1:X2}{2:X2}", identity.MacAddress[3], identity.MacAddress[4], identity.MacAddress[5]);
                return dacName;
            }
        }

        public static string GetDeviceIp(DacDto dac)
        {
            unsafe
            {
                return dac.Ip;
            }
        }
    }

}
