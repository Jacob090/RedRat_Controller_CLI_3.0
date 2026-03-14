using NAudio.Wave;
using System.Collections.Concurrent;

namespace RedRat3ControllerWebServer.Services;

public class AudioStreamingService
{
    private WaveInEvent? waveIn;
    private bool isStreaming = false;
    private readonly object _lock = new object();
    
    // Audio buffer for clients
    private readonly ConcurrentDictionary<string, (BlockingCollection<byte[]> queue, int volume)> clients = new();
    private readonly object clientsLock = new object();

    public event Action<string>? OnStatusChanged;

    public bool IsStreaming
    {
        get { lock (_lock) { return isStreaming; } }
    }

    public string[] GetAvailableInputDevices()
    {
        int deviceCount = WaveInEvent.DeviceCount;
        var devices = new List<string>();
        
        for (int i = 0; i < deviceCount; i++)
        {
            var caps = WaveInEvent.GetCapabilities(i);
            devices.Add($"{i}: {caps.ProductName}");
        }
        
        return devices.ToArray();
    }

    public string StartStreaming(int deviceIndex = 0)
    {
        lock (_lock)
        {
            if (isStreaming)
            {
                StopStreaming();
            }

            try
            {
                int deviceCount = WaveInEvent.DeviceCount;
                if (deviceIndex < 0 || deviceIndex >= deviceCount)
                {
                    deviceIndex = 0;
                }

                waveIn = new WaveInEvent
                {
                    DeviceNumber = deviceIndex,
                    WaveFormat = new WaveFormat(44100, 16, 2) // 44.1kHz, 16-bit, stereo
                };

                waveIn.DataAvailable += WaveIn_DataAvailable;
                waveIn.RecordingStopped += WaveIn_RecordingStopped;
                
                waveIn.StartRecording();
                isStreaming = true;
                
                var deviceName = WaveInEvent.GetCapabilities(deviceIndex).ProductName;
                OnStatusChanged?.Invoke($"Audio streaming started: {deviceName}");
                
                return deviceName;
            }
            catch (Exception ex)
            {
                isStreaming = false;
                OnStatusChanged?.Invoke($"Error starting audio: {ex.Message}");
                throw;
            }
        }
    }

    public void StopStreaming()
    {
        lock (_lock)
        {
            if (waveIn != null)
            {
                try
                {
                    waveIn.StopRecording();
                    waveIn.Dispose();
                }
                catch { }
                waveIn = null;
            }
            isStreaming = false;
            OnStatusChanged?.Invoke("Audio streaming stopped");
        }
    }

    private void WaveIn_DataAvailable(object? sender, WaveInEventArgs e)
    {
        // Distribute audio data to all connected clients
        lock (clientsLock)
        {
            foreach (var clientId in clients.Keys.ToList())
            {
                var (queue, volume) = clients[clientId];
                
                try
                {
                    // Apply volume control
                    if (volume < 100)
                    {
                        float volumeFactor = volume / 100.0f;
                        byte[] adjustedBuffer = new byte[e.Buffer.Length];
                        
                        for (int i = 0; i < e.Buffer.Length; i += 2)
                        {
                            short sample = BitConverter.ToInt16(e.Buffer, i);
                            sample = (short)(sample * volumeFactor);
                            byte[] bytes = BitConverter.GetBytes(sample);
                            adjustedBuffer[i] = bytes[0];
                            adjustedBuffer[i + 1] = bytes[1];
                        }
                        
                        queue.TryAdd(adjustedBuffer);
                    }
                    else
                    {
                        queue.TryAdd(e.Buffer.ToArray());
                    }
                }
                catch
                {
                    // Client queue might be full or closed
                }
            }
        }
    }

    private void WaveIn_RecordingStopped(object? sender, StoppedEventArgs e)
    {
        lock (_lock)
        {
            isStreaming = false;
        }
        OnStatusChanged?.Invoke("Audio streaming stopped");
    }

    public string ConnectClient(string clientId)
    {
        lock (clientsLock)
        {
            var queue = new BlockingCollection<byte[]>(1000); // Buffer up to 1000 frames
            clients[clientId] = (queue, 100); // Default volume 100%
            return clientId;
        }
    }

    public void DisconnectClient(string clientId)
    {
        lock (clientsLock)
        {
            if (clients.TryGetValue(clientId, out var clientData))
            {
                clientData.queue.CompleteAdding();
                clientData.queue.Dispose();
                clients.TryRemove(clientId, out _);
            }
        }
    }

    public void SetClientVolume(string clientId, int volume)
    {
        lock (clientsLock)
        {
            if (clients.TryGetValue(clientId, out var clientData))
            {
                var clampedVolume = Math.Max(0, Math.Min(100, volume));
                clients[clientId] = (clientData.queue, clampedVolume);
            }
        }
    }

    public byte[]? GetAudioChunk(string clientId, int timeoutMs = 100)
    {
        lock (clientsLock)
        {
            if (clients.TryGetValue(clientId, out var clientData))
            {
                try
                {
                    if (clientData.queue.TryTake(out var chunk, timeoutMs))
                    {
                        return chunk;
                    }
                }
                catch
                {
                    // Queue might be completed
                }
            }
        }
        return null;
    }

    public void Dispose()
    {
        StopStreaming();
        
        lock (clientsLock)
        {
            foreach (var clientId in clients.Keys.ToList())
            {
                DisconnectClient(clientId);
            }
        }
    }
}