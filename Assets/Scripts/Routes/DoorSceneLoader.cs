using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorSceneLoader : MonoBehaviour
{
    public string sceneName;

    public void Load()
    {
        InteractSound interactSound = GetComponent<InteractSound>();
        if (interactSound != null && interactSound.clip != null)
            StartCoroutine(LoadAfterSound(interactSound.clip.length));
        else
            SceneManager.LoadScene(sceneName);
    }

    IEnumerator LoadAfterSound(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }
}
