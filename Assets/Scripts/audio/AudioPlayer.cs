using Unity;
using UnityEngine;

public class AudioPlayer : MonoBehaviour
{
    public static System.Random rng = new System.Random();

    [SerializeField] private AudioClip[] walkSounds;
    [SerializeField] private AudioClip[] goldSounds;
    [SerializeField] private AudioClip[] itemSounds;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void PlayWalk()
    {
        audioSource.PlayOneShot(walkSounds[rng.Next(walkSounds.Length)]);
    }

    public void PlayGold()
    {
        audioSource.PlayOneShot(goldSounds[rng.Next(goldSounds.Length)]);
    }

    public void PlayItemTrade()
    {
        audioSource.PlayOneShot(itemSounds[rng.Next(itemSounds.Length)]);
    }
}