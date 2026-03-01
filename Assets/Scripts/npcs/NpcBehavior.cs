using DefaultNamespace.npcs.functions;
using io;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

namespace npcs
{
    public class NpcBehavior : MonoBehaviour
    {
        [SerializeField] private TextMeshPro entityNameMesh;
        private MonoBehaviour functionManagerObject;

        private NpcEntity entity;

        public void Init(NpcEntity npcEntity)
        {
            entity = npcEntity;
            npcEntity.functionManager = this.GetComponent<INpcFunction>();
            Spawn();
        }
        
        /*void Start()
        {
            entity = new NpcEntity();
            entity.functionManager = this.GetComponent<INpcFunction>();
            Spawn();
        }*/

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

        public async void Talk(string message)
        {
        }
    }
}
