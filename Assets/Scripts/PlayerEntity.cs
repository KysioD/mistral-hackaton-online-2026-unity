using System.Collections;
using System.Collections.Generic;

public class PlayerEntity : AbstractEntity
{
    public int MagicLevel { get; set; } = 0;

    public IDictionary Inventory { get; set; } = new Dictionary<string, int>()
    {
            {"Bread", 1}
        };
    
    public void HasItem(string item)
    {
        Inventory.Contains(item);
    }

    public void AddToInventory(string item, int quantity)
    {
        if (Inventory.Contains(item))
        {
            Inventory[item] = (int)Inventory[item] + quantity;
        }
        else
        {
            Inventory.Add(item, quantity);
        }
    }

    public void RemoveFromInventory(string item, int quantity)
    {
        if (Inventory.Contains(item))
        {
            int currentQuantity = (int)Inventory[item];
            int newQuantity = currentQuantity - quantity;
            if (newQuantity <= 0)
            {
                Inventory.Remove(item);
            }
            else
            {
                Inventory[item] = newQuantity;
            }
        }
    }
}