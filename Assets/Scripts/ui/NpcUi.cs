using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NpcDialogueController : BaseUI
{
    [SerializeField] TextMeshProUGUI npcNameMesh;
    [SerializeField] TextMeshProUGUI npcGoldMesh;
    [SerializeField] Transform playerRequestTransform;
    [SerializeField] Button playerRequestBtn;
    [SerializeField] TMP_InputField playerRequestInputField;
    [SerializeField] Transform npcResponseTransform;

    private NpcEntity trackingEntity;

    public override void OpenUI(System.Action closeCb)
    {
        base.OpenUI(closeCb);

        npcNameMesh.SetText(trackingEntity.Name);
        playerRequestBtn.onClick.AddListener(SubmitRequest);
        playerRequestTransform.gameObject.SetActive(true);
        npcResponseTransform.gameObject.SetActive(true);
        this.gameObject.SetActive(true);
    }

    public override void CloseUI()
    {
        base.CloseUI();
        trackingEntity = null;
        this.gameObject.SetActive(false);
        playerRequestBtn.onClick.RemoveListener(SubmitRequest);
    }

    void FixedUpdate()
    {
        if (trackingEntity == null) return;
        npcGoldMesh.SetText("Gold: " + trackingEntity.Gold);
    }

    public void Init(ref NpcEntity entity)
    {
        trackingEntity = entity;
        this.OpenUI(null);
    }

    private void SubmitRequest()
    {
        Debug.Log("Send to the API: npcid: " + trackingEntity.UUID + " data: " + playerRequestInputField.text);
    }
}
