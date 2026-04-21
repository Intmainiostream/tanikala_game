using UnityEngine;
using UnityEngine.Playables;

public class Level6QuestManager : MonoBehaviour
{
    [Header("Evidence (hidden until Journalist is talked to)")]
    public GameObject evidenceObject;

    [Header("Journalist Panel Sequence")]
    [Tooltip("Assign the Journalist's PanelSequence here. OnComplete will reveal the evidence.")]
    public PanelSequence journalistPanelSequence;

    [Header("Cutscene (plays when evidence is interacted with)")]
    public PlayableDirector cutscene;
    public GameObject cutscenePanel;

    [Header("HUDs to hide during cutscene")]
    public GameObject[] huds;

    [Header("Level Complete")]
    public LevelCompleteManager levelCompleteManager;

    private bool journalistDone = false;

    void Start()
    {
        if (evidenceObject != null) evidenceObject.SetActive(false);
    }

    // Called by Journalist's InteractableObject (assign questManager6 on Journalist NPC)
    public void StartQuest()
    {
        if (!journalistDone)
        {
            journalistDone = true;
            if (evidenceObject != null) evidenceObject.SetActive(true);
            return;
        }

        // Called by evidence InteractableObject after its PanelSequence ends
        OnEvidenceInteracted();
    }

    void OnEvidenceInteracted()
    {
        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(false);

        // Disable all interactables so nothing fires during cutscene
        foreach (InteractableObject obj in FindObjectsOfType<InteractableObject>())
        {
            if (obj.questionMark != null) obj.questionMark.SetActive(false);
            obj.enabled = false;
            Collider2D col = obj.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }

        if (cutscene != null)
        {
            if (cutscenePanel != null) cutscenePanel.SetActive(true);
            cutscene.stopped += OnCutsceneFinished;
            cutscene.Play();
        }
        else
        {
            if (levelCompleteManager != null) levelCompleteManager.OnLevelComplete();
        }
    }

    void OnCutsceneFinished(PlayableDirector director)
    {
        cutscene.stopped -= OnCutsceneFinished;
        if (cutscenePanel != null) cutscenePanel.SetActive(false);
        if (levelCompleteManager != null) levelCompleteManager.OnLevelComplete();
    }
}
