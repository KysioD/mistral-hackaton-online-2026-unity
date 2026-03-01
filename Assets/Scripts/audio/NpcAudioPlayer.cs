using System;
using System.Collections.Concurrent;
using System.IO;
using NLayer;
using UnityEngine;

namespace audio
{
    /// <summary>
    /// Streams NPC voice audio in real time by decoding each incoming MP3 chunk
    /// to PCM using NLayer, then feeding the samples through a PCMReaderCallback.
    ///
    /// Requires NLayer.dll in Assets/Plugins/ (nuget.org/packages/NLayer).
    /// Backend keeps output_format=mp3_44100_128 — no tier change needed.
    ///
    /// Flow:
    ///   AccumulateChunk()  — WS background thread: base64 → MP3 bytes → NLayer
    ///                        decode → float samples → ConcurrentQueue.
    ///   Update()           — starts the streaming AudioClip the frame the first
    ///                        samples land in the queue.
    ///   SignalDone()       — HTTP "done": marks end of stream so the clip stops
    ///                        once the queue is fully drained.
    ///   StopAndClear()     — hard stop (UI close).
    /// </summary>
    public class NpcAudioPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;

        /// Max clip length — always stopped early via SignalDone / StopAndClear.
        private const int MaxClipSeconds = 300;

        private readonly ConcurrentQueue<float> _pcmQueue = new();

        private bool _streaming;

        // Written by main thread (SignalDone), read by audio thread (OnPCMRead).
        private volatile bool _allChunksReceived;

        // Written by audio thread (OnPCMRead), read by main thread (Update).
        private volatile bool _shouldStop;

        // _formatDetected: written by WS BG thread, read by main thread (Update).
        // volatile ensures main thread sees the write without a stale cache.
        // _sampleRate/_channels: written before _formatDetected is set (volatile write
        // creates a happens-before), so main thread reads are guaranteed to be fresh.
        private int           _sampleRate    = 44100;
        private int           _channels      = 1;
        private volatile bool _formatDetected;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Update()
        {
            // Wait until we know the actual format before creating the AudioClip.
            if (!_streaming && _formatDetected && !_pcmQueue.IsEmpty)
                StartStream();

            if (_shouldStop && audioSource.isPlaying)
            {
                _shouldStop = false;
                audioSource.Stop();
                _streaming = false;
                Debug.Log("[NpcAudioPlayer] ⏹ Stream finished");
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Thread-safe. Decodes the base64-encoded MP3 chunk to PCM float samples
        /// via NLayer and enqueues them for the streaming AudioClip.
        /// </summary>
        public void AccumulateChunk(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return;

            byte[] mp3Bytes;
            try { mp3Bytes = Convert.FromBase64String(base64); }
            catch (Exception e)
            {
                Debug.LogError("[NpcAudioPlayer] Base64 decode error: " + e.Message);
                return;
            }

            try
            {
                using var stream = new MemoryStream(mp3Bytes);
                using var mpeg   = new MpegFile(stream);

                // Capture format on the first chunk (thread-safe: ints, written once).
                if (!_formatDetected)
                {
                    _sampleRate    = mpeg.SampleRate;
                    _channels      = mpeg.Channels;
                    _formatDetected = true;
                    Debug.Log($"[NpcAudioPlayer] Format detected: {_sampleRate} Hz, {_channels} ch");
                }

                // Decode all PCM samples in this chunk and enqueue them.
                var buf = new float[4096];
                int samplesRead;
                int total = 0;
                while ((samplesRead = mpeg.ReadSamples(buf, 0, buf.Length)) > 0)
                {
                    for (int i = 0; i < samplesRead; i++)
                        _pcmQueue.Enqueue(buf[i]);
                    total += samplesRead;
                }

                Debug.Log($"[NpcAudioPlayer] +{total} PCM samples decoded (queue≈{_pcmQueue.Count})");
            }
            catch (Exception e)
            {
                Debug.LogError("[NpcAudioPlayer] MP3 decode error: " + e.Message);
            }
        }

        /// <summary>Main thread only. Signal that no more chunks will arrive.</summary>
        public void SignalDone()
        {
            _allChunksReceived = true;
            Debug.Log("[NpcAudioPlayer] SignalDone — draining remaining samples");
        }

        /// <summary>Immediate hard stop — clears queue and destroys the clip.</summary>
        public void StopAndClear()
        {
            _shouldStop        = false;
            _allChunksReceived = false;
            _streaming         = false;
            _formatDetected    = false;

            if (audioSource != null)
                audioSource.Stop();

            while (_pcmQueue.TryDequeue(out _)) { }

            if (audioSource != null && audioSource.clip != null)
            {
                Destroy(audioSource.clip);
                audioSource.clip = null;
            }

            Debug.Log("[NpcAudioPlayer] StopAndClear");
        }

        // ── Internal ─────────────────────────────────────────────────────────

        private void StartStream()
        {
            _streaming         = true;
            _allChunksReceived = false;
            _shouldStop        = false;

            if (audioSource.clip != null)
                Destroy(audioSource.clip);

            AudioClip clip = AudioClip.Create(
                "npc_voice_stream",
                _sampleRate * MaxClipSeconds,
                _channels,
                _sampleRate,
                stream: true,
                pcmreadercallback: OnPCMRead
            );

            audioSource.clip = clip;
            audioSource.loop = false;
            audioSource.Play();
            Debug.Log("[NpcAudioPlayer] ▶ Streaming started");
        }

        /// <summary>
        /// Audio thread — must be lock-free.
        /// Feeds queued PCM samples to Unity's audio buffer (silence when empty).
        /// </summary>
        private void OnPCMRead(float[] data)
        {
            bool wroteAnySilence = false;

            for (int i = 0; i < data.Length; i++)
            {
                if (_pcmQueue.TryDequeue(out float sample))
                {
                    data[i]         = sample;
                    wroteAnySilence = false;
                }
                else
                {
                    data[i]         = 0f;
                    wroteAnySilence = true;
                }
            }

            if (wroteAnySilence && _allChunksReceived && _pcmQueue.IsEmpty)
                _shouldStop = true;
        }
    }
}
