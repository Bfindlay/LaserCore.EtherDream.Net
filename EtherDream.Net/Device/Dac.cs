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
    public delegate void SafetyFault(string reason);

    public class Dac : IDisposable
    {

        // Events
        public event StatusUpdate? StatusUpdated;
        public event DeviceDisconnect? DeviceDisconnected;
        public event DeviceConnect? DeviceConnected;
        public event SafetyFault? SafetyFaultDetected;

        // Consts
        private const int CommunicationPort = 7765;
        private const int ResponseSize = 22;

        private const byte CommandBegin = 0x62;         // 'b'
        private const byte CommandData = 0x64;          // 'd'
        private const byte CommandPing = 0x3F;          // '?'
        private const byte CommandPrepare = 0x70;       // 'p'
        private const byte CommandQueueChange = 0x71;   // 'q'
        private const byte CommandStop = 0x73;          // 's'
        private const byte CommandEStop = 0x00;
        private const byte CommandClearEStop = 0x63;    // 'c'

        // Default buffer capacity for EtherDream 1/2. Overridden by value
        // from discovery broadcast when available.
        private const int DefaultBufferCapacity = 1799;

        // Fields
        private DacResponseDto _lastResponse;
        private TcpClient? _socket;
        private volatile bool _isConnected;
        private Timer? _timer;
        private readonly object _socketLock = new();
        private bool _disposed;
        private uint _currentPointRate;

        /// <summary>
        /// Connect to an EtherDream DAC at the given IP.
        /// Pass the <see cref="DacBroadcastDto.BufferCapacity"/> from discovery
        /// to use the device's actual buffer size (varies by hardware version).
        /// </summary>
        public Dac(string ip, ushort bufferCapacity = DefaultBufferCapacity, uint maxPointRate = 0)
        {
            Ip = ip;
            BufferCapacity = bufferCapacity;
            MaxPointRate = maxPointRate;
            _lastResponse = default;
            ConnectSocket();
            _timer = new Timer(Heartbeat, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Connect using a discovered device — automatically uses the reported
        /// buffer capacity and max point rate for the hardware version.
        /// </summary>
        public Dac(DacDto device)
            : this(device.Ip, device.Identity.BufferCapacity, device.Identity.MaxPointRate)
        {
        }

        private void ConnectSocket()
        {
            _socket = new TcpClient();
            _socket.Client.NoDelay = true;
            _socket.Client.ReceiveTimeout = 5000;
            _socket.Client.SendTimeout = 5000;
            _socket.Connect(Ip, CommunicationPort);

            // The DAC sends an initial response immediately on connect
            var initialBytes = ReceiveResponseRaw();
            var initialResponse = DacResponse.ParseDacResponse(initialBytes);
            _lastResponse = initialResponse;

            _isConnected = true;
            DeviceConnected?.Invoke();
        }

        #region Device Control Functions

        public bool TryPrepare()
        {
            if (_lastResponse.DacStatus.PlaybackFlags != 0x0 &&
              _lastResponse.DacStatus.PlaybackFlags != 0x1)
            {
                Debug.WriteLine("bad playback flags");
            }

            if (_lastResponse.DacStatus.LightEngineState is not LightEngineState.Ready)
            {
                return false;
            }

            // Already prepared or playing — no need to re-prepare
            if (_lastResponse.DacStatus.PlayBackState is PlayBackEngineState.Prepared
                or PlayBackEngineState.Playing)
            {
                return true;
            }

            if (_lastResponse.DacStatus.PlayBackState is not PlayBackEngineState.Idle)
            {
                return false;
            }

            Prepare();
            return true;
        }

        public DacResponseDto Stop()
        {
            byte[] command = { CommandStop };
            return Transmit(command);
        }

        public DacResponseDto EStop()
        {
            byte[] command = { CommandEStop };
            return Transmit(command);
        }

        public DacResponseDto ClearEStop()
        {
            byte[] command = { CommandClearEStop };
            return Transmit(command);
        }

        public DacResponseDto Prepare()
        {
            byte[] command = { CommandPrepare };
            return Transmit(command);
        }

        public DacResponseDto Begin(uint pointRate = 30000)
        {
            var cmd = new BeginCommandDto
            {
                Command = CommandBegin,
                LowWaterMark = 0,
                PointRate = pointRate
            };
            var serialized = Serialize(cmd);
            return Transmit(serialized);
        }

        public DacResponseDto QueueRateChange(uint pointRate)
        {
            // Clamp to hardware maximum
            if (MaxPointRate > 0 && pointRate > MaxPointRate)
                pointRate = MaxPointRate;

            var cmd = new QueueChangeCommandDto
            {
                Command = CommandQueueChange,
                PointRate = pointRate
            };
            var serialized = Serialize(cmd);
            return Transmit(serialized);
        }

        /// <summary>
        /// Streams an array of points to the DAC. Handles prepare, begin, and
        /// buffer management automatically. Thread-safe — blocks the heartbeat
        /// while active.
        /// </summary>
        public void StreamPoints(DacPointDto[] points, uint pointRate = 30000)
        {
            // Clamp point rate to hardware maximum if known
            if (MaxPointRate > 0 && pointRate > MaxPointRate)
                pointRate = MaxPointRate;

            lock (_socketLock)
            {
                // Ensure we're prepared
                if (_lastResponse.DacStatus.LightEngineState != LightEngineState.Ready)
                {
                    throw new InvalidOperationException(
                        $"Light engine not ready: {_lastResponse.DacStatus.LightEngineState}");
                }

                // If idle, stop (reset) then prepare
                if (_lastResponse.DacStatus.PlayBackState == PlayBackEngineState.Idle)
                {
                    TransmitRaw(new byte[] { CommandPrepare });
                }

                // If the point rate changed while already playing, send queue_change
                if (_lastResponse.DacStatus.PlayBackState == PlayBackEngineState.Playing &&
                    _currentPointRate != 0 && _currentPointRate != pointRate)
                {
                    var qc = new QueueChangeCommandDto
                    {
                        Command = CommandQueueChange,
                        PointRate = pointRate
                    };
                    TransmitRaw(Serialize(qc));
                    _currentPointRate = pointRate;
                }

                var buffer = new ReadOnlySpan<DacPointDto>(points);
                var played = 0;
                var invalidRetries = 0;
                var begun = _lastResponse.DacStatus.PlayBackState == PlayBackEngineState.Playing;

                while (played < buffer.Length)
                {
                    var bufferSpace = BufferCapacity - _lastResponse.DacStatus.BufferFullness;

                    if (bufferSpace <= 0)
                    {
                        // Buffer full — ping to get updated status
                        TransmitRaw(new byte[] { CommandPing });
                        continue;
                    }

                    var remaining = buffer.Length - played;
                    var chunkSize = (int)Math.Min(bufferSpace, remaining);

                    var playablePoints = buffer.Slice(played, chunkSize);

                    // Serialize and send data command
                    var serialized = SerializeDataCommand(playablePoints);
                    var response = TransmitRaw(serialized);

                    var ack = DacResponse.ParseAckCode(response.Response);
                    if (ack == AckCode.NackBufferFull)
                    {
                        // Buffer was fuller than reported — retry after ping
                        TransmitRaw(new byte[] { CommandPing });
                        continue;
                    }
                    if (ack == AckCode.NackInvalid)
                    {
                        // DAC likely went to Idle (buffer underrun). Re-prepare and retry this chunk.
                        if (++invalidRetries > 3)
                            throw new InvalidOperationException($"DAC NACK: {ack}");
                        TransmitRaw(new byte[] { CommandPrepare });
                        begun = false;
                        continue;
                    }
                    if (ack != AckCode.Ack)
                    {
                        throw new InvalidOperationException($"DAC NACK: {ack}");
                    }

                    invalidRetries = 0;
                    played += chunkSize;

                    // Begin playback once after first data chunk is queued
                    if (!begun)
                    {
                        var beginCmd = new BeginCommandDto
                        {
                            Command = CommandBegin,
                            LowWaterMark = 0,
                            PointRate = pointRate
                        };
                        TransmitRaw(Serialize(beginCmd));
                        _currentPointRate = pointRate;
                        begun = true;
                    }
                }
            }
        }

        #endregion

        #region Serialization

        private byte[] SerializeDataCommand(ReadOnlySpan<DacPointDto> points)
        {
            using var stream = new MemoryStream();
            using var writer = new BinaryWriter(stream);

            // Data command header
            writer.Write(CommandData);
            writer.Write((ushort)points.Length);

            // Point data (18 bytes each)
            foreach (var point in points)
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

            return stream.ToArray();
        }

        private byte[] Serialize<T>(T param) where T : struct
        {
            var bytes = new byte[Marshal.SizeOf<T>()];
            MemoryMarshal.Write(bytes, in param);
            return bytes;
        }

        #endregion

        #region Transmission

        /// <summary>
        /// Send command and read response. Caller must hold _socketLock.
        /// </summary>
        private DacResponseDto TransmitRaw(byte[] cmd)
        {
            _socket.Client.Send(cmd);
            var bytes = ReceiveResponseRaw();
            var response = DacResponse.ParseDacResponse(bytes);
            _lastResponse = response;

            var ack = DacResponse.ParseAckCode(response.Response);
            StatusUpdated?.Invoke(ack, response.DacStatus.PlayBackState,
                response.DacStatus.LightEngineState, response.DacStatus.BufferFullness);

            // Safety flag checks
            CheckSafetyFlags(response.DacStatus);

            return response;
        }

        /// <summary>
        /// Check LightEngineFlags and PlaybackFlags for safety conditions.
        /// Fires SafetyFaultDetected event on critical faults.
        /// </summary>
        private void CheckSafetyFlags(DacStatusDto status)
        {
            var leFlags = status.LightEngineFlags;
            var pbFlags = status.PlaybackFlags;

            // Bit 3: overtemperature event occurred
            if ((leFlags & 0x08) != 0)
                SafetyFaultDetected?.Invoke("Overtemperature condition detected");

            // Bit 4: overtemperature currently active
            if ((leFlags & 0x10) != 0)
                SafetyFaultDetected?.Invoke("Overtemperature condition active — laser output disabled");

            // Bit 5: Ethernet link loss
            if ((leFlags & 0x20) != 0)
                SafetyFaultDetected?.Invoke("Ethernet link loss detected");

            // Bit 0: E-Stop via packet or invalid command
            if ((leFlags & 0x01) != 0 && status.LightEngineState == LightEngineState.EmergencyStop)
                SafetyFaultDetected?.Invoke("Emergency stop triggered by command");

            // Bit 1: E-Stop via hardware input
            if ((leFlags & 0x02) != 0)
                SafetyFaultDetected?.Invoke("Emergency stop triggered by hardware input");

            // Playback bit 1: buffer underflow
            if ((pbFlags & 0x02) != 0)
                SafetyFaultDetected?.Invoke("Buffer underflow — stream ended unexpectedly");
        }

        /// <summary>Thread-safe transmit — acquires _socketLock.</summary>
        private DacResponseDto Transmit(byte[] cmd)
        {
            lock (_socketLock)
            {
                return TransmitRaw(cmd);
            }
        }

        public DacResponseDto Ping()
        {
            byte[] command = { CommandPing };
            return Transmit(command);
        }

        /// <summary>
        /// Reads exactly 22 bytes from the socket. Does NOT acquire lock.
        /// </summary>
        private byte[] ReceiveResponseRaw()
        {
            byte[] received = new byte[ResponseSize];
            int totalRead = 0;
            while (totalRead < ResponseSize)
            {
                int read = _socket.Client.Receive(received, totalRead,
                    ResponseSize - totalRead, SocketFlags.None);
                if (read == 0)
                    throw new IOException("DAC connection closed");
                totalRead += read;
            }
            return received;
        }

        #endregion

        #region Properties

        public string Ip { get; }

        /// <summary>
        /// Hardware buffer capacity in points. Reported by the DAC during
        /// discovery broadcast — larger on newer hardware revisions.
        /// EtherDream 1/2: 1799, EtherDream 4: typically larger.
        /// </summary>
        public ushort BufferCapacity { get; }

        /// <summary>
        /// Maximum point rate supported by the hardware (points/sec).
        /// 0 if not provided at construction.
        /// </summary>
        public uint MaxPointRate { get; }

        #endregion

        #region Heartbeat

        private void Heartbeat(object? state)
        {
            // Use TryEnter to skip heartbeat if streaming or another operation holds the lock
            if (!Monitor.TryEnter(_socketLock))
                return;

            try
            {
                if (!_socket.Connected && _isConnected)
                {
                    _isConnected = false;
                    DeviceDisconnected?.Invoke();
                }

                if (!_isConnected)
                {
                    try
                    {
                        ConnectSocket();
                    }
                    catch
                    {
                        _isConnected = false;
                    }
                    return;
                }

                TransmitRaw(new byte[] { CommandPing });
            }
            catch
            {
                _isConnected = false;
                DeviceDisconnected?.Invoke();
            }
            finally
            {
                Monitor.Exit(_socketLock);
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _timer?.Dispose();
            _timer = null;

            try
            {
                if (_isConnected && _socket?.Connected == true)
                {
                    lock (_socketLock)
                    {
                        TransmitRaw(new byte[] { CommandStop });
                    }
                }
            }
            catch { /* best effort cleanup */ }

            _socket?.Dispose();
            _socket = null;
            _isConnected = false;
        }

        #endregion
    }
}
