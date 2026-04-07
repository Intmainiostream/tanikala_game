using UnityEngine;

public class MainMenuBGM : MonoBehaviour
{
    public AudioClip bgMusic;
    [Range(0f, 1f)]
    public float volume = 1f;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = bgMusic;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = volume;
        audioSource.Play();
    }
}
