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

    public static bool AnyInteractionActive = false;

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

        AnyInteractionActive = true;

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

        // Find a PanelSequence in the assigned panels
        PanelSequence seq = null;
        foreach (GameObject panel in panels)
            if (panel != null) { seq = panel.GetComponentInChildren<PanelSequence>(); if (seq != null) break; }

        Level10Trackable trackable = GetComponent<Level10Trackable>();
        System.Action notifyTrackable = null;
        if (trackable != null && trackable.manager != null)
        {
            var idx = trackable.objectIndex;
            var mgr = trackable.manager;
            notifyTrackable = () => mgr.OnObjectInteracted(idx);
        }

        Level9WaterRecipient water = GetComponent<Level9WaterRecipient>();
        System.Action notifyWater = null;
        if (water != null && water.questManager != null)
        {
            var idx = water.npcIndex;
            var wMgr = water.questManager;
            var self = this;
            notifyWater = () => wMgr.OnNPCGiven(idx, self);
        }

        if (startQuest != null)
        {
            if (seq != null)
            {
                var captured = startQuest;
                var notify = notifyTrackable;
                var notifyW = notifyWater;
                seq.onComplete = () => { AnyInteractionActive = false; notify?.Invoke(); notifyW?.Invoke(); captured(); };
            }
            else
            {
                notifyTrackable?.Invoke();
                notifyWater?.Invoke();
                startQuest();
            }
        }
        else if (seq != null)
        {
            var notify = notifyTrackable;
            var notifyW = notifyWater;
            seq.onComplete = () => { AnyInteractionActive = false; notify?.Invoke(); notifyW?.Invoke(); };
        }
        else
        {
            notifyTrackable?.Invoke();
            notifyWater?.Invoke();
        }

        // Level 4 flyer benches
        Level4FlyerInteractable flyer = GetComponent<Level4FlyerInteractable>();
        if (flyer != null && flyer.questManager != null)
            flyer.questManager.OnFlyerInteracted(flyer.flyerIndex);

        // Door scene loader
        DoorSceneLoader door = GetComponent<DoorSceneLoader>();
        if (door != null) door.Load();

        // Sequential audio (e.g. gunshot → crowd scream)
        SequentialAudioPlayer seqAudio = GetComponent<SequentialAudioPlayer>();
        if (seqAudio != null) seqAudio.Play();

        if (interactSound != null)
            interactSound.Play();
    }

    public void ClosePanel()
    {
        AnyInteractionActive = false;

        foreach (GameObject panel in panels)
            if (panel != null) panel.SetActive(false);

        // Re-show question mark if player is still nearby
        if (playerNearby && questionMark != null)
            questionMark.SetActive(true);

        if (interactSound != null)
            interactSound.Stop();
    }
}
