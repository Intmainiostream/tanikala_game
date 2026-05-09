using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Auth;

public class MainMenuOptions : MonoBehaviour
{
    [Header("Buttons (all optional)")]
    public Button muteBtn;
    public Button logoutBtn;
    public Button exitBtn;

    [Header("Sound Toggle Icons")]
    public GameObject soundOnIcon;
    public GameObject soundOffIcon;

    private bool isMuted = false;

    void Start()
    {
        if (muteBtn != null)   muteBtn.onClick.AddListener(ToggleMute);
        if (soundOnIcon != null)  soundOnIcon.SetActive(true);
        if (soundOffIcon != null) soundOffIcon.SetActive(false);
        if (logoutBtn != null) logoutBtn.onClick.AddListener(Logout);
        if (exitBtn != null)   exitBtn.onClick.AddListener(() => Application.Quit());
    }

    void ToggleMute()
    {
        isMuted = !isMuted;
        AudioListener.volume = isMuted ? 0f : 1f;

        if (soundOnIcon != null)  soundOnIcon.SetActive(!isMuted);
        if (soundOffIcon != null) soundOffIcon.SetActive(isMuted);
    }

    void Logout()
    {
        FirebaseAuth.DefaultInstance.SignOut();
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        SceneManager.LoadScene("LoginScene");
    }
}
