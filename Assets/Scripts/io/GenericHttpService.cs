using System.Collections.Generic;
using System.Linq;
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
        
        public static GenericHttpService Instance { get; } = new GenericHttpService();

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
        
    }
}