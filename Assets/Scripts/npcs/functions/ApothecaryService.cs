using System;
using System.Collections.Generic;
using npcs.dto;
using Unity.VisualScripting;
using UnityEngine;

namespace DefaultNamespace.npcs.functions
{
    public class ApothecaryService : SellerService, IApothecary, INpcFunction
    {
        
        public string inspectPlayer()
        {
            Debug.Log("Inspecting player...");
            string response = $"Player inspected; Health: {playerEntity.Health}, Statuses: {string.Join(", ", playerEntity.GetStatuses())}";
            return response;
        }

        string ISeller.giveItem(string name)
        {
            return giveItem(name);
        }

        string ISeller.sellItem(string name, int price)
        {
            return sellItem(name, price);
        }

        public string processFunction(string functionName, IDictionary<string, string> args)
        {
            switch (functionName)
            {
                case "list_medicine":
                    return listItems();
                case "give_medicine":
                    if (args.TryGetValue("name", out var name))
                    {
                        return giveItem(name);
                    }
                    return "Error: Missing argument 'name'";
                case "sell_medicine":
                    if (args.TryGetValue("name", out var itemName) 
                        && args.TryGetValue("price", out var priceStr) 
                        && int.TryParse(priceStr, out var price))
                    {
                        return sellItem(itemName, price);
                    }
                    return "Error: Missing or invalid arguments 'name' and 'price'";
                case "inspect_player":
                    return inspectPlayer();
                default:
                    return $"Error: Unknown function '{functionName}'";
            }
        }

        public List<string> FunctionsList()
        {
            return new List<string>
            {
                "list_medicine",
                "give_medicine",
                "sell_medicine",
                "inspect_player"
            };
        }
    }
}