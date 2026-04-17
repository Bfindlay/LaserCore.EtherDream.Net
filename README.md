# LaserCore.EtherDream.Net

A .NET library for communicating with [Ether Dream](https://ether-dream.com/) laser DACs. Handles device discovery, connection management, and real-time point streaming with automatic buffer management and safety monitoring.

## Features

- UDP broadcast device discovery
- TCP streaming with automatic buffer management
- Thread-safe point streaming with NACK recovery
- Heartbeat-based connection monitoring and auto-reconnect
- Safety fault detection (E-stop, overtemperature, link loss)
- Point rate changes during playback
- No external dependencies

## Installation

Add a reference to the project:

```bash
dotnet add reference path/to/LaserCore.Etherdream.Net.csproj
```

## Quick Start

### Discover and stream to a device

```csharp
using LaserCore.Etherdream.Net.Device;
using LaserCore.Etherdream.Net.Discovery;
using LaserCore.Etherdream.Net.Dto;

// Discover devices on the network
using var discovery = new DeviceDiscovery();
var device = discovery.FindFirstDevice(timeoutMs: 5000);

Console.WriteLine($"Found: {DeviceDiscovery.GetDeviceName(device)}");
Console.WriteLine($"IP: {DeviceDiscovery.GetDeviceIp(device)}");

// Connect to the device
using var dac = new Dac(device);

// Build a simple square pattern
var points = new DacPointDto[]
{
    DacPoint.XYRgb(-10000, -10000, 65535, 0, 0),     // Bottom-left, red
    DacPoint.XYRgb( 10000, -10000, 0, 65535, 0),     // Bottom-right, green
    DacPoint.XYRgb( 10000,  10000, 0, 0, 65535),     // Top-right, blue
    DacPoint.XYRgb(-10000,  10000, 65535, 65535, 0),  // Top-left, yellow
    DacPoint.XYRgb(-10000, -10000, 65535, 0, 0),     // Close the shape
};

// Stream at 30,000 points per second
dac.StreamPoints(points, pointRate: 30000);
```

### Connect by IP address

```csharp
// Connect directly if you know the device IP
using var dac = new Dac("192.168.1.100");

var points = BuildMyPattern();
dac.StreamPoints(points, pointRate: 30000);
```

### Continuous streaming loop

```csharp
using var dac = new Dac(device);

// Subscribe to events
dac.DeviceConnected += () => Console.WriteLine("Connected");
dac.DeviceDisconnected += () => Console.WriteLine("Disconnected");
dac.SafetyFaultDetected += (reason) => Console.WriteLine($"Safety fault: {reason}");
dac.StatusUpdated += (ack, playback, light, buffer) =>
    Console.WriteLine($"Buffer: {buffer} points");

// Streaming loop
while (true)
{
    var points = GenerateNextFrame();
    dac.StreamPoints(points, pointRate: 30000);
}
```

### Discover all devices

```csharp
using var discovery = new DeviceDiscovery();

// Wait for devices to announce themselves
Thread.Sleep(3000);

foreach (var device in discovery.GetAvailableDevices())
{
    var name = DeviceDiscovery.GetDeviceName(device);
    var ip = DeviceDiscovery.GetDeviceIp(device);
    var bufferSize = DacBroadcast.GetBufferCapacity(device);
    var maxRate = DacBroadcast.GetMaxPointRate(device);

    Console.WriteLine($"{name} at {ip} (buffer: {bufferSize}, max rate: {maxRate})");
}
```

### Creating points

```csharp
// RGB color point
var red = DacPoint.XYRgb(0, 0, 65535, 0, 0);

// Grayscale point (same value for R, G, B)
var white = DacPoint.XYLuma(0, 0, 65535);

// Blank point (laser off, used for repositioning)
var blank = DacPoint.XYBlank(5000, 5000);
```

### Device control

```csharp
using var dac = new Dac(device);

// Low-level control (StreamPoints handles this automatically)
dac.Prepare();
dac.Begin(pointRate: 30000);

// Change point rate during playback
dac.QueueRateChange(45000);

// Stop playback
dac.Stop();

// Emergency stop
dac.EStop();
dac.ClearEStop();

// Check connection
dac.Ping();
```

## API Reference

### `DeviceDiscovery`

| Member | Description |
|--------|-------------|
| `FindFirstDevice(int timeoutMs = 10000)` | Block until a device is found or timeout |
| `GetAvailableDevices()` | Get all currently discovered devices |
| `static GetDeviceName(DacDto dac)` | Format device name from MAC (e.g., "Ether Dream 8763FA") |
| `static GetDeviceIp(DacDto dac)` | Get device IP address |
| `DiscoveredDevices` | Static cache of all discovered devices |

### `Dac`

| Member | Description |
|--------|-------------|
| `Dac(string ip, ushort bufferCapacity, uint maxPointRate)` | Connect by IP address |
| `Dac(DacDto device)` | Connect from discovered device |
| `StreamPoints(DacPointDto[] points, uint pointRate)` | Stream points with automatic buffer management |
| `TryPrepare()` | Prepare device, waiting for light engine ready |
| `Prepare()` | Send prepare command |
| `Begin(uint pointRate)` | Begin playback |
| `Stop()` | Stop playback |
| `EStop()` | Emergency stop |
| `ClearEStop()` | Clear emergency stop |
| `QueueRateChange(uint pointRate)` | Change point rate during playback |
| `Ping()` | Heartbeat / status query |
| `Ip` | Device IP address |
| `BufferCapacity` | Hardware buffer size (points) |
| `MaxPointRate` | Maximum supported point rate |

### Events

| Event | Description |
|-------|-------------|
| `StatusUpdated` | Fires on every response with ACK code, engine states, buffer fullness |
| `DeviceConnected` | Fires when connection is established |
| `DeviceDisconnected` | Fires when connection is lost |
| `SafetyFaultDetected` | Fires on safety conditions (overtemp, E-stop, link loss) |

### `DacPoint` (helper)

| Method | Description |
|--------|-------------|
| `XYRgb(short x, short y, ushort r, ushort g, ushort b)` | Create a colored point |
| `XYLuma(short x, short y, ushort luma)` | Create a grayscale point |
| `XYBlank(short x, short y)` | Create a blank point (laser off) |

### Data Types

#### `DacPointDto` (struct, 18 bytes)

| Field | Type | Range | Description |
|-------|------|-------|-------------|
| `Control` | `ushort` | - | Control flags |
| `X` | `short` | -32768 to 32767 | X coordinate |
| `Y` | `short` | -32768 to 32767 | Y coordinate |
| `R` | `ushort` | 0-65535 | Red channel |
| `G` | `ushort` | 0-65535 | Green channel |
| `B` | `ushort` | 0-65535 | Blue channel |
| `I` | `ushort` | 0-65535 | Intensity |
| `U1` | `ushort` | - | User data 1 |
| `U2` | `ushort` | - | User data 2 |

### Enums

| Enum | Values |
|------|--------|
| `PlayBackEngineState` | `Idle`, `Prepared`, `Playing` |
| `LightEngineState` | `Ready`, `Warmup`, `Cooldown`, `EmergencyStop` |
| `AckCode` | `Ack`, `NackBufferFull`, `NackInvalid`, `NackStop` |

## Protocol Details

- **Discovery:** UDP broadcast on port 7654
- **Streaming:** TCP on port 7765
- **Default buffer:** 1799 points (hardware-reported)
- **Typical point rate:** 30,000 pps

## Requirements

- .NET 9.0+
- Ether Dream DAC on the same network

## License

MIT
