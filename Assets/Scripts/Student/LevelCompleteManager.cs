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
        levelSelectBtn.onClick.AddListener(GoToLevelSelect);
        mainMenuBtn.onClick.AddListener(GoToMainMenu);
    }

    public void OnLevelComplete()
    {
        LevelSelectionManager.MarkLevelFinished(GlobalUserData.CurrentLevel);

        // Hide next level button if on last level
        if (GlobalUserData.CurrentLevel >= 10)
            nextLevelBtn.gameObject.SetActive(false);

        completionPanel.SetActive(true);
    }

    void GoToNextLevel()
    {
        int nextLevel = GlobalUserData.CurrentLevel + 1;
        GlobalUserData.CurrentLevel = nextLevel;
        SceneManager.LoadScene("Level" + nextLevel + "Scene");
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
