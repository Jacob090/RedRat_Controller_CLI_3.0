using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Windows.Forms;
using RedRat.IR;
using RedRat.RedRat3;
using RedRat.Util;
using RedRat.AvDeviceDb;
using RedRat.Util.Serialization;
using AForge.Video;
using AForge.Video.DirectShow;

namespace RedRat3ControllerCLI
{
    public partial class RedRat3ControllerForm : Form
    {
        private IRedRat3? rr3;
        private XmlDeserializationResult<AVDeviceDB>? signalDB;
        private SerialPort? serialPort;
        private bool serialPortConnected = false;
        private VideoCaptureDevice? videoCaptureDevice;
        private bool cameraRunning = false;

        // USB Switch serial port
        private SerialPort? usbSwitchSerialPort;
        private bool usbSwitchSerialConnected = false;
        private string currentUSBDevice = "None";

        // Dynamic key assignments loaded from file
        private List<KeyAssignment> keyAssignments;
        private Dictionary<Keys, string> KeyToIRSignalMap;

        // UI Controls
        private Label titleLabel = new Label();
        private Label redRatStatusLabel = new Label();
        private Label serialStatusLabel = new Label();
        private Label usbSwitchStatusLabel = new Label();
        private Button showCommandsButton = new Button();
        private ComboBox comPortComboBox = new ComboBox();
        private ComboBox usbSwitchComboBox = new ComboBox();
        private ComboBox cameraComboBox = new ComboBox();
        private Button onButton = new Button();
        private Button offButton = new Button();
        private Button tvButton = new Button();
        private Button pcButton = new Button();
        private TextBox actionHistoryTextBox = new TextBox();
        private PictureBox cameraPictureBox = new PictureBox();
        private Label cameraLabel = new Label();
        
        // Layout constants
        private const int rightPanelX = 700;
        private const int rightPanelWidth = 480;

        public RedRat3ControllerForm()
        {
            InitializeComponent();
            LoadKeyAssignments();
            InitializeRedRat();
            LoadAvailableComPorts();
            LoadAvailableUsbSwitchPorts();
            LoadAvailableCameras();
        }

        private void LoadKeyAssignments()
        {
            keyAssignments = KeyAssignmentsWindow.GetCurrentKeyAssignments();
            RebuildKeyMap();
        }

