using TMPro;
using UnityEngine;

public class PlayerHudController : MonoBehaviour
{
    //[SerializeField] TextMeshProUGUI healthMesh;
    [SerializeField] TextMeshProUGUI goldMesh;
    //[SerializeField] TextMeshProUGUI magicMesh;
    [SerializeField] TextMeshProUGUI playeItems;
    [SerializeField] TextMeshProUGUI interactTooltip;

    void Awake()
    {
        this.gameObject.SetActive(true);
    }

    //public void SetHealth(float health)
    //{
    //    healthMesh.SetText("Health: " + health);
    //}

    public void SetGold(float gold)
    {
        goldMesh.SetText(""+gold);
    }
    
    public void setItems(string items)
    {
        playeItems.SetText(items);
    }

    public void setInteractTooltipEnabled(bool enabled)
    {
        interactTooltip.gameObject.SetActive(enabled);
    }

    //public void SetMagic(int magic)
    //{
    //    magicMesh.SetText("Magic: " + magic);
    //}
}
