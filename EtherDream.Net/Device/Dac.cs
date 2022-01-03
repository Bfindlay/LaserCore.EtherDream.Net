using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using LaserCore.EtherDream.Net.Dto;
using LaserCore.EtherDream.Net.Enums;

namespace LaserCore.EtherDream.Net.Device
{

    public delegate void StatusUpdate(AckCode ack, PlayBackEngineState playBackEngineState, LightEngineState lightEngineState, ushort bufferFullness);
    public delegate void DeviceDisconnect();
    public delegate void DeviceConnect();

    public class Dac
    {

        // Events

        public event StatusUpdate StatusUpdated;
        public event DeviceDisconnect DeviceDisconnected;
        public event DeviceConnect DeviceConnected;

        // Consts
        private const int CommunicationPort = 7765;
        private const byte CommandBegin = 0x62;
        private const byte CommandData = 0x64;
        private const byte CommandPing = 0x3F;
        private const byte CommandPrepare = 0x70;

        // TODO make dynamic based on the EtherDream Version
        private const int BufferSize = 1799;

        private const byte CommandStop = 0x73;
        private const byte CommandEStop = 0x00;
        private const byte CommandClearEStop = 0x63;

        // Fields
        private DacResponseDto _lastResponse;

        // Device network Stream
        private TcpClient _socket;
        private bool _isConnected;

        // Hearbeat
        private Timer _timer;

        public Dac(string ip)
        {
            _lastResponse = default;
            //TODO handle socket not connect exception
            Ip = ip;
            ConnectSocket();

            //init heartbeat //TODO Teardown to prevent memory leak
            _timer = new Timer(Heartbeat, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        }

        private void ConnectSocket()
        {
            _socket = new TcpClient(Ip, CommunicationPort);
            _socket.Client.ReceiveTimeout = 500;
            _isConnected = true;
            DeviceConnected?.Invoke();
        }

        #region Device Control Functions

        public bool TryPrepare()
        {
            if (_lastResponse.DacStatus.PlaybackFlags != 0x0 &&
              _lastResponse.DacStatus.PlaybackFlags != 0x1)
            {
                // TODO Handle bad playback flags
                Debug.WriteLine("bad playback flags");
            }

            if (_lastResponse.DacStatus.LightEngineState is not LightEngineState.Ready ||
                _lastResponse.DacStatus.PlayBackState is not PlayBackEngineState.Idle)
            {
                return false;
            }

            Prepare();
            return true;
        }

        public void Stop()
        {
            byte[] command = { CommandStop };
            Transmit(command);
        }

        public void EStop()
        {
            byte[] command = { CommandEStop };
            Transmit(command);
        }

        public void ClearEStop()
        {
            byte[] command = { CommandClearEStop };
            Transmit(command);
        }

        public DacResponseDto Prepare()
        {
            byte[] command = { CommandPrepare };

            var response = Transmit(command);
            return response;
        }

        public DacResponseDto Begin(ushort pointRate = 30000)
        {
            var cmd = new BeginCommandDto()
            {
                Command = CommandBegin,
                LowWaterMark = 0, //not implemented
                PointRate = pointRate
            };
            var serialized = Serialize(cmd);

            var response = Transmit(serialized);
            return response;
        }


        public void StreamPoints(DacPointDto[] points, ushort pointRate = 30000)
        {
            try
            {
                // Try prepare 
                var canPlay = TryPrepare();
                if (!canPlay)
                {
                    throw new Exception("Not Ready To play");
                }
                var response = _lastResponse;
                var played = 0;
                var buffer = new ReadOnlySpan<DacPointDto>(points);
                while (true)
                {
                    var pointCap = (buffer.Length < BufferSize) ? buffer.Length - 1 : (BufferSize - response.DacStatus.BufferFullness);

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
                        var cmd = new DataCommandDto()
                        {
                            Command = CommandData,
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
            catch
            {
                // NOOP
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
            var response = DacResponse.ParseDacResponse(bytes);
            _lastResponse = response;
            var ack = DacResponse.ParseAckCode(response.Response);

            StatusUpdated?.Invoke(ack, response.DacStatus.PlayBackState, response.DacStatus.LightEngineState, response.DacStatus.BufferFullness);
            return response;
        }

        private DacResponseDto Transmit(Span<byte> cmd)
        {
            _socket.Client.Send(cmd);
            var bytes = ReceiveResponse();
            var response = DacResponse.ParseDacResponse(bytes);
            _lastResponse = response;

            var ack = DacResponse.ParseAckCode(response.Response);
            StatusUpdated?.Invoke(ack, response.DacStatus.PlayBackState, response.DacStatus.LightEngineState, response.DacStatus.BufferFullness);
            return response;
        }

        public DacResponseDto Ping()
        {
            byte[] command = { CommandPing };
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

        public string Ip { get; }

        #endregion

        #region Heartbeat

        private void Heartbeat(object state)
        {
            if (!_socket.Connected && _isConnected)
            {
                DeviceDisconnected?.Invoke();
                _isConnected = false;
            }

            if (!_isConnected)
            {
                // Try reconnect
                try
                {
                    ConnectSocket();
                    DeviceConnected?.Invoke();

                }
                catch
                {
                    _isConnected = false;
                }
            }

            try
            {
                var response = Ping();

                var statusSpan = Serialize(response.DacStatus);

                var dacStatus = DacStatus.ParseDacStatus(statusSpan);

                var ack = DacResponse.ParseAckCode(response.Response);

                if (dacStatus.LightEngineFlags != 0)
                {
                    StatusUpdated?.Invoke(ack, dacStatus.PlayBackState, dacStatus.LightEngineState, dacStatus.BufferFullness);
                }

                StatusUpdated?.Invoke(ack, dacStatus.PlayBackState, dacStatus.LightEngineState, dacStatus.BufferFullness);
            }
            catch
            {
                // NOOP
            }
        }
        #endregion
    }
}