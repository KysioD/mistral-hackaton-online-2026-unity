using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.npcs.functions
{
    public class ApothecaryService : SellerService, IApothecary, INpcFunction
    {
        
        // Name, description and price of the available medications
        private readonly string[] medications = {
            "Health Potion - Restores 50 HP - 10 gold",
            "Mana Potion - Restores 30 MP - 15 gold",
            "Antidote - Cures poison - 20 gold",
            "Stamina Elixir - Restores 20 stamina - 25 gold"
        };
        
        public string inspectPlayer()
        {
            Debug.Log("Inspecting player...");
            return "Player inspected. He is poisoned";
        }
        
        public override string listItems()
        {
            return string.Join("\n", medications);
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
    }
}