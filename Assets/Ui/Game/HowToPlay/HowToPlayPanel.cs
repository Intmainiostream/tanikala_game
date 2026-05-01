using UnityEngine;
using UnityEngine.UI;

public class HowToPlayPanel : MonoBehaviour
{
    [Header("Pages")]
    public GameObject[] pages; // Assign pages 1-8 in the Inspector

    [Header("Buttons")]
    public Button prevBtn;
    public Button nextBtn;
    public Button closeBtn;

    private int currentPageIndex = 0;

    void Start()
    {
        prevBtn.onClick.AddListener(OnPrevClicked);
        nextBtn.onClick.AddListener(OnNextClicked);

        if (closeBtn != null)
            closeBtn.onClick.AddListener(OnCloseClicked);

        ShowPage(currentPageIndex);
    }

    void OnNextClicked()
    {
        if (currentPageIndex < pages.Length - 1)
        {
            currentPageIndex++;
            ShowPage(currentPageIndex);
        }
    }

    void OnPrevClicked()
    {
        if (currentPageIndex > 0)
        {
            currentPageIndex--;
            ShowPage(currentPageIndex);
        }
    }

    void OnCloseClicked()
    {
        gameObject.SetActive(false);
    }

    void ShowPage(int index)
    {
        // Hide all pages
        for (int i = 0; i < pages.Length; i++)
        {
            pages[i].SetActive(i == index);
        }

        // Update button interactability
        prevBtn.interactable = index > 0;
        nextBtn.interactable = index < pages.Length - 1;
    }

    // Optional: Call this to open the panel and reset to page 1
    public void OpenPanel()
    {
        gameObject.SetActive(true);
        currentPageIndex = 0;
        ShowPage(currentPageIndex);
    }
}