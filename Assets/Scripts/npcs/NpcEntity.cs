using System.Collections.Generic;
using DefaultNamespace.npcs.functions;
using UnityEngine;

public class NpcEntity : AbstractEntity
{
    public string UUID { get; set; }
    public string Name { get; set; }
    public string Prefab { get; set; }
    public Vector4 SpawnCoords { get; set; } = Vector4.zero;
    public List<INpcFunction> functionManager { get; set; }
}