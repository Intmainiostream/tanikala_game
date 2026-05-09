using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

public class Level5QuestManager : MonoBehaviour
{
    [System.Serializable]
    public class BallotEntry
    {
        public GameObject ballot;

        [Header("Tick for ballots 9 and 10 — hides Hindi Aprubado button")]
        public bool approveOnly;

        [Header("Leave empty to not show")]
        [TextArea] public string dialogueText;
        [TextArea] public string innerMonologueText;
        [TextArea] public string approveText;
        [TextArea] public string notApproveText;

        public string lessonTitle;
        [TextArea] public string lessonDescription;

        [Header("Sounds")]
        public AudioClip dialogueSound;
        public AudioClip innerMonologueSound;
        public AudioClip approveSound;
        public AudioClip notApproveSound;
    }

    [Header("Ballot Sequence")]
    public BallotEntry[] ballots;

    [Header("Pause after ballot 8 (index 7)")]
    public PanelSequence pausePanel;

    [Header("Panels")]
    public GameObject questPanel;
    public GameObject dialoguePanel;
    public GameObject innerMonologuePanel;
    public GameObject choicesPanel;
    public GameObject responsePanel;
    public GameObject lessonPanel;

    [Header("Dialogue")]
    public TMP_Text dialogueText;
    public TMP_Text innerMonologueText;

    [Header("Choices")]
    public Button aprubadoBtn;
    public Button hindiAprubadoBtn;

    [Header("Response")]
    public TMP_Text responseText;

    [Header("Lesson")]
    public TMP_Text lessonTitleText;
    public TMP_Text lessonDescriptionText;

    [Header("Tap Anywhere")]
    public Button tapAnywhereBtn;

    [Header("HUDs")]
    public GameObject[] huds;

    [Header("Level Complete")]
    public LevelCompleteManager levelCompleteManager;
    public PanelSequence endPanel;

    [Header("Stamp Effect")]
    public GameObject stampImage;

    [Header("Stamp Sound")]
    public AudioClip stampSound;

    [Header("Supervisor Voice (wrong choice)")]
    public AudioClip supervisorScoldsVoice;

    [Header("Inner Monologue Voice (wrong choice)")]
    public AudioClip iHaveToDoItVoice;

    [Header("Score Panel")]
    public GameObject scorePanel;
    public TMP_Text approveCountText;
    public TMP_Text notApproveCountText;
    public Button tapAnywhereScoreBtn;

    private AudioSource audioSource;
    private int currentIndex = 0;
    private int approveCount = 0, notApproveCount = 0;
    private bool currentBallotCounted = false;

    private enum QuestState { Ballot, Dialogue, Choices, Response, WrongResponse, Lesson }
    private QuestState state;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        questPanel.SetActive(false);
        tapAnywhereBtn.onClick.AddListener(OnTap);
        aprubadoBtn.onClick.AddListener(() => OnChoice(true));
        hindiAprubadoBtn.onClick.AddListener(() => OnChoice(false));
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

        foreach (BallotEntry entry in ballots)
            if (entry.ballot != null) entry.ballot.SetActive(false);

        if (ballots[0].ballot != null) ballots[0].ballot.SetActive(true);

