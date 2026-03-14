# Quick Start Guide - RedRat3 Controller Web Server

## Running the Application

### Step 1: Start the Web Server
```bash
cd RedRat3ControllerWebServer
dotnet run
```

The server will start and display:
```
Building...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
```

### Step 2: Access the Web Interface
Open your browser and navigate to:
- Local access: `http://localhost:5000`
- From another computer on your network: `http://YOUR_IP_ADDRESS:5000`

## Finding Your IP Address
Run this command in Command Prompt:
```bash
ipconfig
```
Look for "IPv4 Address" (e.g., 192.168.1.100)

## Firewall Configuration (Required for Network Access)
If you want to access from other computers on your network, run this command as Administrator:
```bash
netsh advfirewall firewall add rule name="RedRat Web Server" dir=in action=allow protocol=TCP localport=5000
```

## First-Time Setup

### 1. Connect Camera
- Select a camera from the dropdown
- Click "Start"
- Camera feed will appear in the left panel

### 2. Connect Serial Port (Power Switch)
- Select COM port from dropdown
- Click "Connect"
- Use ON/OFF buttons to control the power switch

### 3. Connect USB Switch (Optional)
- Select COM port for USB Switch
- Click "Connect"
- Use TV/PC buttons to switch between devices

### 4. Start Audio Streaming (Optional)
- Click "Start Audio"
- Adjust volume using the slider
- Use "Mute" button to quickly silence

### 5. Send IR Signals
- Click any IR control button (Power, Menu, Numbers, etc.)
- Signals are sent immediately to RedRat3 device
- All actions are logged in the Action History panel

## Features Overview

### 📹 Camera Streaming
- Real-time camera feed (100ms refresh)
- Support for multiple cameras
- Automatic camera detection

### 📡 RedRat3 IR Control
- All standard remote control buttons
- Power, Menu, Numbers 0-9
- Navigation (arrows), OK, Back, Exit
- Mute, Volume, Channel controls
- Color buttons (Red, Green, Yellow, Blue)

### 🔌 Serial Port Control
- Power Switch: ON/OFF at 115200 baud
- USB Switch: TV/PC at 9600 baud
- Auto-detection of available COM ports
- Real-time logging of all communication

### 🔊 Audio Streaming
- Real-time audio from desktop to browser
- Volume control slider (0-100%)
- Mute/Unmute functionality
- Multiple client support

### 📊 Status Monitoring
- Real-time connection status for all devices
- WebSocket-based live updates
- Action history with timestamps
- Color-coded log entries

## Troubleshooting

### Camera Not Starting
- Ensure camera is connected and not in use
- Try selecting a different camera
- Check camera permissions in Windows settings

### Cannot Access from Other Computer
- Check firewall settings
- Verify both computers are on same network
- Confirm correct IP address
- Disable VPN if enabled

### RedRat3 Not Connecting
- Ensure RedRat3 device is connected via USB
- Check that REDRAT.xml file is in the project directory
- Try unplugging and reconnecting the device

### Audio Not Playing
- Ensure microphone/audio input is not muted
- Check browser audio permissions
- Verify audio input device is working
- Try stopping and restarting audio stream

## Architecture

The application runs as a single web server on your desktop:
- **Backend**: ASP.NET Core 8.0 handles all hardware communication
- **Frontend**: Modern responsive web interface
- **Communication**: REST API + WebSocket for real-time updates
- **Streaming**: Camera (HTTP) + Audio (WebSocket)

All hardware remains connected to the desktop computer. The web server exposes controls through the browser interface.

## Security Notes

- Designed for local network use only
- No authentication implemented
- Consider using HTTPS in production
- Restrict firewall to trusted IPs
- Keep .NET runtime updated

## Next Steps

1. Test all features locally first
2. Configure firewall for network access
3. Test from other computers on your network
4. Consider running as Windows Service for automatic startup

For detailed information, see README.md