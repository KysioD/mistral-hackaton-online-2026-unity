using System.Collections.Generic;
using npcs;
using ui;
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
            return $"Player has {playerEntity.Gold} coins and has the following items : {string.Join(", ", playerEntity.Inventory)}";
        }

        /*
         * The trader by an item to the player
         */
        public string BuyItem(string name, int price)
        {
            Debug.Log($"Buying item {name} for {price} coins");
            
            if (!playerEntity.Inventory.Contains(name))
            {
                return $"Player does not have {name} in inventory";
            }
            
            npcEntity.TransferGoldTo(playerEntity, price);
            playerEntity.RemoveFromInventory(name, 1);
            ToastNotificationService.Show($"Sold : {name} (+{price} or)");
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