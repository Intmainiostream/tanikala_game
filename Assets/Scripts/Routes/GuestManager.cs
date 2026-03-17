using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GuestManager : MonoBehaviour
{
    public Button loginAsGuestBtn;

    void Start()
    {
        if (loginAsGuestBtn != null)
            loginAsGuestBtn.onClick.AddListener(LoginAsGuest);
    }

    void LoginAsGuest()
    {
        GlobalUserData.IsGuest = true;
        GlobalUserData.UserId  = "";
        Debug.Log("👤 Logged in as Guest.");
        SceneManager.LoadScene("LoadingToMainMenu");
    }
}