using System.Collections;
using TMPro;
using UnityEngine;

namespace ui
{
    public class ToastItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private float fadeDuration = 0.3f;

        private CanvasGroup canvasGroup;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                Debug.LogWarning("[ToastItem] Pas de CanvasGroup sur le prefab, en ajout d'un automatiquement.");
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void Init(string message, float displayDuration)
        {
            if (messageText == null)
            {
                Debug.LogError("[ToastItem] messageText non assigné dans l'inspector du prefab !");
                Destroy(gameObject);
                return;
            }
            messageText.SetText(message);
            StartCoroutine(ToastRoutine(displayDuration));
        }

        private IEnumerator ToastRoutine(float displayDuration)
        {
            // Fade in
            yield return Fade(0f, 1f, fadeDuration);

            // Hold
            yield return new WaitForSeconds(displayDuration);

            // Fade out
            yield return Fade(1f, 0f, fadeDuration);

            Destroy(gameObject);
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            canvasGroup.alpha = from;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            canvasGroup.alpha = to;
        }
    }
}
