using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DefaultNamespace;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace io
{
    /// <summary>
    /// WebSocket client for the backend /npc-audio gateway (port 8080).
    /// On connect, the server sends { event:"connected", clientId:"uuid" }.
    /// When a talk request is in progress with voice, the server sends
    /// { type:"audio", content:"base64mp3", format:"mp3" } chunks.
    /// </summary>
    public class NpcAudioWebSocketService
    {
        public static readonly NpcAudioWebSocketService Instance = new NpcAudioWebSocketService();

        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cts;
        private readonly string _wsUrl;

        /// <summary>Fired (from background thread) once the server sends the clientId.</summary>
        public event Action<string> OnClientIdReceived;

        /// <summary>Fired (from background thread) for each base64 MP3 audio chunk.</summary>
        public event Action<string> OnAudioChunkReceived;

        public bool IsConnected => _webSocket?.State == WebSocketState.Open;

        private NpcAudioWebSocketService()
        {
            var config = Resources.Load<AppConfig>("AppConfig");
            if (config != null && !string.IsNullOrEmpty(config.NpcAudioBaseUrl))
            {
                _wsUrl = $"{config.NpcAudioBaseUrl}/npc-audio";
            }
            else
            {
                Debug.LogWarning("[NpcAudio] NpcAudioBaseUrl manquant dans AppConfig. Fallback: ws://localhost:8080/npc-audio");
                _wsUrl = "ws://localhost:8080/npc-audio";
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
                Debug.Log($"[NpcAudio] Connected to {_wsUrl}");
                _ = ReceiveLoopAsync();
            }
            catch (Exception ex)
            {
                var cause = ex.InnerException ?? ex;
                Debug.LogError($"[NpcAudio] Connection failed ({_wsUrl}): {cause.Message}");
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
                Debug.LogWarning($"[NpcAudio] Disconnect warning: {ex.Message}");
            }
            finally
            {
                _webSocket?.Dispose();
                _webSocket = null;
            }
        }

        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[65536];

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
                            Debug.LogWarning($"[NpcAudio] Server closed — {_webSocket.CloseStatus}");
                            await DisconnectAsync();
                            return;
                        }

                        ms.Write(buffer, 0, result.Count);
                    }
                    while (!result.EndOfMessage);

                    byte[] received = ms.ToArray();
                    if (received.Length == 0) continue;

                    string json = Encoding.UTF8.GetString(received);
                    HandleMessage(json);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError($"[NpcAudio] Receive error: {ex.Message}");
            }
        }

        private void HandleMessage(string json)
        {
            try
            {
                var obj = JObject.Parse(json);

                // { event: "connected", clientId: "uuid" }
                string evt = obj["event"]?.Value<string>();
                if (evt == "connected")
                {
                    string clientId = obj["clientId"]?.Value<string>();
                    if (!string.IsNullOrEmpty(clientId))
                    {
                        Debug.Log($"[NpcAudio] Got clientId: {clientId}");
                        OnClientIdReceived?.Invoke(clientId);
                    }
                    return;
                }

                // { type: "audio", content: "base64...", format: "mp3" }
                string type = obj["type"]?.Value<string>();
                if (type == "audio")
                {
                    string content = obj["content"]?.Value<string>();
                    if (!string.IsNullOrEmpty(content))
                        OnAudioChunkReceived?.Invoke(content);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NpcAudio] Failed to parse message: {ex.Message}\nRaw: {json}");
            }
        }
    }
}
