using UnityEngine;

namespace ui
{
    public class ToastNotificationService : MonoBehaviour
    {
        public static ToastNotificationService Instance { get; private set; }

        [SerializeField] private ToastItem toastPrefab;
        [SerializeField] private Transform toastContainer;
        [SerializeField] private float defaultDisplayDuration = 2.5f;
        [SerializeField] private int maxToasts = 5;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Debug.Log("[ToastNotificationService] Instance créée sur : " + gameObject.name);
        }

        public static void Show(string message, float duration = -1f)
        {
            if (Instance == null)
            {
                Debug.LogError("[ToastNotificationService] Instance NULL — ajoute le composant dans la scène !");
                return;
            }
            Instance.ShowToast(message, duration);
        }

        public void ShowToast(string message, float duration = -1f)
        {
            if (toastPrefab == null)
            {
                Debug.LogError("[ToastNotificationService] toastPrefab non assigné dans l'inspector !");
                return;
            }
            if (toastContainer == null)
            {
                Debug.LogError("[ToastNotificationService] toastContainer non assigné dans l'inspector !");
                return;
            }

            if (toastContainer.childCount >= maxToasts)
            {
                Destroy(toastContainer.GetChild(0).gameObject);
            }

            float displayDuration = duration > 0f ? duration : defaultDisplayDuration;
            Debug.Log($"[ToastNotificationService] Affichage toast : \"{message}\"");
            ToastItem toast = Instantiate(toastPrefab, toastContainer);
            toast.Init(message, displayDuration);
        }
    }
}
