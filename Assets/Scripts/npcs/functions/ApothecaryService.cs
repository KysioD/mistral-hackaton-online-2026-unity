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

        public override ItemDto[] getAvailableItems()
        {
            return new ItemDto[]
            {
                new ItemDto { Name = "Health Potion", Description = "Restores 50 HP", Price = 10 },
                new ItemDto { Name = "Mana Potion", Description = "Restores 30 MP", Price = 15 },
                new ItemDto { Name = "Antidote", Description = "Cures poison", Price = 20 },
                new ItemDto { Name = "Stamina Elixir", Description = "Restores 20 stamina", Price = 25 }
            };
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
    }
}