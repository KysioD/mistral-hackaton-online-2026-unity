using Newtonsoft.Json;

namespace io.dto
{
    public class ToolParameterDto
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("toolId")] public string ToolId { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("required")] public bool Required { get; set; }
        [JsonProperty("createdAt")] public string CreatedAt { get; set; }
        [JsonProperty("updatedAt")] public string UpdatedAt { get; set; }
    }
}