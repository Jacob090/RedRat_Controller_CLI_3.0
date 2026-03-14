// API Base URL
const API_BASE = '';

// Global state
let statusWebSocket = null;
let audioWebSocket = null;
let audioContext = null;
let audioClientId = null;
let audioNextPlayTime = 0; // For sequential scheduling - no overlap
let cameraInterval = null;
let keyAssignments = [];
let keyToSignalMap = {};
let serialPortConnected = false;
let usbSwitchConnected = false;
let currentUSBDevice = 'None';

// Initialize on page load (new version will be added at the end of file)
document.addEventListener('DOMContentLoaded', () => {
    initializeApp();
});

function updateConnectionStatus(connected) {
    const statusEl = document.getElementById('connectionStatus');
    if (connected) {
        statusEl.textContent = '● Connected';
        statusEl.className = 'status-indicator connected';
    } else {
        statusEl.textContent = '● Disconnected';
        statusEl.className = 'status-indicator disconnected';
    }
}

async function loadStatus() {
    try {
        const response = await fetch(`${API_BASE}/api/status`);
        const data = await response.json();
        
        updateStatusDisplay(data);
    } catch (error) {
        console.error('Error loading status:', error);
        addLogEntry('Error loading status', 'error');
    }
}

function updateStatusDisplay(data) {
    document.getElementById('redRatStatus').textContent = data.redRat.status;
    document.getElementById('redRatStatus').className = `status-value ${data.redRat.isConnected ? 'success' : 'error'}`;
    
    document.getElementById('serialStatus').textContent = data.serialPort.isConnected ? data.serialPort.currentPort : 'Disconnected';
    document.getElementById('serialStatus').className = `status-value ${data.serialPort.isConnected ? 'success' : 'error'}`;
    
    document.getElementById('cameraStatus').textContent = data.camera.isRunning ? 'Running' : 'Stopped';
    document.getElementById('cameraStatus').className = `status-value ${data.camera.isRunning ? 'success' : 'error'}`;
    
    document.getElementById('audioStatus').textContent = data.audio.isStreaming ? 'Streaming' : 'Stopped';
    document.getElementById('audioStatus').className = `status-value ${data.audio.isStreaming ? 'success' : 'error'}`;
}

async function loadSerialPorts() {
    try {
        const response = await fetch(`${API_BASE}/api/serialport/ports`);
        const data = await response.json();
        
        const serialSelect = document.getElementById('serialPortSelect');
        const usbSelect = document.getElementById('usbSwitchSelect');
        
        serialSelect.innerHTML = '<option value="">Select a port...</option>';
        usbSelect.innerHTML = '<option value="">Select a port...</option>';
        
        data.ports.forEach(port => {
            serialSelect.innerHTML += `<option value="${port}">${port}</option>`;
            usbSelect.innerHTML += `<option value="${port}">${port}</option>`;
        });
    } catch (error) {
        console.error('Error loading serial ports:', error);
        addLogEntry('Error loading serial ports', 'error');
    }
}

async function loadCameras() {
    try {
        const response = await fetch(`${API_BASE}/api/camera/cameras`);
        const data = await response.json();
        
        const cameraSelect = document.getElementById('cameraSelect');
        cameraSelect.innerHTML = '<option value="">Select a camera...</option>';
        
        data.cameras.forEach((camera, index) => {
            cameraSelect.innerHTML += `<option value="${index}">${camera}</option>`;
        });
    } catch (error) {
        console.error('Error loading cameras:', error);
        addLogEntry('Error loading cameras', 'error');
    }
}

function connectStatusWebSocket() {
    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const wsUrl = `${protocol}//${window.location.host}${API_BASE}/api/websocket/status`;
    
    statusWebSocket = new WebSocket(wsUrl);
    
    statusWebSocket.onopen = () => {
        console.log('Status WebSocket connected');
        addLogEntry('Connected to server', 'info');
    };
    
    statusWebSocket.onmessage = (event) => {
        const message = JSON.parse(event.data);
        handleStatusMessage(message);
    };
    
    statusWebSocket.onerror = (error) => {
        console.error('WebSocket error:', error);
    };
    
    statusWebSocket.onclose = () => {
        console.log('Status WebSocket closed');
        addLogEntry('Disconnected from server', 'error');
        // Reconnect after 5 seconds
        setTimeout(connectStatusWebSocket, 5000);
    };
}

