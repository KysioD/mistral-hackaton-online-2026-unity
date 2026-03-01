using UnityEngine;

public class NpcEntity : AbstractEntity
{
    public string UUID { get; set; } = "GetFromRestToo";
    public string Name { get; set; } = "No Name";
    public string Prefab { get; set; } //TODO: use to load the 3D model
    public Vector4 SpawnCoords { get; set; } = Vector4.zero;
}