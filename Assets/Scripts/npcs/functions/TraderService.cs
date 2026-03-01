using System.Collections.Generic;
using npcs;
using UnityEngine;

namespace DefaultNamespace.npcs.functions
{
    public class TraderService : MonoBehaviour, ITrader, INpcFunction
    {
        protected PlayerEntity playerEntity;
        protected NpcEntity npcEntity;
        
        private void Start()
        {
            playerEntity = FindFirstObjectByType<PlayerController>().entity;
            npcEntity = GetComponent<NpcBehavior>().GetNpcEntity();
        }
        
        public string ListPlayerItems()
        {
            Debug.Log("Listing player items");
            return $"Player has {playerEntity.Gold} coins and has no items."; // TODO : Add simple item management system to player
        }

        public string BuyItem(string name, int price)
        {
            Debug.Log($"Buying item {name} for {price} coins");
            npcEntity.TransferGoldTo(playerEntity, price);
            return $"Bought {name} for {price} coins";
        }

        public string processFunction(string functionName, IDictionary<string, string> args)
        {
            switch (functionName)
            {
                case "listPlayerItems":
                    return ListPlayerItems();
                case "buyItem":
                    if (args.TryGetValue("name", out string name) && args.TryGetValue("price", out string priceStr) && int.TryParse(priceStr, out int price))
                    {
                        return BuyItem(name, price);
                    }
                    else
                    {
                        return "Invalid arguments for buyItem function";
                    }
                default:
                    return $"Function {functionName} not found in TraderService";
            }
        }
    }
}