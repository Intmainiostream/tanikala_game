using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Auth;

public class Logout : MonoBehaviour
{
    public void LogoutUser()
    {
        FirebaseAuth.DefaultInstance.SignOut();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        SceneManager.LoadScene("LoginScene");
    }
}