        private void RebuildKeyMap()
        {
            KeyToIRSignalMap = new Dictionary<Keys, string>();
            foreach (var assignment in keyAssignments)
            {
                KeyToIRSignalMap[assignment.AssignedKey] = assignment.SignalName;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "RedRat3 Controller";
            this.ClientSize = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyPreview = true;

            // Camera Section (Left Side)
            // Camera Label
            cameraLabel.Text = "Camera Feed";
            cameraLabel.Location = new Point(20, 20);
            cameraLabel.Size = new Size(200, 20);
            cameraLabel.Font = new Font("Arial", 12, FontStyle.Bold);
            this.Controls.Add(cameraLabel);

            // Camera Selection Label
            Label cameraSelectLabel = new Label();
            cameraSelectLabel.Text = "Select Camera:";
            cameraSelectLabel.Location = new Point(20, 50);
            cameraSelectLabel.Size = new Size(100, 20);
            this.Controls.Add(cameraSelectLabel);

            // Camera ComboBox
            cameraComboBox.Location = new Point(120, 48);
            cameraComboBox.Size = new Size(200, 25);
            cameraComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            cameraComboBox.SelectedIndexChanged += CameraComboBox_SelectedIndexChanged;
            this.Controls.Add(cameraComboBox);

            // Camera PictureBox (Large display area)
            cameraPictureBox.Location = new Point(20, 80);
            cameraPictureBox.Size = new Size(640, 480);
            cameraPictureBox.BackColor = Color.Black;
            cameraPictureBox.BorderStyle = BorderStyle.FixedSingle;
            cameraPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            cameraPictureBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom;
            this.Controls.Add(cameraPictureBox);

            // Controls Section (Right Side)
            // Title
            titleLabel.Text = "Functions Controller";
            titleLabel.Font = new Font("Arial", 16, FontStyle.Bold);
            titleLabel.Location = new Point(rightPanelX, 20);
            titleLabel.Size = new Size(rightPanelWidth, 30);
            titleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.Controls.Add(titleLabel);

            // RedRat Status
            redRatStatusLabel.Text = "RedRat Status: Connecting...";
            redRatStatusLabel.Location = new Point(rightPanelX, 60);
            redRatStatusLabel.Size = new Size(rightPanelWidth, 20);
            redRatStatusLabel.ForeColor = Color.Blue;
            redRatStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.Controls.Add(redRatStatusLabel);

            // Serial Status
            serialStatusLabel.Text = "Serial Port Status: Disconnected";
            serialStatusLabel.Location = new Point(rightPanelX, 90);
            serialStatusLabel.Size = new Size(rightPanelWidth, 20);
            serialStatusLabel.ForeColor = Color.Red;
            serialStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.Controls.Add(serialStatusLabel);

            // COM Port Label
            Label comPortLabel = new Label();
            comPortLabel.Text = "Power Switch:";
            comPortLabel.Location = new Point(rightPanelX, 135);
            comPortLabel.Size = new Size(90, 20);
            this.Controls.Add(comPortLabel);

            // COM Port ComboBox
            comPortComboBox.Location = new Point(rightPanelX + 95, 132);
            comPortComboBox.Size = new Size(150, 25);
            comPortComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            comPortComboBox.SelectedIndexChanged += ComPortComboBox_SelectedIndexChanged;
            comPortComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.Controls.Add(comPortComboBox);

            // ON Button
            onButton.Text = "ON";
            onButton.Location = new Point(rightPanelX + 250, 128);
            onButton.Size = new Size(100, 30);
            onButton.Click += OnButton_Click;
            onButton.Enabled = false;
            onButton.BackColor = Color.LightGreen;
            onButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.Controls.Add(onButton);

            // OFF Button
            offButton.Text = "OFF";
            offButton.Location = new Point(rightPanelX + 360, 128);
            offButton.Size = new Size(100, 30);
            offButton.Click += OffButton_Click;
            offButton.Enabled = false;
            offButton.BackColor = Color.LightCoral;
            offButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.Controls.Add(offButton);

            // USB Switch Section
            // Separator Line
            Label separator1 = new Label();
            separator1.Location = new Point(rightPanelX, 165);
            separator1.Size = new Size(rightPanelWidth, 2);
            separator1.BorderStyle = BorderStyle.Fixed3D;
            this.Controls.Add(separator1);

            // USB Switch Label
            Label usbSwitchLabel = new Label();
            usbSwitchLabel.Text = "USB Switch:";
            usbSwitchLabel.Location = new Point(rightPanelX, 180);
            usbSwitchLabel.Size = new Size(90, 20);
            this.Controls.Add(usbSwitchLabel);

            // USB Switch ComboBox
            usbSwitchComboBox.Location = new Point(rightPanelX + 95, 178);
            usbSwitchComboBox.Size = new Size(150, 25);
            usbSwitchComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            usbSwitchComboBox.SelectedIndexChanged += UsbSwitchComboBox_SelectedIndexChanged;
            usbSwitchComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.Controls.Add(usbSwitchComboBox);

            // TV Button
            tvButton.Text = "TV (MA01)";
            tvButton.Location = new Point(rightPanelX + 250, 172);
            tvButton.Size = new Size(100, 35);
            tvButton.Click += TvButton_Click;
            tvButton.Enabled = false;
            tvButton.BackColor = Color.LightGray;
            tvButton.Font = new Font("Arial", 9, FontStyle.Bold);
            tvButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.Controls.Add(tvButton);

            // PC Button
            pcButton.Text = "PC (MB01)";
            pcButton.Location = new Point(rightPanelX + 360, 172);
            pcButton.Size = new Size(100, 35);
            pcButton.Click += PcButton_Click;
            pcButton.Enabled = false;
            pcButton.BackColor = Color.LightGray;
            pcButton.Font = new Font("Arial", 9, FontStyle.Bold);
            pcButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.Controls.Add(pcButton);

            // USB Switch Status
            usbSwitchStatusLabel.Text = "USB Switch: Disconnected";
            usbSwitchStatusLabel.Location = new Point(rightPanelX, 215);
            usbSwitchStatusLabel.Size = new Size(rightPanelWidth, 20);
            usbSwitchStatusLabel.ForeColor = Color.Red;
            usbSwitchStatusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.Controls.Add(usbSwitchStatusLabel);

            // Action History Label
            Label actionHistoryLabel = new Label();
            actionHistoryLabel.Text = "Action History:";
            actionHistoryLabel.Location = new Point(rightPanelX, 245);
            actionHistoryLabel.Size = new Size(rightPanelWidth, 20);
            actionHistoryLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.Controls.Add(actionHistoryLabel);

            // Action History TextBox (resized to fit new USB Switch section)
            actionHistoryTextBox = new TextBox();
            actionHistoryTextBox.Name = "actionHistoryTextBox";
            actionHistoryTextBox.Location = new Point(rightPanelX, 270);
            actionHistoryTextBox.Size = new Size(rightPanelWidth, 385);
            actionHistoryTextBox.Multiline = true;
            actionHistoryTextBox.ScrollBars = ScrollBars.Vertical;
            actionHistoryTextBox.ReadOnly = true;
            actionHistoryTextBox.BackColor = Color.White;
            actionHistoryTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
            this.Controls.Add(actionHistoryTextBox);

            // Command Manager Button (renamed and centered)
            showCommandsButton.Text = "Command Manager";
            showCommandsButton.Location = new Point(rightPanelX + 165, 650);
            showCommandsButton.Size = new Size(150, 30);
            showCommandsButton.BackColor = Color.LightBlue;
            showCommandsButton.Click += ShowCommandsButton_Click;
            showCommandsButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.Controls.Add(showCommandsButton);

            // KeyPress event for form (better for character keys)
            this.KeyPress += RedRat3ControllerForm_KeyPress;

            // Handle window resize event
            this.Resize += RedRat3ControllerForm_Resize;
        }

        private void RedRat3ControllerForm_Resize(object? sender, EventArgs e)
        {
            // Resize camera PictureBox to fill available space on the left
            int margin = 20;
            int topOffset = 80;
            int cameraWidth = this.ClientSize.Width - rightPanelWidth - margin * 3;
            int cameraHeight = this.ClientSize.Height - topOffset - margin;
            
            if (cameraWidth > 0 && cameraHeight > 0)
            {
                cameraPictureBox.Size = new Size(cameraWidth, cameraHeight);
            }
        }

        private void InitializeRedRat()
        {
            try
            {
                rr3 = FindRedRat();
                rr3.Connect();
                redRatStatusLabel.Text = "RedRat Status: Connected";
                redRatStatusLabel.ForeColor = Color.Green;

                signalDB = Serializer.AvDeviceDbFromXmlFile("REDRAT.xml");
                redRatStatusLabel.Text = "RedRat Status: Connected - Database loaded";
                redRatStatusLabel.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                redRatStatusLabel.Text = $"RedRat Status: Error - {ex.Message}";
                redRatStatusLabel.ForeColor = Color.Red;
            }
        }

        private void LoadAvailableComPorts()
        {
            comPortComboBox.Items.Clear();
            string[] ports = SerialPort.GetPortNames();
            if (ports.Length > 0)
            {
                comPortComboBox.Items.AddRange(ports);
                comPortComboBox.SelectedIndex = 0;
                // Auto-connect to first port
                ConnectToSelectedPort();
            }
            else
            {
                comPortComboBox.Items.Add("No COM ports available");
                comPortComboBox.SelectedIndex = 0;
            }
        }

        private void LoadAvailableCameras()
        {
            cameraComboBox.Items.Clear();
            
            try
            {
                // Get available video devices using DirectShow
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                
                if (videoDevices.Count > 0)
                {
                    foreach (FilterInfo device in videoDevices)
                    {
                        cameraComboBox.Items.Add(device.Name);
                    }
                    cameraComboBox.SelectedIndex = 0;
                    
                    // Automatically start the first camera
                    StartCamera(cameraComboBox.SelectedIndex);
                }
                else
                {
                    cameraComboBox.Items.Add("No cameras available");
                    cameraComboBox.SelectedIndex = 0;
                    SetCameraPlaceholder("No cameras found");
                }
            }
            catch (Exception ex)
            {
                cameraComboBox.Items.Add("Error loading cameras");
                cameraComboBox.SelectedIndex = 0;
                SetCameraPlaceholder($"Error: {ex.Message}");
                LogToActionHistory($"Error loading cameras: {ex.Message}");
            }
        }

        private void LoadAvailableUsbSwitchPorts()
        {
            usbSwitchComboBox.Items.Clear();
            string[] ports = SerialPort.GetPortNames();
            if (ports.Length > 0)
            {
                usbSwitchComboBox.Items.AddRange(ports);
                usbSwitchComboBox.SelectedIndex = 0;
                // Auto-connect to first port
                ConnectToUsbSwitchPort();
            }
            else
            {
                usbSwitchComboBox.Items.Add("No COM ports available");
                usbSwitchComboBox.SelectedIndex = 0;
            }
        }

        private void UsbSwitchComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Disconnect from current USB switch port if connected
            DisconnectFromUsbSwitchPort();
            
            // Connect to new selected USB switch port
            ConnectToUsbSwitchPort();
        }

