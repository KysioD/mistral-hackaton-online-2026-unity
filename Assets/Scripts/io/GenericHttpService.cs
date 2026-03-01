using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using UnityEngine.Networking;
using System.Threading.Tasks;
using DefaultNamespace;
using Newtonsoft.Json;
using UnityEngine;

namespace io
{
    public class GenericHttpService
    {   
        private string BaseUrl { get; }
        
        public static readonly GenericHttpService Instance = new GenericHttpService();

        private static readonly HttpClient httpClient = new HttpClient();

        private GenericHttpService()
        {
            // Private constructor to prevent instantiation of the singleton class
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
        
        public async Task StreamPostAsync(string endpoint, string message, Action<string> onChunkReceived)
        {
            string url = $"{BaseUrl}/{endpoint}";

            try
            {
                // 1. On crée le payload avec l'objet anonyme (nom de variable "message" attendu par ton API)
                var payload = new { message = message };
                string jsonBody = JsonConvert.SerializeObject(payload);
        
                // 2. On prépare le contenu au format JSON
                var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                // 3. On construit la requête POST
                using var request = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = content
                };

                // 4. On l'envoie avec l'option magique pour le streaming
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                // 5. On lit le flux au fur et à mesure
                await using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                while (!reader.EndOfStream)
                {
                    string chunk = await reader.ReadLineAsync();

                    if (!string.IsNullOrWhiteSpace(chunk))
                    {
                        onChunkReceived?.Invoke(chunk);
                    }
                }
            } 
            catch (Exception ex)
            {
                Debug.LogError($"Stream POST ERROR ({url}): {ex.Message}");
            }
        }
    }
}