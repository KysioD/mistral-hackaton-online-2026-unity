using DefaultNamespace.npcs.functions;
using TMPro;
using UnityEngine;

namespace npcs
{
    public class NpcBehavior : MonoBehaviour
    {
        [SerializeField] private TextMeshPro entityNameMesh;

        private NpcEntity entity;
        private Camera playerCam;

        private void Awake()
        {
            playerCam = FindFirstObjectByType<Camera>();
        }

        public void Init(NpcEntity npcEntity)
        {
            entity = npcEntity;
            npcEntity.functionManager = this.GetComponent<INpcFunction>();
            Spawn();
        }

        void Update()
        {
            entityNameMesh.transform.forward = playerCam.transform.forward;
        }

        public void InteractWith()
        {
        }

        void Spawn()
        {
            this.gameObject.SetActive(true);
            Vector4 spawnCoords = entity.SpawnCoords;
            this.transform.position = new Vector3(spawnCoords.x, spawnCoords.y, spawnCoords.z);
            this.transform.localRotation = Quaternion.Euler(new Vector3(0.0f, spawnCoords.w, 0.0f));
            entityNameMesh.SetText(entity.Name);
        }

        public ref NpcEntity GetNpcEntity()
        {
            return ref entity;
        }

        public void Talk(string message)
        {
        }
    }
}
