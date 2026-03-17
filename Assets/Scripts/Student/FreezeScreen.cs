using UnityEngine;
using UnityEngine.UI;

public class FreezeScreen : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject logo;
    public GameObject navsParent;
    public GameObject iconLogo;

    [Header("Settings")]
    public float idleTime = 5f;
    public float floatSpeed = 1.5f;
    public float floatAmount = 20f;

    private float timer = 0f;
    private bool isIdle = false;
    private Vector3 iconLogoStartPos;

    void Start()
    {
        iconLogo.SetActive(false);
        iconLogoStartPos = iconLogo.GetComponent<RectTransform>().anchoredPosition;
    }

    void Update()
    {
        if (!isIdle)
        {
            timer += Time.deltaTime;

            if (timer >= idleTime)
            {
                EnterIdleMode();
            }
        }

        // Check for input - works on both PC and mobile
        bool inputDetected = false;

        // Mobile touch
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            inputDetected = true;
        }

        // PC mouse click
        if (Input.GetMouseButtonDown(0))
        {
            inputDetected = true;
        }

        if (inputDetected)
        {
            if (isIdle)
            {
                ExitIdleMode();
            }
            else
            {
                timer = 0f;
            }
        }

        // Float animation
        if (isIdle && iconLogo.activeSelf)
        {
            RectTransform rt = iconLogo.GetComponent<RectTransform>();
            float newY = iconLogoStartPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmount;
            rt.anchoredPosition = new Vector2(iconLogoStartPos.x, newY);
        }
    }

    void EnterIdleMode()
    {
        isIdle = true;
        logo.SetActive(false);
        navsParent.SetActive(false);
        iconLogo.SetActive(true);
        iconLogoStartPos = iconLogo.GetComponent<RectTransform>().anchoredPosition;
    }

    void ExitIdleMode()
    {
        isIdle = false;
        timer = 0f;
        logo.SetActive(true);
        navsParent.SetActive(true);
        iconLogo.SetActive(false);
        iconLogo.GetComponent<RectTransform>().anchoredPosition = iconLogoStartPos;
    }
}