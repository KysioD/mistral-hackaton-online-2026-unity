using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DefaultNamespace;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace io
{
    public class GenericHttpService
    {
        private string BaseUrl { get; }

        public static readonly GenericHttpService Instance = new GenericHttpService();

        private static readonly HttpClient httpClient = new HttpClient();

        private GenericHttpService()
        {
            var config = Resources.Load<AppConfig>("AppConfig");
            if (config != null && !string.IsNullOrEmpty(config.BaseUrl))
            {
                BaseUrl = config.BaseUrl;
            }
            else
            {
                Debug.LogWarning("AppConfig not found or BaseUrl is empty. Using default BaseUrl.");
            }
        }

        public async Task<T> GetAsync<T>(string endpoint, Dictionary<string, string> queryParams = null)
        {
            string url = $"{BaseUrl}/{endpoint}";

            if (queryParams != null && queryParams.Count > 0)
            {
                var queryString = string.Join("&", queryParams.Select(param => $"{param.Key}={param.Value}"));
                url = $"{url}?{queryString}";
            }

            using var request = UnityWebRequest.Get(url);

            var operation = request.SendWebRequest();
            while (!operation.isDone) await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"GET ERROR ({url}): {request.error}");
                return default;
            }

            return JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
        }

        public async Task StreamPostAsync(
            string endpoint,
            string message,
            string sessionId,
            Action<string> onChunkReceived,
            bool voiceEnabled = false,
            string clientId = null,
            CancellationToken cancellationToken = default)
        {
            string url = $"{BaseUrl}/{endpoint}";
            if (voiceEnabled)
                url += "?voice=true";

            // Capture the Unity main-thread SynchronizationContext NOW, before any
            // ConfigureAwait(false) switches us onto a thread-pool thread.
            // All onChunkReceived calls will be posted back through this context so
            // Unity API calls (SetText, SetActive, …) are always on the main thread.
            var unitySyncCtx = SynchronizationContext.Current;

            var payload = new { message, sessionId, clientId };
            string jsonBody = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

            // Send on a thread-pool thread — ConfigureAwait(false) prevents resuming
            // on the Unity main thread, so the read loop never blocks it.
            using var response = await httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content
                .ReadAsStreamAsync()
                .ConfigureAwait(false);

            using var reader = new StreamReader(stream);

            // When the token is cancelled, dispose the response to unblock ReadLineAsync.
            // (ReadLineAsync has no CancellationToken overload in .NET Standard 2.1.)
            using var reg = cancellationToken.Register(() =>
            {
                try { response.Dispose(); } catch { }
            });

            try
            {
                while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
                {
                    // Read on the thread-pool thread — does not touch the main thread.
                    string chunk = await reader.ReadLineAsync().ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(chunk)) continue;

                    // Dispatch callback to the Unity main thread so callers can safely
                    // touch GameObjects, UI elements, etc.
                    if (unitySyncCtx != null)
                        unitySyncCtx.Post(_ => onChunkReceived?.Invoke(chunk), null);
                    else
                        onChunkReceived?.Invoke(chunk); // fallback (e.g. unit tests)
                }
            }
            catch (OperationCanceledException) { }
            catch (IOException) when (cancellationToken.IsCancellationRequested) { }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested) { }
        }
    }
}
