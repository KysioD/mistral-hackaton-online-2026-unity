using System.IO;
using audio;
using io;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
    [SerializeField] private VoxtralAudioCapture voxtralCapture;
    [SerializeField] private Button voiceBtn;
    [SerializeField] private TMP_Dropdown micDropdown;

    [SerializeField] private float maxRecordingSeconds = 30f;

    private NpcEntity trackingEntity;
    private string sessionId;
    private bool _isRecording;
    private string _transcriptBuffer = "";
    private Coroutine _debounceCoroutine;
    private Coroutine _recordingTimeoutCoroutine;

    public override void OpenUI(System.Action closeCb)
    {
        base.OpenUI(closeCb);

        npcNameMesh.SetText(trackingEntity.Name);
        playerRequestBtn.onClick.RemoveListener(SubmitRequest);
        playerRequestBtn.onClick.AddListener(SubmitRequest);
        playerRequestTransform.gameObject.SetActive(true);
        npcResponseTransform.gameObject.SetActive(false);
        this.gameObject.SetActive(true);
        voxtralCapture.OnTranscriptReceived -= HandleVoxtralResponse;
        voxtralCapture.OnTranscriptReceived += HandleVoxtralResponse;
        voxtralCapture.SetMicDropdown(micDropdown);
        _transcriptBuffer = "";

        if (voiceBtn != null)
        {
            voiceBtn.onClick.RemoveListener(ToggleVoiceCapture);
            voiceBtn.onClick.AddListener(ToggleVoiceCapture);
        }
    }

    public override void CloseUI()
    {
        base.CloseUI();
        playerRequestBtn.onClick.RemoveListener(SubmitRequest);
        playerRequestTransform.gameObject.SetActive(false);
        voxtralCapture.OnTranscriptReceived -= HandleVoxtralResponse;

        if (voiceBtn != null)
            voiceBtn.onClick.RemoveListener(ToggleVoiceCapture);

        if (_debounceCoroutine != null)
        {
            StopCoroutine(_debounceCoroutine);
            _debounceCoroutine = null;
        }

        if (_recordingTimeoutCoroutine != null)
        {
            StopCoroutine(_recordingTimeoutCoroutine);
            _recordingTimeoutCoroutine = null;
        }

        _transcriptBuffer = "";

        if (_isRecording)
        {
            voxtralCapture.StopCapturing();
            _isRecording = false;
        }
    }

    private void ToggleVoiceCapture()
    {
        if (_isRecording)
        {
            FlushTranscript();
        }
        else
        {
            voxtralCapture.StartCapturing();
            _isRecording = true;
            _recordingTimeoutCoroutine = StartCoroutine(RecordingTimeoutCoroutine());
        }
    }

    private System.Collections.IEnumerator RecordingTimeoutCoroutine()
    {
        yield return new WaitForSeconds(maxRecordingSeconds);
        Debug.LogWarning($"[Voxtral] Timeout ({maxRecordingSeconds}s) — arrêt automatique de l'enregistrement.");
        FlushTranscript();
    }

    private void CloseNpcUi()
    {
        trackingEntity = null;
        this.gameObject.SetActive(false);
        this.sessionId = null;
    }

    private void HandleVoxtralResponse(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;

        using var reader = new JsonTextReader(new StringReader(json));
        reader.SupportMultipleContent = true;

        while (reader.Read())
        {
            var obj = JObject.Load(reader);
            string evt = obj["event"]?.Value<string>();

            if (evt == "session_ready") continue;

            if (evt == "text_response")
            {
                string text = obj["data"]?["text"]?.Value<string>();
                bool isFinal = obj["data"]?["isFinal"]?.Value<bool>() ?? false;

                if (string.IsNullOrEmpty(text)) continue;

                _transcriptBuffer += text;
                playerRequestInputField.text = _transcriptBuffer;

                if (isFinal)
                {
                    if (_debounceCoroutine != null)
                    {
                        StopCoroutine(_debounceCoroutine);
                        _debounceCoroutine = null;
                    }
                    string message = _transcriptBuffer.Trim();
                    _transcriptBuffer = "";
                    playerRequestInputField.text = "";
                    if (!string.IsNullOrWhiteSpace(message))
                        SubmitRequest(message);
                    return;
                }

                if (_debounceCoroutine != null)
                    StopCoroutine(_debounceCoroutine);
                _debounceCoroutine = StartCoroutine(SubmitTranscriptAfterDelay(1.5f));
            }
        }
    }

    private System.Collections.IEnumerator SubmitTranscriptAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        FlushTranscript();
    }

    private void FlushTranscript()
    {
        if (_debounceCoroutine != null)
        {
            StopCoroutine(_debounceCoroutine);
            _debounceCoroutine = null;
        }

        if (_recordingTimeoutCoroutine != null)
        {
            StopCoroutine(_recordingTimeoutCoroutine);
            _recordingTimeoutCoroutine = null;
        }

        string message = _transcriptBuffer.Trim();
        Debug.Log("RECEIVED MESSAGE FROM VOXTRAL: " + message);
        _transcriptBuffer = "";
        playerRequestInputField.text = "";

        if (_isRecording)
        {
            voxtralCapture.StopCapturing();
            _isRecording = false;
        }

        if (string.IsNullOrWhiteSpace(message)) return;

        SubmitRequest(message);
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
            }
            else if ("text".Equals(streamingResponse.Type))
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
