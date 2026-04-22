using UnityEngine;

public class PreLevel10Manager : MonoBehaviour
{
    [Header("How many objects must be interacted with")]
    public int requiredCount = 2;

    [Header("Revealed when all objects are interacted with")]
    public GameObject nextArea;

    private bool[] interacted;

    void Start()
    {
        interacted = new bool[requiredCount];
        if (nextArea != null) nextArea.SetActive(false);
    }

    public void OnObjectInteracted(int index)
    {
        if (index < 0 || index >= interacted.Length) return;

        interacted[index] = true;
        Debug.Log($"[PreLevel10Manager] Object {index} interacted. Checking all...");
        CheckAllInteracted();
    }

    void CheckAllInteracted()
    {
        int doneCount = 0;
        foreach (bool done in interacted) if (done) doneCount++;
        Debug.Log($"[PreLevel10Manager] {doneCount}/{interacted.Length} done.");

        foreach (bool done in interacted)
            if (!done) return;

        Debug.Log($"[PreLevel10Manager] All done! nextArea={(nextArea != null ? nextArea.name : "NULL")}");
        if (nextArea != null)
        {
            nextArea.SetActive(true);
            Debug.Log($"[PreLevel10Manager] nextArea.activeSelf={nextArea.activeSelf}");
        }
    }
}
