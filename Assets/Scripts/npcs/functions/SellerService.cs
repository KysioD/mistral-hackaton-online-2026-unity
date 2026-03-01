using System.Linq;
using npcs;
using npcs.dto;
using UnityEngine;

namespace DefaultNamespace.npcs.functions
{
    public class SellerService : MonoBehaviour, ISeller
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
            return "GIVEN ITEM : "+name;
        }

        public virtual string sellItem(string name, int price)
        {
            Debug.Log("SELL ITEM : "+name+" for "+price);
            playerEntity.TransferGoldTo(npcEntity, price);
            return "SOLD ITEM : "+name+" for "+price;
        }

        public virtual string listItems()
        {
            return "Available items:\n" + string.Join("\n", availableItems.Select(item => item.ToString()));
        }
        
    }
}