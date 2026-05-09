using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

public class Level7QuestManager : MonoBehaviour
{
    [System.Serializable]
    public class NewspaperEntry
    {
        public GameObject newspaper;

        [Header("Leave empty to not show")]
        [TextArea] public string innerMonologueText;

        [TextArea] public string believeText;
        [TextArea] public string questionText;

        public string lessonTitle;
        [TextArea] public string lessonDescription;

        [Header("Sounds")]
        public AudioClip innerMonologueSound;
        public AudioClip believeSound;
        public AudioClip questionSound;
    }

    [Header("Newspaper Sequence")]
    public NewspaperEntry[] newspapers;

    [Header("Panels")]
    public GameObject questPanel;
    public GameObject innerMonologuePanel;
    public GameObject choicesPanel;
    public GameObject lessonPanel;

    [Header("Inner Monologue")]
    public TMP_Text innerMonologueText;

    [Header("Choices")]
    public Button believeBtn;
    public Button questionBtn;

    [Header("Lesson")]
    public TMP_Text lessonTitleText;
    public TMP_Text lessonDescriptionText;

    [Header("Tap Anywhere")]
    public Button tapAnywhereBtn;

    [Header("HUDs to hide during quest")]
    public GameObject[] huds;

    [Header("End Panel")]
    public PanelSequence endPanel;

    [Header("Level Complete")]
    public LevelCompleteManager levelCompleteManager;

    [Header("Newspaper Sound")]
    public AudioClip newspaperSound;

    [Header("Score Panel")]
    public GameObject scorePanel;
    public TMP_Text trustCountText;
    public TMP_Text doubtCountText;
    public Button tapAnywhereScoreBtn;

    private AudioSource audioSource;
    private int currentIndex = 0;
    private int trustCount = 0, doubtCount = 0;

    private enum QuestState { Newspaper, InnerMonologue, Choices, Lesson }
    private QuestState state;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        questPanel.SetActive(false);
        tapAnywhereBtn.onClick.AddListener(OnTap);
        believeBtn.onClick.AddListener(() => OnChoice(true));
        questionBtn.onClick.AddListener(() => OnChoice(false));
    }

    void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void StartQuest()
    {
        currentIndex = 0;
        questPanel.SetActive(true);
        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(false);

        foreach (NewspaperEntry entry in newspapers)
            if (entry.newspaper != null) entry.newspaper.SetActive(true);

        ShowNewspaper();
    }

    void ShowNewspaper()
    {
        state = QuestState.Newspaper;

        innerMonologuePanel.SetActive(false);
        choicesPanel.SetActive(false);
        lessonPanel.SetActive(false);
        tapAnywhereBtn.gameObject.SetActive(true);

        PlaySound(newspaperSound);
    }

    void OnTap()
    {
        switch (state)
        {
            case QuestState.Newspaper:      ShowInnerMonologue(); break;
            case QuestState.InnerMonologue: ShowChoices();        break;
            case QuestState.Lesson:         NextNewspaper();      break;
        }
    }

    void ShowInnerMonologue()
    {
        state = QuestState.InnerMonologue;
        var entry = newspapers[currentIndex];

        innerMonologueText.text = entry.innerMonologueText;
        bool showInner = !string.IsNullOrEmpty(entry.innerMonologueText);
        innerMonologuePanel.SetActive(showInner);
        if (showInner) PlaySound(entry.innerMonologueSound);
    }

    void ShowChoices()
    {
        state = QuestState.Choices;
        innerMonologuePanel.SetActive(false);
        choicesPanel.SetActive(true);
        tapAnywhereBtn.gameObject.SetActive(false);
    }

    void OnChoice(bool believed)
    {
        if (believed) trustCount++; else doubtCount++;
        choicesPanel.SetActive(false);
        var entry = newspapers[currentIndex];
        PlaySound(believed ? entry.believeSound : entry.questionSound);
        ShowLesson();
    }

    void ShowLesson()
    {
        state = QuestState.Lesson;

        var entry = newspapers[currentIndex];
        bool hasLesson = !string.IsNullOrEmpty(entry.lessonTitle) || !string.IsNullOrEmpty(entry.lessonDescription);

        if (hasLesson)
        {
            lessonTitleText.text = entry.lessonTitle;
            lessonDescriptionText.text = entry.lessonDescription;
            lessonPanel.SetActive(true);
            tapAnywhereBtn.gameObject.SetActive(true);
        }
        else
        {
            lessonPanel.SetActive(false);
            NextNewspaper();
        }
    }

    void NextNewspaper()
    {
        if (newspapers[currentIndex].newspaper != null)
            newspapers[currentIndex].newspaper.SetActive(false);

        currentIndex++;

        if (currentIndex >= newspapers.Length)
        {
            EndQuest();
            return;
        }

        ShowNewspaper();
    }

    void EndQuest()
    {
        SaveLevelData();
        audioSource.Stop();
        questPanel.SetActive(false);
        foreach (GameObject hud in huds) if (hud != null) hud.SetActive(false);
        ShowScorePanel();
    }

    void ShowScorePanel()
    {
        if (trustCountText != null) trustCountText.text = trustCount.ToString();
        if (doubtCountText != null) doubtCountText.text = doubtCount.ToString();

        if (scorePanel != null)
        {
            scorePanel.SetActive(true);
            if (tapAnywhereScoreBtn != null)
            {
                tapAnywhereScoreBtn.gameObject.SetActive(true);
                tapAnywhereScoreBtn.onClick.RemoveAllListeners();
                tapAnywhereScoreBtn.onClick.AddListener(ShowEndPanel);
            }
        }
        else
        {
            ShowEndPanel();
        }
    }

    void ShowEndPanel()
    {
        if (scorePanel != null) scorePanel.SetActive(false);
        if (tapAnywhereScoreBtn != null) tapAnywhereScoreBtn.gameObject.SetActive(false);

        if (endPanel != null)
        {
            if (huds.Length > 1 && huds[1] != null) huds[1].SetActive(true);
            endPanel.gameObject.SetActive(true);
            endPanel.onComplete = () =>
            {
                if (huds.Length > 1 && huds[1] != null) huds[1].SetActive(false);
                if (levelCompleteManager != null) levelCompleteManager.OnLevelComplete();
            };
        }
        else
        {
            if (levelCompleteManager != null) levelCompleteManager.OnLevelComplete();
        }
    }

    void SaveLevelData()
    {
        string uid = GlobalUserData.UserId;
        if (string.IsNullOrEmpty(uid)) return;

        var db = FirebaseFirestore.DefaultInstance;
        var userRef = db.Collection("users").Document(uid);

        userRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (!task.IsCompletedSuccessfully) return;
            if (task.Result.TryGetValue("level7_data", out object _)) return;

            userRef.UpdateAsync("level7_data", new Dictionary<string, object>
            {
                { "trust", trustCount },
                { "doubt", doubtCount }
            });
        });
    }
}
