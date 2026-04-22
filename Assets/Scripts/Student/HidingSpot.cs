using UnityEngine;
using UnityEngine.SceneManagement;

public class HidingSpot : MonoBehaviour
{
    private static int hidingCount = 0;
    public static bool IsPlayerHiding => hidingCount > 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetOnSceneLoad()
    {
        SceneManager.sceneLoaded += (scene, mode) => hidingCount = 0;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            hidingCount++;
            Debug.Log($"[HidingSpot] {gameObject.name} ENTER — hidingCount={hidingCount}");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            hidingCount = Mathf.Max(0, hidingCount - 1);
            Debug.Log($"[HidingSpot] {gameObject.name} EXIT — hidingCount={hidingCount}");
        }
    }

    public static void Reset()
    {
        hidingCount = 0;
    }
}
