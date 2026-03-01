using Newtonsoft.Json;

namespace io.dto
{
    public class NpcDto
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("firstName")] public string FirstName { get; set; }
        [JsonProperty("lastName")] public string LastName { get; set; }
        [JsonProperty("prefab")] public string Prefab { get; set; }
        [JsonProperty("spawnX")] public int SpawnX { get; set; }
        [JsonProperty("spawnY")] public int SpawnY { get; set; }
        [JsonProperty("spawnZ")] public int SpawnZ { get; set; }
        [JsonProperty("spawnRotation")] public int SpawnRotation { get; set; }
        [JsonProperty("characterPrompt")] public string CharacterPrompt { get; set; }
        [JsonProperty("tools")] public NpcToolDto[] Tools { get; set; }
        [JsonProperty("createdAt")] public string CreatedAt { get; set; }
        [JsonProperty("updatedAt")] public string UpdatedAt { get; set; }
    }
}