function handleStatusMessage(message) {
    switch (message.type) {
        case 'redrat':
            addLogEntry(`RedRat: ${message.message}`, 'info');
            break;
        case 'signal':
            addLogEntry(`IR Signal: ${message.message}`, 'success');
            break;
        case 'serial':
            addLogEntry(`Serial: ${message.message}`, 'info');
            break;
        case 'camera':
            addLogEntry(`Camera: ${message.message}`, 'info');
            break;
        case 'audio':
            addLogEntry(`Audio: ${message.message}`, 'info');
            break;
        case 'error':
            addLogEntry(`Error: ${message.message}`, 'error');
            break;
    }
    
    // Refresh status after message
    loadStatus();
}

function setupEventListeners() {
    // Camera controls
    document.getElementById('cameraStartBtn').addEventListener('click', startCamera);
    document.getElementById('cameraStopBtn').addEventListener('click', stopCamera);
    
    // Serial port controls
    document.getElementById('serialConnectBtn').addEventListener('click', connectSerialPort);
    document.getElementById('serialDisconnectBtn').addEventListener('click', disconnectSerialPort);
    document.getElementById('onButton').addEventListener('click', () => sendSerialCommand('1'));
    document.getElementById('offButton').addEventListener('click', () => sendSerialCommand('0'));
    
    // USB Switch controls
    document.getElementById('usbConnectBtn').addEventListener('click', connectUsbSwitch);
    document.getElementById('usbDisconnectBtn').addEventListener('click', disconnectUsbSwitch);
    document.getElementById('tvButton').addEventListener('click', () => sendUsbSwitchCommand('MA01'));
    document.getElementById('pcButton').addEventListener('click', () => sendUsbSwitchCommand('MB01'));
    
    // IR signal buttons
    document.querySelectorAll('.ir-btn').forEach(btn => {
        btn.addEventListener('click', () => sendIRSignal(btn.dataset.signal));
    });
    
    // Audio controls
    document.getElementById('audioStartBtn').addEventListener('click', startAudio);
    document.getElementById('audioStopBtn').addEventListener('click', stopAudio);
    document.getElementById('volumeSlider').addEventListener('input', handleVolumeChange);
    document.getElementById('muteBtn').addEventListener('click', toggleMute);
    
    // Clear history
    document.getElementById('clearHistoryBtn').addEventListener('click', clearHistory);
}

async function startCamera() {
    const cameraSelect = document.getElementById('cameraSelect');
    const cameraIndex = cameraSelect.value;
    
    if (!cameraIndex) {
        addLogEntry('Please select a camera first', 'error');
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/api/camera/start`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ cameraIndex: parseInt(cameraIndex) })
        });
        
        if (response.ok) {
            addLogEntry('Camera started', 'success');
            loadStatus();
        } else {
            throw new Error('Failed to start camera');
        }
    } catch (error) {
        console.error('Error starting camera:', error);
        addLogEntry('Error starting camera', 'error');
    }
}

async function stopCamera() {
    try {
        const response = await fetch(`${API_BASE}/api/camera/stop`, {
            method: 'POST'
        });
        
        if (response.ok) {
            addLogEntry('Camera stopped', 'info');
            loadStatus();
        }
    } catch (error) {
        console.error('Error stopping camera:', error);
        addLogEntry('Error stopping camera', 'error');
    }
}

function startCameraRefresh() {
    cameraInterval = setInterval(() => {
        const cameraImage = document.getElementById('cameraImage');
        cameraImage.src = `${API_BASE}/api/camera/frame?t=${Date.now()}`;
    }, 33); // 33ms ≈ 30 FPS for lower latency, better A/V sync
}

async function connectSerialPort() {
    const portSelect = document.getElementById('serialPortSelect');
    const portName = portSelect.value;
    
    if (!portName) {
        addLogEntry('Please select a serial port', 'error');
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/api/serialport/connect`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ portName, baudRate: 115200 })
        });
        
        if (response.ok) {
            serialPortConnected = true;
            document.getElementById('onButton').disabled = false;
            document.getElementById('offButton').disabled = false;
            addLogEntry(`Connected to ${portName}`, 'success');
            loadStatus();
        } else {
            throw new Error('Failed to connect');
        }
    } catch (error) {
        console.error('Error connecting to serial port:', error);
        addLogEntry('Error connecting to serial port', 'error');
    }
}

