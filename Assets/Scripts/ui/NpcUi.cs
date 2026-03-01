using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using audio;
using DefaultNamespace.npcs.functions;
using io;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NpcUi : BaseUI
{
    private const string AudioButtonTextStart = "Start mic capture";
    private const string AudioButtonTextStop = "Stop mic capture";

    [SerializeField] TextMeshProUGUI npcNameMesh;
    [SerializeField] Transform playerRequestTransform;
    [SerializeField] Button playerRequestBtn;
    [SerializeField] TMP_InputField playerRequestInputField;
    [SerializeField] Transform npcResponseTransform;
    [SerializeField] TextMeshProUGUI npcResponseText;
    [SerializeField] private VoxtralAudioCapture voxtralCapture;
    [SerializeField] private Button voiceBtn;
    [SerializeField] private TMP_Dropdown micDropdown;

    [Header("Voice (ElevenLabs)")]
    [Tooltip("Toggle in code to enable NPC text-to-speech via ElevenLabs WebSocket.")]
    [SerializeField] private bool voiceEnabled = false;
    [SerializeField] private NpcAudioPlayer npcAudioPlayer;

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

    // Cancelled on CloseUI to abort any in-flight HTTP stream immediately.
    private CancellationTokenSource _streamCts;

    // clientId assigned by the /npc-audio WebSocket gateway; used to route audio
    // chunks back to this client over the dedicated WebSocket connection.
    private string _npcAudioClientId;

    // ── Public API ────────────────────────────────────────────────────────────

    public void Init(ref NpcEntity entity)
    {
        // If switching to a different NPC, clear the previous session.
        if (trackingEntity != null && trackingEntity.UUID != entity.UUID)
            sessionId = null;

        CancelPendingDialogueClose();

        trackingEntity = entity;
        OpenUI(null);
    }

    public void AudioButtonSetListening(bool isListening)
    {
        if (voiceBtn == null) return;
        voiceBtn.GetComponentInChildren<TextMeshProUGUI>().SetText(
            isListening ? AudioButtonTextStop : AudioButtonTextStart);
    }

    // ── UI lifecycle ──────────────────────────────────────────────────────────

    public override void OpenUI(System.Action closeCb)
    {
        CancelPendingDialogueClose();
        _conversationId++;

        // Cancel any lingering HTTP stream from the previous conversation.
        _streamCts?.Cancel();
        _streamCts?.Dispose();
        _streamCts = new CancellationTokenSource();

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

        // Connect to the NPC audio WebSocket so the server can push ElevenLabs
        // audio chunks directly to this client in parallel with the text stream.
        if (voiceEnabled)
            ConnectNpcAudioWebSocket();
    }

    public override void CloseUI()
    {
        if (!Opened) return;

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
            AudioButtonSetListening(false);
            _isRecording = false;
        }

        if (npcAudioPlayer != null)
            npcAudioPlayer.StopAndClear();

        // Force-close the in-flight HTTP stream (ElevenLabs 20s timeout workaround).
        _streamCts?.Cancel();

        // Disconnect the audio WebSocket and unsubscribe its events.
        if (voiceEnabled)
            DisconnectNpcAudioWebSocket();

        _closeDialogueCoroutine = StartCoroutine(CloseDialogueAfterDelay(dialogueCloseDelay));
    }

    // ── NPC audio WebSocket helpers ───────────────────────────────────────────

    private void ConnectNpcAudioWebSocket()
    {
        _npcAudioClientId = null;

        // Always unsubscribe first to avoid double-registration.
        NpcAudioWebSocketService.Instance.OnClientIdReceived -= OnNpcAudioClientId;
        NpcAudioWebSocketService.Instance.OnAudioChunkReceived -= OnNpcAudioChunk;

        NpcAudioWebSocketService.Instance.OnClientIdReceived += OnNpcAudioClientId;
        NpcAudioWebSocketService.Instance.OnAudioChunkReceived += OnNpcAudioChunk;

        // Fire-and-forget: the clientId will arrive asynchronously via the event.
        // If it hasn't arrived by the time the player submits a message, the
        // HTTP request is sent without clientId and the backend falls back to
        // streaming audio inline in the HTTP response.
        _ = NpcAudioWebSocketService.Instance.ConnectAsync();
    }

    private void DisconnectNpcAudioWebSocket()
    {
        NpcAudioWebSocketService.Instance.OnClientIdReceived -= OnNpcAudioClientId;
        NpcAudioWebSocketService.Instance.OnAudioChunkReceived -= OnNpcAudioChunk;

        _npcAudioClientId = null;

        _ = NpcAudioWebSocketService.Instance.DisconnectAsync();
    }

    // Fired from background thread — just store the value (string assignment is atomic).
    private void OnNpcAudioClientId(string clientId)
    {
        _npcAudioClientId = clientId;
        Debug.Log($"[NpcUi] ✅ clientId received: {clientId}");
    }

    // Fired from background thread — AccumulateChunk uses a ConcurrentQueue so it is thread-safe.
    private void OnNpcAudioChunk(string base64Mp3)
    {
        if (npcAudioPlayer != null)
            npcAudioPlayer.AccumulateChunk(base64Mp3);
    }

    // ── Dialogue close helpers ────────────────────────────────────────────────

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

    // ── Voxtral (speech-to-text) ──────────────────────────────────────────────

    private void ToggleVoiceCapture()
    {
        if (_isRecording)
        {
            FlushTranscript();
        }
        else
        {
            voxtralCapture.StartCapturing();
            AudioButtonSetListening(true);
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
        _transcriptBuffer = "";
        playerRequestInputField.text = "";

        if (_isRecording)
        {
            voxtralCapture.StopCapturing();
            AudioButtonSetListening(false);
            _isRecording = false;
        }

        if (string.IsNullOrWhiteSpace(message)) return;

        SubmitRequest(message);
    }

    // ── NPC talk (HTTP streaming) ─────────────────────────────────────────────

    private async void SubmitRequest(string message)
    {
        if (trackingEntity == null) return;

        int conversationId = _conversationId;
        string entityUUID = trackingEntity.UUID;
        string entityName = trackingEntity.Name;
        List<INpcFunction> functionManagers = trackingEntity.functionManager;
        string requestSessionId = sessionId;

        // Snapshot clientId now: the WS event may update _npcAudioClientId at any time.
        string audioClientId = voiceEnabled ? _npcAudioClientId : null;

        if (npcResponseText != null)
            npcResponseText.SetText("");

        string fullResponse = "";

        var streamToken = _streamCts?.Token ?? CancellationToken.None;

        await NpcApiService.Instance.StreamNpcTalk(
            entityUUID, message, requestSessionId,
            response =>
            {
                if (_conversationId != conversationId) return;

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
                    var selectedFunctionManager = functionManagers.Find(fn => fn.FunctionsList().Contains(streamingResponse.ToolName));
                    if (selectedFunctionManager == null)
                    {
                        Debug.LogError("No function manager found for tool: " + streamingResponse.ToolName);
                        return;
                    }
                    string result = selectedFunctionManager.processFunction(streamingResponse.ToolName, streamingResponse.Parameters);
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
                    // The backend awaits audioListenerDone before sending "done",
                    // so all WS audio chunks have already arrived on the WS connection.
                    // SignalDone() tells NpcAudioPlayer to play (or preload) any
                    // remaining buffered chunks.
                    if (voiceEnabled && npcAudioPlayer != null)
                        npcAudioPlayer.SignalDone();
                }
                else if ("close".Equals(streamingResponse.Type) || streamingResponse.Closed)
                {
                    CloseUI();
                }
                else
                {
                    Debug.LogError("Received invalid streaming response: " + response);
                }
            },
            voiceEnabled,
            audioClientId,
            streamToken);

        Debug.Log("Full NPC response: " + fullResponse);
    }

    private void SubmitRequest()
    {
        if (trackingEntity == null) return;

        string message = playerRequestInputField.text;
        playerRequestInputField.text = "";
        SubmitRequest(message);
    }
}
