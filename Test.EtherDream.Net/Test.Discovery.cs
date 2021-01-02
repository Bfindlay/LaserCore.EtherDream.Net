using NUnit.Framework;
using System;
using EtherDream.Net.Discovery;
using EtherDream.Net.Device;
using EtherDream.Net.Dto;
namespace Test.EtherDream.Net
{
    public class DiscoveryTests
    {
        [SetUp]
        public void Setup()
        {
            
        }

        [Test]

        public void DacTest()
        {
            var discovery = new DeviceDiscovery();
            var dac = discovery.FindFirstDevice();
            var name = discovery.GetDeviceName(dac);
            var ip = discovery.GetDeviceIp(dac);

            Dac etherDream = new Dac(ip);
            var points = MakePoints(1000);
            etherDream.StreamPoints(points);
        }

        public static DacPointDto[] MakePoints(short num)
        {
            DacPointDto[] points = new DacPointDto[num];

            for (short i = 0; i < num / 4; i += 4)
            {
                DacPointDto p1 = new DacPointDto()
                {
                    Control = 0,
                    X = Convert.ToInt16(i * 10),
                    Y = 15000,
                    R = 45000,
                    G = 0,
                    B = 0,
                    I = 1,
                    U1 = 0,
                    U2 = 0,
                };
                DacPointDto p2 = new DacPointDto()
                {
                    Control = 0,
                    X = 15000,
                    Y = Convert.ToInt16(i * -10),
                    R = 0,
                    G = 25000,
                    B = 0,
                    I = 1,
                    U1 = 0,
                    U2 = 0,
                };
                DacPointDto p3 = new DacPointDto()
                {
                    Control = 0,
                    X = -15000,
                    Y = Convert.ToInt16(i * 10),
                    R = 0,
                    G = 0,
                    B = 20000,
                    I = 1,
                    U1 = 0,
                    U2 = 0,
                };
                DacPointDto p4 = new DacPointDto()
                {
                    Control = 0,
                    X = Convert.ToInt16(i * 10),
                    Y = 15000,
                    R = 45000,
                    G = 0,
                    B = 0,
                    I = 1,
                    U1 = 0,
                    U2 = 0,
                };

                points[i] = p1;
                points[i + 1] = p2;
                points[i + 2] = p3;
                points[i + 3] = p4;
            }

            return points;
        }

        [Test]
        public void FindFirstDevice()
        {
            var discovery = new DeviceDiscovery();
            var dac = discovery.FindFirstDevice();
            var name = discovery.GetDeviceName(dac);
            var ip = discovery.GetDeviceIp(dac);
            Assert.AreEqual("ether dream 8763fa", name);
            Assert.AreEqual("192.168.1.111", ip);
        }


       
    }
}