        private void ConnectToUsbSwitchPort()
        {
            try
            {
                if (usbSwitchComboBox.SelectedItem?.ToString() == "No COM ports available")
                {
                    return;
                }

                string portName = usbSwitchComboBox.SelectedItem?.ToString() ?? "COM1";
                usbSwitchSerialPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
                usbSwitchSerialPort.Open();
                
                usbSwitchSerialConnected = true;
                usbSwitchStatusLabel.Text = $"USB Switch: Connected to {portName}";
                usbSwitchStatusLabel.ForeColor = Color.Green;
                
                tvButton.Enabled = true;
                pcButton.Enabled = true;

                LogToActionHistory($"USB Switch connected to {portName} at 9600 baud");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to USB Switch port: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                usbSwitchSerialConnected = false;
                usbSwitchStatusLabel.Text = $"USB Switch: Error - {ex.Message}";
                usbSwitchStatusLabel.ForeColor = Color.Red;
                tvButton.Enabled = false;
                pcButton.Enabled = false;
            }
        }

        private void DisconnectFromUsbSwitchPort()
        {
            try
            {
                if (usbSwitchSerialPort != null && usbSwitchSerialPort.IsOpen)
                {
                    usbSwitchSerialPort.Close();
                    usbSwitchSerialPort.Dispose();
                    usbSwitchSerialPort = null;
                }

                usbSwitchSerialConnected = false;
                usbSwitchStatusLabel.Text = "USB Switch: Disconnected";
                usbSwitchStatusLabel.ForeColor = Color.Red;
                
                tvButton.Enabled = false;
                pcButton.Enabled = false;
                
                // Reset button colors
                tvButton.BackColor = Color.LightGray;
                pcButton.BackColor = Color.LightGray;
                currentUSBDevice = "None";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error disconnecting from USB Switch port: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TvButton_Click(object? sender, EventArgs e)
        {
            if (usbSwitchSerialConnected && usbSwitchSerialPort != null && usbSwitchSerialPort.IsOpen)
            {
                try
                {
                    // Send "MA01" followed by carriage return and line feed (Enter key equivalent)
                    usbSwitchSerialPort.Write("MA01\r\n");
                    LogToActionHistory("USB Switch: Sent MA01 (TV)");
                    
                    // Update button states
                    currentUSBDevice = "TV";
                    tvButton.BackColor = Color.LightGreen;
                    pcButton.BackColor = Color.LightGray;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error sending TV command: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("USB Switch port is not connected.", "Not Connected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void PcButton_Click(object? sender, EventArgs e)
        {
            if (usbSwitchSerialConnected && usbSwitchSerialPort != null && usbSwitchSerialPort.IsOpen)
            {
                try
                {
                    // Send "MB01" followed by carriage return and line feed (Enter key equivalent)
                    usbSwitchSerialPort.Write("MB01\r\n");
                    LogToActionHistory("USB Switch: Sent MB01 (PC)");
                    
                    // Update button states
                    currentUSBDevice = "PC";
                    pcButton.BackColor = Color.LightGreen;
                    tvButton.BackColor = Color.LightGray;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error sending PC command: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("USB Switch port is not connected.", "Not Connected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CameraComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Handle camera selection change
            if (cameraComboBox.SelectedIndex >= 0 && cameraComboBox.SelectedItem != null)
            {
                string selectedCamera = cameraComboBox.SelectedItem.ToString() ?? "";
                
                // Stop current camera if running
                StopCamera();
                
                // Start new camera
                StartCamera(cameraComboBox.SelectedIndex);
                
                LogToActionHistory($"Camera switched to: {selectedCamera}");
            }
        }

        private void StartCamera(int cameraIndex)
        {
            // Stop any existing camera first
            StopCamera();
            
            // Add a small delay to ensure previous camera is fully stopped
            System.Threading.Thread.Sleep(200);
            
            try
            {
                if (cameraIndex < 0 || cameraComboBox.Items[cameraIndex].ToString() == "No cameras available")
                {
                    SetCameraPlaceholder("No cameras found");
                    return;
                }

                // Get video devices
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                
                if (cameraIndex >= videoDevices.Count)
                {
                    return;
                }

                // Create video capture device
                videoCaptureDevice = new VideoCaptureDevice(videoDevices[cameraIndex].MonikerString);
                videoCaptureDevice.NewFrame += VideoCaptureDevice_NewFrame;
                
                // Start the camera
                videoCaptureDevice.Start();
                cameraRunning = true;
                
                LogToActionHistory($"Camera started: {videoDevices[cameraIndex].Name}");
            }
            catch (Exception ex)
            {
                LogToActionHistory($"Error starting camera: {ex.Message}");
                SetCameraPlaceholder($"Camera error: {ex.Message}");
                
                // Force cleanup
                if (videoCaptureDevice != null)
                {
                    try
                    {
                        videoCaptureDevice.NewFrame -= VideoCaptureDevice_NewFrame;
                    }
                    catch { }
                    videoCaptureDevice = null;
                }
                cameraRunning = false;
            }
        }

        private void StopCamera()
        {
            try
            {
                if (videoCaptureDevice != null)
                {
                    // Remove event handler immediately
                    videoCaptureDevice.NewFrame -= VideoCaptureDevice_NewFrame;
                    
                    // Signal stop without waiting
                    try
                    {
                        videoCaptureDevice.SignalToStop();
                    }
                    catch { /* Ignore */ }
                    
                    // Force close immediately - don't wait
                    videoCaptureDevice = null;
                    cameraRunning = false;
                }
            }
            catch
            {
                // Ignore all errors - force close
                videoCaptureDevice = null;
                cameraRunning = false;
            }
        }

        private void VideoCaptureDevice_NewFrame(object? sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                // Dispose old image to prevent memory leak
                Bitmap? oldImage = cameraPictureBox.Image as Bitmap;
                
                this.Invoke((MethodInvoker)delegate {
                    cameraPictureBox.Image = (Bitmap)eventArgs.Frame.Clone();
                });
                
                // Dispose the old image outside Invoke to avoid thread issues
                oldImage?.Dispose();
            }
            catch (Exception ex)
            {
                LogToActionHistory($"Error displaying camera frame: {ex.Message}");
            }
        }

        private void SetCameraPlaceholder(string message)
        {
            // Dispose old image first
            Bitmap? oldImage = cameraPictureBox.Image as Bitmap;
            oldImage?.Dispose();
            
            // Create a placeholder image for the camera
            int width = cameraPictureBox.Width > 0 ? cameraPictureBox.Width : 640;
            int height = cameraPictureBox.Height > 0 ? cameraPictureBox.Height : 480;
            
            using (Bitmap bmp = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Black);
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    
                    // Draw text
                    float centerX = width / 2f;
                    float centerY = height / 2f;
                    
                    g.DrawString(message, 
                        new Font("Arial", 18, FontStyle.Bold), 
                        Brushes.White, 
                        new PointF(centerX, centerY - 20),
                        new StringFormat { Alignment = StringAlignment.Center });
                    
                    g.DrawString("Camera Feed",
                        new Font("Arial", 16),
                        Brushes.Gray,
                        new PointF(centerX, centerY + 20),
                        new StringFormat { Alignment = StringAlignment.Center });
                }
                
                cameraPictureBox.Image = (Bitmap)bmp.Clone();
            }
        }

        private void ComPortComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // Disconnect from current port if connected
            DisconnectFromPort();
            
            // Connect to new selected port
            ConnectToSelectedPort();
        }

        private void ConnectToSelectedPort()
        {
            try
            {
                if (comPortComboBox.SelectedItem?.ToString() == "No COM ports available")
                {
                    return;
                }

                string portName = comPortComboBox.SelectedItem?.ToString() ?? "COM1";
                serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.Open();
                
                serialPortConnected = true;
                serialStatusLabel.Text = $"Serial Port Status: Connected to {portName}";
                serialStatusLabel.ForeColor = Color.Green;
                
                onButton.Enabled = true;
                offButton.Enabled = true;

                LogToActionHistory($"Connected to {portName} at 115200 baud");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting to serial port: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                serialPortConnected = false;
                serialStatusLabel.Text = $"Serial Port Status: Error - {ex.Message}";
                serialStatusLabel.ForeColor = Color.Red;
                onButton.Enabled = false;
                offButton.Enabled = false;
            }
        }

        private void DisconnectFromPort()
        {
            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort.Close();
                    serialPort.Dispose();
                    serialPort = null;
                }

                serialPortConnected = false;
                serialStatusLabel.Text = "Serial Port Status: Disconnected";
                serialStatusLabel.ForeColor = Color.Red;
                
                onButton.Enabled = false;
                offButton.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error disconnecting from serial port: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnButton_Click(object? sender, EventArgs e)
        {
            if (serialPortConnected && serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    // Send "1" followed by carriage return and line feed (Enter key equivalent)
                    serialPort.Write("1\r\n");
                    LogToActionHistory("Sent: 1 (ON)");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error sending ON command: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Serial port is not connected.", "Not Connected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OffButton_Click(object? sender, EventArgs e)
        {
            if (serialPortConnected && serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    // Send "0" followed by carriage return and line feed (Enter key equivalent)
                    serialPort.Write("0\r\n");
                    LogToActionHistory("Sent: 0 (OFF)");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error sending OFF command: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Serial port is not connected.", "Not Connected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SerialPort_DataReceived(object? sender, SerialDataReceivedEventArgs e)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    string data = serialPort.ReadExisting();
                    this.Invoke((MethodInvoker)delegate {
                        LogToActionHistory($"Received: {data}");
                    });
                }
                catch (Exception ex)
                {
                    this.Invoke((MethodInvoker)delegate {
                        LogToActionHistory($"Error reading data: {ex.Message}");
                    });
                }
            }
        }

        private void RedRat3ControllerForm_KeyPress(object? sender, KeyPressEventArgs e)
        {
            // Map character to Keys enum
            Keys keyChar = (Keys)char.ToUpper(e.KeyChar);
            
            // Check if key has mapped IR signal
            if (KeyToIRSignalMap.ContainsKey(keyChar))
            {
                e.Handled = true;
                var signalName = KeyToIRSignalMap[keyChar];
                SignalOutput(keyChar, signalName);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Handle special keys (arrows, F-keys, etc.) that don't trigger KeyPress
            if (KeyToIRSignalMap.ContainsKey(keyData))
            {
                var signalName = KeyToIRSignalMap[keyData];
                SignalOutput(keyData, signalName);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void SignalOutput(Keys keyPressed, string signalName)
        {
            try
            {
                // Check if this key is assigned to multiple commands (duplicate key assignment)
                var duplicateAssignments = keyAssignments.Where(ka => ka.AssignedKey == keyPressed).ToList();
                if (duplicateAssignments.Count > 1)
                {
                    var duplicateNames = string.Join(", ", duplicateAssignments.Select(ka => ka.DisplayName));
                    string errorMessage = $"Invalid key assignment! Key '{keyPressed}' is assigned to multiple commands: {duplicateNames}";
                    LogToActionHistory($"ERROR: {errorMessage}");
                    MessageBox.Show(errorMessage, "Invalid Key Assignment", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (rr3 == null)
                {
                    redRatStatusLabel.Text = "RedRat Status: Not connected";
                    redRatStatusLabel.ForeColor = Color.Red;
                    return;
                }

                var signal = GetSignal("tMate", signalName);
                rr3.OutputModulatedSignal(signal);
                LogToActionHistory($"IR Signal sent: {signalName}");
            }
            catch (Exception ex)
            {
                redRatStatusLabel.Text = $"Error: {ex.Message}";
                redRatStatusLabel.ForeColor = Color.Red;
                LogToActionHistory($"Error sending {signalName}: {ex.Message}");
            }
        }

        private void LogToActionHistory(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            actionHistoryTextBox.AppendText($"[{timestamp}] {message}{Environment.NewLine}");
            actionHistoryTextBox.ScrollToCaret();
            
            // Prevent memory leak by limiting history to last 1000 lines
            if (actionHistoryTextBox.Lines.Length > 1000)
            {
                string[] lines = actionHistoryTextBox.Lines;
                string[] newLines = new string[1000];
                Array.Copy(lines, lines.Length - 1000, newLines, 0, 1000);
                actionHistoryTextBox.Text = string.Join(Environment.NewLine, newLines);
            }
        }

        private IRedRat3 FindRedRat()
        {
            if (!(RRUtil.GetDefaultUsbRedRat() is IRedRat3 rr))
            {
                throw new Exception("Unable to find any USB RedRat devices attached to this computer.");
            }
            return rr;
        }

        private IRPacket GetSignal(string deviceName, string signalName)
        {
            var avdevice = signalDB?.Object as AVDeviceDB;
            var device = avdevice?.GetAVDevice(deviceName);
            if (device == null)
            {
                throw new Exception($"No device of name '{deviceName}' found in the signal database.");
            }
            var signal = device.GetSignal(signalName);
            if (signal == null)
            {
                throw new Exception($"No signal of name '{signalName}' found for device '{deviceName}' in the signal database.");
            }
            return signal;
        }

        private void ShowCommandsButton_Click(object? sender, EventArgs e)
        {
            // Create and show key assignments window
            var assignmentsWindow = new KeyAssignmentsWindow(keyAssignments);
            assignmentsWindow.KeyAssignmentsUpdated += AssignmentsWindow_KeyAssignmentsUpdated;
            assignmentsWindow.ShowDialog(this);
        }

        private void AssignmentsWindow_KeyAssignmentsUpdated(object? sender, KeyAssignmentsUpdatedEventArgs e)
        {
            // Update key assignments from the window
            keyAssignments = e.KeyAssignments;
            
            // Rebuild the key map
            RebuildKeyMap();
            
            LogToActionHistory("Key assignments updated and saved.");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Force cleanup - no waiting, no errors
            try
            {
                // 1. Force stop camera immediately
                StopCamera();
                
                // 2. Dispose camera image
                if (cameraPictureBox.Image != null)
                {
                    cameraPictureBox.Image.Dispose();
                    cameraPictureBox.Image = null;
                }
                
                // 3. Close serial port immediately
                if (serialPort != null)
                {
                    try { serialPort.Close(); } catch { }
                    try { serialPort.Dispose(); } catch { }
                    serialPort = null;
                }

                // 4. Close USB Switch serial port
                if (usbSwitchSerialPort != null)
                {
                    try { usbSwitchSerialPort.Close(); } catch { }
                    try { usbSwitchSerialPort.Dispose(); } catch { }
                    usbSwitchSerialPort = null;
                }

                // 5. Disconnect RedRat
                if (rr3 != null)
                {
                    try { rr3.Disconnect(); } catch { }
                    rr3 = null;
                }
                
                // 6. Clear database
                signalDB = null;
            }
            catch
            {
                // Ignore all errors - force close
            }

            base.OnFormClosing(e);
            
            // Force application exit immediately
            Application.Exit();
        }
