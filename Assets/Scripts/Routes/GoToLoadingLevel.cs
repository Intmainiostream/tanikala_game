using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToLoadingLevel : MonoBehaviour
{
    public void OnButtonClick()
    {
        SceneManager.LoadScene("IntroScene");
    }

    public void OnExitClick()
    {
        Application.Quit();
    }
}