using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using io;
using TMPro;
using UnityEngine;

namespace audio
{
    public class VoxtralAudioCapture : MonoBehaviour
    {
        [Header("Audio settings")]
        public int sampleRate = 16000;
        public int minChunkSamples = 1600;

        private TMP_Dropdown _micDropdown;
        private const int MicBufferSeconds = 10;

        private AudioClip _micClip;
        private int _lastSamplePos;
        private bool _isCapturing;
        private string _micDevice;

        private readonly ConcurrentQueue<string> _transcriptQueue = new ConcurrentQueue<string>();

        public event Action<string> OnTranscriptReceived;

        private void OnEnable()
        {
            VoxtralWebSocketService.Instance.OnTranscriptReceived += EnqueueTranscript;
        }

        private void OnDisable()
        {
            VoxtralWebSocketService.Instance.OnTranscriptReceived -= EnqueueTranscript;
        }

        public void SetMicDropdown(TMP_Dropdown dropdown)
        {
            _micDropdown = dropdown;
            if (_micDropdown == null) return;

            _micDropdown.ClearOptions();
            var devices = new List<string>(Microphone.devices);
            if (devices.Count == 0)
            {
                _micDropdown.AddOptions(new List<string> { "Aucun micro détecté" });
                _micDropdown.interactable = false;
                return;
            }
            _micDropdown.interactable = true;
            _micDropdown.AddOptions(devices);
        }

        public async void StartCapturing()
        {
            if (_isCapturing) return;
            _isCapturing = true;
            
            var devices = Microphone.devices;
            _micDevice = _micDropdown != null && devices.Length > 0 ? devices[_micDropdown.value]
                        : null;

            _micClip = Microphone.Start(_micDevice, true, MicBufferSeconds, sampleRate);
            _lastSamplePos = 0;

            Debug.Log($"[Voxtral] Capture started — mic: '{_micDevice ?? "default"}' @ {sampleRate} Hz");

            await VoxtralWebSocketService.Instance.ConnectAsync();

            if (!VoxtralWebSocketService.Instance.IsConnected)
            {
                Debug.LogError("[Voxtral] Cannot start capture: WebSocket not connected.");
                _isCapturing = false;
                Microphone.End(_micDevice);
                _micClip = null;
            }
        }

        public async void StopCapturing()
        {
            if (!_isCapturing) return;

            _isCapturing = false;
            Microphone.End(_micDevice);
            _micClip = null;

            await VoxtralWebSocketService.Instance.DisconnectAsync();
        }

        private void Update()
        {
            if (_isCapturing) SendNewSamples();
            ProcessTranscriptQueue();
        }

        private void SendNewSamples()
        {
            if (_micClip == null) return;

            if (!VoxtralWebSocketService.Instance.IsConnected) return;

            int currentPos = Microphone.GetPosition(_micDevice);
            if (currentPos < 0) return;

            int available = currentPos >= _lastSamplePos
                ? currentPos - _lastSamplePos
                : (_micClip.samples - _lastSamplePos) + currentPos;

            if (available < minChunkSamples) return;

            float[] samples = new float[available];
            _micClip.GetData(samples, _lastSamplePos);
            _lastSamplePos = currentPos;

            _ = VoxtralWebSocketService.Instance.SendAudioBytesAsync(FloatToPcm16(samples));
        }

        private void EnqueueTranscript(string transcript)
        {
            _transcriptQueue.Enqueue(transcript);
        }

        private void ProcessTranscriptQueue()
        {
            while (_transcriptQueue.TryDequeue(out string transcript))
                OnTranscriptReceived?.Invoke(transcript);
        }

        private static byte[] FloatToPcm16(float[] samples)
        {
            byte[] pcm = new byte[samples.Length * 2];
            for (int i = 0; i < samples.Length; i++)
            {
                short s = (short)Mathf.Clamp(samples[i] * 32767f, short.MinValue, short.MaxValue);
                pcm[i * 2]     = (byte)(s & 0xFF);
                pcm[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
            }
            return pcm;
        }
    }
}
