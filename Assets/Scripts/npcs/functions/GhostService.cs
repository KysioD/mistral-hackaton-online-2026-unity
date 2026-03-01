using System.Collections.Generic;
using npcs;
using UnityEngine;

namespace DefaultNamespace.npcs.functions
{
    public class GhostService : MonoBehaviour, INpcFunction
    {
        protected PlayerEntity playerEntity;
        protected NpcEntity npcEntity;

        private void Start()
        {
            playerEntity = FindFirstObjectByType<PlayerController>().entity;
            npcEntity = GetComponent<NpcBehavior>().GetNpcEntity();
        }
        
        string Fear()
        {
            Debug.Log("Fear the player");
            return "You've scared the player";
        }

        string ListPlayerCoins()
        {
            return $"Player has {playerEntity.Gold} coins";
        }

        string StealCoins(int amount)
        {
            Debug.Log("Steal " + amount + " coins from the player");
            playerEntity.TransferGoldTo(npcEntity, amount);
            return $"You stole {amount} coins from the player";
        }
        
        public string processFunction(string functionName, IDictionary<string, string> args)
        {
            switch (functionName)
            {
                case "fear":
                    return Fear();
                case "list_player_coins":
                    return ListPlayerCoins();
                case "steal_coins":
                    if (args.TryGetValue("amount", out string amountStr) && int.TryParse(amountStr, out int amount))
                    {
                        return StealCoins(amount);
                    }
                    else
                    {
                        return "Invalid or missing 'amount' argument for steal_coins function.";
                    }
                default:
                    return $"Unknown function: {functionName}";
            }
        }
    }
}