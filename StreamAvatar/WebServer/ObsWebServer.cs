using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;
using StreamAvatar.Rendering;

namespace StreamAvatar.WebServer
{
    /// <summary>
    /// HTTP server for OBS Browser Source integration
    /// Serves the avatar as a transparent PNG stream
    /// </summary>
    public class ObsWebServer : IDisposable
    {
        private HttpListener? _listener;
        private AvatarRenderer _renderer;
        private bool _isRunning;
        private int _port = 8080;
        
        public event Action<string>? OnUrlGenerated;
        
        public int Port
        {
            get => _port;
            set
            {
                _port = value;
                if (_isRunning)
                {
                    Restart();
                }
            }
        }
        
        public string ServerUrl => $"http://localhost:{_port}/avatar";
        
        public AvatarRenderer Renderer
        {
            get => _renderer;
            set => _renderer = value;
        }
        
        public ObsWebServer(AvatarRenderer renderer)
        {
            _renderer = renderer;
        }
        
        public async Task StartAsync()
        {
            if (_isRunning) return;
            
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{_port}/");
                _listener.Start();
                _isRunning = true;
                
                OnUrlGenerated?.Invoke(ServerUrl);
                
                while (_isRunning)
                {
                    try
                    {
                        var context = await _listener.GetContextAsync();
                        _ = HandleRequestAsync(context);
                    }
                    catch (HttpListenerException) when (!_isRunning)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start server: {ex.Message}");
                _isRunning = false;
            }
        }
        
        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            
            try
            {
                if (request.Url?.AbsolutePath == "/avatar")
                {
                    // Render avatar to PNG
                    using var bitmap = new SKBitmap(_renderer.CanvasWidth, _renderer.CanvasHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
                    using var canvas = new SKCanvas(bitmap);
                    
                    _renderer.Render(canvas);
                    
                    // Encode to PNG
                    using var image = SKImage.FromBitmap(bitmap);
                    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                    
                    response.ContentType = "image/png";
                    response.ContentLength64 = data.Size;
                    response.Headers.Add("Access-Control-Allow-Origin", "*");
                    response.Headers.Add("Cache-Control", "no-cache");
                    
                    using var stream = response.OutputStream;
                    data.SaveTo(stream);
                }
                else if (request.Url?.AbsolutePath == "/health")
                {
                    // Health check endpoint
                    response.ContentType = "text/plain";
                    var buffer = Encoding.UTF8.GetBytes("OK");
                    response.ContentLength64 = buffer.Length;
                    using var stream = response.OutputStream;
                    await stream.WriteAsync(buffer.AsMemory(0, buffer.Length));
                }
                else
                {
                    // Serve simple HTML page for testing
                    var html = GenerateHtmlPage();
                    response.ContentType = "text/html";
                    response.ContentEncoding = Encoding.UTF8;
                    var buffer = Encoding.UTF8.GetBytes(html);
                    response.ContentLength64 = buffer.Length;
                    using var stream = response.OutputStream;
                    await stream.WriteAsync(buffer.AsMemory(0, buffer.Length));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling request: {ex.Message}");
                response.StatusCode = 500;
            }
            finally
            {
                response.Close();
            }
        }
        
        private string GenerateHtmlPage()
        {
            return $@"<!DOCTYPE html>
<html>
<head>
    <title>StreamAvatar Preview</title>
    <style>
        body {{
            background: #1a1a2e;
            color: #eee;
            font-family: Arial, sans-serif;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            height: 100vh;
            margin: 0;
        }}
        h1 {{ color: #00d9ff; }}
        .container {{
            text-align: center;
            background: #16213e;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 4px 6px rgba(0,0,0,0.3);
        }}
        img {{
            border: 2px solid #0f3460;
            border-radius: 5px;
            max-width: 512px;
            max-height: 512px;
        }}
        .url-box {{
            background: #0f3460;
            padding: 10px;
            border-radius: 5px;
            margin-top: 20px;
            word-break: break-all;
        }}
        .instructions {{
            margin-top: 20px;
            font-size: 14px;
            color: #aaa;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1>🎭 StreamAvatar Preview</h1>
        <img src=""/avatar"" alt=""Avatar"" id=""avatarImg"" />
        <div class=""url-box"">
            <strong>OBS Browser Source URL:</strong><br/>
            {ServerUrl}/avatar
        </div>
        <div class=""instructions"">
            <p><strong>To use in OBS:</strong></p>
            <ol style=""text-align: left;"">
                <li>Add a new ""Browser"" source</li>
                <li>Paste the URL above</li>
                <li>Set Width: 512, Height: 512</li>
                <li>Check ""Shutdown source when tab becomes inactive""</li>
            </ol>
        </div>
    </div>
    <script>
        // Auto-refresh avatar every 16ms (~60fps)
        setInterval(() => {{
            const img = document.getElementById('avatarImg');
            img.src = '/avatar?' + Date.now();
        }}, 16);
    </script>
</body>
</html>";
        }
        
        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            _listener?.Close();
            _listener = null;
        }
        
        private void Restart()
        {
            Stop();
            _ = StartAsync();
        }
        
        public bool IsRunning => _isRunning;
        
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Stop();
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