async function disconnectSerialPort() {
    try {
        const response = await fetch(`${API_BASE}/api/serialport/disconnect`, {
            method: 'POST'
        });
        
        if (response.ok) {
            serialPortConnected = false;
            document.getElementById('onButton').disabled = true;
            document.getElementById('offButton').disabled = true;
            addLogEntry('Disconnected from serial port', 'info');
            loadStatus();
        }
    } catch (error) {
        console.error('Error disconnecting from serial port:', error);
        addLogEntry('Error disconnecting from serial port', 'error');
    }
}

async function sendSerialCommand(command) {
    if (!serialPortConnected) {
        addLogEntry('Serial port not connected', 'error');
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/api/serialport/send`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ command })
        });
        
        if (response.ok) {
            addLogEntry(`Sent: ${command}`, 'success');
        }
    } catch (error) {
        console.error('Error sending serial command:', error);
        addLogEntry('Error sending serial command', 'error');
    }
}

async function connectUsbSwitch() {
    const usbSelect = document.getElementById('usbSwitchSelect');
    const portName = usbSelect.value;
    
    if (!portName) {
        addLogEntry('Please select a serial port', 'error');
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/api/serialport/connect`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ portName, baudRate: 9600 })
        });
        
        if (response.ok) {
            usbSwitchConnected = true;
            document.getElementById('tvButton').disabled = false;
            document.getElementById('pcButton').disabled = false;
            addLogEntry(`USB Switch connected to ${portName}`, 'success');
            loadStatus();
        } else {
            throw new Error('Failed to connect');
        }
    } catch (error) {
        console.error('Error connecting to USB Switch:', error);
        addLogEntry('Error connecting to USB Switch', 'error');
    }
}

async function disconnectUsbSwitch() {
    try {
        const response = await fetch(`${API_BASE}/api/serialport/disconnect`, {
            method: 'POST'
        });
        
        if (response.ok) {
            usbSwitchConnected = false;
            document.getElementById('tvButton').disabled = true;
            document.getElementById('pcButton').disabled = true;
            document.getElementById('tvButton').className = 'btn btn-gray';
            document.getElementById('pcButton').className = 'btn btn-gray';
            currentUSBDevice = 'None';
            addLogEntry('USB Switch disconnected', 'info');
            loadStatus();
        }
    } catch (error) {
        console.error('Error disconnecting USB Switch:', error);
        addLogEntry('Error disconnecting USB Switch', 'error');
    }
}

async function sendUsbSwitchCommand(command) {
    if (!usbSwitchConnected) {
        addLogEntry('USB Switch not connected', 'error');
        return;
    }
    
    try {
        const response = await fetch(`${API_BASE}/api/serialport/send`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ command })
        });
        
        if (response.ok) {
            const deviceName = command === 'MA01' ? 'TV' : 'PC';
            addLogEntry(`USB Switch: Sent ${command} (${deviceName})`, 'success');
            
            // Update button states
            if (command === 'MA01') {
                document.getElementById('tvButton').className = 'btn btn-green';
                document.getElementById('pcButton').className = 'btn btn-gray';
                currentUSBDevice = 'TV';
            } else {
                document.getElementById('pcButton').className = 'btn btn-green';
                document.getElementById('tvButton').className = 'btn btn-gray';
                currentUSBDevice = 'PC';
            }
        }
    } catch (error) {
        console.error('Error sending USB Switch command:', error);
        addLogEntry('Error sending USB Switch command', 'error');
    }
}

