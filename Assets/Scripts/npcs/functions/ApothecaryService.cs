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
        
        public void inspectPlayer()
        {
            throw new System.NotImplementedException();
        }
        
        public override string listItems()
        {
            return string.Join("\n", medications);
        }

        public string processFunction(string functionName, IDictionary<string, string> args)
        {
            switch (functionName)
            {
                case "listItems":
                    return listItems();
                case "giveItem":
                    if (args.TryGetValue("name", out var name))
                    {
                        return giveItem(name);
                    }
                    return "Error: Missing argument 'name'";
                case "sellItem":
                    if (args.TryGetValue("name", out var itemName) 
                        && args.TryGetValue("price", out var priceStr) 
                        && int.TryParse(priceStr, out var price))
                    {
                        return sellItem(itemName, price);
                    }
                    return "Error: Missing or invalid arguments 'name' and 'price'";
                case "inspectPlayer":
                    inspectPlayer();
                    return "Player inspected.";
                default:
                    return $"Error: Unknown function '{functionName}'";
            }
        }
    }
}