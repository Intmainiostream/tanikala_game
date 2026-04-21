using UnityEngine;

public class PreLevel10Manager : MonoBehaviour
{
    [Header("Required interactable objects (must interact with ALL)")]
    public GameObject[] requiredObjects;

    [Header("Revealed when all objects are interacted with")]
    public GameObject nextArea;

    private bool[] interacted;

    void Start()
    {
        interacted = new bool[requiredObjects.Length];
        if (nextArea != null) nextArea.SetActive(false);
    }

    public void OnObjectInteracted(GameObject obj)
    {
        bool found = false;
        for (int i = 0; i < requiredObjects.Length; i++)
        {
            if (requiredObjects[i] == obj)
            {
                interacted[i] = true;
                found = true;
                Debug.Log($"[PreLevel10Manager] Marked interacted: {obj.name} (index {i})");
                break;
            }
        }

        if (!found)
            Debug.LogWarning($"[PreLevel10Manager] OnObjectInteracted called with '{obj.name}' but it's NOT in requiredObjects[]!");

        CheckAllInteracted();
    }

    void CheckAllInteracted()
    {
        int doneCount = 0;
        foreach (bool done in interacted) if (done) doneCount++;
        Debug.Log($"[PreLevel10Manager] {doneCount}/{interacted.Length} objects interacted");

        foreach (bool done in interacted)
            if (!done) return;

        Debug.Log("[PreLevel10Manager] All interacted! Revealing nextArea.");
        if (nextArea != null) nextArea.SetActive(true);
    }
}
