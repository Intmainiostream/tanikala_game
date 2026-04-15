using UnityEngine;
using UnityEngine.UI;

public class LoadingDotsAnimation : MonoBehaviour
{
    [Header("Dot Images")]
    public Image dot1;
    public Image dot2;
    public Image dot3;
    public Image dot4;

    [Header("Settings")]
    public float dotInterval = 0.4f;

    private float dotTimer = 0f;
    private int dotCount = 0;
    private Image[] dots;

    void OnEnable()
    {
        dots = new Image[] { dot1, dot2, dot3, dot4 };
        dotCount = 0;
        HideAllDots();
    }

    void Update()
    {
        dotTimer += Time.deltaTime;
        if (dotTimer >= dotInterval)
        {
            dotTimer = 0f;
            dotCount = (dotCount % dots.Length) + 1;
            HideAllDots();
            for (int i = 0; i < dotCount; i++)
                dots[i].gameObject.SetActive(true);
        }
    }

    void HideAllDots()
    {
        foreach (Image dot in dots)
            if (dot != null) dot.gameObject.SetActive(false);
    }
}
