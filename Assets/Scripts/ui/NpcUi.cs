using io;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NpcUi : BaseUI
{
    [SerializeField] TextMeshProUGUI npcNameMesh;
    [SerializeField] TextMeshProUGUI npcGoldMesh;
    [SerializeField] Transform playerRequestTransform;
    [SerializeField] Button playerRequestBtn;
    [SerializeField] TMP_InputField playerRequestInputField;
    [SerializeField] Transform npcResponseTransform;
    [SerializeField] TextMeshProUGUI npcResponseText;

    private NpcEntity trackingEntity;
    private string sessionId;

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
        this.sessionId = null;
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

    private async void SubmitRequest()
    {
        Debug.Log("Send to the API: npcid: " + trackingEntity.UUID + " data: " + playerRequestInputField.text);

        string message = playerRequestInputField.text;

        npcResponseText.SetText("");
        string fullResponse = "";
        
        await NpcApiService.Instance.StreamNpcTalk(trackingEntity.UUID, message, sessionId,response =>
        {
            Debug.Log($"NPC {trackingEntity.Name} says: {response}");
            
            // Map the response to the LLMStreamingResponse dto
            LLMStreamingResponse streamingResponse = JsonConvert.DeserializeObject<LLMStreamingResponse>(response);
            
            if (streamingResponse == null)
            {
                Debug.LogError("Failed to deserialize streaming response: " + response);
                return;
            }
            
            if (streamingResponse.SessionId != null)
            {
                this.sessionId = streamingResponse.SessionId;
            }
            
            if ("tool_call".Equals(streamingResponse.Type))
            {
                trackingEntity.functionManager.processFunction(streamingResponse.ToolName, streamingResponse.Parameters);
            } else if ("text".Equals(streamingResponse.Type))
            {
                fullResponse += streamingResponse.Content;
                npcResponseText.SetText(fullResponse);
            }
            else
            {
                Debug.LogError("Received invalid streaming response: " + response);
            }

        });
        
        Debug.Log("Full NPC response: " + fullResponse);

    }
}
