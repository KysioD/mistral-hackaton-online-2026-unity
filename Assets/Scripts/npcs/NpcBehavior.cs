using TMPro;
using UnityEngine;

public class NpcBehavior : MonoBehaviour
{
    [SerializeField] private TextMeshPro entityNameMesh;

    private NpcEntity entity;

    void Start()
    {
        entity = new NpcEntity();
        Spawn();
    }

    void Update()
    {

    }

    public void InteractWith()
    {

    }

    void Spawn()
    {
        this.gameObject.SetActive(true);
        Vector4 spawnCoords = entity.SpawnCoords;
        //this.transform.position = new Vector3(spawnCoords.x, spawnCoords.y, spawnCoords.z);
        this.transform.localRotation = Quaternion.Euler(new Vector3(0.0f, spawnCoords.w, 0.0f));
        entityNameMesh.SetText(entity.Name);

    }

    public ref NpcEntity GetNpcEntity()
    {
        return ref entity;
    }
}
