using System.Collections.Generic;
using System.Threading.Tasks;
using io.dto;

namespace io
{
    public class NpcApiService
    {
        
        public static NpcApiService Instance { get; } = new NpcApiService();
        
        private NpcApiService()
        {
            // Private constructor to prevent instantiation of the singleton class
        }
        
        // Method to fetch NPC data from the API
        public async Task<PaginatedNpcsEntity> ListNpcs(int page, int perPage)
        {
            string endpoint = "npcs";

            return await GenericHttpService.Instance.GetAsync<PaginatedNpcsEntity>(endpoint, new Dictionary<string, string>
            {
                { "page", page.ToString() },
                { "perPage", perPage.ToString() }
            }
            );
        }

        public async Task<NpcDto> GetNpcAsync(string id)
        {
            string endpint = "npcs/{id}".Replace("{id}", id);
            
            return await GenericHttpService.Instance.GetAsync<NpcDto>(endpint);
        }

        public async Task StreamNpcTalk(string npcId, string playerMessage, string sessionId, System.Action<string> onChunkReceived, bool voiceEnabled = false, string clientId = null, System.Threading.CancellationToken cancellationToken = default)
        {
            string endpoint = $"npcs/{npcId}/talk";
            await GenericHttpService.Instance.StreamPostAsync(endpoint, playerMessage, sessionId, onChunkReceived, voiceEnabled, clientId, cancellationToken);
        }
    }
}