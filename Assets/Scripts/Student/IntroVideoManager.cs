using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class IntroVideoManager : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Button skipBtn;
    public string nextScene = "LoadingToLevel";

    void Start()
    {
        skipBtn.onClick.AddListener(LoadNext);
        videoPlayer.loopPointReached += _ => LoadNext();
        videoPlayer.Play();
    }

    void LoadNext()
    {
        videoPlayer.Stop();
        SceneManager.LoadScene(nextScene);
    }
}
