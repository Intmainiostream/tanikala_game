using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuOptions : MonoBehaviour
{
    [Header("Buttons (all optional)")]
    public Button muteBtn;
    public Button logoutBtn;
    public Button exitBtn;

    private bool isMuted = false;

    void Start()
    {
        if (muteBtn != null)   muteBtn.onClick.AddListener(ToggleMute);
        if (logoutBtn != null) logoutBtn.onClick.AddListener(Logout);
        if (exitBtn != null)   exitBtn.onClick.AddListener(() => Application.Quit());
    }

    void ToggleMute()
    {
        isMuted = !isMuted;
        AudioListener.volume = isMuted ? 0f : 1f;

        if (muteBtn != null)
        {
            Image img = muteBtn.GetComponent<Image>();
            if (img != null) img.color = isMuted ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
        }
    }

    void Logout()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        SceneManager.LoadScene("LoginScene");
    }
}
