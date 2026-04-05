using UnityEngine;
using UnityEngine.UI;

public class PanelSequence : MonoBehaviour
{
    [Header("Slides (assign in order)")]
    public GameObject[] slides;

    private int currentIndex = 0;

    void OnEnable()
    {
        currentIndex = 0;
        ShowSlide(0);
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
    }
}