async function sendIRSignal(signalName) {
    try {
        const response = await fetch(`${API_BASE}/api/redrat/send`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ deviceName: 'tMate', signalName })
        });
        
        if (!response.ok) {
            throw new Error('Failed to send IR signal');
        }
    } catch (error) {
        console.error('Error sending IR signal:', error);
        addLogEntry(`Error sending IR signal: ${signalName}`, 'error');
    }
}

async function loadAudioDevices() {
    try {
        const response = await fetch(`${API_BASE}/api/audio/devices`);
        const data = await response.json();
        const select = document.getElementById('audioDeviceSelect');
        select.innerHTML = '<option value="">Select microphone...</option>';
        data.devices.forEach((device, index) => {
            select.innerHTML += `<option value="${index}">${device}</option>`;
        });
    } catch (error) {
        console.error('Error loading audio devices:', error);
        document.getElementById('audioDeviceSelect').innerHTML = '<option value="">Failed to load</option>';
    }
}

async function startAudio() {
    const deviceSelect = document.getElementById('audioDeviceSelect');
    const deviceIndex = parseInt(deviceSelect.value) || 0;

    try {
        // 1. Start server-side audio capture first
        const startResponse = await fetch(`${API_BASE}/api/audio/start`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ deviceIndex })
        });
        const startData = await startResponse.json();
        if (!startData.success) {
            addLogEntry(`Audio error: ${startData.error || 'Failed to start'}`, 'error');
            return;
        }

        // 2. Initialize audio context (must be user gesture)
        audioContext = new (window.AudioContext || window.webkitAudioContext)();
        audioClientId = Math.random().toString(36).substring(7);
        audioNextPlayTime = 0;

        // 3. Connect WebSocket to receive stream (pass clientId for volume control)
        const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        const wsUrl = `${protocol}//${window.location.host}${API_BASE}/api/websocket/audio?clientId=${encodeURIComponent(audioClientId)}`;
        audioWebSocket = new WebSocket(wsUrl);
        
        audioWebSocket.onopen = () => {
            addLogEntry(`Audio streaming: ${startData.deviceName}`, 'success');
            loadStatus();
        };
        
        audioWebSocket.onmessage = async (event) => {
            try {
                const rawPcm = await event.data.arrayBuffer();
                playRawPcm(rawPcm);
            } catch (error) {
                console.error('Error playing audio:', error);
            }
        };
        
        audioWebSocket.onerror = () => {
            addLogEntry('Audio streaming error', 'error');
        };
        
        audioWebSocket.onclose = () => {
            addLogEntry('Audio streaming stopped', 'info');
            loadStatus();
        };
    } catch (error) {
        console.error('Error starting audio:', error);
        addLogEntry(`Error starting audio: ${error.message}`, 'error');
    }
}

// Play raw 16-bit stereo PCM at 48000 Hz - proper conversion for clarity
function playRawPcm(arrayBuffer) {
    if (!audioContext || arrayBuffer.byteLength < 4) return;
    
    const sampleRate = 48000;
    const dv = new DataView(arrayBuffer);
    const numSamples = arrayBuffer.byteLength / 2;
    const numFrames = numSamples / 2; // stereo interleaved
    
    const buffer = audioContext.createBuffer(2, numFrames, sampleRate);
    const left = buffer.getChannelData(0);
    const right = buffer.getChannelData(1);
    
    for (let i = 0; i < numFrames; i++) {
        const sL = dv.getInt16(i * 4, true);
        const sR = dv.getInt16(i * 4 + 2, true);
        left[i] = Math.max(-1, Math.min(1, sL / 32768));
        right[i] = Math.max(-1, Math.min(1, sR / 32768));
    }
    
    const now = audioContext.currentTime;
    if (audioNextPlayTime < now) audioNextPlayTime = now;
    
    const source = audioContext.createBufferSource();
    source.buffer = buffer;
    source.connect(audioContext.destination);
    source.start(audioNextPlayTime);
    audioNextPlayTime += buffer.duration;
}

