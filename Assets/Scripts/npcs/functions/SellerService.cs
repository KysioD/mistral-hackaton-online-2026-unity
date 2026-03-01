using UnityEngine;

namespace DefaultNamespace.npcs.functions
{
    public class SellerService : MonoBehaviour, ISeller
    {
        public virtual string giveItem(string name)
        {
            Debug.Log("GIVE ITEM : "+name);
            return "GIVEN ITEM : "+name;
        }

        public virtual string sellItem(string name, int price)
        {
            Debug.Log("SELL ITEM : "+name+" for "+price);
            return "SOLD ITEM : "+name+" for "+price;
        }

        public virtual string listItems()
        {
            Debug.Log("LIST ITEMS");
            return "LISTED ITEMS";
        }
    }
}