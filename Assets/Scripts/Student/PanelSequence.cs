using UnityEngine;
using UnityEngine.UI;

public class PanelSequence : MonoBehaviour
{
    [Header("Slides (assign in order)")]
    public GameObject[] slides;

    [Header("Dialogue Sound")]
    public AudioClip[] dialogueClips;
    [Range(0f, 1f)] public float dialogueVolume = 1f;

    private int currentIndex = 0;
    private AudioSource audioSource;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = dialogueVolume;
    }

    void OnEnable()
    {
        currentIndex = 0;
        ShowSlide(0);
    }

    void OnDisable()
    {
        if (audioSource != null)
            audioSource.Stop();
    }

    public void OnInteractPressed()
    {
        currentIndex++;

        if (currentIndex >= slides.Length)
        {
            gameObject.SetActive(false);
            return;
        }

        ShowSlide(currentIndex);
    }

    void ShowSlide(int index)
    {
        for (int i = 0; i < slides.Length; i++)
            slides[i].SetActive(i == index);

        if (dialogueClips != null && index < dialogueClips.Length && dialogueClips[index] != null)
        {
            audioSource.Stop();
            audioSource.clip = dialogueClips[index];
            audioSource.Play();
        }
    }
}
