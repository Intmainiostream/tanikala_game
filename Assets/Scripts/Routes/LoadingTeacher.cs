using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingTeacher : MonoBehaviour
{
    [Header("Dot Images")]
    public Image dot1;
    public Image dot2;
    public Image dot3;
    public Image dot4;

    [Header("Settings")]
    public float delay = 3f;
    public float dotInterval = 0.4f;

    private float timer = 0f;
    private float dotTimer = 0f;
    private int dotCount = 0;
    private Image[] dots;

    void Start()
    {
        dots = new Image[] { dot1, dot2, dot3, dot4 };
        HideAllDots();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= delay)
        {
            SceneManager.LoadScene("TeacherScene");
        }

        dotTimer += Time.deltaTime;
        if (dotTimer >= dotInterval)
        {
            dotTimer = 0f;
            AnimateDots();
        }
    }

    private void AnimateDots()
    {
        HideAllDots();
        dotCount = (dotCount % dots.Length) + 1;

        for (int i = 0; i < dotCount; i++)
        {
            dots[i].gameObject.SetActive(true);
        }
    }

    private void HideAllDots()
    {
        foreach (Image dot in dots)
        {
            dot.gameObject.SetActive(false);
        }
    }
}