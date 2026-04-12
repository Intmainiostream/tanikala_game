using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    [Header("Question Mark")]
    public GameObject questionMark;

    [Header("Panels to show when interacted (can assign multiple)")]
    public GameObject[] panels;

    [Header("Optional: triggers a quest when interacted")]
    public Level1QuestManager questManager;
    public Level2QuestManager questManager2;
    public Level3QuestManager questManager3;
    public Level4QuestManager questManager4;
    public Level5QuestManager questManager5;
    public Level6QuestManager questManager6;
    public Level7QuestManager questManager7;
    public Level8QuestManager questManager8;
    public Level9QuestManager questManager9;
    public Level10QuestManager questManager10;

    [Header("Optional: sound when interacted")]
    public InteractSound interactSound;

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

        if (interactSound != null)
            interactSound.Stop();

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

        System.Action startQuest = null;
        if      (questManager   != null) startQuest = questManager.StartQuest;
        else if (questManager2  != null) startQuest = questManager2.StartQuest;
        else if (questManager3  != null) startQuest = questManager3.StartQuest;
        else if (questManager4  != null) startQuest = questManager4.StartQuest;
        else if (questManager5  != null) startQuest = questManager5.StartQuest;
        else if (questManager6  != null) startQuest = questManager6.StartQuest;
        else if (questManager7  != null) startQuest = questManager7.StartQuest;
        else if (questManager8  != null) startQuest = questManager8.StartQuest;
        else if (questManager9  != null) startQuest = questManager9.StartQuest;
        else if (questManager10 != null) startQuest = questManager10.StartQuest;

        if (startQuest != null)
        {
            // Find a PanelSequence in the assigned panels; if found, wait for it to finish
            PanelSequence seq = null;
            foreach (GameObject panel in panels)
                if (panel != null) { seq = panel.GetComponentInChildren<PanelSequence>(); if (seq != null) break; }

            if (seq != null)
                seq.onComplete = startQuest;
            else
                startQuest();
        }

        // Level 4 flyer benches
        Level4FlyerInteractable flyer = GetComponent<Level4FlyerInteractable>();
        if (flyer != null && flyer.questManager != null)
            flyer.questManager.OnFlyerInteracted(flyer.flyerIndex);

        if (interactSound != null)
            interactSound.Play();
    }

    public void ClosePanel()
    {
        foreach (GameObject panel in panels)
            if (panel != null) panel.SetActive(false);

        // Re-show question mark if player is still nearby
        if (playerNearby && questionMark != null)
            questionMark.SetActive(true);

        if (interactSound != null)
            interactSound.Stop();
    }
}
