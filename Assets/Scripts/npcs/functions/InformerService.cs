using System.Collections.Generic;
using System.Linq;
using npcs;
using npcs.dto;
using UnityEngine;

namespace DefaultNamespace.npcs.functions
{
    public class InformerService : MonoBehaviour, IInformer, INpcFunction
    {
        [SerializeField] protected InfoDto[] availableInfos;

        protected PlayerEntity playerEntity;
        protected NpcEntity npcEntity;

        private void Start()
        {
            playerEntity = FindFirstObjectByType<PlayerController>().entity;
            npcEntity = GetComponent<NpcBehavior>().GetNpcEntity();
        }
        
        public string listInfo()
        {
            return "Available infos:\n" + string.Join("\n", availableInfos.Select(info => info.ToString()));
        }

        public string sellInfo(string name, int price)
        {
            Debug.Log($"SELL INFO {name} for {price}");
            playerEntity.TransferGoldTo(npcEntity, price);
            return $"SOLD INFO {name} for {price}";
        }

        public string buyInfo(int price)
        {
            Debug.Log($"BUY INFO for {price}");
            npcEntity.TransferGoldTo(playerEntity, price);
            return $"BOUGHT INFO for {price}";
        }

        public string processFunction(string functionName, IDictionary<string, string> args)
        {
            switch (functionName)
            {
                case "list_info":
                    return listInfo();
                case "sell_info":
                    if (args.TryGetValue("name", out var itemName) 
                        && args.TryGetValue("price", out var priceStr) 
                        && int.TryParse(priceStr, out var price))
                    {
                        return sellInfo(itemName, price);
                    }
                    return "Error: Missing or invalid arguments 'name' and 'price'";
                case "buy_info":
                    if (args.TryGetValue("price", out var buyPriceStr) 
                        && int.TryParse(buyPriceStr, out var buyPrice))
                    {
                        return buyInfo(buyPrice);
                    }
                    return "Error: Missing or invalid arguments 'price'";
                default:
                    return $"Error: Unknown function '{functionName}'";
            }
        }

        public List<string> FunctionsList()
        {
            return new List<string> { "list_info", "sell_info", "buy_info" };
        }
    }
}