using TMPro;
using UnityEngine;

public class PlayerHudController : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI healthMesh;
    [SerializeField] TextMeshProUGUI goldMesh;
    [SerializeField] TextMeshProUGUI magicMesh;

    void Awake()
    {
        this.gameObject.SetActive(true);
    }

    public void SetHealth(float health)
    {
        healthMesh.SetText("Health: " + health);
    }

    public void SetGold(float gold)
    {
        goldMesh.SetText("Gold: " + gold);
    }

    public void SetMagic(int magic)
    {
        magicMesh.SetText("Magic: " + magic);
    }
}
