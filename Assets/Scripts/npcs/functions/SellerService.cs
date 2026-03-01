using UnityEngine;

namespace DefaultNamespace.npcs.functions
{
    public class SellerService : MonoBehaviour, ISeller
    {
        public virtual string giveItem(string name)
        {
            throw new System.NotImplementedException();
        }

        public virtual string sellItem(string name, int price)
        {
            throw new System.NotImplementedException();
        }

        public virtual string listItems()
        {
            throw new System.NotImplementedException();
        }
    }
}