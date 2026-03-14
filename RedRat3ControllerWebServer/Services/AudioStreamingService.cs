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
                    WaveFormat = new WaveFormat(48000, 16, 2),
                    BufferMilliseconds = 100,
                    NumberOfBuffers = 4
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
        int bytesToSend = e.BytesRecorded;
        if (bytesToSend <= 0) return;

        lock (clientsLock)
        {
            foreach (var clientId in clients.Keys.ToList())
            {
                var (queue, volume) = clients[clientId];
                try
                {
                    byte[] dataToSend;
                    if (volume < 100)
                    {
                        float vol = volume / 100.0f;
                        dataToSend = new byte[bytesToSend];
                        for (int i = 0; i < bytesToSend; i += 2)
                        {
                            short s = BitConverter.ToInt16(e.Buffer, i);
                            s = (short)(s * vol);
                            var b = BitConverter.GetBytes(s);
                            dataToSend[i] = b[0];
                            dataToSend[i + 1] = b[1];
                        }
                    }
                    else
                    {
                        dataToSend = new byte[bytesToSend];
                        Array.Copy(e.Buffer, 0, dataToSend, 0, bytesToSend);
                    }
                    queue.TryAdd(dataToSend);
                }
                catch { }
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
            var queue = new BlockingCollection<byte[]>(20); // Buffer for network jitter - avoid dropouts
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

    public byte[]? GetAudioChunk(string clientId, int timeoutMs = 25)
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