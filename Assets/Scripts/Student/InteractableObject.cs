using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Header("Question Mark")]
    public GameObject questionMark;

    [Header("Panels to show when interacted (can assign multiple)")]
    public GameObject[] panels;

    [Header("Optional: triggers a quest when interacted")]
    public Level1QuestManager questManager;

    private bool playerNearby = false;

    void Start()
    {
        if (questionMark != null)
            questionMark.SetActive(false);

        foreach (GameObject panel in panels)
            if (panel != null) panel.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerNearby = true;
        if (questionMark != null)
            questionMark.SetActive(true);

        InteractionManager.Instance.SetNearbyInteractable(this);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        playerNearby = false;
        if (questionMark != null)
            questionMark.SetActive(false);

        foreach (GameObject panel in panels)
            if (panel != null) panel.SetActive(false);

        InteractionManager.Instance.ClearNearbyInteractable(this);
    }

    public void Interact()
    {
        // Hide all question marks
        foreach (InteractableObject obj in FindObjectsOfType<InteractableObject>())
            if (obj.questionMark != null) obj.questionMark.SetActive(false);

        // Show all assigned panels
        foreach (GameObject panel in panels)
            if (panel != null) panel.SetActive(true);

        if (questManager != null)
            questManager.StartQuest();
    }

    public void ClosePanel()
    {
        foreach (GameObject panel in panels)
            if (panel != null) panel.SetActive(false);

        // Re-show question mark if player is still nearby
        if (playerNearby && questionMark != null)
            questionMark.SetActive(true);
    }
}
