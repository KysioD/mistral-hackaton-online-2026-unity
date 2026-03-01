using System.Collections.Generic;
using Newtonsoft.Json;

namespace io
{
    public class LLMStreamingResponse
    {
        [JsonProperty("type")] public string Type { get; set; }
        [JsonProperty("content")] public string Content { get; set; }
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("toolName")] public string ToolName { get; set; }
        [JsonProperty("parameters")] public IDictionary<string, string> Parameters { get; set; }
        [JsonProperty("sessionId")] public string SessionId { get; set; }
        [JsonProperty("closed")] public bool Closed { get; set; }
        [JsonProperty("format")] public string Format { get; set; }
    }
}