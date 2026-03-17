using UnityEngine;

public class NavbarToggle : MonoBehaviour
{
    public GameObject navbar;
    public GameObject hamburgerBtn;
    public RectTransform contentContainer;

    [Header("Layout Settings")]
    [SerializeField] private float expandedScaleX = 1f;
    [SerializeField] private float hiddenScaleX = 1.3f;

    void Start()
    {
        navbar.SetActive(false);
        hamburgerBtn.SetActive(true);
        contentContainer.localScale = new Vector3(expandedScaleX, 1f, 1f);
        contentContainer.anchoredPosition = new Vector2(0f, contentContainer.anchoredPosition.y);
    }

    [SerializeField] private float contentShiftX = 80f;

    public void ShowMenu()
    {
        navbar.SetActive(true);
        hamburgerBtn.SetActive(false);
        contentContainer.localScale = new Vector3(hiddenScaleX, 1f, 1f);
        contentContainer.anchoredPosition = new Vector2(contentShiftX, contentContainer.anchoredPosition.y);
    }

    public void HideMenu()
    {
        navbar.SetActive(false);
        hamburgerBtn.SetActive(true);
        contentContainer.localScale = new Vector3(expandedScaleX, 1f, 1f);
        contentContainer.anchoredPosition = new Vector2(0f, contentContainer.anchoredPosition.y);
    }
}