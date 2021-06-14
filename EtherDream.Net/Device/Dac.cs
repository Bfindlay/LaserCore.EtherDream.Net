using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using LaserCore.Etherdream.Net.Dto;
using LaserCore.Etherdream.Net.Enums;

namespace LaserCore.Etherdream.Net.Device
{

    public delegate void StatusUpdate(AckCode ack, ushort bufferFullness);
    public delegate void DeviceDisconnect();
    public delegate void DeviceConnect();

    public class Dac
    {

        // Events

        public event StatusUpdate StatusUpdated;
        public event DeviceDisconnect DeviceDisconnected;
        public event DeviceConnect DeviceConnected;

        // Consts
        private readonly int Communication_Port = 7765;
        private readonly byte COMMAND_BEGIN = 0x62;
        private readonly byte COMMAND_DATA = 0x64;
        private readonly byte COMMAND_PING = 0x3F;
        private readonly byte COMMAND_PREPARE = 0x70;

        private readonly int BUFFER_SIZE = 1799;

        private readonly byte COMMAND_STOP = 0x73;
        private readonly byte COMMAND_E_STOP = 0x00;
        private readonly byte COMMAND_CLEAR_E_STOP = 0x63;

        // Device network Stream
        private TcpClient _socket;
        private string _ip;
        private bool IsConnected = false;



        public Dac(string ip)
        {
            //TODO handle socket not connect excpetion
            _ip = ip;
            ConnectSocket();

            //init heartbeat //TODO Teardown to prevent memory leak
            Timer timer = new Timer(Heartbeat, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        }

        private void ConnectSocket()
        {
            _socket = new TcpClient(_ip, Communication_Port);
            _socket.Client.ReceiveTimeout = 500;
            IsConnected = true;
            DeviceConnected?.Invoke();
        }

        #region Device Control Functions

        public void TryPrepare(DacResponseDto response)
        {
            if (response.DacStatus.PlaybackFlags != 0x0 &&
                response.DacStatus.PlaybackFlags != 0x1)
            {
                Debug.WriteLine("bad playback flags");
            }
        }

        public void Stop()
        {
            byte[] command = { COMMAND_STOP };
            Transmit(command);
        }

        public void EStop()
        {
            byte[] command = { COMMAND_E_STOP };
            Transmit(command);
        }

        public void ClearEStop()
        {
            byte[] command = { COMMAND_CLEAR_E_STOP };
            Transmit(command);
        }

        public DacResponseDto Prepare()
        {
            byte[] command = { COMMAND_PREPARE };

            var response = Transmit(command);
            return response;
        }

        public DacResponseDto Begin(ushort pointRate = 30000)
        {
            BeginCommandDto cmd = new BeginCommandDto()
            {
                Command = COMMAND_BEGIN,
                LowWaterMark = 0, //not implemented
                PointRate = pointRate
            };
            var serialized = Serialize<BeginCommandDto>(cmd);

            var response = Transmit(serialized);

            var ack = DacResponse.ParseAckCode(response.Response);

            return response;
        }


        public void StreamPoints(DacPointDto[] points, ushort pointRate = 30000)
        {
            // Try prepare 
            var response = Prepare();
            var played = 0;
            ReadOnlySpan<DacPointDto> buffer = new ReadOnlySpan<DacPointDto>(points);

            for (; ; )
            {
                var pointCap = (buffer.Length < BUFFER_SIZE) ? buffer.Length - 1 : (BUFFER_SIZE - response.DacStatus.BufferFullness);

                if (pointCap < 0)
                {
                    response = Ping();
                }
                else
                {
                    if ((played + pointCap) >= buffer.Length)
                    {
                        // playback done
                        break;
                    }

                    var playablePoints = buffer.Slice(played, pointCap);
                    DataCommandDto cmd = new DataCommandDto()
                    {
                        Command = COMMAND_DATA,
                        NPoints = Convert.ToUInt16(pointCap),
                        Points = playablePoints.ToArray()
                    };

                    //serialize points

                    var serialized = SerializePointsCommand(cmd);

                    //transmit

                    response = Transmit(serialized);
                    played += pointCap;

                    Begin(pointRate);
                }

            }

        }

        #endregion

        #region Serializaton / Deserialization

        private byte[] SerializePointsCommand(DataCommandDto cmd)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(cmd.Command);
                    writer.Write(cmd.NPoints);

                    foreach (var point in cmd.Points)
                    {
                        writer.Write(point.Control);
                        writer.Write(point.X);
                        writer.Write(point.Y);
                        writer.Write(point.R);
                        writer.Write(point.G);
                        writer.Write(point.B);
                        writer.Write(point.I);
                        writer.Write(point.U1);
                        writer.Write(point.U2);
                    }

                }
                return stream.ToArray();
            }
        }
        private Span<byte> Serialize<T>(T param) where T : struct
        {
            Span<byte> bytes = MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateSpan(ref param, 1));
            return bytes;
        }

        #endregion

        #region Transmission

        private DacResponseDto Transmit(byte[] cmd)
        {
            _socket.Client.Send(cmd);
            var bytes = ReceiveResponse();
            return DacResponse.ParseDacResponse(bytes);
        }

        private DacResponseDto Transmit(Span<byte> cmd)
        {
            _socket.Client.Send(cmd);
            var bytes = ReceiveResponse();
            return DacResponse.ParseDacResponse(bytes);

        }

        private DacResponseDto Ping()
        {
            byte[] command = { COMMAND_PING };
            return Transmit(command);
        }

        private byte[] ReceiveResponse()
        {
            byte[] received = new byte[Marshal.SizeOf(typeof(DacResponseDto))];
            _socket.Client.Receive(received);
            return received;
        }

        #endregion

        #region Fields

        public string IP
        {
            get
            {
                return _ip;
            }
        }

        #endregion

        #region Heartbeat

        private void Heartbeat(object state)
        {

            if (!_socket.Connected && IsConnected)
            {
                DeviceDisconnected?.Invoke();
                IsConnected = false;
            }

            if (!IsConnected)
            {
                // Try reconnect
                try
                {
                    ConnectSocket();
                    DeviceConnected?.Invoke();

                }
                catch (Exception e)
                {
                    IsConnected = false;
                }
            }

            try
            {
                var response = Ping();

                Span<byte> statusSpan = Serialize<DacStatusDto>(response.DacStatus);

                var dacStatus = DacStatus.ParseDacStatus(statusSpan);

                var ack = DacResponse.ParseAckCode(response.Response);     

                StatusUpdated?.Invoke(ack, dacStatus.BufferFullness);
            }
            catch (Exception e)
            {

            }
        }

        #endregion
    }

}