using System.Collections.Generic;
using System.Threading.Tasks;
using io;
using io.dto;
using npcs;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DefaultNamespace
{
    public class AppLoader : MonoBehaviour
    {
        [SerializeField] private Transform npcContainer;
        
        private const string GameSceneName = "GameScene";
        
        private async void Start()
        {
            Debug.Log("Loading app");
            await LoadNpcs();
            Debug.Log("App loaded");
        }

        private async Task<PaginatedNpcsEntity> LoadNpcs()
        {
            int page = 1;
            int perPage = 10;
            PaginatedNpcsEntity paginatedNpcsEntity = await NpcApiService.Instance.ListNpcs(page, perPage);
            
            Debug.LogError($"Loaded {paginatedNpcsEntity.Data.Length} NPCs");
            for (int i = 0; i < paginatedNpcsEntity.Data.Length; i++)
            {
                NpcDto npc = paginatedNpcsEntity.Data[i];

                NpcEntity npcEntity = new NpcEntity();
                npcEntity.UUID = npc.Id;
                npcEntity.Name = npc.FirstName + " " + npc.LastName;
                npcEntity.Prefab = npc.Prefab;
                npcEntity.SpawnCoords = new Vector4(npc.SpawnX, npc.SpawnY, npc.SpawnZ, npc.SpawnRotation);

                Debug.Log($"Loading prefab Prefabs/{npcEntity.Prefab}");
                GameObject npcPrefab = Resources.Load<GameObject>($"Prefabs/{npcEntity.Prefab}");
                    if (npcPrefab != null)
                    {
                        GameObject npcInstance = Instantiate(npcPrefab, npcContainer);
                        NpcBehavior npcBehavior = npcInstance.GetComponent<NpcBehavior>();
                        npcBehavior.Init(npcEntity);
                    }
                    else
                    {
                        Debug.LogError($"Failed to load prefab for NPC {npcEntity.Name} (ID: {npcEntity.UUID})");
                    }
                
                
                Debug.Log($"NPC {i + 1}: {npc.FirstName} {npc.LastName} (ID: {npc.Id})");
            }

            return paginatedNpcsEntity;
        }

        private async Task LoadGameSceneAsync(string sceneName)
        {
            Debug.Log("Loading game scene...");
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = false;

            while (asyncLoad.progress < 0.9f)
            {
                await Task.Yield();
            }
            
            Debug.Log("Game scene loaded, activating...");
            asyncLoad.allowSceneActivation = true;
        }
    }
}