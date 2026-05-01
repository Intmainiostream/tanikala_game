using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;

public class Level3QuestManager : MonoBehaviour
{
    [Header("Lesson Panels (shown on 1st, 2nd, 3rd catch)")]
    public GameObject lessonPanel1;
    public GameObject lessonPanel2;
    public GameObject lessonPanel3;

    [Header("Dark Background (shows behind lesson panel)")]
    public GameObject darkBg;

    [Header("Tap Anywhere (dismisses lesson panel)")]
    public Button tapAnywhereBtn;

    [Header("Completion NPC — level completes when this PanelSequence finishes")]
    public PanelSequence completionNPC;

    [Header("Cutscene (plays after completion NPC dialogue)")]
    public PlayableDirector cutscene;
    public GameObject cutscenePanel;

    [Header("HUDs")]
    public GameObject[] huds;

    [Header("Level Complete")]
    public LevelCompleteManager levelCompleteManager;

    private int catchCount = 0;
    private GameObject[] lessonPanels;

    void Start()
    {
        lessonPanels = new GameObject[] { lessonPanel1, lessonPanel2, lessonPanel3 };

        if (tapAnywhereBtn != null)
        {
            tapAnywhereBtn.onClick.AddListener(DismissLesson);
            tapAnywhereBtn.gameObject.SetActive(false);
        }

        if (completionNPC != null)
            completionNPC.onComplete = CompleteLevel;
    }

    public void OnPlayerCaught()
    {
        HidingSpot.Reset();

        foreach (var pm in FindObjectsOfType<PlayerMovement>()) pm.Freeze();
        foreach (var ps in FindObjectsOfType<PlayerSpaceMovement>()) ps.enabled = false;
        foreach (var guard in FindObjectsOfType<GuardPatrol>()) guard.paused = true;
        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(false);

        if (darkBg != null) darkBg.SetActive(true);

        int panelIndex = Mathf.Clamp(catchCount, 0, lessonPanels.Length - 1);
        if (lessonPanels[panelIndex] != null) lessonPanels[panelIndex].SetActive(true);

        if (tapAnywhereBtn != null) tapAnywhereBtn.gameObject.SetActive(true);

        catchCount++;
    }

    void DismissLesson()
    {
        SceneManager.LoadScene("PreLevel3Scene");
    }

    void CompleteLevel()
    {
        foreach (InteractableObject obj in FindObjectsOfType<InteractableObject>())
        {
            if (obj.questionMark != null) obj.questionMark.SetActive(false);
            obj.enabled = false;
            Collider2D col = obj.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }

        foreach (var pm in FindObjectsOfType<PlayerMovement>()) pm.enabled = false;
        foreach (var ps in FindObjectsOfType<PlayerSpaceMovement>()) ps.enabled = false;
        foreach (var guard in FindObjectsOfType<GuardPatrol>()) guard.paused = true;
        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(false);

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

    public void StartQuest() { CompleteLevel(); }
}