using System.Collections.Generic;
using System.Linq;
using npcs;
using npcs.dto;
using ui;
using UnityEngine;

namespace DefaultNamespace.npcs.functions
{
    public class SellerService : MonoBehaviour, ISeller, INpcFunction
    {
        [SerializeField] protected ItemDto[] availableItems;
        
        protected PlayerEntity playerEntity;
        protected NpcEntity npcEntity;

        private void Start()
        {
            playerEntity = FindFirstObjectByType<PlayerController>().entity;
                npcEntity = GetComponent<NpcBehavior>().GetNpcEntity();
        }

        public virtual string giveItem(string name)
        {
            Debug.Log("GIVE ITEM : "+name);
            playerEntity.AddToInventory(name, 1);
            ToastNotificationService.Show($"Received item : {name}");
            return "GIVEN ITEM : "+name;
        }

        /*
         * Seller sells an item to the player.
         */
        public virtual string sellItem(string name, int price)
        {
            Debug.Log("SELL ITEM : "+name+" for "+price);
            playerEntity.TransferGoldTo(npcEntity, price);
            playerEntity.AddToInventory(name, 1);
            ToastNotificationService.Show($"Bought : {name} (-{price} or)");
            return "SOLD ITEM : "+name+" for "+price;
        }

        public virtual string listItems()
        {
            return "Available items:\n" + string.Join("\n", availableItems.Select(item => item.ToString()));
        }

        public string processFunction(string functionName, IDictionary<string, string> args)
        {
            
            switch (functionName)
            {
                case "list_drinks":
                    return listItems();
                case "give_drink":
                    if (args.TryGetValue("name", out var name))
                    {
                        return giveItem(name);
                    }
                    return "Error: Missing argument 'name'";
                case "sell_drink":
                    if (args.TryGetValue("name", out var itemName) 
                        && args.TryGetValue("price", out var priceStr) 
                        && int.TryParse(priceStr, out var price))
                    {
                        return sellItem(itemName, price);
                    }
                    return "Error: Missing or invalid arguments 'name' and 'price'";
                default:
                    return $"Error: Unknown function '{functionName}'";
            }
        }

        public List<string> FunctionsList()
        {
            return new List<string>
            {
                "list_drinks",
                "give_drink",
                "sell_drink"
            };
        }
    }
}