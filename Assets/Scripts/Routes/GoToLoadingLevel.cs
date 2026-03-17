using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToLoadingLevel : MonoBehaviour
{
    public void OnButtonClick()
    {
        SceneManager.LoadScene("LoadingToLevel");
    }

    public void OnExitClick()
    {
        Application.Quit();
    }
}