using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelCompleteManager : MonoBehaviour
{
    [Header("Completion Panel")]
    public GameObject completionPanel;

    [Header("Buttons")]
    public Button nextLevelBtn;
    public Button levelSelectBtn;
    public Button mainMenuBtn;

    void Start()
    {
        completionPanel.SetActive(false);

        nextLevelBtn.onClick.AddListener(GoToNextLevel);
        if (levelSelectBtn != null) levelSelectBtn.onClick.AddListener(GoToLevelSelect);
        if (mainMenuBtn != null) mainMenuBtn.onClick.AddListener(GoToMainMenu);
    }

    public void OnLevelComplete()
    {
        if (GlobalUserData.CurrentLevel >= 10)
            nextLevelBtn.gameObject.SetActive(false);

        completionPanel.SetActive(true);
    }

    void GoToNextLevel()
    {
        nextLevelBtn.interactable = false;
        LevelSelectionManager.MarkLevelFinished(GlobalUserData.CurrentLevel, () =>
        {
            SceneManager.LoadScene("LevelScene");
        });
    }

    void GoToLevelSelect()
    {
        SceneManager.LoadScene("LevelScene");
    }

    void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
