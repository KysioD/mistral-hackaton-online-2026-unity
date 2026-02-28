using System.Threading.Tasks;
using io;
using io.dto;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DefaultNamespace
{
    public class AppLoader : MonoBehaviour
    {
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
                Debug.LogError($"NPC {i + 1}: {npc.FirstName} {npc.LastName} (ID: {npc.Id})");
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