        ShowBallot();
    }

    void ShowBallot()
    {
        currentBallotCounted = false;
        state = QuestState.Ballot;

        dialoguePanel.SetActive(false);
        innerMonologuePanel.SetActive(false);
        choicesPanel.SetActive(false);
        responsePanel.SetActive(false);
        lessonPanel.SetActive(false);
        tapAnywhereBtn.gameObject.SetActive(true);
        if (stampImage != null) stampImage.SetActive(false);

        PlaySound(stampSound);
    }

    void OnTap()
    {
        switch (state)
        {
            case QuestState.Ballot:        ShowDialogue();    break;
            case QuestState.Dialogue:      ShowChoices();     break;
            case QuestState.Response:      ShowLesson();      break;
            case QuestState.WrongResponse: StartCoroutine(LoopAfterScold()); break;
            case QuestState.Lesson:        NextBallot();      break;
        }
    }

    void ShowDialogue()
    {
        state = QuestState.Dialogue;
        var entry = ballots[currentIndex];

        dialogueText.text = entry.dialogueText;
        bool showDialogue = !string.IsNullOrEmpty(entry.dialogueText);
        dialoguePanel.SetActive(showDialogue);
        if (showDialogue) PlaySound(entry.dialogueSound);

        innerMonologueText.text = entry.innerMonologueText;
        bool showInner = !string.IsNullOrEmpty(entry.innerMonologueText);
        innerMonologuePanel.SetActive(showInner);
        if (showInner && !showDialogue) PlaySound(entry.innerMonologueSound);
    }

    void ShowChoices()
    {
        state = QuestState.Choices;
        dialoguePanel.SetActive(false);
        innerMonologuePanel.SetActive(false);
        choicesPanel.SetActive(true);
        tapAnywhereBtn.gameObject.SetActive(false);

        hindiAprubadoBtn.gameObject.SetActive(!ballots[currentIndex].approveOnly);
    }

    void OnChoice(bool approved)
    {
        if (!currentBallotCounted) { if (approved) approveCount++; else notApproveCount++; currentBallotCounted = true; }
        var entry = ballots[currentIndex];
        choicesPanel.SetActive(false);

        if (!approved)
        {
            state = QuestState.WrongResponse;
            string wrongText = entry.notApproveText;
            responseText.text = wrongText;
            bool showWrong = !string.IsNullOrEmpty(wrongText);
            responsePanel.SetActive(showWrong);
            if (showWrong) PlaySound(entry.notApproveSound);
            tapAnywhereBtn.gameObject.SetActive(true);
            return;
        }

        state = QuestState.Response;

        if (stampImage != null)
        {
            stampImage.SetActive(false);
            stampImage.SetActive(true);
        }

        string text = entry.approveText;
        responseText.text = text;
        bool showResponse = !string.IsNullOrEmpty(text);
        responsePanel.SetActive(showResponse);
        if (showResponse) PlaySound(entry.approveSound);

        tapAnywhereBtn.gameObject.SetActive(true);
    }

    System.Collections.IEnumerator LoopAfterScold()
    {
        responsePanel.SetActive(false);
        tapAnywhereBtn.gameObject.SetActive(false);

        if (supervisorScoldsVoice != null)
        {
            PlaySound(supervisorScoldsVoice);
            yield return new WaitForSeconds(supervisorScoldsVoice.length);
        }
        if (iHaveToDoItVoice != null)
        {
            PlaySound(iHaveToDoItVoice);
            yield return new WaitForSeconds(iHaveToDoItVoice.length);
        }
        ShowBallot();
    }

    void ShowLesson()
    {
        state = QuestState.Lesson;
        responsePanel.SetActive(false);
        if (stampImage != null) stampImage.SetActive(false);

        var entry = ballots[currentIndex];
        bool hasLesson = !string.IsNullOrEmpty(entry.lessonTitle) || !string.IsNullOrEmpty(entry.lessonDescription);

        if (hasLesson)
        {
            lessonTitleText.text = entry.lessonTitle;
            lessonDescriptionText.text = entry.lessonDescription;
            lessonPanel.SetActive(true);
        }
        else
        {
            lessonPanel.SetActive(false);
            NextBallot();
        }
    }

    void NextBallot()
    {
        if (stampImage != null) stampImage.SetActive(false);
        if (ballots[currentIndex].ballot != null)
            ballots[currentIndex].ballot.SetActive(false);

        currentIndex++;

        if (currentIndex >= ballots.Length)
        {
            EndQuest();
            return;
        }

        if (currentIndex == 8 && pausePanel != null)
        {
            questPanel.SetActive(false);
            if (huds.Length > 1 && huds[1] != null) huds[1].SetActive(true);
            pausePanel.gameObject.SetActive(true);
            int indexAfterPause = currentIndex;
            pausePanel.onComplete = () =>
            {
                if (huds.Length > 1 && huds[1] != null) huds[1].SetActive(false);
                questPanel.SetActive(true);
                if (ballots[indexAfterPause].ballot != null)
                    ballots[indexAfterPause].ballot.SetActive(true);
                ShowBallot();
            };
            return;
        }

        if (ballots[currentIndex].ballot != null)
            ballots[currentIndex].ballot.SetActive(true);

        ShowBallot();
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
        if (approveCountText != null) approveCountText.text = approveCount.ToString();
        if (notApproveCountText != null) notApproveCountText.text = notApproveCount.ToString();

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
            if (task.Result.TryGetValue("level5_data", out object _)) return;

            userRef.UpdateAsync("level5_data", new Dictionary<string, object>
            {
                { "approve", approveCount },
                { "notApprove", notApproveCount }
            });
        });
    }
}
