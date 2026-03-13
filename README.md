# RedRat3 Controller UI

A Windows Forms application for controlling RedRat3 IR devices via keyboard input and serial port communication with camera integration.

## Features

### RedRat3 IR Control
- **Keyboard Control**: Press keys on your keyboard to send IR signals to your TV/device
- The application uses the same key mappings as the original console application:
  - `P` = Power
  - `M` = Menu
  - `0-9` = Number buttons
  - Arrow keys = Navigation
  - `V` = Mute
  - `Enter` = OK/Select
  - `X` = Exit
  - `F1-F4` = Color buttons (Red, Green, Yellow, Blue)
  - `+/-` = Volume Up/Down
  - And many more...
- **Action History**: View all sent IR signals with timestamps in the Action History panel
- **Real-time Feedback**: Immediate visual confirmation when signals are sent

### Serial Port Communication
- **Auto-Connect**: Automatically connects when you select a COM port from the dropdown
- **ON/OFF Buttons**: Simple button controls for sending commands (ON = 1, OFF = 0)
- **Real-time Logging**: All serial communication is logged with timestamps in Action History
- **No Manual Connection**: No need to click Connect/Disconnect buttons - it's automatic

### Camera Integration
- **Automatic Camera Detection**: Automatically detects all available cameras on your system using DirectShow
- **Large Camera Display**: 640x480 pixel live camera feed on the left side
- **Camera Selection**: Choose from multiple detected cameras via dropdown (webcams, capture cards, etc.)
- **Live Video Feed**: Real-time video display with automatic streaming
- **Camera Switching**: Seamlessly switch between cameras with automatic reconnection
- **Visual Monitoring**: Watch camera feed while controlling devices with IR signals and serial communication

## How to Use

### RedRat3 Control
1. Launch the application
2. The RedRat3 device will auto-connect on startup
3. Simply press any mapped key to send IR signals
4. Watch the Action History panel (right side) for confirmation of sent signals
5. Errors (if any) will also appear in the Action History

### Serial Port Control
1. Select a COM port from the dropdown on the right (ports are auto-detected from Device Manager)
2. The application automatically connects to the selected port
3. Click the "ON (1)" button to send signal 1
4. Click the "OFF (0)" button to send signal 0
5. View all communication in the Action History panel
6. Change COM port anytime to disconnect and reconnect to a different port

### Camera Control
1. The application automatically detects all available cameras on startup
2. The first available camera starts automatically and displays live video
3. Select a different camera from the dropdown to switch (automatically stops current and starts new camera)
4. The live video feed displays in the large picture box (640x480) on the left
5. Camera selection changes are logged in Action History
6. If no cameras are found, a placeholder message is displayed

### UI Layout
- **Left Side**: Camera section with camera selection dropdown and large video display (640x480)
- **Right Side**: Control panel with:
  - Status indicators for RedRat3 and Serial Port
  - COM port selection and ON/OFF control buttons
  - Action History panel showing all IR signals and serial communication
  - Keyboard shortcut reference at the bottom

## Requirements

- Windows OS
- .NET 8.0
- RedRat3 device connected via USB
- REDRAT.xml signal database file (included)

## Building and Running

```bash
# Restore NuGet packages
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run --project RedRat3ControllerCLI/RedRat3ControllerCLI.csproj
```

## Connection Settings

The serial port uses PuTTY-compatible settings:
- **Connection Type**: Serial
- **Port**: COMx (selectable from dropdown)
- **Speed**: 115200 baud
- **Data Bits**: 8
- **Parity**: None
- **Stop Bits**: 1

## Notes

- Ensure your RedRat3 device is connected before launching
- Serial port must be connected before sending 0/1 commands
- All keyboard shortcuts work when focus is on the main form
- The application automatically detects and connects to available cameras on startup
- Camera switching stops the current camera and starts the newly selected one
- The application automatically disconnects and closes all devices (camera, serial port, RedRat) when closing
- Camera detection uses DirectShow (requires compatible video devices)
