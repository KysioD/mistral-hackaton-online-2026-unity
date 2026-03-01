using DefaultNamespace.npcs.functions;
using UnityEngine;

public class NpcEntity : AbstractEntity
{
    public string UUID { get; set; } = "80b808f6-dafb-4be3-afcd-9514b7ed8e5b";
    public string Name { get; set; } = "Mao Mao";
    public string Prefab { get; set; } //TODO: use to load the 3D model
    public Vector4 SpawnCoords { get; set; } = Vector4.zero;
    public INpcFunction functionManager { get; set; }
}