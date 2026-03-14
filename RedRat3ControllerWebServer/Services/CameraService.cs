using AForge.Video;
using AForge.Video.DirectShow;
using System.Drawing;
using System.Drawing.Imaging;

namespace RedRat3ControllerWebServer.Services;

public class CameraService
{
    private VideoCaptureDevice? videoCaptureDevice;
    private bool isRunning = false;
    private int currentCameraIndex = -1;
    private readonly object _lock = new object();
    
    // Latest frame buffer
    private byte[]? latestFrame;
    private readonly object frameLock = new object();

    public event Action<string>? OnStatusChanged;
    public event Action<string>? OnError;

    public bool IsRunning
    {
        get { lock (_lock) { return isRunning; } }
    }

    public string[] GetAvailableCameras()
    {
        try
        {
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            return videoDevices.Cast<FilterInfo>().Select(d => d.Name).ToArray();
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Error getting cameras: {ex.Message}");
            return Array.Empty<string>();
        }
    }

    public void StartCamera(int cameraIndex)
    {
        lock (_lock)
        {
            if (isRunning)
            {
                StopCameraInternal();
                System.Threading.Thread.Sleep(200);
            }

            try
            {
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                
                if (cameraIndex < 0 || cameraIndex >= videoDevices.Count)
                {
                    throw new Exception($"Invalid camera index: {cameraIndex}");
                }

                videoCaptureDevice = new VideoCaptureDevice(videoDevices[cameraIndex].MonikerString);
                videoCaptureDevice.NewFrame += VideoCaptureDevice_NewFrame;
                videoCaptureDevice.Start();
                
                isRunning = true;
                currentCameraIndex = cameraIndex;
                
                OnStatusChanged?.Invoke($"Camera started: {videoDevices[cameraIndex].Name}");
            }
            catch (Exception ex)
            {
                isRunning = false;
                currentCameraIndex = -1;
                OnError?.Invoke($"Error starting camera: {ex.Message}");
                throw;
            }
        }
    }

    public void StopCamera()
    {
        lock (_lock)
        {
            StopCameraInternal();
        }
    }

    private void StopCameraInternal()
    {
        if (videoCaptureDevice != null)
        {
            try
            {
                videoCaptureDevice.NewFrame -= VideoCaptureDevice_NewFrame;
                videoCaptureDevice.SignalToStop();
            }
            catch { }
            videoCaptureDevice = null;
        }
        isRunning = false;
        currentCameraIndex = -1;
        OnStatusChanged?.Invoke("Camera stopped");
    }

    private void VideoCaptureDevice_NewFrame(object? sender, NewFrameEventArgs eventArgs)
    {
        try
        {
            // Convert frame to JPEG bytes with higher quality
            using (var ms = new System.IO.MemoryStream())
            {
                var frame = (Bitmap)eventArgs.Frame.Clone();
                
                // Use JPEG encoder with 95% quality for better image
                var jpegEncoder = new EncoderParameters(1);
                jpegEncoder.Param[0] = new EncoderParameter(Encoder.Quality, 95L);
                
                frame.Save(ms, GetEncoderInfo("image/jpeg"), jpegEncoder);
                frame.Dispose();
                
                lock (frameLock)
                {
                    latestFrame = ms.ToArray();
                }
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Error processing frame: {ex.Message}");
        }
    }

    private ImageCodecInfo GetEncoderInfo(string mimeType)
    {
        ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
        foreach (ImageCodecInfo codec in codecs)
        {
            if (codec.MimeType == mimeType)
                return codec;
        }
        return ImageCodecInfo.GetImageEncoders()[0];
    }

    public byte[]? GetLatestFrame()
    {
        lock (frameLock)
        {
            return latestFrame;
        }
    }

    public void Dispose()
    {
        StopCamera();
    }
}