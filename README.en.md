# WheelSimu

WheelSimu is a racing simulator peripheral solution based on Android devices. By converting Android device sensor data (steering wheel angle, pedal inputs, gear shifts, etc.) into vJoy virtual joystick signals, it achieves perfect compatibility with PC racing games.

## Features

- **Steering Wheel Simulation**: Supports 540°/720° steering wheel angle detection, mapped in real-time to the vJoy X-axis
- **Three-Pedal System**: Independently simulates throttle, brake, and clutch pedals with adjustable precision
- **Handbrake Function**: Supports handbrake switch signal input
- **Gear Control**: Supports upshift/downshift operations
- **Accelerometer Support**: Dual-sensor data acquisition for more precise dynamic feedback
- **Network Connection**: Supports TCP/IP direct connection and LAN auto-discovery
- **Auto-Reconnect**: Automatically attempts to reconnect after network disconnection to ensure continuity of gameplay
- **vJoy Integration**: Compatible with various racing games via the vJoy virtual joystick driver

## System Requirements

### Android Side
- Android 4.3 (API 18) and above
- Device must support accelerometer sensors
- Landscape mode recommended

### PC Side
- Windows 7/8/10/11 (64-bit)
- [vJoy](https://sourceforge.net/projects/vjoy/) virtual joystick driver installed
- .NET 6.0 Runtime installed

## Installation Instructions

### 1. Install vJoy Driver

1. Download and install the [vJoy SDK](https://sourceforge.net/projects/vjoy/)
2. Configure the vJoy device: Ensure the following axes and buttons are enabled
   - X Axis (Steering Wheel)
   - Buttons 1-8 (Handbrake, Gear Shifting, etc.)
   - Optional: Z Axis, RX Axis, etc.

### 2. Install PC Server

Located at `Release/WheelSimuServer.exe`, double-click to run.

### 3. Install Android App

Install `Release/WheelSimu.apk` onto the Android device.

## Usage

### Start Server

1. Run `WheelSimuServer.exe`
2. The server will automatically broadcast its IP address
3. Confirm the vJoy status is "OWN" (Control Acquired)

### Connect Android App

**Method One: Auto-Discovery**
1. Ensure the phone and PC are on the same LAN
2. Click the "Network Mode" button on the Android side
3. Select the discovered server from the list

**Method Two: Manual Connection**
1. Enter the PC's IP address on the Android side
2. Click the "Connect" button

### Operation Guide

| Function | Operation Method |
|------|----------|
| Steering | Tilt device or rotate physical steering wheel (if available) |
| Throttle/Brake/Clutch | Hold corresponding pedal area and slide to adjust |
| Handbrake | Toggle handbrake switch |
| Upshift/Downshift | Click upshift/downshift buttons |
| Reset Center | Click "Reset Angle" button |
| Enable Steering | Enable "Steering Enable" switch |

## Project Structure

```
WheelSimu/
├── WheelSimu/                    # Android Application Project
│   ├── MainActivity.cs           # Main interface and core logic
│   ├── SteeringWheelView.cs      # Custom steering wheel view
│   ├── PedalGaugeView.cs         # Custom pedal gauge view
│   ├── CommonCode.cs             # Common code
│   └── Resources/                # Resource files (layouts, icons, styles, etc.)
│
├── WheelSimuServer/              # PC Server Project
│   ├── MainForm.cs               # Main form and business logic
│   ├── VJoyDiag.cs               # vJoy diagnostic tool
│   └── Program.cs                # Program entry point
│
└── Release/                      # Release Files
    ├── WheelSimu.apk             # Android installer package
    └── WheelSimuServer.exe       # Windows server
```

## Technical Architecture

### Communication Protocol

The server listens on port 8866 using the TCP protocol. Data format is a single-line JSON string:

```json
{
  "type": "control",
  "angle": 45.5,
  "throttle": 75,
  "brake": 0,
  "clutch": 0,
  "handbrake": 0,
  "gearUp": 0,
  "gearDown": 0
}
```

Discovery protocol uses UDP broadcast on port 12001, magic number is `"WheelSimu"`.

### Core Class Description

- **MainActivity**: Sensor management, network communication, data sending
- **SteeringWheelView**: Steering wheel UI rendering, supports smooth angle transitions
- **PedalGaugeView**: Pedal gauge UI, supports touch interaction
- **MainForm (Server)**: vJoy control, TCP server, client management

## Build Instructions

### Android Project

```bash
# Xamarin.Android development environment required
# Open WheelSimu.sln using Visual Studio
# Select Release configuration, build to generate APK
```

### Server Project

```bash
# .NET 6.0 SDK required
cd WheelSimuServer
dotnet build -c Release
```

## FAQ

**Q: vJoy status displays MISS**
A: Check if the vJoy driver is installed correctly, try re-configuring the vJoy device

**Q: Android side cannot discover server**
A: Confirm firewall allows UDP 12001 port communication, check if on the same network

**Q: Steering wheel response is sluggish**
A: Try lowering the sensor data sending interval on the Android side

**Q: Pedal values jitter**
A: Enable the smoothing filter function in PedalGaugeView

## License

This project is open source under the MIT License.

## Acknowledgements

- [vJoy](https://sourceforge.net/projects/vjoy/) - Virtual Joystick Driver
- [Xamarin.Android](https://dotnet.microsoft.com/en-us/apps/xamarin/android) - Android Application Development Framework