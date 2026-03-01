using UnityEngine;

namespace DefaultNamespace
{
    [CreateAssetMenu(fileName = "AppConfig", menuName = "Configuration/AppConfig")]
    public class AppConfig : ScriptableObject
    {
        public string BaseUrl;
        public string VoxtralBaseUrl;
        public string NpcAudioBaseUrl;
    }
}