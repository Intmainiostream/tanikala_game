using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToLevel3 : MonoBehaviour
{
    public void LoadLevel3()
    {
        SceneManager.LoadScene("LoadLevel3Scene");
    }
}