async function stopAudio() {
    if (audioWebSocket) {
        audioWebSocket.close();
        audioWebSocket = null;
    }
    
    if (audioContext) {
        audioContext.close();
        audioContext = null;
    }
    
    try {
        await fetch(`${API_BASE}/api/audio/stop`, { method: 'POST' });
    } catch (e) {}
    
    addLogEntry('Audio streaming stopped', 'info');
    loadStatus();
}

function handleVolumeChange() {
    const volume = document.getElementById('volumeSlider').value;
    document.getElementById('volumeValue').textContent = `${volume}%`;
    
    if (audioClientId) {
        fetch(`${API_BASE}/api/websocket/audio/volume`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ clientId: audioClientId, volume: parseInt(volume) })
        });
    }
}

function toggleMute() {
    const volumeSlider = document.getElementById('volumeSlider');
    const muteBtn = document.getElementById('muteBtn');
    
    if (volumeSlider.value !== '0') {
        volumeSlider.dataset.previousValue = volumeSlider.value;
        volumeSlider.value = '0';
        document.getElementById('volumeValue').textContent = '0%';
        muteBtn.textContent = 'Unmute';
    } else {
        const previousValue = volumeSlider.dataset.previousValue || '100';
        volumeSlider.value = previousValue;
        document.getElementById('volumeValue').textContent = `${previousValue}%`;
        muteBtn.textContent = 'Mute';
    }
    
    handleVolumeChange();
}

function addLogEntry(message, type = 'info') {
    const history = document.getElementById('actionHistory');
    const timestamp = new Date().toLocaleTimeString();
    
    const entry = document.createElement('div');
    entry.className = `log-entry ${type}`;
    entry.textContent = `[${timestamp}] ${message}`;
    
    // Remove placeholder if it exists
    if (history.querySelector('.log-entry').textContent === 'Waiting for actions...') {
        history.innerHTML = '';
    }
    
    history.appendChild(entry);
    history.scrollTop = history.scrollHeight;
    
    // Keep only last 100 entries
    while (history.children.length > 100) {
        history.removeChild(history.firstChild);
    }
}

function clearHistory() {
    const history = document.getElementById('actionHistory');
    history.innerHTML = '<div class="log-entry">History cleared</div>';
}

// Handle page unload
window.addEventListener('beforeunload', () => {
    if (statusWebSocket) {
        statusWebSocket.close();
    }
    if (audioWebSocket) {
        audioWebSocket.close();
    }
    if (cameraInterval) {
        clearInterval(cameraInterval);
    }
});

// Keyboard event handler for IR signals
const KEY_ASSIGNMENTS_STORAGE = 'redrat_key_assignments';
const keyboardMap = {
    'p': 'DTV_POWER_K', 'P': 'DTV_POWER_K',
    'm': 'DTV_MENU_K', 'M': 'DTV_MENU_K',
    'v': 'DTV_MUTE_K', 'V': 'DTV_MUTE_K',
    'x': 'DTV_EXIT_K', 'X': 'DTV_EXIT_K',
    '0': 'DTV_0_K', '1': 'DTV_1_K', '2': 'DTV_2_K', '3': 'DTV_3_K', '4': 'DTV_4_K', 
    '5': 'DTV_5_K', '6': 'DTV_6_K', '7': 'DTV_7_K', '8': 'DTV_8_K', '9': 'DTV_9_K',
    'ArrowUp': 'DTV_UP_K',
    'ArrowDown': 'DTV_DOWN_K',
    'ArrowLeft': 'DTV_LEFT_K',
    'ArrowRight': 'DTV_RIGHT_K',
    'Enter': 'DTV_ENTER_K',
    '+': 'DTV_VOL_UP_K', '=': 'DTV_VOL_UP_K',
    '-': 'DTV_VOL_DOWN_K', '_': 'DTV_VOL_DOWN_K',
    'PageUp': 'DTV_CH_UP_K',
    'PageDown': 'DTV_CH_DOWN_K',
    'F1': 'DTV_RED_K',
    'F2': 'DTV_GREEN_K',
    'F3': 'DTV_YELLOW_K',
    'F4': 'DTV_BLUE_K'
};

