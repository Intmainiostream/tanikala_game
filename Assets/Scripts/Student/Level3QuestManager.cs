using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

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

    [Header("Score Panel")]
    public GameObject scorePanel;
    public TMP_Text firstRecordText;
    public TMP_Text currentRecordText;
    public Button tapAnywhereScoreBtn;

    private static float s_accumulated = 0f;
    private static bool s_returning = false;

    private int catchCount = 0;
    private GameObject[] lessonPanels;
    private float sessionStart;

    void Start()
    {
        if (!s_returning) s_accumulated = 0f; // fresh entry — reset accumulator
        s_returning = false;
        sessionStart = Time.time;

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
        s_accumulated += Time.time - sessionStart; // bank elapsed time before leaving
        s_returning = true;
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

        SaveAndShowScore();
    }

    void SaveAndShowScore()
    {
        float elapsed = s_accumulated + (Time.time - sessionStart);
        s_accumulated = 0f; // clean up after level complete

        string uid = GlobalUserData.UserId;
        if (string.IsNullOrEmpty(uid)) { ProceedToEnd(); return; }

        var db = FirebaseFirestore.DefaultInstance;
        var userRef = db.Collection("users").Document(uid);

        userRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            float firstRecord = elapsed;

            if (task.IsCompletedSuccessfully)
            {
                if (task.Result.TryGetValue("level3_data", out object raw) && raw is Dictionary<string, object> d && d.ContainsKey("time"))
                    firstRecord = System.Convert.ToSingle(d["time"]);
                else
                    userRef.UpdateAsync("level3_data", new Dictionary<string, object> { { "time", elapsed } });
            }

            if (firstRecordText != null)  firstRecordText.text  = $"{Mathf.RoundToInt(firstRecord)}s";
            if (currentRecordText != null) currentRecordText.text = $"{Mathf.RoundToInt(elapsed)}s";

            if (scorePanel != null)
            {
                scorePanel.SetActive(true);
                if (tapAnywhereScoreBtn != null)
                {
                    tapAnywhereScoreBtn.gameObject.SetActive(true);
                    tapAnywhereScoreBtn.onClick.RemoveAllListeners();
                    tapAnywhereScoreBtn.onClick.AddListener(() =>
                    {
                        tapAnywhereScoreBtn.gameObject.SetActive(false);
                        scorePanel.SetActive(false);
                        ProceedToEnd();
                    });
                }
            }
            else
            {
                ProceedToEnd();
            }
        });
    }

    void ProceedToEnd()
    {
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
