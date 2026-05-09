using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

public class Level4QuestManager : MonoBehaviour
{
    [System.Serializable]
    public class FlyerEntry
    {
        public GameObject flyerImage;
        [TextArea] public string innerMonologueText;
        public AudioClip innerMonologueSound;
    }

    [Header("Flyers (index 0 = Bench1, index 1 = Bench2)")]
    public FlyerEntry[] flyers;

    [Header("Panels")]
    public GameObject questPanel;
    public GameObject innerMonologuePanel;

    [Header("Inner Monologue")]
    public TMP_Text innerMonologueText;

    [Header("Tap Anywhere")]
    public Button tapAnywhereBtn;

    [Header("HUDs")]
    public GameObject[] huds;

    [Header("Celia - reveals benches when her dialogue finishes")]
    public PanelSequence celiaPanelSequence;
    public GameObject bench1Object;
    public GameObject bench2Object;

    [Header("Cutscene")]
    public PlayableDirector cutscene;
    public GameObject cutscenePanel;

    [Header("Level Complete")]
    public LevelCompleteManager levelCompleteManager;

    [Header("Score Panel")]
    public GameObject scorePanel;
    public TMP_Text firstRecordText;
    public TMP_Text currentRecordText;
    public Button tapAnywhereScoreBtn;

    private AudioSource audioSource;
    private bool[] flyersFound;
    private int currentFlyerIndex = -1;
    private float startTime = -1f;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        flyersFound = new bool[flyers.Length];

        questPanel.SetActive(false);
        tapAnywhereBtn.onClick.AddListener(OnTap);

        if (bench1Object != null) bench1Object.SetActive(false);
        if (bench2Object != null) bench2Object.SetActive(false);
    }

    public void StartQuest()
    {
        startTime = Time.time;
        if (bench1Object != null) bench1Object.SetActive(true);
        if (bench2Object != null) bench2Object.SetActive(true);
        if (huds.Length > 1 && huds[1] != null) huds[1].SetActive(true);
    }

    public void OnFlyerInteracted(int index)
    {
        if (index < 0 || index >= flyers.Length) return;

        currentFlyerIndex = index;

        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(false);

        questPanel.SetActive(true);

        for (int i = 0; i < flyers.Length; i++)
            if (flyers[i].flyerImage != null)
                flyers[i].flyerImage.SetActive(i == index);

        var entry = flyers[index];
        innerMonologueText.text = entry.innerMonologueText;
        innerMonologuePanel.SetActive(!string.IsNullOrEmpty(entry.innerMonologueText));

        if (entry.innerMonologueSound != null)
        {
            audioSource.Stop();
            audioSource.clip = entry.innerMonologueSound;
            audioSource.Play();
        }

        tapAnywhereBtn.gameObject.SetActive(true);
    }

    void OnTap()
    {
        audioSource.Stop();
        innerMonologuePanel.SetActive(false);

        if (currentFlyerIndex >= 0 && currentFlyerIndex < flyers.Length)
            if (flyers[currentFlyerIndex].flyerImage != null)
                flyers[currentFlyerIndex].flyerImage.SetActive(false);

        questPanel.SetActive(false);
        tapAnywhereBtn.gameObject.SetActive(false);

        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(true);

        if (currentFlyerIndex >= 0)
        {
            flyersFound[currentFlyerIndex] = true;

            if (currentFlyerIndex == 0 && bench1Object != null) bench1Object.SetActive(false);
            else if (currentFlyerIndex == 1 && bench2Object != null) bench2Object.SetActive(false);
        }
        currentFlyerIndex = -1;

        bool allFound = true;
        foreach (bool found in flyersFound) if (!found) { allFound = false; break; }
        if (allFound) TriggerEnd();
    }

    void TriggerEnd()
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
        float elapsed = startTime >= 0f ? Time.time - startTime : 0f;

        string uid = GlobalUserData.UserId;
        if (string.IsNullOrEmpty(uid)) { ProceedToEnd(); return; }

        var db = FirebaseFirestore.DefaultInstance;
        var userRef = db.Collection("users").Document(uid);

        userRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            float firstRecord = elapsed;

            if (task.IsCompletedSuccessfully)
            {
                if (task.Result.TryGetValue("level4_data", out object raw) && raw is Dictionary<string, object> d && d.ContainsKey("time"))
                    firstRecord = System.Convert.ToSingle(d["time"]);
                else
                    userRef.UpdateAsync("level4_data", new Dictionary<string, object> { { "time", elapsed } });
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
        if (levelCompleteManager != null) levelCompleteManager.OnLevelComplete();
    }
}
