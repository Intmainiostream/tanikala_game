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

    void Start()
    {
        // Hide evidence until journalist is talked to
        if (evidenceObject != null) evidenceObject.SetActive(false);

        // Hook into journalist dialogue completion
        if (journalistPanelSequence != null)
            journalistPanelSequence.onComplete = OnJournalistTalked;
    }

    void OnJournalistTalked()
    {
        if (evidenceObject != null) evidenceObject.SetActive(true);
    }

    // Called by InteractableObject on the evidence object
    public void StartQuest()
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
