using io;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NpcUi : BaseUI
{
    [SerializeField] TextMeshProUGUI npcNameMesh;
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
        npcResponseTransform.gameObject.SetActive(false);
        this.gameObject.SetActive(true);
    }

    public override void CloseUI()
    {
        base.CloseUI();
        playerRequestBtn.onClick.RemoveListener(SubmitRequest);
        playerRequestTransform.gameObject.SetActive(false);
    }

    private void CloseNpcUi()
    {
        trackingEntity = null;
        this.gameObject.SetActive(false);
        this.sessionId = null;
    }

    public void Init(ref NpcEntity entity)
    {
        trackingEntity = entity;
        this.OpenUI(null);
    }

    private async void SubmitRequest(string message)
    {
        if (npcResponseText != null)
        {
            npcResponseText.SetText("");
        }
        
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
                string result = trackingEntity.functionManager.processFunction(streamingResponse.ToolName, streamingResponse.Parameters);
                SubmitRequest("TOOL CALL '"+streamingResponse.ToolName+"' RESPONSE : "+result);
            } else if ("text".Equals(streamingResponse.Type))
            {
                fullResponse += streamingResponse.Content;
                npcResponseTransform.gameObject.SetActive(true);
                npcResponseText.SetText(fullResponse);
            }
            else if ("done".Equals(streamingResponse.Type))
            {
                Debug.Log("End of response stream for NPC " + trackingEntity.Name);
            }
            else if ("close".Equals(streamingResponse.Type) || streamingResponse.Closed)
            {
                // Wait 10s before closing the UI to allow the player to read the final response
                CloseUI();
                Invoke(nameof(CloseNpcUi), 6.0f);
            }
            else
            {
                Debug.LogError("Received invalid streaming response: " + response);
            }

        });
        
        Debug.Log("Full NPC response: " + fullResponse);
    }

    private async void SubmitRequest()
    {
        Debug.Log("Send to the API: npcid: " + trackingEntity.UUID + " data: " + playerRequestInputField.text);

        string message = playerRequestInputField.text;
        playerRequestInputField.text = "";
        
        SubmitRequest(message);
    }
}
