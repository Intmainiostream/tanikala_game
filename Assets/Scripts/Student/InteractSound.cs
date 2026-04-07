using UnityEngine;

public class InteractSound : MonoBehaviour
{
    [Header("Sound")]
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = false;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.loop = loop;
    }

    public void Play()
    {
        if (clip == null) return;
        audioSource.Stop();
        audioSource.Play();
    }

    public void Stop()
    {
        audioSource.Stop();
    }
}
