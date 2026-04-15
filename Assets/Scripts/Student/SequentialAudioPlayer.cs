using System.Collections;
using UnityEngine;

// Add this to any GameObject alongside InteractableObject.
// When the object is interacted with, plays clip1 then clip2 in sequence.
public class SequentialAudioPlayer : MonoBehaviour
{
    [Header("Audio Sequence")]
    public AudioClip clip1;
    [Range(0f, 1f)] public float volume1 = 1f;

    public AudioClip clip2;
    [Range(0f, 1f)] public float volume2 = 1f;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void Play()
    {
        StartCoroutine(PlaySequence());
    }

    public void Stop()
    {
        StopAllCoroutines();
        audioSource.Stop();
    }

    IEnumerator PlaySequence()
    {
        if (clip1 != null)
        {
            audioSource.volume = volume1;
            audioSource.clip = clip1;
            audioSource.Play();
            yield return new WaitForSeconds(clip1.length);
        }

        if (clip2 != null)
        {
            audioSource.volume = volume2;
            audioSource.clip = clip2;
            audioSource.Play();
        }
    }
}
