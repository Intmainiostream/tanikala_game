using UnityEngine;
using UnityEngine.SceneManagement;

public class Logout : MonoBehaviour
{
    public void LogoutUser()
    {
        // Clear saved login/session data
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // Load login scene
        SceneManager.LoadScene("LoginScene");
    }
}