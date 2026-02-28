using Newtonsoft.Json;

namespace io.dto
{
    public class PaginatedNpcsEntity
    {
        [JsonProperty("data")] public NpcDto[] Data { get; set; }
    }
}