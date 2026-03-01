using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace audio
{
    /// <summary>
    /// Receives base64-encoded MP3 chunks from the NPC voice stream,
    /// decodes them and plays them sequentially via an AudioSource.
    /// </summary>
    public class NpcAudioPlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;

        private readonly Queue<string> _pendingBase64Clips = new();
        private bool _isPlaying;
        private int _tempFileIndex;

        /// <summary>Enqueue a base64-encoded MP3 string for playback.</summary>
        public void EnqueueAudio(string base64Mp3)
        {
            if (string.IsNullOrEmpty(base64Mp3)) return;
            _pendingBase64Clips.Enqueue(base64Mp3);
            if (!_isPlaying)
                StartCoroutine(PlayNextClips());
        }

        /// <summary>Stop current playback and discard queued clips.</summary>
        public void StopAndClear()
        {
            StopAllCoroutines();
            if (audioSource != null)
                audioSource.Stop();
            _pendingBase64Clips.Clear();
            _isPlaying = false;
        }

        private IEnumerator PlayNextClips()
        {
            _isPlaying = true;

            while (_pendingBase64Clips.Count > 0)
            {
                string base64 = _pendingBase64Clips.Dequeue();

                byte[] mp3Bytes;
                try
                {
                    mp3Bytes = Convert.FromBase64String(base64);
                }
                catch (Exception e)
                {
                    Debug.LogError("[NpcAudioPlayer] Base64 decode error: " + e.Message);
                    continue;
                }

                string tempPath = Path.Combine(
                    Application.temporaryCachePath,
                    $"npc_voice_{_tempFileIndex++}.mp3"
                );

                try
                {
                    File.WriteAllBytes(tempPath, mp3Bytes);
                }
                catch (Exception e)
                {
                    Debug.LogError("[NpcAudioPlayer] Failed to write temp file: " + e.Message);
                    continue;
                }

                using var req = UnityWebRequestMultimedia.GetAudioClip(
                    "file://" + tempPath, AudioType.MPEG
                );
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("[NpcAudioPlayer] Failed to load audio clip: " + req.error);
                    TryDeleteTempFile(tempPath);
                    continue;
                }

                AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
                audioSource.clip = clip;
                audioSource.Play();

                yield return new WaitWhile(() => audioSource.isPlaying);

                TryDeleteTempFile(tempPath);
            }

            _isPlaying = false;
        }

        private static void TryDeleteTempFile(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { /* non-fatal */ }
        }
    }
}
