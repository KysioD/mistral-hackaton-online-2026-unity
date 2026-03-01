using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DefaultNamespace;
using UnityEngine;

namespace io
{
    public class VoxtralWebSocketService
    {
        public static readonly VoxtralWebSocketService Instance = new VoxtralWebSocketService();

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;
        private readonly string _wsUrl;

        public event Action<string> OnTranscriptReceived;
        public event Action OnConnected;
        public event Action OnDisconnected;

        public bool IsConnected => _webSocket?.State == WebSocketState.Open;

        private VoxtralWebSocketService()
        {
            var config = Resources.Load<AppConfig>("AppConfig");
            if (config != null && !string.IsNullOrEmpty(config.VoxtralBaseUrl))
            {
                _wsUrl = $"{config.VoxtralBaseUrl}/voxtral";
            }
            else
            {
                Debug.LogWarning("[Voxtral] VoxtralBaseUrl manquant dans AppConfig. Fallback: ws://localhost:3000/voxtral");
                _wsUrl = "ws://localhost:3000/voxtral";
            }
        }

        public async Task ConnectAsync()
        {
            if (IsConnected) return;

            _webSocket?.Dispose();
            _webSocket = new ClientWebSocket();
            _cts = new CancellationTokenSource();

            try
            {
                await _webSocket.ConnectAsync(new Uri(_wsUrl), _cts.Token);
                Debug.Log($"[Voxtral] Connected to {_wsUrl}");
                OnConnected?.Invoke();
                _ = ReceiveLoopAsync();
            }
            catch (Exception ex)
            {
                var cause = ex.InnerException ?? ex;
                Debug.LogError($"[Voxtral] Connection failed ({_wsUrl}): {cause.Message}");
                _webSocket?.Dispose();
                _webSocket = null;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_webSocket == null) return;

            _cts?.Cancel();

            try
            {
                if (_webSocket.State == WebSocketState.Open)
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Voxtral] Disconnect warning: {ex.Message}");
            }
            finally
            {
                _webSocket?.Dispose();
                _webSocket = null;
                OnDisconnected?.Invoke();
            }
        }

        public async Task SendAudioBytesAsync(byte[] audioData)
        {
            if (!IsConnected) return;

            try
            {
                var segment = new ArraySegment<byte>(audioData);
                await _webSocket.SendAsync(segment, WebSocketMessageType.Binary, true, _cts.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError($"[Voxtral] Send error: {ex.Message}");
            }
        }

        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[32768];

            try
            {
                while (_webSocket?.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                {
                    using var ms = new MemoryStream();
                    WebSocketReceiveResult result;

                    do
                    {
                        var segment = new ArraySegment<byte>(buffer);
                        result = await _webSocket.ReceiveAsync(segment, _cts.Token);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            Debug.LogWarning($"[Voxtral] Server closed — {_webSocket.CloseStatus}: {_webSocket.CloseStatusDescription}");
                            await DisconnectAsync();
                            return;
                        }

                        ms.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);

                    byte[] received = ms.ToArray();
                    if (received.Length > 0)
                        OnTranscriptReceived?.Invoke(Encoding.UTF8.GetString(received));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError($"[Voxtral] Receive error: {ex.Message}");
            }
        }
    }
}
