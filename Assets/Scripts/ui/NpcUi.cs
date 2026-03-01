using System.Collections;
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

    [SerializeField] private float dialogueCloseDelay = 5.0f;
    [SerializeField] private float maxRecordingSeconds = 30f;

    private NpcEntity trackingEntity;
    private string sessionId;
    private bool _isRecording;
    private string _transcriptBuffer = "";
    private Coroutine _debounceCoroutine;
    private Coroutine _recordingTimeoutCoroutine;
    private Coroutine _closeDialogueCoroutine;

    // Incremented each time a conversation opens, used to discard stale API responses.
    private int _conversationId = 0;

    public void Init(ref NpcEntity entity)
    {
        // If switching to a different NPC, clear the previous session.
        if (trackingEntity != null && trackingEntity.UUID != entity.UUID)
            sessionId = null;

        // Cancel any pending delayed close before reopening.
        CancelPendingDialogueClose();

        trackingEntity = entity;
        OpenUI(null);
    }

    public override void OpenUI(System.Action closeCb)
    {
        // Cancel any pending delayed close (player may be reopening within the 5-second window).
        CancelPendingDialogueClose();
        _conversationId++;

        base.OpenUI(closeCb);

        npcNameMesh.SetText(trackingEntity.Name);

        playerRequestBtn.onClick.RemoveListener(SubmitRequest);
        playerRequestBtn.onClick.AddListener(SubmitRequest);

        playerRequestTransform.gameObject.SetActive(true);
        npcResponseTransform.gameObject.SetActive(false);
        npcResponseText.SetText("");

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
        // Guard: already closed, nothing to do.
        if (!Opened) return;

        // base.CloseUI() sets Opened = false and restores player controls.
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

        // Keep the dialogue panel visible for a few seconds, then fully hide the UI.
        _closeDialogueCoroutine = StartCoroutine(CloseDialogueAfterDelay(dialogueCloseDelay));
    }

    private IEnumerator CloseDialogueAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CloseNpcUi();
    }

    private void CancelPendingDialogueClose()
    {
        if (_closeDialogueCoroutine != null)
        {
            StopCoroutine(_closeDialogueCoroutine);
            _closeDialogueCoroutine = null;
        }
    }

    private void CloseNpcUi()
    {
        _closeDialogueCoroutine = null;
        trackingEntity = null;
        sessionId = null;
        this.gameObject.SetActive(false);
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

    private IEnumerator RecordingTimeoutCoroutine()
    {
        yield return new WaitForSeconds(maxRecordingSeconds);
        Debug.LogWarning($"[Voxtral] Timeout ({maxRecordingSeconds}s) — arrêt automatique de l'enregistrement.");
        FlushTranscript();
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

    private IEnumerator SubmitTranscriptAfterDelay(float delay)
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

    private async void SubmitRequest(string message)
    {
        if (trackingEntity == null) return;

        // Capture conversation-specific values so stale responses from a previous
        // conversation can be detected and discarded.
        int conversationId = _conversationId;
        string entityUUID = trackingEntity.UUID;
        string entityName = trackingEntity.Name;
        var functionManager = trackingEntity.functionManager;
        string requestSessionId = sessionId;

        if (npcResponseText != null)
            npcResponseText.SetText("");

        string fullResponse = "";

        await NpcApiService.Instance.StreamNpcTalk(entityUUID, message, requestSessionId, response =>
        {
            // Discard responses that belong to a previous conversation.
            if (_conversationId != conversationId) return;

            Debug.Log($"NPC {entityName} says: {response}");

            LLMStreamingResponse streamingResponse = JsonConvert.DeserializeObject<LLMStreamingResponse>(response);

            if (streamingResponse == null)
            {
                Debug.LogError("Failed to deserialize streaming response: " + response);
                return;
            }

            if (streamingResponse.SessionId != null)
                sessionId = streamingResponse.SessionId;

            if ("tool_call".Equals(streamingResponse.Type))
            {
                string result = functionManager.processFunction(streamingResponse.ToolName, streamingResponse.Parameters);
                SubmitRequest("TOOL CALL '" + streamingResponse.ToolName + "' RESPONSE : " + result);
            }
            else if ("text".Equals(streamingResponse.Type))
            {
                fullResponse += streamingResponse.Content;
                npcResponseTransform.gameObject.SetActive(true);
                npcResponseText.SetText(fullResponse);
            }
            else if ("done".Equals(streamingResponse.Type))
            {
                Debug.Log("End of response stream for NPC " + entityName);
            }
            else if ("close".Equals(streamingResponse.Type) || streamingResponse.Closed)
            {
                CloseUI();
            }
            else
            {
                Debug.LogError("Received invalid streaming response: " + response);
            }
        });

        Debug.Log("Full NPC response: " + fullResponse);
    }

    private void SubmitRequest()
    {
        if (trackingEntity == null) return;
        Debug.Log("Send to the API: npcid: " + trackingEntity.UUID + " data: " + playerRequestInputField.text);

        string message = playerRequestInputField.text;
        playerRequestInputField.text = "";
        SubmitRequest(message);
    }
}