function loadKeyAssignments() {
    try {
        const saved = localStorage.getItem(KEY_ASSIGNMENTS_STORAGE);
        if (saved) {
            const parsed = JSON.parse(saved);
            for (const [key, cmd] of Object.entries(parsed)) {
                keyboardMap[key] = cmd;
            }
        }
    } catch (e) {}
}

function saveKeyAssignments() {
    try {
        localStorage.setItem(KEY_ASSIGNMENTS_STORAGE, JSON.stringify(keyboardMap));
    } catch (e) {}
}

document.addEventListener('keydown', (event) => {
    // Don't handle if typing in input fields
    if (event.target.tagName === 'INPUT' || event.target.tagName === 'TEXTAREA' || event.target.tagName === 'SELECT') {
        return;
    }
    
    const key = event.key;
    
    // Check if key is mapped
    if (keyboardMap[key]) {
        event.preventDefault(); // Prevent default browser behavior
        const signalName = keyboardMap[key];
        sendIRSignal(signalName);
        highlightButton(signalName);
    }
});

// Highlight button when key is pressed
function highlightButton(signalName) {
    const button = document.querySelector(`.ir-btn[data-signal="${signalName}"]`);
    if (button) {
        button.style.transform = 'translateY(-4px)';
        button.style.boxShadow = '0 8px 15px rgba(102, 126, 234, 0.4)';
        setTimeout(() => {
            button.style.transform = '';
            button.style.boxShadow = '';
        }, 150);
    }
}

// ==================== COMMAND INPUT FUNCTIONALITY ====================

// Available IR commands
const availableCommands = [
    'DTV_POWER_K', 'DTV_SOURCE_K', 'DTV_0_K', 'DTV_1_K', 'DTV_2_K', 'DTV_3_K', 'DTV_4_K', 'DTV_5_K', 
    'DTV_6_K', 'DTV_7_K', 'DTV_8_K', 'DTV_9_K', 'DTV_TTX_MIX_K', 'DTV_MUTE_K', 'DTV_CH_LIST_K',
    'DTV_VOL_UP_K', 'DTV_VOL_DOWN_K', 'DTV_CH_UP_K', 'DTV_CH_DOWN_K', 'DTV_MENU_K', 'DTV_SMART_K',
    'DTV_GUIDE_K', 'DTV_UP_K', 'DTV_DOWN_K', 'DTV_LEFT_K', 'DTV_RIGHT_K', 'DTV_ENTER_K',
    'DTV_INFO_K', 'DTV_RETURN_K', 'DTV_EXIT_K', 'DTV_RED_K', 'DTV_GREEN_K', 'DTV_YELLOW_K',
    'DTV_BLUE_K', 'SPE_MORE_K', 'DTV_REWIND_K', 'DTV_FF_K', 'DTV_PAUSE_K', 'DTV_REC_K',
    'DTV_PLAY_K', 'DTV_STOP_K', 'DTV_FACTORY_K'
];

// Format key for display
function formatKeyDisplay(key) {
    const map = {
        ' ': 'Space', 'ArrowLeft': '←', 'ArrowRight': '→', 'ArrowUp': '↑', 'ArrowDown': '↓',
        'Enter': 'Enter', 'Backspace': 'Backspace', 'Escape': 'Esc', 'Tab': 'Tab'
    };
    return map[key] || key;
}

// Get first assigned key for a command
function getAssignedKey(signalName) {
    for (const [key, signal] of Object.entries(keyboardMap)) {
        if (signal === signalName) return key;
    }
    return null;
}

