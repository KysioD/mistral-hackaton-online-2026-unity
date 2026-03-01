using Newtonsoft.Json;

namespace io.dto
{
    public class NpcToolDto
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("npcId")] public string NpcId { get; set; }
        [JsonProperty("toolId")] public string ToolId { get; set; }
        [JsonProperty("tool")] public ToolDto Tool { get; set; }
    }
}