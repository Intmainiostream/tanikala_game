using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

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

    [Header("Score Panel")]
    public GameObject scorePanel;
    public TMP_Text firstRecordText;
    public TMP_Text currentRecordText;
    public Button tapAnywhereScoreBtn;

    private bool journalistDone = false;
    private float startTime;

    void Start()
    {
        startTime = Time.time;
        if (evidenceObject != null) evidenceObject.SetActive(false);
    }

    public void StartQuest()
    {
        if (!journalistDone)
        {
            journalistDone = true;
            if (evidenceObject != null) evidenceObject.SetActive(true);
            return;
        }

        OnEvidenceInteracted();
    }

    void OnEvidenceInteracted()
    {
        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(false);

        foreach (InteractableObject obj in FindObjectsOfType<InteractableObject>())
        {
            if (obj.questionMark != null) obj.questionMark.SetActive(false);
            obj.enabled = false;
            Collider2D col = obj.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
        }

        SaveAndShowScore();
    }

    void SaveAndShowScore()
    {
        float elapsed = Time.time - startTime;

        string uid = GlobalUserData.UserId;
        if (string.IsNullOrEmpty(uid)) { ProceedToEnd(); return; }

        var db = FirebaseFirestore.DefaultInstance;
        var userRef = db.Collection("users").Document(uid);

        userRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            float firstRecord = elapsed;

            if (task.IsCompletedSuccessfully)
            {
                if (task.Result.TryGetValue("level6_data", out object raw) && raw is Dictionary<string, object> d && d.ContainsKey("time"))
                    firstRecord = System.Convert.ToSingle(d["time"]);
                else
                    userRef.UpdateAsync("level6_data", new Dictionary<string, object> { { "time", elapsed } });
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
}