// Load available commands as read-only list with assign buttons
function loadAvailableCommands() {
    const listEl = document.getElementById('commandsList');
    listEl.innerHTML = '';
    
    availableCommands.sort().forEach(command => {
        const assignedKey = getAssignedKey(command);
        const displayKey = assignedKey ? formatKeyDisplay(assignedKey) : '—';
        
        const item = document.createElement('div');
        item.className = 'command-item';
        item.dataset.command = command;
        item.innerHTML = `
            <span class="command-item-name" title="${command}">${command}</span>
            <span class="command-item-key">${displayKey}</span>
            <button type="button" class="command-item-assign">Assign</button>
        `;
        
        item.querySelector('.command-item-assign').addEventListener('click', () => startKeyAssignment(item, command));
        listEl.appendChild(item);
    });
}

let keyAssignmentCallback = null;

function startKeyAssignment(itemEl, command) {
    if (keyAssignmentCallback) return;
    
    const btn = itemEl.querySelector('.command-item-assign');
    btn.textContent = 'Press key...';
    btn.classList.add('listening');
    
    keyAssignmentCallback = (event) => {
        event.preventDefault();
        event.stopPropagation();
        const key = event.key;
        
        if (key === 'Escape') {
            cancelKeyAssignment();
            return;
        }
        
        // Remove key from previous command
        if (keyboardMap[key]) {
            delete keyboardMap[key];
        }
        keyboardMap[key] = command;
        saveKeyAssignments();
        
        btn.textContent = 'Assign';
        btn.classList.remove('listening');
        document.removeEventListener('keydown', keyAssignmentCallback);
        keyAssignmentCallback = null;
        
        itemEl.querySelector('.command-item-key').textContent = formatKeyDisplay(key);
        addLogEntry(`Assigned ${formatKeyDisplay(key)} → ${command}`, 'info');
    };
    
    document.addEventListener('keydown', keyAssignmentCallback, { once: false });
}

function cancelKeyAssignment() {
    if (keyAssignmentCallback) {
        document.removeEventListener('keydown', keyAssignmentCallback);
        keyAssignmentCallback = null;
    }
    document.querySelectorAll('.command-item-assign.listening').forEach(btn => {
        btn.textContent = 'Assign';
        btn.classList.remove('listening');
    });
}

// Send command on mapped keys; block unassigned keys from typing
function setupCommandInput() {
    const commandInput = document.getElementById('commandInput');
    
    commandInput.addEventListener('keydown', async (event) => {
        const key = event.key;
        
        // Enter - send full command name if typed, otherwise DTV_ENTER_K
        if (key === 'Enter') {
            event.preventDefault();
            const signalName = commandInput.value.trim();
            if (signalName) {
                await sendIRSignal(signalName);
                addLogEntry(`Sent IR signal: ${signalName}`, 'success');
                commandInput.value = '';
            } else {
                await sendIRSignal('DTV_ENTER_K');
                addLogEntry(`Sent IR signal: DTV_ENTER_K`, 'success');
            }
            return;
        }
        
        // Mapped key - execute immediately, don't type
        if (keyboardMap[key]) {
            event.preventDefault();
            const signalName = keyboardMap[key];
            await sendIRSignal(signalName);
            addLogEntry(`Sent IR signal: ${signalName}`, 'success');
            commandInput.value = '';
            return;
        }
        
        // Key that would type a character but is NOT assigned - block it
        if (key.length === 1 && !event.ctrlKey && !event.metaKey && !event.altKey) {
            event.preventDefault();
            return;
        }
        
        // Allow: Backspace, Delete, Ctrl+V (paste), Ctrl+A, Ctrl+C, etc.
        // Arrow keys are in keyboardMap so they execute - that's intentional
    });
}

// Initialize on app load
async function initializeApp() {
    try {
        loadKeyAssignments();
        setupCommandInput();
        loadAvailableCommands();
        
        // Load initial data
        await loadStatus();
        await loadSerialPorts();
        await loadCameras();
        await loadAudioDevices();
        
        // Setup WebSocket for status updates
        connectStatusWebSocket();
        
        // Setup event listeners
        setupEventListeners();
        
        // Start camera refresh
        startCameraRefresh();
        
        updateConnectionStatus(true);
    } catch (error) {
        console.error('Error initializing app:', error);
        updateConnectionStatus(false);
    }
}
